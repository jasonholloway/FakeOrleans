using MockOrleans.Components;
using MockOrleans.Grains;
using NSubstitute;
using NUnit.Framework;
using Orleans;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockOrleans.Tests
{    
    
    [TestFixture]
    public class PlacementDispatcherTests
    {
        GrainPlacement _placement;
        Func<GrainPlacement, IActivationSite> _siteFac;
        PlacementDispatcher _disp;
        

        [SetUp]
        public void SetUp() {
            _placement = CreatePlacement();

            _siteFac = Substitute.For<Func<GrainPlacement, IActivationSite>>();
            _siteFac(Arg.Any<GrainPlacement>()).Returns(_ => Substitute.For<IActivationSite>());
            
            _disp = new PlacementDispatcher(_siteFac);
        }


        [Test]
        public async Task DefersToFactoryToCreateSite() 
        {            
            await _disp.Dispatch(_placement, g => Task.FromResult(true));
            
            _siteFac.Received(1)(Arg.Is(_placement));            
        }
                

        [Test]
        public async Task CachesSiteWhenCreated() 
        {
            for(int i = 0; i < 10; i++) {
                await _disp.Dispatch(_placement, g => Task.FromResult(true));
            }

            _siteFac.Received(1)(Arg.Is(_placement));            
        }

        
        [Test]
        public async Task DelegatesDispatchToSite() 
        {
            var site = Substitute.For<IActivationSite>();
            _siteFac(Arg.Is(_placement)).Returns(site);

            await _disp.Dispatch(_placement, g => Task.FromResult(true));
            
            await site.Received(1)
                    .Dispatch(Arg.Any<Func<IActivation, Task<bool>>>(), Arg.Any<RequestMode>());            
        }


        [Test]
        public async Task LeavesRequestModeUnspecified() 
        {
            var site = Substitute.For<IActivationSite>();
            _siteFac(Arg.Is(_placement)).Returns(site);

            await _disp.Dispatch(_placement, g => Task.FromResult(true));

            await site.Received(1)
                    .Dispatch(Arg.Any<Func<IActivation, Task<bool>>>(), Arg.Is(RequestMode.Unspecified));
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
                        await _disp.Dispatch(_placement, g => Task.FromResult(true));
                    })
                    .WhenAll();

            Assert.That(sites.Count(s => s.ReceivedCalls().Any()), Is.EqualTo(1));
        }

                

        public class TestGrain : Grain, IGrainWithGuidKey { }


        GrainPlacement CreatePlacement()
            => new GrainPlacement(new GrainKey(typeof(TestGrain), Guid.NewGuid()), null);

    }
}
