using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockOrleans
{
    public static class Require
    {


        public static void That(bool cond, Func<string> fnMessage) {
            if(!cond) throw new InvalidOperationException(fnMessage());
        }


        public static T NotNull<T>(T obj, Func<string> fnMessage) where T : class {
            if(obj == null) throw new InvalidOperationException(fnMessage());
            return obj;
        }


        public static T NotEmpty<T>(T subject, Func<string> fnMessage) {
            if(subject.Equals(default(T))) throw new InvalidOperationException(fnMessage());
            return subject;
        }



        public static void That(bool cond, string message = null)
            => That(cond, () => message ?? "Bad operation attempted!");


        public static T NotNull<T>(T obj, string message = null)
            where T : class
            => NotNull(obj, () => message ?? "Value can't be null!");


        public static T NotEmpty<T>(T subject, string message = null)
            => NotEmpty(subject, () => message ?? "Value can't be empty!");




    }
}
