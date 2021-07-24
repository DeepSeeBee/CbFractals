using System;
using System.Collections.Generic;
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

}
