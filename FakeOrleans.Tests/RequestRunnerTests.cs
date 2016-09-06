using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static FakeOrleans.Tests.ExceptionTests;

namespace FakeOrleans.Tests
{

    [TestFixture]
    public class RequestRunnerTests
    {

        [Test]
        public async Task IsolatedRequestsIsolatedFromEachOther() 
        {
            var exceptions = new ExceptionSink();
            var requests = new RequestRunner(TaskScheduler.Default, exceptions);

            List<int> callCounts = new List<int>();
            int callCount = 0;

            await Enumerable.Range(0, 20)
                        .Select(_ => requests.Perform(async () => {
                                                        int c = Interlocked.Increment(ref callCount);
                                                        callCounts.Add(c);
                                                        await Task.Delay(15);
                                                        Interlocked.Decrement(ref callCount);
                                                    }, RequestMode.Isolated)
                                            ).WhenAll();

            Assert.That(callCounts.All(c => c == 1));
        }



        [Test]
        public async Task IsolatedRequestsRespectedByReentrantFollowers() 
        {
            var exceptions = new ExceptionSink();
            var requests = new RequestRunner(TaskScheduler.Default, exceptions);

            bool isolatedExecuting = false;
                        
            var tIsolated = requests.Perform(async () => {
                                                isolatedExecuting = true;
                                                await Task.Delay(100);
                                                isolatedExecuting = false;
                                            }, RequestMode.Isolated);

            var clashed = await requests.Perform(async () => {
                                                await Task.Delay(15);
                                                return isolatedExecuting;
                                            }, RequestMode.Reentrant);

            await tIsolated;

            Assert.That(clashed, Is.False);
        }


        [Test]
        public async Task IsolatedRequestsWaitForPrecedingReentrantsToClearOff() 
        {
            var exceptions = new ExceptionSink();
            var requests = new RequestRunner(TaskScheduler.Default, exceptions);
            
            long callCount = 0;

            requests.PerformAndForget(async () => {
                    Interlocked.Increment(ref callCount);
                    await Task.Delay(100);
                    Interlocked.Decrement(ref callCount);
                }, RequestMode.Reentrant);

            await Task.Delay(15);

            bool clashed = await requests.Perform(() => {
                var c = Interlocked.Read(ref callCount);
                return Task.FromResult(c > 0);
            }, RequestMode.Isolated);
            
            await requests.WhenIdle();
            exceptions.RethrowAll();

            Assert.That(clashed, Is.False);
        }



        [Test]
        public void Closing_DisallowsFurtherRequests() 
        {
            var exceptions = new ExceptionSink();
            var requests = new RequestRunner(TaskScheduler.Default, exceptions);

            requests.Close(() => Task.Delay(50));
            
            Assert.That(
                () => requests.Perform(() => Task.CompletedTask),
                Throws.Exception.InstanceOf<RequestRunnerClosedException>());
        }


        
        [Test]
        public async Task WhenIdle_AwaitsCompletionOfDeactivation() 
        {
            var exceptions = new ExceptionSink();
            var requests = new RequestRunner(TaskScheduler.Default, exceptions);
            
            bool closed = false;
            
            requests.Close(async () => {
                                        await Task.Delay(100);
                                        closed = true;
                                    });

            await requests.WhenIdle();
            exceptions.RethrowAll();

            Assert.That(closed, Is.True);
        }



        [Test]
        public async Task Closing_Performed() 
        {
            var exceptions = new ExceptionSink();
            var requests = new RequestRunner(TaskScheduler.Default, exceptions);

            bool closed = false;

            requests.PerformAndForget(() => Task.Delay(50));
            
            requests.Close(() => {
                closed = true;
                return Task.CompletedTask;
            });

            await requests.WhenIdle();
            exceptions.RethrowAll();

            Assert.That(closed, Is.True);
        }


        [Test]
        public async Task Closes_Immediately_WhenNoneWaiting() 
        {
            var exceptions = new ExceptionSink();
            var requests = new RequestRunner(TaskScheduler.Default, exceptions);
            
            bool closed = false;
            
            requests.Close(() => {
                closed = true;
                return Task.CompletedTask;
            });

            await requests.WhenIdle();
            exceptions.RethrowAll();

            Assert.That(closed, Is.True);
        }


        [Test]
        public async Task Closes_OnlyAfter_PreviouslyScheduledPerformances() 
        {
            var exceptions = new ExceptionSink();
            var requests = new RequestRunner(TaskScheduler.Default, exceptions);

            int calls = 0;

            Enumerable.Range(0, 100)
                        .Select(_ => requests.Perform(async () => {
                            Interlocked.Increment(ref calls);
                            await Task.Delay(30);
                            Interlocked.Decrement(ref calls);
                        }))
                        .ToArray();
            
            requests.Close(() => {                
                    Assert.That(calls, Is.EqualTo(0));
                    return Task.CompletedTask;
                });
            
            await requests.WhenIdle();
            exceptions.RethrowAll();
        }



        //[Test]
        //public async Task Closing_SinksExceptions() 
        //{
        //    var exceptions = new ExceptionSink();
        //    var requests = new RequestRunner(TaskScheduler.Default, exceptions);

        //    requests.PerformAndClose(() => {
        //        throw new TestException();
        //    });

        //    await requests.WhenIdle();

        //    Assert.That(
        //        () => exceptions.RethrowAll(), 
        //        Throws.InnerException.InstanceOf<TestException>());            
        //}



        [Test]
        public async Task Closing_IsAwaitable() 
        {
            var exceptions = new ExceptionSink();
            var requests = new RequestRunner(TaskScheduler.Default, exceptions);

            bool completed = false;

            await requests.Close(() => Task.Delay(500).ContinueWith(_ => completed = true));

            Assert.That(completed);
        }





    }


}
