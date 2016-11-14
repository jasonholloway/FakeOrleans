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

        public class SomeGrain : Grain, ISomeGrain
        {
            
        }

        #endregion
        

        [Test]
        public async Task GrainKey_ObtainableFromConcreteGrain() 
        {
            var key = new AbstractKey(typeof(ISomeGrain), Guid.NewGuid());

            var grain = await GrainConstructor.New(
                                    typeof(SomeGrain),
                                    key,
                                    Substitute.For<IGrainRuntime>(),
                                    Substitute.For<IServiceProvider>(), 
                                    new StorageCell(null),
                                    null
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


        [Test]
        public void AbstractKey_ToString_RoundTrip() 
        {
            var original = new AbstractKey(typeof(ISomeGrain), Guid.NewGuid());

            var stringified = AbstractKey.Stringify(original);

            var returned = AbstractKey.Parse(stringified);

            Assert.That(returned, Is.EqualTo(original));
        }



    }
}
