using MockOrleans.Grains;
using Orleans.Providers;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockOrleans.Streams
{


    public class GrainStreamProviderAdaptor : IStreamProvider, IProvider
    {
        public string Name { get; private set; }
        public bool IsRewindable { get; } = false;

        StreamRegistry _streamReg;
        GrainHarness _activation;
        
        public GrainStreamProviderAdaptor(GrainHarness activation, StreamRegistry streamReg, string providerName) {
            Name = providerName;
            _streamReg = streamReg;
            _activation = activation;
        }
        
        public IAsyncStream<T> GetStream<T>(Guid streamId, string streamNamespace) 
        {
            var key = new StreamKey(Name, streamNamespace, streamId);
            
            var stream = _streamReg.GetStream<T>(key);
            
            return new GrainStreamAdaptor<T>(_activation, stream);
        }

        #region IProvider

        Task IProvider.Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
            => Task.CompletedTask;

        Task IProvider.Close()
            => Task.CompletedTask;

        #endregion
    }



    public class GrainStreamAdaptor<T> : IAsyncStream<T>
    {
        GrainHarness _activation;
        GrainKey _grainKey;
        StreamHub<T> _stream;

        public GrainStreamAdaptor(GrainHarness activation, StreamHub<T> stream) {
            _activation = activation;
            _grainKey = _activation.Placement.Key;
            _stream = stream;
        }
        
        public Guid Guid {
            get { return _stream.Key.Id; }
        }

        public bool IsRewindable {
            get { return false; }
        }

        public string Namespace {
            get { return _stream.Key.Namespace; }
        }

        public string ProviderName {
            get { return _stream.Key.ProviderName; }
        }


        public Task<IList<StreamSubscriptionHandle<T>>> GetAllSubscriptionHandles() {
            
            throw new NotImplementedException();
        }





        public Task OnCompletedAsync()
            => _stream.OnCompletedAsync();

        public Task OnErrorAsync(Exception ex)
            => _stream.OnErrorAsync(ex);         

        public Task OnNextAsync(T item, StreamSequenceToken token = null)
            => _stream.OnNextAsync(item, token);
        
        public Task OnNextBatchAsync(IEnumerable<T> batch, StreamSequenceToken token = null) {
            throw new NotImplementedException();
        }



        public Task<StreamSubscriptionHandle<T>> SubscribeAsync(IAsyncObserver<T> observer) 
        {            
            var handle = _stream.Subscribe(_grainKey);

            _activation.StreamObservers.Register(_stream.Key, observer);

            return Task.FromResult(handle);            
        }


        public Task<StreamSubscriptionHandle<T>> SubscribeAsync(IAsyncObserver<T> observer, StreamSequenceToken token, StreamFilterPredicate filterFunc = null, object filterData = null) {
            throw new NotImplementedException();
        }



        int IComparable<IAsyncStream<T>>.CompareTo(IAsyncStream<T> other) {
            int r = ProviderName.CompareTo(other.ProviderName);
            if(r != 0) return r;

            r = Namespace.CompareTo(other.Namespace);
            if(r != 0) return r;

            return Guid.CompareTo(other.Guid);
        }

        bool IEquatable<IAsyncStream<T>>.Equals(IAsyncStream<T> other)
            => ProviderName.Equals(other.ProviderName)
                    && Namespace.Equals(other.Namespace)
                    && Guid.Equals(other.Guid);

    }










    class StreamProviderManagerAdaptor : IStreamProviderManager
    {
        GrainHarness _activation;
        StreamRegistry _streamReg;

        public StreamProviderManagerAdaptor(GrainHarness activation, StreamRegistry streamReg) {
            _activation = activation;
            _streamReg = streamReg;
        }

        public IProvider GetProvider(string name)
            => new GrainStreamProviderAdaptor(_activation, _streamReg, name);


        public IEnumerable<IStreamProvider> GetStreamProviders() {

            //StreamRegistry needs a dictionary of streamproviders, implicitly created

            throw new NotImplementedException(); //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        }
    }
    

}
