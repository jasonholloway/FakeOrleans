using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockOrleans.Grains
{
    public interface IPlacer
    {
        GrainPlacement Place(GrainKey key);
    }
}
