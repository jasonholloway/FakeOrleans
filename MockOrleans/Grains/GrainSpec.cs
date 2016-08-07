using Orleans.Concurrency;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockOrleans.Grains
{

    public class GrainSpec
    {
        public bool SerializesRequests { get; private set; }

        private GrainSpec() { }


        static ConcurrentDictionary<Type, GrainSpec> _dSpecs = new ConcurrentDictionary<Type, GrainSpec>();

        public static GrainSpec GetFor(Type grainType)
            => _dSpecs.GetOrAdd(grainType, GenerateSpec);


        static GrainSpec GenerateSpec(Type grainType) {
            var atts = grainType.GetCustomAttributes(true);

            return new GrainSpec() {
                SerializesRequests = !atts.OfType<ReentrantAttribute>().Any()
            };
        }

    }


}
