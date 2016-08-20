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
        public async Task NonReentrantGrainIsolatesRequests() 
        {
            var fx = new MockFixture();
            fx.Types.Map<ICallCounter, CallCounter>();
            
            var callCounts = fx.Services.Inject(new List<int>());

            var grain = fx.GrainFactory.GetGrain<ICallCounter>(Guid.NewGuid());
            
            await Enumerable.Range(0, 50)
                            .Select(_ => grain.Yap())
                            .WhenAll();

            Assert.That(callCounts.All(c => c == 1));
        }

        

        public interface ICallCounter : IGrainWithGuidKey
        {
            Task Yap();
        }


        public class CallCounter : Grain, ICallCounter
        {
            int _callCount = 0;

            List<int> _callCounts;

            public CallCounter(List<int> callCounts) {
                _callCounts = callCounts;
            }
            
            public async Task Yap() {
                _callCount++;
                _callCounts.Add(_callCount);
                await Task.Delay(15);
                _callCount--;
            }            
        }









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

            await fx.Requests.WhenIdle();

            await grain.Reactivate();

            Assert.That(recorder.Activations, Has.Count.EqualTo(2));
            Assert.That(recorder.Deactivations, Has.Count.EqualTo(1));
        }




        volatile bool _fireFlak;

        [Test]
        public async Task DeactivationReactivationCompetition() 
        {
            var fx = new MockFixture();
            fx.Types.Map<IReactivatable, Reactivatable>();

            var recorder = fx.Services.Inject(new ActivationRecorder());

            var grain = fx.GrainFactory.GetGrain<IReactivatable>(Guid.NewGuid());
            
            _fireFlak = true;
            
            var tFlak = Task.Run(async () => {
                while(_fireFlak) {
                    await Enumerable.Range(0, 20)
                                .Select(_ => grain.Reactivate())
                                .WhenAll();
                }
            });

            for(int i = 0; i < 100; i++) {
                await grain.PrecipitateDeactivation();
                await grain.Reactivate();
            }
            
            _fireFlak = false;
            await tFlak;
            
            await fx.Requests.WhenIdle();

            fx.Exceptions.Rethrow();

            Assert.That(recorder.Activations, Has.Count.EqualTo(101));
            Assert.That(recorder.Deactivations, Has.Count.EqualTo(100));
            
            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            //PROBLEM: GrainRegistry isn't catching exceptions and reactivating...
            //... will probably want special exception to propagate, so as to be precisely catchable
            
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
            public ConcurrentBag<IGrain> Calls = new ConcurrentBag<IGrain>();
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
                _recorder.Calls.Add(this.CastAs<IReactivatable>());
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

        





        [Test]
        public async Task ReentrantGrainsRespectSingleOnDeactivationRequest() 
        {
            var fx = new MockFixture();
            fx.Types.Map<IReentrantDeactivator, ReentrantDeactivator>();

            var callCounts = fx.Services.Inject(new List<int>());
            
            var grain = fx.GrainFactory.GetGrain<IReentrantDeactivator>(Guid.NewGuid());

            await grain.Deactivate();

            for(int i = 0; i< 30; i++) {
                await grain.Yap();
            }
                        
            Assert.That(callCounts, Has.None.GreaterThan(0));
        }

        

        public interface IReentrantDeactivator : IGrainWithGuidKey
        {
            Task Yap();
            Task Deactivate();
        }


        public class ReentrantDeactivator : Grain, IReentrantDeactivator
        {
            int _callCount = 0;

            List<int> _callCounts;

            public ReentrantDeactivator(List<int> callCounts) {
                _callCounts = callCounts;
            }


            public async Task Yap() {
                _callCount++;
                await Task.Delay(15);
                _callCount--;
            }

            public Task Deactivate() {
                this.DeactivateOnIdle();
                return Task.CompletedTask;
            }

            public override async Task OnDeactivateAsync() {
                for(int i = 0; i < 30; i++) {
                    _callCounts.Add(_callCount);                    
                    await Task.Delay(15);                    
                }                
            }
            
        }

        



        [Test]
        public async Task SamePlacementGetsSameActivation() //though same key may get diff placements
        {
            var fx = new MockFixture();
            fx.Types.Map<IEmptyGrain, EmptyGrain>();
            
            var key = new GrainKey(typeof(EmptyGrain), Guid.NewGuid());
            var placement = fx.Grains.GetPlacement(key);
                        
            var activations = await Enumerable.Range(0, 50)
                                                .Select(_ => fx.Grains.GetActivation(placement))
                                                .WhenAll();
            
            Assert.That(activations.All(a => a == activations.First()));
        }

        

        public interface IEmptyGrain : IGrainWithGuidKey
        { }

        public class EmptyGrain : Grain, IEmptyGrain
        { }





              
        [Test]
        public async Task GrainProxiesPassableAsArgs() 
        {
            var fx = new MockFixture();
            fx.Types.Map<IProxyPasser, ProxyPasser>();
            
            var grain1 = fx.GrainFactory.GetGrain<IProxyPasser>(Guid.NewGuid());
            var grain2 = fx.GrainFactory.GetGrain<IProxyPasser>(Guid.NewGuid());
            
            var result = await grain1.Intermediate(grain2);
            
            Assert.That(result, Is.EqualTo(13));
        }
    



        public interface IProxyPasser : IGrainWithGuidKey
        {
            Task<int> Intermediate(IProxyPasser other);
            Task<int> Source();
        }

        public class ProxyPasser : Grain, IProxyPasser
        {
            public Task<int> Intermediate(IProxyPasser other) {
                return other.Source();
            }

            public Task<int> Source() {
                return Task.FromResult(13);
            }
        }



    }
}
