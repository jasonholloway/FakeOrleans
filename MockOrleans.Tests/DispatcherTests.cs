using MockOrleans.Grains;
using NSubstitute;
using NUnit.Framework;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockOrleans.Tests
{


    public interface IActivationSiteFac
    {
        IActivationSite Create(GrainPlacement placement);
    }
    
    public interface IDispatcher
    {
        Task<TResult> Dispatch<TResult>(GrainPlacement placement, Func<Grain, Task<TResult>> fn);
    }




    public class Dispatcher : IDispatcher
    {
        readonly IActivationSiteFac _siteFac;
        readonly Dictionary<GrainPlacement, IActivationSite> _dSites;

        public Dispatcher(IActivationSiteFac siteFac) {
            _siteFac = siteFac;
            _dSites = new Dictionary<GrainPlacement, IActivationSite>();
        }


        public Task<TResult> Dispatch<TResult>(GrainPlacement placement, Func<Grain, Task<TResult>> fn) {            
            return Task.FromResult(default(TResult));
        }

    }



    

    [TestFixture]
    public class DispatcherTests
    {
        GrainPlacement _placement;
        IActivationSiteFac _siteFac;


        [SetUp]
        public void SetUp() {
            _placement = CreatePlacement();
            _siteFac = Substitute.For<IActivationSiteFac>();
        }


        [Test]
        public async Task DefersToFactoryToCreateSite() 
        {            
            var disp = new Dispatcher(_siteFac);

            var result = await disp.Dispatch(_placement, g => Task.FromResult(true));
            
            _siteFac.Create(Arg.Is(_placement)).Received(1);

            throw new NotImplementedException("above not working strangely...");
        }

        

        [Test]
        public async Task CachesSiteWhenCreated() 
        {
            var disp = new Dispatcher(_siteFac);

            for(int i = 0; i < 10; i++) {
                await disp.Dispatch(_placement, g => Task.FromResult(true));
            }

            _siteFac.Create(Arg.Is(_placement)).Received(1);

            throw new NotImplementedException("above not working strangely...");
        }


        [Test]
        public async Task CreatesSingleSiteUnderRaceConditions() {
            throw new NotImplementedException();
        }






        public class TestGrain : Grain, IGrainWithGuidKey { }


        GrainPlacement CreatePlacement()
            => new GrainPlacement(new GrainKey(typeof(TestGrain), Guid.NewGuid()), null);

    }
}
