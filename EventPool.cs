using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;

namespace AsyncSocketer
{
    class EventPool
    {
        public Queue<SocketAsyncEventArgs> Pooler { get; private set; }
        private int TokenIndex;
        private bool mbrForceClose;
        private AutoResetEvent PoolerLocker { get; set; }
        //public int PoolerSize { get; private set; }
        public EventPool(int size)
        {
            TokenIndex = 0;
            Pooler = new Queue<SocketAsyncEventArgs>(size);
            PoolerLocker = new AutoResetEvent(false);
        }
        public int NextTokenID
        {
            get
            {
                return Interlocked.Increment(ref TokenIndex);
            }
        }
        public SocketAsyncEventArgs Pop(SocketConfigure config)
        {
            lock (Pooler)
            {
                if (Pooler.Count == 0)
                {
#if DEBUG
                    System.Console.WriteLine("Pooler is empty,WaitOne");
#endif
                    PoolerLocker.WaitOne();
                    if (mbrForceClose)
                    {
                        return null;
                    }
                    PoolerLocker.Reset();
                }
                SocketAsyncEventArgs e = Pooler.Dequeue();
                e.RemoteEndPoint = config.RemotePoint;
                return e;
            }
        }
        public bool Push(SocketAsyncEventArgs e)
        {
            if (e == null)
            {
                return false;
            }
            Pooler.Enqueue(e);
            if (Pooler.Count == 1)
            {
                PoolerLocker.Set();
            }
            return true;
        }
        internal void ForceClose()
        {
            mbrForceClose = true;
            PoolerLocker.Set();
        }
    }
}
