using FakeOrleans.Grains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeOrleans.Components
{
    
    public interface IPlacer
    {
        GrainPlacement Place(GrainKey key);
    }


    public class Placer : IPlacer
    {
        public GrainPlacement Place(GrainKey key) {
            return new GrainPlacement(key);
        }
    }

}
