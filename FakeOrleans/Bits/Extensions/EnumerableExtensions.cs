using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeOrleans
{
    public static class EnumerableExtensions
    {
        

        public static void ForEach<T>(this IEnumerable<T> @this, Action<T> fn) {
            foreach(var el in @this) fn(el);
        }

        public static Task ForEach<T>(this IEnumerable<T> @this, Func<T, Task> fn) {
            return Task.WhenAll(@this.Select(v => fn(v)));
        }



        public static ISet<R> ToSet<T, R>(this IEnumerable<T> @this, Func<T, R> fn) {
            return new HashSet<R>(@this.Select(fn));
        }

        public static ISet<T> ToSet<T>(this IEnumerable<T> @this) {
            return new HashSet<T>(@this);
        }
        
        public static Task<T[]> WhenAll<T>(this IEnumerable<Task<T>> @this) {
            return Task.WhenAll(@this);
        }

        public static Task WhenAll(this IEnumerable<Task> @this)
            => Task.WhenAll(@this);



        public static async Task<IEnumerable<T>> Where<T>(this IEnumerable<T> @this, Func<T, Task<bool>> fnPredicate)
        {
            var results = await @this.Select(async i => new { Item = i, Flag = await fnPredicate(i) })
                                    .WhenAll();

            return results.Where(t => t.Flag).Select(t => t.Item);
        }



        public static Task<T[]> WrapInTask<T>(this IEnumerable<T> @this)
            => Task.FromResult(@this.ToArray()); 


    }
}
