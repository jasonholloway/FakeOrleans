using Orleans;
using Orleans.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Collections.Concurrent;

namespace FakeOrleans.Grains
{
    using Components;
    using FnProxifier = Func<Fixture, AbstractKey, GrainProxy>;



    public class GrainProxyCtx
    {
        public readonly IDispatcher Dispatcher;
        public readonly FakeSerializer Serializer;

        public GrainProxyCtx(IDispatcher dispatcher, FakeSerializer serializer) {
            Dispatcher = dispatcher;
            Serializer = serializer;
        }
    }


    public abstract class GrainProxy : Grain {    //inheriting Grain is a hack in order to use Orleans extensions methods nicely
        
        public Fixture Fixture;
        public AbstractKey Key;

        public abstract Type GrainType { get; }
        
        protected GrainProxy(Fixture fx, AbstractKey key) {
            Fixture = fx;
            Key = key;
        }

        public IGrainIdentity Identity {
            get { return Key; }
        }

        
        protected Task<TResult> Dispatch<TResult>(MethodInfo method, object[] args) 
        {
            var argData = new byte[args.Length][];

            for(int i = 0; i< args.Length; i++) {
                var arg = args[i];

                //BELOW SHOULD BE IN GENERAL SERIALIZER, RATHER THAN AD-HOC HERE !!!!!!!!!!!!!!!
                //so serializer will be sensitive to grains - seems reasonable...

                //proxify before passing to grain method
                if(arg is Grain && !(arg is GrainProxy)) {  //nb GrainProxy derives from Grain these days, oddly
                    var argKey = ((IGrain)arg).GetGrainKey(); //NEED TO BURROW IN TO GRAINRUNTIME - WHICH WILL BE GRAINHARNESS
                    
                    var param = method.GetParameters()[i];

                    var grainKey = new AbstractKey(param.ParameterType, argKey.Id);
                                        
                    arg = Proxify(Fixture, grainKey);
                }
                                
                argData[i] = Fixture.Serializer.Serialize(arg);
            }
            
            return Fixture.Dispatcher.Dispatch(Key, a => a.Invoke<TResult>(method, argData));
        }


        public override string ToString() => $"Proxy:{Key}";



        #region Static

        static ConcurrentDictionary<Type, FnProxifier> _dProxifiers = new ConcurrentDictionary<Type, FnProxifier>();


        public static GrainProxy Proxify(Fixture fx, AbstractKey key) {
            var proxifier = _dProxifiers.GetOrAdd(
                                            key.AbstractType,
                                            t => BuildProxifier(t));
            
            return proxifier(fx, key);
        }

               


        static MethodInfo _mProxyGetGrain = typeof(GrainProxy)
                                                .GetMethod("GetGrain", BindingFlags.NonPublic | BindingFlags.Instance);

        static MethodInfo _mProxyGetMediator = typeof(GrainProxy)
                                                .GetMethod("GetCallMediator", BindingFlags.NonPublic | BindingFlags.Instance);

        static MethodInfo _mgProxyDispatch = typeof(GrainProxy)
                                                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                                                .First(m => m.Name == "Dispatch" && m.IsGenericMethodDefinition);


