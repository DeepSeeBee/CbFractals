using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CbFractals.Tools
{

    internal abstract class CSink
    {
        internal virtual void Flush() { }
    }
    internal abstract class CSink<T> : CSink
    {
        internal abstract void Write(T aData);        
    }

    internal sealed class CNullSink<T> : CSink<T>
    {
        internal CNullSink() { }
        internal override void Write(T aData) {}
    }

    internal sealed class CActionSink<T> : CSink<T>
    {
        internal CActionSink(Action<T> aAction)
        {
            this.InnerSink = aAction;
        }
        private readonly Action<T> InnerSink;
        internal override void Write(T aData)
            => this.InnerSink(aData);
    }


    internal sealed class CForkSink<T> : CSink<T>
    {
        internal CForkSink(CSink<T>[] aForks)
        {
            this.Forks = aForks;
        }

        private readonly CSink<T>[] Forks;
        internal override void Write(T aData)
        {
            foreach (var aFork in this.Forks)
                aFork.Write(aData);
        }
    }


    internal sealed class CBufferSink<T> : CSink<T>
    {
        internal CBufferSink(CSink<T> aInnerSink = null)
        {
            this.InnerSink = aInnerSink;
        }
        internal T Buffer { get; private set; }
        private readonly CSink<T> InnerSink;
        internal override void Write(T aData)
            => this.Buffer = aData;
        internal override void Flush()
            => this.InnerSink.Write(this.Buffer);
    }

    internal sealed class CArrayFragmentBufferedSink<T> : CSink<T>
    {
        internal CArrayFragmentBufferedSink(T[] aBuffer, int aOffset, int aCount, CSink<T[]> aInnerSink = null)
        {
            this.Buffer = aBuffer;
            this.ArrayFragment = new CArrayFragment<T>(aBuffer, aOffset, aCount);
            this.InnerSink = aInnerSink;
        }
        private readonly T[] Buffer;
        private readonly CArrayFragment<T> ArrayFragment;
        private int Pos = 0;
        private readonly CSink<T[]> InnerSink;
        internal override void Write(T aData)
        {
            this.ArrayFragment[this.Pos] = aData;
            ++this.Pos;
        }
        internal override void Flush()
        {
            this.InnerSink.Write(this.Buffer);
        }
    }
}
