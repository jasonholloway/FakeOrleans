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
        IPlacementDispatcher _disp;
        ExceptionSink _exceptions;
        ConcurrentDictionary<StreamKey, Stream> _dStreams;
        ConcurrentDictionary<string, ConcurrentBag<Type>> _dImplicitSubTypes;


        public StreamRegistry(IPlacementDispatcher disp, ExceptionSink exceptions, TypeMap typeMap) 
        {
            _disp = disp;
            _exceptions = exceptions;
            _dStreams = new ConcurrentDictionary<StreamKey, Stream>();
            _dImplicitSubTypes = new ConcurrentDictionary<string, ConcurrentBag<Type>>();

            typeMap.AddTypeProcessor(DetectImplicitStreamSubs);
        }
        
        public Stream GetStream(StreamKey key)
            => _dStreams.GetOrAdd(key, CreateStream);


        Stream CreateStream(StreamKey key) 
        { 
            var stream = new Stream(key, this, _disp, _exceptions);

            //implicit subs -----------------------------------------------------------
            ConcurrentBag<Type> implicitSubGrainTypes;
            
            if(_dImplicitSubTypes.TryGetValue(key.Namespace, out implicitSubGrainTypes)) {
                implicitSubGrainTypes.ForEach(grainType => {
                    throw new NotImplementedException(); //so - concretetypes subscribe to streams, do they???
                    //var concreteKey = new ConcreteKey(grainType, key.Id);   //!!!!  ConcreteKey should be enough to make a special placement                    
                    //stream.Subscribe(new Placement(concreteKey), true);     //this should be done via service, etc etc etc
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
