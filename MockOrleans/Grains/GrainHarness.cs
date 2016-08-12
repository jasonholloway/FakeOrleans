using Orleans;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans.Streams;
using Orleans.Timers;
using System.Reflection;
using System.Threading;
using System.Collections.Concurrent;
using Orleans.Concurrency;
using System.Runtime.InteropServices;

namespace MockOrleans.Grains
{        

    public class GrainHarness : IGrainEndpoint, IGrainRuntime, IDisposable
    {
        public readonly MockFixture Fixture;
        public readonly GrainPlacement Placement;
        public readonly GrainSpec Spec;
        public readonly TaskScheduler Scheduler;
        public readonly RequestRegistry Requests;
        public readonly ExceptionSink Exceptions;
        public readonly MockTimerRegistry Timers;
        
        IGrain Grain { get; set; } = null;
        
        
        public GrainHarness(MockFixture fx, GrainPlacement placement) 
        {
            Fixture = fx;
            Placement = placement;
            Spec = GrainSpec.GetFor(placement.Key.ConcreteType);
            Exceptions = new ExceptionSink(fx.Exceptions);
            Scheduler = new GrainTaskScheduler(fx.Scheduler, Exceptions);
            Requests = new RequestRegistry(Scheduler, fx.Requests);
            Timers = new MockTimerRegistry(this);
        }


        //specially for injecting
        public GrainHarness(MockFixture fx, GrainPlacement placement, IGrain grain) 
            : this(fx, placement)
        {
            Grain = grain;
        }
        
        
        


        public async Task Deactivate() 
        {
            await _smActive.WaitAsync();  //uncomfortable with semaphores being used here... for public use should have deactivateonidle only - which we already have below...

            try {
                Timers.Clear();

                await ((Grain)Grain).OnDeactivateAsync();

                _tActivating = null; 
                Grain = null;
            }
            catch(Exception ex) {
                throw ex;
            }
            finally {
                _smActive.Release();
            }
        }

        


        #region IGrainEndpoint
        
        SemaphoreSlim _smActive = new SemaphoreSlim(1);
        Task _tActivating = null;

        public Task<TResult> Invoke<TResult>(Func<Task<TResult>> fn) 
        {
            //single-threaded below - though sometimes interleaved
            var t = new Task<Task<TResult>>(async () => {

                Requests.Increment();
                if(Spec.SerializesRequests) await _smActive.WaitAsync();

                try {               
                    if(_tActivating == null) {
                        _tActivating = ActivateGrain();
                    }
                    
                    await _tActivating;
                    
                    return await fn();                    
                }
                catch(Exception) {
                    throw; //strangely, swallowing exception unless rethrown (really???)
                }
                finally {
                    if(Spec.SerializesRequests) _smActive.Release();
                    Requests.Decrement();
                }
            });
            
            t.Start(Scheduler);
            
            return t.Unwrap();
        }


        public Task Invoke(Func<Task> fn)
            => Invoke(() => fn().Box());

        public Task<TResult> Invoke<TResult>(MethodInfo method, object[] args)
            => Invoke(() => CallMethod<TResult>(method, args));

        


        async Task ActivateGrain() {
            Grain = await GrainActivator.Activate(this, Placement, Fixture.Stores[Placement.Key]);
        }


        //below guaranteed to be single-threaded
        async Task<TResult> CallMethod<TResult>(MethodInfo method, object[] args) 
        {
            if(typeof(TResult).Equals(typeof(VoidType))) {

                try {
                    await (Task)method.Invoke(Grain, args);
                }
                catch(TargetInvocationException ex) {
                    throw ex.InnerException;
                }
                
                return default(TResult);
            }
                        
            return await (Task<TResult>)method.Invoke(Grain, args);
        }
        
        
        


        //FOR DEBUGGING ONLY! DON'T USE ELSEWHERE!!!
        public async Task<IGrain> GetGrain() 
        {
            Requests.Increment();
            var success = await _smActive.WaitAsync(1000);

            if(!success) {
                throw new InvalidOperationException("Can't access grain during method call!");
            }

            try {
                if(_tActivating == null) _tActivating = ActivateGrain();

                await _tActivating;

                return Grain;
            }
            finally {
                _smActive.Release();
                Requests.Decrement();
            }
        }
        
        #endregion
                       

        #region IGrainRuntime

        Guid IGrainRuntime.ServiceId { get; } = Guid.NewGuid();

        string IGrainRuntime.SiloIdentity { get; } = "SiloIdentity";

        IGrainFactory IGrainRuntime.GrainFactory {
            get { return Fixture.GrainFactory; }
        }

        ITimerRegistry IGrainRuntime.TimerRegistry {
            get { return Timers; } 
        }

        IReminderRegistry IGrainRuntime.ReminderRegistry {
            get { return Fixture.Reminders.GetRegistry(Placement.Key); }
        }

        IStreamProviderManager IGrainRuntime.StreamProviderManager {
            get { throw new NotImplementedException(); }
        }

        IServiceProvider IGrainRuntime.ServiceProvider {
            get { return Fixture.Services; }
        }
        
        void IGrainRuntime.DeactivateOnIdle(Grain grain) { //this reveals grainruntime to be a general service
            Requests.WhenIdle().ContinueWith(_ => Deactivate(), Scheduler);
        }

        void IGrainRuntime.DelayDeactivation(Grain grain, TimeSpan timeSpan) {
            throw new NotImplementedException();
        }

        public Logger GetLogger(string loggerName)
        {
            throw new NotImplementedException();
        }

        #endregion


        #region IDisposable

        bool disposedValue = false;

        protected virtual void Dispose(bool disposing) {
            if(!disposedValue) {
                if(disposing) {
                    _smActive.Dispose();
                    Timers.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose() {
            Dispose(true);
        }

        #endregion
        
    }



}
