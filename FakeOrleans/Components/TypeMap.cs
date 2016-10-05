using FakeOrleans.Grains;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeOrleans
{    

    public static class TypeMapExtensions
    {
        public static void Map<TInterface, TImplementation>(this TypeMap typeMap)
            where TImplementation : class, TInterface
            => typeMap.Map(typeof(TInterface), typeof(TImplementation));
    }
    


    public class TypeMap
    {
        ConcurrentDictionary<Type, Type> _dMap = new ConcurrentDictionary<Type, Type>();
        Queue<Action<Type>> _qTypeProcessors = new Queue<Action<Type>>();
        

        public TypeMap() {
        }
        

        public Type GetConcreteType(Type abstractType) {
            return _dMap.GetOrAdd(abstractType, t => Resolve(t));
        }


        public void Map(Type abstractType, Type concreteType) 
        {            
            Require.That(abstractType.IsGenericTypeDefinition 
                                || abstractType.IsAssignableFrom(concreteType));

            _dMap.AddOrUpdate(abstractType, concreteType, (_, __) => concreteType);

            ProcessType(concreteType);
        }


        internal void AddTypeProcessor(Action<Type> fn) {
            lock(_qTypeProcessors) _qTypeProcessors.Enqueue(fn);
        }
        

        HashSet<Type> _processedTypes = new HashSet<Type>();

        void ProcessType(Type grainType) 
        {
            lock(_processedTypes) {
                if(_processedTypes.Contains(grainType)) {
                    return;
                }

                _processedTypes.Add(grainType);
            }

            lock(_qTypeProcessors) _qTypeProcessors.ForEach(fn => fn(grainType));
        }



        Type Resolve(Type abstractType) 
        {
            Require.That(!abstractType.IsGenericTypeDefinition);
            
            Type concreteType = null;

            if(!_dMap.TryGetValue(abstractType, out concreteType)) {
                //no exact match, but if we're generic, we have one further option
                if(abstractType.IsGenericType) {
                    Type concreteTypeDef = null;
                    var abstractTypeDef = abstractType.GetGenericTypeDefinition();

                    if(_dMap.TryGetValue(abstractTypeDef, out concreteTypeDef)) {
                        var genArgs = abstractType.GetGenericArguments();

                        //for now, just do as Orleans does:
                        concreteType = concreteTypeDef.MakeGenericType(genArgs);
                    }
                }
            }
            
            if(concreteType == null) {
                throw new InvalidOperationException($"Passed abstract type {abstractType} must be mapped and concretizable!");
            }

            return concreteType;
        }
    }

    
}
