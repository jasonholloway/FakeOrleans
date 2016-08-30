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

    public static class GrainSetExtensions
    {
        public static Task DeactivateAll(this IGrainSet @this) {
            var acts = @this.GetActivations();

            return acts.Select(a => a.Deactivate())
                        .WhenAll();
        }
    }


    public interface IGrainSet
    {
        IActivation[] GetActivations();
    }


    public interface IPlacementDispatcher
    {
        Task<TResult> Dispatch<TResult>(GrainPlacement placement, Func<IActivation, Task<TResult>> fn);
    }


    public class ActivationHub : IPlacementDispatcher, IGrainSet
    {
        readonly Func<GrainPlacement, IActivationSite> _siteFac;
        readonly ConcurrentDictionary<GrainPlacement, IActivationSite> _dSites;

        public ActivationHub(Func<GrainPlacement, IActivationSite> siteFac) {
            _siteFac = siteFac;
            _dSites = new ConcurrentDictionary<GrainPlacement, IActivationSite>();
        }


        public Task<TResult> Dispatch<TResult>(GrainPlacement placement, Func<IActivation, Task<TResult>> fn) {
            var site = _dSites.GetOrAdd(placement, p => _siteFac(p));

            return site.Dispatch(fn, RequestMode.Unspecified);
        }


        public IActivation[] GetActivations() 
            => _dSites.Values.ToArray()
                    .Select(s => s.Activation)
                    .Where(a => a != null)
                    .ToArray();


    }



}
