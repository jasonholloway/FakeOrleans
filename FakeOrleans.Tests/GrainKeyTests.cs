using FakeOrleans;
using FakeOrleans.Grains;
using NSubstitute;
using NUnit.Framework;
using Orleans;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeOrleans.Tests
{
    [TestFixture]
    public class GrainKeyTests
    {

        #region stuff

        Fixture _fx;

        [SetUp]
        public void SetUp() {
            _fx = new Fixture();
        }


        public interface ISomeGrain : IGrainWithGuidKey
        {
            
        }

        public class SomeGrain : ISomeGrain
        {
            
        }

        #endregion
        

        [Test]
        public async Task GrainKey_ObtainableFromConcreteGrain() 
        {
            var key = new ConcreteKey(typeof(SomeGrain), Guid.NewGuid());

            var grain = await GrainConstructor.New(
                                    key, 
                                    Substitute.For<IGrainRuntime>(),
                                    Substitute.For<IServiceProvider>(), 
                                    Substitute.For<StorageCell>(),
                                    new FakeSerializer(_ => null)
                                    );
            
            var foundKey = grain.GetGrainKey();

            Assert.That(foundKey, Is.EqualTo(key));
        }


        [Test]
        public void GrainKey_ObtainableFromGrainProxy() 
        {
            var key = new AbstractKey(typeof(ISomeGrain), Guid.NewGuid());

            var proxy = (ISomeGrain)GrainProxy.Proxify(_fx, key);
            
            var foundKey = proxy.GetGrainKey();

            Assert.That(foundKey, Is.EqualTo(key));
        }


    }
}
