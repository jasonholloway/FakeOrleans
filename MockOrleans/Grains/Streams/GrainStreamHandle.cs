using MockOrleans.Streams;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MockOrleans.Grains
{


    [Serializable]
    public class GrainStreamHandle<T> : StreamSubscriptionHandle<T>, ISerializable
    {
        public readonly Stream.SubKey SubscriptionKey;

        readonly StreamRegistry _streamReg;
        readonly GrainHarness _activation;

        public GrainStreamHandle(Stream.SubKey subKey, GrainHarness activation, StreamRegistry streamReg) {
            SubscriptionKey = subKey;
            _activation = activation;
            _streamReg = streamReg;
        }

        protected GrainStreamHandle(SerializationInfo info, StreamingContext context) {
            var ctx = context.Context as GrainContext;
            Require.NotNull(ctx, $"Deserializing {nameof(GrainStreamHandle<T>)} requires GrainContext!");

            _streamReg = ctx.Fixture.Streams;
            _activation = ctx.Activation;

            SubscriptionKey = (Stream.SubKey)info.GetValue("subKey", typeof(Stream.SubKey));
        }


        public override Guid HandleId {
            get { return SubscriptionKey.SubscriptionId; }
        }

        public override IStreamIdentity StreamIdentity {
            get { return SubscriptionKey.StreamKey; }
        }

        public override Task<StreamSubscriptionHandle<T>> ResumeAsync(IAsyncObserver<T> observer, StreamSequenceToken token = null) 
        {            
            _activation.StreamReceivers.Register(SubscriptionKey, observer);
            return Task.FromResult((StreamSubscriptionHandle<T>)this);
        }

        public override Task UnsubscribeAsync() 
        {            
            var stream = _streamReg.GetStream(SubscriptionKey.StreamKey);

            stream.Unsubscribe(SubscriptionKey);
            
            _activation.StreamReceivers.Unregister(SubscriptionKey);

            return Task.CompletedTask;
        }


        #region Overrides etc

        public override bool Equals(StreamSubscriptionHandle<T> other) {
            var otherHandle = other as GrainStreamHandle<T>;
            return otherHandle != null && otherHandle.SubscriptionKey.Equals(SubscriptionKey);
        }

        public override bool Equals(object obj) {
            var other = obj as GrainStreamHandle<T>;
            return other != null && other.SubscriptionKey.Equals(SubscriptionKey);
        }

        public override int GetHashCode() {
            return SubscriptionKey.GetHashCode();
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("subKey", SubscriptionKey);
        }

        #endregion

    }



}
