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
    
    public interface IStreamSink
    {
        Task OnNext(byte[] itemData, StreamSequenceToken token = null);
        Task OnCompleted();
        Task OnError(Exception ex);
    }
        


    public class Stream : IStreamSink
    {
        public readonly StreamKey Key;

        StreamRegistry _streamReg;
        GrainRegistry _grainReg;
        ConcurrentDictionary<Guid, Subscription> _dSubscriptions;


        public Stream(StreamKey key, StreamRegistry streamReg, GrainRegistry grainReg)
        {
            Key = key;
            _streamReg = streamReg;
            _grainReg = grainReg;
            _dSubscriptions = new ConcurrentDictionary<Guid, Subscription>();
        }

        
        public Task OnNext(byte[] itemData, StreamSequenceToken token = null)
            => Perform(s => s.OnNext(itemData, token));

        public Task OnCompleted()
            => Perform(s => s.OnCompleted());

        public Task OnError(Exception ex)
            => Perform(s => s.OnError(ex));


        Task Perform(Func<Subscription, Task> fn) {
            var subscriptions = _dSubscriptions.Values.ToArray();
            return subscriptions.Select(fn).WhenAll();
        }


        

        public SubKey Subscribe(GrainKey grainKey) 
        {
            var subKey = new SubKey(Key, Guid.NewGuid());

            var subscription = new Subscription(subKey, grainKey, this, _grainReg);

            _dSubscriptions[subKey.SubscriptionId] = subscription;

            return subKey;
        }

        
        public void Unsubscribe(SubKey subKey) 
        {
            Subscription _;
            _dSubscriptions.TryRemove(subKey.SubscriptionId, out _);
        }



        [Serializable]
        public struct SubKey : IEquatable<SubKey>
        {
            public readonly StreamKey StreamKey;
            public readonly Guid SubscriptionId;

            public SubKey(StreamKey streamKey, Guid subId) {
                StreamKey = streamKey;
                SubscriptionId = subId;
            }

            #region overrides

            public bool Equals(SubKey other)
                => other.StreamKey.Equals(StreamKey) && other.SubscriptionId.Equals(SubscriptionId);

            public override bool Equals(object obj)
                => obj is SubKey && Equals((SubKey)obj);

            public override int GetHashCode()
                => StreamKey.GetHashCode() ^ SubscriptionId.GetHashCode();

            #endregion

        }





        class Subscription : IStreamSink
        {
            public readonly SubKey Key;
            public readonly GrainKey GrainKey;
            public readonly Stream Stream;
            public readonly GrainRegistry GrainReg;
            
            
            public Subscription(SubKey key, GrainKey grainKey, Stream stream, GrainRegistry grainReg) {
                Key = key;
                GrainKey = grainKey;
                Stream = stream;
                GrainReg = grainReg;
            }
            
            //stream should be generic really...

            public Task OnNext(byte[] itemData, StreamSequenceToken token = null) {
                Perform(o => o.OnNext(itemData, token));
                return Task.CompletedTask;
            }

            public Task OnCompleted() {
                Perform(o => o.OnCompleted());
                return Task.CompletedTask;
            }

            public Task OnError(Exception ex) {
                Perform(o => o.OnError(ex));
                return Task.CompletedTask;
            }
            

            void Perform(Func<IStreamSink, Task> fn) 
            {
                var activation = GrainReg.GetActivation(GrainKey); //observer usually populated here

                var observer = activation.StreamReceivers.Find(Key);

                if(observer != null) {
                    activation.Requests.Perform(() => fn(observer));
                }
            }
                        
        }
        
    }




}
