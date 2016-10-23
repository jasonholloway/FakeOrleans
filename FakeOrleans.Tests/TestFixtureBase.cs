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

        protected class TestGrain : Grain, IGrainWithGuidKey { }
        protected class TestGrain<T> : Grain, IGrainWithGuidKey { }


        protected Placement CreatePlacement()
                    => new Placement(new ConcreteKey(typeof(TestGrain), Guid.NewGuid()));

    }

}
