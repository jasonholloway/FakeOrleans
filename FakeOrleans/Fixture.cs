using FakeOrleans.Components;
using FakeOrleans.Grains;
using FakeOrleans.Reminders;
using FakeOrleans.Streams;
using Orleans;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeOrleans
{
    
    public class Fixture
    {
        public readonly FixtureScheduler Scheduler;
        public readonly RequestRunner Requests;
        public readonly ExceptionSink Exceptions;
        
        public readonly TypeMap Types;
        public readonly FakeSerializer Serializer;
        
        public readonly ProviderRegistry Providers;
        public readonly ReminderRegistry Reminders;
        public readonly StorageRegistry Stores;
        public readonly StreamRegistry Streams;

        public readonly ServiceRegistry Services;
        public readonly IGrainFactory GrainFactory;

        public readonly IGrainSet Grains;
        public readonly IDispatcher Dispatcher;



        public Fixture(IServiceProvider services = null) 
        {
            Serializer = new FakeSerializer(this);
            Exceptions = new ExceptionSink();
            Scheduler = new FixtureScheduler(Exceptions);
            Requests = new RequestRunner(Scheduler, Exceptions);
            Services = new ServiceRegistry(services);          
            Types = new TypeMap(this);
            GrainFactory = new FakeGrainFactory(this);
            Stores = new StorageRegistry(Serializer);

            Reminders = new ReminderRegistry(this);
            Providers = new ProviderRegistry(this);



            var grainCreator = new GrainFac(Services);


            var actFac = new Func<GrainPlacement, IActivation>(
                                placement => {
                                    var scheduler = new GrainTaskScheduler(Scheduler, Exceptions);
                                    var runner = new RequestRunner(scheduler, Exceptions, Requests, true);
                                    
                                    return new Activation(placement, runner, null);
                                });
            
            var siteFac = new Func<GrainPlacement, IActivationSite>(
                                placement => {
                                    var site = new ActivationSite(actFac);
                                    site.Init(placement);
                                    return site;
                                });
            
            var hub = new ActivationHub(siteFac);

            Grains = hub;
            
            Dispatcher = new Dispatcher(k => new GrainPlacement(k), hub);
            Streams = new StreamRegistry(Dispatcher, Requests, Types);
        }




        public GrainProxy GetGrainProxy(ResolvedGrainKey key) {
            return GrainProxy.Proxify(this, key);
        }
               

    }
}
