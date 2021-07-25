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
            => vns.Where(vn => vn.NameEnum == n).Single();
    }

}
