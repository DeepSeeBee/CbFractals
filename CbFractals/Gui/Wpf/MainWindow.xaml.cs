using CbFractals.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;
using CbFractals.ViewModel.PropertySystem;
using CbFractals.ViewModel.SceneManager;
using CbFractals.ViewModel.Mandelbrot;
using CbFractals.ViewModel.MandelModel;

namespace CbFractals.Gui.Wpf
{
    using CVec2 = Tuple<double, double>;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region ctor
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            CFullMandelModel.CalcMemoryCosts();

            new CMainWindowLoadedState(this).OnMainWindowLoaded();
        }
        #endregion
        #region ZoomSlider
        private double? ZoomSliderValueM;
        internal double ZoomSliderValue
        {
            get => CLazyLoad.Get(ref this.ZoomSliderValueM, () => this.ZoomSlider.Maximum);
            set
            {
                this.ZoomSliderValueM = value;
                this.State.UpdateFrameCountProposal();
                this.OnPropertyChanged(nameof(this.VmZoomSliderValue));
            }
        }
        internal double ZoomInFaktor => this.ZoomSliderValue;
        internal double ZoomOutFaktor => 2 - this.ZoomSliderValue;
        public double VmZoomSliderValue { get => this.ZoomSliderValue; set => this.ZoomSliderValue = value; }
        internal bool ZoomSliderIsEnabled { get => !this.State.AnythingIsPending; }
        public bool VmZoomSliderIsEnabled => this.ZoomSliderIsEnabled;
        #endregion
        #region PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string aPropertyName)
        {
            if (this.PropertyChanged is object)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(aPropertyName));
            }
        }
        #endregion
        #region MandelbrotState
        private CMandelbrotState StateM;
        internal CMandelbrotState State
        {
            get => this.StateM; 
            set
            {
                if (!object.ReferenceEquals(this.StateM, value))
                {
                    if (this.StateM is object)
                    {
                        var aOldState = this.StateM;
                        this.StateM = default;
                        aOldState.OnLeft();
                    }
                    this.StateM = value;
                    this.OnPropertyChanged(nameof(this.VmState));
                    this.OnPropertyChanged(nameof(this.VmCenterSourceEllipsePos));
                    this.OnPropertyChanged(nameof(this.VmCenterSourceMnd));
                    this.OnPropertyChanged(nameof(this.VmCenterSourcePxlRounded));
                    this.OnPropertyChanged(nameof(this.VmZoomSliderIsEnabled));
                    value.OnEntered();
                }

            }
        }
        public CMandelbrotState VmState => this.State;
        #endregion
        #region States
        private readonly ObservableCollection<CMandelbrotState> StatesM = new ObservableCollection<CMandelbrotState>();
        internal ObservableCollection<CMandelbrotState> States => this.StatesM;
        public ObservableCollection<CMandelbrotState> VmStates => this.States;
        #endregion
        #region ZoomCenter
        private Point CenterSourcePxl => this.ZoomCenterByMouse.HasValue
                                ? this.ZoomCenterByMouse.Value
                                : this.State.CenterPxl.ToPoint();
        public object VmCenterSourcePxlRounded => this.CenterSourcePxl.ToVec2().Round().ToPoint();
        private Point? ZoomCenterByMouseM;
        private Point? ZoomCenterByMouse
        {
            get => this.ZoomCenterByMouseM;
            set
            {
                this.ZoomCenterByMouseM = value;
                this.OnPropertyChanged(nameof(this.VmCenterSourceEllipsePos));
                this.OnPropertyChanged(nameof(this.VmCenterSourceMnd));
                this.OnPropertyChanged(nameof(this.VmCenterSourcePxlRounded));
            }
        }
        #endregion
        #region ZoomEllipsePos
        public Point CenterSourceEllipsePos => this.ElipseCenterToElipsePos(this.CenterSourcePxl.ToVec2(), this.CenterEllipse);
        public Point VmCenterSourceEllipsePos => this.CenterSourceEllipsePos;
        internal Point CenterSourceMnd => this.State.PixelToMandel(this.CenterSourcePxl.ToVec2()).ToPoint();
        public Point VmCenterSourceMnd => this.CenterSourceMnd;


        internal Point ElipseCenterToElipsePos(CVec2 v, Ellipse aEllipse)
            => new Point(v.Item1 - aEllipse.Width / 2d,
                        v.Item2 - aEllipse.Height / 2d);

        #endregion
        #region MouseMove
        private void MoveCenter(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed
            && this.Canvas.IsMouseOver
            && !this.State.AnythingIsPending)
            {
                this.ZoomCenterByMouse = e.GetPosition(this.Canvas);
            }
        }

        private void OnImageMouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                this.MoveCenter(e);
            }
            catch (Exception aExc)
            {
                aExc.CatchUnexpected(this);
            }
        }
        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseUp(e);

            try
            {
                if (this.ZoomCenterByMouse.HasValue)
                {
                    var aCenterPxl = this.ZoomCenterByMouse.Value.ToVec2();
                    var aCenterMnd = this.State.PixelToMandel(aCenterPxl);
                    this.ZoomCenterByMouse = default;
                    this.State.Move(aCenterMnd);
                }
            }
            catch (Exception aExc)
            {
                aExc.CatchUnexpected(this);
            }
        }
        private void OnImageMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                this.MoveCenter(e);
            }
            catch (Exception aExc)
            {
                aExc.CatchUnexpected(this);
            }
        }
        #endregion
        #region Zoom
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            try
            {

                //if (e.Key == Key.Add)
                //{
                //    if (!this.State.AnythingIsPending)
                //    {
                //        this.State.Zoom(this.ZoomInFaktor);
                //    }
                //    e.Handled = true;

                //}
                //else if (e.Key == Key.Subtract)
                //{
                //    if (!this.State.AnythingIsPending)
                //    {
                //        this.State.Zoom(this.ZoomOutFaktor);
                //    }
                //    e.Handled = true;
                //}
            }
            catch (Exception aExc)
            {
                aExc.CatchUnexpected(this);
            }
        }
        #endregion
        #region RenderMovie
        private void OnBeginRenderMovieButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                this.State.BeginRenderMovie();
            }
            catch (Exception aExc)
            {
                aExc.CatchUnexpected(this);
            }
        }

        private void OnRenderBatchCancelButtonClick(object sender, RoutedEventArgs e)
        {
            this.State.BeginCancelRenderMovie();
        }
        #endregion
        #region Reset
        private void Reset()
        {
            this.State.Reset();
        }
        private void ResetButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Reset();
            }
            catch (Exception aExc)
            {
                aExc.CatchUnexpected(this);
            }
        }
        #endregion
        #region ProgressionManager
        private CProgressionManager ProgressionManagerM;
        internal CProgressionManager ProgressionManager => CLazyLoad.Get(ref this.ProgressionManagerM, () => new CProgressionManager(this));
        public CProgressionManager VmProgressionManager => this.ProgressionManager;
        #endregion

        private void SaveProgression(object sender, RoutedEventArgs e)
        {
            try
            {
                this.ProgressionManager.Parameters.SaveAsDefault();
            }
            catch(Exception aExc)
            {
                aExc.CatchUnexpected(this);
            }
        }
    }
}
