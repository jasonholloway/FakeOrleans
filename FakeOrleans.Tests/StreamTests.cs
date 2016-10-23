using FakeOrleans.Grains;
using FakeOrleans.Streams;
using NSubstitute;
using NUnit.Framework;
using Orleans;
using Orleans.Streams;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Orleans.Runtime;
using FakeOrleans.Components;

namespace FakeOrleans.Tests
{
    [TestFixture]
    public class StreamTests
    {
        [Test]
        public async Task SubscriberReceivesFromPublisher() 
        {
            var fx = new Fixture();
            fx.Types.Map(typeof(ISubscriber<>), typeof(Subscriber<>));
            fx.Types.Map(typeof(IPublisher<>), typeof(Publisher<>));

            var received = fx.Services.Inject(new ConcurrentBag<int>());

            var sub = fx.GrainFactory.GetGrain<ISubscriber<int>>(Guid.NewGuid());
            await sub.Subscribe();

            var prod = fx.GrainFactory.GetGrain<IPublisher<int>>(Guid.NewGuid());

            for(int i = 0; i < 10; i++) {
                await prod.Publish(i);
            }

            await fx.Requests.WhenIdle();

            Assert.That(received, Is.EquivalentTo(Enumerable.Range(0, 10)));
        }


        [Test]
        public async Task MultipleSubscribersReceiveFromPublisher() 
        {
            var fx = new Fixture();
            fx.Types.Map(typeof(ISubscriber<>), typeof(Subscriber<>));
            fx.Types.Map(typeof(IPublisher<>), typeof(Publisher<>));

            var received = fx.Services.Inject(new ConcurrentBag<int>());

            for(int i = 0; i < 10; i++) {
                var sub = fx.GrainFactory.GetGrain<ISubscriber<int>>(Guid.NewGuid());
                await sub.Subscribe();
            }

            var prod = fx.GrainFactory.GetGrain<IPublisher<int>>(Guid.NewGuid());

            for(int i = 0; i < 10; i++) {
                await prod.Publish(i);
            }

            await fx.Requests.WhenIdle();

            Assert.That(received, Is.EquivalentTo(Enumerable.Range(0, 10).SelectMany(_ => Enumerable.Range(0, 10))));
        }






        [Test]
        public async Task PublisherFiresAndForgets() 
        {
            var fx = new Fixture();
            fx.Types.Map(typeof(ISubscriber<>), typeof(Subscriber<>));
            fx.Types.Map(typeof(IPublisher<>), typeof(Publisher<>));

            var received = fx.Services.Inject(new ConcurrentBag<int>());

            for(int i = 0; i < 10; i++) {
                var sub = fx.GrainFactory.GetGrain<ISubscriber<int>>(Guid.NewGuid());
                await sub.SubscribeAndSlowlyConsume();
            }

            var prod = fx.GrainFactory.GetGrain<IPublisher<int>>(Guid.NewGuid());
            
            await prod.Publish(123);

            Assert.That(received.ToArray(), Is.Empty);
        }




        [Test]
        public async Task SubscribersRunViaGrainRequest()
        {
            var fx = new Fixture();
            fx.Types.Map(typeof(ISubscriber<>), typeof(Subscriber<>));
            fx.Types.Map(typeof(IPublisher<>), typeof(Publisher<>));

            var received = fx.Services.Inject(new ConcurrentBag<int>());
            
            var sub = fx.GrainFactory.GetGrain<ISubscriber<int>>(Guid.NewGuid());
            await sub.SubscribeAndSlowlyConsume();

            var prod = fx.GrainFactory.GetGrain<IPublisher<int>>(Guid.NewGuid());

            await prod.Publish(123);

            await fx.Requests.WhenIdle(); //if requests not used, this returns too soon..

            Assert.That(received, Is.EqualTo(new[] { 123 }));
        }



