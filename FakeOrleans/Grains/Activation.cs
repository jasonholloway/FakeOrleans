using Orleans;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Orleans.Streams;
using Orleans.Timers;
using FakeOrleans.Reminders;
using FakeOrleans.Streams;

namespace FakeOrleans.Grains
{
    
    public enum ActivationStatus
    {
        Unactivated,
        Activated,
        Deactivated
    }

                

    //public static class ActivationFac
    //{        
    //    public static IActivation Create(Fixture fx, GrainPlacement placement) 
    //    {
    //        var scheduler = new GrainTaskScheduler(fx.Scheduler, fx.Exceptions);
    //        var runner = new RequestRunner(scheduler, fx.Exceptions, fx.Requests); //isolate by default? - depends on spec
            
    //        var ctx = new ActivationCtx() {
    //            Placement = placement,
    //            Fixture = fx,
    //            Scheduler = scheduler,
    //            Runner = runner,
    //            Timers = new MockTimerRegistry(scheduler),
    //            Serializer = fx.Serializer,
    //            Receivers = new StreamReceiverRegistry(fx.Serializer)
    //        };

    //        return new Activation();
    //    }
    //}

    
    //public class ActivationCtx
    //{
    //    public Fixture Fixture;
    //    public GrainPlacement Placement;
    //    public TaskScheduler Scheduler;
    //    public RequestRunner Runner;
    //    public MockTimerRegistry Timers;
    //    public FakeSerializer Serializer;
    //    public StreamReceiverRegistry Receivers;
    //    public GrainReminderRegistry Reminders;
    //    public StorageCell Storage;
    //}

    

    public interface IActivation
    {

    }


    public class Activation_New : IActivation
    {
        public readonly Fixture Fixture;
        public readonly GrainPlacement Placement;        
        public readonly TaskScheduler Scheduler;
        public readonly RequestRunner Runner;
        public readonly FakeSerializer Serializer;
        public readonly MockTimerRegistry Timers;
        public readonly StreamReceiverRegistry Receivers;
        public readonly GrainReminderRegistry Reminders;
        public readonly StorageCell Storage;
        public readonly ActivationDispatcher Dispatcher;

        public Activation_New(Fixture fx, GrainPlacement placement) 
        {
            Fixture = fx;
            Placement = placement;

            Serializer = fx.Serializer;
            Scheduler = new GrainTaskScheduler(fx.Scheduler, fx.Exceptions);
            Runner = new RequestRunner(Scheduler, fx.Exceptions, fx.Requests, true); //default isolation???
            
            Dispatcher = new ActivationDispatcher(Runner, this, () => GrainPrimer.Build(this));

            Timers = new MockTimerRegistry(Scheduler);
            Receivers = new StreamReceiverRegistry(Serializer);

            Storage = fx.Stores.GetStorage(placement.Key);
            Reminders = fx.Reminders.GetRegistry(placement.Key);
        }

    }




    public interface IActivationDispatcher
    {
        //Grain Grain { get; } //grain should only be got via performances...
        //ActivationStatus Status { get; }

        //StreamReceiverRegistry Receivers { get; }

        Task<TResult> Perform<TResult>(Func<IActivation, Grain, Task<TResult>> fn, RequestMode mode = RequestMode.Unspecified);
        Task Deactivate();
    }


    public class ActivationDispatcher : IActivationDispatcher
    {
        readonly RequestRunner _runner;
        readonly IActivation _act;
        readonly Func<Task<Grain>> _grainFac;

        public ActivationDispatcher(RequestRunner runner, IActivation act, Func<Task<Grain>> grainFac) {
            _runner = runner;
            _act = act;
            _grainFac = grainFac;
        }


        volatile ActivationStatus _status = ActivationStatus.Unactivated;

        Grain _grain = null;
        SemaphoreSlim _sm = new SemaphoreSlim(1);

        public async Task<TResult> Perform<TResult>(Func<IActivation, Grain, Task<TResult>> fn, RequestMode mode = RequestMode.Unspecified) {
            try {
                await _sm.WaitAsync();

                try {
                    if(_status == ActivationStatus.Deactivated) {
                        throw new DeactivatedException();
                    }

                    if(_grain == null) {
                        _grain = await _grainFac();

                        await _runner.Perform(async () => {
                            await _grain.OnActivateAsync();
                            _status = ActivationStatus.Activated;
                            return true;
                        }, RequestMode.Isolated);
                    }
                }
                finally {
                    _sm.Release();
                }

                return await _runner.Perform(() => fn(_act, _grain), mode);
            }
            catch(RequestRunnerClosedException) {
                throw new DeactivatedException();
            }
        }


        public Task Deactivate() {
            if(_status == ActivationStatus.Unactivated) {
                throw new NotImplementedException("Activation not yet activated!");
            }

            _runner.Close(() => {
                _status = ActivationStatus.Deactivated;
                return _grain.OnDeactivateAsync();
            });

            return Task.CompletedTask;
        }

    }






    //public class Activation : IActivation
    //{
    //    readonly ActivationCtx _ctx;
        
    //    public Activation(ActivationCtx ctx) {
    //        _ctx = ctx;
    //    }
                

    //    Grain _grain = null;
    //    public Grain Grain { get { return _grain; } }
        
    //    volatile ActivationStatus _status = ActivationStatus.Unactivated;
    //    public ActivationStatus Status { get { return _status; } }
        
    //    public StreamReceiverRegistry Receivers { get { return _ctx.Receivers; } }


        
    //    SemaphoreSlim _sm = new SemaphoreSlim(1);

    //    public async Task<TResult> Perform<TResult>(Func<IActivation, Task<TResult>> fn, RequestMode mode = RequestMode.Unspecified) 
    //    {
    //        try {
    //            await _sm.WaitAsync();

    //            try {
    //                if(_status == ActivationStatus.Deactivated) {
    //                    throw new DeactivatedException();
    //                }

    //                if(_grain == null) {
    //                    _grain = GrainBuilder.Build(_ctx, this);

    //                	await _ctx.Runner.Perform(async () => {
				//								await _grain.OnActivateAsync();
				//								_status = ActivationStatus.Activated;
    //                                            return true;
				//							}, RequestMode.Isolated);
				//	}
    //            }
    //            finally {
    //                _sm.Release();
    //            }

    //            return await _ctx.Runner.Perform(() => fn(this), mode);
    //        }
    //        catch(RequestRunnerClosedException) {
    //            throw new DeactivatedException();
    //        }
    //    }

    //    public Task Deactivate() 
    //    {    
    //        if(_status == ActivationStatus.Unactivated) {
    //            throw new NotImplementedException("Activation not yet activated!");
    //        }
                                
    //        _ctx.Runner.Close(() => {
    //            _status = ActivationStatus.Deactivated;
    //            return Grain.OnDeactivateAsync();
    //        });

    //        return Task.CompletedTask;
    //    }

    //}





    //public static class ActivationExtensions
    //{
    //    public static Task<TResult> Invoke<TResult>(this IActivation act, MethodInfo method, byte[][] argData) {
    //        throw new NotImplementedException();
    //    }
    //}
    

}
