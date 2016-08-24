using MockOrleans.Components;
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
        IDispatcher _disp;
        RequestRunner _requests;
        ConcurrentDictionary<Guid, Subscription> _dSubscriptions;


        public Stream(StreamKey key, StreamRegistry streamReg, IDispatcher disp, RequestRunner requests)
        {
            Key = key;
            _streamReg = streamReg;
            _disp = disp;
            _requests = requests;
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
        
        

        public SubKey Subscribe(GrainKey grainKey, bool isImplicit = false) 
        {
            var subs = _dSubscriptions.Values.ToArray();
            var foundImplicitSub = subs.FirstOrDefault(s => s.IsImplicit && s.GrainKey.Equals(grainKey));
            if(foundImplicitSub != null) return foundImplicitSub.Key;
            
            var subKey = new SubKey(Key, Guid.NewGuid());

            var subscription = new Subscription(subKey, grainKey, this, _disp, _requests, isImplicit);

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
            public readonly IDispatcher Dispatcher;
            public readonly RequestRunner Requests;
            public readonly bool IsImplicit;
            
            
            public Subscription(SubKey key, GrainKey grainKey, Stream stream, IDispatcher disp, RequestRunner requests, bool isImplicit) {
                Key = key;
                GrainKey = grainKey;
                Stream = stream;
                Dispatcher = disp;
                Requests = requests;
                IsImplicit = isImplicit;
            }
            

            public Task OnNext(byte[] itemData, StreamSequenceToken token = null)
                => Perform(o => o.OnNext(itemData, token));
            
            public Task OnCompleted()
                => Perform(o => o.OnCompleted());
            
            public Task OnError(Exception ex) 
                => Perform(o => o.OnError(ex));
            

            Task Perform(Func<IStreamSink, Task> fn) {
                Requests.PerformAndForget(async () => {
                    //var activation = await Grains.GetActivation(GrainKey);

                    await Dispatcher.Dispatch(GrainKey, g => Task.FromResult(true)); //need to get observer from activation

                    //AND FORGET ABOVE!

                    //var observer = activation.StreamReceivers.Find(Key);

                    //if(observer != null) {
                    //    activation.Requests.PerformAndForget(() => fn(observer)); //should isolate with the activation default
                    //}
                });

                return Task.CompletedTask;
            }
                        
        }
        
    }




}
