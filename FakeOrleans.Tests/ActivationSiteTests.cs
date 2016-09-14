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

        Func<IActivation, Task<Guid>> _fn = g => Task.FromResult(Guid.Empty);


        [Test]
        public async Task DispatchesToActivation() 
        {
            var guid = Guid.NewGuid();
            
            var activation = Substitute.For<IActivation>();
            activation.Perform(Arg.Is(_fn)).Returns(guid);
            
            var site = new ActivationSite(_ => activation);

            var result = await site.Dispatch(_fn);

            Assert.That(result, Is.EqualTo(guid));
        }

        
        
        [Test]
        public async Task ReactivatesWhenDeactivatedFound() 
        {
            var expectedReturnVal = Guid.NewGuid();
            var placement = new GrainPlacement(new GrainKey(typeof(Grain), Guid.NewGuid()));

            var deadActivation = Substitute.For<IActivation>();
            deadActivation
                .When(x => x.Perform(Arg.Is(_fn)))
                .Do(_ => { throw new DeactivatedException(); });

            var goodActivation = Substitute.For<IActivation>();
            goodActivation.Perform(Arg.Is(_fn)).Returns(expectedReturnVal);
            
            var actCreator = Substitute.For<Func<GrainPlacement, IActivation>>();
            actCreator(Arg.Is(placement)).Returns(deadActivation, goodActivation);
            
            var site = new ActivationSite(actCreator);
            site.Init(placement);

            var result = await site.Dispatch(_fn);

            Assert.That(result, Is.EqualTo(expectedReturnVal));
        }




        [Test]
        public async Task SameActivationUsedIfGood() 
        {
            var actCreator = new Func<GrainPlacement, IActivation>(p => {
                                        var act = Substitute.For<IActivation>();
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
            var actCreator = new Func<GrainPlacement, IActivation>(p => {
                                    var act = Substitute.For<IActivation>();
                                    act.Perform(Arg.Is(_fn)).Returns(Guid.NewGuid());
                                    return act;
                                });

            var site = new ActivationSite(actCreator);

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
