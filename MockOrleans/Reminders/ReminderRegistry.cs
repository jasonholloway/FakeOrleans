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

namespace MockOrleans.Reminders
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

            reminder.Schedule(dueTime, period);
            
            //_fx.Requests.Perform(() => reminder.Schedule(dueTime, period)); //but scheduling should be tracked - the first execution, less so...
            
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

    




}
