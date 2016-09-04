using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FakeOrleans
{
    public interface IGrainEndpoint
    {
        Task Invoke(Func<Task> fn);

        Task<TResult> Invoke<TResult>(Func<Task<TResult>> fn);

        Task Invoke<TGrainInterface>(Func<TGrainInterface, Task> fn);

        Task<TResult> Invoke<TResult>(MethodInfo method, byte[][] args);

    }

}
