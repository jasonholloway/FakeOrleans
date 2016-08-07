using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MockOrleans
{

    public class RequestRegistry
    {
        RequestRegistry _innerReqs;
        int _count;
        Queue<TaskCompletionSource<bool>> _taskSources;


        public RequestRegistry(RequestRegistry innerReqs = null) {
            _innerReqs = innerReqs;
            _taskSources = new Queue<TaskCompletionSource<bool>>();
        }

        public void Increment() {
            Interlocked.Increment(ref _count);
            _innerReqs?.Increment();
        }

        //Not convinced by below
        public void Decrement() {
            Queue<TaskCompletionSource<bool>> capturedTaskSources = null;

            int c = Interlocked.Decrement(ref _count);

            if(c == 0) {
                capturedTaskSources = Interlocked.Exchange(ref _taskSources, new Queue<TaskCompletionSource<bool>>());
            }

            _innerReqs?.Decrement();
            capturedTaskSources?.ForEach(ts => ts.SetResult(true));
        }

        public Task WhenIdle() {
            var source = new TaskCompletionSource<bool>();
            _taskSources.Enqueue(source);
            return source.Task;
        }

    }


}
