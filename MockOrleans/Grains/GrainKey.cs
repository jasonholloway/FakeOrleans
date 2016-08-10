using Orleans.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MockOrleans
{
    [Serializable]
    public class GrainKey : IGrainIdentity, IComparable<GrainKey>, IEquatable<GrainKey>, ISerializable
    {
        public Type ConcreteType { get; private set; }
        public Guid Key { get; private set; }
        
        public GrainKey(Type concreteType, Guid key)
        {
            ConcreteType = concreteType;
            Key = key;
        }

        public override string ToString() => $"{ConcreteType.Name}/{Key}";

        #region IGrainIdentity

        Guid IGrainIdentity.PrimaryKey
        {
            get {
                return Key;
            }
        }

        long IGrainIdentity.PrimaryKeyLong
        {
            get {
                throw new NotImplementedException();
            }
        }

        string IGrainIdentity.PrimaryKeyString
        {
            get {
                throw new NotImplementedException();
            }
        }

        string IGrainIdentity.IdentityString
        {
            get {
                throw new NotImplementedException();
            }
        }
        
        long IGrainIdentity.GetPrimaryKeyLong(out string keyExt)
        {
            throw new NotImplementedException();
        }

        Guid IGrainIdentity.GetPrimaryKey(out string keyExt)
        {
            throw new NotImplementedException();
        }

        #endregion
        
        #region Comparison etc

        int IComparable<GrainKey>.CompareTo(GrainKey other)
        {
            var typeComparison = Comparer<Type>.Default.Compare(ConcreteType, other.ConcreteType);

            return typeComparison != 0
                    ? typeComparison
                    : Comparer<Guid>.Default.Compare(Key, other.Key);
        }


        bool IEquatable<GrainKey>.Equals(GrainKey other) {
            return ConcreteType == other.ConcreteType
                    && Key == other.Key;
        }

        public override bool Equals(object obj) {
            return obj is GrainKey
                    && ((IEquatable<GrainKey>)this).Equals((GrainKey)obj);
        }

        public override int GetHashCode() {
            return (ConcreteType.GetHashCode() << 8)
                    ^ Key.GetHashCode();
        }

        #endregion

        #region Serialization

        protected GrainKey(SerializationInfo info, StreamingContext ctx) {
            Key = (Guid)info.GetValue("Key", typeof(Guid));
            ConcreteType = (Type)info.GetValue("ConcreteType", typeof(Type));
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("ConcreteType", ConcreteType);
            info.AddValue("Key", Key);
        }

        #endregion
        
    }


    public class GrainKeyComparer : IEqualityComparer<GrainKey>
    {
        public readonly static GrainKeyComparer Instance = new GrainKeyComparer();

        public bool Equals(GrainKey x, GrainKey y) {
            return x.ConcreteType == y.ConcreteType
                    && x.Key == y.Key;
        }

        public int GetHashCode(GrainKey obj) {
            return (obj.ConcreteType.GetHashCode() << 8)
                    ^ obj.Key.GetHashCode();
        }
    }






    [Serializable]
    public class ResolvedGrainKey : GrainKey, IComparable<ResolvedGrainKey>, IEquatable<ResolvedGrainKey>, ISerializable
    {
        public Type AbstractType { get; private set; }

        public ResolvedGrainKey(Type abstractType, Type concreteType, Guid key) 
            : base(concreteType, key)
        {
            AbstractType = abstractType;
        }
        
        #region Comparison etc

        int IComparable<ResolvedGrainKey>.CompareTo(ResolvedGrainKey other)
        {
            var typeComparison = Comparer<Type>.Default.Compare(ConcreteType, other.ConcreteType);

            return typeComparison != 0
                    ? typeComparison
                    : Comparer<Guid>.Default.Compare(Key, other.Key);
        }


        bool IEquatable<ResolvedGrainKey>.Equals(ResolvedGrainKey other) {
            return
                AbstractType == other.AbstractType
                && ConcreteType == other.ConcreteType
                && Key == other.Key;
        }

        public override bool Equals(object obj) {
            return obj is ResolvedGrainKey
                    && ((IEquatable<ResolvedGrainKey>)this).Equals((ResolvedGrainKey)obj);
        }

        public override int GetHashCode() {
            return (AbstractType != null ? (AbstractType.GetHashCode() << 16) : 0)
                    ^ (ConcreteType.GetHashCode() << 8) 
                    ^ Key.GetHashCode();
        }

        #endregion

        #region Serialization

        protected ResolvedGrainKey(SerializationInfo info, StreamingContext ctx) : base(info, ctx) {
            AbstractType = (Type)info.GetValue("AbstractType", typeof(Type));
        }
        
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("AbstractType", AbstractType);
            info.AddValue("ConcreteType", ConcreteType);
            info.AddValue("Key", Key);
        }

        #endregion

    }



}
