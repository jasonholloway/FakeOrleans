using MockOrleans.Grains;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockOrleans.Tests
{

    public abstract class TestFixtureBase
    {

        protected class TestGrain : Grain, IGrainWithGuidKey { }
        protected class TestGrain<T> : Grain, IGrainWithGuidKey { }


        protected GrainPlacement CreatePlacement()
                    => new GrainPlacement(new GrainKey(typeof(TestGrain), Guid.NewGuid()));

    }

}
