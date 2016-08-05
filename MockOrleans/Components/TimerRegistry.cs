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
        TaskScheduler _scheduler;
        GrainHarness _harness;

        object _sync = new object();
        ConcurrentBag<Task> _timers = new ConcurrentBag<Task>();
        

        public MockTimerRegistry(GrainHarness harness) 
        {
            _harness = harness;
            _scheduler = harness.Scheduler;
        }
        


        class DisposableLink : IDisposable
        {
            public readonly IDisposable This;
            public IDisposable Next = null;

            public DisposableLink(IDisposable thisDisp) {
                This = thisDisp;
            }

            public void Dispose() {
                This.Dispose();
                Next?.Dispose();
            }
        }


        public IDisposable RegisterTimer(Grain grain, Func<object, Task> fn, object state, TimeSpan dueTime, TimeSpan period) 
        {
            var task = new Task<Task>(fn, state);
            
            Task.Delay(dueTime)
                .ContinueWith(_ => {
                    task.Start(_scheduler);
                }, TaskScheduler.Default);

            var fullTask = task.Unwrap(); //and exception handler???
            var dispLink = new DisposableLink(fullTask);

            if(period > TimeSpan.Zero) {
                fullTask.ContinueWith(_ => {
                    dispLink.Next = RegisterTimer(grain, fn, state, period, period);
                }, TaskScheduler.Default);
            }

            _timers.Add(fullTask);
            _harness.Fixture.RegisterTask(fullTask); //but won't all this be registered through scheduler? eventually, yes...

            return dispLink;            
        }


        public void Clear() {
            lock(_sync) {
                _timers.ForEach(t => t.Dispose()); //bit abrupt and nasty - timers should self-dispose given deactivation
                _timers = new ConcurrentBag<Task>();
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
