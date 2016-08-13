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
        ConcurrentDictionary<StreamKey, Stream> _dStreams;
        
        public StreamRegistry(GrainRegistry grains) {
            _grains = grains;
            _dStreams = new ConcurrentDictionary<StreamKey, Stream>();
        }
        
        public Stream GetStream(StreamKey key)
            => _dStreams.GetOrAdd(key, CreateStream);
        
        Stream CreateStream(StreamKey key)
            => new Stream(key, this, _grains);
        
    }
        
}
