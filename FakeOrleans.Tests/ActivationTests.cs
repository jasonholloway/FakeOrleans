using FakeOrleans.Grains;
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

namespace FakeOrleans.Tests
{
    [TestFixture]
    public class ActivationTests
    {

        #region etc

        Func<IActivation, Grain> _grainFac;
        IRequestRunner _runner;
        Activation _activation;
        Grain _grain;
        GrainPlacement _placement;

        Func<IActivation, Task<bool>> _fn;
        
        [SetUp]
        public void SetUp() { //our expectations of others - but what enforces others' real implementations to fulfil these? Integration testing, obvs.

            _placement = new GrainPlacement(new GrainKey(typeof(Grain), Guid.NewGuid()));

            _grain = Substitute.For<Grain>();       //integration testing could even be automatically done by substituting mocks for realities.

            _grainFac = Substitute.For<Func<IActivation, Grain>>();
            _grainFac(Arg.Any<IActivation>()).Returns(_grain);
            
            var exceptionSink = new ExceptionSink();

            _runner = new RequestRunner(new GrainTaskScheduler(new FixtureScheduler(), exceptionSink), exceptionSink);
          
            _activation = new Activation(_placement, _runner, _grainFac);

            _fn = Substitute.For<Func<IActivation, Task<bool>>>();            
        }
        

        #endregion


        [Test]
        public async Task Perform_ProvidesActivationToDelegate() 
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
            _grainFac(Arg.Any<IActivation>())
                    .Returns(_ => Substitute.For<Grain>());
            
            await Enumerable.Range(0, 100)
                    .Select(async _ => {
                        var grain = await _activation.Perform(a => Task.FromResult(a.Grain));
                        Assert.That(grain, Is.Not.Null);
                    })
                    .WhenAll();

            _grainFac.Received(1)(Arg.Any<IActivation>());
        }
        

        [Test]
        public async Task Perform_ExecutesDelegate() 
        {          
            await _activation.Perform(_fn);

            await _fn.Received(1)(Arg.Is(_activation));            
        }


        
        [Test]
        public async Task Perform_AfterDeactivation_ThrowsException()
        {
            await _activation.Perform(_ => Task.FromResult(true));

            await _activation.Deactivate();
            
            Assert.That(
                () => _activation.Perform(_ => Task.FromResult(true), RequestMode.Unspecified),
                Throws.Exception.InstanceOf<DeactivatedException>());            
        }

               

        [Test]
        public async Task Deactivate_CallsOnDeactivation() 
        {
            await _activation.Perform(_ => Task.FromResult(true));

            await _activation.Deactivate();
            
            await _runner.WhenIdle();
             
            await _grain.Received(1).OnDeactivateAsync();
        }

        

        [Test]
        public async Task Deactivate_CompletesWhenDeactivated() {
            _activation.Perform(_ => Task.Delay(500).ContinueWith(t => true)).Ignore();

            await _activation.Deactivate();

            Assert.That(_activation.Status, Is.EqualTo(ActivationStatus.Deactivated));
        }



    }

    
}
