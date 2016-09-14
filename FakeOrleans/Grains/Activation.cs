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
        

        public Grain Grain {
            get { return _grain; } //DEBUG ONLY???
        }



        SemaphoreSlim _sm = new SemaphoreSlim(1);

        public async Task<TResult> Perform<TResult>(Func<IActivation, Task<TResult>> fn, RequestMode mode = RequestMode.Unspecified) 
        {
            await _sm.WaitAsync();

            try {
                if(_grain == null) {
                    _grain = _grainFac(this); //await _grainFac.Create(_placement, this);
                    await _runner.Perform(() => _grain.OnActivateAsync().Box(), RequestMode.Isolated);
                }
            }
            finally {
                _sm.Release();
            }

            return await _runner.Perform(() => fn(this), mode);
        }


        public Task Deactivate() {
            _runner.PerformAndClose(() => Grain.OnDeactivateAsync());
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
