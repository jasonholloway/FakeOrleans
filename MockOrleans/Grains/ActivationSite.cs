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
        IActivation Activation { get; }
        Task<TResult> Dispatch<TResult>(Func<IActivation, Task<TResult>> fn, RequestMode mode);
    }



    public class ActivationSite : IActivationSite
    {
        readonly Func<IActivation>_actCreator;

        IActivation _act = null;
        object _sync = new object();

        public ActivationSite(Func<IActivation> actCreator) {
            _actCreator = actCreator;
        }
        
        public IActivation Activation {
            get { return _act; }
        }
        
        public Task<TResult> Dispatch<TResult>(Func<IActivation, Task<TResult>> fn, RequestMode mode = RequestMode.Unspecified) 
        {
            IActivation act = null;

            lock(_sync) {
                act = _act ?? (_act = _actCreator());
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
