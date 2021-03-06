using CbFractals.Gui.Wpf;
using CbFractals.Tools;
using CbFractals.ViewModel.Mandelbrot.Obsolete;
using CbFractals.ViewModel.PropertySystem;
using CbFractals.ViewModel.Render;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace CbFractals.ViewModel.Mandelbrot
{
    using CVec2 = Tuple<double, double>;
    using CVec4 = Tuple<double, double, double, double>;
    using CVec2Int = Tuple<int, int>;
    using CRenderFrameInput = Tuple<Tuple<double, double>,               // ImageSize
                               Tuple<double, double, double, double>,    // MandelSpace 
                               Tuple<double, double>,                    // MandelCenter
                               int,                                      // ZoomCount
                               double,                                   // TotalZoom
                               Tuple<double, double>,                    // MandelTargetCenter
                               CParameterSnapshot             // ParameterSnapshot
                                >;
    using CRenderFrameOutput = Tuple<BitmapSource,                       // BitmapSource, 
                            Tuple<double, double, double, double>,   // SizeMnd
                            Tuple<double, double>,                   // CenterMnd
                            CBaseColorEnum,               // RarestColor
                            CBaseColorEnum,               // DominatingColor
                            Tuple<double, double>                   // TargetCenterMnd
                                                                    //  CMandelbrotState                         // PreviousState
                            >;
    using CRenderFrameSegmentInput = Tuple<int,                            // YStart
                                           int                             // YCount                                           
                                           >;

    public sealed class CMainWindowLoadedState : CMandelbrotState
    {
        internal CMainWindowLoadedState(MainWindow aMainWindow) : base(aMainWindow)
        {
        }

        internal void OnMainWindowLoaded()
        {
            this.Enter();
        }
        internal override void OnEntered()
        {
            base.OnEntered();
            this.CurrentState.UpdateFrameCountProposal();
            this.CurrentState.BeginRenderFrame();
        }
    }

    public sealed class CEndRenderFrameState : CMandelbrotState
    {
        internal CEndRenderFrameState(CMandelbrotState aPreviousState, CRenderFrameOutput aRenderFrameOutput, CEndRenderFrameEnum aCmd)
        :
            base(aPreviousState, aRenderFrameOutput, aCmd)
        {
        }

        internal override void OnEntered()
        {
            base.OnEntered();
            if (this.RenderMovieIsPending.Value)
            {
                this.OnRenderMovieEndRenderFrame();
            }
        }
        internal override void OnEnterCanceled()
        {
            base.OnEnterCanceled();

            this.CurrentState.EndCancelRenderMovie();
        }
    }

    public sealed class CMoveState : CMandelbrotState
    {
        internal CMoveState(CMandelbrotState aPreviousState, CVec2 aNewCenter, CSetCenterEnum aCmd)
            : base(aPreviousState, aNewCenter, aCmd)
        {
        }
        internal override void OnEntered()
        {
            base.OnEntered();

            this.BeginRenderFrame();
        }
    }

    public sealed class CBeginRenderFrameState : CMandelbrotState
    {
        internal CBeginRenderFrameState(CMandelbrotState aPreviousState, CRenderFrameInput aRenderFrameInput, CBeginRenderFrameEnum aCmd)
        :
            base(aPreviousState, aCmd)
        {
            this.RenderFrameInput = aRenderFrameInput;
        }
        internal readonly CRenderFrameInput RenderFrameInput;
        private Task RenderFrameTaskNullable;

        internal override void OnEntered()
        {
            base.OnEntered();

            this.RenderFrameTaskNullable = Task.Factory.StartNew(new Action(delegate ()
            {
                this.BeginRenderFrameCore(this.RenderFrameInput);
            }));
        }
    }

    public sealed class CZoomState : CMandelbrotState
    {
        internal CZoomState(CMandelbrotState aPreviousState, double aZoomFaktor, CZoomEnum aCmd) : base(aPreviousState, aZoomFaktor, aCmd)
        {
        }
        internal override void OnEntered()
        {
            base.OnEntered();
            this.BeginRenderFrame();
        }
    }

    public sealed class CBeginRenderMovieState : CMandelbrotState
    {

        internal CBeginRenderMovieState(CMandelbrotState aPreviousState, CBeginRenderMovieEnum aCmd) : base(aPreviousState, aCmd)
        {

        }
        internal override void OnEntered()
        {
            base.OnEntered();
            this.BeginRenderFrame();
        }
    }

    public sealed class CEndRenderMovieState : CMandelbrotState
    {
        internal CEndRenderMovieState(CMandelbrotState aPreviousState, CEndRenderMovieEnum aCmd) : base(aPreviousState, aCmd)
        {

        }
    }

    public sealed class CBeginCancelRenderMovieState : CMandelbrotState
    {
        internal CBeginCancelRenderMovieState(CMandelbrotState aPreviousState, Exception aCancelExc, CBeginCancelRenderMovieEnum aCmd) : base(aPreviousState, aCmd)
        {
            this.CancelExc = aCancelExc;
        }
        internal readonly Exception CancelExc;
    }

    public sealed class CEndCancelRenderMovieState : CMandelbrotState
    {
        internal CEndCancelRenderMovieState(CMandelbrotState aPreviousState, CEndCancelRenderMovieEnum aCmd) : base(aPreviousState, aCmd)
        {
        }
    }

    public sealed class CNextFrameState : CMandelbrotState
    {
        internal CNextFrameState(CMandelbrotState aPreviousState, CNextFrameEnum aCmd) : base(aPreviousState, aCmd)
        {

        }
        internal override void OnEntered()
        {
            base.OnEntered();
            this.Zoom(this.MainWindow.ZoomInFaktor);
        }
    }

    public abstract class CMandelbrotState : CViewModel
    {
        #region ctor
        internal static CVec4 SizeMndFullRange => new CVec4(-2.5d, -1d, 3.5d, 2d);
        private static CVec2 CenterMndDefault => new CVec2(-1.1398974475396566d, -0.2831806035333824d);
        private static CVec4 SizeMndDefault => SizeMndFullRange.GetRectAllignedAtCenter(CenterMndDefault);
        private static double ZoomFaktorDefault(MainWindow aMainWindow) => aMainWindow.ZoomSlider.Maximum;
        private static CVec2 CenterCurrentFktDefault => new CVec2(0.5, 0.5);
        private static double FpsDefault => 30d;
        private static BitmapSource ImageSourceDefault(CVec2 aSizePxl) => BitmapSource.Create((int)aSizePxl.Item1, (int)aSizePxl.Item2, 96, 96, PixelFormats.Rgb24, default(BitmapPalette), new byte[(int)aSizePxl.Item1 * (int)aSizePxl.Item2 * 3], (int)(3 * aSizePxl.Item1));
        private DirectoryInfo DirectoryInfoDefault => new DirectoryInfo(System.IO.Path.Combine(new FileInfo(this.GetType().Assembly.Location).Directory.FullName, "frames"));
        internal CMandelbrotState(MainWindow aMainWindow)
        {
            var aDirectoryInfo = DirectoryInfoDefault;
            var aSizeMnd = SizeMndFullRange;
            var aSizePxl = new CVec2(aMainWindow.Canvas.Width, aMainWindow.Canvas.Height);
            var aBitmapSource = ImageSourceDefault(aSizePxl);
            var aCenterTargetMnd = aSizeMnd.GetRectCenter();
            var aDominantColorBrush = Brushes.Transparent;
            var aRarestColorBrush = Brushes.Transparent;
            var aFps = FpsDefault;
            var aRenderMovieCancelIsPending = false;
            var aRenderMovieIsPending = false;
            var aSpeedFkt = new CVec2(0, 0);
            var aSpeedPxl = FaktorToPixel(aSpeedFkt, aSizePxl);

            this.BitmapSourceM = aBitmapSource;
            this.SizePxl = aSizePxl;
            this.SizeMnd = aSizeMnd;
            this.MainWindow = aMainWindow;
            this.CenterTargetMnd = aCenterTargetMnd;
            this.DominantColorBrushM = aDominantColorBrush;
            this.RarestColorBrushM = aRarestColorBrush;
            this.Fps = aFps;
            this.RenderMovieCancelIsPending = aRenderMovieCancelIsPending;
            this.RenderMovieIsPending = aRenderMovieIsPending;
            this.SpeedFkt = aSpeedFkt;
            this.SpeedPxl = aSpeedPxl;
            this.DirectoryInfo = aDirectoryInfo;
            this.RenderFrameIsPending = false;

            this.StateEnum = CStateEnum.MainWindowLoaded;
        }
        private readonly DirectoryInfo DirectoryInfo;
        public DirectoryInfo VmDirectoryInfo => this.DirectoryInfo;
        private CMandelbrotState(CMandelbrotState aPreviousState)
        {
            this.PreviousStateNullable = aPreviousState;
            this.MainWindow = aPreviousState.MainWindow;
            this.SizePxl = aPreviousState.SizePxl;
            this.SizeMnd = aPreviousState.SizeMnd;
            this.CenterTargetMnd = aPreviousState.CenterTargetMnd;
            this.Fps = aPreviousState.Fps;
            this.SpeedPxl = aPreviousState.SpeedPxl;
            this.SpeedFkt = aPreviousState.SpeedFkt;
            this.BitmapSourceM = aPreviousState.BitmapSource;
            this.RenderMovieIsPending = aPreviousState.RenderMovieIsPending;
            this.RarestColorBrushM = aPreviousState.RarestColorBrush;
            this.DominantColorBrushM = aPreviousState.DominantColorBrush;
            this.RenderFrameIsPending = aPreviousState.RenderFrameIsPending;
            this.RenderMovieCancelIsPending = aPreviousState.RenderMovieCancelIsPending;
            this.DirectoryInfo = aPreviousState.DirectoryInfo;
            this.FrameCountProposal = aPreviousState.FrameCountProposal;
        }

        internal enum CEndRenderFrameEnum
        {
            Default
        }
        internal CMandelbrotState(CMandelbrotState aPreviousState, CRenderFrameOutput aRenderFrameOutput, CEndRenderFrameEnum aCmd)
        :
            this(aPreviousState)
        {
            var aSizePxl = aPreviousState.SizePxl;
            var aSizeMnd = aRenderFrameOutput.Item2;
            //var aCenterSourcePxl = CMandelbrotState.MandelToPixel(aCenterSourceMnd, aSizePxl, aSizeMnd);
            //var aCenterSourceFkt = CMandelbrotState.PixelToFaktor(aCenterSourcePxl, aSizePxl);            
            //var aCenterTargetPxl = CMandelbrotState.MandelToPixel(aCenterTargetMnd, aSizePxl, aSizeMnd);
            //var aCenterTargetFkt = CMandelbrotState.PixelToFaktor(aCenterTargetPxl, aSizePxl);

            var aCenterTargetMnd = aRenderFrameOutput.Item6;
            var aFps = aPreviousState.Fps.Value;
            var aSpeedPxl = aSizePxl.Divide(aFps);
            var aSpeedFkt = aSpeedPxl.Divide(aSizePxl);
            //var aMoveVectorFkt = aCenterSourceFkt.Add(aCenterTargetFkt.Subtract(aCenterSourceFkt).Mul(aSpeedFkt));
            //var aCenterCurrentFkt = aCenterSourceFkt.Add(aMoveVectorFkt);
            //var aCenterCurrentMnd = CMandelbrotState.FaktorToMandel(aCenterCurrentFkt, aSizePxl, aSizeMnd);
            //var aRenderInputTargetMnd = CMandelbrotState.FaktorToMandel(aCenterCurrentFkt, aSizePxl, aSizeMnd);
            var aBitmapSource = aRenderFrameOutput.Item1;
            var aRarestColor = CColorDetector.GetColor(aRenderFrameOutput.Item4);
            var aDominantColor = CColorDetector.GetColor(aRenderFrameOutput.Item5);
            var aRarestColorBrush = new SolidColorBrush(aRarestColor);
            var aDominantColorBrush = new SolidColorBrush(aDominantColor);
            var aRenderFrameIsPending = false;

            this.SizeMnd = aSizeMnd;
            this.CenterTargetMnd = aCenterTargetMnd;
            this.Fps = aFps;
            this.SpeedPxl = aSpeedPxl;
            this.SpeedFkt = aSpeedFkt;
            this.BitmapSourceM = aBitmapSource;
            this.RarestColorBrushM = aRarestColorBrush;
            this.DominantColorBrushM = aDominantColorBrush;
            this.RenderFrameIsPending = aRenderFrameIsPending;
            this.StateEnum = CStateEnum.EndRenderFrame;
        }
        internal enum CSetCenterEnum
        {
            Default
        }
        internal CMandelbrotState(CMandelbrotState aPreviousState, CVec2 aNewCenterMnd, CSetCenterEnum aCmd) : this(aPreviousState)
        {
            aPreviousState.CheckAnyRenderNotPending();
            var aSizeMnd = aPreviousState.SizeMnd.Zoom(1d, aNewCenterMnd);
            var aCenterTargetMnd = aNewCenterMnd;
            this.SizeMnd = aSizeMnd;
            this.CenterTargetMnd = aCenterTargetMnd;
            this.StateEnum = CStateEnum.SetCenter;
        }

        internal enum CMoveEnum { Default }
        internal CMandelbrotState(CMandelbrotState aPreviousState, CVec2 aNewCenterMnd, CMoveEnum aMoveEnum)
        {
            throw new NotImplementedException();
        }


        internal enum CZoomEnum { Default }
        internal CMandelbrotState(CMandelbrotState aPreviousState, double aZoomFaktor, CZoomEnum aZoomEnum) : this(aPreviousState)
        {
            var aSizeMnd = aPreviousState.SizeMnd.Zoom(aZoomFaktor);
            this.SizeMnd = aSizeMnd;
            this.StateEnum = CStateEnum.Zoom;


        }

        private enum CResetEnum
        {
            Default
        }
        private CMandelbrotState(CMandelbrotState aPreviousState, CResetEnum aEnum)
        {
            this.CheckAnyRenderNotPending();
            var aMainWindow = aPreviousState.MainWindow;
            var aSizeMnd = SizeMndDefault;
            var aCenterCurrentFkt = CenterCurrentFktDefault;
            var aSizePxl = aPreviousState.SizePxl;
            var aCenterCurrentMnd = CMandelbrotState.FaktorToMandel(aCenterCurrentFkt, aSizePxl, aSizeMnd);
            this.SizeMnd = aSizeMnd;
            this.StateEnum = CStateEnum.Reset;
        }
        #endregion        
        #region Converter
        private static CVec2 PixelToMandel(CVec2 v, CVec2 aSizePxl, CVec4 aSizeMnd)
            => v.Divide(aSizePxl).Mul(aSizeMnd.GetRectSize()).Add(aSizeMnd.GetRectPos());
        internal CVec2 PixelToMandel(CVec2 v)
            => PixelToMandel(v, this.SizePxl, this.SizeMnd);
        private static CVec2 FaktorToPixel(CVec2 v, CVec2 aSizePxl)
            => aSizePxl.Mul(v);
        private static CVec2 MandelToPixel(CVec2 v, CVec2 aSizePxl, CVec4 aSizeMnd)
            => v.Subtract(aSizeMnd.GetRectPos()).Divide(aSizeMnd.GetRectSize()).Mul(aSizePxl);
        internal CVec2 MandelToPixel(CVec2 v)
            => MandelToPixel(v, this.SizePxl, this.SizeMnd);
        internal static CVec2 PixelToFaktor(CVec2 v, CVec2 aSizePxl)
            => v.Divide(aSizePxl);
        internal CVec2 PixelToFaktor(CVec2 v)
            => PixelToFaktor(v, this.SizePxl);
        internal CVec2 FaktorToPixel(CVec2 v)
            => this.SizePxl.Mul(v);
        internal static CVec2 FaktorToMandel(CVec2 v, CVec2 aSizePxl, CVec4 aSizeMnd)
            => PixelToMandel(FaktorToPixel(v, aSizePxl), aSizePxl, aSizeMnd);
        internal CVec2 FaktorToMandel(CVec2 v)
            => FaktorToMandel(v, this.SizePxl, this.SizeMnd);  // this.PixelToMandel(this.FktToPixel(v));
        internal CVec2 MandelToFaktor(CVec2 v)
            => this.PixelToFaktor(this.MandelToPixel(v));
        #endregion
        #region Checks
        private void CheckRenderMoviePending()
        {
            if (!this.RenderMovieIsPending.Value)
            {
                throw new InvalidOperationException();
            }
        }
        private void CheckRenderFrameNotPending()
        {
            if (this.RenderFrameIsPending.Value)
            {
                throw new InvalidOperationException();
            }
        }
        private void CheckRenderMovieNotPending()
        {
            if (this.RenderMovieIsPending.Value)
            {
                throw new InvalidOperationException();
            }
        }
        private void CheckAnyRenderNotPending()
        {
            this.CheckRenderFrameNotPending();
            this.CheckRenderMovieNotPending();
        }
        private void CheckRenderFramePending()
        {
            if (!this.RenderFrameIsPending.Value)
            {
                throw new InvalidOperationException();
            }
        }
        #endregion
        #region ColorFuncs
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
            var o = (-max / 2d) * ho;
            var r = ChannelHue(c + max * 0d + o);
            var g = ChannelHue(c + max * 1d + o);
            var b = ChannelHue(c + max * 2d + o);
            return Color.FromScRgb(1f, r, g, b);
        }
        #endregion
        #region RarestColor
        private readonly Brush RarestColorBrushM;
        public Brush RarestColorBrush => this.RarestColorBrushM;
        #endregion
        #region DominantColor
        private Brush DominantColorBrushM;
        public Brush DominantColorBrush => this.DominantColorBrushM;
        #endregion


        #region StateChange-Method
        internal void Move(CVec2 aNewCenterMnd)
        {
            new CMoveState(this, aNewCenterMnd, CSetCenterEnum.Default).Enter();
        }
        internal void Zoom(double aFaktor)
        {
            new CZoomState(this, aFaktor, CZoomEnum.Default).Enter();
        }
        internal void Reset()
        {
            throw new NotImplementedException();
        }
        #endregion
        #region EnterState
        protected void Enter()
        {
            if (!(this.MainWindow.State is object)
            || object.ReferenceEquals(this.MainWindow.State, this.PreviousStateNullable))
            {
                this.MainWindow.States.Add(this);
                this.MainWindow.State = this;
            }
            else
            {
                this.OnEnterCanceled();
            }
        }
        internal virtual void OnEnterCanceled()
        {
        }

        internal virtual void OnEntered()
        {
        }

        internal virtual void OnLeft()
        {
            this.BitmapSourceM = default;
        }
        #endregion
        #region StateEnum
        private enum CStateEnum
        {
            MainWindowLoaded,
            BeginRenderFrame,
            EndRenderFrame,
            SetCenter,
            Zoom,
            BeginRenderMovie,
            Reset,
            NextFrame,
            BeginCancelRenderMovie,
            EndCancelRenderMovie,
            EndRenderMovie,
        }

        private readonly CStateEnum? StateEnum;
        public override string ToString() => this.StateEnum.ToString();
        #endregion
        #region Dispatcher
        private Dispatcher Dispatcher => this.MainWindow.Dispatcher;
        #endregion
        #region RenderFrame
        private CRenderFrameInput NewFrameRenderInput(CMandelbrotState aCurrentState)
             => new CRenderFrameInput
             (
                 this.SizePxl,
                 this.SizeMnd,
                 new CVec2(0, 0),
                 0,
                 this.MainWindow.ZoomSliderValue,
                 new CVec2(0, 0),
                 //aCurrentState,
                 this.MainWindow.ProgressionManager.Parameters.NewParameterSnapshot()
             );
        internal void BeginRenderFrame()
            => this.BeginRenderFrame(this.NewFrameRenderInput(this));

        internal enum CBeginRenderFrameEnum { Default }
        internal CMandelbrotState(CMandelbrotState aPreviousState, CBeginRenderFrameEnum aCmd) : this(aPreviousState)
        {
            this.CheckRenderFrameNotPending();
            this.RenderFrameIsPending = true;
            this.StateEnum = CStateEnum.BeginRenderFrame;
        }


        internal void BeginRenderFrame(CRenderFrameInput aRenderInput)
        {
            new CBeginRenderFrameState(this, aRenderInput, CBeginRenderFrameEnum.Default).Enter();
        }
        internal void BeginRenderFrame(CMandelbrotState aCurrentState)
        {
            var aRenderInput = this.NewFrameRenderInput(aCurrentState);
            this.BeginRenderFrame(aRenderInput);
        }



        internal void BeginRenderFrameCore(CRenderFrameInput aRenderFrameInput)
        {
            var aStopwatch = new Stopwatch();
            aStopwatch.Start();
            var aSizePxlDbl = aRenderFrameInput.Item1;
            var aDx = (int)aSizePxlDbl.Item1;
            var aDy = (int)aSizePxlDbl.Item2;
            var aSizePxlInt = new CVec2Int(aDx, aDy);
            var aSizeMnd = aRenderFrameInput.Item2;
            var aCenterMnd = aSizeMnd.GetRectCenter();
            //var aThreadCount = 1;
            var aThreadCount = 90;
            var aThreadLinesRest = aDy % aThreadCount;
            var aLinesPerThread = (aDy - aThreadLinesRest) / aThreadCount;
            var aThreadInputs = from aThreadId in Enumerable.Range(0, aThreadCount)
                                select new CRenderFrameSegmentInput(aThreadId * aLinesPerThread, (aThreadId == aThreadCount - 1 && aThreadCount != 1 && aThreadLinesRest != 0) ? aThreadLinesRest : aLinesPerThread);
            var aPixels = new Color[aDx * aDy];
            var aColorFkts = new double[aDx * aDy];
            var aParametersSnapshot = aRenderFrameInput.Item7;
            var aBitmapSourceSink = new CBufferSink<BitmapSource>();
            var aColorArrayToBitmapSourceSink = new CColorArrayToBitmapSourceSink(aBitmapSourceSink, aSizePxlInt);
            var aColorArrayBufferSink = new CBufferSink<Color[]>(aColorArrayToBitmapSourceSink);
            var aNullSink = new CNullSink<double[]>(); // TODO: Hier ModelWriter reinbauen.
            var aColorFktArrayBufferSink = new CBufferSink<double[]>(aNullSink);
            var aFlushSinks = new List<CSink>(aThreadCount);
            var aRenderPartFunc = new Action<CRenderFrameSegmentInput>(delegate (CRenderFrameSegmentInput aRenderFrameSegmentsInput)
            {
                var aYStart = aRenderFrameSegmentsInput.Item1;
                var aYCount = aRenderFrameSegmentsInput.Item2;
                var aPixelCoords = from aY in Enumerable.Range(aYStart, aYCount)
                                   from aX in Enumerable.Range(0, (int)aDx)
                                   select new CVec2Int(aX, aY);
                var aColorAlgorithmInput = new CColorAlgorithmInput(aParametersSnapshot);
                var aColorAlgorithmEnum = aParametersSnapshot.Get<CColorAlgorithmEnum>(CParameterEnum.ColorAlgorithm);
                var aColorAlgorithm = CTypeAttribute.GetByEnum(aColorAlgorithmEnum).DataType.New<CColorAlgorithm>(aColorAlgorithmInput);
                var aPixelAlgoEnum = aParametersSnapshot.Get<CPixelAlgorithmEnum>(CParameterEnum.PixelAlgorithm1);
                var aGetColor = default(Func<double, Color>); // new Func<double, Color>(d => aColorAlgorithm.GetColor(d));
                var aPixelAlgorithmInput = new CPixelAlgorithmInput(aSizePxlDbl, aSizeMnd, aParametersSnapshot, aGetColor);
                var aPixelAlgorithm = CTypeAttribute.GetByEnum(aPixelAlgoEnum).DataType.New<CMandelbrotPixelAlgorithm>(aPixelAlgorithmInput);
                var aArrayFragmentOffset = aYStart * aDx;
                var aArrayFragmentSize = aYCount * aSizePxlInt.Item1;
                var aColorFktBufferSink = new CArrayFragmentBufferedSink<double>(aColorFkts, aArrayFragmentOffset, aArrayFragmentSize, aColorFktArrayBufferSink);
                var aColorBufferSink = new CArrayFragmentBufferedSink<Color>(aPixels, aArrayFragmentOffset, aArrayFragmentSize, aColorArrayBufferSink);
                lock (aFlushSinks)
                {
                    aFlushSinks.Add(aColorFktBufferSink);
                    aFlushSinks.Add(aColorBufferSink);
                }
                var aDoubleToColorAlgoSink = new CColorFktThroughColorAlgorithmSink(aColorAlgorithm, aColorBufferSink);
                var aForks = new List<CSink<double>>();
                aForks.Add(aDoubleToColorAlgoSink);
                aForks.Add(aColorFktBufferSink);
                var aColorFktSink = new CForkSink<double>(aForks.ToArray());

                foreach (var aPixelCoord in aPixelCoords)
                {
                    var aPixelIdx = aPixelCoord.Item2 * aDx + aPixelCoord.Item1;
                    var aColorFkt = aPixelAlgorithm.GetPixelFkt(aPixelCoord.Item1, aPixelCoord.Item2);
                    aColorFktSink.Write(aColorFkt);
                }
            });

            var aTasks = (from aThreadInput in aThreadInputs select Task.Factory.StartNew(delegate () { aRenderPartFunc(aThreadInput); })).ToArray();

            foreach (var aTask in aTasks)
            {
                aTask.Wait();
            }

            aFlushSinks.Add(aColorFktArrayBufferSink);


            foreach (var aFlushSink in aFlushSinks)
            {
                aFlushSink.Flush();
            }

            var aNewCenter = aCenterMnd;
            var aNewBitmapSource = new Func<BitmapSource>(() => {
                aColorArrayBufferSink.Flush();
                aColorArrayToBitmapSourceSink.Flush();
                return aBitmapSourceSink.Buffer;
            });
            var aNewRenderFrameOutput = new Func<CRenderFrameOutput>(() => new CRenderFrameOutput
            (
                aNewBitmapSource(),
                aSizeMnd,
                new CVec2(0, 0),
                default, // aRarestColor,
                default, //aDominatingColor,
                aNewCenter
            ));
            aStopwatch.Stop();
            System.Diagnostics.Debug.Print(nameof(CMandelbrotState) + "." + nameof(BeginRenderFrameCore) + " took " + aStopwatch.Elapsed.TotalSeconds + " second(s)." + Environment.NewLine);

            this.Dispatcher.BeginInvoke(new Action(delegate ()
            {
                try
                {
                    this.EndRenderFrame(aNewRenderFrameOutput);
                }
                catch (Exception aExc)
                {
                    aExc.CatchUnexpected(this);
                }
            }));

        }
        private void EndRenderFrame(Func<CRenderFrameOutput> aNewRenderFrameOutput)
        {
            this.CheckRenderFramePending();
            var aRenderFrameOutput = aNewRenderFrameOutput();
            var aState = new CEndRenderFrameState(this, aRenderFrameOutput, CEndRenderFrameEnum.Default);
            aState.Enter();
        }
        #endregion     
        #region Center
        internal CVec2 CenterMnd => this.SizeMnd.GetRectCenter();
        internal CVec2 CenterPxl => this.MandelToPixel(this.CenterMnd);
        #endregion
        #region FrameCountProposal
        private int FrameCountProposalM;
        private int FrameCountProposal
        {
            get => this.FrameCountProposalM;
            set
            {
                this.FrameCountProposalM = value;
                this.OnPropertyChanged(nameof(this.VmFrameCountProposal));
            }
        }
        public int VmFrameCountProposal => this.FrameCountProposal;
        internal void UpdateFrameCountProposal()
        {
            var aFrameCount = 1;
            var aTmp = SizeMndDefault.Item3;
            var aZoom = this.MainWindow.ZoomInFaktor;
            while (aTmp > this.SizeMnd.Item3)
            {
                aTmp = aTmp * aZoom;
                ++aFrameCount;
            }
            this.FrameCountProposal = aFrameCount;
        }
        #endregion
        public Visibility RenderMovieGridVisibility => this.RenderMovieIsPending.Value ? Visibility.Collapsed : Visibility.Visible;
        public Visibility RenderMovieProgressGridVisibility => this.RenderMovieIsPending.Value ? Visibility.Visible : Visibility.Collapsed;


        private readonly bool? RenderMovieCancelIsPending;

        private BitmapSource BitmapSourceM;
        public BitmapSource BitmapSource => this.BitmapSourceM;

        #region FrameCount
        internal int FrameCount => (int)this.MainWindow.ProgressionManager.Parameters[CParameterEnum.FrameCount].GetDoubleValue();
        public int VmFrameCount => this.FrameCount;
        #endregion
        #region FrameNr
        internal int FrameNr => (int)this.MainWindow.ProgressionManager.Parameters[CParameterEnum.FrameIndex].GetDoubleValue();
        public int VmFrameNr => this.FrameNr;
        #endregion
        internal bool? RenderFrameIsPending;
        //internal readonly double? ZoomFactor;
        public Visibility ProgressbarVisibility => this.AnythingIsPending ? Visibility.Visible : Visibility.Hidden;
        internal bool AnythingIsPending => this.RenderFrameIsPending.Value || this.RenderMovieIsPending.Value;
        internal readonly MainWindow MainWindow;

        #region CurrentState
        internal readonly CMandelbrotState PreviousStateNullable;
        internal CMandelbrotState CurrentState => this.MainWindow.State;
        #endregion

        private readonly CVec2 SizePxl;

        internal readonly CVec4 SizeMnd;

        internal readonly bool? RenderMovieIsPending;

        /// <summary>
        /// Zielposition als Mandelkoordinate.
        /// </summary>
        internal readonly CVec2 CenterTargetMnd;

        /// <summary>
        /// Frames pro sekunde
        /// </summary>
        internal readonly double? Fps;

        /// <summary>
        /// Tempo der Bewegung (in pixel)
        /// </summary>
        internal readonly CVec2 SpeedPxl;

        /// <summary>
        /// Tempo der bewegung als Faktor (0..1) der Pixelkoordinaten.
        /// </summary>
        internal readonly CVec2 SpeedFkt;

        #region BeginRenderMovie
        internal enum CBeginRenderMovieEnum
        {
            Default
        }
        internal CMandelbrotState(CMandelbrotState aPreviousState, CBeginRenderMovieEnum aEnum) : this(aPreviousState)
        {
            this.MainWindow.ProgressionManager.BeginRender();
            this.RenderMovieIsPending = true;
            this.StateEnum = CStateEnum.BeginRenderMovie;
        }
        internal void BeginRenderMovie()
        {
            this.CheckAnyRenderNotPending();
            new CBeginRenderMovieState(this, CBeginRenderMovieEnum.Default).Enter();
        }
        #endregion
        internal void SaveFrame()
        {
            this.CheckRenderMoviePending();
            this.CheckRenderFrameNotPending();
            var aDir = this.DirectoryInfo;
            var aFrameNr = this.FrameNr;
            var aFrameNrLen = this.FrameCount.ToString().Length;
            var aFrameNrText = aFrameNr.ToString().PadLeft(aFrameNrLen, '0');
            var aFileName = aFrameNrText + ".bmp";
            var aFileInfo = new FileInfo(System.IO.Path.Combine(aDir.FullName, aFileName));
            this.SaveBitmap(aFileInfo);
        }
        private void SaveBitmap(FileInfo aFileInfo)
        {
            aFileInfo.Directory.Create();
            var aBitmapSource = (BitmapSource)this.BitmapSource;
            var aEncoder = new BmpBitmapEncoder();
            aEncoder.Frames.Add(BitmapFrame.Create(aBitmapSource));
            using var aFileStream = new FileStream(aFileInfo.FullName, FileMode.Create);
            aEncoder.Save(aFileStream);
        }
        internal void OnRenderMovieEndRenderFrame()
        {
            this.CheckRenderMoviePending();
            this.BeginSaveFrame();
        }

        private void BeginSaveFrame()
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
            if (aSaveFrameExc is object)
            {
                this.BeginCancelRenderMovie(aSaveFrameExc);
            }
            else if (this.RenderMovieCancelIsPending.Value)
            {
                this.EndCancelRenderMovie();
            }
            else if (this.FrameNr < this.FrameCount - 1)
            {
                this.NextFrame();
            }
            else
            {
                this.EndRenderMovie();
            }

        }

        #region NextFrame
        internal enum CNextFrameEnum
        {
            Default
        }
        internal CMandelbrotState(CMandelbrotState aPreviousState, CNextFrameEnum aCmd) : this(aPreviousState)
        {
           // this.MainWindow.ProgressionManager.Parameters[CParameterEnum.FrameIndex].Constant.Increment();
            var aSecond = this.MainWindow.ProgressionManager.Parameters[CParameterEnum.SecondIndex].Constant.As<CDoubleConstant>();
            var aFps = this.MainWindow.ProgressionManager.Parameters[CParameterEnum.FramesPerSecond].Constant.As<CDoubleConstant>().Value;
            var aOldSecond = aSecond.Value;
            var aNewSecond = aOldSecond + 1d / aFps;
            aSecond.Value = aNewSecond;

            var aFrameIndex = this.MainWindow.ProgressionManager.Parameters[CParameterEnum.FrameIndex].Constant.As<CDoubleConstant>();
            var aOldFrameIndex = aFrameIndex.Value;
            var aNewFrameIndex = Math.Floor(aOldFrameIndex + 1);
            aFrameIndex.Value = aNewFrameIndex;


            this.StateEnum = CStateEnum.NextFrame;
        }

        internal void NextFrame()
        {
            new CNextFrameState(this, CNextFrameEnum.Default).Enter();
        }

        #endregion
        internal enum CEndRenderMovieEnum
        {
            Default
        }
        internal CMandelbrotState(CMandelbrotState aPreviousState, CEndRenderMovieEnum aCmd) : this(aPreviousState)
        {
            this.RenderMovieIsPending = false;
            this.MainWindow.ProgressionManager.EndRender();

            this.StateEnum = CStateEnum.EndRenderMovie;
        }
        private void EndRenderMovie()
        {
            new CEndRenderMovieState(this, CEndRenderMovieEnum.Default).Enter();
        }
        #region BeginCancelRenderMovie
        internal enum CBeginCancelRenderMovieEnum { Default }
        internal CMandelbrotState(CMandelbrotState aPreviousState, CBeginCancelRenderMovieEnum aCmd) : this(aPreviousState)
        {
            this.CheckRenderMoviePending();
            this.RenderMovieCancelIsPending = true;
            this.StateEnum = CStateEnum.BeginCancelRenderMovie;
        }
        private void BeginCancelRenderMovie(Exception aExc)
        {
            new CBeginCancelRenderMovieState(this, aExc, CBeginCancelRenderMovieEnum.Default).Enter();
        }
        internal void BeginCancelRenderMovie()
        {
            this.BeginCancelRenderMovie(new OperationCanceledException());
        }

        #endregion
        #region EndCancelRenderMovie
        internal enum CEndCancelRenderMovieEnum { Default };
        internal CMandelbrotState(CMandelbrotState aPreviousState, CEndCancelRenderMovieEnum aCmd) : this(aPreviousState)
        {
            this.RenderMovieIsPending = false;
            this.RenderFrameIsPending = false;
            this.RenderMovieCancelIsPending = false;
            this.MainWindow.ProgressionManager.EndRender();

            this.StateEnum = CStateEnum.EndCancelRenderMovie;
        }
        internal void EndCancelRenderMovie()
        {
            new CEndCancelRenderMovieState(this, CEndCancelRenderMovieEnum.Default).Enter();
        }
        #endregion

        public bool ZoomSliderIsEnabled => !this.AnythingIsPending;
        public bool ResetButtonIsEnabled => !this.AnythingIsPending;
        public bool RenderMovieStartButtonIsEnabled => !this.AnythingIsPending;
        public bool RenderMovieCancelButtonIsEnabled => this.RenderMovieIsPending.Value && !this.RenderMovieCancelIsPending.Value;
        internal CVec2 CenterTargetMarkerPxl => this.MandelToPixel(this.CenterTargetMnd); // MandelToPixel(this.MandelFktToPixel(this.CenterAnimFktCoord), this.ImageSize, this.MandelRect);        
        public Point CenterTargetMarkerPos => this.MainWindow.ElipseCenterToElipsePos(this.CenterTargetMarkerPxl, this.MainWindow.TargetEllipse);


    }

}
