using System;
using System.Collections.Generic;
using System.Text;

namespace CbFractals.Tools
{
    using CVec2 = Tuple<double, double>;

    internal static class CDreisatzExtensions
    {
        internal static double GetScale(this double v, double aMin, double aMax)
              => (v - aMin) / (aMax - aMin);

        internal static double GetScale(this double v, CVec2 aRange)
            => v.GetScale(aRange.Item1, aRange.Item2);
        internal static double Scale(this double fkt, double aMin, double aMax)
            => (aMax - aMin) * fkt + aMin;
        internal static double Scale(this double fkt, CVec2 aRange)
            => fkt.Scale(aRange.Item1, aRange.Item2);
        internal static double Map(this double fkt, double aSourceMin, double aSourceMax, double aTargetMin, double aTargetMax)
            => fkt.GetScale(aSourceMin, aSourceMax).Scale(aTargetMin, aTargetMax);

        internal static double Map(this double fkt, CVec2 aSourceRange, CVec2 aTargetRange)
            => fkt.Map(aSourceRange.Item1, aSourceRange.Item2, aTargetRange.Item1, aTargetRange.Item2);

        internal static double Map(this double fkt,
                                   double aCenter,
                                   CVec2 aLoSourceRange,
                                   CVec2 aLoTargetRange,
                                   CVec2 aHiSourceRange,
                                   CVec2 aHiTargetRange)
            => fkt < aCenter
            ? fkt.Map(aLoSourceRange, aLoTargetRange)
            : fkt.Map(aHiSourceRange, aHiTargetRange)
            ;

        internal static CVec2 GetScale(this CVec2 v, CVec2 aXRange, CVec2 aYRange)
            => new CVec2(v.Item1.GetScale(aXRange.Item1, aXRange.Item2),
                         v.Item2.GetScale(aYRange.Item1, aYRange.Item2));
        internal static CVec2 Scale(this CVec2 fkt, CVec2 aXRange, CVec2 aYRange)
            => new CVec2(fkt.Item1.Scale(aXRange),
                         fkt.Item2.Scale(aYRange));
        internal static CVec2 Map(this CVec2 v, CVec2 aXSourceRange, CVec2 aYSourceRange, CVec2 aXTargetRange, CVec2 aYTargetRange)
            => new CVec2(v.Item1.Map(aXSourceRange.Item1, aXSourceRange.Item2, aXTargetRange.Item1, aXTargetRange.Item2),
                         v.Item2.Map(aYSourceRange.Item1, aYSourceRange.Item2, aYTargetRange.Item1, aYTargetRange.Item2));
    }


}
