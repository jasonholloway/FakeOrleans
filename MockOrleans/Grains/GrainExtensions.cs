using MockOrleans.Grains;
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

namespace MockOrleans
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






        public static GrainKey GetConcreteGrainKey(this IGrain @this) {
            if(@this is GrainProxy) {
                return ((GrainProxy)@this).Key;
            }

            if(@this is Grain) {
                var harness = (GrainHarness)ExtractGrainRuntimeFrom(@this);
                return harness.Key;
            }

            //otherwise have to delve into private stuff - orleans etc etc
            //...

            return new GrainKey(
                        @this.GetConcreteGrainType(),
                        @this.ExtractKey()
                        );
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











        ///// <summary>
        ///// Use instead of IGrain.AsReference() and IGrain.Cast()!
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="this"></param>
        ///// <returns></returns>
        //public static T CastAs<T>(this IGrain @this) where T : IGrain {
        //    //in place to short-circuit Orleans when testing; should always be used
        //    if(@this.IsMocked()) {
        //        if(@this is GrainProxy) {
        //            return (T)@this;
        //        }

        //        if(@this is Grain) {
        //            var runtime = ((GrainHarness)ExtractGrainRuntimeFrom(@this)).Runtime;
        //            var concreteKey = @this.GetConcreteGrainKey();

        //            var grainKey = new ResolvedGrainKey(typeof(T), concreteKey.ConcreteType, concreteKey.Key);

        //            return (T)(object)runtime.GetGrainProxy(grainKey);
        //        }
        //    }

        //    return @this.AsReference<T>();
        //}


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
