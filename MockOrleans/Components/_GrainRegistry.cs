using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans;
using System.Collections.Concurrent;
using System.Threading;
using MockOrleans.Grains;
using System.Collections.ObjectModel;

namespace MockOrleans
{
    


    //public static class GrainPlacementExtensions
    //{
    //    public static Task<GrainHarness> GetActivation(this GrainPlacement placement)
    //        => placement.Registry.GetActivation(placement);       

    //}


    public static class GrainRegistryExtensions {

        //public static GrainPlacement GetPlacement<TGrain>(this GrainRegistry reg, Guid id)
        //    where TGrain : IGrainWithGuidKey
        //    => reg.GetPlacement(reg.GetKey<TGrain>(id));


        //public static Task<GrainHarness> GetActivation(this GrainRegistry reg, GrainKey key)
        //    => reg.GetPlacement(key).GetActivation();


        //public static Task<GrainHarness> GetActivation<TGrain>(this GrainRegistry reg, Guid id)
        //    where TGrain : IGrainWithGuidKey
        //    => reg.GetPlacement<TGrain>(id).GetActivation();




        //public static GrainKey GetKey<TGrain>(this GrainRegistry reg, Guid id)
        //    where TGrain : IGrainWithGuidKey
        //{
        //    var grainType = reg.Fixture.Types.GetConcreteType(typeof(TGrain));
        //    return new GrainKey(grainType, id);
        //}
        
    }




    public class GrainRegistry
    {
        public MockFixture Fixture { get; private set; }
        
        ConcurrentDictionary<GrainPlacement, GrainHarness> _dActivations;


        public GrainRegistry(MockFixture fx) {
            Fixture = fx;
            //Harnesses = new ConcurrentDictionary<GrainKey, GrainHarness>(GrainKeyComparer.Instance);
            _dActivations = new ConcurrentDictionary<GrainPlacement, GrainHarness>();
        }
                
        
        


        public async Task<GrainHarness> GetActivation(GrainPlacement placement) 
        {
            var harness = _dActivations.AddOrUpdate(
                                            placement,
                                            p => new GrainHarness(Fixture, p),
                                            (p, h) => /*h.IsDead*/ true ? new GrainHarness(Fixture, p) : h);
            

            await harness.Activate(); //but if dying? No problem, will sail through - problem is only assailable at point of Request.Perform - ie later

            return harness;
        }








        //public TGrain Inject<TGrain>(GrainKey key, TGrain grain)
        //    where TGrain : class, IGrain 
        //{
        //    var placement = GetPlacement(key);
            
        //    GrainHarness oldActivation = null;
            
        //    _dActivations.AddOrUpdate(placement,
        //                p => new GrainHarness(Fixture, p, grain),
        //                (p, old) => {
        //                    oldActivation = old;
        //                    return new GrainHarness(Fixture, p, grain);
        //                });

        //    if(oldActivation != null) {
        //        Fixture.Requests.Perform(async () => {
        //            await oldActivation.DeactivateWhenIdle();
        //            oldActivation.Dispose();
        //        });
        //    }

        //    var resolvedKey = new ResolvedGrainKey(typeof(TGrain), key.ConcreteType, key.Key);

        //    return (TGrain)(object)Fixture.GetGrainProxy(resolvedKey);
        //}




        public async Task Deactivate(GrainPlacement placement) 
        {
            GrainHarness activation;

            if(_dActivations.TryRemove(placement, out activation)) {
                await activation.Deactivate();
                activation.Dispose();
            }
        }
                

        public async Task DeactivateAll() 
        {
            var captured = Interlocked.Exchange(ref _dActivations, new ConcurrentDictionary<GrainPlacement, GrainHarness>());

            await captured.Values.Select(async a => {
                                            await a.Deactivate();
                                            a.Dispose();
                                        }).WhenAll();
        }

        

    }
}
