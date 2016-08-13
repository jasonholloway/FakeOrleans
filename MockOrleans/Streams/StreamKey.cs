using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockOrleans.Streams
{
    [Serializable]
    public struct StreamKey : IStreamIdentity
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

    }
}
