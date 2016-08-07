using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MockOrleans
{    

    public class FixtureScheduler : TaskScheduler
    {
        volatile bool _closed = false;
        volatile bool _closeRequested = false;
        
        object _sync = new object();
        int _taskCount = 0;
        TaskCompletionSource<bool> _tsOnClose = new TaskCompletionSource<bool>();

        
        public bool IsOpen {
            get { return !_closed; }
        }


        protected override IEnumerable<Task> GetScheduledTasks() {
            return Enumerable.Empty<Task>();
        }


        
        protected override void QueueTask(Task task) 
        {
            lock(_sync) {
                if(_closed) throw new ObjectDisposedException(nameof(FixtureScheduler));
                _taskCount++;
            }
            
            try {
                ThreadPool.QueueUserWorkItem(_ => {
                    if(_closed) { return; } //this shouldn't happen...

                    try {
                        TryExecuteTask(task);
                    }
                    finally {
                        DecrementTaskCount();
                    }
                });
            }
            catch(NotSupportedException) {
                DecrementTaskCount();
                throw;
            }
        }


        void DecrementTaskCount() 
        {
            lock(_sync) {
                _taskCount--;

                if(_closeRequested && _taskCount == 0) {
                    _closed = true;
                }
            }
            
            if(_closed) _tsOnClose.TrySetResult(true); //queues continuation on originating scheduler
        }


        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) {
            return false;
        }
        

        public Task CloseWhenIdle() {
            _closeRequested = true;

            lock(_sync) {
                if(_taskCount == 0) {
                    _closed = true;
                }
            }

            if(_closed) _tsOnClose.TrySetResult(true);
            
            return _tsOnClose.Task;
        }
                        

    }
}
