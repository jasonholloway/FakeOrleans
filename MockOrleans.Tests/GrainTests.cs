using MockOrleans;
using MockOrleans.Grains;
using NSubstitute;
using NUnit.Framework;
using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MockOrleans.Tests
{

    [TestFixture]
    public class GrainTests
    {        
        [Test]
        public async Task ReentrantGrainsInterleaveRequests() 
        {
            var fx = new MockFixture();
            fx.Types.Map<IPingPonger, PingPonger>();

            var qReqCounts = fx.Services.Inject(new ConcurrentQueue<int>());

            var grain1 = fx.GrainFactory.GetGrain<IPingPonger>(Guid.NewGuid());
            var grain2 = fx.GrainFactory.GetGrain<IPingPonger>(Guid.NewGuid());

            await grain1.PingPong(grain2, 10); //if reentrancy not working, will deadlock

            Assert.That(qReqCounts, Has.Some.GreaterThan(1));
        }
        

        public interface IPingPonger : IGrainWithGuidKey
        {
            Task PingPong(IPingPonger other, int further);
        }
        

        [Reentrant]
        public class PingPonger : Grain, IPingPonger
        {
            int _currReqCount = 0;

            ConcurrentQueue<int> _qReqCounts;
            
            public PingPonger(ConcurrentQueue<int> qReqCounts) {
                _qReqCounts = qReqCounts;
            }

            
            public async Task PingPong(IPingPonger other, int further) 
            {
                int c = Interlocked.Increment(ref _currReqCount);
                _qReqCounts.Enqueue(c);
                
                await Task.Delay(10);

                if(further > 0) {
                    await other.PingPong(this, further - 1);
                }
                
                Interlocked.Decrement(ref _currReqCount);
            }
        }

        






        [Test]
        public async Task ReentrantGrainsRespectSingleActivationRoutine() 
        {
            var fx = new MockFixture();
            fx.Types.Map<IReentrantActivator, ReentrantActivator>();
            var qActivations = fx.Services.Inject(new ConcurrentQueue<int>());

            var grain = fx.GrainFactory.GetGrain<IReentrantActivator>(Guid.NewGuid());
            
            await Enumerable.Range(0, 10).Select(_ => grain.Hello()).WhenAll();

            Assert.That(qActivations.Count, Is.EqualTo(1));
        }


        [Test]
        public async Task ReentrantGrainsRespectSingleDeactivationRoutine() 
        {
            throw new NotImplementedException();
            //var fx = new MockFixture();
            //fx.Types.Map<IReentrantActivator, ReentrantActivator>();
            //var qActivations = fx.Services.Inject(new ConcurrentQueue<int>());

            //var grain = fx.GrainFactory.GetGrain<IReentrantActivator>(Guid.NewGuid());

            //await Enumerable.Range(0, 10).Select(_ => grain.Hello()).WhenAll();

            //Assert.That(qActivations.Count, Is.EqualTo(1));
        }







        public interface IReentrantActivator : IGrainWithGuidKey
        {
            Task Hello();

        }


        [Reentrant]
        public class ReentrantActivator : Grain, IReentrantActivator
        {
            ConcurrentQueue<int> _qActivations;

            public ReentrantActivator(ConcurrentQueue<int> qActivations) {
                _qActivations = qActivations;
            }

            public Task Hello() => Task.CompletedTask;
            
            public override async Task OnActivateAsync() {
                _qActivations.Enqueue(1);
                await Task.Delay(50);
            }
        }








        [Test]
        public async Task DeactivationIncursFreshActivation() 
        {
            var fx = new MockFixture();
            fx.Types.Map<IReactivatable, Reactivatable>();

            var recorder = fx.Services.Inject(new ActivationRecorder());

            var grain = fx.GrainFactory.GetGrain<IReactivatable>(Guid.NewGuid());

            await grain.PrecipitateDeactivation();

            await fx.Requests.WhenIdle(); //and what if deactivations and reactivations compete? Once deactivation has started, we're done...

            await grain.Reactivate();

            Assert.That(recorder.Activations, Has.Count.EqualTo(2));
            Assert.That(recorder.Deactivations, Has.Count.EqualTo(1));
        }


        [Test]
        public async Task DeactivationReactivationCompetition() 
        {
            var fx = new MockFixture();
            fx.Types.Map<IReactivatable, Reactivatable>();

            var recorder = fx.Services.Inject(new ActivationRecorder());

            var grain = fx.GrainFactory.GetGrain<IReactivatable>(Guid.NewGuid());

            await grain.PrecipitateDeactivation();            
            await grain.Reactivate();
            await grain.PrecipitateDeactivation();
            await grain.Reactivate();
            await grain.PrecipitateDeactivation();
            await grain.Reactivate();
            await grain.PrecipitateDeactivation();
            await grain.Reactivate();

            await fx.Requests.WhenIdle();

            Assert.That(recorder.Activations, Has.Count.EqualTo(5));
            Assert.That(recorder.Deactivations, Has.Count.EqualTo(4));
        }





        [Test]
        public async Task DeactivatesWhenIdle()
        {
            var fx = new MockFixture();            
            fx.Types.Map<IReactivatable, Reactivatable>();

            var recorder = fx.Services.Inject(new ActivationRecorder());

            var grain = fx.GrainFactory.GetGrain<IReactivatable>(Guid.NewGuid());

            await grain.PrecipitateDeactivation();
            
            await fx.Requests.WhenIdle();

            Assert.That(recorder.Activations.Single(), Is.EqualTo(grain));
            Assert.That(recorder.Deactivations.Single(), Is.EqualTo(grain));            
        }

        

        
        public class ActivationRecorder
        {
            public ConcurrentBag<IGrain> Activations = new ConcurrentBag<IGrain>();
            public ConcurrentBag<IGrain> Deactivations = new ConcurrentBag<IGrain>();
        }


        

        public interface IReactivatable : IGrainWithGuidKey
        {
            Task Reactivate();
            Task PrecipitateDeactivation();
        }


        public class Reactivatable : Grain, IReactivatable
        {
            ActivationRecorder _recorder;

            public Reactivatable(ActivationRecorder recorder) {
                _recorder = recorder;
            }
            
            public Task Reactivate() {
                return Task.CompletedTask;
            }

            public Task PrecipitateDeactivation() {
                DeactivateOnIdle();
                return Task.CompletedTask;
            }

            public override Task OnActivateAsync() {
                _recorder.Activations.Add(this.CastAs<IReactivatable>()); 
                return Task.CompletedTask;
            }
            
            public override Task OnDeactivateAsync() {
                _recorder.Deactivations.Add(this.CastAs<IReactivatable>());
                return Task.CompletedTask;
            }

        }



    }
}
