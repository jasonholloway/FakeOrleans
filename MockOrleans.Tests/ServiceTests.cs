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
    [TestFixture]
    public class ServiceTests
    {
        [Test]
        public async Task InjectedServiceGivenToGrain() 
        {                        
            var fx = new MockFixture(Substitute.For<IServiceProvider>());
            fx.Types.Map<IIntEmitter, IntEmitter>();
            
            var sink = fx.Services.Inject<IList<int>>(new List<int>());
            
            var emitter = fx.GrainFactory.GetGrain<IIntEmitter>(Guid.NewGuid());
                        
            await emitter.Emit(1);
            await emitter.Emit(2);
            await emitter.Emit(3);
            
            Assert.That(sink, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        





        public interface IIntEmitter : IGrainWithGuidKey
        {
            Task Emit(int i);
        }

        public class IntEmitter : Grain, IIntEmitter
        {
            IList<int> _sink;

            public IntEmitter(IList<int> sink) {
                _sink = sink;
            }

            public Task Emit(int i) {
                _sink.Add(i);
                return Task.CompletedTask;
            }
        }





    }
}
