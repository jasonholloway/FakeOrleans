using FakeOrleans.Streams;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeOrleans.Grains
{

    public class GrainStreamClient<T> : IAsyncStream<T>
    {
        GrainHarness _activation;
        GrainKey _grainKey;
        Stream _stream;
        StreamRegistry _streamReg;

        public GrainStreamClient(GrainHarness activation, Stream stream, StreamRegistry streamReg) {
            _activation = activation;
            _grainKey = _activation.Placement.Key;
            _stream = stream;
            _streamReg = streamReg;
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


        #region IAsyncObserver<T>

        public Task OnCompletedAsync()
            => _stream.OnCompleted();

        public Task OnErrorAsync(Exception ex)
            => _stream.OnError(ex);

        public Task OnNextAsync(T item, StreamSequenceToken token = null)
            => _stream.OnNext(_activation.Serializer.Serialize(item), token);

        public Task OnNextBatchAsync(IEnumerable<T> batch, StreamSequenceToken token = null) { 
            throw new NotImplementedException();
        }

        #endregion


        #region IAsyncObservable<T>

        public Task<StreamSubscriptionHandle<T>> SubscribeAsync(IAsyncObserver<T> observer) 
        {
            //oddly, there's special implicit-only logic here
            //if this stream is implicit, then we just reattach our observer, no subscription-creation required...

            //how de we know if we're implicit or not? The client should be in a different mode maybe
            //but the client gets created from the StreamProviderAdaptor: this latter then needs to know whether
            //its creation is implicitly subscribed or not


            var subKey = _stream.Subscribe(_grainKey);

            _activation.StreamReceivers.Register(subKey, observer);

            var handle = new GrainStreamHandle<T>(subKey, _activation, _streamReg);

            return Task.FromResult((StreamSubscriptionHandle<T>)handle);
        }


        public Task<StreamSubscriptionHandle<T>> SubscribeAsync(IAsyncObserver<T> observer, StreamSequenceToken token, StreamFilterPredicate filterFunc = null, object filterData = null) {
            throw new NotImplementedException();
        }

        #endregion


        #region IComparable, IEquatable...

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

        #endregion

    }




}
