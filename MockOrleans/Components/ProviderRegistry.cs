using Orleans.Providers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;

namespace MockOrleans
{
    public class BootstrapperRegistry
    {
        MockFixture _fx;
        ConcurrentBag<IProvider> _providers = new ConcurrentBag<IProvider>();
        
        public BootstrapperRegistry(MockFixture fx) {
            _fx = fx;    
        }



        public void Add(Type provType)
        {
            var prov = (IProvider)Activator.CreateInstance(provType);            
            _providers.Add(prov);
        }



        public async Task Init() {
            await _providers.ForEach(p => p.Init("", new ProviderRuntimeAdaptor(_fx), null));            
        }

        public async Task Close() {
            await _providers.ForEach(p => p.Close());
        }


        
        //providers should be closed also
        //...





        class ProviderRuntimeAdaptor : IProviderRuntime
        {
            MockFixture _runtime;

            public ProviderRuntimeAdaptor(MockFixture runtime) {
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
