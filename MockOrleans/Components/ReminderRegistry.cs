using Orleans.Timers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans.Runtime;
using System.Collections.Concurrent;
using Orleans;
using System.Reflection;
using System.Threading;

namespace MockOrleans
{    
        
    public class ReminderRegistry
    {
        MockFixture _fx;
        ConcurrentDictionary<GrainKey, GrainReminderRegistry> _dRegistries;

        public ReminderRegistry(MockFixture fx) {
            _fx = fx;
            _dRegistries = new ConcurrentDictionary<GrainKey, GrainReminderRegistry>();
        }


        public GrainReminderRegistry GetRegistry(GrainKey key)
            => _dRegistries.GetOrAdd(key, k => new GrainReminderRegistry(_fx, k));


        public void CancelAll()
            => _dRegistries.Values.ToArray().ForEach(r => r.CancelAll());
        
        public void FireAndCancelAll()
            => _dRegistries.Values.ToArray().ForEach(r => r.FireAndCancelAll());
        

        double _speed = 1;
        object _speedSync = new object();

        public double Speed {
            get { lock(_speedSync) return _speed; }
            set { lock(_speedSync) _speed = value; }
        }

    }



    public class GrainReminderRegistry : IReminderRegistry
    {
        MockFixture _fx;
        GrainKey _grainKey;
        ConcurrentDictionary<string, Reminder> _reminders = new ConcurrentDictionary<string, Reminder>();


        public GrainReminderRegistry(MockFixture fx, GrainKey grainKey) {
            _fx = fx;
            _grainKey = grainKey;
        }

        
        public Task<IGrainReminder> GetReminder(string reminderName) 
        {
            Reminder reminder = null;

            if(_reminders.TryGetValue(reminderName, out reminder)) {
                return Task.FromResult((IGrainReminder)reminder);
            }

            return null;
        }
        

        public Task<List<IGrainReminder>> GetReminders()
            => Task.FromResult(_reminders.Values.OfType<IGrainReminder>().ToList());
        

        public async Task<IGrainReminder> RegisterOrUpdateReminder(string reminderName, TimeSpan dueTime, TimeSpan period) 
        {
            Require.That(period >= TimeSpan.FromMinutes(1), "Reminder period must be at least one minute!");

            await UnregisterReminder(reminderName);
            
            var reminder = new Reminder(_fx, _grainKey, reminderName);

            _reminders[reminderName] = reminder;
            
            _fx.Requests.Perform(() => reminder.Schedule(dueTime, period)); //but scheduling should be tracked - the first execution, less so...
            
            return reminder;
        }
                

        Task UnregisterReminder(string name) 
        {
            Reminder rem;

            if(_reminders.TryRemove(name, out rem)) {
                rem.Clear();
            }

            return Task.CompletedTask;
        }


        public Task UnregisterReminder(IGrainReminder reminder)
            => UnregisterReminder(reminder.ReminderName);
        
        
        public void CancelAll() {
            var reminders = Interlocked.Exchange(ref _reminders, new ConcurrentDictionary<string, Reminder>());
            reminders.Values.ForEach(r => r.Clear());
        }
        

        public void FireAndCancelAll() 
        {
            var reminders = Interlocked.Exchange(ref _reminders, new ConcurrentDictionary<string, Reminder>());
            reminders.Values.ForEach(r => r.FireAndClear());
        }


    }

    


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


        public Reminder(MockFixture fx, GrainKey key, string name) {
            _fx = fx;
            _key = key;
            _name = name;

            _status = ReminderState.Normal;
            _cancelToken = _cancelTokenSource.Token;
        }


        static MethodInfo _mReceiveRemindable = typeof(IRemindable).GetMethod("ReceiveReminder");
        

        public async Task Schedule(TimeSpan due, TimeSpan period)
        {
            var adjustedDue = TimeSpan.FromMilliseconds(due.TotalMilliseconds / _fx.Reminders.Speed);

            if(adjustedDue > TimeSpan.Zero) {
                try {
                    await Task.Delay(adjustedDue, _cancelTokenSource.Token);
                }
                catch(TaskCanceledException) { }
            }

            var status = _status;

            if(status == ReminderState.Normal) {
                _fx.Requests.Perform(() => Schedule(period, period));
            }

            if(status != ReminderState.Cancelled) {
                _fx.Requests.Perform(async () => {
                    var endpoint = _fx.Grains.GetGrainEndpoint(_key);
                    await endpoint.Invoke<VoidType>(_mReceiveRemindable, new object[] { _name, default(TickStatus) });
                });
            }
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
