using MockOrleans.Grains;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockOrleans.Tests
{
    [TestFixture]
    public class SchedulerTests
    {

        [Test]
        public void DisposeCancelsQueuedTasks() 
        {
            var fxScheduler = new FixtureScheduler();            
            var scheduler = new GrainTaskScheduler(fxScheduler);

            var taskSource = new TaskCompletionSource<bool>();
            var tasks = new List<Task>();

            Enumerable.Range(0, 20)
                .Aggregate(
                    (Task)taskSource.Task, 
                    (lastTask, _) => {
                        var t = lastTask.ContinueWith(__ => { }, scheduler);
                        tasks.Add(t);
                        return t;
                    });
                        
            scheduler.Dispose();

            taskSource.SetResult(true);
            
            Assert.That(
                () => Task.WhenAll(tasks), 
                Throws.Exception.InstanceOf<TaskSchedulerException>()); //other exceptions = bad; no completion = bad    
                     
        }

             

              
        



        //but, when we complete all, we don't want to stop async methods so suddenly - we just want to
        //stop reminders and timers, and also yield back when all tasks have completed (tasks may be added as part of the
        //completion, of course) - so we can't just WhenAll on the current task list: we need a special awaiter exposing.

        //So immediately disposing of grains is bad.
        //Deactivation should occur when there are no more messages to process.
        //only the scheduler will know when there's nothing else about
        //but another grain may be about to queue us a message... and so, if we had deactivated, another fresh one would appear...

        //We need to quieten the system at the first opportunity
        //Given that our input into has finished, and that all timers and reminders have been cancelled,
        //everything *should* quieten, unless there's looping going on, which is not impossible - though never returning in that case seems reasonable.
        //and all schedulers will eventually be disposed... so these loops will eventually be arrested

        //but - given we've set some mechanism in action, we would like to sense it quitting.
        //otherwise there can be no testing of timers except by waiting.

        //all grain schedulers should delegate to the fixture scheduler, which will delegate to the threadpool
        //Task.Run won't be handled by this, obvs - but as we have no way of overriding the default, we have to accept this.
        //








    }
}