        [Test]
        public async Task StreamObservationResumableViaPersistedHandle() 
        {
            var fx = new Fixture();
            fx.Types.Map(typeof(ISubscriber<>), typeof(Subscriber<>));
            fx.Types.Map(typeof(IPublisher<>), typeof(Publisher<>));

            var received = fx.Services.Inject(new ConcurrentBag<int>());

            var sub = fx.GrainFactory.GetGrain<ISubscriber<int>>(Guid.NewGuid());
            await sub.SubscribeAndWrite();
            
            await fx.Activations.DeactivateAll(); //should be specific to subscriber grain - not general!

            await sub.ResumeFromPersistedHandle();
            
            var pub = fx.GrainFactory.GetGrain<IPublisher<int>>(Guid.NewGuid());
            await pub.Publish(1);
            await pub.Publish(2);
            await pub.Publish(3);

            await fx.Requests.WhenIdle();

            Assert.That(received, Is.EquivalentTo(new[] { 1, 2, 3 }));
        }




        [Test]
        public async Task StreamItemsPassThroughSerializer() 
        {
            var fx = new Fixture();
            fx.Types.Map(typeof(ISubscriber<>), typeof(Subscriber<>));
            fx.Types.Map(typeof(IPublisher<>), typeof(Publisher<>));

            var received = fx.Services.Inject(new ConcurrentBag<object>());

            var sub = fx.GrainFactory.GetGrain<ISubscriber<object>>(Guid.NewGuid());
            var pub = fx.GrainFactory.GetGrain<IPublisher<object>>(Guid.NewGuid());

            await sub.Subscribe();

            await pub.PublishNewTwice();

            await fx.Requests.WhenIdle();

            Assert.That(received.First(), Is.Not.EqualTo(received.Skip(1).First()));
        }



        [Test]
        public async Task CanUnsubscribeFromStreamViaHandle() 
        {
            var fx = new Fixture();
            fx.Types.Map(typeof(ISubscriber<>), typeof(Subscriber<>));
            fx.Types.Map(typeof(IPublisher<>), typeof(Publisher<>));

            var received = fx.Services.Inject(new ConcurrentBag<int>());

            var sub = fx.GrainFactory.GetGrain<ISubscriber<int>>(Guid.NewGuid());
            var pub = fx.GrainFactory.GetGrain<IPublisher<int>>(Guid.NewGuid());
            
            await sub.Subscribe();
            await sub.Unsubscribe();

            await pub.Publish(7);

            await fx.Requests.WhenIdle();

            Assert.That(received, Is.Empty);
        }




        [Test]
        public async Task ImplicitStreamSubcriptionsWork() 
        {
            var fx = new Fixture();
            fx.Types.Map<IImplicitSubscriber<int>, ImplicitSubscriber<int>>(); //have to register resolved gen types for imp subs to function
            fx.Types.Map(typeof(IPublisher<>), typeof(Publisher<>));

            var received = fx.Services.Inject(new ConcurrentBag<int>());
            
            var pub = fx.GrainFactory.GetGrain<IPublisher<int>>(Guid.NewGuid());

            await pub.SwitchStream("one", Guid.NewGuid());
            await pub.Publish(1);

            await pub.SwitchStream("two", Guid.NewGuid());
            await pub.Publish(2);

            await pub.SwitchStream("three", Guid.NewGuid());
            await pub.Publish(3);
            
            await fx.Requests.WhenIdle();

            Assert.That(received, Is.EqualTo(new[] { 1, 3 }));
        }




        public interface IImplicitSubscriber<T> : IGrainWithGuidKey
        { }


        [ImplicitStreamSubscription("one")]
        [ImplicitStreamSubscription("three")]
        public class ImplicitSubscriber<T> : Grain, IImplicitSubscriber<T>, IAsyncObserver<int>
        {
            ConcurrentBag<int> _received;

            public ImplicitSubscriber(ConcurrentBag<int> received) {
                _received = received;
            }
            
            public override Task OnActivateAsync() 
            {
                {
                    var streamProv = GetStreamProvider(StreamProviderName);
                    var stream = streamProv.GetStream<int>(this.GetPrimaryKey(), "one");
                    stream.SubscribeAsync(this);
                }

                {
                    var streamProv = GetStreamProvider(StreamProviderName);
                    var stream = streamProv.GetStream<int>(this.GetPrimaryKey(), "three");
                    stream.SubscribeAsync(this);
                }

                return Task.CompletedTask;
            }

            Task IAsyncObserver<int>.OnCompletedAsync() {
                throw new NotImplementedException();
            }

            Task IAsyncObserver<int>.OnErrorAsync(Exception ex) {
                throw new NotImplementedException();
            }

            Task IAsyncObserver<int>.OnNextAsync(int item, StreamSequenceToken token) {
                _received.Add(item);
                return Task.CompletedTask;
            }
        }





        

