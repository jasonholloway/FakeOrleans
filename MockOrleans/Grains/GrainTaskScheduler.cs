using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MockOrleans.Grains
{
    
    public class GrainTaskScheduler : TaskScheduler, IDisposable
    {
        GrainHarness _harness;

        object _sync = new object();
        Task _last = Task.FromResult(true);
        CancellationTokenSource _cancelSource;
        CancellationToken _cancelToken;

        public GrainTaskScheduler(GrainHarness harness) {
            _harness = harness;
            _cancelSource = new CancellationTokenSource();
            _cancelToken = _cancelSource.Token;
        }


        protected override IEnumerable<Task> GetScheduledTasks() {
            return new Task[0];
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) {
            return false;
        }

        protected override void QueueTask(Task task) {
            if(_disposed) throw new ObjectDisposedException(nameof(GrainTaskScheduler));

            lock(_sync) {
                _last = _last.ContinueWith(_ => TryExecuteTask(task), _cancelToken, TaskContinuationOptions.RunContinuationsAsynchronously, TaskScheduler.Default); //catch exceptions also?

                _harness.Fixture.RegisterTask(_last);
            }
        }


        volatile bool _disposed = false;

        public void Dispose() {
            _disposed = true;
            _cancelSource.Cancel();
            // _last.Dispose();
        }
    }



    //class GrainTaskScheduler : TaskScheduler
    //{
    //    object _sync = new object();
    //    Task _last = Task.FromResult(true);

    //    GrainHarness _harness;

    //    public GrainTaskScheduler(GrainHarness harness) {
    //        _harness = harness;
    //    }


    //    protected override IEnumerable<Task> GetScheduledTasks() {
    //        return Enumerable.Empty<Task>();
    //    }

    //    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) {
    //        return false;
    //    }

    //    protected override void QueueTask(Task task) {
    //        lock(_sync) {
    //            _last = _last.ContinueWith(_ => TryExecuteTask(task), TaskScheduler.Default); //catch exceptions also?
    //            _harness.Fixture.RegisterTask(_last);
    //        }
    //    }
    //}
    

}
