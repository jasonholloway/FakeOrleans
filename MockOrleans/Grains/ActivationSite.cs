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
        Task<TResult> Perform<TResult>(Func<IActivation, Task<TResult>> fn);
    }


    public interface IActivationProvider
    {
        IActivation GetActivation();
    }



    public interface IActivationSite
    {
        Task<TResult> Dispatch<TResult>(Func<IActivation, Task<TResult>> fn);
    }



    public class ActivationSite : IActivationSite
    {
        readonly IActivationProvider _actProv;

        IActivation _act = null;
        object _sync = new object();

        public ActivationSite(IActivationProvider actProv) {
            _actProv = actProv;
        }

        public Task<TResult> Dispatch<TResult>(Func<IActivation, Task<TResult>> fn) {
            IActivation act = null;

            lock(_sync) {
                act = _act ?? (_act = _actProv.GetActivation());
            }

            try {
                return act.Perform(fn);
            }
            catch(DeactivatedException) {
                lock(_sync) _act = null;
                return Dispatch(fn);
            }
        }

    }


}
