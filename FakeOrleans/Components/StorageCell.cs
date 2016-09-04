using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeOrleans.Grains
{

    public class StorageCell
    {
        FakeSerializer _serializer;

        public bool IsEmpty { get; private set; } = true;
        public byte[] Data { get; private set; }
        public string ETag { get; private set; }

        object _sync = new object();


        public StorageCell(FakeSerializer serializer) {
            _serializer = serializer;
        }


        public object State {
            get { return IsEmpty ? null : _serializer.Deserialize(Data); }
        }
        
        public void Update(object state, string etag = null, FakeSerializer serializer = null) {
            lock(_sync) {
                Data = (serializer ?? _serializer).Serialize(state);
                ETag = etag;
                IsEmpty = false;
            }
        }


        internal void Clear() {
            lock(_sync) {
                Data = null;
                ETag = null;
                IsEmpty = true;
            }
        }

        internal void Write(IGrainState grainState, FakeSerializer serializer = null) {
            lock(_sync) {
                ETag = grainState.ETag;
                Data = (serializer ?? _serializer).Serialize(grainState.State);
                IsEmpty = false;
            }
        }

        internal void Read(IGrainState grainState, FakeSerializer serializer = null) {
            lock(_sync) {
                if(!IsEmpty) {
                    grainState.ETag = ETag;
                    grainState.State = (serializer ?? _serializer).Deserialize(Data);
                }
            }
        }

    }





}
