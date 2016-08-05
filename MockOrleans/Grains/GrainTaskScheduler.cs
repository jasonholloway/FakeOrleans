using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockOrleans.Grains
{

    class GrainTaskScheduler : TaskScheduler
    {
        object _sync = new object();
        Task _last = Task.FromResult(true);

        GrainHarness _harness;

        public GrainTaskScheduler(GrainHarness harness) {
            _harness = harness;
        }


        protected override IEnumerable<Task> GetScheduledTasks() {
            return Enumerable.Empty<Task>();
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) {
            return false;
        }

        protected override void QueueTask(Task task) {
            lock(_sync) {
                _last = _last.ContinueWith(_ => TryExecuteTask(task), TaskScheduler.Default); //catch exceptions also?
                _harness.Fixture.RegisterTask(_last);
            }
        }
    }
    

}
