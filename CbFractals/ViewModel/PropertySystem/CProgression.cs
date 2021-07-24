using CbFractals.Tools;
using CbFractals.ViewModel.SceneManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CbFractals.ViewModel.PropertySystem
{
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

    internal sealed class CInputParameter : CValueNode
    {
        #region ctor
        internal CInputParameter(CValueNode aParentValueNode, CNameEnum aName) : base(aParentValueNode.ParentProgressionManager, aName)
        {

        }
        #endregion
        #region ParameterRef
        private CParameterRef ParameterRefM;
        internal CParameterRef ParameterRef => CLazyLoad.Get(ref this.ParameterRefM, () => new CParameterRef(this, CNameEnum.ParameterRef));
        public CParameterRef VmParameterRef => this.ParameterRef;
        internal override IEnumerable<CValueNode> SubValueNodes 
        {
            get
            {
                foreach (var aValueSource in base.SubValueNodes)
                    yield return aValueSource;
                yield return this.ParameterRef;
            }
        }
        #endregion        
        internal override object GetTypelessValue() => this.ParameterRef.GetTypelessValue();
        internal override CValueNodeGuiEnum ValueNodeGuiEnum => CValueNodeGuiEnum.InputParameter;
    }

    internal sealed class CInputParameters : CValueNode
    {
        internal CInputParameters(CValueNode aParentValueNode, CNameEnum aName, params CNameEnum[] aNames) : base(aParentValueNode.ParentProgressionManager, aName)
        {
            this.InputParameters = aNames.Select(n => new CInputParameter(this, n)).ToArray();
        }
        internal object[] GetParamArray()
            => this.InputParameters.Select(p => p.GetTypelessValue()).ToArray();
        internal override object GetTypelessValue()
            => this.GetParamArray();

        internal readonly CInputParameter[] InputParameters;
        internal override IEnumerable<CValueNode> SubValueNodes => this.InputParameters;
        internal override IEnumerable<CValueNode> ValueSources => this.InputParameters;
        internal override CValueNodeGuiEnum ValueNodeGuiEnum => CValueNodeGuiEnum.ValueSources;
        internal override void Build()
        {
            base.Build();
        }
    }

    internal abstract class CFunc : CValueNode
    {
        #region ctor
        internal CFunc(CValueNode aParentValueNode, CNameEnum aName) : base(aParentValueNode.ParentProgressionManager, aName)
        {
        }
        #endregion
        #region InputParameters
        internal abstract CNameEnum[] InputParameterNames { get; }
        private CInputParameters InputParametersM;
        internal CInputParameters InputParameters => CLazyLoad.Get(ref this.InputParametersM, () => new CInputParameters(this, CNameEnum.InputParameters, this.InputParameterNames));
        public CInputParameters VmInputParameters => this.InputParameters;
        internal override IEnumerable<CValueNode> SubValueNodes => base.SubValueNodes.Concat(new CValueNode[] { this.InputParameters });
        #endregion
        #region Invoke
        internal override object GetTypelessValue()
            => this.Invoke(this.InputParameters.GetParamArray());

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
                             CNameEnum[] aInputParameterNames
                             ): base(aParentValueNode, aName)
        {
            this.InputParameterNamesM = aInputParameterNames;
            this.FuncImplementation = aFuncImpl;
        }
        internal static CNativeFunc[] New(CValueNode aParentValueNode)
        {
            var aFuncs = new CNativeFunc[]
            {
                new CNativeFunc(aParentValueNode, 
                                CNameEnum.Func_SecondsToBeatCount, 
                                new CFuncImplementation(SecondsToBeatCount),
                                new CNameEnum[]{ CNameEnum.Func_SecondsToBeatCount_In_Seconds }
                                ),
                new CNativeFunc(aParentValueNode,
                                CNameEnum.Func_SecondsToFrameCount,
                                new CFuncImplementation(SecondsToFrameCount),
                                new CNameEnum[]{ CNameEnum.Func_SecondsToFrameCount_In_Seconds }
                                ),
            };
            return aFuncs;
        }
        #endregion
        #region InputParameters
        private readonly CNameEnum[] InputParameterNamesM;
        internal override CNameEnum[] InputParameterNames => this.InputParameterNamesM;
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
