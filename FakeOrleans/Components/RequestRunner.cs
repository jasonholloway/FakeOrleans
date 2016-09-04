using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FakeOrleans
{

    public interface IRequestRunner
    {
        Task<T> Perform<T>(Func<Task<T>> fn, RequestMode mode);
        void PerformAndForget(Func<Task> fn, RequestMode mode);
        void PerformAndClose(Func<Task> fn);
        Task WhenIdle();
    }


    public enum RequestMode
    {
        Unspecified,
        Isolated,
        Reentrant
    }


    public class RequestRunner : IRequestRunner, IDisposable
    {
        TaskScheduler _scheduler;
        ExceptionSink _exceptionSink;
        RequestRunner _innerReqs;
        int _count;
        Queue<TaskCompletionSource<bool>> _whenIdleTaskSources;
        Queue<TaskCompletionSource<bool>> _whenInnerClearTaskSources;
        object _sync = new object();

        bool _defaultIsolation = false;

        volatile bool _disposed = false;


        enum Mode
        {
            Active,
            Closing,
            Closed
        }
        


        volatile Mode _mode = Mode.Active;
        Func<Task<VoidType>> _fnOnClose;




        public RequestRunner(TaskScheduler scheduler, ExceptionSink exceptionSink, RequestRunner innerReqs = null, bool isolate = false) {
            _scheduler = scheduler;
            _exceptionSink = exceptionSink;
            _innerReqs = innerReqs;
            _whenIdleTaskSources = new Queue<TaskCompletionSource<bool>>();
            _whenInnerClearTaskSources = new Queue<TaskCompletionSource<bool>>();
            _defaultIsolation = isolate;
        }


        void Enter(bool isDeactivation = false) {            
            lock(_sync) {
                if(!isDeactivation && _mode == Mode.Closed) throw new InvalidOperationException($"RequestRunner is {_mode}!");

                _count++;
            }

            _innerReqs?.Enter();
        }




        object _innerSync = new object();
        volatile int _innerCount;

        void EnterInner() {
            lock(_innerSync) {
                _innerCount++;
            }
        }

        void LeaveInner() {
            Queue<TaskCompletionSource<bool>> capturedTaskSources = null;

            lock(_innerSync) {
                _innerCount--;

                if(_innerCount <= 1) {
                    capturedTaskSources = Interlocked.Exchange(ref _whenInnerClearTaskSources, new Queue<TaskCompletionSource<bool>>());
                }
            }

            capturedTaskSources?.ForEach(s => s.SetResult(true));
        }

        Task WhenInnerClear() {
            lock(_innerSync) {
                if(_innerCount <= 1) {
                    return Task.CompletedTask;
                }

                var source = new TaskCompletionSource<bool>();
                _whenInnerClearTaskSources.Enqueue(source);

                return source.Task;
            }
        }






        //Not convinced by below
        void Leave() 
        {
            Queue<TaskCompletionSource<bool>> capturedTaskSources = null;
            bool runDeactivation = false;

            lock(_sync) {
                _count--;
                
                if(_count == 0) {
                    if(_mode == Mode.Closing) {  //!!! as soon as deactivation requested, closed should == true
                        _mode = Mode.Closed;
                        runDeactivation = true; //this allows sneak preview of idleness
                    }
                    else {
                        capturedTaskSources = Interlocked.Exchange(ref _whenIdleTaskSources, new Queue<TaskCompletionSource<bool>>());
                    }
                }
            }

            if(runDeactivation) {
                PerformInner(_fnOnClose, RequestMode.Isolated, true)
                    .SinkExceptions(_exceptionSink);
            }

            _innerReqs?.Leave();
            capturedTaskSources?.ForEach(ts => ts.SetResult(true));
        }


        //and also - current request, if isolated, should be protected from intrusion
        //...


        public void PerformAndForget(Func<Task> fn, RequestMode reqMode = RequestMode.Unspecified)
            => Perform(fn, reqMode).SinkExceptions(_exceptionSink);
        


        public Task Perform(Func<Task> fn, RequestMode reqMode = RequestMode.Unspecified)
            => PerformInner(async () => {
                            await fn();
                            return default(VoidType);
                        }, reqMode);




        public void PerformAndClose(Func<Task> fn = null) 
        {
            _fnOnClose = async () => {
                await fn();
                return default(VoidType);
            };

            bool runDeactivation = false;

            lock(_sync) {
                if(_count == 0) {
                    _mode = Mode.Closed;
                    runDeactivation = true;
                }                
                else {
                    _mode = Mode.Closing;
                }
            }
            
            if(runDeactivation) {
                PerformInner(_fnOnClose, RequestMode.Isolated, true)
                    .SinkExceptions(_exceptionSink);
            }

            //should use TCS to communicate back when deactivation done
            //...        
        }




        public Task<T> Perform<T>(Func<Task<T>> fn, RequestMode mode = RequestMode.Unspecified)
            => PerformInner<T>(fn, mode, false);





        SemaphoreSlim _smActive = new SemaphoreSlim(1);
        
        async Task<T> PerformInner<T>(Func<Task<T>> fn, RequestMode mode, bool isDeactivation = false)
        {
            bool isolated = mode == RequestMode.Isolated
                            || (mode == RequestMode.Unspecified && _defaultIsolation);

            Enter(isDeactivation);
            
            await _smActive.WaitAsync(); 

            EnterInner();               
                                         
            if(isolated) {
                await WhenInnerClear();
            }                            
            else {                       
                _smActive.Release();     
            }


            TaskCompletionSource<T> _return = new TaskCompletionSource<T>();

            var task = new Task<Task<T>>(fn);

            task.Start(_scheduler);

            task.Unwrap()
                .ContinueWith(t => {
                    LeaveInner();

                    if(isolated) _smActive.Release();

                    if(t.IsFaulted) {
                        _return.SetException(t.Exception);
                    }
                    else {
                        _return.SetResult(t.Result);
                    }

                    Leave(); 
                             
                }, _scheduler)
                .SinkExceptions(_exceptionSink);

            return await _return.Task;
        }

                

        public Task WhenIdle() {
            lock(_sync) {
                if(_count == 0) {
                    return Task.CompletedTask;
                }
                
                var source = new TaskCompletionSource<bool>();
                _whenIdleTaskSources.Enqueue(source);

                return source.Task;
            }
        }



        void IDisposable.Dispose() {
            _disposed = true;
            _smActive.Dispose();
        }


    }


}
