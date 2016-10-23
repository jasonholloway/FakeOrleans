using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using FakeOrleans.Grains;

namespace FakeOrleans.Reminders
{    
        
    public class ReminderRegistry
    {
        Fixture _fx;
        ConcurrentDictionary<Placement, GrainReminderRegistry> _dRegistries;

        public ReminderRegistry(Fixture fx) {
            _fx = fx;
            _dRegistries = new ConcurrentDictionary<Placement, GrainReminderRegistry>();
        }


        public GrainReminderRegistry GetRegistry(Placement placement)
            => _dRegistries.GetOrAdd(placement, p => new GrainReminderRegistry(_fx, p));


        public Task CancelAll()
            => _dRegistries.Values.ToArray()
                    .Select(r => r.CancelAll())
                    .WhenAll();
        
        public Task FireAndCancelAll()
            => _dRegistries.Values.ToArray()
                    .Select(r => r.FireAndCancelAll())
                    .WhenAll();

        public Task FireAll()
            => _dRegistries.Values.ToArray()
                    .Select(r => r.FireAll())
                    .WhenAll();



        double _speed = 1;
        object _speedSync = new object();

        public double Speed {
            get { lock(_speedSync) return _speed; }
            set { lock(_speedSync) _speed = value; }
        }

    }
    


}
