using FakeOrleans.Components;
using FakeOrleans.Grains;
using NUnit.Framework;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeOrleans.Tests
{

    [TestFixture]
    public class PlacerTests : TestFixtureBase
    {

        IPlacer _placer;
        GrainKey[] _keys;

        Type[] _normGrainTypes = { typeof(TestGrain), typeof(TestGrain<int>), typeof(TestGrain<TestGrain<object>>) };


        [SetUp]
        public void SetUp() 
        {            
            _keys = Enumerable.Range(0, 100)
                        .Select(i => new GrainKey(_normGrainTypes[i % _normGrainTypes.Length], Guid.NewGuid()))
                        .ToArray();

            _placer = new Placer();
        }

        

        [Test]
        public void PlacingNormalGrain_ReturnsSamePlacement() 
        {
            var resultSets = Enumerable.Range(0, 10)
                                .Select(_ => _keys.Select(_placer.Place).ToArray())
                                .ToArray();

            resultSets.ForEach(r => {
                Assert.That(r, Is.EqualTo(resultSets.First()));
            });
        }


    }
}
