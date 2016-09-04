using FakeOrleans;
using FakeOrleans.Grains;
using Orleans;
using Orleans.Core;
using Orleans.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace FakeOrleans
{

    public class StorageRegistry
    {
        FakeSerializer _serializer;
        ConcurrentDictionary<GrainKey, StorageCell> _storages;
        
        public StorageRegistry(FakeSerializer serializer) {
            _serializer = serializer;
            _storages = new ConcurrentDictionary<GrainKey, StorageCell>(GrainKeyComparer.Instance);
        }
        
        public StorageCell GetStorage(GrainKey key)
            => _storages.GetOrAdd(key, k => new StorageCell(_serializer)); //serializing back into grain-land needs to be done in the activation

        
        public StorageCell this[GrainKey key] {
            get {
                return GetStorage(key);
            }
        }

    }
    
    
}
