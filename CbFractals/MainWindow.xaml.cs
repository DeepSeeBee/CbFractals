using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
using System.Windows.Threading;

namespace CbFractals
{
    using CVec4 = Tuple<double, double, double, double>;
    using CVec2 = Tuple<double, double>;
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
    using CVec2Int = Tuple<int, int>;
    using CRenderFrameSegmentInput = Tuple<int,                            // YStart
                                           int                             // YCount                                           
                                           >;
    internal enum CBaseColorEnum
    {
        Red = 0,
        Green = 1,
        Blue = 2
    }
    internal static class CExtensions
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
        public void OnPropertyChanged(string aName)
        {
            if (this.PropertyChanged is object)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(aName));
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
        internal CEndRenderMovieState(CMandelbrotState aPreviousState, CEndRenderMovieEnum aCmd):base(aPreviousState, aCmd)
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
        internal CEndCancelRenderMovieState(CMandelbrotState aPreviousState, CEndCancelRenderMovieEnum aCmd):base(aPreviousState, aCmd)
        {
        }
    }

    public sealed class CNextFrameState : CMandelbrotState
    {
        internal CNextFrameState(CMandelbrotState aPreviousState, CNextFrameEnum aCmd):base(aPreviousState, aCmd)
        {

        }
        internal override void OnEntered()
        {
            base.OnEntered();
            this.Zoom(this.MainWindow.ZoomInFaktor);
        }
    }

    public sealed class CParameter : CValueNode
    {
        internal CParameter(CParameters aParameters, CParameterEnum aParameterEnum) : base(aParameters.ParentProgressionManager, aParameterEnum.ToString())
        {
            this.ParentParameters = aParameters;
            this.ParameterEnum = aParameterEnum;
            this.Constant = new CConstant(this, Name_Constant);
            this.MappedProgression = new CMappedProgression(this, Name_MappedProgression);
            this.NullProgression = new CNullProgression(this);

            this.Progression = this.NullProgression;
        }
        internal readonly CParameters ParentParameters;
        internal readonly CParameterEnum ParameterEnum;
        #region Constant
        internal readonly CConstant Constant;
        public CConstant VmConstant => this.Constant;
        #endregion
        #region LinearProgression
        internal readonly CMappedProgression MappedProgression;
        #endregion
        #region NullProgression
        internal readonly CNullProgression NullProgression;
        #endregion
        #region Progressions
        internal IEnumerable<CProgression> Progressions
        {
            get
            {
                yield return this.NullProgression;
                yield return this.Constant;
                yield return this.MappedProgression;
            }
        }
        public IEnumerable<CProgression> VmProgressions => this.Progressions;
        #endregion
        #region Progression
        private CProgression ProgressionM;
        internal CProgression Progression
        {
            get => this.ProgressionM;
            set
            {
                this.ProgressionM = value;
                this.OnPropertyChanged(nameof(this.VmProgression));
            }
        }
        public CProgression VmProgression
        {
            get => this.Progression;
            set => this.Progression = value; 
        }
        internal override double GetValue()
            => this.Progression.GetValue();
        #endregion
        #region Build
        internal override void Build()
        {
            base.Build();
            foreach(var aProgression in this.Progressions)
            {
                aProgression.Build();
            }
        }
        #endregion

    }

    public enum CParameterEnum
    {
        Zero,
        FrameCount,
        FramePos,
        JuliaConstRealPart,
        JuliaConstImaginaryPart,
        JuliaMoveX,
        JuliaMoveY,
        JuliaZoom,
        _Count
    }

    internal sealed class CParameterSnapshot
    {
        internal CParameterSnapshot(CParameters aParameters)
        {
            this.Values = aParameters.Parameters.Select(aParam => aParam.GetValue()).ToArray();
        }
        private double[] Values;
        public double this[CParameterEnum aParameterEnum]
        {
            get => this.Values[(int)aParameterEnum];
        }
    }

    public sealed class CParameters : CValueNode
    {
        #region ctor
        internal CParameters(CProgressionManager aParentProgressionManager) : base(aParentProgressionManager, "Parameters")
        {
            var aParameterCount = (int)CParameterEnum._Count;
            var aParameters = (from aIdx in Enumerable.Range(0, aParameterCount) select new CParameter(this, (CParameterEnum)aIdx)).ToArray();
            this.Parameters = aParameters;
        }
        internal override void Build()
        {
            base.Build();
            foreach (var aParameter in this.Parameters)
            {
                aParameter.Build();
            }

            var aZero = this[CParameterEnum.Zero];
            aZero.Constant.Value = 0;
            aZero.Progression = aZero.Constant;

            var aFrameCount = this[CParameterEnum.FrameCount];
            aFrameCount.Constant.Value = 900;
            aFrameCount.Progression = aFrameCount.Constant;

            var aFramePos = this[CParameterEnum.FramePos];
            aFramePos.Constant.Value = 0;
            aFramePos.Progression = aFramePos.Constant;

            var aJuliaConstReal = this[CParameterEnum.JuliaConstRealPart];
            //aJuliaConstReal.Constant.Value = -0.7d;
            aJuliaConstReal.Constant.Value = -0.68;
            aJuliaConstReal.MappedProgression.MappedMin.Value = -0.677d;
            aJuliaConstReal.MappedProgression.MappedMax.Value = -1.5d;//  - 0.88d;
            //aJuliaConstReal.Progression = aJuliaConstReal.Constant;
            aJuliaConstReal.Progression = aJuliaConstReal.MappedProgression;

            var aJuliaConstImaginaryPart = this[CParameterEnum.JuliaConstImaginaryPart];
            //aJuliaConstImaginaryPart.Constant.Value = -0.27015d;
            aJuliaConstImaginaryPart.Constant.Value = 0.3d;
            aJuliaConstImaginaryPart.MappedProgression.MappedMin.Value = 0.3d;
            aJuliaConstImaginaryPart.MappedProgression.MappedMax.Value = 0.1d;
            //aJuliaConstImaginaryPart.Progression = aJuliaConstImaginaryPart.Constant;
            aJuliaConstImaginaryPart.Progression = aJuliaConstImaginaryPart.MappedProgression;

            var aJuliaZoom = this[CParameterEnum.JuliaZoom];
            aJuliaZoom.Constant.Value = 1d;
            aJuliaZoom.Progression = aJuliaZoom.Constant;
        }
        #endregion
        #region Parameters
        internal readonly CParameter[] Parameters;
        public IEnumerable<CParameter> VmParameters => this.Parameters;
        
        public CParameter this[CParameterEnum aParameterEnum]
        {
            get => this.Parameters[(int)aParameterEnum];
        }
        #endregion
        #region Parameter
        private CParameter ParameterM;
        internal CParameter Parameter
        {
            get => this.ParameterM;
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
        #region Value
        internal override double GetValue()
           => this.Parameter.GetValue();
        #endregion
        #region ParameterSnapshot
        internal CParameterSnapshot NewParameterSnapshot()
            => new CParameterSnapshot(this);
        #endregion
    }

    public enum CValueNodeGuiEnum
    {
        Null,
        ParameterRef,
        Constant,
        ValueSources,
    }

    public abstract class CValueNode : CViewModel
    {
        internal CValueNode (CProgressionManager aParentProgressionManager, string aName)
        {
            this.ParentProgressionManager = aParentProgressionManager;
            this.Name = aName;
        }
        internal readonly CProgressionManager ParentProgressionManager;

        #region Name
        internal const string Name_Constant = "Constant";
        internal const string Name_Null = "Null";
        internal const string Name_MappedProgression = "MappedProgression";
        private string Name;
        public string VmName => this.Name;
        #endregion
        #region Value
        internal abstract double GetValue(); // => default;
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
        internal virtual IEnumerable<CValueNode> ValueSources { get; }
        public IEnumerable<CValueNode> VmValueSources => this.ValueSources;
        #endregion
        #region ValueNode
        private CValueNode ValueSourceM;
        internal CValueNode ValueSource
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
        public CValueNodeGuiEnum VmValueNodeGuiEnum => this.ValueNodeGuiEnum;
        #endregion
    }

    public abstract class CProgression : CValueNode
    {
        #region ctor
        internal CProgression(CValueNode aParentValueNode, string aName):base(aParentValueNode.ParentProgressionManager, aName)
        {
            this.ParentValueNode = aParentValueNode;
        }
        #endregion
        #region ParentParameter
        internal readonly CValueNode ParentValueNode;
        #endregion
    }
    public sealed class CNullProgression : CProgression
    {
        internal CNullProgression(CParameter aParameter) : base(aParameter, Name_Null)
        {
        }
        internal override double GetValue() => default;
    }

    public sealed class CConstant : CProgression
    {
        #region ctor
        internal CConstant(CValueNode aParentValueSource, string aName) : base(aParentValueSource, aName)
        {
        }
        #endregion
        #region Value
        internal override double GetValue() => this.Value;
        private double ValueM;
        internal double Value
        {
            get => this.ValueM;
            set
            {
                if(this.ValueM != value)
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
            get => this.Value.ToString();
            set
            {
                this.Value = double.Parse(value);
            }
        }
        public double VmValue { get => this.Value; set => this.Value = value; }
        #endregion
        #region
        internal override CValueNodeGuiEnum ValueNodeGuiEnum => CValueNodeGuiEnum.Constant;
        #endregion
    }

    internal sealed class CParameterRef : CValueNode
    {
        #region ctor
        internal CParameterRef(CProgression aProgression, string aName) :base(aProgression.ParentProgressionManager, aName)
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
            get => CLazyLoad.Get(ref this.ParameterM, () => this.ParentProgressionManager.Parameters[CParameterEnum.Zero]);
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
        #region Value
        internal override double GetValue()
            => this.VmParameter.GetValue();
        #endregion
        internal override CValueNodeGuiEnum ValueNodeGuiEnum => CValueNodeGuiEnum.ParameterRef;
    }

    internal sealed class CMappedProgression : CProgression
    {
        internal CMappedProgression(CValueNode aParentValueNode, string aName) : base(aParentValueNode, aName)
        {
            this.ControlMin = new CParameterRef(this, "ControlMin");
            this.ControlMax = new CParameterRef(this, "ControlMax");
            this.ControlValue = new CParameterRef(this, "ControlValue");
            this.MappedMin = new CConstant(this, "MappedMin");
            this.MappedMax = new CConstant(this, "MappedMax");
        }

        internal readonly CParameterRef ControlMin;
        internal readonly CParameterRef ControlMax;
        internal readonly CParameterRef ControlValue;
        internal readonly CConstant MappedMin;
        internal readonly CConstant MappedMax;
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
        internal override double GetValue()
        {
            var aControlMin = this.ControlMin.GetValue();
            var aControlMax = this.ControlMax.GetValue();
            var aControlValue = this.ControlValue.GetValue();
            var aMappedMin = this.MappedMin.GetValue();
            var aMappMaxMax = this.MappedMax.GetValue();

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
            this.ControlMin.Parameter = this.ParentProgressionManager.Parameters[CParameterEnum.Zero];
            this.ControlMax.Parameter = this.ParentProgressionManager.Parameters[CParameterEnum.FrameCount];
            this.ControlValue.Parameter = this.ParentProgressionManager.Parameters[CParameterEnum.FramePos];
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
        }


        private readonly MainWindow MainWindow;
        internal readonly CParameters Parameters;
        public CParameters VmParameters => this.Parameters;
        internal CMandelbrotState MandelbrotState => this.MainWindow.State;

        internal void OnChangeValue(CValueNode aValueNode)
        {
            if(object.ReferenceEquals(aValueNode, this.Parameters[CParameterEnum.FramePos].Constant))
            {
                if(!this.MainWindow.State.AnythingIsPending)
                {
                    this.BeginRenderEntpreller.Entprellen();
                }
            }
        }
        #region Dispatcher
        internal Dispatcher Dispatcher => this.MainWindow.Dispatcher;
        #endregion
        #region EntprellTimer
        private CEntpreller BeginRenderEntprellerM;
        private CEntpreller BeginRenderEntpreller => CLazyLoad.Get(ref this.BeginRenderEntprellerM, () => new CEntpreller(this.Dispatcher, ()=>new Action(delegate() { this.MandelbrotState.BeginRenderFrame(); })));
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
                    catch(Exception aExc)
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

    public abstract class CMandelbrotState : CViewModel
    {
        #region ctor
        private static CVec4 SizeMndFullRange => new CVec4(-2.5d, -1d, 3.5d, 2d);
        private static CVec2 CenterMndDefault => new CVec2(-1.1398974475396566d, -0.2831806035333824d);
        private static CVec4 SizeMndDefault => SizeMndFullRange.GetRectAllignedAtCenter(CenterMndDefault);
        private static double ZoomFaktorDefault(MainWindow aMainWindow)=> aMainWindow.ZoomSlider.Maximum;
        private static CVec2 CenterCurrentFktDefault => new CVec2(0.5, 0.5);
        private static double FpsDefault => 30d;
        private static BitmapSource ImageSourceDefault(CVec2 aSizePxl) => BitmapSource.Create((int)aSizePxl.Item1, (int)aSizePxl.Item2, 96, 96, PixelFormats.Rgb24, default(BitmapPalette), new byte[(int)aSizePxl.Item1 * (int)aSizePxl.Item2 * 3], (int)(3 * aSizePxl.Item1));
        private DirectoryInfo DirectoryInfoDefault => new DirectoryInfo(System.IO.Path.Combine(new FileInfo(this.GetType().Assembly.Location).Directory.FullName, "frames"));
        internal CMandelbrotState(MainWindow aMainWindow)
        {
            var aDirectoryInfo = DirectoryInfoDefault;
            var aSizeMnd = SizeMndDefault;
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
        internal CMandelbrotState (CMandelbrotState aPreviousState, CVec2 aNewCenterMnd, CSetCenterEnum aCmd) : this(aPreviousState)
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
        internal CMandelbrotState (CMandelbrotState aPreviousState, double aZoomFaktor, CZoomEnum aZoomEnum) : this(aPreviousState)
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
            if(!this.RenderMovieIsPending.Value)
            {
                throw new InvalidOperationException();
            }
        }
        private void CheckRenderFrameNotPending()
        {
            if(this.RenderFrameIsPending.Value)
            {
                throw new InvalidOperationException();
            }
        }
        private void CheckRenderMovieNotPending()
        {
            if(this.RenderMovieIsPending.Value)
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
            if(!this.RenderFrameIsPending.Value)
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
            if(!(this.MainWindow.State is object)
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
            var aImageSize = aRenderFrameInput.Item1;
            var aDx = (int)aImageSize.Item1;
            var aDy = (int)aImageSize.Item2;
            var aSizeMnd = aRenderFrameInput.Item2;
            var aCenterMnd = aSizeMnd.GetRectCenter(); // aRenderInput.Item3;
            //var aThreadCount = 1;
            var aThreadCount = 90;
            var aThreadLinesRest = aDy % aThreadCount;
            var aLinesPerThread = (aDy - aThreadLinesRest) / aThreadCount;
            var aThreadInputs = from aThreadId in Enumerable.Range(0, aThreadCount)
                                select new CRenderFrameSegmentInput(aThreadId * aLinesPerThread, (aThreadId == aThreadCount -1 && aThreadCount != 1) ? aThreadLinesRest : aLinesPerThread);
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
                 var co = 1.5f;
                 var cperiod = 1d;
                 var aDarkenThresholdLo = 0.1;
                 var aDarkenTheesholdHi = 0.3;
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

                 var aMandelPixelFunc = new Func<CVec2Int, Color>(aPixelCoord=>
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
                     var itm = 300; //after how much iterations the function should stop

                     //pick some values for the constant c, this determines the shape of the Julia Set
                     var cRe = aParametersSnapshot[CParameterEnum.JuliaConstRealPart];// -0.7d;    // real part of the constant c, determinate shape of the Julia Set
                     var cIm = aParametersSnapshot[CParameterEnum.JuliaConstImaginaryPart]; //0.27015d; // imaginary part of the constant c, determinate shape of the Julia Set
                     //cIm = 0.3d;
                     //cRe = -0.68;
                     var h = aDy;
                     var w = aDx;
                     var x = aPixelCoord.Item1;
                     var y = aPixelCoord.Item2;
                     var zoom = aParametersSnapshot[CParameterEnum.JuliaZoom]; // SizeMndDefault.Item3 / aSizeMnd.Item3;
                     var moveX = aParametersSnapshot[CParameterEnum.JuliaMoveX];
                     var moveY = aParametersSnapshot[CParameterEnum.JuliaMoveY];

                     //if(x == aDx / 2
                     //&& y == aDy / 2 )
                     //{
                     //    System.Diagnostics.Debugger.Break();
                     //}

                     var newRe = 1.5 * (x - w / 2) / (0.5 * zoom * w) + moveX;
                     var newIm = (y - h / 2) / (0.5 * zoom * h) + moveY;
                     //i will represent the number of iterations
                     var it = 0;
                     //start the iteration process
                     for (it = 0; it < itm; it++)
                     {
                         //remember value of previous iteration
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

                 //var aPixelFunc = aMandelPixelFunc;
                 var aPixelFunc = aJuliaPixelFunc;
                 foreach (var aPixelCoord in aPixelCoords)
                 {
                     var aPixelIdx = aPixelCoord.Item2 * aDx + aPixelCoord.Item1;
                     aPixels[aPixelIdx] = aPixelFunc(aPixelCoord);
                 }
             });

            var aTasks = (from aThreadInput in aThreadInputs select Task.Factory.StartNew(delegate () { aRenderPartFunc(aThreadInput); })).ToArray();

            foreach(var aTask in aTasks)
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
            var aStrideInBytes = aDx * aBytesPerPixel+ aRest;
            var aGap = new byte[aRest]; 
            var aGetLine = new Func<int, IEnumerable<byte>>(aLine => aPixelsRgb24a.Skip(aLine * aDx * aBytesPerPixel).Take(aDx* aBytesPerPixel).Concat(aGap));
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

            this.Dispatcher.BeginInvoke(new Action(delegate () {
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
        internal int FrameCount => (int)this.MainWindow.ProgressionManager.Parameters[CParameterEnum.FrameCount].GetValue();
        public int VmFrameCount => this.FrameCount;
        #endregion
        #region FrameNr
        internal int FrameNr => (int)this.MainWindow.ProgressionManager.Parameters[CParameterEnum.FramePos].GetValue();
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
            else if(this.RenderMovieCancelIsPending.Value)
            {
                this.EndCancelRenderMovie();
            }
            else if (this.FrameNr  < this.FrameCount - 1)
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
            //this.FrameNr = aPreviousState.FrameNr + 1;
            this.MainWindow.ProgressionManager.Parameters[CParameterEnum.FramePos].Constant.Value = this.MainWindow.ProgressionManager.Parameters[CParameterEnum.FramePos].Constant.Value + 1;
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
        internal CMandelbrotState(CMandelbrotState aPreviousState, CEndCancelRenderMovieEnum aCmd): this(aPreviousState)
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
        private void OnPropertyChanged(string aName)
        {
            if (this.PropertyChanged is object)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(aName));
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
                    if(this.StateM is object)
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
        private Point CenterSourcePxl =>this.ZoomCenterByMouse.HasValue
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

                if (e.Key == Key.Add)
                {
                    if (!this.State.AnythingIsPending)
                    {
                        this.State.Zoom(this.ZoomInFaktor);
                    }
                    e.Handled = true;

                }
                else if (e.Key == Key.Subtract)
                {
                    if (!this.State.AnythingIsPending)
                    {
                        this.State.Zoom(this.ZoomOutFaktor);
                    }
                    e.Handled = true;
                }
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
            catch(Exception aExc)
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
            catch(Exception aExc)
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
    }
}
