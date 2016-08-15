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
using Orleans.Providers;
using MockOrleans.Streams;

namespace MockOrleans.Grains
{    

    public class GrainHarness : IGrainEndpoint, IGrainRuntime, IDisposable
    {
        public readonly MockFixture Fixture;
        public readonly GrainPlacement Placement;
        public readonly GrainSpec Spec;
        public readonly TaskScheduler Scheduler;
        public readonly MockSerializer Serializer;
        public readonly RequestRunner Requests;
        public readonly ExceptionSink Exceptions;
        public readonly MockTimerRegistry Timers;
        public readonly StreamReceiverRegistry StreamReceivers;

        
        IGrain Grain { get; set; } = null;
        
        
        public GrainHarness(MockFixture fx, GrainPlacement placement) 
        {
            Fixture = fx;
            Placement = placement;
            Spec = GrainSpec.GetFor(placement.Key.ConcreteType);
            Exceptions = new ExceptionSink(fx.Exceptions);
            Scheduler = new GrainTaskScheduler(fx.Scheduler, Exceptions);
            Serializer = new MockSerializer(new GrainContext(fx, this));
            Requests = new RequestRunner(Scheduler, Exceptions, fx.Requests, !Spec.IsReentrant);
            Timers = new MockTimerRegistry(this);
            StreamReceivers = new StreamReceiverRegistry(Serializer);
        }


        //specially for injecting
        public GrainHarness(MockFixture fx, GrainPlacement placement, IGrain grain) 
            : this(fx, placement)
        {
            Grain = grain;
        }
        
        

        Task _tActivating = null;
        public volatile bool IsDead = false;


        public async Task Activate() 
        {            
            if(_tActivating == null) {  //failures should reset tActivating to null
                _tActivating = Requests.Perform(ActivateGrain, true);   //and exceptions? returning them here seems reasonable
            }                                                           //though tActivating needs to revert to null 
                                                                        //Requests.Perform also packages errors...
            await _tActivating;
        }



        public async Task Deactivate() 
        {
            IsDead = true; //should stop further requests being queued... TO DO

            await Requests.Perform(DeactivateGrain, true);
        }





        async Task ActivateGrain() {
            Grain = await GrainActivator.Activate(this, Placement, Fixture.Stores[Placement.Key]);
        }


        async Task DeactivateGrain() 
        {
            Timers.Clear(); //timers will themselves raise interleavable requests, and so will be muffled by IsDead

            await ((Grain)Grain).OnDeactivateAsync();

            _tActivating = null; 
            Grain = null;
        }

        


        #region IGrainEndpoint
        
        public Task<TResult> Invoke<TResult>(Func<Task<TResult>> fn)
            => Requests.Perform(fn);  //isolation to default
                
        public Task Invoke(Func<Task> fn)
            => Invoke(() => fn().Box());

        public Task<TResult> Invoke<TResult>(MethodInfo method, byte[][] argData)
            => Invoke(() => CallMethod<TResult>(method, argData));
        
        public Task Invoke<TInterface>(Func<TInterface, Task> fn)
            => Invoke(() => fn((TInterface)Grain));



        //below guaranteed to be single-threaded
        async Task<TResult> CallMethod<TResult>(MethodInfo method, byte[][] argData) 
        {
            var args = argData    //this should be done before entering the single-request zone, really - still on this grain's scheduler, though
                        .Select(d => Serializer.Deserialize(d))
                        .ToArray();
            
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
        public Task<IGrain> GetGrain()
            => Task.FromResult(Grain);
        

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
            get { return new StreamProviderManagerAdaptor(this, Fixture.Streams); }
        }

        IServiceProvider IGrainRuntime.ServiceProvider {
            get { return Fixture.Services; }
        }
        
        void IGrainRuntime.DeactivateOnIdle(Grain grain) {
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
                    //_smActive.Dispose();
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
