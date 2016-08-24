using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockOrleans.Components
{

    public interface IDispatcher
    {
        Task<TResult> Dispatch<TResult>(GrainKey key, Func<Grain, Task<TResult>> fn);
    }
    


    public class Dispatcher : IDispatcher
    {
        readonly IPlacementDispatcher _innerDispatcher;
        readonly Func<GrainKey, GrainPlacement> _placer;

        public Dispatcher(Func<GrainKey, GrainPlacement> placer, IPlacementDispatcher innerDisp) {
            _placer = placer;
            _innerDispatcher = innerDisp;
        }


        public Task<TResult> Dispatch<TResult>(GrainKey key, Func<Grain, Task<TResult>> fn) {
            var placement = _placer(key);

            return _innerDispatcher.Dispatch(placement, fn);
        }

    }

}
