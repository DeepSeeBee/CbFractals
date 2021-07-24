using System;
using System.Collections.Generic;
using System.Text;

namespace CbFractals.ViewModel.SceneManager
{
    internal struct CTimePosition
    {
        internal CTimePosition(TimeSpan aTimeSpan) { this.TimeSpan = aTimeSpan; }
        internal CTimePosition(double aSeconds) : this(TimeSpan.FromSeconds(aSeconds)) { }
        internal readonly TimeSpan TimeSpan;
        internal CBeatPosition ToBeatPosition(double aBpm)
            => new CBeatPosition(this.TimeSpan.TotalMinutes * aBpm);
    }

    internal struct CFramePosition
    {
        internal CFramePosition(int aFramePos, double aFps)
        {
            this.FramePos = aFramePos;
            this.Fps = aFps;
        }
        internal readonly int FramePos;
        internal readonly double Fps;

        internal CTimePosition ToTimePosition()
            => new CTimePosition(((double)this.FramePos) / this.Fps);
    }

    internal struct CBeatPosition
    {
        internal CBeatPosition(double aBeats) { this.Beats = aBeats; }
        internal readonly double Beats;
        internal CTimePosition ToTimePosition(double aBpm)
            => new CTimePosition(TimeSpan.FromMinutes(aBpm / this.Beats));
    }

}
