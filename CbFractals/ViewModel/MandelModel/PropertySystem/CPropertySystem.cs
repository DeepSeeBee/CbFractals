using CbFractals.ViewModel.PropertySystem;
using System;
using System.Collections.Generic;
using System.Text;

namespace CbFractals.ViewModel.MandelModel.PropertySystem
{
    internal sealed class CModelRenderModeEnumConstant : CEnumConstant<CModelRenderModeEnum>
    {
        public CModelRenderModeEnumConstant(CValueNode aParentValueSource, CNameEnum aName) : base(aParentValueSource, aName)
        {
        }
    }

    internal sealed class CModelRenderModeEnumParameter : CEnumParameter<CModelRenderModeEnum>
    {
        public CModelRenderModeEnumParameter(CParameters aParentParameters, CNameEnum aNameEnum) : base(aParentParameters, aNameEnum)
        {
        }
        internal override CConstant NewConstant(CValueNode aParentValueNode, CNameEnum aName)
            => new CModelRenderModeEnumConstant(aParentValueNode, aName);
    }

}
