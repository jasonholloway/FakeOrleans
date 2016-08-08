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
        object _sync = new object();
        int _taskCount = 0;
        TaskCompletionSource<bool> _tsOnIdle = new TaskCompletionSource<bool>();
        

        protected override IEnumerable<Task> GetScheduledTasks() {
            return Enumerable.Empty<Task>();
        }


        
        protected override void QueueTask(Task task) 
        {
            lock(_sync) {
                _taskCount++;
            }
            
            try {
                ThreadPool.QueueUserWorkItem(_ => {
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
            TaskCompletionSource<bool> ts = null;
            
            lock(_sync) {
                _taskCount--;

                if(_taskCount == 0) {
                    ts = _tsOnIdle;
                    _tsOnIdle = new TaskCompletionSource<bool>();
                }
            }

            ts?.SetResult(true);
        }


        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) {
            return false;
        }
        



        public Task WhenIdle() 
        {
            lock(_sync) {
                return _taskCount == 0
                        ? Task.CompletedTask
                        : _tsOnIdle.Task;
            }
        }
    
    

    }
}
