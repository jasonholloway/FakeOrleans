using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeOrleans.Grains
{

    public class GrainSpec
    {
        public bool IsReentrant { get; private set; }
        public bool IsStatelessWorker { get; private set; }
        public int StatelessWorkerMaxNumber { get; private set; }
        public string[] StreamSubNamespaces { get; private set; }
        
        private GrainSpec() { }


        static ConcurrentDictionary<Type, GrainSpec> _dSpecs = new ConcurrentDictionary<Type, GrainSpec>();

        public static GrainSpec GetFor(Type grainType)
            => _dSpecs.GetOrAdd(grainType, GenerateSpec);


        static GrainSpec GenerateSpec(Type grainType) 
        {
            var attrs = grainType.GetCustomAttributes(true);
            
            var spec = new GrainSpec();
            spec.IsReentrant = attrs.OfType<ReentrantAttribute>().Any();
            
            var statelessWorkerAttr = attrs.OfType<StatelessWorkerAttribute>().FirstOrDefault();

            if(statelessWorkerAttr != null) {
                spec.IsStatelessWorker = true;
                spec.StatelessWorkerMaxNumber = statelessWorkerAttr.MaxLocalWorkers;
            }
                        
            var streamSubAttrs = attrs.OfType<ImplicitStreamSubscriptionAttribute>();
            spec.StreamSubNamespaces = streamSubAttrs.Select(a => a.Namespace).ToArray();
            
            return spec;
        }

    }


}
