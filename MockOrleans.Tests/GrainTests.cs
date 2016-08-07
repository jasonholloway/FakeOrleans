using MockOrleans;
using NSubstitute;
using NUnit.Framework;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MockOrleans.Tests
{

    [TestFixture]
    public class GrainTests
    {
        
        [Test]
        public async Task DeactivateOnIdleCalledWhenCurrentRequestsGone()
        {
            var fx = new MockFixture(Substitute.For<IServiceProvider>());            
            fx.Types.Map<IDeactivatable, Deactivatable>();
            fx.Types.Map<IDeactivationRecorder, DeactivationRecorder>();
                        
            var recorder = fx.GrainFactory.GetGrain<IDeactivationRecorder>(Guid.Empty);

            var deactivatable = fx.GrainFactory.GetGrain<IDeactivatable>(Guid.NewGuid());
            await deactivatable.SetRecorder(recorder);

            await deactivatable.PrecipitateDeactivation();
            
            await fx.Requests.WhenIdle();
            
            //assert is deactivated here
            //Assert.That(fx.Silo.IsActive(grain), Is.False);
            
            var deactivated = await recorder.GetDeactivated();
            Assert.That(deactivated, Is.True);            
        }

        


        public interface IDeactivationRecorder : IGrainWithGuidKey
        {
            Task SetDeactivated();
            Task<bool> GetDeactivated();
        }

        public class DeactivationRecorder : Grain, IDeactivationRecorder
        {
            bool _deactivated;

            public Task<bool> GetDeactivated() {
                return Task.FromResult(_deactivated);
            }

            public Task SetDeactivated() {
                _deactivated = true;
                return Task.CompletedTask;
            }
        }






        public interface IDeactivatable : IGrainWithGuidKey
        {
            Task SetRecorder(IDeactivationRecorder recorder);
            Task PrecipitateDeactivation();
        }


        public class Deactivatable : Grain, IDeactivatable
        {
            IDeactivationRecorder _recorder;

            public Task SetRecorder(IDeactivationRecorder recorder) {
                _recorder = recorder;
                return Task.CompletedTask;
            }
            
            public Task PrecipitateDeactivation() {
                DeactivateOnIdle();
                return Task.CompletedTask;
            }

            public override Task OnDeactivateAsync() {
                return _recorder.SetDeactivated();
            }

        }



    }
}
