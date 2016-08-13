using MockOrleans.Grains;
using Orleans.Providers;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MockOrleans.Streams
{
    

    public class GrainStreamProviderAdaptor : IStreamProvider, IProvider
    {
        public string Name { get; private set; }
        public bool IsRewindable { get; } = false;

        StreamRegistry _streamReg;
        GrainHarness _activation;
        
        public GrainStreamProviderAdaptor(GrainHarness activation, StreamRegistry streamReg, string providerName) {
            Name = providerName;
            _streamReg = streamReg;
            _activation = activation;
        }
        
        public IAsyncStream<T> GetStream<T>(Guid streamId, string streamNamespace) 
        {
            var key = new StreamKey(Name, streamNamespace, streamId);
            
            var stream = _streamReg.GetStream(key);
            
            return new GrainStreamClient<T>(_activation, stream, _streamReg);
        }

        #region IProvider

        Task IProvider.Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
            => Task.CompletedTask;

        Task IProvider.Close()
            => Task.CompletedTask;

        #endregion
    }

    
    class StreamProviderManagerAdaptor : IStreamProviderManager
    {
        GrainHarness _activation;
        StreamRegistry _streamReg;

        public StreamProviderManagerAdaptor(GrainHarness activation, StreamRegistry streamReg) {
            _activation = activation;
            _streamReg = streamReg;
        }

        public IProvider GetProvider(string name)
            => new GrainStreamProviderAdaptor(_activation, _streamReg, name);


        public IEnumerable<IStreamProvider> GetStreamProviders() {

            //StreamRegistry needs a dictionary of streamproviders, implicitly created

            throw new NotImplementedException(); //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        }
    }
    

}
