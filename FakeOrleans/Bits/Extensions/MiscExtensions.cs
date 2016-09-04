using FakeOrleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeOrleans
{
    public static class MiscExtensions
    {
        
        public static bool IsEmpty(this Guid guid) => guid == System.Guid.Empty;

        public static bool IsEmpty(this DateTime date) => date == DateTime.MinValue;

        public static T[] NullIfEmpty<T>(this T[] inp) => inp.Any() ? inp : (T[])null;


        public static TVal GetOrDefault<TKey, TVal>(this IDictionary<TKey, TVal> @this, TKey key) {
            TVal val;
            if(@this.TryGetValue(key, out val)) return val;
            else return default(TVal);
        }

                


        public static TService Get<TService>(this IServiceProvider @this) {
            return (TService)@this.GetService(typeof(TService));
        }




        //public static IAsyncStream<T> GetStream<T>(this IGrainRuntime runtime, StreamKey<T> key) 
        //{
        //    var provider = (IStreamProvider)runtime.StreamProviderManager.GetProvider(key.ProviderName);
        //    return provider.GetStream<T>(key.Id, key.Namespace);
        //}



        public static DateTime StripMilliseconds(this DateTime date)
            => new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Kind);
        



        public static Guid Guid(this Random @this)
        {
            var bytes = new byte[16];

            @this.NextBytes(bytes);

            return new Guid(bytes);
        }


        public static T Pick<T>(this IEnumerable<T> @this, Random r) => @this.ElementAtOrDefault(r.Next(@this.Count()));

        public static string String(this Random @this) => @this.Guid().ToString();
        
        public static T Of<T>(this Random @this, params T[] vals) => vals[@this.Next(0, vals.Length)];

        public static DateTime Date(this Random @this) => new DateTime(2050, 1, 1).AddHours(@this.NextDouble() * 24 * 356 * 50);
        
    }
}
