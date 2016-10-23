using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeOrleans.Grains
{

    public class DeactivatedException : Exception { }
    


    public interface IActivationSite
    {
        Task<TResult> Dispatch<TResult>(Func<IGrainContext, Task<TResult>> fn, RequestMode mode);
    }



    public class ActivationSite : IActivationSite
    {
        readonly Func<Placement, IActivationDispatcher> _dispFac;

        Placement _placement;
        IActivationDispatcher _disp = null;
        object _sync = new object();

        public ActivationSite(Func<Placement, IActivationDispatcher> actDispFac) {
            _dispFac = actDispFac;
        }
        

        public void Init(Placement placement) {
            _placement = placement;
        }
        
        
        public Task<TResult> Dispatch<TResult>(Func<IGrainContext, Task<TResult>> fn, RequestMode mode = RequestMode.Unspecified) 
        {
            IActivationDispatcher disp = null;

            lock(_sync) {
                disp = _disp ?? (_disp = _dispFac(_placement));
            }

            try {
                return disp.Perform(fn, mode);
            }
            catch(DeactivatedException) {
                lock(_sync) _disp = null;
                return Dispatch(fn, mode);
            }
        }

    }


}
