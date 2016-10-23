using FakeOrleans.Components;
using FakeOrleans.Grains;
using NSubstitute;
using NUnit.Framework;
using Orleans;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeOrleans.Tests
{   

    [TestFixture]
    public class ActivationHubTests : TestFixtureBase
    {
        Placement _placement;
        Func<Placement, IActivationSite> _siteFac;
        Func<Placement, IActivationDispatcher> _dispFac;
        ActivationHub _hub;
        

        [SetUp]
        public void SetUp() {
            _placement = CreatePlacement();

            _dispFac = (_) => {
                //var act = Substitute.For<IActivation>();
                var disp = Substitute.For<IActivationDispatcher>();

                //act.Dispatcher.Returns(disp);

                disp.Perform(Arg.Any<Func<IGrainContext, Task<IActivation>>>(), Arg.Any<RequestMode>())
                    .Returns(c => c.Arg<Func<IGrainContext, Task<IActivation>>>()(null)); //NEED CONTEXT SPECIFYING!!!

                return disp;
                //return act;
            };
            
            _siteFac = Substitute.For<Func<Placement, IActivationSite>>();
            _siteFac(Arg.Any<Placement>())
                .Returns(_ => new ActivationSite(_dispFac)); //reliant on ActivationSite

            _hub = new ActivationHub(_siteFac);
        }


        [Test]
        public async Task ActivationSites_CreatedByPassedFactory() 
        {            
            await _hub.Dispatch(_placement, g => Task.FromResult(true));
            
            _siteFac.Received(1)(Arg.Is(_placement));            
        }
                

        [Test]
        public async Task ActivationSites_CachedOnceCreated() 
        {
            for(int i = 0; i < 10; i++) {
                await _hub.Dispatch(_placement, g => Task.FromResult(true));
            }

            _siteFac.Received(1)(Arg.Is(_placement));            
        }
        
        
        [Test]
        public async Task Dispatch_DelegatesToRelevantActivationSite() 
        {
            var site = Substitute.For<IActivationSite>();
            _siteFac(Arg.Is(_placement)).Returns(site);

            await _hub.Dispatch(_placement, g => Task.FromResult(true));
            
            await site.Received(1)
                    .Dispatch(Arg.Any<Func<IGrainContext, Task<bool>>>(), Arg.Any<RequestMode>());            
        }


        [Test]
        public async Task Dispatch_LeavesRequestModeUnspecified() 
        {
            var site = Substitute.For<IActivationSite>();
            _siteFac(Arg.Is(_placement)).Returns(site);

            await _hub.Dispatch(_placement, g => Task.FromResult(true));

            await site.Received(1)
                    .Dispatch(Arg.Any<Func<IGrainContext, Task<bool>>>(), Arg.Is(RequestMode.Unspecified));
        }

        
        [Test]
        public async Task UsesSingleSiteUnderRaceConditions() 
        {
            var sites = new ConcurrentBag<IActivationSite>();

            _siteFac(Arg.Is(_placement)).Returns(_ => {
                                            var site = Substitute.For<IActivationSite>();
                                            sites.Add(site);
                                            return site;
                                        });

            await Enumerable.Range(0, 100)
                    .Select(async _ => {
                        await Task.Delay(15);
                        await _hub.Dispatch(_placement, g => Task.FromResult(true));
                    })
                    .WhenAll();

            Assert.That(sites.Count(s => s.ReceivedCalls().Any()), Is.EqualTo(1));
        }

                
        //[Test]
        //public async Task GetActivations_ReturnsAllCreatedActivations() 
        //{
        //    var createdActs = await Enumerable.Range(0, 100)
        //                            .Select(_ => _hub.Dispatch(CreatePlacement(), a => Task.FromResult(a)))
        //                            .WhenAll();

        //    var returnedActs = _hub.GetActivations();

        //    Assert.That(returnedActs, Is.EquivalentTo(createdActs));            
        //}


        //[Test]
        //public async Task GetActivations_DoesntReturnDeactivated() 
        //{
        //    Assert.Ignore("How to deactivate grains from outside?");

        //    //var createdActs = await Enumerable.Range(0, 100)
        //    //                        .Select(_ => _hub.Dispatch(CreatePlacement(), a => Task.FromResult(a)))
        //    //                        .WhenAll();

        //    //var killedActs = createdActs.Skip(30).Take(20).ToArray();
        //    //await Task.WhenAll(killedActs.Select(c => c.Dispatcher.Deactivate()));
                        
        //    ////may need to wait here...

        //    //var returnedActs = _hub.GetActivations();

        //    //Assert.That(returnedActs, Is.EquivalentTo(createdActs.Except(killedActs)));
        //}

        
    }
}
