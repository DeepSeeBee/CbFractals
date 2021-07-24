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

            this.UpdateCurrentValues();

            this.RenderFrameOnChangeValueIsEnabled = true;
        }


        private readonly MainWindow MainWindow;
        internal readonly CParameters Parameters;
        public CParameters VmParameters => this.Parameters;
        internal CMandelbrotState MandelbrotState => this.MainWindow.State;
        private bool RenderFrameOnChangeValueIsEnabled;
        internal void OnChangeValue(CValueNode aValueNode)
        {
            this.UpdateCurrentValues();
            this.OnChangeRenderFrameOnDemand();
        }

        private void UpdateCurrentValues()
        {
            foreach (var aParameter in this.Parameters.Parameters)
            {
                aParameter.UpdateCurrentValue();
            }
        }

        internal void DisableRenderFrame(Action aAction)
        {
            var aOldValue = this.RenderFrameOnChangeValueIsEnabled;
            this.RenderFrameOnChangeValueIsEnabled = false;
            try
            {
                aAction();
            }
            finally
            {
                this.RenderFrameOnChangeValueIsEnabled = aOldValue;
            }
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
        #region Render
        internal void BeginRender()
        {
            this.DisableRenderFrame(delegate ()
            {
                this.MainWindow.ProgressionManager.Parameters.SetFrameIndexByConst(0);

            });
        }
        internal void EndRender()
        {
            this.MainWindow.ProgressionManager.Parameters.SetFrameIndexBySecondIndex();
        }
        #endregion


    }
}
