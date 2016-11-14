using NSubstitute;
using NUnit.Framework;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeOrleans.Tests
{
    

    [TestFixture]
    public class IntegrationTests
    {

        [Test]
        public async Task BasicTypeResolutionAndDispatch() 
        {
            var fx = new Fixture(Substitute.For<IServiceProvider>());            

            fx.Types.Map<ITestGrain, TestGrain>();
            
            var grain = fx.GrainFactory.GetGrain<ITestGrain>(Guid.NewGuid());
            
            var result = await grain.SayHello();
                        
            Assert.That(result, Is.EqualTo("Hello"));
        }



        

        public interface ITestGrain : IGrainWithGuidKey
        {
            Task<string> SayHello();
        }



        public class TestGrain : Grain, ITestGrain
        {
            public Task<string> SayHello() {
                return Task.FromResult("Hello");
            }

            public bool Blah() => false;
        }



    }
}
