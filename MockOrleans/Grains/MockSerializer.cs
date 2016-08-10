using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace MockOrleans.Grains
{

    public static class MockSerializerExtensions
    {
        public static T Clone<T>(this MockSerializer @this, T source)
            => (T)@this.Deserialize(@this.Serialize(source));
    }



    public class MockSerializer
    {
        MockFixture _fx;
        ISurrogateSelector _surrogateSelector;

        public MockSerializer(MockFixture fx) {
            _fx = fx;
            _surrogateSelector = new GrainAwareSurrogateSelector();            
        }


        public byte[] Serialize(object inp) 
        {
            var formatter = new BinaryFormatter(
                                    _surrogateSelector, 
                                    new StreamingContext(StreamingContextStates.All, _fx));

            using(var str = new MemoryStream()) {
                formatter.Serialize(str, inp);
                return str.ToArray();
            }

        }


        public object Deserialize(byte[] bytes) 
        {
            var formatter = new BinaryFormatter(
                                    _surrogateSelector, 
                                    new StreamingContext(StreamingContextStates.All, _fx));

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

                //if(type.IsAssignableTo<GrainActivation>()) {
                //    return new GrainSurrogate();
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
                var fx = (MockFixture)context.Context;

                var key = (ResolvedGrainKey)info.GetValue("key", typeof(ResolvedGrainKey));

                return fx.GetGrainProxy(key);
            }
        }


        class GrainSurrogate : ISerializationSurrogate
        {
            public void GetObjectData(object obj, SerializationInfo info, StreamingContext context) 
            {
                throw new NotImplementedException();
            }

            public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector) 
            {
                //proxify
                //the serialize
                throw new NotImplementedException();
            }
        }

    }
}
