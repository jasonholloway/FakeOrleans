using FakeOrleans.Grains;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeOrleans.Components
{

    public interface IDispatcher
    {
        Task<TResult> Dispatch<TResult>(GrainKey key, Func<IActivation, Task<TResult>> fn);
    }
    


    public class Dispatcher : IDispatcher
    {
        readonly IPlacementDispatcher _innerDispatcher;
        readonly Func<GrainKey, GrainPlacement> _placer;

        public Dispatcher(Func<GrainKey, GrainPlacement> placer, IPlacementDispatcher innerDisp) {
            _placer = placer;
            _innerDispatcher = innerDisp;
        }


        public Task<TResult> Dispatch<TResult>(GrainKey key, Func<IActivation, Task<TResult>> fn) {
            var placement = _placer(key);

            // one too many dispatchers, surely
            //
            //
            
            return _innerDispatcher.Dispatch(placement, fn);
        }

    }




    public static class DispatcherExtensions
    {
        public static Task Dispatch(this IDispatcher disp, GrainKey key, Func<IActivation, Task> fn)
            => disp.Dispatch(key, async a => { await fn(a); return default(VoidType); });
    }



}
