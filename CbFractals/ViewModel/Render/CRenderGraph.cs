using System;
using System.Collections.Generic;
using System.Text;
using CbFractals.Tools;
using CbFractals.ViewModel.Mandelbrot.Obsolete;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Linq;

namespace CbFractals.ViewModel.Render
{
    using CVec2Int = Tuple<int, int>;

    internal sealed class CColorFktThroughColorAlgorithmSink : CSink<double>
    {
        internal CColorFktThroughColorAlgorithmSink(CColorAlgorithm aColorAlgorithm, CSink<Color> aInnerSink)
        {
            this.ColorAlgorithm = aColorAlgorithm;
            this.InnerSink = aInnerSink;
        }
        private readonly CColorAlgorithm ColorAlgorithm;
        private readonly CSink<Color> InnerSink;
        internal override void Write(double aColorFkt)
            => this.InnerSink.Write(this.ColorAlgorithm.GetColor(aColorFkt));
    }

    internal sealed class CColorArrayToBitmapSourceSink : CSink<Color[]>
    {
        internal CColorArrayToBitmapSourceSink(CSink<BitmapSource> aInnerSink, CVec2Int aSizePxl)
        {
            this.InnerSink = aInnerSink;
            this.SizePxl = aSizePxl;
        }
        internal readonly double DpiX = 96;
        internal readonly double DpiY = 96;
        private readonly CSink<BitmapSource> InnerSink;
        internal readonly CVec2Int SizePxl;
        internal override void Write(Color[] aPixels)
        {
            var aDx = this.SizePxl.Item1;
            var aDy = this.SizePxl.Item2;
            var aColorToRgb24 = new Func<Color, byte[]>(c =>
            {
                var a = new byte[3];
                a[0] = c.R;
                a[1] = c.G;
                a[2] = c.B;
                return a;
            });
            var aPixelsRgb24a = (from aPixel in aPixels.Select(aColorToRgb24)
                                 from aColor in aPixel
                                 select aColor).ToArray();
            var aBytesPerPixel = 3;
            var aRest = (aBytesPerPixel * aDx) % 4;
            var aStrideInBytes = aDx * aBytesPerPixel + aRest;
            var aGap = new byte[aRest];
            var aGetLine = new Func<int, IEnumerable<byte>>(aLine => aPixelsRgb24a.Skip(aLine * aDx * aBytesPerPixel).Take(aDx * aBytesPerPixel).Concat(aGap));
            var aPixelsRgb24
                = (from aLine in (from aY in Enumerable.Range(0, (int)aDy) select aGetLine(aY))
                   from aByte in aLine
                   select aByte).ToArray();
            var aBitmapSource = BitmapSource.Create(aDx, aDy, this.DpiX, this.DpiY, PixelFormats.Rgb24, default(BitmapPalette), aPixelsRgb24, aStrideInBytes);
            this.InnerSink.Write(aBitmapSource);
        }
    }

}
