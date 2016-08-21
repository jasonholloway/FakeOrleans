
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

    public class DeactivatedException : Exception { }


    public interface IActivation
    {        
        Task<TResult> Perform<TResult>(Func<IActivation, Task<TResult>> fn);
    }


    public interface IActivationProvider
    {
        IActivation GetActivation();
    }


        
    public interface IActivationSite
    {
        Task<TResult> Dispatch<TResult>(Func<IActivation, Task<TResult>> fn);
    }



    public class ActivationSite : IActivationSite
    {
        readonly IActivationProvider _actProv;

        public ActivationSite(IActivationProvider actProv) {
            _actProv = actProv;
        }

        public Task<TResult> Dispatch<TResult>(Func<IActivation, Task<TResult>> fn) {
            var act = _actProv.GetActivation();
            return act.Perform(fn);
        }
    }



    public class TestGrain : Grain, IGrainWithGuidKey { }



    [TestFixture]
    public class GrainDispatcherTests
    {

        Func<IActivation, Task<bool>> _fn = g => Task.FromResult(true);


        [Test]
        public async Task DispatchesToActivation() 
        {
            var activation = Substitute.For<IActivation>();
            activation.Perform(Arg.Is(_fn)).Returns(true);
            
            var actProv = Substitute.For<IActivationProvider>();
            actProv.GetActivation().Returns(activation);
            
            var site = new ActivationSite(actProv);

            var result = await site.Dispatch(_fn);
            
            Assert.That(result, Is.True);
        }

        




        //[Test]
        //public async Task ReactivatesWhenDeactivatedFound() 
        //{
        //    var activation1 = Substitute.For<IActivation>();

        //    activation1
        //        .When(x => x.Perform(Arg.Is(_fn)))
        //        .Do(x => { throw new DeactivatedException(); });
            

        //    var activation2 = Substitute.For<IActivation>();
        //    activation2.Perform(Arg.Is(_fn)).Returns(true);
            
                        
        //    var site = Substitute.For<IActivationSite>();
        //    site.GetActivation().Returns(activation1);

        //    var result = await GrainDispatcher.Dispatch(site, _fn);

        //    Assert.That(result, Is.True);
        //}







    }
}
