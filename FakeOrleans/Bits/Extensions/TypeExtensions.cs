using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FakeOrleans
{
    public static class TypeExtensions
    {

        public static Type GetGrainStateType(this Type @this) {
            var tGenGrain = @this.GetGenericBaseClass(typeof(Grain<>));

            return tGenGrain != null
                    ? tGenGrain.GetGenericArguments().Single()
                    : null;
        }


        public static bool IsStatefulGrainType(this Type @this)
            => @this.GetGrainStateType() != null;


        public static bool IsTaskType(this Type @this)
            => @this.IsAssignableTo<Task>();





        public static MethodInfo GetGenMethod(this Type @this, string name, Type[] genArgs, Type[] argTypes) 
        {
            return @this.GetMethods()
                            .Where(m => m.Name == name)
                            .Select(m => new {
                                Method = m,
                                Params = m.GetParameters(),
                                GenArgs = m.GetGenericArguments()
                            })
                            .Where(t => t.Params
                                            .Select(p => p.ParameterType)
                                            .SequenceEqual(argTypes))
                            .First(t => t.GenArgs
                                           .Select(a => a.GetGenericParameterConstraints().FirstOrDefault())
                                           .SequenceEqual(genArgs))
                            .Method;
        }



        public static bool IsAssignableTo<T>(this Type @this) {
            return typeof(T) == @this || typeof(T).IsAssignableFrom(@this);
        }



        public static Type GetGenericBaseClass(this Type @this, Type genericTypeDef) {
            Require.That(genericTypeDef.IsGenericTypeDefinition);

            if(@this == null) {
                return null;
            }

            if(@this.IsGenericType) {
                if(@this.GetGenericTypeDefinition() == genericTypeDef) {
                    return @this;
                }
            }

            return @this.BaseType.GetGenericBaseClass(genericTypeDef);
        }




    }
}
