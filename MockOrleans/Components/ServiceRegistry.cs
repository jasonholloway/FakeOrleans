using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockOrleans
{
    public class ServiceRegistry : IServiceProvider
    {
        readonly IServiceProvider _inner;
        readonly ConcurrentDictionary<Type, object> _dInjecteds;
        
        public ServiceRegistry(IServiceProvider inner) {
            _inner = inner;
            _dInjecteds = new ConcurrentDictionary<Type, object>();
        }

        public object GetService(Type serviceType) {
            object impl = null;

            if(_dInjecteds.TryGetValue(serviceType, out impl)) {
                return impl;
            }

            return _inner?.GetService(serviceType);
        }

        public object Inject(Type serviceType, object impl) {
            _dInjecteds[serviceType] = impl;
            return impl;
        }
        
    }



    public static class ServiceRegistryExtensions
    {
        public static TService Inject<TService>(this ServiceRegistry @this, TService impl)
            => (TService)@this.Inject(typeof(TService), impl);
    }

}
