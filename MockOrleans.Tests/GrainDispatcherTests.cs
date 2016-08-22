
using MockOrleans.Grains;
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

        IActivation _act = null;
        object _sync = new object();
        
        public ActivationSite(IActivationProvider actProv) {
            _actProv = actProv;
        }
                
        public Task<TResult> Dispatch<TResult>(Func<IActivation, Task<TResult>> fn) 
        {
            IActivation act = null;

            lock(_sync) {
                act = _act ?? (_act = _actProv.GetActivation());
            }
            
            try {
                return act.Perform(fn);
            }
            catch(DeactivatedException) {
                lock(_sync) _act = null;
                return Dispatch(fn);
            }
        }
        
    }
    

    [TestFixture]
    public class ActivationSiteTests
    {

        Func<IActivation, Task<Guid>> _fn = g => Task.FromResult(Guid.Empty);


        [Test]
        public async Task DispatchesToActivation() 
        {
            var guid = Guid.NewGuid();

            var activation = Substitute.For<IActivation>();
            activation.Perform(Arg.Is(_fn)).Returns(guid);

            var actProv = Substitute.For<IActivationProvider>();
            actProv.GetActivation().Returns(activation);

            var site = new ActivationSite(actProv);

            var result = await site.Dispatch(_fn);

            Assert.That(result, Is.EqualTo(guid));
        }

        
        
        [Test]
        public async Task ReactivatesWhenDeactivatedFound() 
        {
            var guid = Guid.NewGuid();

            var deadActivation = Substitute.For<IActivation>();
            deadActivation
                .When(x => x.Perform(Arg.Is(_fn)))
                .Do(_ => { throw new DeactivatedException(); });

            var goodActivation = Substitute.For<IActivation>();
            goodActivation.Perform(Arg.Is(_fn)).Returns(guid);

            var actProv = Substitute.For<IActivationProvider>();
            actProv.GetActivation().Returns(deadActivation, goodActivation);

            var site = new ActivationSite(actProv);

            var result = await site.Dispatch(_fn);

            Assert.That(result, Is.EqualTo(guid));
        }




        [Test]
        public async Task SameActivationUsedIfGood() 
        {            
            var actProv = Substitute.For<IActivationProvider>();

            actProv.GetActivation().Returns(_ => {
                var activation = Substitute.For<IActivation>();
                activation.Perform(Arg.Is(_fn)).Returns(Guid.NewGuid());
                return activation;
            });

            var site = new ActivationSite(actProv);

            var result1 =  await site.Dispatch(_fn);
            var result2 = await site.Dispatch(_fn);
            
            Assert.That(result1, Is.EqualTo(result2));
        }





        [Test]
        public async Task ReactivatesOneAtATimeViaLock() 
        {
            var actProv = Substitute.For<IActivationProvider>();

            actProv.GetActivation().Returns(_ => {
                var activation = Substitute.For<IActivation>();
                activation.Perform(Arg.Is(_fn)).Returns(Guid.NewGuid());
                return activation;
            });

            var site = new ActivationSite(actProv);
            
            var results = await Enumerable.Range(0, 1000)
                                    .Select(async _ => {
                                        await Task.Delay(15);
                                        return await site.Dispatch(_fn);
                                        })
                                    .WhenAll();

            Assert.That(results, Is.All.EqualTo(results.First()));
        }



    }
}
