using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;

namespace TEArts.Networking.AsyncSocketer
{
    public class EventPool
    {
        private Pooler<EventArgObject> mbrPooler;
        public string PoolerIdentity { get; set; }
        public EventPool(int size)
        {
            mbrPooler = new Pooler<EventArgObject>(size, 0, size);
        }
        public int NextTokenID
        {
            get { return mbrPooler.NextIndex; }
        }
        public SocketAsyncEventArgs Pop(SocketConfigure config)
        {
            EventArgObject o = mbrPooler.Popup();
            while (o == null)
            {
                Console.WriteLine("Pooler is empty");
                o = mbrPooler.Popup();
            }
            o.SocketEventArgs.RemoteEndPoint = config.SocketPoint;
            return o.SocketEventArgs;
        }
        public int Push(SocketAsyncEventArgs e)
        {
            EventArgObject o = new EventArgObject(e, mbrPooler.NextIndex);
            return mbrPooler.Pushin(o);
        }
        public void ForceClose()
        {
            mbrPooler.AbortWait();
        }
        public int Count { get { return mbrPooler.CurrentSize; } }
    }
    internal class EventArgObject : IDentity
    {
        #region IDentity Members

        public int IDentity
        {
            get { return mbrIDentity; }
        }

        #endregion
        private int mbrIDentity;
        public SocketAsyncEventArgs SocketEventArgs { get; private set; }
        public EventArgObject(SocketAsyncEventArgs e, int id)
        {
            mbrIDentity = id;
            SocketEventArgs = e;
            (SocketEventArgs.UserToken as EventToken).EventID = id;
        }
    }
}
