using FakeOrleans.Grains;
using Orleans;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeOrleans.Components
{

    public static class GrainSetExtensions
    {
        public static Task DeactivateAll(this IActivationSet @this) {
            var acts = @this.GetActivations();
            return acts.Select(a => a.Dispatcher.Deactivate()).WhenAll();
        }
    }


    public interface IActivationSet
    {
        IActivation[] GetActivations();
    }


    public interface IPlacementDispatcher
    {
        Task<TResult> Dispatch<TResult>(Placement placement, Func<IGrainContext, Task<TResult>> fn);
    }


    public class ActivationHub : IPlacementDispatcher //, IActivationSet
    {
        readonly Func<Placement, IActivationSite> _siteFac;
        readonly ConcurrentDictionary<Placement, IActivationSite> _dSites;

        public ActivationHub(Func<Placement, IActivationSite> siteFac) {
            _siteFac = siteFac;
            _dSites = new ConcurrentDictionary<Placement, IActivationSite>();
        }


        public Task<TResult> Dispatch<TResult>(Placement placement, Func<IGrainContext, Task<TResult>> fn) {
            var site = _dSites.GetOrAdd(placement, p => _siteFac(p));

            return site.Dispatch(fn, RequestMode.Unspecified);
        }


        //public IActivation[] GetActivations() 
        //    => _dSites.Values.ToArray()
        //            .Select(s => s.Activation)
        //            .Where(a => a != null)
        //            .ToArray();


    }



}
