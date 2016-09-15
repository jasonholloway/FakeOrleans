using FakeOrleans.Grains;
using FakeOrleans.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace FakeOrleans
{

    
    //public interface ISerializationContext
    //{ }


    //public class FixtureContext : ISerializationContext
    //{
    //    public readonly Fixture Fixture;

    //    public FixtureContext(Fixture fx) {
    //        Fixture = fx;
    //    }
    //}


    //public class GrainContext : FixtureContext
    //{
    //    public readonly IActivation Activation;

    //    public GrainContext(Fixture fx, IActivation activation) 
    //        : base(fx) {
    //        Activation = activation;
    //    }
    //}





    public static class FakeSerializerExtensions
    {
        public static T Clone<T>(this FakeSerializer @this, T source)
            => (T)@this.Deserialize(@this.Serialize(source));
    }



    public class FakeSerializer
    {
        object _ctx;        
        ISurrogateSelector _surrogateSelector;

        public FakeSerializer(object ctx) {
            _ctx = ctx;
            _surrogateSelector = new GrainAwareSurrogateSelector();
        }


        public byte[] Serialize(object inp) 
        {
            if(inp == null) return null;

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
            if(bytes == null) return null;

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
                var ctx = context.Context as FixtureCtx;
                
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
