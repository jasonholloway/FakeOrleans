using Orleans.Providers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;

namespace FakeOrleans
{
    public class ProviderRegistry
    {
        Func<IProviderRuntime> _runtimeFac;
        ConcurrentBag<IProvider> _providers = new ConcurrentBag<IProvider>();
        

        public ProviderRegistry(Func<IProviderRuntime> runtimeFac) {
            _runtimeFac = runtimeFac;
        }



        public async Task Add(Type provType)
        {
            var prov = (IProvider)Activator.CreateInstance(provType);
            
            await prov.Init("", _runtimeFac(), null);

            _providers.Add(prov);
        }


        
        //providers should be closed also
        //...

        

    }
}
