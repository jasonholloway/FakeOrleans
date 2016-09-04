using Orleans.Runtime;
using Orleans.Timers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FakeOrleans.Reminders
{
    
    public class GrainReminderRegistry : IReminderRegistry
    {
        Fixture _fx;
        GrainKey _grainKey;
        ConcurrentDictionary<string, Reminder> _reminders = new ConcurrentDictionary<string, Reminder>();


        public GrainReminderRegistry(Fixture fx, GrainKey grainKey) {
            _fx = fx;
            _grainKey = grainKey;
        }


        public Task<IGrainReminder> GetReminder(string reminderName) {
            Reminder reminder = null;

            if(_reminders.TryGetValue(reminderName, out reminder)) {
                return Task.FromResult((IGrainReminder)reminder);
            }

            return null;
        }


        public Task<List<IGrainReminder>> GetReminders()
            => Task.FromResult(_reminders.Values.OfType<IGrainReminder>().ToList());


        public async Task<IGrainReminder> RegisterOrUpdateReminder(string reminderName, TimeSpan dueTime, TimeSpan period) {
            Require.That(period >= TimeSpan.FromMinutes(1), "Reminder period must be at least one minute!");

            await UnregisterReminder(reminderName);

            var reminder = new Reminder(_fx, _grainKey, reminderName);
            _reminders[reminderName] = reminder;

            reminder.Schedule(dueTime, period);

            return reminder;
        }


        Task UnregisterReminder(string name) {
            Reminder rem;

            if(_reminders.TryRemove(name, out rem)) {
                rem.ClearAndWait();
            }

            return Task.CompletedTask;
        }


        public Task UnregisterReminder(IGrainReminder reminder)
            => UnregisterReminder(reminder.ReminderName);


        public Task CancelAll() {
            var reminders = Interlocked.Exchange(ref _reminders, new ConcurrentDictionary<string, Reminder>());

            return reminders.Values
                    .Select(r => r.ClearAndWait())
                    .WhenAll();
        }


        public Task FireAndCancelAll() {
            var reminders = Interlocked.Exchange(ref _reminders, new ConcurrentDictionary<string, Reminder>());

            return reminders.Values
                    .Select(r => r.FireClearAndWait())
                    .WhenAll();
        }

        public Task FireAll() {
            var reminders = _reminders;

            return reminders.Values
                    .Select(r => r.FireAndWait())
                    .WhenAll();
        }

    }


}
