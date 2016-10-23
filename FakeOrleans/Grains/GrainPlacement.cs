using Orleans.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeOrleans.Grains
{
    public struct ConcreteKey : IGrainIdentity
    {
        public readonly Type ConcreteType;
        public readonly Guid Id;

        public ConcreteKey(Type concreteType, Guid id) {
            ConcreteType = concreteType;
            Id = id;
        }

        #region IGrainIdentity

        string IGrainIdentity.IdentityString {
            get {
                throw new NotImplementedException();
            }
        }

        Guid IGrainIdentity.PrimaryKey {
            get {
                throw new NotImplementedException();
            }
        }

        long IGrainIdentity.PrimaryKeyLong {
            get {
                throw new NotImplementedException();
            }
        }

        string IGrainIdentity.PrimaryKeyString {
            get {
                throw new NotImplementedException();
            }
        }

        Guid IGrainIdentity.GetPrimaryKey(out string keyExt) {
            throw new NotImplementedException();
        }

        long IGrainIdentity.GetPrimaryKeyLong(out string keyExt) {
            throw new NotImplementedException();
        }

        #endregion
    }



    public class Placement //stateless workers can use subtypes of this?
    {
        public readonly ConcreteKey ConcreteKey;
        
        public Placement(ConcreteKey concreteKey) {
            ConcreteKey = concreteKey;
        }

        public override bool Equals(object obj)
            => (obj as Placement)?.ConcreteKey.Equals(ConcreteKey) ?? false; //beware subtypes...

        public override int GetHashCode()
            => ConcreteKey.GetHashCode();
    }


}
