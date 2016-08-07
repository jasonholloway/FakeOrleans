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


        public void ClearAll()
            => _dRegistries.Values.ToArray().ForEach(r => r.Clear());


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

            //_fx.Tasks.Register(reminder.Schedule(dueTime, period));

            return reminder;
        }
                

        Task UnregisterReminder(string name) 
        {
            Reminder rem;

            if(_reminders.TryRemove(name, out rem)) {
                rem.Dispose();
            }

            return Task.FromResult(true);
        }


        public Task UnregisterReminder(IGrainReminder reminder)
            => UnregisterReminder(reminder.ReminderName);
        

        public void Clear()
            => _reminders.Values.ToArray().ForEach(r => r.Dispose());

    }





    public class Reminder : IGrainReminder, IDisposable
    {
        string _name;
        GrainKey _key;
        MockFixture _fx;
        CancellationTokenSource _cancelTokenSource = new CancellationTokenSource();
        CancellationToken _cancelToken; 


        public Reminder(MockFixture fx, GrainKey key, string name) {
            _fx = fx;
            _key = key;
            _name = name;
            _cancelToken = _cancelTokenSource.Token;
        }


        static MethodInfo _mReceiveRemindable = typeof(IRemindable).GetMethod("ReceiveReminder");
        

        public async Task Schedule(TimeSpan due, TimeSpan period)  //BEWARE! - must be called in default task scheduling context
        {
            if(_cancelToken.IsCancellationRequested) return;

            var adjustedDue = TimeSpan.FromMilliseconds(due.TotalMilliseconds / _fx.Reminders.Speed);
            
            try {
                await Task.Delay(adjustedDue, _cancelTokenSource.Token);
            }
            catch(TaskCanceledException) {
                return;
            }
            
            if(_cancelToken.IsCancellationRequested) return;

            var endpoint = _fx.Silo.GetGrainEndpoint(_key);            
            await endpoint.Invoke<VoidType>(_mReceiveRemindable, new object[] { _name, default(TickStatus) });

            await Schedule(period, period);            
        }


        public void Dispose() {
            _cancelTokenSource.Cancel();
        }


        string IGrainReminder.ReminderName {
            get { return _name; }
        }

    }



}
