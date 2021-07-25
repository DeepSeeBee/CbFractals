using CbFractals.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace CbFractals.ViewModel.Mandelbrot.Obsolete
{
    using CVec2 = Tuple<double, double>;
    using CVec4 = Tuple<double, double, double, double>;

    internal enum CBaseColorEnum
    {
        Red = 0,
        Green = 1,
        Blue = 2
    }
    internal class CColorDetector
    {
        internal CColorDetector(Color aCriteriaColor, CVec2 aCriteriaPos, CVec2 aPosMax)
        {
            this.PosWeightMax = aPosMax.GetHypothenuse();
            this.CriteriaPos = aCriteriaPos;
            this.CriteriaColor = aCriteriaColor;
        }

        private double GetColorDiff(byte aBaseColor, byte c)
        {
            var aTest = true;
            var cd1 = ((double)(Math.Max(aBaseColor, c) - Math.Min(aBaseColor, c))) / 255d;
            var cd2 = 1d - cd1;
            var cd3 = aBaseColor * cd2;
            var cd4 = aTest
                    ? 1d - cd3
                    : cd1
                    ;
            return cd4;
        }
        private double GetColorDiff(Color aBaseColor, Color aColor)
        {
            this.ColorDiffs[0] = GetColorDiff(aBaseColor.R, aColor.R);
            this.ColorDiffs[1] = GetColorDiff(aBaseColor.G, aColor.G);
            this.ColorDiffs[2] = GetColorDiff(aBaseColor.B, aColor.B);
            return this.ColorDiffs.Min();
        }
        private readonly double[] ColorDiffs = new double[3];

        private double GetColorDiff(Color c)
            => GetColorDiff(this.CriteriaColor, c);
        private double GetPosWeight(CVec2 v)
            => this.CriteriaPos.GetDistanceAbs(v) / this.PosWeightMax + 0.1;
        private double GetColorWeight(Color c)
            => this.GetColorDiff(c);
        private double GetWeight(Color c, CVec2 p)
            => (this.GetColorWeight(c) + this.GetPosWeight(p)) / 2d;

        private readonly Color CriteriaColor;
        private readonly CVec2 CriteriaPos;
        private readonly double PosWeightMax;

        private bool CurValid;
        private double CurWeight;
        private CVec2 CurPos;
        internal CVec2 NewCenter => this.CurPos;

        internal void Add(Color c, CVec2 p)
        {
            if (this.CurValid)
            {
                var aNewWeight = this.GetWeight(c, p);
                var aOldWeight = this.CurWeight;
                if (aNewWeight < aOldWeight)
                {
                    this.CurWeight = aNewWeight;
                    this.CurPos = p;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(true);
                }
            }
            else
            {
                this.CurWeight = this.GetPosWeight(p);
                this.CurPos = p;
                this.CurValid = true;
            }
        }

        internal static Tuple<CBaseColorEnum, CBaseColorEnum> GetRarestAndDominatingColor(Color[] aPixels)
        {
            var aRates = new UInt64[3];

            foreach (var aPixel in aPixels)
            {
                aRates[(int)CBaseColorEnum.Red] += aPixel.R;
                aRates[(int)CBaseColorEnum.Green] += aPixel.G;
                aRates[(int)CBaseColorEnum.Blue] += aPixel.B;
            }
            var r = aRates[(int)CBaseColorEnum.Red];
            var g = aRates[(int)CBaseColorEnum.Green];
            var b = aRates[(int)CBaseColorEnum.Blue];
            var aRarestColor = r <= g && r <= b
                ? CBaseColorEnum.Red
                : g <= b
                ? CBaseColorEnum.Green
                : CBaseColorEnum.Blue;
            var aDominatingColor = r >= g && r >= b
                ? CBaseColorEnum.Red
                : g >= b
                ? CBaseColorEnum.Green
                : CBaseColorEnum.Blue;
            var aRarestAndDominatingColor = new Tuple<CBaseColorEnum, CBaseColorEnum>(aRarestColor, aDominatingColor);
            return aRarestAndDominatingColor;
        }
        internal static Color GetColor(CBaseColorEnum aBaseColorEnum)
        {
            switch (aBaseColorEnum)
            {
                case CBaseColorEnum.Red:
                    return Color.FromRgb(255, 0, 0);
                case CBaseColorEnum.Green:
                    return Color.FromRgb(0, 255, 0);
                case CBaseColorEnum.Blue:
                    return Color.FromRgb(0, 0, 255);
                default:
                    throw new ArgumentException();
            }
        }
    }
}
