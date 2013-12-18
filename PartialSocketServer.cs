using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace AsyncSocketer
{
    public class PartialSocketServer : PartialSocket
    {
        protected object evtBeginAccept, evtAccepted;
        public event SocketEventHandler BeginAccept { add { base.Events.AddHandler(evtBeginAccept, value); } remove { base.Events.RemoveHandler(evtBeginAccept, value); } }
        public event ServerSocketEventHandler Accepted { add { base.Events.AddHandler(evtAccepted, value); } remove { base.Events.RemoveHandler(evtAccepted, value); } }
        protected virtual void OnBeginAccept(SocketAsyncEventArgs e)
        {
            SocketEventArgs a = new SocketEventArgs(e);
            fireEvent(evtBeginAccept, a);
        }
        protected virtual void OnAccepted(SocketAsyncEventArgs e)
        {
            ServerSocketEventArgs a = new ServerSocketEventArgs(e);
            fireEvent(evtAccepted, a);
        }
        private EventPool mbrAcceptEventer;
        private BufferManager mbrAcceptBuffer;
        protected SocketAsyncEventArgs GetAcceptAsyncEvent()
        {
            if (mbrAcceptEventer == null)
            {
                mbrAcceptEventer = new EventPool(Config.MaxDataConnection);
                for (int i = 0; i < Config.MaxDataConnection; i++)
                {
                    SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                    e.RemoteEndPoint = Config.RemotePoint;
                    e.UserToken = new EventToken(mbrAcceptEventer.NextTokenID, Config);
                    e.Completed += (o, x) =>
                        {
                            if (x.SocketError != SocketError.Success)
                            {
                                OnError(x);
                                if (!Config.OnErrorContinue)
                                {
                                    return;
                                }
                            }
                            else
                            {
                                OnAccepted(x);
                            }
                            GetAcceptBuffer().FreeBuffer(x);
                            mbrAcceptEventer.Push(x);
                        };
                    GetAcceptBuffer().SetBuffer(e);
                    GetAcceptBuffer().SetBuffer(e, 6);
                    if (!mbrAcceptEventer.Push(e))
                    {
                        SocketErrorArgs a = new SocketErrorArgs(e);
                        a.Exception = null;
                        a.Message = "Initialize Accept pooler failed.";
                        a.Operation = SocketAsyncOperation.None;
                        fireEvent(evtError, a);
                        if (!Config.OnErrorContinue)
                        {
                            return null;
                        }
                    }
                }
            }
            return mbrAcceptEventer.Pop(Config);
        }
        protected BufferManager GetAcceptBuffer()
        {
            if (mbrAcceptBuffer == null)
            {
                mbrAcceptBuffer = new BufferManager(Config.MaxDataConnection);
            }
            return mbrAcceptBuffer;
        }
        private PartialSocketServer()
        {
            evtBeginAccept = new object();
            evtAccepted = new object();
        }
        public PartialSocketServer(SocketConfigure sc)
            : base(sc)
        {
        }
        public bool Start()
        {
            try
            {
                ClientSocket.Bind(Config.RemotePoint);
                ClientSocket.Listen(Config.MaxDataConnection);
                SocketAsyncEventArgs e = Accept();
                fireEvent(evtBeginAccept, e);
                return true;
            }
            catch (Exception c)
            {
                SocketErrorArgs a = new SocketErrorArgs();
                a.Exception = c;
                a.Message = "On Begin Accept Client Connect Error";
                a.Operation = SocketAsyncOperation.Accept;
                fireEvent(evtError, a);
            }
            return false;
        }
        private SocketAsyncEventArgs Accept()
        {
            SocketAsyncEventArgs e = GetAcceptAsyncEvent();
            if (!ClientSocket.AcceptAsync(e))
            {
                OnAccepted(e);
            }
            return e;
        }
        protected override void fireEvent(object evt, object e)
        {
            if (evt == evtAccepted)
            {
                object o = base.Events[evtAccepted];
                if (o != null)
                {
                    (o as ServerSocketEventHandler)(this, (e as ServerSocketEventArgs));
                }
            }
            else
            {
                base.fireEvent(evt, e);
            }
        }
    }
}
