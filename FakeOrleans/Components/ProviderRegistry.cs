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
        Fixture _runtime;
        ConcurrentBag<IProvider> _providers = new ConcurrentBag<IProvider>();
        
        public ProviderRegistry(Fixture runtime) {
            _runtime = runtime;    
        }



        public async Task Add(Type provType)
        {
            var prov = (IProvider)Activator.CreateInstance(provType);
            
            await prov.Init("", new ProviderRuntimeAdaptor(_runtime), null);

            _providers.Add(prov);
        }


        
        //providers should be closed also
        //...





        class ProviderRuntimeAdaptor : IProviderRuntime
        {
            Fixture _runtime;

            public ProviderRuntimeAdaptor(Fixture runtime) {
                _runtime = runtime;
            }


            public IGrainFactory GrainFactory {
                get { return _runtime.GrainFactory; }
            }

            public Guid ServiceId {
                get {
                    throw new NotImplementedException();
                }
            }

            public IServiceProvider ServiceProvider {
                get { return _runtime.Services; }
            }

            public string SiloIdentity {
                get {
                    throw new NotImplementedException();
                }
            }

            public InvokeInterceptor GetInvokeInterceptor() {
                throw new NotImplementedException();
            }

            public Logger GetLogger(string loggerName) {
                return ((IGrainRuntime)_runtime).GetLogger(loggerName);
            }

            public void SetInvokeInterceptor(InvokeInterceptor interceptor) {
                throw new NotImplementedException();
            }
        }


    }
}
