using System;
using System.Collections.Generic;
using System.Text;

namespace CbFractals.Tools
{
    internal static class CLazyLoad
    {
        internal static T Get<T>(ref T? v, Func<T> n) where T : struct
        {
            if (!v.HasValue)
                v = n();
            return v.Value;
        }
        internal static T Get<T>(ref T v, Func<T> n) where T : class
        {
            if (!(v is object))
            {
                v = n();
            }
            return v;
        }
    }

}
