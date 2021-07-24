using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace CbFractals.Tools
{
    internal static class CExceptionExtensions
    {
        internal static void CatchUnexpected(this Exception e, object aCatcher)
        {
            System.Windows.MessageBox.Show(e.Message, aCatcher.ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
