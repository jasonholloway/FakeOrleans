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

namespace MockOrleans.Reminders
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
        GrainKey _key;
        MockFixture _fx;

        CancellationTokenSource _cancelTokenSource = new CancellationTokenSource();
        CancellationToken _cancelToken;

        volatile ReminderState _status;

        ConcurrentQueue<Task> _tasks;


        public Reminder(MockFixture fx, GrainKey key, string name) {
            _fx = fx;
            _key = key;
            _name = name;

            _status = ReminderState.Normal;
            _cancelToken = _cancelTokenSource.Token;
            _tasks = new ConcurrentQueue<Task>();
        }


        static MethodInfo _mReceiveRemindable = typeof(IRemindable).GetMethod("ReceiveReminder");


        public void Schedule(TimeSpan due, TimeSpan period) 
        {
            var adjustedDue = TimeSpan.FromMilliseconds(due.TotalMilliseconds / _fx.Reminders.Speed);
            
            var task = Task.Delay(adjustedDue, _cancelToken)
                        .ContinueWith(async _ => {
                            var status = _status;

                            if(status == ReminderState.Normal) {
                                Schedule(period, period);
                            }

                            if(status != ReminderState.Cancelled) {
                                var endpoint = _fx.Grains.GetGrainEndpoint(_key);
                                await endpoint.Invoke<VoidType>(_mReceiveRemindable, new object[] { _name, default(TickStatus) });
                            }

                        }, _cancelToken, TaskContinuationOptions.OnlyOnRanToCompletion, _fx.Scheduler)
                        .Unwrap();
            
            _tasks.Enqueue(task);
        }


        public void FireAndClear() {
            _status = ReminderState.OneShot;
            _cancelTokenSource.Cancel();
        }


        public void Clear() {
            _status = ReminderState.Cancelled;
            _cancelTokenSource.Cancel();
        }


        string IGrainReminder.ReminderName {
            get { return _name; }
        }

    }



}
