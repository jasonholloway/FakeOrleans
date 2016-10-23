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
        Placement Place(AbstractKey key);
    }


    public class Placer : IPlacer
    {
        readonly Func<Type, Type> _typeMapper;
        
        public Placer(Func<Type, Type> typeMapper) {
            _typeMapper = typeMapper;
        }

        public Placement Place(AbstractKey key) {
            var concreteType = _typeMapper(key.AbstractType);
            var concreteKey = new ConcreteKey(concreteType, key.Id);            
            return new Placement(concreteKey);
        }
    }

}
