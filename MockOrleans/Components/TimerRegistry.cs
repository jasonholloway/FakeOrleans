using Orleans.Timers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans;
using System.Threading;
using System.Collections.Concurrent;
using System.Diagnostics;
using MockOrleans.Grains;

namespace MockOrleans
{
    //this should all be single-threaded, as accessed only from grain        

    public class MockTimerRegistry : ITimerRegistry, IDisposable
    {
        GrainHarness _harness;
        List<Timer> _timers = new List<Timer>();


        public MockTimerRegistry(GrainHarness harness) {
            _harness = harness;
        }





        class Timer : IDisposable
        {
            Func<Task> _fn;
            GrainHarness _harness;
            CancellationTokenSource _cancelSource;
            CancellationToken _cancelToken;

            public Timer(GrainHarness harness, Func<Task> fn) {
                _harness = harness;
                _fn = fn;
                _cancelSource = new CancellationTokenSource();
                _cancelToken = _cancelSource.Token;
            }

            public void Run(TimeSpan delay, TimeSpan period) {
                if(_cancelToken.IsCancellationRequested) return;

                var task = Task.Delay(delay, _cancelToken)
                                .ContinueWith(_ => _fn(), _cancelToken, TaskContinuationOptions.None, _harness.Scheduler)
                                .Unwrap();

                if(period > TimeSpan.Zero) {
                    task = task.ContinueWith(
                                    _ => Run(period, period),
                                    _cancelToken,
                                    TaskContinuationOptions.None,
                                    TaskScheduler.Default);
                }

                _harness.Fixture.Tasks.Register(task);
            }

            public void Dispose() {
                _cancelSource.Cancel();
            }
        }


        public IDisposable RegisterTimer(Grain grain, Func<object, Task> fn, object state, TimeSpan dueTime, TimeSpan period) {
            var timer = new Timer(_harness, () => fn(state));
            lock(_timers) _timers.Add(timer);

            timer.Run(dueTime, period);

            return timer;
        }


        public void Clear() {
            lock(_timers) {
                _timers.ForEach(t => t.Dispose());
                _timers.Clear();
            }
        }


        #region IDisposable

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if(!disposedValue) {
                if(disposing) {
                    _timers.ForEach(t => t.Dispose());
                }

                disposedValue = true;
            }
        }

        public void Dispose() {
            Dispose(true);
        }

        #endregion


    }
}
