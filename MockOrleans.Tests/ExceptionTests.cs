using FluentAssertions;
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
    public class ExceptionTests
    {

        [Test]
        public void InRequestExceptionsReturnedToCallerNotSinked() 
        {
            var fx = new MockFixture();
            fx.Types.Map<IExceptionThrower, ExceptionThrower>();

            var grain = fx.GrainFactory.GetGrain<IExceptionThrower>(Guid.Empty);
            
            Assert.That(() => grain.ThrowException(), Throws.Exception.InstanceOf<TestException>());
            
            Assert.That(() => fx.Exceptions.Rethrow(), Throws.Nothing); //should be no sinked exceptions - packed and handled above
        }
        

        [Test]
        public async Task OutOfRequestExceptionsSinked() 
        {
            var fx = new MockFixture();
            fx.Types.Map<IExceptionThrower, ExceptionThrower>();

            var grain = fx.GrainFactory.GetGrain<IExceptionThrower>(Guid.Empty);

            await grain.ThrowExceptionOutOfRequest();

            await fx.Requests.WhenIdle();
            await fx.Scheduler.WhenIdle();

            new Action(() => fx.Exceptions.Rethrow())
                        .ShouldThrow<TestException>();            
        }


        public interface IExceptionThrower : IGrainWithGuidKey
        {
            Task ThrowException();
            Task ThrowExceptionOutOfRequest();
            Task ThrowExceptionOnDeactivation();
        }


        public class ExceptionThrower : Grain, IExceptionThrower
        {
            bool _throwOnDeactivation = false;

            public Task ThrowException() {
                throw new TestException();
            }

            public Task ThrowExceptionOutOfRequest() 
            {
                Task.Factory.StartNew(() => {
                    throw new TestException();
                });

                return Task.CompletedTask;
            }

            public Task ThrowExceptionOnDeactivation() 
            {
                _throwOnDeactivation = true;
                DeactivateOnIdle();

                return Task.CompletedTask;
            }
            
            public override Task OnDeactivateAsync() 
            {
                if(_throwOnDeactivation) throw new TestException();

                return Task.CompletedTask;
            }

        }




        public class TestException : Exception
        { }



    }
}
