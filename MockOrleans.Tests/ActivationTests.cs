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
    [TestFixture]
    public class ActivationTests
    {

        #region etc

        IGrainCreator _creator;
        IRequestRunner _runner;
        IActivation _activation;
        Grain _grain;
        Func<IActivation, Task<bool>> _fn;
        
        [SetUp]
        public void SetUp() { //our expectations of others - but what enforces others' real implementations to fulfil these? Integration testing, obvs.
            _grain = Substitute.For<Grain>();       //integration testing could even be automatically done by substituting mocks for realities.
            
            _creator = Substitute.For<IGrainCreator>();
            _creator.Activate(Arg.Any<IActivation>()).Returns(_grain);
            
            _runner = Substitute.For<IRequestRunner>();
            _activation = new Activation(_creator, _runner);

            _fn = Substitute.For<Func<IActivation, Task<bool>>>();

            SetupRunner<Grain>();
            SetupRunner<bool>();
        }
        
        void SetupRunner<T>() 
        {
            _runner.Perform(Arg.Any<Func<Task<T>>>(), Arg.Any<RequestMode>())
                    .Returns(x => {
                        var fn = x.ArgAt<Func<Task<T>>>(0);
                        return fn();
                    });

            _runner
                .When(x => x.PerformAndClose(Arg.Any<Func<Task>>()))
                .Do(x => {
                    _runner
                        .When(y => y.Perform(Arg.Any<Func<Task<T>>>(), Arg.Any<RequestMode>()))
                        .Throw(new DeactivatedException());
                });

        }

        #endregion


        [Test]
        public async Task Performance_TakesActivationAsArg() 
        {   
            var result = await _activation.Perform(a => Task.FromResult(a.Equals(_activation)));

            Assert.That(result, Is.True);
        }


        [Test]
        public void Grain_OriginallyEmpty() {
            Assert.That(_activation.Grain, Is.Null);
        }


        [Test]
        public async Task Grain_EmplacedByFirstPerformance() 
        {   
            var grain = await _activation.Perform(a => Task.FromResult(a.Grain));

            Assert.That(grain, Is.EqualTo(_grain));
        }


        [Test]
        public async Task Activation_OccursOnlyOnce() 
        {
            _creator.Activate(Arg.Any<IActivation>())
                    .Returns(async _ => {
                            await Task.Delay(15);
                            return Substitute.For<Grain>();
                        });
            
            await Enumerable.Range(0, 100)
                    .Select(async _ => {
                        var grain = await _activation.Perform(a => Task.FromResult(a.Grain));
                        Assert.That(grain, Is.Not.Null);
                    })
                    .WhenAll();

            await _creator.Received(1)
                        .Activate(Arg.Any<IActivation>());
        }


        [Test]
        public async Task Activation_PerformedAsIsolated()
        {
            await _activation.Perform(_ => Task.FromResult(true));
            
            await _runner.Received(1)
                    .Perform(Arg.Any<Func<Task<Grain>>>(), Arg.Is(RequestMode.Isolated));
        }
        

        [Test]
        public async Task Performance_IsExecuted() 
        {          
            await _activation.Perform(_fn);

            await _fn.Received(1)(Arg.Is(_activation));            
        }


        
        [Test]
        public async Task Performance_AfterDeactivation_ThrowsException()
        {                                                    
            await _activation.Deactivate();

            Assert.That(
                () => _activation.Perform(_ => Task.FromResult(true), RequestMode.Unspecified),
                Throws.Exception.InstanceOf<DeactivatedException>());            
        }

               

        [Test]
        public async Task Deactivating_CallsOnDeactivation() 
        {
            await _activation.Deactivate();
            
            await _grain.Received(1).OnDeactivateAsync();
        }

        
    }

    
}
