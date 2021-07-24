using System;
using System.Collections.Generic;
using System.Text;

namespace CbFractals.Tools
{
    using CVec2 = Tuple<double, double>;
    using CVec2Int = Tuple<int, int>;
    using CVec4 = Tuple<double, double, double, double>;

    internal static class CVectorExtensions
    {
        internal static string TryTrimStart(this string s, string aTrim)
          => s.StartsWith(aTrim) ? s.Substring(aTrim.Length, s.Length - aTrim.Length) : s;

        internal static T New<T>(this Type aType, params object[] aArgs)
            => (T)Activator.CreateInstance(aType, aArgs);
        internal static CVec4 Mul(this CVec4 lhs, CVec4 rhs)
            => new CVec4(lhs.Item1 * rhs.Item1,
                         lhs.Item2 * rhs.Item2,
                         lhs.Item3 * rhs.Item3,
                         lhs.Item4 * rhs.Item4);


        internal static CVec4 GetViewportByFaktor(this CVec4 aRect, CVec4 aZoomFaktor)
            => new CVec4(aRect.Item1 + aRect.Item3 * aZoomFaktor.Item1,
                         aRect.Item2 + aRect.Item4 * aZoomFaktor.Item2,
                         aRect.Item3 * aZoomFaktor.Item3,
                         aRect.Item4 * aZoomFaktor.Item4);

        internal static CVec2 GetRectRangeX(this CVec4 aRect)
            => new CVec2(aRect.Item1, aRect.Item1 + aRect.Item3);
        internal static CVec2 GetRectRangeY(this CVec4 aRect)
                    => new CVec2(aRect.Item2, aRect.Item2 + aRect.Item4);
        internal static double GetRectLeft(this CVec4 aRect)
            => aRect.Item1;
        internal static double GetRectTop(this CVec4 aRect)
            => aRect.Item2;
        internal static double GetRectWidth(this CVec4 aRect)
            => aRect.Item3;
        internal static double GetRectHeight(this CVec4 aRect)
            => aRect.Item4;
        internal static CVec4 GetRectNormalize(this CVec4 aRect)
            => new CVec4(0,
                         0,
                         aRect.GetRectWidth(),
                         aRect.GetRectHeight()
                         );

        internal static CVec2 GetRectPos(this CVec4 aRect)
            => new CVec2(aRect.Item1, aRect.Item2);
        internal static CVec2 GetRectSize(this CVec4 aRect)
            => new CVec2(aRect.Item3, aRect.Item4);
        internal static CVec2 Add(this CVec2 lhs, CVec2 rhs)
            => new CVec2(lhs.Item1 + rhs.Item1, lhs.Item2 + rhs.Item2);
        internal static CVec2 Divide(this CVec2 lhs, CVec2 rhs)
            => new CVec2(lhs.Item1 / rhs.Item1, lhs.Item2 / rhs.Item2);
        internal static CVec2 Divide(this CVec2 lhs, double rhs)
            => lhs.Divide(new CVec2(rhs, rhs));
        internal static CVec2 Mul(this CVec2 lhs, CVec2 rhs)
            => new CVec2(lhs.Item1 * rhs.Item1, lhs.Item2 * rhs.Item2);
        internal static CVec2 Mul(this CVec2 lhs, double rhs)
            => lhs.Mul(new CVec2(rhs, rhs));
        internal static CVec2 Subtract(this CVec2 lhs, CVec2 rhs)
            => new CVec2(lhs.Item1 - rhs.Item1, lhs.Item2 - rhs.Item2);
        internal static CVec4 Mul(this CVec4 lhs, double rhs)
            => lhs.Mul(new CVec4(rhs, rhs, rhs, rhs));
        internal static CVec4 GetRectTranslate(this CVec4 aRect, CVec2 aVec2)
            => new CVec4(aRect.GetRectLeft() + aVec2.Item1,
                         aRect.GetRectTop() + aVec2.Item2,
                         aRect.GetRectWidth(),
                         aRect.GetRectHeight()
                         );
        internal static CVec4 GetRectAllignedAtCenter(this CVec4 aRect, CVec2 aCenter)
            => new CVec4(aCenter.Item1 - aRect.GetRectWidth() / 2d,
                        aCenter.Item2 - aRect.GetRectHeight() / 2d,
                        aRect.GetRectWidth(),
                        aRect.GetRectHeight()
                        );
        internal static CVec4 ZoomPrivate(this CVec4 aRect, double aZoom)
            => aRect.GetRectNormalize().Mul(aZoom).GetRectTranslate(aRect.GetRectPos().Mul(aZoom));
        internal static CVec4 Zoom(this CVec4 aRect, double aZoom)
            => aRect.Zoom(aZoom, aRect.GetRectCenter());
        internal static CVec4 Zoom(this CVec4 aRect, double aZoom, CVec2 aCenter)
            => aRect.ZoomPrivate(aZoom).GetRectAllignedAtCenter(aCenter);
        internal static CVec2 GetRectCenter(this CVec4 aRect)
            => new CVec2(aRect.GetRectLeft() + aRect.GetRectWidth() / 2d,
                         aRect.GetRectTop() + aRect.GetRectHeight() / 2d);

        internal static CVec2 ToVec2(this CVec2Int v)
            => new CVec2(v.Item1, v.Item2);

        internal static double GetHypothenuse(this CVec2 v)
            => Math.Sqrt(v.Item1 * v.Item1 + v.Item2 * v.Item2);
        internal static double GetDistanceMaybeNegative(this CVec2 c1, CVec2 c2)
            => c1.Subtract(c2).GetHypothenuse();
        internal static double GetDistanceAbs(this CVec2 c1, CVec2 c2)
            => Math.Abs(c1.GetDistanceMaybeNegative(c2));
        internal static CVec2 Round(this CVec2 v)
            => new CVec2(Math.Round(v.Item1), Math.Round(v.Item2));
    }

}
