using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Threading;

namespace CbFractals.Tools
{
    internal sealed class CEntpreller
    {
        internal CEntpreller(Dispatcher aDispatcher, Func<Action> aNewAction)
        {
            this.Dispatcher = aDispatcher;
            this.NewAction = aNewAction;
        }
        private readonly Func<Action> NewAction;
        private readonly Dispatcher Dispatcher;
        private DispatcherTimer EntprellTimerM;
        private DispatcherTimer EntprellTimer => CLazyLoad.Get(ref this.EntprellTimerM, this.NewEntprellTimer);
        private DispatcherTimer NewEntprellTimer()
        {
            var aTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(500), DispatcherPriority.Normal, new EventHandler(this.OnEntprellTimerElapsed), this.Dispatcher);
            return aTimer;
        }
        private void OnEntprellTimerElapsed(object aSender, EventArgs aArgs)
        {
            this.EntprellTimer.Stop();
            this.EntprellTimer.IsEnabled = false;
        }
        private void StartTimer()
        {
            this.EntprellTimer.Stop();
            this.EntprellTimer.Start();
            this.EntprellTimer.IsEnabled = true;
        }
        private Action EntprellAction;
        public void Entprellen()
        {
            this.Entprellen(this.NewAction());
        }
        public void Entprellen(Action aAction)
        {
            var aTimer = this.EntprellTimer;
            var aHandler = default(EventHandler);
            aHandler = new EventHandler(delegate (object aSender, EventArgs aArgs)
            {
                if (object.ReferenceEquals(this.EntprellAction, aAction))
                {
                    aTimer.Tick -= aHandler;
                    try
                    {
                        aAction();
                    }
                    catch (Exception aExc)
                    {
                        aExc.CatchUnexpected(this);
                    }
                }
            });
            aTimer.Tick += aHandler;
            this.EntprellAction = aAction;
            this.StartTimer();
        }
    }

}
