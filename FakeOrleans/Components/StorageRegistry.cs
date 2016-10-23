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
        ConcurrentDictionary<Placement, StorageCell> _storages;
        
        public StorageRegistry(FakeSerializer serializer) {
            _serializer = serializer;
            _storages = new ConcurrentDictionary<Placement, StorageCell>();
        }
        
        public StorageCell GetStorage(Placement placement)
            => _storages.GetOrAdd(placement, k => new StorageCell(_serializer)); //serializing back into grain-land needs to be done in the activation

        
        public StorageCell this[Placement placement] {
            get {
                return GetStorage(placement);
            }
        }

    }
    
    
}
