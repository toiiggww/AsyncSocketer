using System;
using System.Net.Sockets;

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
            o.SocketEventArgs.RemoteEndPoint = config.RemoteSocketPoint;
            return o.SocketEventArgs;
        }
        public int Push(SocketAsyncEventArgs e)
        {
            EventArgObject o = new EventArgObject(e, mbrPooler.NextIndex);
            //TEArts.Etc.CollectionLibrary.Debuger.Loger.DebugInfo(o);
            return mbrPooler.Pushin(o);
        }
        public void ForceClose()
        {
            mbrPooler.AbortWait();
        }
        public int Count { get { return mbrPooler == null ? -1 : mbrPooler.CurrentSize; } }
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
        public override string ToString()
        {
            return string.Format("EventArgObject:[{0}]-[{2}]-[{1}]", mbrIDentity, SocketEventArgs.RemoteEndPoint, SocketEventArgs.LastOperation);
        }
    }
}
