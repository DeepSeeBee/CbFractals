using CbFractals.Tools;
using CbFractals.ViewModel.PropertySystem;
using CbFractals.ViewModel.Render;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace CbFractals.ViewModel.Mandelbrot
{
    using CVec2 = Tuple<double, double>;
    using CVec4 = Tuple<double, double, double, double>;
    using CVec2Int = Tuple<int, int>;

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

    internal sealed class CClassicMandelbrotSetPixelAlgorithm : CPixelAlgorithm
    {
        public CClassicMandelbrotSetPixelAlgorithm(CPixelAlgorithmInput aInput) : base(aInput)
        {
        }


        internal override Color RenderPixel(int aX, int aY)
        {
            var aPixelCoord = new CVec2Int(aX, aY);
            var aDx = this.SizePxl.Item1;
            var aDy = this.SizePxl.Item2;
            var aSizeMnd = this.SizeMnd;
            var aMandelPos = this.GetMandelPos(aPixelCoord);

            var s = (1 << 16);
            var x0 = aMandelPos.Item1;
            var y0 = aMandelPos.Item2;
            var x = 0.0d;
            var y = 0.0d;
            var it = 0;
            var itm = 100;
            while (x * x + y * y <= s && it < itm)
            {
                var xtmp = x * x - y * y + x0;
                y = 2 * x * y + y0;
                x = xtmp;
                ++it;
            }
            var m = true;
            if (it < itm
            && m)
            {
                var aLogZn = Math.Log(x * x + y * y) / 2;
                var nu = (int)(Math.Log(aLogZn / Math.Log(2)) / Math.Log(2));
                it = it + 1 - nu;
            }
            var itf = (float)it / (float)itm;
            var c = this.GetColor(itf);
            return c;
        }
    }

    internal sealed class CSingleJuliaMandelbrotSetPixelAlgorithm : CPixelAlgorithm
    {
        public CSingleJuliaMandelbrotSetPixelAlgorithm(CPixelAlgorithmInput aInput) : base(aInput)
        {
            var aParameters = this.Parameters;
            var aCenterX = aParameters.Get<double>(CParameterEnum.CenterX);
            var aCenterY = aParameters.Get<double>(CParameterEnum.CenterY);
            var aRangeMid = 0.5d;
            var aDeltaX = aCenterX.Map(aRangeMid,
                                    new CVec2(0, aRangeMid),
                                    new CVec2(-2d, 0),
                                    new CVec2(aRangeMid, 1),
                                    new CVec2(0, 3d));
            var aDeltaY = aCenterY.Map(aRangeMid,
                                    new CVec2(0, aRangeMid),
                                    new CVec2(-2d, 0),
                                    new CVec2(aRangeMid, 1),
                                    new CVec2(0, 1.5d));

            //var aDeltaY = aCenterY;
            var aJuliaDeltaPos = new CVec2(aDeltaX, aDeltaY);
            this.JuliaDeltaPos = aJuliaDeltaPos;
        }

        private readonly CVec2 JuliaDeltaPos;

        internal override Color RenderPixel(int aX, int aY)
        {
            var aPixelCoord = new CVec2Int(aX, aY);
            var aDx = this.SizePxl.Item1;
            var aDy = this.SizePxl.Item2;
            var aParameters = this.Parameters;
            var itm = Convert.ToInt64(aParameters.Get<double>(CParameterEnum.Iterations)); // 300; //after how much iterations the function should stop

            //pick some values for the constant c, this determines the shape of the Julia Set
            var cRe = aParameters.Get<double>(CParameterEnum.JuliaPartReal);// -0.7d;    // real part of the constant c, determinate shape of the Julia Set
            var cIm = aParameters.Get<double>(CParameterEnum.JuliaPartImaginary); //0.27015d; // imaginary part of the constant c, determinate shape of the Julia Set
                                                                                  //cIm = 0.3d;
                                                                                  //cRe = -0.68;
            var h = aDy;
            var w = aDx;
            var x = aPixelCoord.Item1;
            var y = aPixelCoord.Item2;
            var zoom = aParameters.Get<double>(CParameterEnum.Zoom); // SizeMndDefault.Item3 / aSizeMnd.Item3;

            var moveX = this.JuliaDeltaPos.Item1; // aParameters.Get<double>(CParameterEnum.JuliaMoveX);
            var moveY = this.JuliaDeltaPos.Item2; // aParameters.Get<double>(CParameterEnum.JuliaMoveY);
            var newRe = 1.5 * (x - w / 2) / (0.5 * zoom * w) + moveX;
            var newIm = (y - h / 2) / (0.5 * zoom * h) + moveY;
            var it = 0;
            for (it = 0; it < itm; it++)
            {
                var oldRe = newRe;
                var oldIm = newIm;
                //the actual iteration, the real and imaginary part are calculated
                newRe = oldRe * oldRe - oldIm * oldIm + cRe;
                newIm = 2 * oldRe * oldIm + cIm;
                //if the point is outside the circle with radius 2: stop
                if ((newRe * newRe + newIm * newIm) > 4) break;
            }
            var itf = (float)it / (float)itm;
            var c = this.GetColor(itf);
            return c;
        }
    }

    internal sealed class CMultiJuliaMandelbrotSetPixelAlgorithm : CPixelAlgorithm
    {
        public CMultiJuliaMandelbrotSetPixelAlgorithm(CPixelAlgorithmInput aInput) : base(aInput)
        {

        }
        internal override Color RenderPixel(int aX, int aY)
        {
            //throw new NotImplementedException();
            /*
            Pseudocode for normal Julia sets
            {\displaystyle f(z)=z^{2}+c}f(z)=z^{2}+c
            R = escape radius  # choose R > 0 such that R**2 - R >= sqrt(cx**2 + cy**2)

            for each pixel (x, y) on the screen, do:   
            {
            zx = scaled x coordinate of pixel # (scale to be between -R and R)
               # zx represents the real part of z.
            zy = scaled y coordinate of pixel # (scale to be between -R and R)
               # zy represents the imaginary part of z.

            iteration = 0
            max_iteration = 1000

            while (zx * zx + zy * zy < R**2  AND  iteration < max_iteration) 
            {
                xtemp = zx * zx - zy * zy
                zy = 2 * zx * zy  + cy 
                zx = xtemp + cx

                iteration = iteration + 1 
            }

            if (iteration == max_iteration)
                return black;
            else
                return iteration;
            }
            Pseudocode for multi-Julia sets
            {\displaystyle f(z)=z^{n}+c}{\displaystyle f(z)=z^{n}+c}
            R = escape radius #  choose R > 0 such that R**n - R >= sqrt(cx**2 + cy**2)
            */

            var aPixelCoord = new CVec2Int(aX, aY);
            var aDx = this.SizePxl.Item1;
            var aDy = this.SizePxl.Item2;
            var aSizeMnd = this.SizeMnd;
            var aMandelPos = this.GetMandelPos(aPixelCoord);
            var zx = aMandelPos.Item1;
            var zy = aMandelPos.Item2;
            var cx = 0d; // aSizeMnd.Item1;
            var cy = 0; // aSizeMnd.Item2;
            var R = 2d; // 2d; // R = escape radius  # choose R > 0 such that R**2 - R >= sqrt(cx**2 + cy**2)
            var n = 10d;

            var it = 0;
            var itm = 1000;
            while (zx * zx + zy * zy < R * R && it < itm)
            {
                var xtmp = Math.Pow(zx * zx + zy * zy, (n / 2) * Math.Cos(n * Math.Atan2(zy, zx))) + cx;
                zy = Math.Pow((zx * zx + zy * zy), (n / 2) * Math.Sin(n * Math.Atan2(zy, zx))) + cy;
                zx = xtmp;
                ++it;
            }
            var itf = (double)it / (double)itm;
            var cl = it == itm
                   ? Colors.Black
                   : this.GetColor(itf);
            return cl;

            /*
                    for each pixel (x, y) on the screen, do:
                    {
                    zx = scaled x coordinate of pixel # (scale to be between -R and R)
                    zy = scaled y coordinate of pixel # (scale to be between -R and R)

                    iteration = 0
                    max_iteration = 1000

                    while (zx * zx + zy * zy < R**2  AND  iteration < max_iteration) 
                    {
                        xtmp = (zx * zx + zy * zy) ^ (n / 2) * cos(n * atan2(zy, zx)) + cx;
                        zy = (zx * zx + zy * zy) ^ (n / 2) * sin(n * atan2(zy, zx)) + cy;
                        zx = xtmp;

                        iteration = iteration + 1
                    } 
                    if (iteration == max_iteration)
                        return black;
                    else
                        return iteration;
                    }
*/
        }
    }


}
