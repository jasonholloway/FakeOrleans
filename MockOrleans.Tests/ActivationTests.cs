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
    [TestFixture]
    public class ActivationTests
    {

        [Test]
        public async Task ActivationOccursOnFirstResolutionOfPlacement() 
        {
            var fx = new MockFixture();
            fx.Types.Map<IActivatable, Activatable>();
            
            var tallies = fx.Services.Inject(new ConcurrentBag<int>());
            var delay = fx.Services.Inject(0);

            var placement = fx.Grains.GetPlacement<IActivatable>(Guid.NewGuid());
            
            var activation = await fx.Grains.GetActivation(placement);
            
            Assert.That(tallies, Has.Count.EqualTo(1));
        }




        public interface IActivatable : IGrainWithGuidKey
        { }

        public class Activatable : Grain, IActivatable
        {
            ConcurrentBag<int> _tallies;
            int _delay;

            public Activatable(ConcurrentBag<int> tallies, int delay) {
                _tallies = tallies;
                _delay = delay;
            }

            public override async Task OnActivateAsync() {
                await Task.Delay(_delay);
                _tallies.Add(1);
            }

        }





        [Test]
        public async Task NewActivationOccursDirectlyAfterDeactivation() 
        {
            throw new NotImplementedException();
        }



    }
}
