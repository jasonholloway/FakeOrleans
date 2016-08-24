using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockOrleans.Grains
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


}
