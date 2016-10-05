using Orleans;
using Orleans.Core;
using Orleans.Runtime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Reflection;
using Orleans.Storage;
using Orleans.Providers;
using Orleans.Timers;
using Orleans.Streams;
using FakeOrleans.Streams;

namespace FakeOrleans.Grains
{
    using FnStorageAssigner = Action<Grain, IStorage>;
    using FnStateExtractor = Func<Grain, IGrainState>;

       

    public static class GrainPrimer
    {
        static ConcurrentDictionary<Type, FnStorageAssigner> _dStorageAssigners = new ConcurrentDictionary<Type, FnStorageAssigner>();
        static ConcurrentDictionary<Type, FnStateExtractor> _dStateExtractors = new ConcurrentDictionary<Type, FnStateExtractor>();
        
        public static async Task<Grain> Build(Activation_New act) 
        {
            var key = act.Placement.Key;
            var grainType = key.ConcreteType;

            var runtime = new FakeGrainRuntime(act);
            
            var creator = new GrainCreator(runtime, act.Fixture.Services);
            
            var stateType = GetStateType(grainType);

            var grain = stateType != null
                            ? creator.CreateGrainInstance(grainType, key, stateType, new DummyStorageProvider()) //IStorage will be hackily assigned below      // new StorageProviderAdaptor(key, store))
                            : creator.CreateGrainInstance(grainType, key);


            if(stateType != null) {
                var fnStateExtractor = _dStateExtractors.GetOrAdd(grainType, t => BuildStateExtractor(t));
                var fnStorageAssign = _dStorageAssigners.GetOrAdd(grainType, t => BuildStorageAssigner(t));

                var grainState = fnStateExtractor(grain);

                var bridge = new GrainStorageBridge(act.Storage, act.Serializer, grainState);
                fnStorageAssign(grain, bridge);

                await bridge.ReadStateAsync();
            }
            
            return grain;
        }





        static Type GetStateType(Type grainType) {
            var tGenericGrainBase = grainType.GetGenericBaseClass(typeof(Grain<>));

            return tGenericGrainBase != null
                        ? tGenericGrainBase.GetGenericArguments().Single()
                        : null;
        }



        static FnStateExtractor BuildStateExtractor(Type tGrain) {
            var exGrainParam = Expression.Parameter(typeof(Grain));

            var tBaseGrain = tGrain.GetGenericBaseClass(typeof(Grain<>));
            var fGrainState = tBaseGrain.GetField("grainState", BindingFlags.Instance | BindingFlags.NonPublic);

            var exLambda = Expression.Lambda<FnStateExtractor>(
                                Expression.MakeMemberAccess(
                                            Expression.Convert(exGrainParam, tGrain),
                                            fGrainState),
                                exGrainParam);

            return exLambda.Compile();
        }



        static FnStorageAssigner BuildStorageAssigner(Type tGrain) {
            var exGrainParam = Expression.Parameter(typeof(Grain));
            var exStorageParam = Expression.Parameter(typeof(IStorage));

            var exLambda = Expression.Lambda<FnStorageAssigner>(
                                Expression.Block(
                                    Expression.Call(
                                                Expression.Convert(exGrainParam, _tStatefulGrain),
                                                _mSetStorage,
                                                exStorageParam)
                                    ),
                                exGrainParam,
                                exStorageParam
                                );

            return exLambda.Compile();
        }




        static T Exec<T>(Func<T> fn) { return fn(); }


        static PropertyInfo GetInternalGrainProp(string name) {
            return typeof(Grain).GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance);
        }


        static Type _tStatefulGrain = typeof(Grain).Assembly.GetType("Orleans.IStatefulGrain");
        static PropertyInfo _pGrainState = _tStatefulGrain.GetProperty("GrainState");
        static MethodInfo _mSetStorage = _tStatefulGrain.GetMethod("SetStorage");
        static PropertyInfo _pGrainStateState = typeof(IGrainState).GetProperty("State");




        class GrainStorageBridge : IStorage
        {
            public readonly StorageCell StorageCell;
            public readonly FakeSerializer Serializer;
            public readonly IGrainState State;

            public GrainStorageBridge(StorageCell storageCell, FakeSerializer serializer, IGrainState state) {
                StorageCell = storageCell;
                Serializer = serializer;
                State = state;
            }

            public Task ClearStateAsync() {
                StorageCell.Clear();
                return Task.CompletedTask;
            }

            public Task WriteStateAsync() {
                StorageCell.Write(State, Serializer);
                return Task.CompletedTask;
            }

            public Task ReadStateAsync() {
                StorageCell.Read(State, Serializer);
                return Task.CompletedTask;
            }
        }



        class DummyStorageProvider : IStorageProvider
        {
            public Logger Log {
                get {
                    throw new NotImplementedException();
                }
            }

            public string Name {
                get {
                    throw new NotImplementedException();
                }
            }

            public Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState) {
                throw new NotImplementedException();
            }

            public Task Close() {
                throw new NotImplementedException();
            }

            public Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config) {
                throw new NotImplementedException();
            }

            public Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState) {
                throw new NotImplementedException();
            }

            public Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState) {
                throw new NotImplementedException();
            }
        }

    }







    public class FakeGrainRuntime : IGrainRuntime
    {
        readonly Activation_New _act;
        
        public FakeGrainRuntime(Activation_New act) {
            _act = act;
        }
        
        public Guid ServiceId { get; } = Guid.NewGuid();
        public string SiloIdentity { get; } = "SiloIdentity";

        public IServiceProvider ServiceProvider {
            get { return _act.Fixture.Services; }
        }

        public IGrainFactory GrainFactory {
            get { return _act.Fixture.GrainFactory; }
        }
        
        public ITimerRegistry TimerRegistry {
            get { return _act.Timers; }
        }

        public IReminderRegistry ReminderRegistry {
            get { return _act.Fixture.Reminders.GetRegistry(_act.Placement.Key); }
        }

        public IStreamProviderManager StreamProviderManager {
            get { return new StreamProviderManagerAdaptor(_act); }
        }
        
        public void DeactivateOnIdle(Grain grain) {
            _act.Dispatcher.Deactivate()
                .SinkExceptions(_act.Fixture.Exceptions);
        }

        public void DelayDeactivation(Grain grain, TimeSpan timeSpan) {
            throw new NotImplementedException();
        }

        public Logger GetLogger(string loggerName) {
            throw new NotImplementedException();
        }
        
    }





}
