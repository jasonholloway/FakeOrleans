using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockOrleans
{
    public interface IStreamKey {
        string ProviderName { get; }
        string Namespace { get; }
        Guid StreamId { get; }
    }
        
    public struct StreamKey<TItem> : IStreamKey
    {
        public readonly string ProviderName;
        public readonly string Namespace;
        public readonly Guid Id;

        public StreamKey(string provName, string @namespace, Guid id) {
            ProviderName = provName;
            Namespace = @namespace;
            Id = id;
        }
        
        #region IStreamKey

        string IStreamKey.ProviderName {
            get { return ProviderName; }
        }

        string IStreamKey.Namespace {
            get { return Namespace; }
        }

        Guid IStreamKey.StreamId {
            get { return Id; }
        }

        #endregion

    }
}
