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
    public class DispatcherTests : TestFixtureBase
    {
        AbstractKey _key;
        Placement _placement;
        Func<IGrainContext, Task<bool>> _fn;
        
        Func<AbstractKey, Placement> _placer;
        IPlacementDispatcher _innerDisp;
        
        IDispatcher _disp;


        [SetUp]
        public void SetUp() {
            _key = new AbstractKey(typeof(ITestGrain), Guid.NewGuid());
            _placement = new Placement(_key, typeof(TestGrain)); 

            _innerDisp = Substitute.For<IPlacementDispatcher>();

            _placer = Substitute.For<Func<AbstractKey, Placement>>();
            _placer(Arg.Is(_key)).Returns(_placement);

            _disp = new Dispatcher(_placer, _innerDisp);

            _fn = a => Task.FromResult(true);
        }

        
        [Test]
        public async Task RequestsPlacementFromPlacer() 
        {            
            await _disp.Dispatch(_key, _fn);

            _placer.Received(1)(Arg.Is(_key));
        }


        [Test]
        public async Task UsesPlacementInDispatchingInwardly() 
        {
            await _disp.Dispatch(_key, _fn);

            await _innerDisp.Received(1)
                    .Dispatch(Arg.Is(_placement), Arg.Is(_fn));            
        }
                
        
                
        
    }
}
