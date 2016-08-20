using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MockOrleans.Tests
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
            exceptions.Rethrow();

            Assert.That(clashed, Is.False);
        }



        [Test]
        public void ClosingDisallowsFurtherRequests() 
        {
            var exceptions = new ExceptionSink();
            var requests = new RequestRunner(TaskScheduler.Default, exceptions);

            requests.CloseAndPerform(() => Task.Delay(50));
            
            var task = requests.Perform(() => Task.CompletedTask);
                        
            Assert.That(
                () => task,
                Throws.InvalidOperationException);
        }



        [Test]
        public async Task WhenIdleWaitsForCompletionOfDeactivation() 
        {
            var exceptions = new ExceptionSink();
            var requests = new RequestRunner(TaskScheduler.Default, exceptions);
            
            bool closed = false;
            
            requests.CloseAndPerform(async () => {
                await Task.Delay(100);
                closed = true;
            });

            await requests.WhenIdle();
            exceptions.Rethrow();

            Assert.That(closed, Is.True);
        }



        [Test]
        public async Task DeactivationPerformed() {
            var exceptions = new ExceptionSink();
            var requests = new RequestRunner(TaskScheduler.Default, exceptions);

            bool closed = false;

            requests.PerformAndForget(() => Task.Delay(50));
            
            requests.CloseAndPerform(() => {
                closed = true;
                return Task.CompletedTask;
            });

            await requests.WhenIdle();
            exceptions.Rethrow();

            Assert.That(closed, Is.True);
        }


        [Test]
        public async Task DeactivatesImmediatelyWhenNoneWaiting() 
        {
            var exceptions = new ExceptionSink();
            var requests = new RequestRunner(TaskScheduler.Default, exceptions);
            
            bool closed = false;
            
            requests.CloseAndPerform(() => {
                closed = true;
                return Task.CompletedTask;
            });

            await requests.WhenIdle();
            exceptions.Rethrow();

            Assert.That(closed, Is.True);
        }


        [Test]
        public async Task ClosesAfterPreviouslyScheduledPerformances() 
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
            
            requests.CloseAndPerform(() => {                
                    Assert.That(calls, Is.EqualTo(0));
                    return Task.CompletedTask;
                });
            
            await requests.WhenIdle();
            exceptions.Rethrow();
        }


    }


}
