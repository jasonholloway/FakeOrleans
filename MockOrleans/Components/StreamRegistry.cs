using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans.Providers;
using System.Collections.Concurrent;

namespace MockOrleans
{

    public class StreamRegistry : IStreamProviderManager
    {
        ConcurrentDictionary<string, IStreamProvider> _dStreamProvs = new ConcurrentDictionary<string, IStreamProvider>();
        
        public IProvider GetProvider(string name) {
            return (IProvider)_dStreamProvs.GetOrAdd(name, n => new MockStreamProvider(n));
        }

        public IEnumerable<IStreamProvider> GetStreamProviders() {
            throw new NotImplementedException();
        }
    }



    public static class StreamRegistryExtensions
    {

        public static MockStream<T> GetStream<T>(this StreamRegistry mgr, StreamKey<T> streamKey) 
        {
            var prov = (IStreamProvider)mgr.GetProvider(streamKey.ProviderName);
            var str = prov.GetStream<T>(streamKey.Id, streamKey.Namespace);
            return (MockStream<T>)str;
        }

    }






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

        public IAsyncStream<T> GetStream<T>(Guid streamId, string streamNamespace) 
        {
            var key = $"{streamNamespace}_{streamId}";

            return (IAsyncStream<T>)_dStreams
                                        .GetOrAdd(key, k => new MockStream<T>() {
                                                                    Guid = streamId,
                                                                    Namespace = streamNamespace,
                                                                    ProviderName = Name,
                                                                    IsRewindable = false
                                                                });
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
        public Guid Guid { get; set; }        
        public bool IsRewindable { get; set; }
        public string Namespace { get; set; }        
        public string ProviderName { get; set; }

        ConcurrentQueue<T> _items = new ConcurrentQueue<T>();

        ConcurrentBag<IAsyncObserver<T>> _observers = new ConcurrentBag<IAsyncObserver<T>>();


        public IEnumerable<T> Items {
            get { return _items.ToArray(); }
        }
 

        public Task OnNextAsync(T item, StreamSequenceToken token = null) 
        {
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


        public Task<StreamSubscriptionHandle<T>> SubscribeAsync(IAsyncObserver<T> observer, StreamSequenceToken token, StreamFilterPredicate filterFunc = null, object filterData = null) {
            throw new NotImplementedException();
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

    }

}
