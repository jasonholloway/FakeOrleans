using MockOrleans.Grains;
using MockOrleans.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace MockOrleans
{

    
    public interface ISerializationContext
    { }


    public class FixtureContext : ISerializationContext
    {
        public MockFixture Fixture { get; set; }
    }


    public class GrainContext : FixtureContext
    {
        public GrainHarness Activation { get; set; }
    }





    public static class MockSerializerExtensions
    {
        public static T Clone<T>(this MockSerializer @this, T source)
            => (T)@this.Deserialize(@this.Serialize(source));
    }



    public class MockSerializer
    {
        object _ctx;        
        ISurrogateSelector _surrogateSelector;

        public MockSerializer(object ctx) {
            _ctx = ctx;
            _surrogateSelector = new GrainAwareSurrogateSelector();            
        }


        public byte[] Serialize(object inp) 
        {
            var formatter = new BinaryFormatter(
                                    _surrogateSelector, 
                                    new StreamingContext(StreamingContextStates.All, _ctx));

            using(var str = new MemoryStream()) {
                formatter.Serialize(str, inp);
                return str.ToArray();
            }

        }


        public object Deserialize(byte[] bytes) 
        {
            var formatter = new BinaryFormatter(
                                    _surrogateSelector, 
                                    new StreamingContext(StreamingContextStates.All, _ctx));

            using(var str = new MemoryStream(bytes)) {
                return formatter.Deserialize(str);
            }
        }



        class GrainProxyDummy { }


        class GrainAwareSurrogateSelector : ISurrogateSelector
        {
            ISurrogateSelector _next = null;
                        

            public void ChainSelector(ISurrogateSelector selector) {
                _next = selector;
            }

            public ISurrogateSelector GetNextSelector() {
                return _next;
            }

            public ISerializationSurrogate GetSurrogate(Type type, StreamingContext context, out ISurrogateSelector selector) 
            {
                selector = _next;
                
                if(type.IsAssignableTo<GrainProxy>() || type.Equals(typeof(GrainProxyDummy))) {
                    return new GrainProxySurrogate();
                }
                
                //if(type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(StreamHub<>.SubscriptionHandle))) {
                //    return new StreamSubscriptionHandleSurrogate();
                //}

                return null;
            }
        }



        



        class GrainProxySurrogate : ISerializationSurrogate
        {
            public void GetObjectData(object obj, SerializationInfo info, StreamingContext context) 
            {
                info.SetType(typeof(GrainProxyDummy));

                var proxy = (GrainProxy)obj;
                info.AddValue("key", proxy.Key, typeof(ResolvedGrainKey));
            }

            public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector) 
            {
                var ctx = context.Context as FixtureContext;
                
                if(ctx == null) {
                    throw new SerializationException("Can't deserialize GrainProxy without FixtureContext!");
                }
                
                var key = (ResolvedGrainKey)info.GetValue("key", typeof(ResolvedGrainKey));

                return ctx.Fixture.GetGrainProxy(key);
            }
        }



        //class StreamSubscriptionHandleSurrogate : ISerializationSurrogate
        //{
        //    public void GetObjectData(object obj, SerializationInfo info, StreamingContext context) {
        //        //info.SetType(typeof(GrainProxyDummy));

        //        var proxy = (GrainProxy)obj;
        //        info.AddValue("key", proxy.Key, typeof(ResolvedGrainKey));
        //    }

        //    public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector) {
        //        var fx = (MockFixture)context.Context;
                
        //        var streamKey = (StreamKey)info.GetValue("streamKey", typeof(StreamKey));
        //        var grainKey = (GrainKey)info.GetValue("grainKey", typeof(GrainKey));
                
        //        return new StreamHub<int>.SubscriptionHandle(streamKey, grainKey, fx.Streams);                
        //    }
        //}
        
        
    }
}
