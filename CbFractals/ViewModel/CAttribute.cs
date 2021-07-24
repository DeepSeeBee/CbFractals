using System;
using System.Collections.Generic;
using System.Text;

namespace CbFractals.ViewModel
{
    public class CGuidAttribute : Attribute
    {
        public CGuidAttribute(string aGuid)
        {
            this.Guid = Guid.Parse(aGuid);
        }
        internal readonly Guid Guid;
    }

}
