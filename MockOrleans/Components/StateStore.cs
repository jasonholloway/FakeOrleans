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

    public interface IStateStore
    {
        Task ReadFrom(GrainKey key, IGrainState state);
        Task WriteTo(GrainKey key, IGrainState state);
        Task Clear(GrainKey key);
    }

    

    public class GrainStorage
    {
        GrainKey _key;
        MockSerializer _serializer;

        public bool IsEmpty { get; private set; } = true;
        public object State { get; private set; }
        public string ETag { get; private set; }

        object _sync = new object();


        public GrainStorage(GrainKey key, MockSerializer serializer) {
            _key = key;
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
                State = grainState.State;
                IsEmpty = false;
            }
        }

        internal void Read(IGrainState grainState) {
            lock(_sync) {
                if(!IsEmpty) {
                    grainState.ETag = ETag;
                    grainState.State = State;
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





    public class MockStore : IStateStore
    {
        MockSerializer _serializer;
        ConcurrentDictionary<GrainKey, byte[]> _dCommits = new ConcurrentDictionary<GrainKey, byte[]>(GrainKeyComparer.Instance);


        public MockStore(MockSerializer serializer) {
            _serializer = serializer;
        }



        public virtual Task Clear(GrainKey key) 
        {
            byte[] _;

            _dCommits.TryRemove(key, out _);

            return TaskDone.Done;
        }

        public virtual Task ReadFrom(GrainKey key, IGrainState state) 
        {
            byte[] commit = null;

            if(_dCommits.TryGetValue(key, out commit)) {
                state.State = _serializer.Deserialize(commit);
            }

            return TaskDone.Done;
        }

        public virtual Task WriteTo(GrainKey key, IGrainState state) 
        {
            _dCommits.AddOrUpdate(key, 
                        (_) => _serializer.Serialize(state.State), 
                        (_, __) => _serializer.Serialize(state.State));

            return TaskDone.Done;
        }





    }
}
