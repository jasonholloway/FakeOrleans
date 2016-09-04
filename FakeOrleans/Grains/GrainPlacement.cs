using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeOrleans.Grains
{

    public class GrainPlacement //stateless workers can use subtypes of this?
    {
        public readonly GrainKey Key;
        
        public GrainPlacement(GrainKey key) {
            Key = key;
        }

        public override bool Equals(object obj)
            => (obj as GrainPlacement)?.Key.Equals(Key) ?? false; //beware subtypes...

        public override int GetHashCode()
            => Key.GetHashCode();
    }


}
