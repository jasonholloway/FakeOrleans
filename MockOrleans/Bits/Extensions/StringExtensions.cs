using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace System
{
    public static class StringExtensions
    {

        public static string[] Match(this string @this, string pattern, int groupIndex = 0) {
            return Regex.Matches(@this, pattern)
                        .OfType<Match>()
                        .Select(m => m.Groups[groupIndex].Value)
                        .ToArray();                            
        }


        public static string Capitalise(this string @this)
        {
            var r = @this.ToCharArray();

            r[0] = (char)(r[0] & ~0x20);

            return new string(r);
        }



    }
}
