using System;
using System.Collections.Generic;
using System.Text;

namespace CbFractals.Tools
{
    internal sealed class CArrayFragment<T>
    {
        internal CArrayFragment(T[] aSourceArray, int aOffset, int aCount)
        {
            this.SourceArray = aSourceArray;
            this.Offset = aOffset;
            this.Count = aCount;
        }
        internal readonly T[] SourceArray;
        internal readonly int Offset;
        internal readonly int Count;
        private int CheckIndex(int i)
            => (i < (this.Offset + this.Count)) ? i : throw new ArgumentException();
        private int GetIndex(int i)
            => this.CheckIndex(i + this.Offset);

        internal T this[int i]
        {
            get => this.SourceArray[this.GetIndex(i)];
            set => this.SourceArray[this.GetIndex(i)] = value;
        }
    }
}
