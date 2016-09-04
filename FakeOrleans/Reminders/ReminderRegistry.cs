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

namespace FakeOrleans.Reminders
{    
        
    public class ReminderRegistry
    {
        Fixture _fx;
        ConcurrentDictionary<GrainKey, GrainReminderRegistry> _dRegistries;

        public ReminderRegistry(Fixture fx) {
            _fx = fx;
            _dRegistries = new ConcurrentDictionary<GrainKey, GrainReminderRegistry>();
        }


        public GrainReminderRegistry GetRegistry(GrainKey key)
            => _dRegistries.GetOrAdd(key, k => new GrainReminderRegistry(_fx, k));


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
