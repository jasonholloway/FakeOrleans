using FakeOrleans.Grains;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeOrleans.Tests
{

    public abstract class TestFixtureBase
    {
        protected interface ITestGrain : IGrainWithGuidKey { }
        protected class TestGrain : Grain, ITestGrain { }
        protected class TestGrain<T> : Grain, ITestGrain { }


        protected Placement CreatePlacement()
                    => new Placement(new AbstractKey(typeof(ITestGrain), Guid.NewGuid()), typeof(TestGrain));

    }

}
