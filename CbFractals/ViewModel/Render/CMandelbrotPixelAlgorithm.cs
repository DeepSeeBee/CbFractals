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
        [CType(typeof(CClassicMandelbrotSetPixelAlgorithm))]
        MandelbrotClassicSet,

        [CType(typeof(CSingleJuliaMandelbrotSetPixelAlgorithm))]
        MandelbrotJuliaSingle,

        [CType(typeof(CMultiJuliaMandelbrotSetPixelAlgorithm))]
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

    internal abstract class CPixelAlgorithm<TResult>
    {
        internal abstract TResult RenderPixel(int aX, int aY);
    }

}
