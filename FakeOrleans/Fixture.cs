using FakeOrleans.Components;
using FakeOrleans.Grains;
using FakeOrleans.Reminders;
using FakeOrleans.Streams;
using MockOrleans.Components;
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

        public readonly IGrainSet Activations;
        public readonly IDispatcher Dispatcher;
                

        public Fixture(IServiceProvider services = null) 
        {
            var proxifier = new Func<ResolvedGrainKey, IGrain>(
                                   key => (IGrain)GrainProxy.Proxify(this, key)); //just needs dispatcher and serializer
                                                
            Exceptions = new ExceptionSink();
            Scheduler = new FixtureScheduler(Exceptions);
            Serializer = new FakeSerializer(proxifier);
            Types = new TypeMap();
            GrainFactory = new FakeGrainFactory(Types, proxifier);
            Requests = new RequestRunner(Scheduler, Exceptions);            
            Services = new ServiceRegistry(services);
            Stores = new StorageRegistry(Serializer);
            Providers = new ProviderRegistry(() => new ProviderRuntimeAdaptor(GrainFactory, Services, null));

            Reminders = new ReminderRegistry(this);
            
            Activations = new ActivationHub(place => {
                                            var actSite = new ActivationSite(p => ActivationFac.Create(this, p));
                                            actSite.Init(place);
                                            return actSite;
                                        });
            
            Dispatcher = new Dispatcher(k => new GrainPlacement(k), (IPlacementDispatcher)Activations);
            Streams = new StreamRegistry(Dispatcher, Requests, Types);
        }

        
    }





    //public class FixtureCtx
    //{
    //    public TaskScheduler Scheduler;
    //    public RequestRunner Runner;
    //    public ExceptionSink Exceptions;
    //    public IServiceProvider Services;
    //    public IGrainFactory GrainFactory;
    //    public ReminderRegistry Reminders;
    //    public StreamRegistry Streams;
    //    public StorageRegistry Store;
    //    public FakeSerializer Serializer;
    //    public ProviderRegistry Providers;
    //    public TypeMap Types;

    //    public Func<ResolvedGrainKey, IGrain> Proxifier;

    //    public Func<FixtureCtx, GrainPlacement, IActivation> ActivationFac;
    //    public Func<ActivationCtx, IActivation, Grain> GrainFac;
    //}



}
