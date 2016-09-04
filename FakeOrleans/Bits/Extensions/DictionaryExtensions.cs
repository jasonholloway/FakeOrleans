using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    public static class DictionaryExtensions
    {
        public static void Merge<TKey, TVal>(
            this IDictionary<TKey, TVal> @this, 
            IEnumerable<KeyValuePair<TKey, TVal>> source)
        {
            foreach(var kv in source) {
                @this.Add(kv);
            }
        }


    }
}
