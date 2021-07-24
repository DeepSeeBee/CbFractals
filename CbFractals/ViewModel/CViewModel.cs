using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace CbFractals.ViewModel
{
    public abstract class CViewModel : INotifyPropertyChanged
    {
        #region PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string aPropertyName)
        {
            if (this.PropertyChanged is object)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(aPropertyName));
            }
        }
        #endregion
    }

}
