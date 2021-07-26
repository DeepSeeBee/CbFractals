using CbFractals.Tools;
using CbFractals.ViewModel.SceneManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CbFractals.ViewModel.PropertySystem
{
    using CParameterDeclaration = Tuple<CNameEnum, Type>;
    public abstract class CProgression : CValueNode
    {
        #region ctor
        internal CProgression(CValueNode aParentValueNode, CNameEnum aName) : base(aParentValueNode, aParentValueNode.ParentProgressionManager, aName)
        {
            this.ParentValueNode = aParentValueNode;
        }
        #endregion
        #region ParentParameter
        internal readonly CValueNode ParentValueNode;
        #endregion
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

    internal sealed class CParameterRefProgression :CProgression
    {
        internal CParameterRefProgression(CValueNode aParentValueNode, CNameEnum aName)  :base(aParentValueNode, aName)
        {
        }

        private CParameterRef ParameterRefM;
        internal CParameterRef ParameterRef => CLazyLoad.Get(ref this.ParameterRefM, () => new CParameterRef(this, CNameEnum.ParameterRef));
        internal override IEnumerable<CValueNode> ValueSources
        {
            get
            {
                foreach (var aValueSource in base.ValueSources)
                    yield return aValueSource;
                yield return this.ParameterRef;
            }
        }
        internal override object GetTypelessValue()
            => this.ParameterRef.GetTypelessValue();

        internal override CValueNodeGuiEnum ValueNodeGuiEnum => CValueNodeGuiEnum.ValueSources;
        internal override void Build()
        {
            base.Build();

            this.ValueSource = this.ParameterRef;
        }
        internal override void SetEditable(bool v)
        {
            base.SetEditable(v);
            this.ParameterRef.SetEditable(v);
        }
    }

    internal sealed class CFuncParameter : CValueNode
    {
        #region ctor
        internal CFuncParameter(CValueNode aParentValueNode, CParameterDeclaration aParameterDeclaration) : base(aParentValueNode, aParentValueNode.ParentProgressionManager, aParameterDeclaration.Item1)
        {
            this.ParameterType = aParameterDeclaration.Item2;
        }
        #endregion
        private readonly Type ParameterType;
        #region ParameterRef
        private CParameter NewParameter()
        {
            var aParameter = CParameter.New(this, this.ParameterType, this.NameEnum);
            aParameter.FuncProgression.SetSelectable(false);
            aParameter.SetMapToRange(false);
            return aParameter;
        }
        private CParameter ParameterM;
        internal CParameter Parameter => CLazyLoad.Get(ref this.ParameterM, () => this.NewParameter());
        public CParameter VmParameter => this.Parameter;
        internal override IEnumerable<CValueNode> SubValueNodes 
        {
            get
            {
                foreach (var aValueSource in base.SubValueNodes)
                    yield return aValueSource;
                yield return this.Parameter;
            }
        }
        #endregion        
        internal override object GetTypelessValue() => this.Parameter.GetTypelessValue();
        internal override CValueNodeGuiEnum ValueNodeGuiEnum => CValueNodeGuiEnum.FuncParameter;
    }

    internal sealed class CFuncParameters : CValueNode
    {
        internal CFuncParameters(CValueNode aParentValueNode, CNameEnum aName, params CParameterDeclaration[] aParameterDeclarations) : base(aParentValueNode, aParentValueNode.ParentProgressionManager, aName)
        {
            this.FuncParameters = aParameterDeclarations.Select(d => new CFuncParameter(this, d)).ToArray();
        }
        internal object[] GetParamArray()
            => this.FuncParameters.Select(p => p.GetTypelessValue()).ToArray();
        internal override object GetTypelessValue()
            => this.GetParamArray();

        internal readonly CFuncParameter[] FuncParameters;
        internal override IEnumerable<CValueNode> SubValueNodes => this.FuncParameters;
        internal override IEnumerable<CValueNode> ValueSources => this.FuncParameters;
        internal override CValueNodeGuiEnum ValueNodeGuiEnum => CValueNodeGuiEnum.ValueSources;
        internal override void Build()
        {
            base.Build();
        }
    }

    internal abstract class CFunc : CValueNode
    {
        #region ctor
        internal CFunc(CValueNode aParentValueNode, CNameEnum aName) : base(aParentValueNode, aParentValueNode.ParentProgressionManager, aName)
        {
        }
        #endregion
        #region InputParameters
        internal abstract Tuple<CNameEnum, Type>[] ParameterDeclarations { get; }
        private CFuncParameters InputParametersM;
        internal CFuncParameters FuncParameters => CLazyLoad.Get(ref this.InputParametersM, () => new CFuncParameters(this, CNameEnum.InputParameters, this.ParameterDeclarations));
        public CFuncParameters VmInputParameters => this.FuncParameters;
        internal override IEnumerable<CValueNode> SubValueNodes => base.SubValueNodes.Concat(new CValueNode[] { this.FuncParameters });
        #endregion
        #region Invoke
        internal override object GetTypelessValue()
            => this.Invoke(this.FuncParameters.GetParamArray());

        internal abstract object Invoke(object[] aParams);
        #endregion
        #region Gui
        internal override CValueNodeGuiEnum ValueNodeGuiEnum => CValueNodeGuiEnum.Func;
        #endregion
        internal override void Build()
        {
            base.Build();
        }
    }

    internal sealed class CNativeFunc : CFunc
    {
        #region ctor

        private CNativeFunc(CValueNode aParentValueNode, 
                             CNameEnum aName,
                             CFuncImplementation aFuncImpl,
                             CParameterDeclaration[] aParameterDeclarations,
                             CBuildNativeFuncAction aBuildFunc
                             ): base(aParentValueNode, aName)
        {
            this.ParameterDeclarationsM = aParameterDeclarations;
            this.FuncImplementation = aFuncImpl;
            this.BuildFunc = aBuildFunc;
        }

        internal override void Build()
        {
            base.Build();

            this.BuildFunc(this);
        }

        private static void BuildNull(CNativeFunc aFunc)
        {

        }
        private static void BuildOscillator(CNativeFunc aOscillatorFunc)
        {
            aOscillatorFunc.FuncParameters.SubValueNodes.Find<CFuncParameter>(CNameEnum.Func_Oscillator_In_Frequency)
                .Parameter.SetConst<double>(1d);
            aOscillatorFunc.FuncParameters.SubValueNodes.Find<CFuncParameter>(CNameEnum.Func_Oscillator_In_Time)
                .Parameter.SetParameterRef(CParameterEnum.BeatIndex, true);
        }

        internal static CNativeFunc[] New(CValueNode aParentValueNode)
        {
            var aFuncs = new CNativeFunc[]
            {
                new CNativeFunc(aParentValueNode, 
                                CNameEnum.Func_SecondsToBeatCount, 
                                new CFuncImplementation(SecondsToBeatCount),
                                new CParameterDeclaration[]{ new CParameterDeclaration(CNameEnum.Func_SecondsToBeatCount_In_Seconds, typeof(double)) },
                                new CBuildNativeFuncAction(BuildNull)
                                ),
                new CNativeFunc(aParentValueNode,
                                CNameEnum.Func_SecondsToFrameCount,
                                new CFuncImplementation(SecondsToFrameCount),
                                new CParameterDeclaration[]{ new CParameterDeclaration(CNameEnum.Func_SecondsToFrameCount_In_Seconds, typeof(double)) },
                                new CBuildNativeFuncAction(BuildNull)
                                ),
                new CNativeFunc(aParentValueNode,
                                CNameEnum.Func_Oscillator,
                                new CFuncImplementation(Oscillator),
                                new CParameterDeclaration[]
                                {
                                    new CParameterDeclaration(CNameEnum.Func_Oscillator_In_Time, typeof(double)),
                                    new CParameterDeclaration(CNameEnum.Func_Oscillator_In_Frequency, typeof(double)),
                                },
                                new CBuildNativeFuncAction(BuildOscillator)
                                ),
            };
            return aFuncs;
        }
        #endregion
        #region BuildFunc
        private delegate void CBuildNativeFuncAction(CNativeFunc aNativeFunc);

        private readonly CBuildNativeFuncAction BuildFunc;
        #endregion
        #region InputParameters
        private readonly CParameterDeclaration[] ParameterDeclarationsM;
        internal override CParameterDeclaration[] ParameterDeclarations => this.ParameterDeclarationsM;
        internal T GetParam<T>(object[] aParams, int aIdx, CNameEnum aName, Func<object, T> aConvertFunc)
            => aConvertFunc(aParams[aIdx]);
        internal double GetDoubleParam(object[] aParams, int aIdx, CNameEnum aName)
            => this.GetParam<double>(aParams, aIdx, aName, Convert.ToDouble);
        internal double GetDoubleParam(CParameterEnum p)
            => this.ParentProgressionManager.Parameters[p].GetDoubleValue();
        #endregion
        #region FuncImplementation
        private delegate object CFuncImplementation(CNativeFunc aFunc, object[] aParams);
        private readonly CFuncImplementation FuncImplementation;
        internal override object Invoke(object[] aParams)
            => this.FuncImplementation(this, aParams);
        #endregion
        #region Implementations
        private static object SecondsToBeatCount(CNativeFunc aFunc, object[] aParams)
        {
            var aSeconds = aFunc.GetDoubleParam(aParams, 0, CNameEnum.Func_SecondsToBeatCount_In_Seconds);
            var aBpm = aFunc.GetDoubleParam(CParameterEnum.BeatsPerMinute);
            var aTimePosition = CTimePosition.FromSeconds(aSeconds);
            var aBeatPosition = aTimePosition.ToBeatPosition(aBpm);
            var aBeats = aBeatPosition.Beats;
            return aBeats;
        }
        private static object SecondsToFrameCount(CNativeFunc aFunc, object[] aParams)
        {
            var aSeconds = aFunc.GetDoubleParam(aParams, 0, CNameEnum.Func_SecondsToBeatCount_In_Seconds);
            var aFps = aFunc.GetDoubleParam(CParameterEnum.FramesPerSecond);
            var aTimePosition = CTimePosition.FromSeconds(aSeconds);
            var aFramePosition = aTimePosition.ToFramePosition(aFps);
            var aFramePos = aFramePosition.FramePos;
            return aFramePos;
        }

        private static object Oscillator(CNativeFunc aFunc, object[] aParams)
        {
            var aTime = aFunc.GetDoubleParam(aParams, 0, CNameEnum.Func_Oscillator_In_Time);
            var aFrq = aFunc.GetDoubleParam(aParams, 1, CNameEnum.Func_Oscillator_In_Frequency);
            var aIn = (Math.PI * 2d * aTime / aFrq) - (Math.PI / 2d);
            var aAmplitude = Math.Sin(aIn).Map(-1d, 1d, 0d, 1d);
            return aAmplitude;
        }

        private object GetDoubleParam(object[] aParams, int v, object func_Oscillator_Time)
        {
            throw new NotImplementedException();
        }
        #endregion
    }

    internal static class CFuncFactory
    {
        internal static CFunc[] NewFuncs(CValueNode aParentValueNode)
            => CNativeFunc.New(aParentValueNode);
    }


    internal sealed class CFuncProgression  :CProgression
    {
        #region ctor
        internal CFuncProgression(CValueNode aParentValueNode, CNameEnum aName) : base(aParentValueNode, aName)
        {
        }
        #endregion
        #region Funcs
        private CFunc[] FuncsM;
        internal CFunc[] Funcs => CLazyLoad.Get(ref this.FuncsM, () => CFuncFactory.NewFuncs(this));
        internal override IEnumerable<CValueNode> SubValueNodes => this.Funcs;
        internal override IEnumerable<CValueNode> ValueSources => this.Funcs;
        internal CFunc Func { get => (CFunc)this.ValueSource; set => this.ValueSource = value; }
        #endregion
        #region Value
        internal override object GetTypelessValue()
            => this.Func.GetTypelessValue();
        #endregion
        #region Gui
        internal override CValueNodeGuiEnum ValueNodeGuiEnum => CValueNodeGuiEnum.ValueSources;
        #endregion
        internal override void Build()
        {
            base.Build();
        }
    }
}
