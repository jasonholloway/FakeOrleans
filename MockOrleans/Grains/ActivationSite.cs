using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockOrleans.Grains
{

    public class DeactivatedException : Exception { }


    public interface IActivation
    {
        Grain Grain { get; }
        StreamReceiverRegistry Receivers { get; }

        Task<TResult> Perform<TResult>(Func<IActivation, Task<TResult>> fn, RequestMode mode = RequestMode.Unspecified);
        Task Deactivate();
    }
    


    public interface IActivationProvider
    {
        IActivation GetActivation();
    }



    public interface IActivationSite
    {
        Task<TResult> Dispatch<TResult>(Func<IActivation, Task<TResult>> fn, RequestMode mode);
    }



    public class ActivationSite : IActivationSite
    {
        readonly IActivationProvider _actProv;

        IActivation _act = null;
        object _sync = new object();

        public ActivationSite(IActivationProvider actProv) {
            _actProv = actProv;
        }

        public Task<TResult> Dispatch<TResult>(Func<IActivation, Task<TResult>> fn, RequestMode mode = RequestMode.Unspecified) {
            IActivation act = null;

            lock(_sync) {
                act = _act ?? (_act = _actProv.GetActivation());
            }

            try {
                return act.Perform(fn, mode);
            }
            catch(DeactivatedException) {
                lock(_sync) _act = null;
                return Dispatch(fn, mode);
            }
        }

    }


}
