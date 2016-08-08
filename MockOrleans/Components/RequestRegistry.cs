using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MockOrleans
{

    public class RequestRegistry
    {
        TaskScheduler _scheduler;
        RequestRegistry _innerReqs;
        int _count;
        Queue<TaskCompletionSource<bool>> _waitingTaskSources;
        object _sync = new object();

        public RequestRegistry(TaskScheduler scheduler, RequestRegistry innerReqs = null) {
            _scheduler = scheduler;
            _innerReqs = innerReqs;
            _waitingTaskSources = new Queue<TaskCompletionSource<bool>>();
        }


        public void Increment() {
            lock(_sync) {
                _count++;
            }
            
            _innerReqs?.Increment();
        }

        //Not convinced by below
        public void Decrement() {
            Queue<TaskCompletionSource<bool>> capturedTaskSources = null;
            
            lock(_sync) {
                _count--;

                if(_count == 0) {
                    capturedTaskSources = Interlocked.Exchange(ref _waitingTaskSources, new Queue<TaskCompletionSource<bool>>());
                }
            }

            _innerReqs?.Decrement();
            capturedTaskSources?.ForEach(ts => ts.SetResult(true));
        }


        public void Perform(Func<Task> fn) 
        {
            var task = new Task<Task>(fn);

            Increment();
            task.Start(_scheduler);
            
            task.Unwrap().ContinueWith(
                                t => {
                                    Decrement();

                                    if(t.IsFaulted) {
                                        var ex = t.Exception; //this should be marshalled somewhere visible
                                    }
                                }, _scheduler);
        }



        public Task WhenIdle() {
            lock(_sync) {
                if(_count == 0) {
                    return Task.CompletedTask;
                }
                
                var source = new TaskCompletionSource<bool>();
                _waitingTaskSources.Enqueue(source);

                return source.Task;
            }
        }

    }


}
