using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace AsyncSocketer
{
    public interface IDentity
    {
        int IDentity { get; }
    }
    public class Pooler<TEArtType> where TEArtType : IDentity
    {
        private Queue<TEArtType> mbrPooler;
        private AutoResetEvent mbrEmptyLocker;
        private int mbrIndexer;
        private int mbrStarter;
        private int mbrMaxpean;
        private bool mbrForAbort;
        public TEArtType Popup()
        {
            if (mbrPooler.Count == 0)
            {
                mbrEmptyLocker.Reset();
                mbrEmptyLocker.WaitOne();
                if (mbrForAbort)
                {
                    Thread.CurrentThread.Abort();
                }
            }
            return mbrPooler.Dequeue();
        }
        public int Pushin(TEArtType tt)
        {
            mbrPooler.Enqueue(tt);
            if (mbrPooler.Count >= 1)
            {
                mbrEmptyLocker.Set();
            }
            return tt.IDentity;
        }
        public void AbortWait()
        {
            mbrForAbort = true;
            mbrEmptyLocker.Set();
        }
        public int NextIndex
        {
            get
            {
                Interlocked.CompareExchange(ref mbrIndexer, mbrStarter, mbrMaxpean);
                return Interlocked.Increment(ref mbrIndexer);
            }
        }
        public int CurrentSize { get { return mbrPooler.Count; } }
        public Pooler(int size, int index, int max)
        {
            mbrPooler = new Queue<TEArtType>(size);
            mbrIndexer = index;
            mbrMaxpean = max;
            mbrStarter = index;
            mbrEmptyLocker = new AutoResetEvent(false);
        }
        public Pooler(int size, int index) : this(size, index, int.MaxValue) { }
        public Pooler(int size) : this(size, int.MinValue + 1, int.MaxValue) { }
    }
}
