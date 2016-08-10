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
            var scheduler = new GrainTaskScheduler(fxScheduler, new ExceptionSink());

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

        

    }
}
