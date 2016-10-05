using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeOrleans.Grains
{

    public class DeactivatedException : Exception { }


    

    public interface IActivationProvider
    {
        IActivationDispatcher GetActivation();
    }



    public interface IActivationSite
    {
        IActivationDispatcher Activation { get; }
        Task<TResult> Dispatch<TResult>(Func<IActivation, Task<TResult>> fn, RequestMode mode);
    }



    public class ActivationSite : IActivationSite
    {
        readonly Func<GrainPlacement, IActivation> _actCreator;

        GrainPlacement _placement;
        IActivation _act = null;
        object _sync = new object();

        public ActivationSite(Func<GrainPlacement, IActivation> actCreator) {
            _actCreator = actCreator;
        }
        

        public void Init(GrainPlacement placement) {
            _placement = placement;
        }


        public IActivation Activation {
            get { return _act; }
        }
        
        public Task<TResult> Dispatch<TResult>(Func<IActivation, Task<TResult>> fn, RequestMode mode = RequestMode.Unspecified) 
        {
            IActivation act = null;

            lock(_sync) {
                act = _act ?? (_act = _actCreator(_placement));
            }

            try {
                return act.Dispatcher.Perform(fn, mode);
            }
            catch(DeactivatedException) {
                lock(_sync) _act = null;
                return Dispatch(fn, mode);
            }
        }

    }


}
