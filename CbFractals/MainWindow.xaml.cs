using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;

namespace CbFractals
{
    using CPixelAlgorithmInput = Tuple<Tuple<double, double>,                           // aSizePxl, 
                                             Tuple<double, double, double, double>,     // aSizeMnd, 
                                             CbFractals.CParameterSnapshot,             // aParameterSnapshot
                                             Func<double, Color>                        // GetColorFunc
                                             >;
    using CRenderFrameInput = Tuple<Tuple<double, double>,               // ImageSize
                               Tuple<double, double, double, double>,    // MandelSpace 
                               Tuple<double, double>,                    // MandelCenter
                               int,                                      // ZoomCount
                               double,                                   // TotalZoom
                               Tuple<double, double>,                    // MandelTargetCenter
                                                                         //CMandelbrotState,                         // CurrentState
                               CbFractals.CParameterSnapshot             // ParameterSnapshot
                                >;
    using CRenderFrameOutput = Tuple<BitmapSource,                       // BitmapSource, 
                                Tuple<double, double, double, double>,   // SizeMnd
                                Tuple<double, double>,                   // CenterMnd
                                CbFractals.CBaseColorEnum,               // RarestColor
                                CbFractals.CBaseColorEnum,               // DominatingColor
                                Tuple<double, double>                   // TargetCenterMnd
                                                                        //  CMandelbrotState                         // PreviousState
                                >;
    using CRenderFrameSegmentInput = Tuple<int,                            // YStart
                                           int                             // YCount                                           
                                           >;
    using CVec2 = Tuple<double, double>;
    using CVec2Int = Tuple<int, int>;
    using CVec4 = Tuple<double, double, double, double>;

    internal static class CDreisatz
    {
        internal static double GetScale(this double v, double aMin, double aMax)
              => (v - aMin) / (aMax - aMin);

        internal static double GetScale(this double v, CVec2 aRange)
            => v.GetScale(aRange.Item1, aRange.Item2);
        internal static double Scale(this double fkt, double aMin, double aMax)
            => (aMax - aMin) * fkt + aMin;
        internal static double Scale(this double fkt, CVec2 aRange)
            => fkt.Scale(aRange.Item1, aRange.Item2);
        internal static double Map(this double fkt, double aSourceMin, double aSourceMax, double aTargetMin, double aTargetMax)
            => fkt.GetScale(aSourceMin, aSourceMax).Scale(aTargetMin, aTargetMax);

        internal static double Map(this double fkt, CVec2 aSourceRange, CVec2 aTargetRange)
            => fkt.Map(aSourceRange.Item1, aSourceRange.Item2, aTargetRange.Item1, aTargetRange.Item2);

        internal static double Map(this double fkt, 
                                   double aCenter,
                                   CVec2 aLoSourceRange,
                                   CVec2 aLoTargetRange,
                                   CVec2 aHiSourceRange,
                                   CVec2 aHiTargetRange)
            => fkt < aCenter
            ? fkt.Map(aLoSourceRange, aLoTargetRange)
            : fkt.Map(aHiSourceRange, aHiTargetRange)
            ;

        internal static CVec2 GetScale(this CVec2 v, CVec2 aXRange, CVec2 aYRange)
            => new CVec2(v.Item1.GetScale(aXRange.Item1, aXRange.Item2),
                         v.Item2.GetScale(aYRange.Item1, aYRange.Item2));
        internal static CVec2 Scale(this CVec2 fkt, CVec2 aXRange, CVec2 aYRange)
            => new CVec2(fkt.Item1.Scale(aXRange),
                         fkt.Item2.Scale(aYRange));
        internal static CVec2 Map(this CVec2 v, CVec2 aXSourceRange, CVec2 aYSourceRange, CVec2 aXTargetRange, CVec2 aYTargetRange)
            => new CVec2(v.Item1.Map(aXSourceRange.Item1, aXSourceRange.Item2, aXTargetRange.Item1, aXTargetRange.Item2),
                         v.Item2.Map(aYSourceRange.Item1, aYSourceRange.Item2, aYTargetRange.Item1, aYTargetRange.Item2));
    }


