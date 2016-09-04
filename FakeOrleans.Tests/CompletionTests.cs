using FakeOrleans;
using NSubstitute;
using NUnit.Framework;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FakeOrleans.Tests
{

    [TestFixture]
    public class CompletionTests
    {
        
        [Test]
        public async Task CompletionSucceedsWhenAllTasksRunViaFixtureScheduler() 
        {
            var fx = new Fixture(Substitute.For<IServiceProvider>());            
            fx.Types.Map<IBranchingExecutor, BranchingExecutor>();
            
            var grain = fx.GrainFactory.GetGrain<IBranchingExecutor>(Guid.NewGuid());
            
            var task = grain.Execute(3, 8);                                          
            
            await fx.Scheduler.WhenIdle();
            
            Assert.That(task.IsCompleted, Is.True);
        }
                

        [Test]
        public async Task SchedulerIsIdleImmediatelyIfEmpty() 
        {
            var scheduler = new FixtureScheduler();
            
            await scheduler.WhenIdle();
        }
        

        [Test]
        public async Task CompletionRespectsRequestsWhenSomeTasksRunElsewhere() 
        {
            var fx = new Fixture(Substitute.For<IServiceProvider>());
            fx.Types.Map<IBranchingExecutor, BranchingExecutor>();

            var grain = fx.GrainFactory.GetGrain<IBranchingExecutor>(Guid.NewGuid());

            var t = grain.Execute(2, 8, 50); //includes Task.Delay, creating gaps in which fixture scheduler will temporarily quieten, before real completion

            await Task.Delay(15); //gives above request chance to start
            Assert.That(t.IsCompleted, Is.False);

            await fx.Requests.WhenIdle();   //why are no requests registered here? There are some definitely ongoing...
            await fx.Scheduler.WhenIdle();

            Assert.That(t.IsCompleted, Is.True);
        }
        

        //executes a tree of async calls
        public interface IBranchingExecutor : IGrainWithGuidKey 
        {
            Task Execute(int branching, int depth, int delay = 0);
        }


        public class BranchingExecutor : Grain, IBranchingExecutor
        {
            public async Task Execute(int branching, int depth, int delay) 
            {
                if(delay > 0) await Task.Delay(delay);
                
                if(depth > 0) {
                    await Task.WhenAll(Enumerable.Range(0, branching)
                                                .Select(async _ => {
                                                    var next = GrainFactory.GetGrain<IBranchingExecutor>(Guid.NewGuid());
                                                    await next.Execute(branching, depth - 1, delay);
                                                }));
                }
            }
            
        }

        



        [Test]
        public async Task RequestIdlenessImmediateIfNoRequests() 
        {
            var fx = new Fixture();
            await fx.Requests.WhenIdle();
        }



    }
}
