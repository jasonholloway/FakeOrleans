using Orleans.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeOrleans.Grains
{
    public struct ConcreteKey
    {
        public readonly Type ConcreteType;
        public readonly Guid Id;

        public ConcreteKey(Type concreteType, Guid id) {
            ConcreteType = concreteType;
            Id = id;
        }
        
    }



    public class Placement //stateless workers can use subtypes of this?
    {
        public readonly AbstractKey GrainKey;
        public readonly Type ConcreteType;
        
        public Placement(AbstractKey key, Type type) {
            GrainKey = key;
            ConcreteType = type;
        }

        public override bool Equals(object obj)
            => (obj as Placement)?.GrainKey.Equals(GrainKey) ?? false; //beware subtypes...

        public override int GetHashCode()
            => GrainKey.GetHashCode();
    }


}
