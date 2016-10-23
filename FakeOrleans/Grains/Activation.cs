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
    public enum ActivationStatus {
        Unactivated,
        Activated,
        Deactivated
    }


    public interface IActivation
    {
        Placement Placement { get; }
        IActivationDispatcher Dispatcher { get; }
    }

    public interface IStreamContext
    {
        Placement Placement { get; }
        StreamRegistry Streams { get; }
        StreamReceiverRegistry Receivers { get; }
        FakeSerializer Serializer { get; }
    }



    public class Activation_New : IActivation, IStreamContext
    {
        public Placement Placement { get; private set; }
        public IActivationDispatcher Dispatcher { get; private set; }

        readonly Fixture _fx;
        readonly Placement _placement;
        readonly FakeSerializer _serializer;
        readonly MockTimerRegistry _timers;
        readonly StreamReceiverRegistry _receivers;
        readonly GrainReminderRegistry _reminders;
        readonly TaskScheduler _scheduler;
        readonly RequestRunner _runner;
        readonly StorageCell _storage;

        public Activation_New(Fixture fx, Placement placement) {
            _fx = fx;
            _placement = placement;
            _serializer = fx.Serializer;
            _scheduler = new GrainTaskScheduler(fx.Scheduler, fx.Exceptions);
            _runner = new RequestRunner(_scheduler, fx.Exceptions, fx.Requests, true); //default isolation???
            _timers = new MockTimerRegistry(_scheduler);
            _receivers = new StreamReceiverRegistry(_serializer);
            _storage = fx.Stores.GetStorage(placement);
            _reminders = fx.Reminders.GetRegistry(placement);

            Placement = placement;
            Dispatcher = new ActivationDispatcher(_runner, CreateGrainContext);
        }


        #region IStreamContext

        Placement IStreamContext.Placement {
            get { return _placement; }
        }

        StreamRegistry IStreamContext.Streams {
            get { return _fx.Streams; }
        }

        StreamReceiverRegistry IStreamContext.Receivers {
            get { return _receivers; }
        }

        FakeSerializer IStreamContext.Serializer {
            get { return _serializer; }
        }

        #endregion


        async Task<IGrainContext> CreateGrainContext() 
        {
            var grain = await GrainConstructor.New(
                                        _placement.ConcreteKey, 
                                        new _GrainRuntime(this), 
                                        _fx.Services,
                                        _storage,
                                        _fx.Serializer);    
                    
            return new _GrainContext(this, grain);
        }

        class _GrainContext : IGrainContext
        {
            public Grain Grain { get; private set; }
            public Placement Placement { get; private set; }
            public FakeSerializer Serializer { get; private set; }

            public _GrainContext(Activation_New act, Grain grain) {
                Grain = grain;
                Placement = act.Placement;
                Serializer = act._serializer;
            }
        }




        class _GrainRuntime : IGrainRuntime
        {
            public Guid ServiceId { get; private set; } = Guid.Empty;
            public string SiloIdentity { get; private set; } = "Silo";

            Activation_New _act;

            public _GrainRuntime(Activation_New act) {
                _act = act;
            }
            
            public IGrainFactory GrainFactory {
                get { return _act._fx.GrainFactory; }
            }
                        
            public IReminderRegistry ReminderRegistry {
                get { return _act._reminders; }
            }
            
            public IServiceProvider ServiceProvider {
                get { return _act._fx.Services; }
            }

            public IStreamProviderManager StreamProviderManager {
                get { throw new NotImplementedException(); }
            }
            
            public ITimerRegistry TimerRegistry {
                get { return _act._timers; }
            }

            public void DeactivateOnIdle(Grain grain) {
                _act.Dispatcher.Deactivate().SinkExceptions(_act._fx.Exceptions);
            }

            public void DelayDeactivation(Grain grain, TimeSpan timeSpan) {
                throw new NotImplementedException();
            }

            public Logger GetLogger(string loggerName) {
                throw new NotImplementedException();
            }
        }

    }



    public interface IGrainContext {
        Placement Placement { get; }
        Grain Grain { get; }
        FakeSerializer Serializer { get; }
    }



    public interface IActivationDispatcher
    {
        Task<TResult> Perform<TResult>(Func<IGrainContext, Task<TResult>> fn, RequestMode mode = RequestMode.Unspecified);
        Task Deactivate();
    }


    public class ActivationDispatcher : IActivationDispatcher
    {
        readonly RequestRunner _runner;
        readonly Func<Task<IGrainContext>> _ctxFac;

        public ActivationDispatcher(RequestRunner runner, Func<Task<IGrainContext>> ctxFac) {
            _runner = runner;
            _ctxFac = ctxFac;
        }


        volatile ActivationStatus _status = ActivationStatus.Unactivated;

        IGrainContext _ctx = null;
        SemaphoreSlim _sm = new SemaphoreSlim(1);

        public async Task<TResult> Perform<TResult>(Func<IGrainContext, Task<TResult>> fn, RequestMode mode = RequestMode.Unspecified) {
            try {
                await _sm.WaitAsync();

                try {
                    if(_status == ActivationStatus.Deactivated) {
                        throw new DeactivatedException();
                    }

                    if(_ctx == null) {
                        _ctx = await _ctxFac();

                        await _runner.Perform(async () => {
                            await _ctx.Grain.OnActivateAsync();
                            _status = ActivationStatus.Activated;
                            return true;
                        }, RequestMode.Isolated);
                    }
                }
                finally {
                    _sm.Release();
                }

                return await _runner.Perform(() => fn(_ctx), mode);
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
                return _ctx.Grain.OnDeactivateAsync();
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
