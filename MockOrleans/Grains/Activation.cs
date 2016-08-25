using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MockOrleans.Grains
{
    public interface IGrainCreator
    {
        Task<Grain> Activate(IActivation act);
    }
    

    public class Activation : IActivation
    {
        readonly IGrainCreator _creator;
        readonly IRequestRunner _runner;

        public Activation(IGrainCreator creator, IRequestRunner runner) {
            _creator = creator;
            _runner = runner;
            Receivers = new StreamReceiverRegistry(new MockSerializer(new GrainContext(null, this))); //!!!!!!!
        }


        Grain _grain = null;

        public Grain Grain {
            get { return _grain; }
        }


        public StreamReceiverRegistry Receivers { get; private set; }



        SemaphoreSlim _sm = new SemaphoreSlim(1);

        public async Task<TResult> Perform<TResult>(Func<IActivation, Task<TResult>> fn, RequestMode mode = RequestMode.Unspecified) {
            await _sm.WaitAsync();

            try {
                if(_grain == null) {
                    _grain = await _runner.Perform(() => _creator.Activate(this), RequestMode.Isolated);
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
