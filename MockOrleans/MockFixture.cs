using MockOrleans.Grains;
using Orleans;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockOrleans
{
    
    public class MockFixture
    {
        public readonly FixtureScheduler Scheduler;
        public readonly RequestRegistry Requests;
        
        public readonly IServiceProvider Services;
        public readonly IGrainFactory GrainFactory;

        public readonly ReminderRegistry Reminders;
        public readonly ProviderRegistry Providers;
        public readonly ITypeMap Types;
        public readonly IStateStore Store;
        public readonly StreamRegistry Streams;
        
        public MockSilo Silo { get; private set; } //should be GrainRegistry...



        public MockFixture(IServiceProvider services) 
        {
            Scheduler = new FixtureScheduler();
            Requests = new RequestRegistry();
            Services = services;          
            Types = new MockTypeMap();
            GrainFactory = new MockGrainFactory(this);
            Store = new MockStateStore(this);
            Streams = new StreamRegistry();
            Reminders = new ReminderRegistry(this);
            Providers = new ProviderRegistry(this);
            Silo = new MockSilo(this);
        }








        public TGrain Inject<TGrain>(GrainKey key, TGrain grain) 
            where TGrain : class, IGrain 
        {
            Silo.Harnesses.AddOrUpdate(key,
                        k => new GrainHarness(this, k, grain),
                        (k, _) => new GrainHarness(this, k, grain));

            var grainKey = new ResolvedGrainKey(typeof(TGrain), key.ConcreteType, key.Key);

            return (TGrain)(object)GetGrainProxy(grainKey);
        }




        public GrainProxy GetGrainProxy(ResolvedGrainKey key) {
            return GrainProxy.Proxify(this, key);
        }
               

    }
}
