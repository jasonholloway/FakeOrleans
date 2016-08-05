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

        //the scheduler outputs tasks as a side-effect
        

        [Test]
        public async Task DisposalCompletesTasks() 
        {            
            var harness = Substitute.For<GrainHarness>(); //no good
            
            var scheduler = new GrainTaskScheduler(harness);

            var tasks = Enumerable.Range(0, 100).Select(i => new Task(() => { }));

            tasks.ForEach(t => t.Start(scheduler));

            await Task.WhenAll(tasks);
        }


    }
}
