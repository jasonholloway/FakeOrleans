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

namespace MockOrleans.Grains
{        

    public class GrainHarness : IGrainEndpoint, IGrainRuntime, IDisposable
    {
        public readonly MockFixture Fixture;
        public readonly GrainKey Key;
        public readonly GrainSpec Spec;
        public readonly TaskScheduler Scheduler;
        public readonly RequestRegistry Requests;
        public readonly MockTimerRegistry Timers;
        
        IGrain Grain { get; set; } = null;
        
        
        public GrainHarness(MockFixture fx, GrainKey key) 
        {
            Fixture = fx;
            Key = key;
            Spec = GrainSpec.GetFor(key.ConcreteType);
            Scheduler = new GrainTaskScheduler(fx.Scheduler);
            Requests = new RequestRegistry(fx.Requests);
            Timers = new MockTimerRegistry(this);
        }


        //specially for injecting
        public GrainHarness(MockFixture fx, GrainKey key, IGrain grain) 
            : this(fx, key)
        {
            Grain = grain;
        }
        
                
                
        public async Task Deactivate() 
        {
            await _smActive.WaitAsync();  //uncomfortable with semaphores being used here... for public use should have deactivateonidle only - which we already have below...

            try {
                Timers.Clear();

                await ((Grain)Grain).OnDeactivateAsync();
                
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
        

        public Task<TResult> Invoke<TResult>(Func<Task<TResult>> fn, bool activate = true) 
        {
            var t = new Task<Task<TResult>>(async () => {
                Requests.Increment();       //PROBLEM NOW OF ENSURING ONLY ONE ACTIVATION IS EMBARKED UPON...
                if(Spec.SerializesRequests) await _smActive.WaitAsync();

                try {
                    if(activate || Grain != null) {
                        var grain = Grain ?? (Grain = await ActivateGrain());
                        return await fn();
                    }

                    return default(TResult);
                }
                catch(Exception ex) {
                    throw ex;
                }
                finally {
                    Requests.Decrement();
                    if(Spec.SerializesRequests) _smActive.Release();
                }
            });
            
            t.Start(Scheduler);
            
            return t.Unwrap();
        }


        public Task Invoke(Func<Task> fn, bool activate = true)
            => Invoke(() => fn().Box());

        public Task<TResult> Invoke<TResult>(MethodInfo method, object[] args)
            => Invoke(() => CallMethod<TResult>(method, args), true);

        


        Task<IGrain> ActivateGrain() {
            return GrainActivator.Activate(this, Fixture.Store, Key);
        }


        //below guaranteed to be single-threaded
        async Task<TResult> CallMethod<TResult>(MethodInfo method, object[] args) 
        {
            if(typeof(TResult).Equals(typeof(VoidType))) {
                await (Task)method.Invoke(Grain, args);
                return default(TResult);
            }
                        
            return await (Task<TResult>)method.Invoke(Grain, args);
        }
        
        
        


        //FOR DEBUGGING ONLY! DON'T USE ELSEWHERE!!!
        public async Task<IGrain> GetGrain() {
            var success = await _smActive.WaitAsync(1000);

            if(!success) {
                throw new InvalidOperationException("Can't access grain during method call!");
            }

            try {
                return Grain ?? (Grain = await ActivateGrain());
            }
            finally {
                _smActive.Release();
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
            get { return Fixture.Reminders.GetRegistry(Key); }
        }

        IStreamProviderManager IGrainRuntime.StreamProviderManager {
            get { return Fixture.Streams; }
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
