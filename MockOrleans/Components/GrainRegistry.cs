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


    public class GrainPlacement //stateless workers can use subtypes of this?
    {
        public readonly GrainKey Key;
        internal readonly GrainRegistry Registry;
        
        public GrainPlacement(GrainKey key, GrainRegistry registry) {
            Key = key;
            Registry = registry;
        }
        
        public override bool Equals(object obj)
            => (obj as GrainPlacement)?.Key.Equals(Key) ?? false; //beware subtypes...

        public override int GetHashCode()
            => Key.GetHashCode();
    }



    public static class GrainPlacementExtensions
    {
        public static GrainHarness GetActivation(this GrainPlacement placement)
            => placement.Registry.GetActivation(placement);


        public static GrainHarness GetActivation(this GrainRegistry reg, GrainKey key)
            => reg.GetPlacement(key).GetActivation();

    }




    
    public class GrainRegistry
    {
        public MockFixture Fixture { get; private set; }
        //public ConcurrentDictionary<GrainKey, GrainHarness> Harnesses { get; private set; }


        //public IReadOnlyDictionary<GrainPlacement, GrainHarness> Activations {
        //    get { return new ReadOnlyDictionary<GrainPlacement, GrainHarness>(_dActivations); }
        //}



        ConcurrentDictionary<GrainPlacement, GrainHarness> _dActivations;


        public GrainRegistry(MockFixture fx) {
            Fixture = fx;
            //Harnesses = new ConcurrentDictionary<GrainKey, GrainHarness>(GrainKeyComparer.Instance);
            _dActivations = new ConcurrentDictionary<GrainPlacement, GrainHarness>();
        }
                

        internal IGrainEndpoint GetGrainEndpoint(GrainKey key) {
            var placement = GetPlacement(key);
            return GetActivation(placement);
        }

        

        //GrainHarness GetHarness(GrainKey key) {
        //    return Harnesses.GetOrAdd(key, k => new GrainHarness(Fixture, k));
        //}



        public GrainPlacement GetPlacement(GrainKey key) 
        {
            var placement = new GrainPlacement(key, this);

            //stateless workers etc

            return placement;
        }


        public GrainPlacement this[GrainKey key] {
            get { return GetPlacement(key); }
        }




        public GrainHarness GetActivation(GrainPlacement placement)
            => _dActivations.GetOrAdd(placement, p => new GrainHarness(Fixture, p));








        public TGrain Inject<TGrain>(GrainKey key, TGrain grain)
            where TGrain : class, IGrain 
        {
            var placement = GetPlacement(key);
            
            GrainHarness oldActivation = null;
            
            _dActivations.AddOrUpdate(placement,
                        p => new GrainHarness(Fixture, p, grain),
                        (p, old) => {
                            oldActivation = old;
                            return new GrainHarness(Fixture, p, grain);
                        });

            if(oldActivation != null) {
                Fixture.Requests.Perform(async () => {
                    await oldActivation.Deactivate();
                    oldActivation.Dispose();
                });
            }

            var resolvedKey = new ResolvedGrainKey(typeof(TGrain), key.ConcreteType, key.Key);

            return (TGrain)(object)Fixture.GetGrainProxy(resolvedKey);
        }




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
