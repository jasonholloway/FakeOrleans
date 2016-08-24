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



    public interface IPlacer
    {

    }

    
    public interface IDispatcher
    {
        Task<TResult> Dispatch<TResult>(GrainKey key, Func<Grain, Task<TResult>> fn);
        Task<TResult> Dispatch<TResult>(GrainPlacement placement, Func<Grain, Task<TResult>> fn);
    }




    public class Dispatcher : IDispatcher
    {
        readonly IPlacer _placer;
        readonly Func<GrainPlacement, IActivationSite> _siteFac;
        readonly ConcurrentDictionary<GrainPlacement, IActivationSite> _dSites;

        public Dispatcher(IPlacer placer, Func<GrainPlacement, IActivationSite> siteFac) {
            _placer = placer;
            _siteFac = siteFac;
            _dSites = new ConcurrentDictionary<GrainPlacement, IActivationSite>();
        }


        public Task<TResult> Dispatch<TResult>(GrainPlacement placement, Func<Grain, Task<TResult>> fn) 
        {
            var site = _dSites.GetOrAdd(placement, p => _siteFac(p));
            
            return site.Dispatch(a => fn(a.Grain), RequestMode.Unspecified);
        }

        public Task<TResult> Dispatch<TResult>(GrainKey key, Func<Grain, Task<TResult>> fn) {
            throw new NotImplementedException();
        }

    }



    

    [TestFixture]
    public class DispatcherTests
    {
        GrainPlacement _placement;
        Func<GrainPlacement, IActivationSite> _siteFac;
        Dispatcher _disp;


        [SetUp]
        public void SetUp() {
            _placement = CreatePlacement();

            _siteFac = Substitute.For<Func<GrainPlacement, IActivationSite>>();
            _siteFac(Arg.Any<GrainPlacement>()).Returns(_ => Substitute.For<IActivationSite>());

            _disp = new Dispatcher(_siteFac);
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
