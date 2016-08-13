using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace MockOrleans.Streams
{

    public class StreamRegistry
    {
        GrainRegistry _grains;
        ConcurrentDictionary<StreamKey, IStreamHub> _dStreams;
        
        public StreamRegistry(GrainRegistry grains) {
            _grains = grains;
            _dStreams = new ConcurrentDictionary<StreamKey, IStreamHub>();
        }


        public StreamHub<T> GetStream<T>(StreamKey key)
            => (StreamHub<T>)_dStreams.GetOrAdd(key, CreateStream<T>);


        IStreamHub CreateStream<T>(StreamKey key)
            => new StreamHub<T>(key, this, _grains);
        
    }
        
}
