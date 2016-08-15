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
        volatile bool _currentIsIsolated = false;


        public RequestRunner(TaskScheduler scheduler, ExceptionSink exceptionSink, RequestRunner innerReqs = null, bool isolate = false) {
            _scheduler = scheduler;
            _exceptionSink = exceptionSink;
            _innerReqs = innerReqs;
            _waitingTaskSources = new Queue<TaskCompletionSource<bool>>();
            _defaultIsolation = isolate;
        }


        void Increment() {
            lock(_sync) _count++;

            _innerReqs?.Increment();
        }

        //Not convinced by below
        void Decrement() {
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
        


        SemaphoreSlim _smActive = new SemaphoreSlim(1);

        public async Task<T> Perform<T>(Func<Task<T>> fn, bool? isolateOverride = null) 
        {
            bool isolate = isolateOverride.HasValue ? isolateOverride.Value : _defaultIsolation;

            var task = new Task<Task<T>>(fn);

            Increment();

            if(isolate || _currentIsIsolated) {
                await _smActive.WaitAsync();
                if(!isolate) _smActive.Release();
            }

            _currentIsIsolated = isolate;

            task.Start(_scheduler);

            return await  task.Unwrap()
                            .ContinueWith(t => {
                                if(isolate) _smActive.Release();

                                Decrement();
                        
                                if(t.IsFaulted) throw t.Exception;
                                else return t.Result;             
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
            _smActive.Dispose();
        }


    }


}
