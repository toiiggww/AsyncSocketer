using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace AsyncSocketer
{
    /// <summary>
    /// This is driverd from http://www.codeproject.com/Articles/83102/C-SocketAsyncEventArgs-High-Performance-Socket-Cod
    /// </summary>
    public abstract class EventSocketer : PerformanceBase
    {
        #region EventSystem
        protected object evtConnecting,
            evtConnected,
            evtSend,
            evtRecevie,
            evtDisconnected,
            evtBeginAccept,
            evtAccepted,
            evtError;
        public event SocketEventHandler BeginAccept { add { base.Events.AddHandler(evtBeginAccept, value); } remove { base.Events.RemoveHandler(evtBeginAccept, value); } }
        public event ServerSocketEventHandler Accepted { add { base.Events.AddHandler(evtAccepted, value); } remove { base.Events.RemoveHandler(evtAccepted, value); } }
        public event SocketEventHandler Connecting { add { base.Events.AddHandler(evtConnecting, value); } remove { base.Events.RemoveHandler(evtConnecting, value); } }
        public event SocketEventHandler AfterConnected { add { base.Events.AddHandler(evtConnected, value); } remove { base.Events.RemoveHandler(evtConnected, value); } }
        public event SocketEventHandler Sended { add { base.Events.AddHandler(evtSend, value); } remove { base.Events.RemoveHandler(evtSend, value); } }
        public event SocketEventHandler Recevied { add { base.Events.AddHandler(evtRecevie, value); } remove { base.Events.RemoveHandler(evtRecevie, value); } }
        public event SocketEventHandler Disconnected { add { base.Events.AddHandler(evtDisconnected, value); } remove { base.Events.RemoveHandler(evtDisconnected, value); } }
        public event SocketErrorHandler Error { add { base.Events.AddHandler(evtError, value); } remove { base.Events.RemoveHandler(evtError, value); } }
        protected override void fireEvent(object evt, object e)
        {
            if (evt != null)
            {
                object o = base.Events[evt];
                if (o != null)
                {
                    if (evt == evtError)
                    {
                        (o as SocketErrorHandler)(this, (e as SocketErrorArgs));
                    }
                    else
                    {
                        (o as SocketEventHandler)(this, (e as SocketEventArgs));
                    }
                }
            }
            base.fireEvent(evt, e);
        }
        #endregion
        protected EventSocketer()
        {
            evtConnecting = new object();
            evtConnected = new object();
            evtSend = new object();
            evtRecevie = new object();
            evtDisconnected = new object();
            evtError = new object();
            mbrSendThread = new Thread(new ThreadStart(Send));
            mbrReceiveThread = new Thread(new ThreadStart(Receive));
        }
        protected EventSocketer(SocketConfigure sc)
            : this()
        {
            Config = sc;
            mbrListenPoint = new IPEndPoint(Config.ListenAddress, Config.ListenPort);
            OutMessage = new MessagePool();
            IncommeMessage = new MessagePool();
            ClientSocket = CreateClientSocket();
            ClientSocket.Bind(mbrListenPoint);
        }
        public virtual void Connect(System.Net.IPAddress iPAddress, int p)
        {
            Config.RemotePoint = new IPEndPoint(iPAddress, p);
            Connect();
        }
        public virtual void Connect()
        {
            if (
                mbrSenderLocker == null || string.IsNullOrEmpty(mbrSenderLocker.Trim()) ||
                mbrReceverLocker == null || string.IsNullOrEmpty(mbrReceverLocker.Trim())
                )
            {
                Random r = new Random();
                r = new Random(r.Next() * 1000);
                ClientIdentity = string.Format("DefaultIdnetity_{0:X}", r.Next() * 10000);
            }
            fireEvent(evtConnecting, null);
            SocketAsyncEventArgs e = GetConnectEventsPooler().Pop(Config);
            if (Config.SendDataOnConnected)
            {
                MessageFragment m = OutMessage.GetMessage();
                (e.UserToken as EventToken).MessageID = m.IDentity;
                try
                {
                    e.SetBuffer(e.Offset, m.Buffer.Length);
                    Buffer.BlockCopy(m.Buffer, 0, e.Buffer, e.Offset, m.Buffer.Length);
                }
                catch
                {
                    string s = string.Format("Cache size {0} not enough for {1}, connect {2}", e.Count, m.Buffer.Length, (Config.OnErrorContinue ? "Continued" : "Droped"));
                    if (!Config.OnErrorContinue)
                    {
                        throw new Exception(s);
                    }
                    else
                    {
                        e.SetBuffer(e.Offset, e.Count);
                        Buffer.BlockCopy(m.Buffer, 0, e.Buffer, e.Offset, e.Count);
                        DebugInfo(s);
                    }
                }
            }
            if (!ClientSocket.Connect(e))
            {
                OnConnected(e);
            }
        }
        public virtual void Disconnect()
        {
            if (mbrWaitForDisconnect)
            {
                SocketAsyncEventArgs e = GetDisconnectEventsPooler().Pop(Config);
                if (!ClientSocket.Disconnect(e))
                {
                    OnDisconnected(e);
                }
            }
            OutMessage.ForceClose();
            IncommeMessage.ForceClose();
            ResetConnectAsyncEvents();
            ResetDisconnectAsyncEvents();
            ResetReceiveAsyncEvents();
            ResetSendAsyncEvents();
        }
        public virtual void ReConnect()
        {
            Disconnect();
            Connect();
        }
        public virtual void Accetp() { }
        protected virtual void Receive()
        {
            Monitor.Enter(mbrReceverLocker);
            int lostCount = 1024;
            while (mbrWaitForDisconnect)
            {
                if (ClientSocket.Available > 0)
                {
                    if (lostCount != 1024)
                    {
                        lostCount = 1024;
                    }
                    SocketAsyncEventArgs e = GetReceiveEventsPooler().Pop(Config);
                    if (!mbrWaitForDisconnect)
                    {
                        break;
                    }
                    if (!ClientSocket.Receive(e))
                    {
                        OnReceived(e);
                    }
                    //DebugInfo(string.Format("[{0}] _ Wait for Receive Compleate", e.UserToken));
                }
                while (ClientSocket.SocketUnAvailable && lostCount > 0)
                {
                    lostCount--;
                }
                if (lostCount == 0)
                {
                    mbrWaitForDisconnect = false;
                    DebugInfo("Connect disconnected");
                    Disconnect();
                    break;
                }
            }
            Monitor.Exit(mbrReceverLocker);
        }
        protected virtual void Send()
        {
            Monitor.Enter(mbrSenderLocker);
            int lostCount = 1024;
            while (mbrWaitForDisconnect)
            {
                MessageFragment m = OutMessage.GetMessage();
                if (!mbrWaitForDisconnect)
                {
                    break;
                }
                while (ClientSocket.SocketUnAvailable && lostCount > 0)
                {
                    lostCount--;
                }
                if (lostCount == 0)
                {
                    mbrWaitForDisconnect = false;
                    Disconnect();
                    break;
                }
                ///
                bool lSended = false;
                int lOffset = 0;
                while (!lSended)
                {
                    SocketAsyncEventArgs e = GetSendEventsPooler().Pop(Config);
                    lSended = m.Buffer.Length < e.Count;
                    int lSendByte = m.Buffer.Length >= e.Count ? e.Count : m.Buffer.Length;
                    if (!mbrWaitForDisconnect)
                    {
                        break;
                    }
                    (e.UserToken as EventToken).MessageID = m.IDentity;
                    e.SetBuffer(e.Offset, lSendByte);
                    Buffer.BlockCopy(m.Buffer, lOffset, e.Buffer, e.Offset, lSendByte);
                    lOffset += lSendByte;
                    if (!ClientSocket.Send(e))
                    {
                        OnSended(e);
                    }
                }
            }
            Monitor.Exit(mbrSenderLocker);
        }
        public SocketConfigure Config { get; set; }
        public string ClientIdentity { set { mbrReceverLocker = "EventSocketer.Receive:" + value; mbrSenderLocker = "EventSocketer.Send:" + value; } }
        public bool Connected { get { return Config.Protocol == ProtocolType.Tcp ? ClientSocket.Connected : mbrWaitForDisconnect; } }
        public virtual int PreparSendMessage(byte[] msg)
        {
            return OutMessage.PushMessage(msg);
        }
        public virtual int PreparSendMessage(string msg) { return PreparSendMessage(Config.Encoding.GetBytes(msg)); }
        protected MessagePool OutMessage { get; private set; }
        protected MessagePool IncommeMessage { get; private set; }
        protected virtual ISocketer ClientSocket { get; set; }
        protected string mbrReceverLocker, mbrSenderLocker;
        protected IPEndPoint mbrListenPoint { get; private set; }
        private bool mbrWaitForDisconnect;
        private Thread mbrSendThread, mbrReceiveThread;
        private void beginSendRec()
        {
            try
            {
                mbrSendThread.Start();
                mbrReceiveThread.Start();
            }
            catch (Exception e)
            {
                SocketErrorArgs r = new SocketErrorArgs();
                r.Exception = e;
                r.Message = e.Message;
                fireEvent(evtError, r);
            }
        }
        internal void InitEventPooler(EventPool pooler, BufferManager buffers, int poolerCount, SocketEvents fun)
        {
            if (pooler != null)
            {
                for (int i = 0; i < poolerCount; i++)
                {
                    SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                    e.RemoteEndPoint = Config.RemotePoint;
                    e.UserToken = new EventToken(pooler.NextTokenID, Config);
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
                            fun(x);
                        }
                        x.SetBuffer(x.Offset, buffers.FragmentSize);
                        pooler.Push(x);
                    };
                    if (buffers.SetBuffer(e))
                    {
                        pooler.Push(e);
                    }
                    else
                    {
                        throw new Exception("SetBuffer");
                    }
                }
            }
        }
        protected virtual ISocketer CreateClientSocket() { return ISocketer.CreateSocket(Config); }
        protected virtual EventPool GetConnectEventsPooler() { throw new NotImplementedException(); }
        protected virtual EventPool GetReceiveEventsPooler() { throw new NotImplementedException(); }
        protected virtual EventPool GetSendEventsPooler() { throw new NotImplementedException(); }
        protected virtual EventPool GetDisconnectEventsPooler() { throw new NotImplementedException(); }
        protected virtual EventPool GetAcceptEventsPooler() { throw new NotImplementedException(); }
        protected virtual BufferManager GetConnectBuffer() { throw new NotImplementedException(); }
        protected virtual BufferManager GetRecevieBuffer() { throw new NotImplementedException(); }
        protected virtual BufferManager GetSendBuffer() { throw new NotImplementedException(); }
        protected virtual BufferManager GetDisonnectBuffer() { throw new NotImplementedException(); }
        protected virtual BufferManager GetAcceptBuffer() { throw new NotImplementedException(); }
        protected virtual void OnSended(SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                SocketErrorArgs r = new SocketErrorArgs(e);
                fireEvent(evtError, r);
                if (!Config.OnErrorContinue)
                {
                    return;
                }
            }
            int T = e.BytesTransferred;
            DebugInfo(string.Format("[{0}]_OnSended:_[{1}]_", e.UserToken, T));
            if (mbrPerformanceEnabled)
            {
                mbrPSendB += T;
                mbrPSendC++;
                if (mbrPSendM < T)
                {
                    mbrPSendM = T;
                }
                mbrPSendT++;
                mbrPSendL += T;
            }
            SocketEventArgs a = new SocketEventArgs(e);
            fireEvent(evtSend, a);
        }
        protected virtual void OnReceived(SocketAsyncEventArgs e)
        {
            int T = e.BytesTransferred;
            DebugInfo(string.Format("[{0}]_OnReceived:_[{1}]_", e.UserToken, T));
            if (T > 0)
            {
                SocketEventArgs a = new SocketEventArgs(e);
                IncommeMessage.PushMessage(a.Buffer);
                fireEvent(evtRecevie, a);
            }
            if (mbrPerformanceEnabled)
            {
                mbrPReceiveB += T;
                mbrPReceiveC++;
                if (mbrPReceiveM < T)
                {
                    mbrPReceiveM = T;
                }
                mbrPReceiveL++;
                mbrPReceiveL += T;
            }
        }
        protected virtual void OnConnected(SocketAsyncEventArgs e)
        {
            if (mbrPerformanceEnabled)
            {
                mbrPConT++;
                if (e.SocketError != SocketError.Success)
                {
                    mbrPConF++;
                }
            }
            SocketEventArgs a = new SocketEventArgs(e);
            fireEvent(evtConnected, a);
            mbrWaitForDisconnect = (e.ConnectSocket == null ? (!ClientSocket.SocketUnAvailable) : e.ConnectSocket.Connected);
            beginSendRec();
        }
        protected virtual void OnError(SocketAsyncEventArgs x)
        {
            if (mbrPerformanceEnabled)
            {
                mbrPErrC++;
            }
            SocketErrorArgs r = new SocketErrorArgs(x);
            switch (x.LastOperation)
            {
                case SocketAsyncOperation.Accept:
                    x.SetBuffer(x.Offset, GetAcceptBuffer().FragmentSize);
                    break;
                case SocketAsyncOperation.Connect:
                    x.SetBuffer(x.Offset, GetConnectBuffer().FragmentSize);
                    break;
                case SocketAsyncOperation.Disconnect:
                    x.SetBuffer(x.Offset, GetDisonnectBuffer().FragmentSize);
                    break;
                case SocketAsyncOperation.None:
                    break;
                case SocketAsyncOperation.Receive:
                case SocketAsyncOperation.ReceiveFrom:
                case SocketAsyncOperation.ReceiveMessageFrom:
                    x.SetBuffer(x.Offset, GetRecevieBuffer().FragmentSize);
                    break;
                case SocketAsyncOperation.Send:
                case SocketAsyncOperation.SendPackets:
                case SocketAsyncOperation.SendTo:
                    x.SetBuffer(x.Offset, GetSendBuffer().FragmentSize);
                    break;
                default:
                    break;
            }
            fireEvent(evtError, r);
        }
        protected virtual void OnDisconnected(SocketAsyncEventArgs e)
        {
            if (mbrPerformanceEnabled)
            {
                mbrPDisC++;
            }
            OutMessage.ForceClose();
            mbrWaitForDisconnect = false;
            ClientSocket.Shutdown(SocketShutdown.Both);
            SocketEventArgs a = new SocketEventArgs(e);
            fireEvent(evtDisconnected, a);
        }
        protected virtual void OnBeginAccept(SocketAsyncEventArgs e)
        {
            //SocketAsyncEventArgs a = new SocketAsyncEventArgs(e);
            fireEvent(evtBeginAccept, e);
        }
        protected virtual void OnAccepted(SocketAsyncEventArgs e)
        {
            //ServerSocketEventArgs a = new ServerSocketEventArgs(e);
            fireEvent(evtAccepted, e);
        }
        protected virtual void ResetAll()
        {
            ResetConnectAsyncEvents();
            ResetDisconnectAsyncEvents();
            ResetReceiveAsyncEvents();
            ResetSendAsyncEvents();
            ResetAcceptAsyncEvents();
        }
        protected virtual void ResetConnectAsyncEvents() { GetConnectEventsPooler().ForceClose(); }
        protected virtual void ResetReceiveAsyncEvents() { GetReceiveEventsPooler().ForceClose(); }
        protected virtual void ResetSendAsyncEvents() { GetSendEventsPooler().ForceClose(); }
        protected virtual void ResetDisconnectAsyncEvents() { GetDisconnectEventsPooler().ForceClose(); }
        protected virtual void ResetAcceptAsyncEvents() { GetAcceptEventsPooler().ForceClose(); }
#if DEBUG
        protected virtual int EventPoolerSizeConnect { get; private set; }
        protected virtual int EventPoolerSizeSend { get; private set; }
        protected virtual int EventPoolerSizeReceive { get; private set; }
        protected virtual int MessagePoolerSizeReceive { get; private set; }
        protected virtual int MessagePoolerSizeSend { get; private set; }
#endif
        protected void DebugInfo(object o)
        {
#if DEBUG
            Debuger.DebugInfo(string.Format("msg:[{0}],EventPoolerSizeConnect:[{1}],EventPoolerSizeReceive:[{2}],EventPoolerSizeSend:[{3}],MessagePoolerSizeReceive:[{4}],MessagePoolerSizeSend:[{5}]", o, EventPoolerSizeConnect, EventPoolerSizeReceive, EventPoolerSizeSend, MessagePoolerSizeReceive, MessagePoolerSizeSend));
#endif
        }
    }
}
