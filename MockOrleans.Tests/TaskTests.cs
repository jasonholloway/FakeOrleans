using MockOrleans;
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
    [TestFixture]
    public class CompletionTests
    {

        //Completion can't be detected per-grain, only across the system.
        //Completion will occur when we have no scheduled tasks, all timers and reminders are dead, and all requests have completed across the fixture

        //but with reentrancy, seems we should still wait for completion of overall tasks, rather than just participles
        //are over-arching tasks detectable? The first part of an async method will return a task.

        //continuations must work by registering awaiters - and at the moment of registering there must be a lock
        //queueing of continuations *must* be done by the base task, or at least the TryExecute method on the scheduler.
        //but if it's done by the scheduler, then we're slightly fucked if we're immediately delegating everything to
        //the default scheduler, 

        
        






        [Test]
        public async Task ContinuationsIncludedInOriginalTask() {

            var t1 = Task.Run(() => Task.Delay(500));
            var t2 = t1.ContinueWith(_ => Task.Delay(10000));

            //it must be the scheduler that organisers the awaiters.
            //as you can await already-completed tasks.


            //the problem with this is by normal task invocation we can't access such follow-ons, which might be tackd on the end at any point

            //so how can we know if everything has really died?
            //if continuations are outside of our grasp - we can't
            //are continuations scheduled before task state is changed to completed? This is reasonable to expect, it seems.
            //as it isn't the task itself setting this info. But awaiters must be processed in series -
            //ie some will be satisfied before others.







            var t = new Task(() => { });

            t.Status == TaskStatus.




        }




        [Test]
        public async Task CompletionRespectsRequestsAsWellAsTurns() 
        {
            var fx = new MockFixture(Substitute.For<IServiceProvider>());            
            fx.Types.Map<IBranchingExecutor, BranchingExecutor>();
            
            var grain = fx.GrainFactory.GetGrain<IBranchingExecutor>(Guid.NewGuid());            

            var t = grain.Execute(2, 4);
            
            await Task.WhenAll(fx.Tasks.All); //this shouldn't work cos of delays and recurring requests - we're awaiting a snapshot only

            Assert.That(t.IsCompleted, Is.True);
        }











        public interface IBranchingExecutor : IGrainWithGuidKey
        {
            Task Execute(int branching, int depth);
        }


        public class BranchingExecutor : Grain, IBranchingExecutor
        {
            public async Task Execute(int branching, int depth) 
            {
                await Task.Delay(50); //this will leave a gap on the scheduler temporarily - we shouldn't take this as completion, though

                if(depth > 0) {
                    await Task.WhenAll(Enumerable.Range(0, branching)
                                                .Select(async _ => {
                                                    var next = GrainFactory.GetGrain<IBranchingExecutor>(Guid.NewGuid());
                                                    await next.Execute(branching, depth - 1);
                                                }));
                }
            }
        }




    }
}
