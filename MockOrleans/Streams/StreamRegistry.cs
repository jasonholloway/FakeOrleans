using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans.Providers;
using System.Collections.Concurrent;
using MockOrleans.Grains;

namespace MockOrleans.Streams
{

    public class StreamRegistry
    {        
        public readonly MockFixture Fixture;

        ConcurrentDictionary<StreamKey, IStreamHub> _dStreamHubs = new ConcurrentDictionary<StreamKey, IStreamHub>();


        public StreamRegistry(MockFixture fixture) {
            Fixture = fixture;
        }


        public IStreamHub GetStream<T>(StreamKey key)
            => _dStreamHubs.GetOrAdd(key, CreateStream<T>);

        
        IStreamHub CreateStream<T>(StreamKey key) {
            return new StreamHub<T>(Fixture.Grains);
        }
        
    }

    


    //the stream provider keeps clients within it - 
    public class MockStreamProvider : IStreamProvider, IProvider
    {
        ConcurrentDictionary<string, object> _dStreams = new ConcurrentDictionary<string, object>();


        public string Name { get; private set; }
        
        public MockStreamProvider(string name) {
            Name = name;
        }
        
        public bool IsRewindable {
            get { return false; }
        }

        string IProvider.Name {
            get {
                throw new NotImplementedException();
            }
        }

        public IAsyncStream<T> GetStream<T>(Guid streamId, string streamNamespace) {
            var key = $"{streamNamespace}_{streamId}";
            return (IAsyncStream<T>)_dStreams.GetOrAdd(key, k => new MockStream<T>(this, streamId, streamNamespace));
        }

        Task IProvider.Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config) {
            return Task.FromResult(true);
        }

        Task IProvider.Close() {
            return Task.FromResult(true);
        }
    }






    public class MockStream<T> : IAsyncStream<T>
    {
        public readonly MockStreamProvider Provider;
        public readonly Guid StreamId;
        public readonly string Namespace;
        
        public bool IsRewindable { get; private set; }       
        public string ProviderName { get; private set; }

        ConcurrentQueue<T> _items = new ConcurrentQueue<T>();

        ConcurrentBag<IAsyncObserver<T>> _observers = new ConcurrentBag<IAsyncObserver<T>>();

        
        public MockStream(MockStreamProvider provider, Guid streamId, string @namespace) {
            Provider = provider;
            StreamId = streamId;
            Namespace = @namespace;
        }

                

        /*
         * Stream is just a local agent, a proxy for the extension that lives in the same silo as the grain
         * and is ruled by its scheduler 
         * 
         * Each activation will have space for such stream clients, which you will be able to pile up.
         * 
         * The stream client receives stuff on caller's scheduler, queues it in local buffer. 
         * 
         * Local buffer just means appending to the request list - ie waiting on the shared semaphore.
         * 
         * For each StreamClient there is a central Stream that does the pubsub registration.
         * 
         * Streams are held, ready for snooping, in the central StreamRegistry.
         * 
         * --------------------------------------------
         * 
         * StreamClient is set up and is addressable by outside agent 
         * but if a grain is deactivated, then what happens to it? 
         * The client remains - and will linger in memory, as the observer attached to the client,
         * but nothing will happen, as the scheduler will be dead.
         */



        public Task OnNextAsync(T item, StreamSequenceToken token = null) 
        {
            //no buffer, but sending of requests to all subscribers
            //a spy here can collect what it likes

            _items.Enqueue(item);
            
            return _observers.ToArray().AsParallel()
                        .ForEach(o => { return o.OnNextAsync(item); });
        }

        public Task OnNextBatchAsync(IEnumerable<T> batch, StreamSequenceToken token = null) {
            throw new NotImplementedException();
        }


        public Task<StreamSubscriptionHandle<T>> SubscribeAsync(IAsyncObserver<T> observer) 
        {
            _observers.Add(observer);

            return Task.FromResult(
                            (StreamSubscriptionHandle<T>)new SubscriptionHandle()
                            ); //good enough for now
        }

                

        class SubscriptionHandle : StreamSubscriptionHandle<T>
        {
            public override Guid HandleId {
                get {
                    throw new NotImplementedException();
                }
            }

            public override IStreamIdentity StreamIdentity {
                get {
                    throw new NotImplementedException();
                }
            }

            public override bool Equals(StreamSubscriptionHandle<T> other) {
                throw new NotImplementedException();
            }

            public override Task<StreamSubscriptionHandle<T>> ResumeAsync(IAsyncObserver<T> observer, StreamSequenceToken token = null) {
                throw new NotImplementedException();
            }

            public override Task UnsubscribeAsync() {
                throw new NotImplementedException();
            }
        }




        public int CompareTo(IAsyncStream<T> other) {
            throw new NotImplementedException();
        }

        public bool Equals(IAsyncStream<T> other) {
            throw new NotImplementedException();
        }




        public Task<IList<StreamSubscriptionHandle<T>>> GetAllSubscriptionHandles() {
            throw new NotImplementedException();
        }

        public Task OnCompletedAsync() {
            throw new NotImplementedException();
        }

        public Task OnErrorAsync(Exception ex) {
            throw new NotImplementedException();
        }

        Task<IList<StreamSubscriptionHandle<T>>> IAsyncStream<T>.GetAllSubscriptionHandles() {
            throw new NotImplementedException();
        }

        bool IEquatable<IAsyncStream<T>>.Equals(IAsyncStream<T> other) {
            throw new NotImplementedException();
        }

        int IComparable<IAsyncStream<T>>.CompareTo(IAsyncStream<T> other) {
            throw new NotImplementedException();
        }
                
        Task<StreamSubscriptionHandle<T>> IAsyncObservable<T>.SubscribeAsync(IAsyncObserver<T> observer, StreamSequenceToken token, StreamFilterPredicate filterFunc, object filterData) {
            throw new NotImplementedException();
        }
        
        Task IAsyncObserver<T>.OnCompletedAsync() {
            throw new NotImplementedException();
        }

        Task IAsyncObserver<T>.OnErrorAsync(Exception ex) {
            throw new NotImplementedException();
        }




        bool IAsyncStream<T>.IsRewindable {
            get { return false; }
        }

        string IAsyncStream<T>.ProviderName {
            get { return Provider.Name; }
        }
                
        Guid IStreamIdentity.Guid {
            get { return StreamId; }
        }

        string IStreamIdentity.Namespace {
            get { return Namespace; }
        }
        
    }

}
