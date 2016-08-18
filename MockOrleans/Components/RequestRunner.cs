using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MockOrleans
{

    public class RequestRunner : IDisposable
    {
        TaskScheduler _scheduler;
        ExceptionSink _exceptionSink;
        RequestRunner _innerReqs;
        int _count;
        Queue<TaskCompletionSource<bool>> _waitingTaskSources;
        object _sync = new object();

        bool _defaultIsolation = false;
        volatile bool _currentEnforcesIsolation = false;
        volatile bool _disposed = false;


        public RequestRunner(TaskScheduler scheduler, ExceptionSink exceptionSink, RequestRunner innerReqs = null, bool isolate = false) {
            _scheduler = scheduler;
            _exceptionSink = exceptionSink;
            _innerReqs = innerReqs;
            _waitingTaskSources = new Queue<TaskCompletionSource<bool>>();
            _defaultIsolation = isolate;
        }


        void Increment(bool isFinal = false) {            
            lock(_sync) {
                if(!isFinal && _closed) throw new InvalidOperationException("RequestRunner is closed!");

                _count++;
            }

            _innerReqs?.Increment();
        }

        //Not convinced by below
        void Decrement() {
            Queue<TaskCompletionSource<bool>> capturedTaskSources = null;
            
            lock(_sync) {
                _count--;
                
                if(_count == 0) {
                    if(_closed) {
                        //now fire off deactivation via a request

                        //parameterising the below is too much forcing of a public function:
                        //its innards need decomposing...

                        Perform(_fnOnClose, true, true); //scheduler already set - THIS WILL DEADLOCK! - it'll increment inline
                    }                           
                                                
                    capturedTaskSources = Interlocked.Exchange(ref _waitingTaskSources, new Queue<TaskCompletionSource<bool>>());
                }
            }

            _innerReqs?.Decrement();
            capturedTaskSources?.ForEach(ts => ts.SetResult(true));
        }


        //and also - current request, if isolated, should be protected from intrusion
        //...


        public void PerformAndForget(Func<Task> fn, bool? isolate = null)
            => Perform(fn, isolate)
                .ContinueWith(t => {
                    _exceptionSink.Add(t.Exception); //will this duplicate exceptions?
                }, TaskContinuationOptions.OnlyOnFaulted);



        public Task Perform(Func<Task> fn, bool? isolate = null)
            => Perform(async () => {
                            await fn();
                            return default(VoidType);
                        }, isolate);





        volatile bool _closed = false;
        Func<Task> _fnOnClose;

        public void CloseAndPerform(Func<Task> fn = null) {
            _closed = true;
            _fnOnClose = fn;

            lock(_sync) {
                if(_count == 0) {
                    _fnOnClose?.Invoke(); //needs to be put on scheduler!!!!!
                }                
            }
        }

        


        SemaphoreSlim _smActive = new SemaphoreSlim(1);

        
        public async Task<T> Perform<T>(Func<Task<T>> fn, bool? isolateOverride = null, bool isFinal = false)   //if dead, throw exception - via dead flag, or disposal, or something
        { 
            bool isolateThisRequest = isolateOverride.HasValue ? isolateOverride.Value : _defaultIsolation;

            var task = new Task<Task<T>>(fn);

            Increment(isFinal);

            if(isolateThisRequest || _currentEnforcesIsolation) {
                await _smActive.WaitAsync();
                if(!isolateThisRequest) _smActive.Release();
            }

            _currentEnforcesIsolation = isolateThisRequest || isFinal; //finalizing requests always enforce isolation - whether they like it or not

            if(isFinal) _smActive.Dispose(); //will throw off all who come after us

            task.Start(_scheduler);

            return await task.Unwrap()
                            .ContinueWith(t => {
                                if(isolateThisRequest && !isFinal) _smActive.Release();

                                Decrement();
                        
                                if(t.IsFaulted) throw t.Exception;
                                else return t.Result; //problem is in returning void
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



        void IDisposable.Dispose() {
            _disposed = true;
            _smActive.Dispose();
        }


    }


}
