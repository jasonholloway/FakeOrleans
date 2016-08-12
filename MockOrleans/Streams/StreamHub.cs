using Orleans.Streams;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockOrleans.Streams
{




    public class StreamClient
    {

        



    }










    public interface IStreamHub
    {

    }



    public class StreamHub<T> : IAsyncObserver<T>, IStreamHub
    {
        public readonly StreamKey Key;

        StreamRegistry _streamReg;
        GrainRegistry _grainReg;
        ConcurrentDictionary<StreamSubscriptionHandle<T>, Subscription> _dSubscriptions;

        public StreamHub(StreamKey key, StreamRegistry streamReg, GrainRegistry grainReg) {
            Key = key;
            _grainReg = grainReg;
            _dSubscriptions = new ConcurrentDictionary<StreamSubscriptionHandle<T>, Subscription>();
        }


        #region IAsyncObserver

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


        public Subscription Subscribe(GrainKey grainKey) 
        {
            var handle = new SubscriptionHandle(Key, grainKey, _streamReg);
            
            var subscription = new Subscription(handle, grainKey);
                        
            _dSubscriptions.AddOrUpdate(handle, subscription, (_, __) => { throw new InvalidOperationException("Subscription already exists!"); });

            return subscription;
        }

        
        public class Subscriber : IAsyncObservable<T>
        {
            readonly StreamHub<T> _stream;
            readonly GrainKey _grainKey;
            
            public Subscriber(StreamHub<T> stream, GrainKey grainKey) {
                _stream = stream;
                _grainKey = grainKey;
            }

            public Task<SubscriptionHandle> SubscribeAsync(IAsyncObserver<T> observer) 
            { 
                var subscription = _stream.Subscribe(_grainKey); //and also need to pass the observer in here somehow
                
                subscription.SetObserver(observer); //this shouldn't be done directly to stream, but to the local subscription...

                return Task.FromResult(subscription.Handle);
            }

            public Task<StreamSubscriptionHandle<T>> SubscribeAsync(IAsyncObserver<T> observer, StreamSequenceToken token, StreamFilterPredicate filterFunc = null, object filterData = null) {
                throw new NotImplementedException();
            }            
        }

        
        class Subscription : IAsyncObserver<T>
        {
            public readonly SubscriptionHandle Handle;
            public readonly GrainRegistry GrainReg;

            IAsyncObserver<T> _observer = null;

            public Subscription(SubscriptionHandle handle, GrainRegistry grainReg) {
                Handle = handle;
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


            void Perform(Func<IAsyncObserver<T>, Task> fn) {
                var activation = GrainReg.GetActivation(Handle.GrainKey); //observer usually populated here
                                
                activation.Requests.Perform(() => _observer != null ? fn(_observer) : Task.CompletedTask);
            }
            
        }


        
        class SubscriptionHandle : StreamSubscriptionHandle<T>
        {
            public readonly StreamKey StreamKey;
            public readonly GrainKey GrainKey;
            readonly StreamRegistry _streamReg;

            public SubscriptionHandle(StreamKey streamKey, GrainKey grainKey, StreamRegistry streamReg) {
                StreamKey = streamKey;
                GrainKey = grainKey;
                _streamReg = streamReg;
            }

            public override Guid HandleId { get; } = Guid.NewGuid(); //safe random???? Each stream should have 
            
            public override IStreamIdentity StreamIdentity {
                get { return StreamKey; }
            }

            public override bool Equals(StreamSubscriptionHandle<T> other) {
                var otherHandle = other as SubscriptionHandle;
                return otherHandle != null && otherHandle.StreamKey.Equals(StreamKey);
            }

            public override Task<StreamSubscriptionHandle<T>> ResumeAsync(IAsyncObserver<T> observer, StreamSequenceToken token = null) {
                throw new NotImplementedException();
            }

            public override Task UnsubscribeAsync() {
                throw new NotImplementedException();
            }

            public override bool Equals(object obj) {
                var other = obj as SubscriptionHandle;
                return other != null && other.StreamKey.Equals(StreamKey);
            }

            public override int GetHashCode() {
                return StreamKey.GetHashCode();
            }
        }



    }




}
