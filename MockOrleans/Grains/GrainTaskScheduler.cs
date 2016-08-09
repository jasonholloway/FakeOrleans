using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MockOrleans.Grains
{
    
    public interface ITaskRegistry
    {
        IEnumerable<Task> All { get; }
        void Register(Task task);
    }


    public class GrainTaskScheduler : TaskScheduler, IDisposable
    {
        TaskScheduler _innerScheduler;

        object _sync = new object();
        Task _last = Task.CompletedTask;
        CancellationTokenSource _cancelSource;
        CancellationToken _cancelToken;

        public GrainTaskScheduler(TaskScheduler innerScheduler) {
            _innerScheduler = innerScheduler;
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
                _last = _last.ContinueWith(_ => TryExecuteTask(task), _cancelToken, TaskContinuationOptions.None, _innerScheduler); //catch exceptions also?                
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
