using FakeOrleans.Grains;
using FakeOrleans.Components;
using Orleans;
using Orleans.Runtime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FakeOrleans.Reminders
{
    public enum ReminderState
    {
        Normal,
        Cancelled,
        OneShot
    }
    

    public class Reminder : IGrainReminder
    {
        string _name;
        AbstractKey _key;
        Fixture _fx;

        volatile ReminderState _status;

        Queue<Task> _tasks;
        CancellationTokenSource _cancelTokenSource;


        public Reminder(Fixture fx, AbstractKey key, string name) {  //BUT!!! don't reminders function against abstract type -
            _fx = fx;                                               //otherwise, how can we upgrade concrete types without ruining reminders???
            _key = key;
            _name = name;

            _status = ReminderState.Normal;
            _cancelTokenSource = new CancellationTokenSource();

            _tasks = new Queue<Task>();
        }


        static MethodInfo _mReceiveRemindable = typeof(IRemindable).GetMethod("ReceiveReminder");


        SemaphoreSlim _sm = new SemaphoreSlim(1);


        public void Schedule(TimeSpan due, TimeSpan period) 
        {
            var adjustedDue = TimeSpan.FromMilliseconds(due.TotalMilliseconds / _fx.Reminders.Speed);
            
            _sm.Wait();

            try {
                if(_status == ReminderState.Cancelled) return;

                var task = Task.Delay(adjustedDue, _cancelTokenSource.Token)
                            .ContinueWith(async _ => {
                                var status = _status;

                                if(status == ReminderState.Normal) {
                                    Schedule(period, period);
                                }

                                if(status != ReminderState.Cancelled) {
                                    await _fx.Dispatcher.Dispatch(_key, a => ((IRemindable)a.Grain).ReceiveReminder(_name, default(TickStatus)));

                                    //var endpoint = await _fx.Grains.GetGrainEndpoint(_key);
                                    //await endpoint.Invoke<IRemindable>(r => r.ReceiveReminder(_name, default(TickStatus)));                                    
                                }

                            }, _fx.Scheduler)
                            .Unwrap();

                _tasks.Enqueue(task); //non-drastic memory leak
            }
            finally {
                _sm.Release();
            }
        }

        

        public Task ClearAndWait() 
        {
            _status = ReminderState.Cancelled;
            _cancelTokenSource.Cancel();
            
            _sm.Wait();

            try {
                return Task.WhenAll(_tasks);
            }
            finally {
                _sm.Release();
            }
        }


        public Task FireClearAndWait() 
        {
            _status = ReminderState.OneShot;
            _cancelTokenSource.Cancel();

            _sm.Wait();

            try {
                return Task.WhenAll(_tasks);
            }
            finally {
                _sm.Release();
            }
        }


        public Task FireAndWait() 
        {
            var tokenSource = Interlocked.Exchange(ref _cancelTokenSource, new CancellationTokenSource());

            _status = ReminderState.Normal;
            tokenSource.Cancel();

            _sm.Wait();

            try {
                return Task.WhenAll(_tasks);   
            }
            finally {
                _sm.Release();
            }
        }




        string IGrainReminder.ReminderName {
            get { return _name; }
        }

    }



}
