using FakeOrleans.Grains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FakeOrleans.Grains
{
    public static class GrainContextExtensions
    {
        
        public static async Task<TResult> Invoke<TResult>(this IGrainContext ctx, MethodInfo method, byte[][] argData) 
        {
            var args = argData.Select(d => ctx.Serializer.Deserialize(d)).ToArray();

            if(typeof(TResult).Equals(typeof(VoidType))) {
                try {
                    await (Task)method.Invoke(ctx.Grain, args);
                }
                catch(TargetInvocationException ex) {
                    throw ex.InnerException;
                }

                return default(TResult);
            }

            return await (Task<TResult>)method.Invoke(ctx.Grain, args);
        }
               


    }
}
