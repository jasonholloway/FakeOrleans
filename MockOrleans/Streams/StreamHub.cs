using MockOrleans.Grains;
using Orleans.Streams;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MockOrleans.Streams
{
    
    public interface IStreamHub
    { }
    

    public class StreamHub<T> : IAsyncObserver<T>, IStreamHub
    {
        public readonly StreamKey Key;

        StreamRegistry _streamReg;
        GrainRegistry _grainReg;
        ConcurrentDictionary<Guid, Subscription> _dSubscriptions;

        public StreamHub(StreamKey key, StreamRegistry streamReg, GrainRegistry grainReg) {
            Key = key;
            
            _streamReg = streamReg;
            _grainReg = grainReg;
            _dSubscriptions = new ConcurrentDictionary<Guid, Subscription>();
        }


        #region Publish

        public Task OnNextAsync(T item, StreamSequenceToken token = null)
            => Perform(o => o.OnNextAsync(item, token));

        public Task OnCompletedAsync()
            => Perform(o => o.OnCompletedAsync());

        public Task OnErrorAsync(Exception ex)
            => Perform(o => o.OnErrorAsync(ex));


        Task Perform(Func<IAsyncObserver<T>, Task> fn) {
            var subscriptions = _dSubscriptions.Values.ToArray();
            return subscriptions.Select(s => fn(s)).WhenAll();
        }

        #endregion


        #region Subscribe
        

        public SubscriptionKey Subscribe(GrainKey grainKey) 
        {
            var id = Guid.NewGuid();
            
            var subscription = new Subscription(id, grainKey, this, _grainReg);

            _dSubscriptions[id] = subscription;

            return new SubscriptionKey(Key, id);
        }

        

        [Serializable]
        public struct SubscriptionKey
        {
            public readonly StreamKey StreamKey;
            public readonly Guid SubscriptionId;

            public SubscriptionKey(StreamKey streamKey, Guid subId) {
                StreamKey = streamKey;
                SubscriptionId = subId;
            }
        }                




        public class Subscription : IAsyncObserver<T>
        {
            //public readonly SubscriptionHandle Handle;

            public readonly Guid Id;
            public readonly GrainKey GrainKey;
            public readonly StreamHub<T> Stream;
            public readonly GrainRegistry GrainReg;
            
            
            public Subscription(Guid id, GrainKey grainKey, StreamHub<T> stream, GrainRegistry grainReg) {
                Id = id;
                GrainKey = grainKey;
                Stream = stream;
                GrainReg = grainReg;
            }

            #region IAsyncObserver

            public Task OnNextAsync(T item, StreamSequenceToken token = null) {
                Perform(o => o.OnNextAsync(item, token));
                return Task.CompletedTask;
            }

            public Task OnCompletedAsync() {
                Perform(o => o.OnCompletedAsync());
                return Task.CompletedTask;
            }

            public Task OnErrorAsync(Exception ex) {
                Perform(o => o.OnErrorAsync(ex));
                return Task.CompletedTask;
            }

            #endregion


            void Perform(Func<IAsyncObserver<T>, Task> fn) 
            {
                var activation = GrainReg.GetActivation(GrainKey); //observer usually populated here

                var observer = activation.StreamObservers.Find<T>(Stream.Key);

                if(observer != null) {
                    activation.Requests.Perform(() => fn(observer));
                }
            }
                        
        }
        
        [Serializable]
        public class SubscriptionHandle : StreamSubscriptionHandle<T>, ISerializable
        {
            public readonly StreamKey StreamKey;
            public readonly GrainKey GrainKey;
            readonly Guid _handleId;
            readonly StreamRegistry _streamReg;
            readonly GrainHarness _acivation;

            public SubscriptionHandle(StreamKey streamKey, GrainKey grainKey, StreamRegistry streamReg, GrainHarness activation) 
            {
                StreamKey = streamKey;
                GrainKey = grainKey;
                _handleId = Guid.NewGuid();
                _streamReg = streamReg;
                _acivation = activation;
            }

            protected SubscriptionHandle(SerializationInfo info, StreamingContext context) 
            {
                var ctx = context.Context as GrainContext;
                Require.NotNull(ctx, "Deserializing StreamHub<T>.SubscriptionHandle requires GrainContext!");
                
                _streamReg = ctx.Fixture.Streams;
                _acivation = ctx.Activation;
                _handleId = (Guid)info.GetValue("handleId", typeof(Guid));
                StreamKey = (StreamKey)info.GetValue("streamKey", typeof(StreamKey));
                GrainKey = (GrainKey)info.GetValue("grainKey", typeof(GrainKey));
            }


            public override Guid HandleId {
                get { return _handleId; }
            }
            
            public override IStreamIdentity StreamIdentity {
                get { return StreamKey; }
            }

            public override Task<StreamSubscriptionHandle<T>> ResumeAsync(IAsyncObserver<T> observer, StreamSequenceToken token = null) 
            {
                //but handle should only be deserializable into an activation context
                //almost as if there should be specialised serializers for each activation...

                //where else will there be deserialization? back to the 'client', and into the inspectable store.
                //the inspectable store is just the client under different guise, at it's there for the inspection of the client

                //the client has the same fixture, happily enough. But it has no grainharness.
                //maybe, then, the surrogates should attempt casting to IGrainActivationContext
                //and if they fail, the sought services will be null...

                //on resumption, the handle needs access to the activation.

                //but I can imagine getting a subscription handle in the client, and wanting to unsubscribe it...



                throw new NotImplementedException();
            }

            public override Task UnsubscribeAsync() {
                throw new NotImplementedException();
                //and so, observers should be against a particular handle! not against a particular stream...
                //though they observers of different handles should receive, potentially, from the same stream
                //double indexing needed
            }


            public override bool Equals(StreamSubscriptionHandle<T> other) {
                var otherHandle = other as SubscriptionHandle;
                return otherHandle != null && otherHandle.StreamKey.Equals(StreamKey);
            }

            public override bool Equals(object obj) {
                var other = obj as SubscriptionHandle;
                return other != null && other.HandleId.Equals(HandleId) && other.StreamKey.Equals(StreamKey) && other.GrainKey.Equals(GrainKey);
            }

            public override int GetHashCode() {
                return StreamKey.GetHashCode() ^ GrainKey.GetHashCode() ^ HandleId.GetHashCode() << 24;
            }

            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) {
                info.AddValue("handleId", _handleId);
                info.AddValue("streamKey", StreamKey);
                info.AddValue("grainKey", GrainKey);
            }
        }

        #endregion

    }




}