        [Test]
        public async Task SubscriptionHandleSerializes() 
        {
            Assert.Ignore();

            //var fx = new Fixture();
            //fx.Types.Map<IDummy, Dummy>();
            
            //var streamKey = new StreamKey("prov", "ns", Guid.NewGuid());
            //var subKey = new Stream.SubKey(streamKey, Guid.NewGuid());
            
            //var activation = new Activation(null, null);
                        
            //var serializer = new FakeSerializer(new GrainContext(fx, activation));
            
            //var handle = new GrainStreamHandle<int>(subKey, activation, fx.Streams);
            
            //var cloned = serializer.Clone(handle);


            //Assert.That(cloned.SubscriptionKey, Is.EqualTo(subKey));

            //var fStreamReg = typeof(GrainStreamHandle<int>).GetField("_streamReg", BindingFlags.Instance | BindingFlags.NonPublic);
            //var clonedStreamReg = fStreamReg.GetValue(cloned);
            //Assert.That(clonedStreamReg, Is.EqualTo(fx.Streams));
        }



        public interface IDummy : IGrainWithGuidKey
        { }

        public class Dummy : Grain, IDummy
        { }




        static string StreamProviderName = "test";
        static string StreamNamespace = "blah";
        static Guid StreamId = Guid.NewGuid();


        public interface IPublisher<T> : IGrainWithGuidKey
            where T : new()
        {
            Task Publish(T val);
            Task PublishNewTwice();
            Task SwitchStream(string @namespace, Guid streamId);
        }

        public class Publisher<T> : Grain, IPublisher<T>
            where T : new()
        {
            IAsyncStream<T> _stream;

            public override Task OnActivateAsync() {
                var streamProv = GetStreamProvider(StreamProviderName);
                _stream = streamProv.GetStream<T>(StreamId, StreamNamespace);
                return Task.CompletedTask;
            }

            public Task Publish(T val)
                => _stream.OnNextAsync(val);


            public Task PublishNewTwice() {
                var o = new T();
                _stream.OnNextAsync(o);
                _stream.OnNextAsync(o);
                return Task.CompletedTask;
            }

            public Task SwitchStream(string @namespace, Guid streamId) {
                var streamProv = GetStreamProvider(StreamProviderName);
                _stream = streamProv.GetStream<T>(streamId, @namespace);
                return Task.CompletedTask;
            }
        }

        


        public interface ISubscriber<T> : IGrainWithGuidKey
        {
            Task Subscribe();
            Task SubscribeAndSlowlyConsume();
            Task SubscribeAndWrite();
            Task ResumeFromPersistedHandle();
            Task Unsubscribe();
        }

        public class Subscriber<T> : Grain<SubscriberState<T>>, ISubscriber<T>, IAsyncObserver<T>
        {            
            ConcurrentBag<T> _sink;
            bool _goSlow = false;

            public Subscriber(ConcurrentBag<T> sink) {
                _sink = sink;
            }
            

            public async Task Subscribe() {
                var streamProv = GetStreamProvider(StreamProviderName);
                var stream = streamProv.GetStream<T>(StreamId, StreamNamespace);
                State.SubscriptionHandle = await stream.SubscribeAsync(this);
            }

            public async Task SubscribeAndSlowlyConsume() {
                _goSlow = true;
                await Subscribe();
            }


            public async Task SubscribeAndWrite() {
                await Subscribe();
                await WriteStateAsync();
            }


            public async Task ResumeFromPersistedHandle() {
                await ReadStateAsync();
                await State.SubscriptionHandle.ResumeAsync(this);
            }

            public async Task Unsubscribe() {
                await State.SubscriptionHandle.UnsubscribeAsync();
            }
            

            public async Task OnNextAsync(T item, StreamSequenceToken token = null) {
                if(_goSlow) {
                    await Task.Delay(300);
                }

                _sink.Add(item);
            }

            public Task OnCompletedAsync() {
                throw new NotImplementedException();
            }

            public Task OnErrorAsync(Exception ex) {
                throw new NotImplementedException();
            }
        }


        [Serializable]
        public class SubscriberState<T>
        {
            public StreamSubscriptionHandle<T> SubscriptionHandle { get; set; }
        }






        



    }
}
