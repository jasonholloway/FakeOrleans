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
    
    public class MockSilo
    {
        public MockFixture Fixture { get; private set; }
        public ConcurrentDictionary<GrainKey, GrainHarness> Harnesses { get; private set; }


        public MockSilo(MockFixture fx) {
            Fixture = fx;
            Harnesses = new ConcurrentDictionary<GrainKey, GrainHarness>(ConcreteGrainKeyComparer.Instance);
        }



        public IGrainEndpoint GetGrainEndpoint(GrainKey key) {
            return GetHarness(key); //harness doubles up as endpoint
        }

        

        GrainHarness GetHarness(GrainKey key) {
            return Harnesses.GetOrAdd(key, k => new GrainHarness(Fixture, k));
        }


        


        public async Task DeactivateGrains() 
        {
            var harnesses = Harnesses.Values.ToArray();
                        
            Harnesses.Clear();

            foreach(var h in harnesses) {
                await h.Deactivate();
                h.Dispose();
            }            
        }

        

    }
}
