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
    public class ActivationDispatcherTests
    {

        #region etc
        
        IRequestRunner _runner;
        Grain _grain;
        
        Placement _placement;
        IGrainContext _ctx;
        ActivationDispatcher _disp;
        Func<Task<IGrainContext>> _ctxFac;

        Func<IGrainContext, Task<bool>> _fn;
        
        [SetUp]
        public void SetUp() 
        {
            _placement = new Placement(new ConcreteKey(typeof(Grain), Guid.NewGuid()));

            _grain = Substitute.For<Grain>();       //integration testing could even be automatically done by substituting mocks for realities.

            //_grainFac = Substitute.For<Func<IActivationDispatcher, Grain>>();
            //_grainFac(Arg.Any<IActivationDispatcher>()).Returns(_grain);
            
            var exceptionSink = new ExceptionSink();

            _runner = new RequestRunner(new GrainTaskScheduler(new FixtureScheduler(), exceptionSink), exceptionSink);
            
            _ctx = Substitute.For<IGrainContext>();
            _ctx.Grain.Returns(_grain);

            _ctxFac = Substitute.For<Func<Task<IGrainContext>>>();
            _ctxFac().Returns(_ctx);

            _disp = new ActivationDispatcher(_runner, _ctxFac);
            
            _fn = Substitute.For<Func<IGrainContext, Task<bool>>>();            
        }
        

        #endregion


        [Test]
        public async Task Perform_ProvidesGrainContextToDelegate() 
        {   
            var result = await _disp.Perform(ctx => Task.FromResult(ctx.Equals(_ctx)));

            Assert.That(result, Is.True);
        }
        

        [Test]
        public async Task Grain_EmplacedByFirstPerformance() 
        {   
            var grain = await _disp.Perform(a => Task.FromResult(a.Grain));

            Assert.That(grain, Is.EqualTo(_grain));
        }


        [Test]
        public async Task GrainContextCreation_OccursOnlyOnce() 
        {
            await Enumerable.Range(0, 100)
                    .Select(async _ => {
                        var grain = await _disp.Perform(a => Task.FromResult(a.Grain));
                        Assert.That(grain, Is.Not.Null);
                    })
                    .WhenAll();

            await _ctxFac.Received(1)();
        }
        

        [Test]
        public async Task Perform_ExecutesDelegate_AndPassesItContext() 
        {          
            await _disp.Perform(_fn);

            await _fn.Received(1)(Arg.Is<IGrainContext>(ctx => ctx != null));            
        }


        
        [Test]
        public async Task Perform_AfterDeactivation_ThrowsException()
        {
            await _disp.Perform(_ => Task.FromResult(true));

            await _disp.Deactivate();
            
            Assert.That(
                () => _disp.Perform(_ => Task.FromResult(true), RequestMode.Unspecified),
                Throws.Exception.InstanceOf<DeactivatedException>());            
        }

               

        [Test]
        public async Task Deactivate_CallsOnDeactivation() 
        {
            await _disp.Perform(_ => Task.FromResult(true));

            await _disp.Deactivate();
            
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
