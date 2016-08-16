using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                                                        callCount++;
                                                        callCounts.Add(callCount);
                                                        await Task.Delay(15);
                                                        callCount--;
                                                    }, true)
                                            ).WhenAll();

            Assert.That(callCounts.All(c => c == 1));
        }



        [Test]
        public async Task IsolatedRequestsRespectedByReentrantOthers() 
        {
            var exceptions = new ExceptionSink();
            var requests = new RequestRunner(TaskScheduler.Default, exceptions);

            bool isolatedExecuting = false;

            var tIsolated = requests.Perform(async () => {
                isolatedExecuting = true;
                await Task.Delay(100);
                isolatedExecuting = false;
            }, true);

            var clashed = await requests.Perform(async () => {
                await Task.Delay(15);
                return isolatedExecuting;
            }, false);

            await tIsolated;

            Assert.That(clashed, Is.False);
        }
               

    }
    

}
