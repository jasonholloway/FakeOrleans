using MockOrleans.Grains;
using Orleans;
using Orleans.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace MockOrleans
{

    public interface IStateStore
    {
        Task ReadFrom(GrainKey key, IGrainState state);
        Task WriteTo(GrainKey key, IGrainState state);
        Task Clear(GrainKey key);
    }


    public class MockStateStore : IStateStore
    {
        MockFixture _fx;
        MockSerializer _serializer;
        ConcurrentDictionary<GrainKey, byte[]> _dCommits = new ConcurrentDictionary<GrainKey, byte[]>(ConcreteGrainKeyComparer.Instance);


        public MockStateStore(MockFixture fx) {
            _fx = fx;
            _serializer = new MockSerializer(fx);
        }



        public virtual Task Clear(GrainKey key) 
        {
            byte[] _;

            _dCommits.TryRemove(key, out _);

            return TaskDone.Done;
        }

        public virtual Task ReadFrom(GrainKey key, IGrainState state) 
        {
            byte[] commit = null;

            if(_dCommits.TryGetValue(key, out commit)) {
                state.State = _serializer.Deserialize(commit);
            }

            return TaskDone.Done;
        }

        public virtual Task WriteTo(GrainKey key, IGrainState state) 
        {
            _dCommits.AddOrUpdate(key, 
                        (_) => _serializer.Serialize(state.State), 
                        (_, __) => _serializer.Serialize(state.State));

            return TaskDone.Done;
        }





    }
}
