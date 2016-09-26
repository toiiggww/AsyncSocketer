using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using TEArts.Etc.CollectionLibrary;

namespace TEArts.Networking.AsyncSocketer
{
    /// <summary>
    /// This is driverd from http://www.codeproject.com/Articles/83102/C-SocketAsyncEventArgs-High-Performance-Socket-Cod
    /// </summary>
    public abstract class EventSocketer : PerformanceBase, IDentity
    {
        #region EventSystem
        protected object evtConnecting,
            evtConnected,
            evtSend,
            evtRecevie,
            evtDisconnected,
            evtAccepted,
            evtError;
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
        private static byte[] mbrEmptyBuffer;
        public static byte[] EmptyBuffer
        {
            get
            {
                if (mbrEmptyBuffer == null)
                {
                    mbrEmptyBuffer = new byte[] { };
                }
                return mbrEmptyBuffer;
            }
        }
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
            mbrPerformanceEnabled = true;
        }
        protected EventSocketer(SocketConfigure sc)
            : this()
        {
            initComponts(sc, null);
        }
        protected EventSocketer(SocketConfigure sc, Socket skt) : this()
        {
            initComponts(sc, skt);
        }
        public virtual void Start()
        {
            InitLockers();
            if (ClientSocket != null)
            {
                mbrWaitForDisconnect = true;
                beginSendRec();
            }
            else if (Config.SocketType == EventSocketType.Client)
            {
                Connect();
            }
            else if (Config.SocketType == EventSocketType.Server)
            {
                //if (TransferConfig == null)
                //{
                //    throw new ArgumentNullException("EventSocketer.TransferConfig", "Must been seted");
                //}
                mbrWaitForDisconnect = true;
                //Accept();
            }
        }
        public virtual void Stop()
        {
            if (Config.SocketType == EventSocketType.Client)
            {
                Disconnect();
            }
            else if (Config.SocketType == EventSocketType.Server)
            {
                Close();
            }
        }
        public virtual void Start(SocketConfigure sc)
        {
            Config = sc;
            Start();
        }
        public virtual void Connect(System.Net.IPAddress iPAddress, int p)
        {
            Config.RemoteSocketPoint = new IPEndPoint(iPAddress, p);
            Connect();
        }
        public virtual void Connect()
        {
            InitLockers();
            fireEvent(evtConnecting, null);
            SocketAsyncEventArgs e = GetConnectEventsPooler().Pop(Config);
            if (Config.SendDataOnConnected)
            {
                //PreparSendMessage(new byte[] { 0x0, 0x0, 0x0, 0x0 });
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
                        e.SetBuffer(m.Buffer, 0, m.Buffer.Length);
                        DebugInfo(s);
                    }
                }
            }
            if (!ClientSocket.Connect(e))
            {
                OnConnected(e);
            }
        }
        private void InitLockers()
        {
            if (
                mbrAcceptLocker == null || string.IsNullOrEmpty(mbrAcceptLocker.Trim()) ||
                mbrSenderLocker == null || string.IsNullOrEmpty(mbrSenderLocker.Trim()) ||
                mbrReceverLocker == null || string.IsNullOrEmpty(mbrReceverLocker.Trim())
                )
            {
                Random r = new Random();
                r = new Random(r.Next() * 1000);
                ClientIdentity = string.Format("DefaultIdnetity_{0:X}", r.Next() * 10000);
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
        }
        public virtual void ReConnect()
        {
            Disconnect();
            Connect();
        }
        public virtual void Accept()
        {
            Monitor.Enter(mbrAcceptLocker);
            ClientSocket.Listen(Config.AsyncSendReceiveEventInstance);
            int i = 0;
            //while (mbrWaitForDisconnect)
            for (int j = 0; j < Config.AsyncSendReceiveEventInstance; j++)
            {
                i++;
                Console.Write(" " + (i / Config.AsyncSendReceiveEventInstance).ToString() + "_" + (i % Config.AsyncSendReceiveEventInstance).ToString());
                SocketAsyncEventArgs e = GetAcceptEventsPooler().Pop(Config);
                if (!ClientSocket.Accept(e))
                {
                    OnAccepted(e);
                }
            }
            Monitor.Exit(mbrAcceptLocker);
        }
        public virtual void Close() { }
        public virtual EventSocketer New(SocketConfigure sc) { throw new NotImplementedException(); }
        public virtual EventSocketer New(Socket accept) { throw new NotImplementedException(); }
        public virtual EventSocketer New(SocketConfigure sc, Socket accept) { throw new NotImplementedException(); }
        protected virtual void Receive()
        {
            Monitor.Enter(mbrReceverLocker);
            while (mbrWaitForDisconnect)
            {
                if (!NotTimeout)
                {
                    try { ClientSocket.Disconnect(GetDisconnectEventsPooler().Pop(Config)); }
                    catch { }
                    break;
                }
                if (ClientSocket.CanRead)
                {
                    SocketAsyncEventArgs e = GetReceiveEventsPooler().Pop(Config);
                    if (!mbrWaitForDisconnect)
                    {
                        break;
                    }
                    if (!ClientSocket.Receive(e))
                    {
                        OnReceived(e);
                    }
                }
                Thread.Sleep(5);
            }
            DebugInfo("Exit Receive");
            Monitor.Exit(mbrReceverLocker);
        }
        protected virtual void Send()
        {
            Monitor.Enter(mbrSenderLocker);
            while (mbrWaitForDisconnect)
            {
                MessageFragment m = OutMessage.GetMessage();
                if (!NotTimeout)
                {
                    try { ClientSocket.Disconnect(GetDisconnectEventsPooler().Pop(Config)); }
                    catch { }
                    break;
                }
                if (ClientSocket.CanWrite && m != null)
                {
                    bool lSended = false;
                    int lOffset = 0;
                    while (!lSended)
                    {
                        SocketAsyncEventArgs e = GetSendEventsPooler().Pop(Config);
                        if (!mbrWaitForDisconnect)
                        {
                            break;
                        }
                        lSended = m.Buffer.Length < e.Count;
                        int lSendByte = m.Buffer.Length >= e.Count ? e.Count : m.Buffer.Length;
                        (e.UserToken as EventToken).MessageID = m.IDentity;
                        e.SetBuffer(e.Offset, lSendByte);
                        Buffer.BlockCopy(m.Buffer, lOffset, e.Buffer, e.Offset, lSendByte);
                        lOffset += lSendByte;
                        try
                        {
                            if (!ClientSocket.Send(e))
                            {
                                OnSended(e);
                            }
                        }
                        catch (Exception ce)
                        {
                            SocketErrorArgs er = new SocketErrorArgs(e);
                            er.Operation = SocketAsyncOperation.Send;
                            er.Exception = ce;
                            fireEvent(evtError, er);
                            if (!Config.OnErrorContinue)
                            {
                                mbrWaitForDisconnect = false;
                                break;
                            }
                        }
                    }
                }
            }
            DebugInfo("Exit Send");
            Monitor.Exit(mbrSenderLocker);
        }
        public SocketConfigure Config { get; set; }
        public SocketConfigure TransferConfig { get; set; }
        public string ClientIdentity { set { mbrReceverLocker = "EventSocketer.Receive:" + value; mbrSenderLocker = "EventSocketer.Send:" + value; mbrAcceptLocker = "EventSocketer.Accept:" + value; } }
        public bool Connected
        {
            get
            {
                bool b = ClientSocket != null;
                b &= Config.SocketType == EventSocketType.Accepted;
                bool c = ClientSocket.ClientSocker != null;
                c &= ClientSocket.Connected;
                b = b |= c;
                return b;
            }
        }
        private MessagePool OutMessageBack { get; set; }
        public virtual int PreparSendMessage(byte[] msg)
        {
            if (OutMessageBack == null)
            {
                OutMessageBack = new MessagePool(10);
            }
            //try
            //{
            //    if (!Connected)
            //    {
            //        //throw new SocketException(((int)(SocketError.NotConnected)));
            //        Debuger.Loger.DebugInfo(DebugType.Error, "NotConnected");
            //    }
            //    Debuger.Loger.DebugInfo(DebugType.Error, RemoteEndPoint.ToString());
            //}
            //catch (Exception ex)
            //{
            //    Debuger.Loger.DebugInfo(DebugType.Error, ex.ToString());
            //}
            if (OutMessageBack.Count > 0)
            {
                if (CheckConnection)
                {
                    OutMessageBack.PushMessage(msg);
                }
                else
                {
                    Disconnect();
                }
            }
            return OutMessage.PushMessage(msg);
        }
        public virtual int PreparSendMessage(string msg) { return PreparSendMessage(Config.Encoding.GetBytes(msg)); }
        public EndPoint LocalEndPoint { get { return ClientSocket.ClientSocker == null ? null : ClientSocket.ClientSocker.LocalEndPoint; } }
        public EndPoint RemoteEndPoint { get { return ClientSocket.ClientSocker == null ? null : ClientSocket.ClientSocker.RemoteEndPoint; } }
        protected MessagePool OutMessage { get; private set; }
        protected MessagePool IncommeMessage { get; private set; }
        //protected EventSocketer TranslateSocket { get; set; }
        protected virtual ISocketer ClientSocket { get; set; }
        protected string mbrReceverLocker, mbrSenderLocker, mbrAcceptLocker;
        protected IPEndPoint mbrListenPoint { get; private set; }
        //protected virtual Pooler<EventSocketer> AccepteSocketPooler { get; set; }
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
        protected void InitEventPooler(EventPool pooler, BufferManager buffers, int poolerCount, SocketEvents fun)
        {
            if (pooler != null)
            {
                for (int i = 0; i < poolerCount; i++)
                {
                    SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                    e.RemoteEndPoint = Config.RemoteSocketPoint;
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
                        try
                        {
                            fun(x);
                        }
                        catch (Exception ex)
                        {
                            Debuger.Loger.DebugInfo(DebugType.Error, ex.ToString());
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
        protected void InitSocketPooler(Pooler<EventSocketer> pooler, EventSocketer socket, SocketEventHandler connect, SocketEventHandler send, SocketEventHandler recevie, SocketEventHandler disconnect)
        {
            if (pooler != null)
            {
                for (int i = 0; i < Config.MaxConnectCount; i++)
                {
                    EventSocketer e = socket.New(TransferConfig);
                    if (connect != null)
                    {
                        e.AfterConnected += connect;
                    }
                    if (send != null)
                    {
                        e.Sended += send;
                    }
                    if (recevie != null)
                    {
                        e.Recevied += recevie;
                    }
                    if (disconnect != null)
                    {
                        e.Disconnected += disconnect;
                    }
                    e.Disconnected += (o, x) =>
                        {
                            DebugInfo("Client Disconnected, Socket will be Recycled");
                            pooler.Pushin(e);
                        };
                }
            }
        }
        protected virtual ISocketer CreateClientSocket() { throw new NotImplementedException(); }
        protected virtual ISocketer CreateClientSocket(Socket skt) { throw new NotImplementedException(); }
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
        protected virtual EventSocketer GetAcceptInstance() { return GetAcceptInstancePooler().Popup(); }
        protected virtual Pooler<EventSocketer> GetAcceptInstancePooler() { throw new NotImplementedException(); }
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
            if (e.SocketError == SocketError.Disconnecting)
            {
                Debuger.Loger.DebugInfo(DebugType.Warning, "Connection for {0} is Disconnecting , connection will be lost.", e.RemoteEndPoint);
                OnDisconnected(e);
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
            Config.RemoteSocketPoint = e.RemoteEndPoint as IPEndPoint;
            SocketEventArgs a = new SocketEventArgs(e);
            fireEvent(evtConnected, a);
            //mbrWaitForDisconnect = (e.ConnectSocket == null ? (!ClientSocket.CanRead) : e.ConnectSocket.Connected);
            mbrWaitForDisconnect = e.ConnectSocket != null && e.ConnectSocket.Connected;
            if (mbrWaitForDisconnect)
            {
                beginSendRec();
            }
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
            ClientSocket.Shutdown(SocketShutdown.Send);
            if (CheckConnection)
            {
                Debuger.Loger.Warning("Connect exists");
            }
            mbrWaitForDisconnect = false;
            SocketEventArgs a = new SocketEventArgs(e);
            fireEvent(evtDisconnected, a);
            OutMessage.ForceClose();
            IncommeMessage.ForceClose();
            ResetConnectAsyncEvents();
            ResetDisconnectAsyncEvents();
            ResetReceiveAsyncEvents();
            ResetSendAsyncEvents();
        }
        protected virtual void OnAccepted(SocketAsyncEventArgs e)
        {
            EventSocketer t = GetAcceptInstance();
            ServerSocketEventArgs a = new ServerSocketEventArgs(e);
            a.AcceptSocket = t.New(TransferConfig, e.AcceptSocket == null ? e.ConnectSocket : e.AcceptSocket);
            fireEvent(evtAccepted, a);
            t.Start();
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
        private void initComponts(SocketConfigure sc, Socket skt)
        {
            Config = sc;
            if (mbrListenPoint == null && sc.LocalSocketPoint == null)
            {
                mbrListenPoint = new IPEndPoint(Config.IPAddress, (sc.SocketType == EventSocketType.Server ? Config.Port : 0));
                sc.LocalSocketPoint = mbrListenPoint;
            }
            else
            {
                mbrListenPoint = sc.LocalSocketPoint;
            }
            if (sc.RemoteSocketPoint == null)
            {
                sc.RemoteSocketPoint = new IPEndPoint(Config.IPAddress, Config.Port);
            }
            OutMessage = new MessagePool(128);
            IncommeMessage = new MessagePool(12800);
            if (skt == null)
            {
                ClientSocket = CreateClientSocket();
            }
            else
            {
                ClientSocket = CreateClientSocket(skt);
            }
            try { ClientSocket.Bind(mbrListenPoint); } catch { }
            LastAlive = DateTime.Now.AddSeconds(Config.TimeOut);
        }
        public void PrintBuffer()
        {
            Debuger.Loger.DebugInfo("------++++ IncommeMessage ++++------");
            Debuger.Loger.DebugInfo(DebugType.Debug, IncommeMessage.ToString());
            Debuger.Loger.DebugInfo("------++++   OutMessage   ++++------");
            Debuger.Loger.DebugInfo(DebugType.Debug, OutMessage.ToString());
            Debuger.Loger.DebugInfo("------++++ OutMessageBack ++++------");
            Debuger.Loger.DebugInfo(DebugType.Debug, OutMessageBack.ToString());
            Debuger.Loger.DebugInfo("------++++       End      ++++------");
        }
        public bool CheckConnection
        {
            get
            {
                if (ClientSocket != null && ClientSocket.Connected)
                {
                    try
                    {
                        ClientSocket.ClientSocker.Send(EmptyBuffer);
                        return true;
                    }
                    catch (SocketException ex)
                    {
                        if (ex.SocketErrorCode == SocketError.WouldBlock)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }
#if DEBUG
        public bool DebugOutpu { get; set; }
        protected virtual int EventPoolerSizeConnect { get; private set; }
        protected virtual int EventPoolerSizeSend { get; private set; }
        protected virtual int EventPoolerSizeReceive { get; private set; }
        protected virtual int MessagePoolerSizeReceive { get; private set; }
        protected virtual int MessagePoolerSizeSend { get; private set; }

#endif
        protected void DebugInfo(object o)
        {
#if DEBUG
            if (DebugOutpu)
            {
                Debuger.Loger.DebugInfo(string.Format("msg:[{0}],EventPoolerSizeConnect:[{1}],EventPoolerSizeReceive:[{2}],EventPoolerSizeSend:[{3}],MessagePoolerSizeReceive:[{4}],MessagePoolerSizeSend:[{5}]", o, EventPoolerSizeConnect, EventPoolerSizeReceive, EventPoolerSizeSend, MessagePoolerSizeReceive, MessagePoolerSizeSend));
            }
#endif
        }
        public int IDentity
        {
            get;
            set;
        }
        public DateTime LastAlive { get; protected set; }
        public bool NotTimeout
        {
            get { return Config.EnableTimeoutCheck ? (Config.TimeOut > 0 ? (DateTime.Now - LastAlive).TotalSeconds < Config.TimeOut : true) : true; }
        }
    }
}
