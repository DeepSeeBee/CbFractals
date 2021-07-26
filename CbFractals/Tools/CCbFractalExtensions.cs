using CbFractals.ViewModel;
using CbFractals.ViewModel.PropertySystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CbFractals.Tools
{
    internal static class CCbFractalsExtensions
    {
        internal static CNameAttribute GetNameAttribute(this Type aType, bool aInherit = false)
            => aType.GetCustomAttributes(typeof(CNameAttribute), aInherit).Cast<CNameAttribute>().Single();
        internal static CGuidAttribute GetGuidAttribute(this Enum aEnum)
            => aEnum.GetType().GetField(aEnum.ToString()).GetCustomAttributes(typeof(CGuidAttribute), false).Cast<CGuidAttribute>().Single();

        internal static CNameAttribute GetNameAttribute(this Enum aEnum)
            => aEnum.GetType().GetField(aEnum.ToString()).GetCustomAttributes(typeof(CNameAttribute), false).Cast<CNameAttribute>().Single();
        internal static CNameEnum GetNameEnum(this Enum aEnum)
            => aEnum.GetNameAttribute().NameEnum;

        internal static T Find<T>(this IEnumerable<T> vns, CNameEnum n) where T : CValueNode
            => vns.Cast<CValueNode>().Find<T>(n); 
        internal static T Find<T>(this IEnumerable<CValueNode> vns, CNameEnum n) where T : CValueNode
            => vns.OfType<T>().Where(vn => vn.NameEnum == n).Single();


        internal static void SetConst<T>(this CParameter aParameter, T v, bool aSelect = false)
        {
            aParameter.Constant.As<CConstant<T>>().Value = v;
            if (aSelect)
            {
                aParameter.Progression = aParameter.Constant;
            }
        }
        internal static void SetConst<T>(this CParameter aParameter, T aValue, T aMin, T aMax, bool aSelect = false)
        {
            aParameter.SetConst(aValue, aSelect);
            aParameter.Min.As<CConstant<T>>().Value = aMin;
            aParameter.MaxConstant.As<CConstant<T>>().Value = aMax;
        }

        internal static void SetFunc(this CParameter aParameter, CNameEnum aFuncName, bool aSelect = false)
        {
            aParameter.FuncProgression.Func = aParameter.GetFunc(CNameEnum.Func_Oscillator);
            if (aSelect)
            {
                aParameter.Progression = aParameter.FuncProgression;
            }
        }

        internal static void SetParameterRef(this CParameter aThis, CParameter aParameter, bool aSelect = false)
        {
            aThis.ParameterRefProgression.ParameterRef.Parameter = aParameter;
            if (aSelect)
            {
                aThis.Progression = aThis.ParameterRefProgression;
            }
        }

        internal static void SetParameterRef(this CParameter aThis, CParameterEnum e, bool aSelect = false)
            => aThis.SetParameterRef(aThis.ParentProgressionManager.Parameters[e], aSelect);

        internal static void SetMappedProgression(this CParameter aThis, double aMappedMin, double aMappedMax)
        {
            var aMappedProgression = aThis.MappedProgression;
            aMappedProgression.MappedMin.Value = aMappedMin;
            aMappedProgression.MappedMax.Value = aMappedMax;
        }



        internal static CFunc GetFunc(this CParameter aParameter, CNameEnum aFuncName)
            => aParameter.FuncProgression.Funcs.Find<CFunc>(aFuncName);


        internal static CParameter GetParam(this CParameter aParameter, CNameEnum aFuncName, CNameEnum aFuncParam)
            => aParameter.GetFunc(aFuncName).FuncParameters.FuncParameters.Find<CFuncParameter>(aFuncParam).Parameter;
    }

}
