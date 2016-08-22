using MockOrleans.Grains;
using NSubstitute;
using NUnit.Framework;
using Orleans;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MockOrleans.Tests
{

    //will take IAc

    public interface IGrainCreator
    {
        Task<Grain> Activate(IActivation act);
    }


    public interface IRequestRunner
    {
        Task Perform(Func<Task> fn, RequestMode mode);
        Task<T> Perform<T>(Func<Task<T>> fn, RequestMode mode);
        void PerformAndForget(Func<Task> fn, RequestMode mode);
        void PerformAndClose(Func<Task> fn, RequestMode mode);
        Task WhenIdle();
    }




    public class Activation : IActivation
    {
        readonly IGrainCreator _creator;
        readonly IRequestRunner _runner;

        public Activation(IGrainCreator creator, IRequestRunner runner) {
            _creator = creator;
            _runner = runner;
        }
        

        Grain _grain = null;

        public Grain Grain {
            get { return _grain; }
        }
        

        SemaphoreSlim _sm = new SemaphoreSlim(1);

        public async Task<TResult> Perform<TResult>(Func<IActivation, Task<TResult>> fn, RequestMode mode = RequestMode.Unspecified) 
        {
            await _sm.WaitAsync();

            try {
                if(_grain == null) {
                    _grain = await _runner.Perform(() => _creator.Activate(this), RequestMode.Isolated);
                }
            }
            finally {
                _sm.Release();
            }
            
            return await _runner.Perform(() => fn(this), mode);
        }
    }


    [TestFixture]
    public class ActivationTests
    {
        IGrainCreator _creator;
        IRequestRunner _runner;

        
        [SetUp]
        public void SetUp() {
            _creator = Substitute.For<IGrainCreator>();
            _runner = Substitute.For<IRequestRunner>();
        }


        void SetupRunner<T>() {
            _runner.Perform(Arg.Any<Func<Task<T>>>(), Arg.Any<RequestMode>())
                    .Returns(x => {
                        var fn = x.ArgAt<Func<Task<T>>>(0);
                        return fn();
                    });            
        }



        [Test]
        public async Task PassesItselfInPerformance() 
        {
            SetupRunner<Grain>();
            SetupRunner<bool>();
            
            var act = new Activation(_creator, _runner);

            var result = await act.Perform(a => Task.FromResult(a.Equals(act)));

            Assert.That(result, Is.True);
        }


        [Test]
        public async Task EmplacesGrainBeforeFirstPerformance() 
        {
            SetupRunner<Grain>();

            _creator.Activate(Arg.Any<IActivation>()).Returns(Substitute.For<Grain>());
            
            var act = new Activation(_creator, _runner);

            var grain = await act.Perform(a => Task.FromResult(a.Grain));

            Assert.That(grain, Is.Not.Null);
        }


        [Test]
        public async Task LockLimitsActivation() 
        {
            int actCount = 0;

            SetupRunner<Grain>();
            
            _creator.Activate(Arg.Any<IActivation>())
                    .Returns(async _ => {
                        int c = Interlocked.Increment(ref actCount);
                        Assert.That(c, Is.EqualTo(1));

                        try {
                            await Task.Delay(15);
                            return Substitute.For<Grain>();
                        }
                        finally {
                            Interlocked.Decrement(ref actCount);
                        }
                    });

            var act = new Activation(_creator, _runner);
            
            await Enumerable.Range(0, 100)
                    .Select(async _ => {
                        var grain = await act.Perform(a => Task.FromResult(a.Grain));
                        Assert.That(grain, Is.Not.Null);
                    })
                    .WhenAll();
        }


        [Test]
        public async Task GrainActivationDoneViaRunner() 
        {
            int reqCount = 0;

            _runner.Perform(Arg.Any<Func<Task<Grain>>>(), Arg.Is(RequestMode.Isolated))
                .Returns(x => {
                    reqCount++;
                    var fn = x.ArgAt<Func<Task<Grain>>>(0);                                        
                    return fn();
                });
                    
            var act = new Activation(_creator, _runner);
            
            await act.Perform(_ => Task.FromResult(true));

            Assert.That(reqCount, Is.EqualTo(1));            
        }



        [Test]
        public async Task PerformanceDoneViaRunner() 
        {
            int reqCount = 0;

            _runner.Perform(Arg.Any<Func<Task<bool>>>(), Arg.Any<RequestMode>())
                    .Returns(x => {
                        reqCount++;
                        var fn = x.ArgAt<Func<Task>>(0);
                        return fn();
                    });

            var act = new Activation(_creator, _runner);

            await act.Perform(_ => Task.FromResult(true));

            Assert.That(reqCount, Is.EqualTo(1));
        }



    }







    [TestFixture]
    public class ActivationIntegrationTests
    {
        
        [Test]
        public async Task ActivationOccursOnFirstResolutionOfPlacement() 
        {
            var fx = new MockFixture();
            fx.Types.Map<IActivatable, Activatable>();
            
            var tallies = fx.Services.Inject(new ConcurrentBag<int>());
            var delay = fx.Services.Inject(0);

            var placement = fx.Grains.GetPlacement<IActivatable>(Guid.NewGuid());
            
            var activation = await fx.Grains.GetActivation(placement);
            
            Assert.That(tallies, Has.Count.EqualTo(1));
        }




        public interface IActivatable : IGrainWithGuidKey
        { }

        public class Activatable : Grain, IActivatable
        {
            ConcurrentBag<int> _tallies;
            int _delay;

            public Activatable(ConcurrentBag<int> tallies, int delay) {
                _tallies = tallies;
                _delay = delay;
            }

            public override async Task OnActivateAsync() {
                await Task.Delay(_delay);
                _tallies.Add(1);
            }

        }





        [Test]
        public async Task NewActivationOccursDirectlyAfterDeactivation() 
        {
            throw new NotImplementedException();
        }



    }
}
