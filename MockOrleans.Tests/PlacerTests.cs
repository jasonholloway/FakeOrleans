using MockOrleans.Grains;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockOrleans.Tests
{

    public class Placer : IPlacer
    {
        public GrainPlacement Place(GrainKey key) {
            throw new NotImplementedException();
        }
    }


    [TestFixture]
    public class PlacerTests
    {

        Placer _placer;


        [SetUp]
        public void SetUp() {
            _placer = new Placer();
        }




        [Test]
        public async Task lklkjlkjlkj() {
            throw new NotImplementedException();
        }

    }
}
