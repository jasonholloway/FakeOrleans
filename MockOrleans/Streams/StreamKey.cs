using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockOrleans.Streams
{
    [Serializable]
    public struct StreamKey : IStreamIdentity, IEquatable<StreamKey>
    {
        public readonly string ProviderName;
        public readonly string Namespace;
        public readonly Guid Id;

        public StreamKey(string provName, string @namespace, Guid id) {
            ProviderName = provName;
            Namespace = @namespace;
            Id = id;
        }

        #region IStreamIdentity
        
        Guid IStreamIdentity.Guid {
            get { return Id; }
        }

        string IStreamIdentity.Namespace {
            get { return Namespace; }
        }

        #endregion

        #region overrides

        public bool Equals(StreamKey other)
            => other.ProviderName.Equals(ProviderName)
                && other.Namespace.Equals(Namespace)
                && other.Id.Equals(Id);

        public override bool Equals(object obj)
            => obj is StreamKey && Equals((StreamKey)obj);

        public override int GetHashCode()
            => (ProviderName.GetHashCode() << 16)
                ^ Namespace.GetHashCode()
                ^ Id.GetHashCode(); 

        #endregion

    }
}
