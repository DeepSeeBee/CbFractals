using CbFractals.ViewModel.PropertySystem;
using System;
using System.Collections.Generic;
using System.Text;

namespace CbFractals.ViewModel.Render.PropertySystem
{
    internal sealed class CPixelAlgorithmEnumConstant : CEnumConstant<CPixelAlgorithmEnum>
    {
        public CPixelAlgorithmEnumConstant(CValueNode aParentValueSource, CNameEnum aName) : base(aParentValueSource, aName)
        {
        }
    }
    internal sealed class CPixelAlgorithmEnumParameter : CEnumParameter<CPixelAlgorithmEnum>
    {
        public CPixelAlgorithmEnumParameter(CParameters aParentParameters, CNameEnum aNameEnum) : base(aParentParameters, aNameEnum)
        {
        }
        internal override CConstant NewConstant(CValueNode aParentValueNode, CNameEnum aName)
            => new CPixelAlgorithmEnumConstant(aParentValueNode, aName);
    }

}
