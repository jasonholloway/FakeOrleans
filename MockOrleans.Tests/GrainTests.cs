using MockOrleans;
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

        

        //In Orleans proper activation is done as part of creating the activation
        //so the activation itself doesn't have to bother with the activation logic

        //instead, it's part of creating the GrainHarness: when you request the endpoint,
        //then a locked activation is carried out. From this point on the harness doesn't care.


        [Test]
        public async Task ReentrantGrainsEnsureOnlyOneActivationRoutine() 
        {
            var fx = new MockFixture();
            fx.Types.Map<IReentrantActivator, ReentrantActivator>();
            var qActivations = fx.Services.Inject(new ConcurrentQueue<int>());

            var grain = fx.GrainFactory.GetGrain<IReentrantActivator>(Guid.NewGuid());
            
            await Enumerable.Range(0, 10).Select(_ => grain.Hello()).WhenAll();

            Assert.That(qActivations.Count, Is.EqualTo(1));
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
        public async Task DeactivatesWhenIdle()
        {
            var fx = new MockFixture(Substitute.For<IServiceProvider>());            
            fx.Types.Map<IDeactivatable, Deactivatable>();
            fx.Types.Map<IDeactivationRecorder, DeactivationRecorder>();
                        
            var recorder = fx.GrainFactory.GetGrain<IDeactivationRecorder>(Guid.Empty);

            var deactivatable = fx.GrainFactory.GetGrain<IDeactivatable>(Guid.NewGuid());
            await deactivatable.SetRecorder(recorder);

            await deactivatable.PrecipitateDeactivation();

            await Task.Delay(15);

            await fx.Requests.WhenIdle();
            
            //assert is deactivated here
            //Assert.That(fx.Silo.IsActive(grain), Is.False);
            
            var deactivated = await recorder.IsDeactivated();
            Assert.That(deactivated, Is.True);            
        }

        


        public interface IDeactivationRecorder : IGrainWithGuidKey
        {
            Task SetDeactivated();
            Task<bool> IsDeactivated();
        }

        public class DeactivationRecorder : Grain, IDeactivationRecorder
        {
            bool _deactivated;

            public Task<bool> IsDeactivated() {
                return Task.FromResult(_deactivated);
            }

            public Task SetDeactivated() {
                _deactivated = true;
                return Task.CompletedTask;
            }
        }






        public interface IDeactivatable : IGrainWithGuidKey
        {
            Task SetRecorder(IDeactivationRecorder recorder);
            Task PrecipitateDeactivation();
        }


        public class Deactivatable : Grain, IDeactivatable
        {
            IDeactivationRecorder _recorder;

            public Task SetRecorder(IDeactivationRecorder recorder) {
                _recorder = recorder;
                return Task.CompletedTask;
            }
            
            public Task PrecipitateDeactivation() {
                DeactivateOnIdle();
                return Task.CompletedTask;
            }

            public override Task OnDeactivateAsync() {
                return _recorder.SetDeactivated();
            }

        }



    }
}