    internal enum CBaseColorEnum
    {
        Red = 0,
        Green = 1,
        Blue = 2
    }
    internal static class CExtensions
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
        internal static CVec2 Round(this CVec2 v)
            => new CVec2(Math.Round(v.Item1), Math.Round(v.Item2));
    }

    internal static class CLazyLoad
    {
        internal static T Get<T>(ref T? v, Func<T> n) where T : struct
        {
            if (!v.HasValue)
                v = n();
            return v.Value;
        }
        internal static T Get<T>(ref T v, Func<T> n) where T : class
        {
            if (!(v is object))
            {
                v = n();
            }
            return v;
        }
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

    public abstract class CViewModel : INotifyPropertyChanged
    {
        #region PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string aPropertyName)
        {
            if (this.PropertyChanged is object)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(aPropertyName));
            }
        }
        #endregion
    }

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

    internal sealed class CParameterClassRegistry : Dictionary<Type, Type>
    {
        private CParameterClassRegistry()
        {
            this.Add(typeof(Int64), typeof(CInt64Parameter));
            this.Add(typeof(double), typeof(CDoubleParameter));
            this.Add(typeof(CPixelAlgorithmEnum), typeof(CPixelAlgorithmEnumParameter));
        }
        internal static readonly CParameterClassRegistry Singleton = new CParameterClassRegistry();
    }


    public abstract class CParameter : CValueNode
    {
        internal CParameter(CParameters aParameters, CParameterEnum aParameterEnum) : base(aParameters.ParentProgressionManager, aParameterEnum.GetNameEnum())
        {
            this.ParentParameters = aParameters;
            this.ParameterEnum = aParameterEnum;
            this.MappedProgression = new CMappedProgression(this, Name_MappedProgression);
        }
        internal readonly CParameters ParentParameters;
        internal readonly CParameterEnum ParameterEnum;
        #region Constant
        private CConstant ConstantM;
        internal CConstant Constant => CLazyLoad.Get(ref this.ConstantM, ()=> this.NewConstant(this, Name_Constant));
        public CConstant VmConstant => this.Constant;

        private CConstant MinM;
        internal CConstant Min => CLazyLoad.Get(ref this.MinM, () => this.NewConstant(this, Name_Min));
        public CConstant VmMin => this.Min;
        private CConstant MaxM;
        internal CConstant Max => CLazyLoad.Get(ref this.MaxM, () => this.NewConstant(this, Name_Max));
        public CConstant VmMax => this.Max;

        internal abstract CConstant NewConstant(CValueNode aParentValueNode, CNameEnum aName);
        #endregion        
        #region LinearProgression
        internal readonly CMappedProgression MappedProgression;
        #endregion
        #region Progressions
        internal IEnumerable<CProgression> Progressions
        {
            get
            {
                yield return this.Constant;
                if(this.MappedProgression.Selectable)
                    yield return this.MappedProgression;
            }
        }
        public IEnumerable<CProgression> VmProgressions => this.Progressions;
        internal override IEnumerable<CValueNode> ValueSources => this.Progressions;
        internal override IEnumerable<CValueNode> SubValueNodes
        {
            get
            {
                foreach (var aNode in base.SubValueNodes)
                    yield return aNode;
                foreach (var aProgression in this.Progressions)
                    yield return aProgression;
                yield return this.Min;
                yield return this.Max;
            }
        }
        #endregion
        #region Progression
        private CProgression ProgressionM;
        internal CProgression Progression
        {
            get => CLazyLoad.Get(ref this.ProgressionM, () => this.Constant);
            set
            {
                this.ProgressionM = value;
                this.ParentProgressionManager.OnChangeRenderFrameOnDemand();
                this.OnPropertyChanged(nameof(this.VmProgression));
            }
        }
        internal override CValueNode ValueSource { get => this.Progression; set { this.Progression = (CProgression)value; base.ValueSource = value; } }
        public CProgression VmProgression
        {
            get => this.Progression;
            set => this.Progression = value;
        }

        internal abstract object ConverTo(object aValue);
        internal override object GetTypelessValue() => this.ConverTo(this.Progression.GetTypelessValue());
        #endregion
        #region Build
        internal override void Build()
        {
            base.Build();

            this.Progression = this.Constant;
            this.Min.SetDefaultMin();
            this.Max.SetDefaultMax();

            foreach (var aProgression in this.Progressions)
            {
                aProgression.Build();
            }
        }
        internal T As<T>() where T : CParameter => (T)this;

        internal void SetConst<T>(T v)
        {
            this.Constant.As<CConstant<T>>().Value = v;
        }
        internal void SetConst<T>(T aValue, T aMin, T aMax)
        {
            this.SetConst(aValue);
            this.Min.As<CConstant<T>>().Value = aMin;
            this.Max.As<CConstant<T>>().Value = aMax;
        }

        internal void SetMappedProgression(double aMappedMin, double aMappedMax)
        {
            var aMappedProgression = this.MappedProgression;
            aMappedProgression.MappedMin.Value = aMappedMin;
            aMappedProgression.MappedMax.Value = aMappedMax;
        }

        internal void SetProgression(double aFrom, double aTo)
        {
            this.As<CParameter<double>>().Min.Value = aFrom;
            this.As<CParameter<double>>().Max.Value = aTo;
        }
        #endregion


    }

    public abstract class CParameter<T> : CParameter
    {
        internal CParameter(CParameters aParentParameters, CParameterEnum aParameterEnum)
        : base(aParentParameters, aParameterEnum)
        {
        }
        internal new CConstant<T> Min => (CConstant<T>)base.Min;
        internal new CConstant<T> Max => (CConstant<T>)base.Max;
    }

    public abstract class CEnumParameter<T> : CParameter
    {
        internal CEnumParameter(CParameters aParentParameters, CParameterEnum aParameterEnum)
        : base(aParentParameters, aParameterEnum)
        {
        }
        internal override void Build()
        {
            base.Build();
            this.Min.Supported = false;
            this.Max.Supported = false;
        }
        internal override object ConverTo(object aValue) => aValue is T ? (T)aValue : Enum.Parse(typeof(T), aValue.ToString());
    }

    public sealed class CInt64Parameter : CParameter<Int64>
    {
        public CInt64Parameter(CParameters aParentParameters, CParameterEnum aParameterEnum) : base(aParentParameters, aParameterEnum)
        {
        }
        internal override CConstant NewConstant(CValueNode aParentValueNode, CNameEnum aName) => new CInt64Constant(aParentValueNode, aName);
        internal override object ConverTo(object aValue) => Convert.ToInt64(aValue);
    }
    public sealed class CDoubleParameter : CParameter<double>
    {
        public CDoubleParameter(CParameters aParentParameters, CParameterEnum aParameterEnum) : base(aParentParameters, aParameterEnum)
        {
            
        }
        internal override CConstant NewConstant(CValueNode aParentValueNode, CNameEnum aName)
            => new CDoubleConstant(aParentValueNode, aName);
        internal override object GetTypelessValue() => this.GetMappedValue();
        internal double GetMappedValue()
            => ((double)base.GetTypelessValue()).Map(0d, 1d, this.Min.Value, this.Max.Value);
        internal override object ConverTo(object aValue) => Convert.ToDouble(aValue);
        internal override void Build()
        {
            base.Build();

            this.MappedProgression.MappedMin.Value = 0d;
            this.MappedProgression.MappedMax.Value = 1d;
        }
    }

    public sealed class CDataTypeAttribute : Attribute
    {
        public CDataTypeAttribute(Type aConstantType)
        {
            this.DataType = aConstantType;
        }
        internal readonly Type DataType;

        internal static CDataTypeAttribute GetByEnum<TEnum>(TEnum e)
            => typeof(TEnum).GetField(e.ToString()).GetCustomAttributes(typeof(CDataTypeAttribute), false).Cast<CDataTypeAttribute>().Single();
        internal CParameter NewParameter(params object[] aArgs)
            => CParameterClassRegistry.Singleton[this.DataType].New<CParameter>(aArgs);

    }

    internal static class CExtensions2
    {
        internal static CNameAttribute GetNameAttribute(this Type aType, bool aInherit = false)
            => aType.GetCustomAttributes(typeof(CNameAttribute), aInherit).Cast<CNameAttribute>().Single();
        internal static CGuidAttribute GetGuidAttribute(this Enum aEnum)
            => aEnum.GetType().GetField(aEnum.ToString()).GetCustomAttributes(typeof(CGuidAttribute), false).Cast<CGuidAttribute>().Single();

        internal static CNameAttribute GetNameAttribute(this Enum aEnum)
            => aEnum.GetType().GetField(aEnum.ToString()).GetCustomAttributes(typeof(CNameAttribute), false).Cast<CNameAttribute>().Single();
        internal static CNameEnum GetNameEnum(this Enum aEnum)
            => aEnum.GetNameAttribute().NameEnum;

    }


    public sealed class CNameAttribute: Attribute
    {
        public CNameAttribute(CNameEnum aName)
        {
            this.NameEnum = aName;
        }
        internal readonly CNameEnum NameEnum;
    }

    public enum CParameterEnum
    {
        [CDataType(typeof(double))]
        [CName(CNameEnum.Parameter_CenterX)]
        CenterX,

        [CName(CNameEnum.Parameter_CenterY)]
        [CDataType(typeof(double))]
        CenterY,

        [CName(CNameEnum.Parameter_DoubleZero)]
        [CDataType(typeof(double))]
        DoubleZero,

        [CName(CNameEnum.Parameter_FrameCount)]
        [CDataType(typeof(Int64))]
        FrameCount,

        [CName(CNameEnum.Parameter_FramePos)]
        [CDataType(typeof(Int64))]
        FrameIndex,

        [CName(CNameEnum.Parameter_JuliaRealPart)]
        [CDataType(typeof(double))]
        JuliaRealPart,

        [CName(CNameEnum.Parameter_JuliaImaginaryPart)]
        [CDataType(typeof(double))]
        JuliaImaginaryPart,

        [CName(CNameEnum.Parameter_JuliaMoveX)]
        [CDataType(typeof(double))]
        JuliaMoveX,

        [CName(CNameEnum.Parameter_JuliaMoveY)]
        [CDataType(typeof(double))]
        JuliaMoveY,

        [CName(CNameEnum.Parameter_Zoom)]
        [CDataType(typeof(double))]
        Zoom,

        [CName(CNameEnum.Parameter_Iterations)]
        [CDataType(typeof(Int64))]
        Iterations,

        [CName(CNameEnum.Parameter_DarkenThresholdLo)]
        [CDataType(typeof(double))]
        DarkenThresholdLo,

        [CName(CNameEnum.Parameter_DarkenThresholdHi)]
        [CDataType(typeof(double))]
        DarkenThresholdHi,

        [CName(CNameEnum.Parameter_ColorPeriod)]
        [CDataType(typeof(double))]
        ColorPeriod,

        [CName(CNameEnum.Parameter_ColorOffset)]
        [CDataType(typeof(double))]
        ColorOffset,
        // Rotation

        [CName(CNameEnum.Parameter_PixelAlgorithm1)]
        [CDataType(typeof(CPixelAlgorithmEnum))]
        PixelAlgorithm1,

        _Count
    }
    // Oscillator : Mit amplitudenmodulation und frequenzmodulation "2 schritte vor 1 zurück" realisieren.
    //  RingelOsci
    //  RingelOsciFm
    //  RingelOsciAm1
    //  RingelOsciAm2
    //  RingelOsciInverter
    //  Pulse
    //  RandomPulse

    // Palleten

    internal sealed class CParameterSnapshot
    {
        internal CParameterSnapshot(CParameters aParameters)
        {
            this.Values = aParameters.Parameters.Select(aParam => aParam.GetTypelessValue()).ToArray();
        }
        private object[] Values;
        private object this[CParameterEnum aParameterEnum]
        {
            get => this.Values[(int)aParameterEnum];
        }
        internal T Get<T>(CParameterEnum e)
            => (T)this[e];
    }

    
    [XmlType("ValueBase")]
    public abstract class CXmlValueBase
    {
        [XmlAttribute("Path")]
        public string Path;

        [XmlAttribute("Value")]
        public string Value;

        [XmlAttribute("Comment")]
        public string Comment;
    }

    [XmlType("Value")]
    public sealed class CXmlValue :CXmlValueBase
    {
    }

    [XmlType("SelectedValueSource")]
    public sealed class CXmlSelectedValueSource : CXmlValueBase
    {
    }

    [XmlRoot("Progression", IsNullable = false)]
    public sealed class CXmlProgressionSnapshot
    {
        [XmlArray("Values")]
        public CXmlValueBase[] Values;

        /*
         <ValueNodes>
             <Data Path="a|b|c" Value="0.0" />
             <SelectedValueSource Path="parameter1" Value="Guid"/>            
         </ValueNodes>
          */

        private const char GuidSeperator = '/';
        private const char CommentSeperator = '.';
        private static string GetPersistentPath(IEnumerable<CNameEnum> aNames)
            => string.Join(GuidSeperator, from aName in aNames select aName.GetGuidAttribute().Guid.ToString());
        private static string GetComment(IEnumerable<CNameEnum> aNames)
            => string.Join(CommentSeperator, from aName in aNames select aName.ToString());

        private static XmlSerializer NewXmlSerializer()
        {
            var aExtraTypes = new Type[] { typeof(CXmlValueBase), typeof(CXmlValue), typeof(CXmlSelectedValueSource) };
            var aSerializer = new XmlSerializer(typeof(CXmlProgressionSnapshot), aExtraTypes);
            return aSerializer;
        }

        internal static void Save(CParameters aParameters, FileInfo aFileInfo)
        {
            var aPath = new List<CNameEnum>();
            var aXmlValues = new List<CXmlValueBase>();
            foreach(var aValueNode in aParameters.StoredNodes)
            {
                FillXmlValues(aValueNode, aPath, aXmlValues);
            }
            var aXmlProgressionSnapshot = new CXmlProgressionSnapshot
            {
                Values = aXmlValues.ToArray(),
            };
            var aXmlSerializer = NewXmlSerializer();
            aFileInfo.Directory.Create();
            if (aFileInfo.Exists)
                aFileInfo.Delete();
            using var aFileStream = File.OpenWrite(aFileInfo.FullName);
            var aXmlWriter = XmlWriter.Create(aFileStream);
            aXmlSerializer.Serialize(aXmlWriter, aXmlProgressionSnapshot);
            aXmlWriter.Flush();
            aFileStream.Flush();
            aFileStream.SetLength(aFileStream.Position);
        }

        private static void FillXmlValues(CValueNode aValueNode, List<CNameEnum> aPath, List<CXmlValueBase> aXmlValues)
        {
            aPath.Add(aValueNode.Name);
            try
            {
                var aSelectedValueSource = aValueNode.ValueSource;
                if (aSelectedValueSource is object)
                {
                    var aXmlSelectedValueSource = new CXmlSelectedValueSource()
                    {
                        Path = GetPersistentPath(aPath),
                        Comment = GetComment(aPath),
                        Value = aSelectedValueSource.Name.GetGuidAttribute().Guid.ToString()
                    };
                    aXmlValues.Add(aXmlSelectedValueSource);
                }
                if (aValueNode.IsData)
                {
                    var aXmlValue = new CXmlValue()
                    {
                        Path = GetPersistentPath(aPath),
                        Comment = GetComment(aPath),
                        Value = aValueNode.StoredData,
                    };
                    aXmlValues.Add(aXmlValue);
                }
                foreach (var aDataNode in aValueNode.StoredNodes)
                {
                    FillXmlValues(aDataNode, aPath, aXmlValues);
                }
            }
            finally
            {
                aPath.RemoveAt(aPath.Count - 1);
            }
        }

    }

    public sealed class CParameters : CValueNode
    {
        #region ctor
        internal CParameters(CProgressionManager aParentProgressionManager) : base(aParentProgressionManager, CNameEnum.Parameters)
        {
            var aParameterCount = (int)CParameterEnum._Count;
            var aParameters = (from aIdx in Enumerable.Range(0, aParameterCount)
                               select CDataTypeAttribute.GetByEnum((CParameterEnum)aIdx).NewParameter(this, (CParameterEnum)aIdx)).ToArray();
            this.Parameters = aParameters;
        }
        internal override void Build()
        {
            base.Build();
            foreach (var aParameter in this.Parameters)
            {
                aParameter.Build();
            }

            var aFrameCount = 900;
            this[CParameterEnum.DoubleZero].SetConst<double>(0);
            this[CParameterEnum.DoubleZero].SetEditable(false);
            this[CParameterEnum.FrameCount].SetConst<Int64>(aFrameCount, 1, Int64.MaxValue);
            this[CParameterEnum.FrameCount].Min.SetEditable(false);
            this[CParameterEnum.FrameCount].Max.SetEditable(false);
            this[CParameterEnum.FrameCount].SetValueSourceEditable(false);
            this[CParameterEnum.FrameIndex].SetConst<Int64>(aFrameCount / 2, 0, Int64.MaxValue);
            this[CParameterEnum.FrameIndex].Min.SetEditable(false);
            this[CParameterEnum.FrameIndex].Max.SetEditable(false);
            this[CParameterEnum.JuliaRealPart].As<CParameter<double>>().Min.Value = -4.3d;
            this[CParameterEnum.JuliaRealPart].As<CParameter<double>>().Max.Value = 3;
            this[CParameterEnum.JuliaRealPart].SetConst<double>(0.446d); // -0.68
            this[CParameterEnum.JuliaRealPart].SetMappedProgression(0.43d, 0.47d);
            this[CParameterEnum.JuliaImaginaryPart].As<CParameter<double>>().Min.Value = 0.3d;
            this[CParameterEnum.JuliaImaginaryPart].As<CParameter<double>>().Max.Value = 0.1d;
            this[CParameterEnum.JuliaImaginaryPart].SetConst<double>(0.24d);
            this[CParameterEnum.JuliaImaginaryPart].SetMappedProgression(0.23d,0.26d);
            this[CParameterEnum.Zoom].SetConst<double>(0d, 1, 1000);
            this[CParameterEnum.Zoom].SetProgression(1d, 1000d);
            this[CParameterEnum.Iterations].SetConst<Int64>(300, 1, Int64.MaxValue);
            this[CParameterEnum.Iterations].SetMappedProgression(300, 10);
            this[CParameterEnum.DarkenThresholdLo].SetConst<double>(0.3);
            this[CParameterEnum.DarkenThresholdHi].SetConst<double>(0.2);
            this[CParameterEnum.ColorPeriod].SetConst<double>(1d);
            this[CParameterEnum.ColorOffset].SetConst<double>(1.5d);
            this[CParameterEnum.PixelAlgorithm1].SetConst(CPixelAlgorithmEnum.MandelbrotJuliaSingle);
            this[CParameterEnum.PixelAlgorithm1].MappedProgression.SetSelectable(false);
            this[CParameterEnum.CenterX].SetConst<double>(0.5d);
            this[CParameterEnum.CenterY].SetConst<double>(0.5d);


            //var aJuliaConstReal = this[CParameterEnum.JuliaConstRealPart];
            //aJuliaConstReal.SetConst<double>(-0.7d);
            //aJuliaConstReal.SetConst<double>(-0.68);
            //aJuliaConstReal.MappedProgression.MappedMin.Value = -0.677d;
            //aJuliaConstReal.MappedProgression.MappedMax.Value = -1.5d;//  - 0.88d;
            //aJuliaConstReal.Progression = aJuliaConstReal.MappedProgression;

            //var aJuliaConstImaginaryPart = this[CParameterEnum.JuliaConstImaginaryPart];
            ////aJuliaConstImaginaryPart.SetConst(-0.27015d);
            //aJuliaConstImaginaryPart.SetConst<double>(0.3d);
            ////aJuliaConstImaginaryPart.MappedProgression.MappedMin.Value = 0.3d;
            ////aJuliaConstImaginaryPart.MappedProgression.MappedMax.Value = 0.1d;
            //aJuliaConstImaginaryPart.Progression = aJuliaConstImaginaryPart.MappedProgression;


            //this[CParameterEnum.JuliaMoveX].SetConst<double>(-0.07d);
            //this[CParameterEnum.JuliaMoveY].SetConst<double>(0.305d);
            //            this[CParameterEnum.Iterations].SetConst<Int64>(300);
            //this[CParameterEnum.DarkenThresholdLo].SetConst<double>(0.1);
            //this[CParameterEnum.DarkenThresholdHi].SetConst<double>(0.2);
            //this[CParameterEnum.ColorPeriod].SetConst<double>(1d);
            //this[CParameterEnum.ColorOffset].SetConst<double>(1.5d);
            //this[CParameterEnum.PixelAlgorithm1].SetConst(CPixelAlgorithmEnum.MandelbrotJuliaSingle);
            //this[CParameterEnum.CenterX].SetConst<double>(0.5d);
            //this[CParameterEnum.CenterY].SetConst<double>(0.5d);
        }
        #endregion
        #region Parameters
        internal readonly CParameter[] Parameters;
        private IEnumerable<CParameter> VmParametersM;
        public IEnumerable<CParameter> VmParameters => CLazyLoad.Get(ref this.VmParametersM, () => this.Parameters.OrderBy(p => p.Name));

        public CParameter this[CParameterEnum aParameterEnum]
        {
            get => this.Parameters[(int)aParameterEnum];
        }
        public T Get<T>(CParameterEnum aParameterEnum) where T : CParameter
            => (T)this[aParameterEnum];
        #endregion
        #region Parameter
        private CParameter ParameterM;
        internal CParameter Parameter
        {
            get => this.ParameterM;
            set
            {
                this.ParameterM = value;
                this.ValueSource = value;
                this.OnPropertyChanged(nameof(this.VmParameter));
            }
        }
        public CParameter VmParameter
        {
            get => this.Parameter;
            set => this.Parameter = value;
        }
        #endregion
        #region Value
        internal override object GetTypelessValue() => this.Parameter.GetTypelessValue();
        #endregion
        #region ParameterSnapshot
        internal CParameterSnapshot NewParameterSnapshot()
            => new CParameterSnapshot(this);
        #endregion
        internal override IEnumerable<CValueNode> StoredNodes => this.Parameters;
        private DirectoryInfo DirectoryInfo => new FileInfo(this.GetType().Assembly.Location).Directory;
        //private FileInfo DefaultFileInfo=>new FileInfo(Environment.GetFolderPath(Path.Combine(Environment.SpecialFolder.MyDocuments), )
        private FileInfo DefaultFileInfo => new FileInfo(System.IO.Path.Combine(this.DirectoryInfo.FullName, "DefaultSettings", "Default.CbFractal.Progression.xml"));
        internal void SaveAsDefault()
        {
            CXmlProgressionSnapshot.Save(this, this.DefaultFileInfo);
        }
        internal override IEnumerable<CValueNode> SubValueNodes => this.Parameters;
        
    }

    public enum CValueNodeGuiEnum
    {
        Null,
        ParameterRef,
        TextBox,
        ValueSources,
        Enum,
    }

    public class CGuidAttribute : Attribute
    {
        public CGuidAttribute(string aGuid)
        {
            this.Guid = Guid.Parse(aGuid);
        }
        internal readonly Guid Guid;
    }

    public enum CNameEnum
    {
        [CGuid("0e3ae2ee-4e6f-4104-ac7c-9a766b38579d")]
        Constant,

        [CGuid("6947d618-1b32-4fc1-9167-72df4ebab00d")]
        Min,

        [CGuid("9589a0e3-c935-4ba5-9ecb-7b70a690aa8d")]
        Max,

        [CGuid("07c49d2f-7824-49a1-9d4f-a7ad5fc8792c")]
        MappedProgression,

        [CGuid("8602b4fc-5a3f-4aab-b743-04dd05e14684")]
        ParameterRef_ControlMin,

        [CGuid("9aae2525-786e-45f0-baad-15f5b86edee7")] 
        ParameterRef_ControlMax,

        [CGuid("e566b2d6-eaff-469e-8cfe-05f5c605c63f")] 
        ParameterRef_ControlValue,

        [CGuid("cb3a35ff-bf99-4a05-b729-3a314a3e8a5a")] 
        ParameterRef_MappedMin,

        [CGuid("36711a15-cbc5-4b36-95ec-671b211b4480")]
        ParameterRef_MappedMax,

        [CGuid("31c6217e-417b-40e6-ae25-793cefb3b5db")]
        Parameters,

        [CGuid("a36f7633-a6fa-45ee-83a5-c0cf674cd802")]
        Parameter_CenterX,

        [CGuid("ad54e80d-ab69-49b1-bcd2-43dcd6af71a2")]
        Parameter_CenterY,

        [CGuid("a6fc4052-386e-4f30-99ab-278cf8af3021")]
        Parameter_DoubleZero,

        [CGuid("a19d056f-c94d-4941-85ff-5a6d5aaa50c6")]
        Parameter_FrameCount,

        [CGuid("6e2b59d2-8e62-42a7-808f-ecab1639de80")]
        Parameter_FramePos,

        [CGuid("1381b37a-bd4a-40ab-865c-fcdda4a1416c")]
        Parameter_JuliaRealPart,

        [CGuid("dc82d396-9bff-430c-9ef2-469b0f77ee7d")]
        Parameter_JuliaImaginaryPart,

        [CGuid("84894455-06c7-40b6-812c-b63eb537d2e6")]
        Parameter_JuliaMoveX,

        [CGuid("ef4e5902-e225-44f1-bc65-a7d42139c275")]
        Parameter_JuliaMoveY,

        [CGuid("13d6f48d-89d7-41ca-898c-0f7c68eb52b8")]
        Parameter_Zoom,

        [CGuid("380d6d30-8083-4ae6-b291-d644f06a1a3f")]
        Parameter_Iterations,

        [CGuid("ea4a1691-74d5-4a58-b7dd-6219d055d971")]
        Parameter_DarkenThresholdLo,

        [CGuid("e85a4dd0-f632-49f5-b755-669881996745")]
        Parameter_DarkenThresholdHi,
        
        [CGuid("7dae15e2-f6b5-4c3f-bc12-9a8b3cfffb05")] 
        Parameter_ColorPeriod,

        [CGuid("b53519c7-6479-454b-986d-5a6783cfd919")] 
        Parameter_ColorOffset,

        [CGuid("5797cdbe-ff8d-4bbb-8f2a-7f470682a6b7")] 
        Parameter_PixelAlgorithm1,

    }

    public abstract class CValueNode : CViewModel
    {
        internal CValueNode(CProgressionManager aParentProgressionManager, CNameEnum aName)
        {
            this.ParentProgressionManager = aParentProgressionManager;
            this.Name = aName;
        }
        internal readonly CProgressionManager ParentProgressionManager;

        #region Name
        internal const CNameEnum Name_Max = CNameEnum.Max;
        internal const CNameEnum Name_Min = CNameEnum.Min;
        internal const CNameEnum Name_Constant = CNameEnum.Constant;
        internal const CNameEnum Name_MappedProgression = CNameEnum.MappedProgression;
        internal readonly CNameEnum Name;
        public string VmName => this.Name.ToString().TryTrimStart("Parameter_"); // TODO-Hack
        #endregion
        #region Value
        //internal abstract void SetTypelessValue(object v);
        internal abstract object GetTypelessValue();
        internal double GetDoubleValue() => Convert.ToDouble(this.GetTypelessValue());
        //internal object TypelessValue { get => this.GetTypelessValue(); set => this.SetTypelessValue(this.ConvertToTypedValue(value)); }
        //internal abstract object ConvertToTypedValue(object o);
        //internal virtual double GetDoubleValue() => Convert.ToDouble(this.TypelessValue);
        internal void OnValueChanged()
        {
            this.ParentProgressionManager.OnChangeValue(this);
        }
        #endregion
        #region Buid
        internal virtual void Build()
        {
        }
        #endregion
        #region ValueSources
        internal virtual IEnumerable<CValueNode> StoredNodes => this.SubValueNodes.Where(n=>n.Supported && n.DataEditable);
        internal virtual IEnumerable<CValueNode> ValueSources => Array.Empty<CValueNode>();
        public IEnumerable<CValueNode> VmValueSources => this.ValueSources;
        #endregion
        #region ValueNode
        private CValueNode ValueSourceM;
        internal virtual CValueNode ValueSource
        {
            get => this.ValueSourceM;
            set
            {
                this.ValueSourceM = value;
                this.OnPropertyChanged(nameof(this.VmValueSource));
            }
        }
        public CValueNode VmValueSource
        {
            get => this.ValueSource;
            set => this.ValueSource = value;
        }
        #endregion
        #region ValueNodeGui
        internal virtual CValueNodeGuiEnum ValueNodeGuiEnum => CValueNodeGuiEnum.Null;
        public CValueNodeGuiEnum VmValueNodeGuiEnum =>  this.Supported ? this.ValueNodeGuiEnum : CValueNodeGuiEnum.Null;
        #endregion
        internal bool Supported = true;
        internal bool IsVisible => this.Supported;
        public bool VmIsVisible => this.IsVisible;
        #region Storage
        internal virtual bool IsData => false;
        internal virtual string StoredData { get => throw new InvalidOperationException(); set => throw new InvalidOperationException(); }
        #endregion
        internal virtual IEnumerable<CValueNode> SubValueNodes => Array.Empty<CValueNode>();

        internal virtual void SetEditable(bool v)
        {
            this.SetValueSourceEditable(v);
            this.SetDataEditable(v);
            foreach (var aSubValueNode in this.SubValueNodes)
                aSubValueNode.SetEditable(v);            
        }
        private bool ValueSourceEditable = true;
        public bool VmValueSourceEditable => this.ValueSourceEditable;
        internal void SetValueSourceEditable(bool b)
        {
            this.ValueSourceEditable = b;
            foreach (var aSubNode in this.SubValueNodes)
                aSubNode.SetValueSourceEditable(b);
        }
        private bool DataEditable = true;
        public bool VmDataEditable => this.DataEditable;
        internal void SetDataEditable(bool b)
        {
            this.DataEditable = b;
            foreach (var aSubValueNode in this.SubValueNodes)
                aSubValueNode.SetDataEditable(b);
        }
        internal void SetSelectable(bool b)
        {
            this.Selectable = b;
        }
        internal bool Selectable = true;
        public bool VmSelectable => this.Selectable;
    
    }

    public abstract class CProgression : CValueNode
    {
        #region ctor
        internal CProgression(CValueNode aParentValueNode, CNameEnum aName) : base(aParentValueNode.ParentProgressionManager, aName)
        {
            this.ParentValueNode = aParentValueNode;
        }
        #endregion
        #region ParentParameter
        internal readonly CValueNode ParentValueNode;
        #endregion
    }
    public abstract class CConstant : CProgression
    {
        #region ctor
        internal CConstant(CValueNode aParentValueSource, CNameEnum aName) : base(aParentValueSource, aName)
        {
        }
        #endregion
        #region Value
        internal virtual void Increment() => throw new NotImplementedException();
        #endregion
        //internal CConstant<T> As<T>()=>(CConstant<T>)this;
        internal T As<T>() where T: CConstant => (T)this;
        internal override bool IsData => true;
        internal virtual void SetDefaultMin() { }
        internal virtual void SetDefaultMax() { }
    }

    public abstract class CConstant<T> : CConstant
    {
        #region ctor
        internal CConstant(CValueNode aParentValueSource, CNameEnum aName) : base(aParentValueSource, aName)
        {
        }
        #endregion
        #region Value
        // internal override object ValueAsObject { get => this.Value; set => this.Value = (T)(object)value; }
        private T ValueM;
        internal T Value
        {
            get => this.ValueM;
            set
            {
                if (!object.Equals(this.ValueM, value))
                {
                    this.ValueM = value;
                    this.OnValueChanged();
                    this.OnPropertyChanged(nameof(this.VmValue));
                    this.OnPropertyChanged(nameof(this.VmValueText));

                }
            }
        }
        public string VmValueText
        {
            get => this.ToString(this.Value);
            set
            {
                this.Value = this.Parse(value);
            }
        }
        public T VmValue { get => this.Value; set => this.Value = value; }
        internal override object GetTypelessValue() => this.Value;
        internal abstract T Parse(string s);
        internal virtual string ToString(T aValue) => aValue.ToString();
        #endregion
        #region Gui
        internal override CValueNodeGuiEnum ValueNodeGuiEnum => CValueNodeGuiEnum.TextBox;
        internal override string StoredData 
        { 
            get => this.VmValueText;
            set
            {
                try
                {
                    this.VmValueText = value;
                }
                catch(Exception)
                {
                    // TODO-may happen when switching os-language
                }
            }
        } 
        #endregion
    }
    internal sealed class CDoubleConstant : CConstant<double>
    {
        #region ctor
        internal CDoubleConstant(CValueNode aParentValueSource, CNameEnum aName) : base(aParentValueSource, aName)
        {
        }
        #endregion
        #region Value
        internal override double Parse(string s) => double.Parse(s);
        #endregion
        #region Gui
        internal override CValueNodeGuiEnum ValueNodeGuiEnum => CValueNodeGuiEnum.TextBox;
        #endregion
        internal override void SetDefaultMin() => this.Value = 0d;
        internal override void SetDefaultMax() => this.Value = 1d;
    }
    internal sealed class CInt64Constant : CConstant<Int64>
    {
        #region ctor
        internal CInt64Constant(CValueNode aParentValueSource, CNameEnum aName) : base(aParentValueSource, aName)
        {
        }
        #endregion
        #region Value
        internal override Int64 Parse(string s) => Int64.Parse(s);
        internal override void Increment()
        {
            this.Value = this.Value + 1;
        }
        #endregion
        #region Gui

        #endregion
    }

    internal abstract class CEnumConstant<TEnum> : CConstant<TEnum>
    {
        #region ctor
        internal CEnumConstant(CValueNode aParentValueSource, CNameEnum aName) : base(aParentValueSource, aName)
        {
        }
        #endregion
        #region Value
        internal override TEnum Parse(string s) => (TEnum)Enum.Parse(typeof(TEnum), s);
        public IEnumerable<TEnum> VmValues => Enum.GetValues(typeof(TEnum)).Cast<TEnum>().OrderBy(e => e.ToString());
        internal override CValueNodeGuiEnum ValueNodeGuiEnum => CValueNodeGuiEnum.Enum;
        #endregion

    }

    internal enum CPixelAlgorithmEnum
    {
        [CDataType(typeof(CClassicMandelbrotSetPixelAlgorithm))]
        MandelbrotClassicSet,

        [CDataType(typeof(CSingleJuliaMandelbrotSetPixelAlgorithm))]
        MandelbrotJuliaSingle,

        [CDataType(typeof(CMultiJuliaMandelbrotSetPixelAlgorithm))]
        MandelbrotJuliaMulti,
    }
    internal sealed class CPixelAlgorithmEnumConstant : CEnumConstant<CPixelAlgorithmEnum>
    {
        public CPixelAlgorithmEnumConstant(CValueNode aParentValueSource, CNameEnum aName) : base(aParentValueSource, aName)
        {
        }
    }
    internal sealed class CPixelAlgorithmEnumParameter : CEnumParameter<CPixelAlgorithmEnum>
    {
        public CPixelAlgorithmEnumParameter(CParameters aParentParameters, CParameterEnum aParameterEnum) : base(aParentParameters, aParameterEnum)
        {
        }
        internal override CConstant NewConstant(CValueNode aParentValueNode, CNameEnum aName)
            => new CPixelAlgorithmEnumConstant(aParentValueNode, aName);
    }


    internal sealed class CParameterRef : CValueNode
    {
        #region ctor
        internal CParameterRef(CProgression aProgression, CNameEnum aName) : base(aProgression.ParentProgressionManager, aName)
        {
            this.ParentProgression = aProgression;
        }
        #endregion
        #region Progression
        internal readonly CProgression ParentProgression;
        #endregion
        #region Parameters
        internal IEnumerable<CParameter> Parameters => this.ParentProgressionManager.Parameters.Parameters;
        public IEnumerable<CParameter> VmParameters => this.Parameters;
        #endregion
        #region Parameter
        private CParameter ParameterM;
        internal CParameter Parameter
        {
            get => CLazyLoad.Get(ref this.ParameterM, () => this.ParentProgressionManager.Parameters[CParameterEnum.DoubleZero]);
            set
            {
                this.ParameterM = value;
                this.OnPropertyChanged(nameof(this.VmParameter));
            }
        }
        public CParameter VmParameter
        {
            get => this.Parameter;
            set => this.Parameter = value;
        }
        #endregion
        internal override CValueNodeGuiEnum ValueNodeGuiEnum => CValueNodeGuiEnum.ParameterRef;
        internal override object GetTypelessValue() => this.Parameter.GetTypelessValue();
    }

    internal sealed class CMappedProgression : CProgression
    {
        internal CMappedProgression(CValueNode aParentValueNode, CNameEnum aName) : base(aParentValueNode, aName)
        {
            this.ControlMin = new CParameterRef(this, CNameEnum.ParameterRef_ControlMin);
            this.ControlMax = new CParameterRef(this, CNameEnum.ParameterRef_ControlMax);
            this.ControlValue = new CParameterRef(this, CNameEnum.ParameterRef_ControlValue);
            this.MappedMin = new CDoubleConstant(this, CNameEnum.ParameterRef_MappedMin);
            this.MappedMax = new CDoubleConstant(this, CNameEnum.ParameterRef_MappedMax);
        }

        internal readonly CParameterRef ControlMin;
        internal readonly CParameterRef ControlMax;
        internal readonly CParameterRef ControlValue;
        internal readonly CDoubleConstant MappedMin;
        internal readonly CDoubleConstant MappedMax;
        internal override IEnumerable<CValueNode> ValueSources
        {
            get
            {
                yield return this.ControlMin;
                yield return this.ControlMax;
                yield return this.ControlValue;
                yield return this.MappedMin;
                yield return this.MappedMax;
            }
        }
        internal override object GetTypelessValue()
        {
            var aControlMin = this.ControlMin.GetDoubleValue();
            var aControlMax = this.ControlMax.GetDoubleValue();
            var aControlValue = this.ControlValue.GetDoubleValue();
            var aMappedMin = this.MappedMin.GetDoubleValue();
            var aMappMaxMax = this.MappedMax.GetDoubleValue();

            var aControlRange = aControlMax - aControlMin;
            var aControlOffset = aControlValue - aControlMin;
            var aControlFaktor = aControlOffset / aControlRange;
            var aMappedRange = aMappMaxMax - aMappedMin;
            var aMappedOffset = aMappedRange * aControlFaktor;
            var aMappedValue = aMappedMin + aMappedOffset;
            return aMappedValue;
        }
        internal override void Build()
        {
            base.Build();
            this.ControlMin.Parameter = this.ParentProgressionManager.Parameters[CParameterEnum.DoubleZero];
            this.ControlMax.Parameter = this.ParentProgressionManager.Parameters[CParameterEnum.FrameCount];
            this.ControlValue.Parameter = this.ParentProgressionManager.Parameters[CParameterEnum.FrameIndex];
        }
        internal override CValueNodeGuiEnum ValueNodeGuiEnum => CValueNodeGuiEnum.ValueSources;
    }


    public sealed class CProgressionManager : CViewModel
    {
        internal CProgressionManager(MainWindow aMainWindow)
        {
            this.MainWindow = aMainWindow;
            this.Parameters = new CParameters(this);

            this.Parameters.Build();

            this.RenderFrameOnChangeValueIsEnabled = true;
        }


        private readonly MainWindow MainWindow;
        internal readonly CParameters Parameters;
        public CParameters VmParameters => this.Parameters;
        internal CMandelbrotState MandelbrotState => this.MainWindow.State;
        private bool RenderFrameOnChangeValueIsEnabled;
        internal void OnChangeValue(CValueNode aValueNode)
        {
            //if(object.ReferenceEquals(aValueNode, this.Parameters[CParameterEnum.FramePos].Constant))
            this.OnChangeRenderFrameOnDemand();
        }

        internal void OnChangeRenderFrameOnDemand()
        {
            if (this.RenderFrameOnChangeValueIsEnabled
            && !this.MainWindow.State.AnythingIsPending)
            {
                this.BeginRenderEntpreller.Entprellen();
            }
        }
        #region Dispatcher
        internal Dispatcher Dispatcher => this.MainWindow.Dispatcher;
        #endregion
        #region EntprellTimer
        private CEntpreller BeginRenderEntprellerM;
        private CEntpreller BeginRenderEntpreller => CLazyLoad.Get(ref this.BeginRenderEntprellerM, () => new CEntpreller(this.Dispatcher, () => new Action(delegate () { this.MandelbrotState.BeginRenderFrame(); })));
        #endregion
    }

    internal sealed class CEntpreller
    {
        internal CEntpreller(Dispatcher aDispatcher, Func<Action> aNewAction)
        {
            this.Dispatcher = aDispatcher;
            this.NewAction = aNewAction;
        }
        private readonly Func<Action> NewAction;
        private readonly Dispatcher Dispatcher;
        private DispatcherTimer EntprellTimerM;
        private DispatcherTimer EntprellTimer => CLazyLoad.Get(ref this.EntprellTimerM, this.NewEntprellTimer);
        private DispatcherTimer NewEntprellTimer()
        {
            var aTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(500), DispatcherPriority.Normal, new EventHandler(this.OnEntprellTimerElapsed), this.Dispatcher);
            return aTimer;
        }
        private void OnEntprellTimerElapsed(object aSender, EventArgs aArgs)
        {
            this.EntprellTimer.Stop();
            this.EntprellTimer.IsEnabled = false;
        }
        private void StartTimer()
        {
            this.EntprellTimer.Stop();
            this.EntprellTimer.Start();
            this.EntprellTimer.IsEnabled = true;
        }
        private Action EntprellAction;
        public void Entprellen()
        {
            this.Entprellen(this.NewAction());
        }
        public void Entprellen(Action aAction)
        {
            var aTimer = this.EntprellTimer;
            var aHandler = default(EventHandler);
            aHandler = new EventHandler(delegate (object aSender, EventArgs aArgs)
            {
                if (object.ReferenceEquals(this.EntprellAction, aAction))
                {
                    aTimer.Tick -= aHandler;
                    try
                    {
                        aAction();
                    }
                    catch (Exception aExc)
                    {
                        aExc.CatchUnexpected(this);
                    }
                }
            });
            aTimer.Tick += aHandler;
            this.EntprellAction = aAction;
            this.StartTimer();
        }
    }

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
    internal abstract class CPixelAlgorithm
    {
        internal CPixelAlgorithm(CPixelAlgorithmInput aInput)
        {
            var aParameters = aInput.Item3;            
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
            this.SizePxl = aInput.Item1;
            this.SizeMnd = aSizeMnd;
            this.Parameters = aInput.Item3;
            this.GetColor = aInput.Item4;
        }
        internal abstract Color RenderPixel(int aX, int aY);

        internal readonly CVec2 SizePxl;
        internal readonly CVec4 SizeMnd;
        internal readonly CParameterSnapshot Parameters;
        internal readonly Func<double, Color> GetColor;

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
            var itm = aParameters.Get<Int64>(CParameterEnum.Iterations); // 300; //after how much iterations the function should stop

            //pick some values for the constant c, this determines the shape of the Julia Set
            var cRe = aParameters.Get<double>(CParameterEnum.JuliaRealPart);// -0.7d;    // real part of the constant c, determinate shape of the Julia Set
            var cIm = aParameters.Get<double>(CParameterEnum.JuliaImaginaryPart); //0.27015d; // imaginary part of the constant c, determinate shape of the Julia Set
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
        private enum CCommitRenderMovieCancelEnum
        {
            Default
        }
        private CMandelbrotState(CMandelbrotState aPreviousState, CCommitRenderMovieCancelEnum aCmd)
        {
            throw new NotImplementedException();

            this.StateEnum = CStateEnum.EndCancelRenderMovie;
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
            var aSizePxl = aRenderFrameInput.Item1;
            var aDx = (int)aSizePxl.Item1;
            var aDy = (int)aSizePxl.Item2;
            var aSizeMnd = aRenderFrameInput.Item2;
            var aCenterMnd = aSizeMnd.GetRectCenter(); // aRenderInput.Item3;
            //var aThreadCount = 1;
            var aThreadCount = 90;
            var aThreadLinesRest = aDy % aThreadCount;
            var aLinesPerThread = (aDy - aThreadLinesRest) / aThreadCount;
            var aThreadInputs = from aThreadId in Enumerable.Range(0, aThreadCount)
                                select new CRenderFrameSegmentInput(aThreadId * aLinesPerThread, (aThreadId == aThreadCount - 1 && aThreadCount != 1 && aThreadLinesRest != 0) ? aThreadLinesRest : aLinesPerThread);
            var aPixels = new Color[aDx * aDy];
            var aParametersSnapshot = aRenderFrameInput.Item7;
            var aRenderPartFunc = new Action<CRenderFrameSegmentInput>(delegate (CRenderFrameSegmentInput aRenderFrameSegmentsInput)
             {
                 var aYStart = aRenderFrameSegmentsInput.Item1;
                 var aYCount = aRenderFrameSegmentsInput.Item2;
                 var aPixelCoords = from aY in Enumerable.Range(aYStart, aYCount)
                                    from aX in Enumerable.Range(0, (int)aDx)
                                    select new CVec2Int(aX, aY);
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
                 var co = aParametersSnapshot.Get<double>(CParameterEnum.ColorOffset); //1.5f
                 var cperiod = aParametersSnapshot.Get<double>(CParameterEnum.ColorPeriod); // 1d;
                 var aDarkenThresholdLo = aParametersSnapshot.Get<double>(CParameterEnum.DarkenThresholdLo); // 0.1;
                 var aDarkenTheesholdHi = aParametersSnapshot.Get<double>(CParameterEnum.DarkenThresholdHi); // 0.3;
                 var aDarken = new Func<Color, double, Color>(delegate (Color c, double d)
                    { return Color.FromScRgb(1.0f, (float)(c.ScR * d), (float)(c.ScG * d), (float)(c.ScB * d)); });
                 var aGetColor = new Func<double, Color>(itf =>
                 {
                     var c1 = this.Hue((itf + (1f / 3f * co)) * cperiod, 0d);
                     var c2 = itf < aDarkenThresholdLo
                            ? aDarken(c1, (itf / aDarkenThresholdLo))
                            : itf > 1d - aDarkenTheesholdHi
                            ? aDarken(c1, 1d - (((itf - (1d - aDarkenTheesholdHi))) / aDarkenTheesholdHi))
                            : c1;
                     return c2;
                 });

                 var aMandelPixelFunc = new Func<CVec2Int, Color>(aPixelCoord =>
                 {
                     var s = (1 << 16);
                     var aMandelPos = aGetMandelPos(aPixelCoord);
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
                     var c = aGetColor(itf);
                     return c;
                 });
                 var aJuliaPixelFunc = new Func<CVec2Int, Color>(aPixelCoord =>
                 {
                     var itm = aParametersSnapshot.Get<Int64>(CParameterEnum.Iterations); // 300; //after how much iterations the function should stop

                     //pick some values for the constant c, this determines the shape of the Julia Set
                     var cRe = aParametersSnapshot.Get<double>(CParameterEnum.JuliaRealPart);// -0.7d;    // real part of the constant c, determinate shape of the Julia Set
                     var cIm = aParametersSnapshot.Get<double>(CParameterEnum.JuliaImaginaryPart); //0.27015d; // imaginary part of the constant c, determinate shape of the Julia Set
                     //cIm = 0.3d;
                     //cRe = -0.68;
                     var h = aDy;
                     var w = aDx;
                     var x = aPixelCoord.Item1;
                     var y = aPixelCoord.Item2;
                     var zoom = aParametersSnapshot.Get<double>(CParameterEnum.Zoom); // SizeMndDefault.Item3 / aSizeMnd.Item3;
                     var moveX = aParametersSnapshot.Get<double>(CParameterEnum.JuliaMoveX);
                     var moveY = aParametersSnapshot.Get<double>(CParameterEnum.JuliaMoveY);
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
                     var c = aGetColor(itf);
                     return c;
                 });

                 var aPixelAlgoEnum = aParametersSnapshot.Get<CPixelAlgorithmEnum>(CParameterEnum.PixelAlgorithm1);
                 var aPixelAlgorithmInput = new CPixelAlgorithmInput(aSizePxl, aSizeMnd, aParametersSnapshot, aGetColor);
                 var aPixelAlgorithm = CDataTypeAttribute.GetByEnum(aPixelAlgoEnum).DataType.New<CPixelAlgorithm>(aPixelAlgorithmInput);
                 var aPixelFunc = aJuliaPixelFunc;
                 foreach (var aPixelCoord in aPixelCoords)
                 {
                     var aPixelIdx = aPixelCoord.Item2 * aDx + aPixelCoord.Item1;
                     var aColor = aPixelAlgorithm.RenderPixel(aPixelCoord.Item1, aPixelCoord.Item2);
                     //var aColor = aPixelFunc(aPixelCoord);
                     //var aColor = aMandelPixelFunc(aPixelCoord);
                     aPixels[aPixelIdx] = aColor;
                 }
             });

            var aTasks = (from aThreadInput in aThreadInputs select Task.Factory.StartNew(delegate () { aRenderPartFunc(aThreadInput); })).ToArray();

            foreach (var aTask in aTasks)
            {
                aTask.Wait();
            }
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
            var aRarestAndDominatingColor = CColorDetector.GetRarestAndDominatingColor(aPixels);
            var aRarestColor = aRarestAndDominatingColor.Item1;
            var aDominatingColor = aRarestAndDominatingColor.Item2;
            var aAutoCenter = false;
            CVec2 aNewCenter;
            if (aAutoCenter)
            {
                throw new NotImplementedException();
                //var aMandelToPixel = new Func<CVec2, CVec2>(v => MandelToPixel(v, aImageSize, aSizeMnd)); // v => v.Subtract(aMandelRect.GetRectPos()).Divide(aMandelRect.GetRectSize()).Mul(aImageSize));
                //var aPixelToMandel = new Func<CVec2, CVec2>(v => PixelToMandel(v, aImageSize, aSizeMnd)); // v =>  v.Divide(aImageSize).Mul(aMandelRect.GetRectSize()).Add(aMandelRect.GetRectPos()));
                //var aPixelCenter = aMandelToPixel(aCenterMnd);
                //var aCenterDetector = new CColorDetector(CColorDetector.GetColor(aRarestColor), aPixelCenter, aImageSize);
                //foreach (var aPixelCoord in aPixelCoords)
                //{
                //    var aPixelIdx = aPixelCoord.Item2 * aDy + aPixelCoord.Item1;
                //    var aPixel = aPixels[aPixelIdx];
                //    var aPixelCoordVec2 = aPixelCoord.ToVec2();
                //    aCenterDetector.Add(aPixel, aPixelCoordVec2);
                //}
                //aNewCenter = aPixelToMandel(aCenterDetector.NewCenter);
            }
            else
            {
                aNewCenter = aCenterMnd;
            }

            var aNewBitmapSource = new Func<BitmapSource>(() => BitmapSource.Create((int)aDx, (int)aDy, 96d, 96d, PixelFormats.Rgb24, default(BitmapPalette), aPixelsRgb24, aStrideInBytes));
            var aNewRenderFrameOutput = new Func<CRenderFrameOutput>(() => new CRenderFrameOutput
            (
                aNewBitmapSource(),
                aSizeMnd,
                new CVec2(0, 0),
                aRarestColor,
                aDominatingColor,
                aNewCenter
            //aRenderFrameInput.Item7
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

            //this.MainWindow.Image.Source = aRenderFrameOutput.Item1;
            //var aMandelSpace = aRenderFrameOutput.Item2;
            //var aMandelCenter = aRenderFrameOutput.Item3;
            //var aRarestColor = CColorDetector.GetColor(aRenderFrameOutput.Item4);
            //var aDominantColor = CColorDetector.GetColor(aRenderFrameOutput.Item5);


            //throw new NotImplementedException();
            ////var aPositionState = new CMandelbrotState(aRenderOutput, CMandelbrotState.CRenderFrameThreadDoneEnum.Default);
            ////this.MandelbrotState = aPositionState;
            ////var aCenterFktCoord = this.NewFktCoordFromMandelCoord(aMandelSpace, aMandelCenter); // aMandelCenter.Subtract(aMandelSpace.GetRectPos()).Divide(aMandelSpace.GetRectSize());
            ////if(aDisableCenterAnimation)
            ////{
            ////this.CenterTargetFktCoord = aCenterFktCoord;
            ////}
            ////this.MandelCenterFktTest = aMandelCenterFkt;
            //this.RarestColorBrush = new SolidColorBrush(aRarestColor);
            //this.DominantColorBrush = new SolidColorBrush(aDominantColor); ;
            //this.RenderFrameTaskNullable = null;
            //this.RenderFrameIsPending = false;
            //if (this.RenderMovieIsPending.Value)
            //{
            //    throw new NotImplementedException();
            //    // this.RenderBatchFrameNext();                
            //}            
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
            this.MainWindow.ProgressionManager.Parameters[CParameterEnum.FrameIndex].Constant.Increment();
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





    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region ctor
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            CFullMandelModel.CalcMemoryCosts();

            new CMainWindowLoadedState(this).OnMainWindowLoaded();
        }
        #endregion
        #region ZoomSlider
        private double? ZoomSliderValueM;
        internal double ZoomSliderValue
        {
            get => CLazyLoad.Get(ref this.ZoomSliderValueM, () => this.ZoomSlider.Maximum);
            set
            {
                this.ZoomSliderValueM = value;
                this.State.UpdateFrameCountProposal();
                this.OnPropertyChanged(nameof(this.VmZoomSliderValue));
            }
        }
        internal double ZoomInFaktor => this.ZoomSliderValue;
        internal double ZoomOutFaktor => 2 - this.ZoomSliderValue;
        public double VmZoomSliderValue { get => this.ZoomSliderValue; set => this.ZoomSliderValue = value; }
        internal bool ZoomSliderIsEnabled { get => !this.State.AnythingIsPending; }
        public bool VmZoomSliderIsEnabled => this.ZoomSliderIsEnabled;
        #endregion
        #region PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string aPropertyName)
        {
            if (this.PropertyChanged is object)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(aPropertyName));
            }
        }
        #endregion
        #region MandelbrotState
        private CMandelbrotState StateM;
        internal CMandelbrotState State
        {
            get => this.StateM; // CLazyLoad.Get(ref this.StateM, () => new CMainWindowLoadedState(this));
            set
            {
                if (!object.ReferenceEquals(this.StateM, value))
                {
                    if (this.StateM is object)
                    {
                        var aOldState = this.StateM;
                        this.StateM = default;
                        aOldState.OnLeft();
                    }
                    this.StateM = value;
                    this.OnPropertyChanged(nameof(this.VmState));
                    this.OnPropertyChanged(nameof(this.VmCenterSourceEllipsePos));
                    this.OnPropertyChanged(nameof(this.VmCenterSourceMnd));
                    this.OnPropertyChanged(nameof(this.VmCenterSourcePxlRounded));
                    this.OnPropertyChanged(nameof(this.VmZoomSliderIsEnabled));
                    value.OnEntered();
                }

            }
        }
        public CMandelbrotState VmState => this.State;
        #endregion
        #region States
        private ObservableCollection<CMandelbrotState> StatesM = new ObservableCollection<CMandelbrotState>();
        internal ObservableCollection<CMandelbrotState> States => this.StatesM;
        public ObservableCollection<CMandelbrotState> VmStates => this.States;
        #endregion
        #region ZoomCenter
        private Point CenterSourcePxl => this.ZoomCenterByMouse.HasValue
                                ? this.ZoomCenterByMouse.Value
                                : this.State.CenterPxl.ToPoint();
        public object VmCenterSourcePxlRounded => this.CenterSourcePxl.ToVec2().Round().ToPoint();
        private Point? ZoomCenterByMouseM;
        private Point? ZoomCenterByMouse
        {
            get => this.ZoomCenterByMouseM;
            set
            {
                this.ZoomCenterByMouseM = value;
                this.OnPropertyChanged(nameof(this.VmCenterSourceEllipsePos));
                this.OnPropertyChanged(nameof(this.VmCenterSourceMnd));
                this.OnPropertyChanged(nameof(this.VmCenterSourcePxlRounded));
            }
        }
        #endregion
        #region ZoomEllipsePos
        public Point CenterSourceEllipsePos => this.ElipseCenterToElipsePos(this.CenterSourcePxl.ToVec2(), this.CenterEllipse);
        public Point VmCenterSourceEllipsePos => this.CenterSourceEllipsePos;
        internal Point CenterSourceMnd => this.State.PixelToMandel(this.CenterSourcePxl.ToVec2()).ToPoint();
        public Point VmCenterSourceMnd => this.CenterSourceMnd;


        internal Point ElipseCenterToElipsePos(CVec2 v, Ellipse aEllipse)
            => new Point(v.Item1 - aEllipse.Width / 2d,
                        v.Item2 - aEllipse.Height / 2d);

        #endregion
        #region MouseMove
        private void MoveCenter(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed
            && this.Canvas.IsMouseOver
            && !this.State.AnythingIsPending)
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
        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseUp(e);

            try
            {
                if (this.ZoomCenterByMouse.HasValue)
                {
                    var aCenterPxl = this.ZoomCenterByMouse.Value.ToVec2();
                    var aCenterMnd = this.State.PixelToMandel(aCenterPxl);
                    this.ZoomCenterByMouse = default;
                    this.State.Move(aCenterMnd);
                }
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
        #endregion
        #region Zoom
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            try
            {

                //if (e.Key == Key.Add)
                //{
                //    if (!this.State.AnythingIsPending)
                //    {
                //        this.State.Zoom(this.ZoomInFaktor);
                //    }
                //    e.Handled = true;

                //}
                //else if (e.Key == Key.Subtract)
                //{
                //    if (!this.State.AnythingIsPending)
                //    {
                //        this.State.Zoom(this.ZoomOutFaktor);
                //    }
                //    e.Handled = true;
                //}
            }
            catch (Exception aExc)
            {
                aExc.CatchUnexpected(this);
            }
        }
        #endregion
        #region RenderMovie
        private void OnBeginRenderMovieButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                this.State.BeginRenderMovie();
            }
            catch (Exception aExc)
            {
                aExc.CatchUnexpected(this);
            }
        }

        private void OnRenderBatchCancelButtonClick(object sender, RoutedEventArgs e)
        {
            this.State.BeginCancelRenderMovie();
        }
        #endregion
        #region Reset
        private void Reset()
        {
            this.State.Reset();
        }
        private void ResetButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Reset();
            }
            catch (Exception aExc)
            {
                aExc.CatchUnexpected(this);
            }
        }
        #endregion
        #region ProgressionManager
        private CProgressionManager ProgressionManagerM;
        internal CProgressionManager ProgressionManager => CLazyLoad.Get(ref this.ProgressionManagerM, () => new CProgressionManager(this));
        public CProgressionManager VmProgressionManager => this.ProgressionManager;
        #endregion

        private void SaveProgression(object sender, RoutedEventArgs e)
        {
            try
            {
                this.ProgressionManager.Parameters.SaveAsDefault();
            }
            catch(Exception aExc)
            {
                aExc.CatchUnexpected(this);
            }
        }
    }
}
