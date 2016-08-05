using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MockOrleans
{
    public interface IGrainEndpoint
    {
        Task Invoke(Func<Task> fn, bool activate = true);
        Task<TResult> Invoke<TResult>(Func<Task<TResult>> fn, bool activate = true);
        
        Task<TResult> Invoke<TResult>(MethodInfo method, object[] args);
    }

    public class VoidType { }

}
