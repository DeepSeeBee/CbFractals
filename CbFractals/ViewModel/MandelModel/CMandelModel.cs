using System;
using System.Collections.Generic;
using System.Text;

namespace CbFractals.ViewModel.MandelModel
{

    public class CFullMandelModel
    {
        internal static void CalcMemoryCosts(int aMaxIterations, TimeSpan aTimeSpan, int aFps, double aMinZoom, double aMaxZoom, int aDx, int aDy)
        {
            var aGetBitCount = new Func<int, int>(m =>
            {
                var r = m;
                var b = 0;
                while (r > 0)
                {
                    r -= (1 << b);
                    ++b;
                }
                return b;
            });
            var aBitCount = aGetBitCount(aMaxIterations);
            var aFrames = (int)aFps * aTimeSpan.TotalSeconds;
            var aZoomRange = aMaxZoom / aMinZoom;
            var aPixels1 = aDx * aDy;
            var aPixels2 = aPixels1 * aZoomRange;
            var aBits = aPixels2 * aBitCount;
            var aTb = aBits / 8d / 1024d / 1024d / 1024d;
            System.Diagnostics.Debug.Write("Model needs " + Math.Round(aTb, 3).ToString() + " GB memory" + Environment.NewLine);
        }

        internal static void CalcMemoryCosts()
        {
            var a9Bits = 511;
            var a10Bit = 1023;
            var a11Bit = 2047;
            CalcMemoryCosts(a11Bit, TimeSpan.FromMinutes(1), 40, 1, 5000, 1920, 1080);
        }



    }

}
