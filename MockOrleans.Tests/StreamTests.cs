using NUnit.Framework;
using Orleans;
using Orleans.Streams;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockOrleans.Tests
{
    [TestFixture]
    public class StreamTests
    {
        [Test]
        public async Task SubscriberReceivesFromPublisher() 
        {
            var fx = new MockFixture();
            fx.Types.Map<IPublisher, Publisher>();
            fx.Types.Map<ISubscriber, Subscriber>();

            var received = fx.Services.Inject(new ConcurrentBag<int>());

            var sub = fx.GrainFactory.GetGrain<ISubscriber>(Guid.NewGuid());
            await sub.Subscribe();

            var prod = fx.GrainFactory.GetGrain<IPublisher>(Guid.NewGuid());

            for(int i = 0; i < 10; i++) {
                await prod.Publish(i);
            }

            await fx.Requests.WhenIdle();
            await fx.Scheduler.WhenIdle();

            Assert.That(received, Is.EquivalentTo(Enumerable.Range(0, 10)));
        }


        [Test]
        public async Task MultipleSubscribersReceiveFromPublisher() 
        {
            var fx = new MockFixture();
            fx.Types.Map<IPublisher, Publisher>();
            fx.Types.Map<ISubscriber, Subscriber>();

            var received = fx.Services.Inject(new ConcurrentBag<int>());

            for(int i = 0; i < 10; i++) {
                var sub = fx.GrainFactory.GetGrain<ISubscriber>(Guid.NewGuid());
                await sub.Subscribe();
            }

            var prod = fx.GrainFactory.GetGrain<IPublisher>(Guid.NewGuid());

            for(int i = 0; i < 10; i++) {
                await prod.Publish(i);
            }

            await fx.Requests.WhenIdle();
            await fx.Scheduler.WhenIdle();

            Assert.That(received, Is.EquivalentTo(Enumerable.Range(0, 10).SelectMany(_ => Enumerable.Range(0, 10))));
        }






        [Test]
        public async Task PublisherFiresAndForgets() 
        {
            var fx = new MockFixture();
            fx.Types.Map<IPublisher, Publisher>();
            fx.Types.Map<ISubscriber, Subscriber>();

            var received = fx.Services.Inject(new ConcurrentBag<int>());

            for(int i = 0; i < 10; i++) {
                var sub = fx.GrainFactory.GetGrain<ISubscriber>(Guid.NewGuid());
                await sub.SubscribeAndSlowlyConsume();
            }

            var prod = fx.GrainFactory.GetGrain<IPublisher>(Guid.NewGuid());
            
            await prod.Publish(123);

            Assert.That(received.ToArray(), Is.Empty);
        }




        [Test]
        public async Task SubscribersRunViaGrainRequest()  //and interleave also!(?) - need to look this up
        {
            var fx = new MockFixture();
            fx.Types.Map<IPublisher, Publisher>();
            fx.Types.Map<ISubscriber, Subscriber>();

            var received = fx.Services.Inject(new ConcurrentBag<int>());
            
            var sub = fx.GrainFactory.GetGrain<ISubscriber>(Guid.NewGuid());
            await sub.SubscribeAndSlowlyConsume();

            var prod = fx.GrainFactory.GetGrain<IPublisher>(Guid.NewGuid());

            await prod.Publish(123);

            await fx.Requests.WhenIdle();

            Assert.That(received, Is.EqualTo(new[] { 123 }));

            //this falsely passes at mo as fire-and-forget isn't working
        }




        [Test]
        public async Task StreamItemsPassThroughSerializer() {
            throw new NotImplementedException();
        }



        [Test]
        public async Task StreamHandlesAreSerializable() {
            throw new NotImplementedException();
        }






        static Guid StreamId = Guid.NewGuid();

        public interface IPublisher : IGrainWithGuidKey
        {
            Task Publish(int i);
        }

        public class Publisher : Grain, IPublisher
        {
            IAsyncStream<int> _stream;

            public override Task OnActivateAsync() {
                var streamProv = GetStreamProvider("Test");
                _stream = streamProv.GetStream<int>(StreamId, "Numbers");
                return Task.CompletedTask;
            }

            public Task Publish(int i)
                => _stream.OnNextAsync(i);

        }

        


        public interface ISubscriber : IGrainWithGuidKey
        {
            Task Subscribe();
            Task SubscribeAndSlowlyConsume();
        }

        public class Subscriber : Grain, ISubscriber, IAsyncObserver<int>
        {
            IAsyncStream<int> _stream;
            ConcurrentBag<int> _numberSink;
            bool _goSlow = false;

            public Subscriber(ConcurrentBag<int> numberSink) {
                _numberSink = numberSink;
            }
            

            public async Task Subscribe() {
                var streamProv = GetStreamProvider("Test");
                _stream = streamProv.GetStream<int>(StreamId, "Numbers");
                await _stream.SubscribeAsync(this);
            }
            

            public async Task SubscribeAndSlowlyConsume() {
                _goSlow = true;
                await Subscribe();
            }


            public async Task OnNextAsync(int item, StreamSequenceToken token = null) {
                if(_goSlow) {
                    await Task.Delay(300);
                }

                _numberSink.Add(item);
            }

            public Task OnCompletedAsync() {
                throw new NotImplementedException();
            }

            public Task OnErrorAsync(Exception ex) {
                throw new NotImplementedException();
            }
        }







        



    }
}
