﻿using FakeOrleans.Components;
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
        GrainPlacement _placement;
        Func<GrainPlacement, IActivationSite> _siteFac;
        Func<GrainPlacement, IActivation> _actFac;
        ActivationHub _hub;
        

        [SetUp]
        public void SetUp() {
            _placement = CreatePlacement();

            _actFac = (_) => {
                var act = Substitute.For<IActivation>();

                act.Perform(Arg.Any<Func<IActivation, Task<IActivation>>>(), Arg.Any<RequestMode>())
                    .Returns(c => c.Arg<Func<IActivation, Task<IActivation>>>()(act));

                return act;
            };
            
            _siteFac = Substitute.For<Func<GrainPlacement, IActivationSite>>();
            _siteFac(Arg.Any<GrainPlacement>())
                .Returns(_ => new ActivationSite(_actFac)); //reliant on ActivationSite

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
                    .Dispatch(Arg.Any<Func<IActivation, Task<bool>>>(), Arg.Any<RequestMode>());            
        }


        [Test]
        public async Task Dispatch_LeavesRequestModeUnspecified() 
        {
            var site = Substitute.For<IActivationSite>();
            _siteFac(Arg.Is(_placement)).Returns(site);

            await _hub.Dispatch(_placement, g => Task.FromResult(true));

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
                        await _hub.Dispatch(_placement, g => Task.FromResult(true));
                    })
                    .WhenAll();

            Assert.That(sites.Count(s => s.ReceivedCalls().Any()), Is.EqualTo(1));
        }

                
        [Test]
        public async Task GetActivations_ReturnsAllCreatedActivations() 
        {
            var placements = Enumerable.Range(0, 100)
                                .Select(_ => CreatePlacement())
                                .ToArray();

            var createdActs = await placements
                                    .Select(p => _hub.Dispatch(p, a => Task.FromResult(a)))
                                    .WhenAll();

            var returnedActs = _hub.GetActivations();

            Assert.That(returnedActs, Is.EquivalentTo(createdActs));            
        }


        [Test]
        public async Task GetActivations_DoesntReturnDeactivated() 
        {
            

            throw new NotImplementedException();
        }

        
    }
}
