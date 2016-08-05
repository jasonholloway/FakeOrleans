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
        public IServiceProvider Services { get; private set; }
        public IGrainFactory GrainFactory { get; private set; }

        public ReminderRegistry Reminders { get; private set; }
        public ProviderRegistry Providers { get; private set; }
        public ITypeMap Types { get; private set; }
        public IStateStore Store { get; private set; } //should be a registry - implicitly filled
        public StreamRegistry Streams { get; private set; }


        public MockSilo Silo { get; private set; } //should be GrainRegistry...


        public MockFixture(IServiceProvider services) 
        {
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






        ConcurrentQueue<Task> _allTasks = new ConcurrentQueue<Task>();

        public IEnumerable<Task> AllTasks {
            get { return _allTasks.ToArray(); }
        }


        //every time a task is added, we try and remove as many as we can
        //ensure as much churn of queue as possible

        public void RegisterTask(Task task) {
            Task dequeued = null;

            while(_allTasks.TryDequeue(out dequeued) && dequeued.Status == TaskStatus.RanToCompletion) ;

            _allTasks.Enqueue(task);

            if(dequeued != null) {
                _allTasks.Enqueue(dequeued);
            }
        }
        


    }
}
