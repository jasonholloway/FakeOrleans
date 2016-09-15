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

                

    public static class ActivationFac
    {        
        public static IActivation Create(FixtureCtx fx, GrainPlacement placement) 
        {
            var scheduler = new GrainTaskScheduler(fx.Scheduler, fx.Exceptions);
            var runner = new RequestRunner(scheduler, fx.Exceptions, fx.Runner); //isolate by default? - depends on spec
            var serializer = new FakeSerializer(null); //!!! //!!!!!! //!!!!!!!!!!!!!!!!!!!!!!!!!

            var ctx = new ActivationCtx() {
                Placement = placement,
                Fixture = fx,
                Scheduler = scheduler,
                Runner = runner,
                Timers = new MockTimerRegistry(scheduler),
                Serializer = serializer, //!!!!!
                Receivers = new StreamReceiverRegistry(serializer)
            };

            return new Activation(ctx);
        }
    }



    public class FixtureCtx
    {
        public TaskScheduler Scheduler;
        public RequestRunner Runner;
        public ExceptionSink Exceptions;
        public IServiceProvider Services;
        public IGrainFactory GrainFactory;
        public ReminderRegistry Reminders;
        public StreamRegistry Streams;

        public Func<FixtureCtx, GrainPlacement, IActivation> ActivationFac;
        public Func<ActivationCtx, IActivation, Grain> GrainFac;
    }


    public class ActivationCtx
    {
        public GrainPlacement Placement;
        public FixtureCtx Fixture;
        public TaskScheduler Scheduler;
        public RequestRunner Runner;
        public MockTimerRegistry Timers;
        public FakeSerializer Serializer;
        public StreamReceiverRegistry Receivers;
        public GrainReminderRegistry Reminders;
        public StorageCell Storage;
    }

    
        


    public interface IActivation
    {
        Grain Grain { get; }
        ActivationStatus Status { get; }

        StreamReceiverRegistry Receivers { get; }

        Task<TResult> Perform<TResult>(Func<IActivation, Task<TResult>> fn, RequestMode mode = RequestMode.Unspecified);
        Task Deactivate();
    }



    public class Activation : IActivation
    {
        readonly ActivationCtx _ctx;
        
        public Activation(ActivationCtx ctx) {
            _ctx = ctx;
        }
                

        Grain _grain = null;
        public Grain Grain { get { return _grain; } }
        
        volatile ActivationStatus _status = ActivationStatus.Unactivated;
        public ActivationStatus Status { get { return _status; } }
        
        public StreamReceiverRegistry Receivers { get { return _ctx.Receivers; } }


        
        SemaphoreSlim _sm = new SemaphoreSlim(1);

        public async Task<TResult> Perform<TResult>(Func<IActivation, Task<TResult>> fn, RequestMode mode = RequestMode.Unspecified) 
        {
            try {
                await _sm.WaitAsync();

                try {
                    if(_status == ActivationStatus.Deactivated) {
                        throw new DeactivatedException();
                    }

                    if(_grain == null) {
                        _grain = _ctx.Fixture.GrainFac(_ctx, this);

                    	await _ctx.Runner.Perform(async () => {
												await _grain.OnActivateAsync();
												_status = ActivationStatus.Activated;
                                                return true;
											}, RequestMode.Isolated);
					}
                }
                finally {
                    _sm.Release();
                }

                return await _ctx.Runner.Perform(() => fn(this), mode);
            }
            catch(RequestRunnerClosedException) {
                throw new DeactivatedException();
            }
        }

        public Task Deactivate() 
        {    
            if(_status == ActivationStatus.Unactivated) {
                throw new NotImplementedException("Activation not yet activated!");
            }
                                
            _ctx.Runner.Close(() => {
                _status = ActivationStatus.Deactivated;
                return Grain.OnDeactivateAsync();
            });

            return Task.CompletedTask;
        }

    }





    public static class ActivationExtensions
    {
        public static Task<TResult> Invoke<TResult>(this IActivation act, MethodInfo method, byte[][] argData) {
            throw new NotImplementedException();
        }
    }
    

}
