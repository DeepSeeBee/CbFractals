using System;
using System.Collections.Generic;
using System.Text;

namespace CbFractals.ViewModel.SceneManager
{
    internal struct CTimePosition
    {
        internal CTimePosition(TimeSpan aTimeSpan) { this.TimeSpan = aTimeSpan; }
        internal static CTimePosition FromSeconds(double aSeconds) => new CTimePosition(TimeSpan.FromSeconds(aSeconds));
        internal readonly TimeSpan TimeSpan;
        internal CBeatPosition ToBeatPosition(double aBpm)
            => new CBeatPosition(this.TimeSpan.TotalMinutes * aBpm);
        internal CFramePosition ToFramePosition(double aFps)
            => new CFramePosition(this.TimeSpan.TotalSeconds * aFps);
    }

    internal struct CFramePosition
    {
        internal CFramePosition(double aFramePos)
        {
            this.FramePos = aFramePos;
        }
        internal readonly double FramePos;
        internal CTimePosition ToTimePosition(double aFps)
            => CTimePosition.FromSeconds(((double)this.FramePos) / aFps);
        internal Int64 FrameIdx => (Int64) Math.Floor(this.FramePos);
    }

    internal struct CBeatPosition
    {
        internal CBeatPosition(double aBeats) { this.Beats = aBeats; }
        internal readonly double Beats;
        internal CTimePosition ToTimePosition(double aBpm)
            => new CTimePosition(TimeSpan.FromMinutes(aBpm / this.Beats));
    }

}
