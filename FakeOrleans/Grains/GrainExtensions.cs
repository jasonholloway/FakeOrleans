using FakeOrleans.Grains;
using Orleans;
using Orleans.Core;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FakeOrleans
{
    public static class GrainExtensions
    {

        public static Type GetConcreteGrainType(this IGrain @this) {
            if(@this is GrainProxy) {
                return ((GrainProxy)@this).GrainType;
            }

            if(@this is Grain) {
                return @this.GetType();
            }

            throw new NotImplementedException("Not handling current type at mo!");
        }






        public static AbstractKey GetGrainKey(this IAddressable @this) {
            if(@this is GrainProxy) {
                return ((GrainProxy)@this).Key;
            }

            throw new InvalidOperationException("Can only get grain key of proxies!");

            //if(@this is Grain) { //can't get grainkey from a grain...
            //    var harness = (GrainHarness)ExtractGrainRuntimeFrom(@this);
            //    return harness.Placement.Key;
            //}

            //otherwise have to delve into private stuff - orleans etc etc
            //...

            //return new GrainKey(
            //            @this.GetConcreteGrainType(),
            //            @this.ExtractKey()
            //            );
        }




        static IGrainRuntime ExtractGrainRuntimeFrom(IGrain @this) {
            if(@this is GrainProxy) {
                throw new NotImplementedException(); //doesn't make sense - GrainRuntime is inner-grain business
                //return (IGrainRuntime)((GrainProxy)@this).Runtime;
            }

            if(@this is Grain) {
                //get from introspection - could be either mock or orleans
                return _lzFnExtractRuntime.Value((Grain)@this);
            }
            
            throw new NotImplementedException();
        }


        static Lazy<Func<Grain, IGrainRuntime>> _lzFnExtractRuntime
            = new Lazy<Func<Grain, IGrainRuntime>>(() => {

                var exGrainParam = Expression.Parameter(typeof(Grain));

                var exLambda = Expression.Lambda<Func<Grain, IGrainRuntime>>(
                                            Expression.MakeMemberAccess(
                                                        exGrainParam,
                                                        typeof(Grain).GetProperty("Runtime", BindingFlags.NonPublic | BindingFlags.Instance) //beware renamings in Orleans, changing to a prop, etc.
                                                        ),
                                            exGrainParam);

                return exLambda.Compile();
            });








        static IGrainFactory ExtractGrainFactoryFrom(IGrain @this) {
            if(@this is GrainProxy) {
                return ((GrainProxy)@this).Fixture.GrainFactory;
            }

            if(@this is Grain) {
                return _lzFnExtractGrainFactory.Value((Grain)@this);
            }

            throw new NotImplementedException("Not handling current type at mo!");
        }



        static Lazy<Func<Grain, IGrainFactory>> _lzFnExtractGrainFactory
            = new Lazy<Func<Grain, IGrainFactory>>(() => {

                var exGrainParam = Expression.Parameter(typeof(Grain));

                var exLambda = Expression.Lambda<Func<Grain, IGrainFactory>>(
                                            Expression.MakeMemberAccess(
                                                        exGrainParam,
                                                        typeof(Grain).GetProperty("GrainFactory", BindingFlags.NonPublic | BindingFlags.Instance) //beware renamings in Orleans, changing to a prop, etc.
                                                        ),
                                            exGrainParam);

                return exLambda.Compile();
            });











        /// <summary>
        /// Use instead of IGrain.AsReference() and IGrain.Cast()!
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="this"></param>
        /// <returns></returns>
        public static T CastAs<T>(this IGrain @this) where T : IGrain {
            //in place to short-circuit Orleans when testing; should always be used

            var proxy = @this as GrainProxy;

            if(proxy != null) {
                return (T)(object)proxy; //eeek!
            }

            var grain = @this as Grain;

            if(grain != null) {

                //need to proxy here - but how can we know what key, and what interface(s), we are proxying?
                //the grain itself is kind of agnostic to this. Except that it isn't - it will need to carry 
                //round its 'activation context' where it clings still to the data that begot it.

                //A placement, as well as carrying concrete type, therefore also needs to carry the initializing abstract key.
                //But what then to do with other keys that resolve to the same?

                //Or do we proxify with the full set of interfaces the grain supports? Can't do - as the resolution logic may be different for other interfaces -
                //roundtripping may not be possible, and therefore the intended reference will point elsewhere...


                throw new NotImplementedException("Need to give access to key via GrainRuntime extraction");

                //var harness = ExtractGrainRuntimeFrom(@this) as GrainHarness; //the GrainRuntime needs to give access to 
                
                //if(harness != null) {
                //    var concreteKey = @this.GetGrainKey();

                //    var grainKey = new ResolvedKey(typeof(T), concreteKey.AbstractType, concreteKey.Id);

                //    return (T)(object)harness.Fixture.GetGrainProxy(grainKey);
                //}
            }

            return @this.AsReference<T>();
        }


        /// <summary>
        /// Use instead of IGrain.GetPrimaryKeyString()!
        /// </summary>
        /// <param name="this"></param>
        /// <returns></returns>
        public static Guid ExtractKey(this IGrain @this) {
            if(@this is GrainReference) {
                return @this.GetPrimaryKey();
            }

            if(@this is GrainProxy) {
                var identity = ExtractGrainIdentityFrom(@this);
                return identity.PrimaryKey;
            }

            if(@this is Grain) {
                return @this.GetPrimaryKey();
            }

            return ExtractGrainIdentityFrom(@this).PrimaryKey;
        }



        static IGrainIdentity ExtractGrainIdentityFrom(IGrain @this) {
            if(@this is IGrainIdentity) { //mock proxies expose IGrainIdentity themselves
                return (IGrainIdentity)@this;
            }

            if(@this is GrainProxy) {
                return ((GrainProxy)@this).Identity;
            }

            if(@this is Grain) { //currently Orleans offers no nice way of extraction: it's there, just malignly internal
                return _lzFnExtractIdentity.Value((Grain)@this);
            }

            //and GrainReference, anybody?
            throw new InvalidOperationException("Don't know how to extract IGrainIdentity from such a type!");
        }




        static Lazy<Func<Grain, IGrainIdentity>> _lzFnExtractIdentity
            = new Lazy<Func<Grain, IGrainIdentity>>(() => {

                var exGrainParam = Expression.Parameter(typeof(Grain));

                var exLambda = Expression.Lambda<Func<Grain, IGrainIdentity>>(
                                            Expression.MakeMemberAccess(
                                                        exGrainParam,
                                                        typeof(Grain).GetField("Identity", BindingFlags.NonPublic | BindingFlags.Instance) //beware renamings in Orleans, changing to a prop, etc.
                                                        ),
                                            exGrainParam);

                return exLambda.Compile();
            });

                

        static Lazy<Func<object>> _lzFnGetAmbientRuntime
            = new Lazy<Func<object>>(() => {
                var tRuntimeClient = typeof(Grain).Assembly.GetType("RuntimeClient");
                var pCurrent = tRuntimeClient.GetProperty("Current", BindingFlags.Static | BindingFlags.Public);

                var exLambda = Expression.Lambda<Func<object>>(
                                    Expression.MakeMemberAccess(null, pCurrent)
                                    );

                return exLambda.Compile();
            });












        //public static Type GetAbstractGrainType(this IGrain @this) => Types.FindCanonicalType(@this.GetType());



        //public static string GetTypedKey(this IGrain scene) 
        //{
        //    var type = scene.GetAbstractGrainType();
        //    var token = Types.FindToken(type);

        //    return Keys.EncodeParts(token, scene.ExtractKey().ToString());
        //}



    }
}
