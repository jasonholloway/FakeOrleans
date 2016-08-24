using MockOrleans.Grains;
using Orleans;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockOrleans.Components
{

    public interface IPlacementDispatcher
    {
        Task<TResult> Dispatch<TResult>(GrainPlacement placement, Func<IActivation, Task<TResult>> fn);
    }


    public class PlacementDispatcher : IPlacementDispatcher
    {
        readonly Func<GrainPlacement, IActivationSite> _siteFac;
        readonly ConcurrentDictionary<GrainPlacement, IActivationSite> _dSites;

        public PlacementDispatcher(Func<GrainPlacement, IActivationSite> siteFac) {
            _siteFac = siteFac;
            _dSites = new ConcurrentDictionary<GrainPlacement, IActivationSite>();
        }


        public Task<TResult> Dispatch<TResult>(GrainPlacement placement, Func<IActivation, Task<TResult>> fn) {
            var site = _dSites.GetOrAdd(placement, p => _siteFac(p));

            return site.Dispatch(fn, RequestMode.Unspecified);
        }

        public Task<TResult> Dispatch<TResult>(GrainKey key, Func<Grain, Task<TResult>> fn) {
            throw new NotImplementedException();
        }

    }



}
