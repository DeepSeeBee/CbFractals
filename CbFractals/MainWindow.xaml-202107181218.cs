using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CbFractals
{
    using CVec4 = Tuple<double, double, double, double>;
    using CVec2 = Tuple<double, double>;
    using CRenderInput = Tuple<Tuple<double, double>,                    // ImageSize
                               Tuple<double, double, double, double>,    // MandelSpace 
                               Tuple<double, double>,                    // MandelCenter
                               int,                                      // ZoomCount
                               double,                                   // TotalZoom
                               bool                                      // DisableCenterAnimation
                                >; 
    using CRenderOutput = Tuple<BitmapSource,                            // BitmapSource, 
                                Tuple<double, double, double, double>,   // MandelSpace
                                Tuple<double, double>,                   // MandelCenter
                                CbFractals.CBaseColorEnum,               // RarestColor
                                CbFractals.CBaseColorEnum,               // DominatingColor
                                bool                                     // DisableCenterAnimation
                                >;
    using CVec2Int = Tuple<int, int>;

    internal enum CBaseColorEnum
    {
        Red = 0,
        Green = 1,
        Blue = 2
    }
    internal static class CVec4Extensions
    {
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
                    => new CVec2(aRect.Item2, aRect.Item2 + aRect.Item4 );
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
        internal static CVec4 Zoom(this CVec4 aRect, double aZoom)
            => aRect.GetRectNormalize().Mul(aZoom).GetRectTranslate(aRect.GetRectPos().Mul(aZoom));

        internal static CVec4 Zoom(this CVec4 aRect, double aZoom, CVec2 aCenter)
            => aRect.Zoom(aZoom).GetRectAllignedAtCenter(aCenter);
        internal static CVec2 GetRectCenter(this CVec4 aRect)
            => new CVec2(aRect.GetRectLeft() + aRect.GetRectWidth() / 2d,
                         aRect.GetRectTop() + aRect.GetRectHeight() / 2d);
        internal static CVec2 ToVec2(this Point pt)
            => new CVec2(pt.X, pt.Y);
        internal static Point ToPoint(this CVec2 v)
            => new Point(v.Item1, v.Item2);
        internal static CVec2 ToVec2(this CVec2Int v)
            => new CVec2(v.Item1, v.Item2);
        internal static void CatchUnexpected(this Exception e, object aCatcher)
        {
            System.Windows.MessageBox.Show(e.Message, aCatcher.ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
        }
        internal static double GetHypothenuse(this CVec2 v)
            => Math.Sqrt(v.Item1 * v.Item1 + v.Item2 * v.Item2);
        internal static double GetDistanceMaybeNegative(this CVec2 c1, CVec2 c2)
            => c1.Subtract(c2).GetHypothenuse();
        internal static double GetDistanceAbs(this CVec2 c1, CVec2 c2)
            => Math.Abs(c1.GetDistanceMaybeNegative(c2));
    }

    internal static class CLazyLoad
    { 
        internal static T Get<T>(ref T? v, Func<T> n) where T :struct
        {
            if (!v.HasValue)
                v = n();
            return v.Value;
        }
        internal static T Get<T>(ref T v, Func<T> n) where T : class
        { 
            if(! (v is object))
            {
                v = n();
            }
            return v;
        }
    }



    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            this.BeginRender(true);
        }
        private float ChannelHue(double c)
        {
            var m1a = c % 1d;
            var m1 = m1a < 0d
                   ? 1d + m1a
                   : m1a
                   ;
            var max1 = 1.0f / 3d * 2d;
            var max2 = max1 / 2d;
            var h = m1 < max2
                  ? m1 / 2f / max2
                  : m1 < max1
                  ? (max2 - (m1 - max2)) / max2
                  : 0f
                  ;
            var hm = (float)(h % 1d);
            return hm;
        }
        private Color Hue(double c, double ho)
        {
            var max = 1.0f / 3d;
            //var o = 0d;
            var o = (- max / 2d ) * ho; // TEST
            var r = ChannelHue(c + max * 0d + o);
            var g = ChannelHue(c + max * 1d + o);
            var b = ChannelHue(c + max * 2d + o);
            return Color.FromScRgb(1f, r, g, b);
        }
        private double? ZoomSliderValueM;
        public double ZoomSliderValue
        {
            get => CLazyLoad.Get(ref this.ZoomSliderValueM, () => this.ZoomSlider.Maximum);
            set
            {
                this.ZoomSliderValueM = value;
                this.OnPropertyChanged(nameof(this.ZoomSliderValue));
                //this.HueSlider.Background = new SolidColorBrush(Hue((float)value));
                //this.Render();
            }
        }

        private double TotalZoom = 1d;
        private int ZoomCount = 0;
        private void OnDrawButtonClick(object sender, RoutedEventArgs e)
        {
            //this.BeginRender();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string aName)
        {
            if (this.PropertyChanged is object)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(aName));
            }
        }

        private bool RenderIsPendingM;
        private bool RenderIsPending
        {
            get => this.RenderIsPendingM;
            set
            {
                this.RenderIsPendingM = value;

                this.OnPropertyChanged(nameof(this.RenderBatchStartButtonIsEnabled));
                this.OnPropertyChanged(nameof(this.ResetButtonIsEnabled));
                this.OnPropertyChanged(nameof(this.ZoomSliderIsEnabled));
                this.ProgressBar.Visibility = value ? Visibility.Visible : Visibility.Hidden;
                this.ZoomSlider.IsEnabled = !value;
            }
        }

        private void BeginRender(CRenderInput aRenderInput)
        {
            if (!this.RenderIsPending)
            {
                this.RenderIsPending = true;

                this.RenderTaskNullable = Task.Factory.StartNew(new Action(delegate ()
                {
                    this.BeginRenderCore(aRenderInput);  //aDx, aDy, aCenter, aZoomCount
                }));
            }
        }
        private void BeginRender(bool aDisableCenterAnimation)
        {
            var aRenderInput = this.NewRenderInput(aDisableCenterAnimation);
            this.BeginRender(aRenderInput);
        }
        private Task RenderTaskNullable;        
        private CVec2 NewMandelCenterFkt(CVec4 aMandelSpace, CVec2 aMandelCenter)
            => aMandelCenter.Subtract(aMandelSpace.GetRectPos()).Divide(aMandelSpace.GetRectSize());
        private void UpdateMandelSpace()
            => this.MandelSpace = this.MandelSpace.Zoom(1d, this.MandelSpaceCenter);
        //this.MandelCenterFkt = this.NewMandelCenterFkt(this.MandelSpace, this.MandelSpaceCenter);

        private void EndRender(Func<CRenderOutput> aNewRenderResult)
        {
            if (this.RenderIsPending)
            {
                var aRenderResult = aNewRenderResult();
                this.Image.Source = aRenderResult.Item1;
                var aMandelSpace = aRenderResult.Item2;
                var aMandelCenter = aRenderResult.Item3;
                var aRarestColor = CColorDetector.GetColor(aRenderResult.Item4);
                var aDominantColor = CColorDetector.GetColor(aRenderResult.Item5);
                var aDisableCenterAnimation = aRenderResult.Item6;

                var aMandelCenterFkt = this.NewMandelCenterFkt(aMandelSpace, aMandelCenter); // aMandelCenter.Subtract(aMandelSpace.GetRectPos()).Divide(aMandelSpace.GetRectSize());
                if(aDisableCenterAnimation)
                {
                    this.MandelCenterFkt = aMandelCenterFkt;
                }
                this.MandelCenterFktTest = aMandelCenterFkt;
                this.RarestColorBrush = new SolidColorBrush(aRarestColor);
                this.DominantColorBrush = new SolidColorBrush(aDominantColor); ;
                this.RenderTaskNullable = null;
                this.RenderIsPending = false;
                if(this.RenderBatchIsPending)
                {
                    this.RenderBatchFrameNext();                
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private CVec4 MandelSpaceMax => new CVec4(-2.5d, -1d, 3.5d, 2d);
        private CVec4 MandelSpaceM;
        private CVec4 MandelSpace
        {
            get => CLazyLoad.Get(ref this.MandelSpaceM, () => this.MandelSpaceMax);
            set => this.MandelSpaceM = value;
        }

        private CVec2 ImageSize => new CVec2(this.Canvas.Width, this.Canvas.Height);
        private CVec2 MandelCenterFkt
        {
            get => this.ZoomCenter.ToVec2().Divide(this.ImageSize);
            set => this.ZoomCenter = value.Mul(this.ImageSize).ToPoint();
        }

        #region MandelCenterFkt
        private CVec2 MandelCenterFktTargetM;
        private CVec2 MandelCenterFktTarget
        {
            get => this.MandelCenterFktTargetM is object ? this.MandelCenterFktTargetM : this.MandelCenterFkt;
            set => this.MandelCenterFktTargetM = value;
        }
        private CVec2 MandelCenterFktTest
        {
            get => this.Test ? this.MandelCenterFktTarget : this.MandelCenterFkt;
            set
            {
                if (this.Test)
                    this.MandelCenterFktTarget = value;
                else
                    this.MandelCenterFkt = value;
            }
        }
        private double Fps = 30d;
        private CVec2 Speed => this.ImageSize.Divide(this.Fps);
        private CVec2 SpeedFkt => this.Speed.Divide(this.ImageSize);
        private void AnimMandelCenterFkt()
        {
            if(this.Test)
                this.MandelCenterFkt = this.MandelCenterFkt.Add(this.MandelCenterFktTarget.Subtract(this.MandelCenterFkt).Mul(this.SpeedFkt));
        }
        #endregion
        private bool Test = false;
        private CVec2 MandelSpaceCenter => this.MandelSpace.GetRectPos().Add(this.MandelSpace.GetRectSize().Mul(this.MandelCenterFkt));
        private CRenderInput NewRenderInput(bool aDisableCenterController) => new CRenderInput
            (
                this.ImageSize, 
                this.MandelSpace, 
                this.MandelSpaceCenter, 
                this.ZoomCount, 
                this.ZoomSliderValue,
                aDisableCenterController
            );
     

        private class CColorDetector
        {
            internal CColorDetector(Color aCriteriaColor, CVec2 aCriteriaPos, CVec2 aPosMax)
            {
                this.PosWeightMax = aPosMax.GetHypothenuse();
                this.CriteriaPos = aCriteriaPos;
                this.CriteriaColor = aCriteriaColor;
            }

            private double GetColorDiff(byte aBaseColor, byte c)
            {
                var aTest = true; // TEST
                var cd0 = aBaseColor / 255d; // 0=low
                var cd1 = ((double)(Math.Max(aBaseColor, c) - Math.Min(aBaseColor, c))) / 255d; // 1=low
                var cd2 = 1d - cd1; // 0=low
                var cd3 = aBaseColor * cd2; // 0=low
                var cd4 = aTest 
                        ? 1d - cd3 // 1=low
                        : cd1       // 1=low
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
                { // new CVec2(250d, 250d)
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
                        return Color.FromRgb(255,0,0);
                    case CBaseColorEnum.Green:
                        return Color.FromRgb(0, 255, 0);
                    case CBaseColorEnum.Blue:
                        return Color.FromRgb(0, 0, 255);
                    default:
                        throw new ArgumentException();
                }
            }

        }


        private void BeginRenderCore(CRenderInput aRenderInput)
        {
            var aImageSize = aRenderInput.Item1;
            var aDx = (int)aImageSize.Item1;
            var aDy = (int)aImageSize.Item2;
            var aMandelRect = aRenderInput.Item2;
            var aMandelCenter = aMandelRect.GetRectCenter(); // aRenderInput.Item3;
            var aZoomCount = aRenderInput.Item4;
            var aStepZoom = aRenderInput.Item5;
            var aTotalZoom = Math.Pow(aStepZoom, aZoomCount);
            var aImage = this.Image;
            var aPixelCoords = from aX in Enumerable.Range(0, (int)aDx)
                               from aY in Enumerable.Range(0, (int)aDy)
                               select new CVec2Int(aX, aY);
            var aScale = new Func<CVec2, double, double>(delegate (CVec2 aRange, double aPos)
            {
                var aDelta = aRange.Item2 - aRange.Item1;
                var aOffset = aDelta * aPos;
                var aScaled = aRange.Item1 + aOffset;
                return aScaled;
            });
            var aPixels = new Color[aDx * aDy];
            var odd = false;
            var zc0 = odd
                    ? aZoomCount + ((aZoomCount + 1) % 2)
                    : aZoomCount + ((aZoomCount + 0) % 2)
                    ;
            var zc1 = Math.Max(zc0, 2);
            var zc = zc1;
            //var zc = Math.Pow(zc1, Math.Max(1,-Math.Log10(aTotalZoom))); // TEST
            var aGetMandelPos = new Func<CVec2Int, CVec2>
            (
                v =>
                new CVec2
                (
                    aScale(aMandelRect.GetRectRangeX(), v.Item1 / (double)aDx),
                    aScale(aMandelRect.GetRectRangeY(), v.Item2 / (double)aDy)
                )
            );


            var l = 1;
            //var l = Math.Max(1, -Math.Log10(aTotalZoom / 10f)); // TEST


            //var lh = 0d;
            var lh = -Math.Log10(aTotalZoom); // TEST

            foreach (var aPixelCoord in aPixelCoords)
            {
                var aMandelPos = aGetMandelPos(aPixelCoord);
                var x0 = aMandelPos.Item1;
                var y0 = aMandelPos.Item2;
                var x = 0.0d;
                var y = 0.0d;
                var it = 0;
                var itm = 100 * l;
                var d = 4d;
                while (Math.Pow(x, zc) + Math.Pow(y, zc ) <= Math.Pow(d, zc)
                    && it < itm)
                {
                    var xtmp = x * x - y * y + x0;
                    y = 2 * x * y + y0;
                    x = xtmp;
                    ++it;
                }
                var itf = (float)it / (float)itm;
                var b = 255.0d * itf;
                var co = 1.5f;
                
                var c = this.Hue(itf + (1f / 3f * co), lh);
                var aPixelIdx = aPixelCoord.Item2 * aDy + aPixelCoord.Item1;
                aPixels[aPixelIdx] = c;
            }
            var aColorToRgb24 = new Func<Color, byte[]>(c =>
                {
                    var a = new byte[3];
                    a[0] = c.R;
                    a[1] = c.G;
                    a[2] = c.B;
                    return a;
                });

            var aPixelsRgb24 = (from aPixel in aPixels.Select(aColorToRgb24)
                                from aColor in aPixel
                                select aColor).ToArray();

            var aRarestAndDominatingColor = CColorDetector.GetRarestAndDominatingColor(aPixels);
            var aRarestColor = aRarestAndDominatingColor.Item1;
            var aDominatingColor = aRarestAndDominatingColor.Item2;
            var aAutoCenter = true;
            CVec2 aNewCenter;
            if (aAutoCenter)
            {
                var aMandelToPixel = new Func<CVec2, CVec2>(v => v.Subtract(aMandelRect.GetRectPos()).Divide(aMandelRect.GetRectSize()).Mul(aImageSize));
                var aPixelToMandel = new Func<CVec2, CVec2>(v => v.Divide(aImageSize).Mul(aMandelRect.GetRectSize()).Add(aMandelRect.GetRectPos()));
                var aPixelCenter = aMandelToPixel(aMandelCenter); // aMandelCenter.Subtract(aMandelSpaceZoomed.GetRectPos()).Divide(aMandelSpaceZoomed.GetRectSize()).Mul(aImageSize);
                var aCenterDetector = new CColorDetector(CColorDetector.GetColor(aRarestColor), aPixelCenter, aImageSize);
                foreach (var aPixelCoord in aPixelCoords)
                {
                    var aPixelIdx = aPixelCoord.Item2 * aDy + aPixelCoord.Item1;
                    var aPixel = aPixels[aPixelIdx];
                    var aPixelCoordVec2 = aPixelCoord.ToVec2();
                    aCenterDetector.Add(aPixel, aPixelCoordVec2);
                }
                aNewCenter = aPixelToMandel(aCenterDetector.NewCenter);
            }
            else
            {
                aNewCenter = aMandelCenter;
            }

            var aDisableCenterAnimation = aRenderInput.Item6;
            var aNewBitmapSource = new Func<BitmapSource>(() => BitmapSource.Create((int)aDx, (int)aDy, 96d, 96d, PixelFormats.Rgb24, default(BitmapPalette), aPixelsRgb24, 3 * aDx));
            var aNewNewRenderResult = new Func<CRenderOutput>(() => new CRenderOutput
            (
                aNewBitmapSource(),
                aMandelRect,
                aNewCenter,
                aRarestColor,
                aDominatingColor,
                aDisableCenterAnimation 
            ));
            this.Dispatcher.BeginInvoke(new Action(delegate () {
                try
                {
                    this.EndRender(aNewNewRenderResult);
                }   
                catch(Exception aExc)
                {
                    aExc.CatchUnexpected(this);
                }
            }));
        }

        #region RarestColor
        private Brush RarestColorBrushM;
        public Brush RarestColorBrush
        {
            get => this.RarestColorBrushM;
            set
            {
                this.RarestColorBrushM = value;
                this.OnPropertyChanged(nameof(this.RarestColorBrush));
            }
        }
        #endregion
        #region DominantColor
        private Brush DominantColorBrushM;
        public Brush DominantColorBrush
        {
            get => this.DominantColorBrushM;
            set
            {
                this.DominantColorBrushM = value;
                this.OnPropertyChanged(nameof(this.DominantColorBrush));
            }
        }
        #endregion
        #region ZoomCenter
        private Point? ZoomCenterM;// = new Point(500, 500);
        public Point ZoomCenter
        {
            get => CLazyLoad.Get(ref ZoomCenterM, () => new Point(this.Canvas.Width / 2d, this.Canvas.Height / 2d));
            set
            {
                this.ZoomCenterM = value;
               // this.UpdateMandelSpace();
                this.OnPropertyChanged(nameof(this.ZoomEllipsePos));
            }
        }
        #endregion
        public Point ZoomEllipsePos
        {
            get => new Point(this.ZoomCenter.X - this.Ellipse.Width / 2d,
                             this.ZoomCenter.Y - this.Ellipse.Height / 2d);
        }

        private Point? ZoomCenterByMouse;

        private void MoveCenter(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed
            && this.Canvas.IsMouseOver
            && !this.RenderIsPending)
            {
                this.ZoomCenterByMouse = e.GetPosition(this.Canvas);
            }
        }

        private void OnImageMouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                this.MoveCenter(e);
            }
            catch (Exception aExc)
            {
                aExc.CatchUnexpected(this);
            }
        }


        private void OnImageMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                this.MoveCenter(e);
            }
            catch (Exception aExc)
            {
                aExc.CatchUnexpected(this);
            }
        }

        private void Zoom(bool aZoomIn, bool aDisableCenterAnimation)
        {
            if (!this.RenderIsPending)
            {
                var aZoom = this.ZoomSliderValue;
                this.TotalZoom = aZoomIn 
                               ? this.TotalZoom * aZoom
                               : this.TotalZoom / aZoom
                               ;
                this.ZoomCount = aZoomIn 
                               ? this.ZoomCount + 1
                               : this.ZoomCount - 1
                               ;                
                this.MandelSpace = this.MandelSpace.Zoom(aZoom, this.MandelSpaceCenter);
                this.BeginRender(aDisableCenterAnimation);
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            try
            {
                if (e.Key == Key.Add)
                {
                    var aDisableCenterAnimation = false;
                    this.NextFrame(aDisableCenterAnimation);
                    e.Handled = true;
                }
                else if (e.Key == Key.Subtract)
                { // Nur für den notfall, AnimMandelCenterFkt lässt sich nicht rückgängig machen.
                    var aDisableCenterAnimation = false;
                    this.Zoom(false, aDisableCenterAnimation);
                    e.Handled = true;
                }
            }
            catch (Exception aExc)
            {
                aExc.CatchUnexpected(this);
            }
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseUp(e);

            try
            {
                if (this.ZoomCenterByMouse.HasValue)
                {
                    //this.BeginRender();
                    this.ZoomCenter = this.ZoomCenterByMouse.Value;
                    this.ZoomCenterByMouse = default;
                    this.MandelCenterFktTargetM = default;
                    this.MandelSpace = this.MandelSpace.Zoom(1d, this.MandelSpaceCenter);
                    var aDisableCenterAnimation = true;
                    this.BeginRender(aDisableCenterAnimation);
                    //this.MandelSpace = this.MandelSpace.Zoom(this.ZoomSliderValue, this.MandelSpaceCenter);
                }
            }
            catch (Exception aExc)
            {
                aExc.CatchUnexpected(this);
            }
        }

        private bool RenderBatchIsPendingM;
        public bool RenderBatchIsPending
        {
            get => this.RenderBatchIsPendingM;
            set
            {
                this.RenderBatchIsPendingM = value;
                this.OnPropertyChanged(nameof(this.RenderBatchIsPending));
                this.OnPropertyChanged(nameof(this.RenderBatchStartButtonIsEnabled));
                this.OnPropertyChanged(nameof(this.RenderBatchCancelButtonIsEnabled));
                this.OnPropertyChanged(nameof(this.FrameCountProgressIsIndeterminate));
                this.OnPropertyChanged(nameof(this.RenderBatchCancelButtonIsEnabled));
                this.OnPropertyChanged(nameof(this.RenderBatchFramesGridVisibility));
                this.OnPropertyChanged(nameof(this.RenderBatchProgressGridVisibility));
                this.OnPropertyChanged(nameof(this.ResetButtonIsEnabled));
                this.OnPropertyChanged(nameof(this.ZoomSliderIsEnabled));
            }
        }
        public bool ZoomSliderIsEnabled { get => !this.RenderIsPending && !this.RenderIsPending; }
        public bool ResetButtonIsEnabled { get => !this.RenderIsPending && !this.RenderIsPending; }
        public Visibility RenderBatchFramesGridVisibility => this.RenderBatchIsPending ? Visibility.Collapsed : Visibility.Visible;
        public Visibility RenderBatchProgressGridVisibility => this.RenderBatchIsPending ? Visibility.Visible : Visibility.Collapsed;
        public bool FrameCountProgressIsIndeterminate { get => this.RenderBatchIsPending; }
        public bool RenderBatchStartButtonIsEnabled { get => !this.RenderIsPending && !this.RenderIsPending; }
        private int RenderBatchFrameNrM;
        public int RenderBatchFrameNr
        {
            get => this.RenderBatchFrameNrM;
            set
            {
                this.RenderBatchFrameNrM = value;
                this.OnPropertyChanged(nameof(this.RenderBatchFrameNr));
            }
        }
        private int RenderBatchFrameCountM = 5;
        public int RenderBatchFrameCount
        {
            get => this.RenderBatchFrameCountM;
            set
            {
                if (value > 0)
                {
                    this.RenderBatchFrameCountM = value;
                    this.OnPropertyChanged(nameof(this.RenderBatchFrameCount));
                }
                else
                {
                    throw new ArgumentException();
                }
            }
        }

        private void SaveFrame()
        {
            var aDirName = "frames";
            var aDir = new DirectoryInfo(System.IO.Path.Combine(new FileInfo(this.GetType().Assembly.Location).Directory.FullName, aDirName));            
            var aFrameNr = this.RenderBatchFrameNr;
            var aFrameNrLen = this.RenderBatchFrameCount.ToString().Length;
            var aFrameNrText = aFrameNr.ToString().PadLeft(aFrameNrLen, '0');
            var aFileName = aFrameNrText + ".bmp";
            var aFileInfo = new FileInfo(System.IO.Path.Combine(aDir.FullName, aFileName));
            this.SaveBitmap(aFileInfo);
        }

        private void SaveBitmap(FileInfo aFileInfo)
        {
            aFileInfo.Directory.Create();
            var aBitmapSource = (BitmapSource)this.Image.Source; // (BitmapSource)Clipboard.GetImage();
            var aEncoder = new BmpBitmapEncoder();
            aEncoder.Frames.Add(BitmapFrame.Create(aBitmapSource));
            using var aFileStream = new FileStream(aFileInfo.FullName, FileMode.Create);
            aEncoder.Save(aFileStream);
        }
        private void NextFrame(bool aDisableCenterAnimation)
        {
            this.AnimMandelCenterFkt();
            this.RenderBatchFrameNr++;
            this.Zoom(true, aDisableCenterAnimation);
            //this.BeginRender(aDisableCenterAnimation);
        }

        private void RenderBatchFrameNext()
        {
            if(this.RenderBatchIsPending)
            {
                Exception aSaveFrameExc;
                try
                {
                    this.SaveFrame();
                    aSaveFrameExc = default;
                }
                catch (Exception aExc)
                {
                    aExc.CatchUnexpected(this);
                    aSaveFrameExc = aExc;
                }

                if(this.RenderBatchCancelIsPending)
                {
                    this.RenderBatchCancelIsPending = false;
                    this.RenderBatchIsPending = false;
                }
                else if(aSaveFrameExc is object)
                {
                    this.RenderBatchIsPending = false;
                }
                else if(this.RenderBatchFrameNr < this.RenderBatchFrameCount)
                {
                    var aDisableCenterAnimation = false;
                    this.NextFrame(aDisableCenterAnimation); 
                }
                else
                {
                    this.RenderBatchIsPending = false;
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private void OnStartRenderBatchButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!this.RenderBatchIsPending
                && !this.RenderIsPending)
                {
                    this.RenderBatchIsPending = true;
                    var aDisableCenterAnimation = false;
                    this.BeginRender(aDisableCenterAnimation);
                }
            }
            catch(Exception aExc)
            {
                aExc.CatchUnexpected(this);
            }
        }

        private bool RenderBatchCancelIsPendingM;
        public bool RenderBatchCancelIsPending
        {
            get => this.RenderBatchCancelIsPendingM;
            set 
            {
                this.RenderBatchCancelIsPendingM = value;
                this.OnPropertyChanged(nameof(this.RenderBatchCancelButtonIsEnabled));
            }
        }
        public bool RenderBatchCancelButtonIsEnabled => this.RenderBatchIsPending && !this.RenderBatchCancelIsPending;
        private void OnRenderBatchCancelButtonClick(object sender, RoutedEventArgs e)
        {
            this.RenderBatchCancelIsPending = true;
        }


        private void Reset()
        {
            this.RenderBatchFrameNr = 0;
            this.MandelSpaceM = default;
            this.ZoomCenterM = default;
            this.MandelCenterFktTargetM = default;
        }
        private void ResetButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Reset();
                var aDisableCenterAnimation = false;
                this.BeginRender(aDisableCenterAnimation);
            }
            catch(Exception aExc)
            {
                aExc.CatchUnexpected(this);
            }
        }

    }
}
