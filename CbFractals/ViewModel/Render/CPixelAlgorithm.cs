using CbFractals.Tools;
using CbFractals.ViewModel.Mandelbrot;
using CbFractals.ViewModel.PropertySystem;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace CbFractals.ViewModel.Render
{
    using CVec2 = Tuple<double, double>;
    using CVec4 = Tuple<double, double, double, double>;
    using CVec2Int = Tuple<int, int>;

    internal enum CPixelAlgorithmEnum
    {
        [CDataType(typeof(CClassicMandelbrotSetPixelAlgorithm))]
        MandelbrotClassicSet,

        [CDataType(typeof(CSingleJuliaMandelbrotSetPixelAlgorithm))]
        MandelbrotJuliaSingle,

        [CDataType(typeof(CMultiJuliaMandelbrotSetPixelAlgorithm))]
        MandelbrotJuliaMulti,
    }
    internal struct CPixelAlgorithmInput
    {
        internal CPixelAlgorithmInput(CVec2 aSizePxl, CVec4 aSizeMnd, CParameterSnapshot aParameterSnapshot, Func<double, Color> aGetColorFunc)
        {
            this.SizePxl = aSizePxl;
            this.SizeMnd = aSizeMnd;
            this.ParameterSnapshot = aParameterSnapshot;
            this.GetColorFunc = aGetColorFunc;
        }

        internal readonly CVec2 SizePxl;
        internal readonly CVec4 SizeMnd;
        internal readonly CParameterSnapshot ParameterSnapshot;
        internal Func<double, Color> GetColorFunc;

    }

    internal abstract class CPixelAlgorithm
    {
        internal CPixelAlgorithm(CPixelAlgorithmInput aInput)
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
        internal abstract Color RenderPixel(int aX, int aY);

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
}
