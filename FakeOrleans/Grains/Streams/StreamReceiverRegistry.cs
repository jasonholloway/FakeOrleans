using FakeOrleans.Streams;
using Orleans.Streams;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeOrleans.Grains
{
    

    public class StreamReceiverRegistry
    {
        FakeSerializer _serializer;
        ConcurrentDictionary<Stream.SubKey, IStreamSink> _dObservers;

        public StreamReceiverRegistry(FakeSerializer serializer) {
            _serializer = serializer;
            _dObservers = new ConcurrentDictionary<Stream.SubKey, IStreamSink>();
        }

        public void Register<T>(Stream.SubKey subKey, IAsyncObserver<T> observer) {
            _dObservers[subKey] = new Observer<T>(_serializer, observer);
        }

        public void Unregister(Stream.SubKey subKey) {
            IStreamSink _;
            _dObservers.TryRemove(subKey, out _);
        }
        
        public IStreamSink Find(Stream.SubKey subKey) {
            IStreamSink observer = null;
            _dObservers.TryGetValue(subKey, out observer);
            return observer;
        }



        public class Observer<T> : IStreamSink
        {
            FakeSerializer _serializer;
            IAsyncObserver<T> _observer;

            public Observer(FakeSerializer serializer, IAsyncObserver<T> observer) {
                _serializer = serializer;
                _observer = observer;
            }

            public Task OnNext(byte[] itemData, StreamSequenceToken token = null) {
                var item = (T)_serializer.Deserialize(itemData);
                return _observer.OnNextAsync(item, token);
            }

            public Task OnCompleted()
                => _observer.OnCompletedAsync();

            public Task OnError(Exception ex)
                => _observer.OnErrorAsync(ex);

        }



    }




}
