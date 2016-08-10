using MockOrleans;
using MockOrleans.Grains;
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


namespace MockOrleans
{
    
    

    public class GrainStorage
    {
        GrainKey _placement;
        MockSerializer _serializer;

        public bool IsEmpty { get; private set; } = true;
        public object State { get; private set; }
        public string ETag { get; private set; }

        object _sync = new object();


        public GrainStorage(GrainKey placement, MockSerializer serializer) {
            _placement = placement;
            _serializer = serializer;
        }


        public void Update(object state, string etag = null) {
            lock(_sync) {
                State = state;
                ETag = etag;
                IsEmpty = false;
            }
        }


        internal void Clear() {
            lock(_sync) {
                State = null;
                ETag = null;
                IsEmpty = true;
            }
        }

        internal void Write(IGrainState grainState) {
            lock(_sync) {
                ETag = grainState.ETag;
                State = _serializer.Clone(grainState.State);
                IsEmpty = false;
            }
        }

        internal void Read(IGrainState grainState) {
            lock(_sync) {
                if(!IsEmpty) {
                    grainState.ETag = ETag;
                    grainState.State = _serializer.Clone(State);
                }
            }
        }

    }



    public class StorageRegistry
    {
        MockSerializer _serializer;
        ConcurrentDictionary<GrainKey, GrainStorage> _storages;
        
        public StorageRegistry(MockSerializer serializer) {
            _serializer = serializer;
            _storages = new ConcurrentDictionary<GrainKey, GrainStorage>(GrainKeyComparer.Instance);
        }
        
        public GrainStorage GetStorage(GrainKey key)
            => _storages.GetOrAdd(key, k => new GrainStorage(k, _serializer));

        
        public GrainStorage this[GrainKey key] {
            get {
                return GetStorage(key);
            }
        }

    }
    
    
}
