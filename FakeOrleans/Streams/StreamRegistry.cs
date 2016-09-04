using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using FakeOrleans.Grains;
using FakeOrleans.Components;

namespace FakeOrleans.Streams
{
    
    public class StreamRegistry
    {
        //GrainRegistry _grains;
        IDispatcher _disp;
        RequestRunner _requests;
        ConcurrentDictionary<StreamKey, Stream> _dStreams;
        ConcurrentDictionary<string, ConcurrentBag<Type>> _dImplicitSubTypes;


        public StreamRegistry(IDispatcher disp, RequestRunner requests, TypeMap typeMap) 
        {
            _disp = disp;
            _requests = requests;
            _dStreams = new ConcurrentDictionary<StreamKey, Stream>();
            _dImplicitSubTypes = new ConcurrentDictionary<string, ConcurrentBag<Type>>();

            typeMap.AddTypeProcessor(DetectImplicitStreamSubs);
        }
        
        public Stream GetStream(StreamKey key)
            => _dStreams.GetOrAdd(key, CreateStream);


        Stream CreateStream(StreamKey key) 
        { 
            var stream = new Stream(key, this, _disp, _requests);

            //implicit subs -----------------------------------------------------------
            ConcurrentBag<Type> implicitSubGrainTypes;
            
            if(_dImplicitSubTypes.TryGetValue(key.Namespace, out implicitSubGrainTypes)) {
                implicitSubGrainTypes.ForEach(grainType => {
                    var grainKey = new GrainKey(grainType, key.Id);
                    stream.Subscribe(grainKey, true);
                });
            }

            return stream;
        }
        
        

        void DetectImplicitStreamSubs(Type grainType) 
        {
            var spec = GrainSpec.GetFor(grainType);

            spec.StreamSubNamespaces.ForEach(ns => {
                var grainTypes = _dImplicitSubTypes.GetOrAdd(ns, _ => new ConcurrentBag<Type>());
                grainTypes.Add(grainType);
            });
        }

    }
        
}
