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
    public class CompletionTests
    {


        [Test]
        public async Task CompletionSucceedsWhenAllTasksRunViaFixtureScheduler() 
        {
            var fx = new MockFixture(Substitute.For<IServiceProvider>());            
            fx.Types.Map<IBranchingExecutor, BranchingExecutor>();
            
            var grain = fx.GrainFactory.GetGrain<IBranchingExecutor>(Guid.NewGuid());
            
            var task = grain.Execute(2, 4);                                          
            
            await fx.Scheduler.CloseWhenQuiet();
            
            Assert.That(task.IsCompleted, Is.True);
        }




        [Test]
        public async Task CompletionRespectsRequestsWhenSomeTasksRunElsewhere() 
        {
            var fx = new MockFixture(Substitute.For<IServiceProvider>());
            fx.Types.Map<IBranchingExecutor, BranchingExecutor>();

            var grain = fx.GrainFactory.GetGrain<IBranchingExecutor>(Guid.NewGuid());

            var t = grain.ExecuteViaDelay(2, 4); //includes Task.Delay, creating gaps in which fixture scheduler will temporarily quiten, before real completion

            await fx.Scheduler.CloseWhenQuiet();

            Assert.That(t.IsCompleted, Is.True);
        }






        public interface IBranchingExecutor : IGrainWithGuidKey
        {
            Task Execute(int branching, int depth);
            Task ExecuteViaDelay(int branching, int depth);
        }


        public class BranchingExecutor : Grain, IBranchingExecutor
        {
            public async Task Execute(int branching, int depth) 
            {
                Interlocked.Increment(ref Blah.Count);

                if(depth > 0) {
                    await Task.WhenAll(Enumerable.Range(0, branching)
                                                .Select(async _ => {
                                                    var next = GrainFactory.GetGrain<IBranchingExecutor>(Guid.NewGuid());
                                                    await next.Execute(branching, depth - 1);
                                                }));
                }
            }

            public async Task ExecuteViaDelay(int branching, int depth) {
                await Task.Delay(50);
                await Execute(branching, depth);
            }
        }



        




    }
}
