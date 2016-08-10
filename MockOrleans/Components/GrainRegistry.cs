using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans;
using System.Collections.Concurrent;
using System.Threading;
using MockOrleans.Grains;

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
    }




    
    public class GrainRegistry
    {
        public MockFixture Fixture { get; private set; }
        //public ConcurrentDictionary<GrainKey, GrainHarness> Harnesses { get; private set; }


        public ConcurrentDictionary<GrainPlacement, GrainHarness> Activations { get; private set; }


        public GrainRegistry(MockFixture fx) {
            Fixture = fx;
            //Harnesses = new ConcurrentDictionary<GrainKey, GrainHarness>(GrainKeyComparer.Instance);
            Activations = new ConcurrentDictionary<GrainPlacement, GrainHarness>();
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
            => Activations.GetOrAdd(placement, p => new GrainHarness(Fixture, p));








        public TGrain Inject<TGrain>(GrainKey key, TGrain grain)
            where TGrain : class, IGrain 
        {
            var placement = GetPlacement(key);
            
            //deactivate current activation if exists
            //...

            Activations.AddOrUpdate(placement,
                        p => new GrainHarness(Fixture, p, grain),
                        (p, _) => new GrainHarness(Fixture, p, grain));

            var resolvedKey = new ResolvedGrainKey(typeof(TGrain), key.ConcreteType, key.Key);

            return (TGrain)(object)Fixture.GetGrainProxy(resolvedKey);
        }




        public async Task Deactivate(GrainKey key) {
            throw new NotImplementedException();
        }


        public async Task DeactivateAll() 
        {
            var activations = Activations.Values.ToArray();
                        
            Activations.Clear();

            foreach(var a in activations) {
                await a.Deactivate();
                a.Dispose();
            }            
        }

        

    }
}
