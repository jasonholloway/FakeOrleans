using FakeOrleans.Grains;
using NSubstitute;
using NUnit.Framework;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FakeOrleans.Tests
{

    [TestFixture]
    public class ActivationSiteTests
    {

        Func<IGrainContext, Task<Guid>> _fn = g => Task.FromResult(Guid.Empty);


        [Test]
        public async Task DispatchesToActivationDispatcher() 
        {
            var guid = Guid.NewGuid();
            
            var disp = Substitute.For<IActivationDispatcher>();
            disp.Perform(Arg.Is(_fn)).Returns(guid);
            
            var site = new ActivationSite(_ => disp); //in reality, passed factory would summon entire activation

            var result = await site.Dispatch(_fn);

            Assert.That(result, Is.EqualTo(guid));
        }

        
        
        [Test]
        public async Task RecreatesDispatcherWhenDeactivatedFound()   //does ActivationSite really need the acivation itself, or just its dispatcher?
        {
            var expectedReturnVal = Guid.NewGuid();
            var placement = new Placement(new ConcreteKey(typeof(Grain), Guid.NewGuid()));

            var deadDisp = Substitute.For<IActivationDispatcher>();
            deadDisp.When(x => x.Perform(Arg.Is(_fn)))
                    .Do(_ => { throw new DeactivatedException(); });

            var goodDisp = Substitute.For<IActivationDispatcher>();
            goodDisp.Perform(Arg.Is(_fn)).Returns(expectedReturnVal);
            
            var dispCreator = Substitute.For<Func<Placement, IActivationDispatcher>>();
            dispCreator(Arg.Is(placement)).Returns(deadDisp, goodDisp);
            
            var site = new ActivationSite(dispCreator);
            site.Init(placement);

            var result = await site.Dispatch(_fn);

            Assert.That(result, Is.EqualTo(expectedReturnVal));
        }




        [Test]
        public async Task SameDispatcherUsedIfGood() 
        {
            var actCreator = new Func<Placement, IActivationDispatcher>(p => {
                                        var act = Substitute.For<IActivationDispatcher>();
                                        act.Perform(Arg.Is(_fn)).Returns(Guid.NewGuid());
                                        return act;
                                    });
            
            var site = new ActivationSite(actCreator);

            var result1 =  await site.Dispatch(_fn);
            var result2 = await site.Dispatch(_fn);
            
            Assert.That(result1, Is.EqualTo(result2));
        }





        [Test]
        public async Task ReactivatesOneAtATimeViaLock() 
        {
            var dispCreator = new Func<Placement, IActivationDispatcher>(p => {
                                    var act = Substitute.For<IActivationDispatcher>();
                                    act.Perform(Arg.Is(_fn)).Returns(Guid.NewGuid());
                                    return act;
                                });

            var site = new ActivationSite(dispCreator);

            var results = await Enumerable.Range(0, 1000)
                                    .Select(async _ => {
                                        await Task.Delay(15);
                                        return await site.Dispatch(_fn);
                                        })
                                    .WhenAll();

            Assert.That(results, Is.All.EqualTo(results.First()));
        }



    }
}