        //should proxy a certain interface, and then in construction take a certain lambda to handle each call
        //...

        
        public static FnProxifier BuildProxifier(Type tGrain) {
            var tProxy = DynaType.Design(x => {
                //x.Name = $"{tGrain.Name}_Wrapper"; //need to get nice names of types
                
                x.BaseType = typeof(GrainProxy<>).MakeGenericType(tGrain);
                x.Attributes |= TypeAttributes.Serializable | TypeAttributes.Public;

                x.Constructor()
                    .ArgTypes(typeof(Fixture), typeof(AbstractKey))
                    .PassThroughToBaseCtor();
                
                var grainInterfaces = new[] { tGrain }.Concat(tGrain.GetInterfaces()); //.Where(t => IsGrainInterface(t));
                
                //each interface fulfilled by grain type should be fulfilled by inward delegation --------------
                foreach(var tInterface in grainInterfaces) {

                    var methods = tInterface.GetMethods();

                    if(!methods.All(m => m.ReturnType.IsTaskType())) {
                        throw new InvalidOperationException($"Can't proxy type {tInterface}, as not all of its methods return tasks!");
                    }
                    
                    x.AddInterface(tInterface);

                    foreach(var m in methods) {
                        
                        var taskReturnType = GetTaskReturnType(m.ReturnType);
                        var mProxyDispatch = _mgProxyDispatch.MakeGenericMethod(taskReturnType); 

                        var fMethod = x.StaticField($"__{Guid.NewGuid()}", typeof(MethodInfo))
                                            .Value(m);

                        var rParams = m.GetParameters();

                        x.OverrideMethod(m)
                            .Emit(il => {
                                //Delegate to GrainProxy.Dispatch(), via packaging of args into object array
                                il.Emit(OpCodes.Ldarg_0);
                                il.Emit(OpCodes.Ldsfld, fMethod);
                                
                                il.Emit(OpCodes.Ldc_I4, rParams.Length);
                                il.Emit(OpCodes.Newarr, typeof(object));

                                foreach(var p in rParams) {
                                    il.Emit(OpCodes.Dup);
                                    il.Emit(OpCodes.Ldc_I4, p.Position);
                                    il.Emit(OpCodes.Ldarg, p.Position + 1);

                                    if(p.ParameterType.IsValueType) {
                                        il.Emit(OpCodes.Box, p.ParameterType);
                                    }

                                    il.Emit(OpCodes.Stelem, typeof(object));
                                }
                                
                                il.Emit(OpCodes.Call, mProxyDispatch);

                                il.Emit(OpCodes.Ret);                                
                            });
                    }

                }

            });


            var exFixtureParam = Expression.Parameter(typeof(Fixture));
            var exKeyParam = Expression.Parameter(typeof(AbstractKey));

            var exLambda = Expression.Lambda<FnProxifier>(
                                        Expression.New(
                                            tProxy.GetConstructor(
                                                        new[] { typeof(Fixture), typeof(AbstractKey) }),
                                            exFixtureParam,
                                            exKeyParam
                                            ),
                                        exFixtureParam,
                                        exKeyParam);

            return exLambda.Compile();
        }


        static bool IsGrainInterface(Type type) {
            return type.IsInterface
                    && type.IsAssignableTo<IGrain>();
        }

        static Type GetTaskReturnType(Type taskType) {
            if(taskType.Equals(typeof(Task))) {
                return typeof(VoidType);
            }

            return taskType.GetGenericArguments().Single();
        }

        #endregion

    }

    
    public abstract class GrainProxy<TGrain> : GrainProxy, IGrain, IEquatable<TGrain> 
    {        
        public GrainProxy(Fixture fx, AbstractKey key) 
            : base(fx, key) 
            { }


        public override Type GrainType {
            get { return typeof(TGrain); }
        }


        public TGrain Grain {
            get { return (TGrain)(object)Fixture.Dispatcher.Dispatch(Key, a => Task.FromResult(a.Grain)).Result; } //for debugging only!
        }



        bool IEquatable<TGrain>.Equals(TGrain other) {
            var x = (GrainProxy<TGrain>)(object)other;
            return x != null && AbstractKeyComparer.Instance.Equals(Key, x.Key);
        }

        public override bool Equals(object obj) {
            if(obj is GrainProxy<TGrain>) {
                return AbstractKeyComparer.Instance.Equals(Key, ((GrainProxy<TGrain>)obj).Key);
            }

            return false;
        }

        public override int GetHashCode() {
            return AbstractKeyComparer.Instance.GetHashCode(Key) + 13;
        }
        
        
    }


}
