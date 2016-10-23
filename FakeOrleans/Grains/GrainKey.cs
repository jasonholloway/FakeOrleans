using Orleans.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace FakeOrleans
{
    [Serializable]
    public class AbstractKey : IGrainIdentity, IComparable<AbstractKey>, IEquatable<AbstractKey>, ISerializable
    {
        public Type AbstractType { get; private set; }
        public Guid Id { get; private set; }
        
        public AbstractKey(Type abstractType, Guid id)
        {
            AbstractType = abstractType;
            Id = id;
        }

        public override string ToString() => $"{AbstractType.Name}/{Id}";

        #region IGrainIdentity

        Guid IGrainIdentity.PrimaryKey
        {
            get {
                return Id;
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

        int IComparable<AbstractKey>.CompareTo(AbstractKey other)
        {
            var typeComparison = Comparer<Type>.Default.Compare(AbstractType, other.AbstractType);

            return typeComparison != 0
                    ? typeComparison
                    : Comparer<Guid>.Default.Compare(Id, other.Id);
        }


        bool IEquatable<AbstractKey>.Equals(AbstractKey other) {
            return AbstractType == other.AbstractType
                    && Id == other.Id;
        }

        public override bool Equals(object obj) {
            return obj is AbstractKey
                    && ((IEquatable<AbstractKey>)this).Equals((AbstractKey)obj);
        }

        public override int GetHashCode() {
            return (AbstractType.GetHashCode() << 8)
                    ^ Id.GetHashCode();
        }

        #endregion

        #region Serialization

        protected AbstractKey(SerializationInfo info, StreamingContext ctx) {
            Id = (Guid)info.GetValue("Key", typeof(Guid));
            AbstractType = (Type)info.GetValue("AbstractType", typeof(Type));
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("AbstractType", AbstractType);
            info.AddValue("Key", Id);
        }

        #endregion
        
    }


    public class AbstractKeyComparer : IEqualityComparer<AbstractKey>
    {
        public readonly static AbstractKeyComparer Instance = new AbstractKeyComparer();

        public bool Equals(AbstractKey x, AbstractKey y) {
            return x.AbstractType == y.AbstractType
                    && x.Id == y.Id;
        }

        public int GetHashCode(AbstractKey obj) {
            return (obj.AbstractType.GetHashCode() << 8)
                    ^ obj.Id.GetHashCode();
        }
    }






    [Serializable]
    public class ResolvedKey : AbstractKey, IComparable<ResolvedKey>, IEquatable<ResolvedKey>, ISerializable
    {
        public Type ConcreteType { get; private set; }

        public ResolvedKey(Type abstractType, Type concreteType, Guid key) 
            : base(concreteType, key)
        {
            ConcreteType = abstractType;
        }

        public ResolvedKey(AbstractKey grainKey, Type concreteType) 
            : this(grainKey.AbstractType, concreteType, grainKey.Id) { }


        #region Comparison etc

        int IComparable<ResolvedKey>.CompareTo(ResolvedKey other)
        {
            var typeComparison = Comparer<Type>.Default.Compare(ConcreteType, other.ConcreteType);

            return typeComparison != 0
                    ? typeComparison
                    : Comparer<Guid>.Default.Compare(Id, other.Id);
        }


        bool IEquatable<ResolvedKey>.Equals(ResolvedKey other) {
            return
                ConcreteType == other.ConcreteType
                && ConcreteType == other.ConcreteType
                && Id == other.Id;
        }

        public override bool Equals(object obj) {
            return obj is ResolvedKey
                    && ((IEquatable<ResolvedKey>)this).Equals((ResolvedKey)obj);
        }

        public override int GetHashCode() {
            return (ConcreteType != null ? (ConcreteType.GetHashCode() << 16) : 0)
                    ^ (base.AbstractType.GetHashCode() << 8) 
                    ^ Id.GetHashCode();
        }

        #endregion

        #region Serialization

        protected ResolvedKey(SerializationInfo info, StreamingContext ctx) : base(info, ctx) {
            ConcreteType = (Type)info.GetValue("ConcreteType", typeof(Type));
        }
        
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("ConcreteType", ConcreteType);
            info.AddValue("AbstractType", base.AbstractType);
            info.AddValue("Key", Id);
        }

        #endregion

    }



}
