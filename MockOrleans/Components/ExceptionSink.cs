using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockOrleans
{
    public class ExceptionSink
    {
        ExceptionSink _inner;
        ConcurrentQueue<Exception> _exceptions;
        

        public ExceptionSink(ExceptionSink inner = null) {
            _inner = inner;
            _exceptions = new ConcurrentQueue<Exception>();
        }


        internal void Add(Exception ex) {            
            _exceptions.Enqueue(ex);
            _inner?.Add(ex);
        }
         
        
        public Exception[] All {
            get { return _exceptions.ToArray(); }
        }
        
                      
        public void RethrowAll() {            
            var captured = _exceptions.ToArray();

            if(captured.Any()) {
                throw new AggregateException(captured).Flatten();
            }
        }
        
    }


    public static class TaskExtensions
    {
        public static void SinkExceptions(this Task task, ExceptionSink sink)
            => task.ContinueWith(t => sink.Add(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
        
    }

}
