using NUnit.Framework;
using Orleans;
using Orleans.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockOrleans.Tests
{
    [TestFixture]
    public class StoreTests
    {       
        

        [Test]
        public async Task GrainPersistsToStorage() 
        {
            var fx = new MockFixture();
            fx.Types.Map<IDogStorer, DogStorer>();
            fx.Services.Inject(new Dog()); //needed
            
            var grain = fx.GrainFactory.GetGrain<IDogStorer>(Guid.NewGuid());
            
            await grain.Write(new Dog("Kevin"));
            
            var store = fx.Stores[grain.GetGrainKey()];
            var state = store.State;
            
            Assert.That(state, Is.Not.Null);
            Assert.That(state, Is.InstanceOf<Dog>());
            Assert.That(((Dog)state).Name, Is.EqualTo("Kevin"));
        }



        [Test]
        public async Task GrainReadsFromStorage() 
        {
            var fx = new MockFixture();
            fx.Types.Map<IDogStorer, DogStorer>();
            fx.Services.Inject(new Dog()); //needed

            var grain = fx.GrainFactory.GetGrain<IDogStorer>(Guid.NewGuid());

            var store = fx.Stores[grain.GetGrainKey()];
            store.Update(new Dog("Geoffrey"));

            var result = await grain.Read();
            
            Assert.That(result.Name, Is.EqualTo("Geoffrey"));
        }


        
        [Test]
        public async Task StateGoesViaSerializerOnStorage() 
        {
            var fx = new MockFixture();
            fx.Types.Map<IDogStorer, DogStorer>();

            var injected = fx.Services.Inject(new Dog("Flump"));

            var grain = fx.GrainFactory.GetGrain<IDogStorer>(Guid.NewGuid());

            await grain.WriteInjected();

            var store = fx.Stores[grain.GetGrainKey()];
            var state = (Dog)store.State;
            
            Assert.That(state.Name, Is.EqualTo(injected.Name));
            Assert.That(state, Is.Not.EqualTo(injected));
        }

        

        public interface IDogStorer : IGrainWithGuidKey
        {
            Task<Dog> Read();
            Task Write(Dog i);
            Task WriteInjected();
        }


        public class DogStorer : Grain<Dog>, IDogStorer
        {
            Dog _injected;
                        
            public DogStorer(Dog dog) {
                _injected = dog;
            }

            public async Task<Dog> Read() {
                await ReadStateAsync();
                return State;
            }

            public Task Write(Dog dog) {
                State = dog;
                return WriteStateAsync();
            }

            public Task WriteInjected() {
                State = _injected;
                return WriteStateAsync();
            }
        }


        [Serializable]
        public class Dog
        {
            public string Name { get; set; }

            public Dog(string name) {
                Name = name;
            }

            public Dog() { }
        }

        

    }
}
