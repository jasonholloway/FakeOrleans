using FakeOrleans;
using Orleans;
using Orleans.Providers;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockOrleans.Components
{

    public class ProviderRuntimeAdaptor : IProviderRuntime
    {
        readonly IGrainFactory _grainFac;
        readonly IServiceProvider _services;
        readonly Logger _logger;

        public ProviderRuntimeAdaptor(IGrainFactory grainFac, IServiceProvider services, Logger logger) {
            _grainFac = grainFac;
            _services = services;
            _logger = logger;
        }


        public IGrainFactory GrainFactory {
            get { return _grainFac; }
        }

        public Guid ServiceId {
            get {
                throw new NotImplementedException();
            }
        }

        public IServiceProvider ServiceProvider {
            get { return _services; }
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
            return _logger;
        }

        public void SetInvokeInterceptor(InvokeInterceptor interceptor) {
            throw new NotImplementedException();
        }
    }

}
