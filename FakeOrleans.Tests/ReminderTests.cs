using NUnit.Framework;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans.Runtime;
using System.Collections.Concurrent;

namespace FakeOrleans.Tests
{
    [TestFixture]
    public class ReminderTests
    {
        

        [Test]
        public async Task PendingRemindersCanBeHurried() 
        {
            var fx = new Fixture();
            fx.Types.Map<ISelfReminder, SelfReminder>();

            var resultBag = fx.Services.Inject(new ConcurrentBag<bool>());
            
            var grain = fx.GrainFactory.GetGrain<ISelfReminder>(Guid.Empty);

            await grain.ScheduleReminder();
            
            await fx.Reminders.FireAndCancelAll();
            
            Assert.That(resultBag.Count, Is.EqualTo(1));
        }


        [Test]
        public async Task ReminderDelayNotItselfARequest() 
        {
            var fx = new Fixture();
            fx.Types.Map<ISelfReminder, SelfReminder>();

            var resultBag = fx.Services.Inject(new ConcurrentBag<bool>());

            var grain = fx.GrainFactory.GetGrain<ISelfReminder>(Guid.Empty);

            await grain.ScheduleReminder();

            await fx.Requests.WhenIdle();

            Assert.That(resultBag, Has.Count.EqualTo(0));
        }






        public interface ISelfReminder : IGrainWithGuidKey
        {
            Task ScheduleReminder();
        }


        public class SelfReminder : Grain, ISelfReminder, IRemindable
        {
            Action _fnOnReminder;

            public SelfReminder(ConcurrentBag<bool> resultBag) {
                _fnOnReminder = () => resultBag.Add(true);
            }

            public Task ReceiveReminder(string reminderName, TickStatus status) {
                _fnOnReminder();
                return Task.CompletedTask;
            }

            public Task ScheduleReminder() {
                return RegisterOrUpdateReminder("hello", TimeSpan.FromHours(1), TimeSpan.FromDays(1));
            }
        }





    }
}
