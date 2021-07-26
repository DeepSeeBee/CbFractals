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

    internal abstract class CMandelbrotPixelAlgorithm : CPixelAlgorithm<double>
    {
        internal CMandelbrotPixelAlgorithm(CPixelAlgorithmInput aInput)
        {
            this.PixelAlgorithmInput = aInput;
            var aParameters = aInput.ParameterSnapshot;
            var aZoom1 = aParameters.Get<double>(CParameterEnum.Zoom);
            var aZoom2 = 1d / aZoom1;
            var aCenterX = aParameters.Get<double>(CParameterEnum.CenterX);
            var aCenterY = aParameters.Get<double>(CParameterEnum.CenterY);
            var aCenter = new CVec2(aCenterX - 0.5d, aCenterY - 0.5d).Mul(2d);
            var aSizeMnd1 = CMandelbrotState.SizeMndFullRange;
            var aSizeMnd1Center1 = aSizeMnd1.GetRectCenter();
            var aSizeMnd1Center2 = aSizeMnd1Center1.Add(aSizeMnd1.GetRectSize().Mul(aCenter));
            var aSizeMnd2 = aSizeMnd1.GetRectAllignedAtCenter(aSizeMnd1Center2);
            var aSizeMnd3 = aSizeMnd2.Zoom(aZoom2);
            var aSizeMnd = aSizeMnd3;
            this.SizeMnd = aSizeMnd;
        }

        internal readonly CPixelAlgorithmInput PixelAlgorithmInput;
        internal CVec2 SizePxl => this.PixelAlgorithmInput.SizePxl;
        internal readonly CVec4 SizeMnd;
        internal CParameterSnapshot Parameters => this.PixelAlgorithmInput.ParameterSnapshot;
        internal Func<double, Color> GetColor => this.PixelAlgorithmInput.GetColorFunc;

        internal CVec2 GetMandelPos(CVec2Int aPixelCoord)
        {
            var aSizeMnd = this.SizeMnd;
            var aDx = this.SizePxl.Item1;
            var aDy = this.SizePxl.Item2;
            var aScale = new Func<CVec2, double, double>(delegate (CVec2 aRange, double aPos)
            {
                var aDelta = aRange.Item2 - aRange.Item1;
                var aOffset = aDelta * aPos;
                var aScaled = aRange.Item1 + aOffset;
                return aScaled;
            });
            var aGetMandelPos = new Func<CVec2Int, CVec2>
            (
                v =>
                new CVec2
                (
                    aScale(aSizeMnd.GetRectRangeX(), v.Item1 / (double)aDx),
                    aScale(aSizeMnd.GetRectRangeY(), v.Item2 / (double)aDy)
                )
            );
            var aMandelPos = aGetMandelPos(aPixelCoord);
            return aMandelPos;
        }
    }

    internal sealed class CClassicMandelbrotSetPixelAlgorithm : CMandelbrotPixelAlgorithm
    {
        public CClassicMandelbrotSetPixelAlgorithm(CPixelAlgorithmInput aInput) : base(aInput)
        {
        }


        internal override double GetPixelFkt(int aX, int aY)
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
            return itf;
            //var c = this.GetColor(itf);
            //return c;
        }
    }

    internal sealed class CSingleJuliaMandelbrotSetPixelAlgorithm : CMandelbrotPixelAlgorithm
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

        internal override double GetPixelFkt(int aX, int aY)
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
            return itf;
            //var c = this.GetColor(itf);
            //return c;
        }
    }

    internal sealed class CMultiJuliaMandelbrotSetPixelAlgorithm : CMandelbrotPixelAlgorithm
    {
        public CMultiJuliaMandelbrotSetPixelAlgorithm(CPixelAlgorithmInput aInput) : base(aInput)
        {

        }
        internal override double GetPixelFkt(int aX, int aY)
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
            return itf;

            //var cl = it == itm
            //       ? Colors.Black
            //       : this.GetColor(itf);
            //return cl;

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
