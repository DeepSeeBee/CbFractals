using CbFractals.Gui.Wpf;
using CbFractals.Tools;
using CbFractals.ViewModel.Mandelbrot;
using CbFractals.ViewModel.PropertySystem;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Threading;

namespace CbFractals.ViewModel.SceneManager
{
    public sealed class CProgressionManager : CViewModel
    {
        internal CProgressionManager(MainWindow aMainWindow)
        {
            this.MainWindow = aMainWindow;
            this.Parameters = new CParameters(this);

            this.Parameters.Build();

            this.RenderFrameOnChangeValueIsEnabled = true;
        }


        private readonly MainWindow MainWindow;
        internal readonly CParameters Parameters;
        public CParameters VmParameters => this.Parameters;
        internal CMandelbrotState MandelbrotState => this.MainWindow.State;
        private bool RenderFrameOnChangeValueIsEnabled;
        internal void OnChangeValue(CValueNode aValueNode)
        {
            //if(object.ReferenceEquals(aValueNode, this.Parameters[CParameterEnum.FramePos].Constant))
            this.OnChangeRenderFrameOnDemand();
        }

        internal void OnChangeRenderFrameOnDemand()
        {
            if (this.RenderFrameOnChangeValueIsEnabled
            && !this.MainWindow.State.AnythingIsPending)
            {
                this.BeginRenderEntpreller.Entprellen();
            }
        }
        #region Dispatcher
        internal Dispatcher Dispatcher => this.MainWindow.Dispatcher;
        #endregion
        #region EntprellTimer
        private CEntpreller BeginRenderEntprellerM;
        private CEntpreller BeginRenderEntpreller => CLazyLoad.Get(ref this.BeginRenderEntprellerM, () => new CEntpreller(this.Dispatcher, () => new Action(delegate () { this.MandelbrotState.BeginRenderFrame(); })));
        #endregion
    }
}
