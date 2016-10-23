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

        Func<IActivationDispatcher, Grain> _grainFac;
        IRequestRunner _runner;
        Activation_New _activation;
        Grain _grain;
        Placement _placement;

        Func<IGrainContext, Task<bool>> _fn;
        
        [SetUp]
        public void SetUp() { //our expectations of others - but what enforces others' real implementations to fulfil these? Integration testing, obvs.

            _placement = new Placement(new ConcreteKey(typeof(Grain), Guid.NewGuid()));

            _grain = Substitute.For<Grain>();       //integration testing could even be automatically done by substituting mocks for realities.

            _grainFac = Substitute.For<Func<IActivationDispatcher, Grain>>();
            _grainFac(Arg.Any<IActivationDispatcher>()).Returns(_grain);
            
            var exceptionSink = new ExceptionSink();

            _runner = new RequestRunner(new GrainTaskScheduler(new FixtureScheduler(), exceptionSink), exceptionSink);

            _activation = new Activation_New(null, _placement); //, _runner, _grainFac);

            _fn = Substitute.For<Func<IGrainContext, Task<bool>>>();            
        }
        

        #endregion


        [Test]
        public async Task Perform_ProvidesActivationToDelegate() 
        {   
            var result = await _activation.Dispatcher.Perform(a => Task.FromResult(a.Equals(_activation)));

            Assert.That(result, Is.True);
        }


        //[Test]                                    Grain is never accessible before its creation...
        //public void Grain_OriginallyEmpty() {
        //    Assert.That(_activation.Grain, Is.Null);
        //}


        [Test]
        public async Task Grain_EmplacedByFirstPerformance() 
        {   
            var grain = await _activation.Dispatcher.Perform(a => Task.FromResult(a.Grain));

            Assert.That(grain, Is.EqualTo(_grain));
        }


        [Test]
        public async Task Activation_OccursOnlyOnce() 
        {
            _grainFac(Arg.Any<IActivationDispatcher>())
                    .Returns(_ => Substitute.For<Grain>());
            
            await Enumerable.Range(0, 100)
                    .Select(async _ => {
                        var grain = await _activation.Dispatcher.Perform(a => Task.FromResult(a.Grain));
                        Assert.That(grain, Is.Not.Null);
                    })
                    .WhenAll();

            _grainFac.Received(1)(Arg.Any<IActivationDispatcher>());
        }
        

        [Test]
        public async Task Perform_ExecutesDelegate_AndPassesItContext() 
        {          
            await _activation.Dispatcher.Perform(_fn);

            await _fn.Received(1)(Arg.Is<IGrainContext>(ctx => ctx != null));            
        }


        
        [Test]
        public async Task Perform_AfterDeactivation_ThrowsException()
        {
            await _activation.Dispatcher.Perform(_ => Task.FromResult(true));

            await _activation.Dispatcher.Deactivate();
            
            Assert.That(
                () => _activation.Dispatcher.Perform(_ => Task.FromResult(true), RequestMode.Unspecified),
                Throws.Exception.InstanceOf<DeactivatedException>());            
        }

               

        [Test]
        public async Task Deactivate_CallsOnDeactivation() 
        {
            await _activation.Dispatcher.Perform(_ => Task.FromResult(true));

            await _activation.Dispatcher.Deactivate();
            
            await _runner.WhenIdle();
             
            await _grain.Received(1).OnDeactivateAsync();
        }

        

        ////what's the below supposed to do? 
        ////change status after deactivation... though activations no longer track this.

        //[Test]
        //public async Task Deactivate_CompletesWhenDeactivated() 
        //{
        //    _activation.Dispatcher.Perform(_ => Task.Delay(500).ContinueWith(t => true)).Ignore();

        //    await _activation.Dispatcher.Deactivate();

        //    Assert.That(_activation.Dispatcher.Status, Is.EqualTo(ActivationStatus.Deactivated));
        //}



    }

    
}
