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
        readonly GrainPlacement _placement;
        readonly IRequestRunner _runner;
        readonly Func<IActivation, Grain> _grainFac;
        
        Grain _grain = null;

        public Activation(GrainPlacement placement, IRequestRunner runner, Func<IActivation, Grain> grainFac) {
            _placement = placement;
            _runner = runner;
            _grainFac = grainFac;
        }


        Grain _grain = null;
        volatile ActivationStatus _status = ActivationStatus.Unactivated;

        

        public Grain Grain {
            get { return _grain; } //DEBUG ONLY???
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
                    	_grain = _grainFac(this); //await _grainFac.Create(_placement, this);
                    	await _runner.Perform(async () => {
												await _grain.OnActivateAsync();
												_status = ActivationStatus.Activated; 
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
            _runner.PerformAndClose(() => {
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
