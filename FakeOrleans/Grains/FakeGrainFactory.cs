using Orleans;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FakeOrleans.Grains
{

    public class FakeGrainFactory : IGrainFactory
    {
        readonly TypeMap _types;
        readonly Func<ResolvedGrainKey, IGrain> _proxifier;
        
        public FakeGrainFactory(TypeMap types, Func<ResolvedGrainKey, IGrain> proxifier) {
            _types = types;
            _proxifier = proxifier;
        }

        
        public TGrainInterface GetGrain<TGrainInterface>(Guid primaryKey, string grainClassNamePrefix = null)
            where TGrainInterface : IGrainWithGuidKey 
        {
            Require.That(grainClassNamePrefix == null);

            var tAbstract = typeof(TGrainInterface);
            var tConcrete = _types.GetConcreteType(tAbstract);

            var key = new ResolvedGrainKey(tAbstract, tConcrete, primaryKey);

            return (TGrainInterface)_proxifier(key);
        }

        

        #region Unimpl (as yet)

        public TGrainInterface GetGrain<TGrainInterface>(long primaryKey, string grainClassNamePrefix = null) where TGrainInterface : IGrainWithIntegerKey {
            throw new NotImplementedException();
        }

        public TGrainInterface GetGrain<TGrainInterface>(long primaryKey, string keyExtension, string grainClassNamePrefix = null) where TGrainInterface : IGrainWithIntegerCompoundKey {
            throw new NotImplementedException();
        }

        public TGrainInterface GetGrain<TGrainInterface>(Guid primaryKey, string keyExtension, string grainClassNamePrefix = null) 
            where TGrainInterface : IGrainWithGuidCompoundKey 
        {
            throw new NotImplementedException();

        }

        public TGrainInterface GetGrain<TGrainInterface>(string primaryKey, string grainClassNamePrefix = null)
            where TGrainInterface : IGrainWithStringKey {
            throw new NotImplementedException();
        }

        public Task<TGrainObserverInterface> CreateObjectReference<TGrainObserverInterface>(IGrainObserver obj) where TGrainObserverInterface : IGrainObserver {
            throw new NotImplementedException();
        }

        public Task DeleteObjectReference<TGrainObserverInterface>(IGrainObserver obj) where TGrainObserverInterface : IGrainObserver {
            throw new NotImplementedException();
        }

        #endregion


               
                
        
    }
}
