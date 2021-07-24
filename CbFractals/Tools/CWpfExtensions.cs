using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace CbFractals.Tools
{
    using CVec2 = Tuple<double, double>;
    using CVec2Int = Tuple<int, int>;
    using CVec4 = Tuple<double, double, double, double>;

    internal static class CWpfExtensions
    {
        internal static CVec2 ToVec2(this Point pt)
            => new CVec2(pt.X, pt.Y);
        internal static Point ToPoint(this CVec2 v)
            => new Point(v.Item1, v.Item2);
    }
}
