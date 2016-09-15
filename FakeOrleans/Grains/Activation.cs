using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FakeOrleans.Grains
{
    
    public interface IActivation
    {
        Grain Grain { get; }
        ActivationStatus Status { get; }
        
        GrainPlacement Placement { get; }
        MockTimerRegistry Timers { get; }
        StreamReceiverRegistry Receivers { get; }
        
        Task<TResult> Perform<TResult>(Func<IActivation, Task<TResult>> fn, RequestMode mode = RequestMode.Unspecified);
        Task Deactivate();
    }




    public enum ActivationStatus
    {
        Unactivated,
        Activated,
        Deactivated
    }



    public class Activation : IActivation
    {
        readonly IRequestRunner _runner;
        readonly GrainFac _grainFac;
        
        public Activation(GrainPlacement placement, IRequestRunner runner, GrainFac grainFac) 
        {
            Placement = placement;

            _runner = runner;
            _grainFac = grainFac;

            Timers = new MockTimerRegistry(null); //!!!!!!!!!
            Receivers = new StreamReceiverRegistry(null); //!!!!!!!!!!
        }


        public GrainPlacement Placement { get; private set; }
        public MockTimerRegistry Timers { get; private set; }
        public StreamReceiverRegistry Receivers { get; private set; }


        Grain _grain = null;

        public Grain Grain {
            get { return _grain; } //DEBUG ONLY???
        }


        volatile ActivationStatus _status = ActivationStatus.Unactivated;

        public ActivationStatus Status {
            get { return _status; }
        }



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
                        _grain = _grainFac.Create(Placement, this, null);

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

                return await _runner.Perform(() => fn(this), mode);
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
                                
            _runner.Close(() => {
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
