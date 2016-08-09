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

namespace MockOrleans.Grains
{
    //using FnConstructor = Func<IGrainIdentity, IGrainRuntime, IGrain>;
    using FnStorageAssigner = Action<Grain, IStorage>;
    using FnStateExtractor = Func<Grain, IGrainState>;


    class StorageProviderAdaptor : IStorageProvider
    {
        GrainKey _key;
        IStateStore _store;

        public StorageProviderAdaptor(GrainKey key, IStateStore store) {
            _key = key;
            _store = store;
        }

        public Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
            => _store.Clear(_key);
        
        public Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
            => _store.ReadFrom(_key, grainState);

        public Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
            => _store.WriteTo(_key, grainState);

        #region IProvider

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

        public Task Close() {
            throw new NotImplementedException();
        }

        public Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config) {
            throw new NotImplementedException();
        }

        #endregion
    }
    

    public static class GrainActivator
    {
        static ConcurrentDictionary<Type, FnStorageAssigner> _dStorageAssigners = new ConcurrentDictionary<Type, FnStorageAssigner>();
        static ConcurrentDictionary<Type, FnStateExtractor> _dStateExtractors = new ConcurrentDictionary<Type, FnStateExtractor>();


        public static async Task<IGrain> Activate(IGrainRuntime runtime, IStateStore store, GrainKey key)
        {
            //Debug.WriteLine($"Activating grain {key}");
            
            var creator = new GrainCreator(runtime, runtime.ServiceProvider);
            
            var stateType = GetStateType(key.ConcreteType);

            var grain = stateType != null
                            ? creator.CreateGrainInstance(key.ConcreteType, key, stateType, new DummyStorageProvider()) //IStorage will be hackily assigned below      // new StorageProviderAdaptor(key, store))
                            : creator.CreateGrainInstance(key.ConcreteType, key);
            
            
            if(stateType != null) {
                var fnStateExtractor = _dStateExtractors.GetOrAdd(key.ConcreteType, t => BuildStateExtractor(t));
                var fnStorageAssign = _dStorageAssigners.GetOrAdd(key.ConcreteType, t => BuildStorageAssigner(t));

                var grainState = fnStateExtractor(grain);

                var storage = new StoreBridge(store, key, grainState);
                fnStorageAssign(grain, storage);

                await storage.ReadStateAsync();
            }

            await grain.OnActivateAsync();

            return (IGrain)grain;
        }





        static Type GetStateType(Type grainType) 
        {
            var tGenericGrainBase = grainType.GetGenericBaseClass(typeof(Grain<>));
            
            return tGenericGrainBase != null
                        ? tGenericGrainBase.GetGenericArguments().Single()
                        : null;
        }

        

        static FnStateExtractor BuildStateExtractor(Type tGrain) 
        {
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



        static FnStorageAssigner BuildStorageAssigner(Type tGrain) 
        {
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







        class StoreBridge : IStorage
        {
            public IStateStore Store { get; private set; }
            public GrainKey Key { get; private set; }
            public IGrainState State { get; private set; }

            public StoreBridge(IStateStore store, GrainKey key, IGrainState state) {
                Store = store;
                Key = key;
                State = state;
            }

            public Task ClearStateAsync() {
                return Store.Clear(Key);
            }

            public Task WriteStateAsync() {
                return Store.WriteTo(Key, State);
            }

            public Task ReadStateAsync() {
                return Store.ReadFrom(Key, State);
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
}
