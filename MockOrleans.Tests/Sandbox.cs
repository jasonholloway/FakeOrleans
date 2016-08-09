using NUnit.Framework;
using Orleans;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace MockOrleans.Tests
{
    [TestFixture]
    public class Sandbox
    {

        [Test]
        public void CasterInjection() 
        {
            var tRuntimeClient = typeof(Grain).Assembly.GetType("Orleans.Runtime.RuntimeClient");
            
            var pCurrent = tRuntimeClient.GetProperty("Current", BindingFlags.NonPublic | BindingFlags.Static);

            var currentClient = pCurrent.GetValue(null);



            var asmOrleans = typeof(Grain).Assembly;
            var asmRuntime = typeof(Silo).Assembly;




            var ctorGrainFactory = typeof(GrainFactory).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
            var grainFactory = ctorGrainFactory.Invoke(new object[0]);
            

            var tClient = asmRuntime.GetType("Orleans.Runtime.InsideRuntimeClient");
            var ctor = tClient.GetConstructors().First();

            var client = ctor.Invoke(new object[] {
                                null, null, null, null,
                                new ClusterConfiguration(),
                                null, null,
                                grainFactory
                            });





            


        }









    }
}
