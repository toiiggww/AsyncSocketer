using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.ComponentModel;
using System.Threading;

namespace AsyncSocketer
{
    /// <summary>
    /// This is driverd from http://www.codeproject.com/Articles/83102/C-SocketAsyncEventArgs-High-Performance-Socket-Cod
    /// </summary>
    public abstract class EventSocketer : Component
    {
        #region EventSystem
        protected object evtConnecting,
            evtConnected,
            evtSend,
            evtRecevie,
            evtDisconnected,
            evtError;
        public event SocketEventHandler Connecting { add { base.Events.AddHandler(evtConnecting, value); } remove { base.Events.RemoveHandler(evtConnecting, value); } }
        public event SocketEventHandler Connected { add { base.Events.AddHandler(evtConnected, value); } remove { base.Events.RemoveHandler(evtConnected, value); } }
        public event SocketEventHandler Sended { add { base.Events.AddHandler(evtSend, value); } remove { base.Events.RemoveHandler(evtSend, value); } }
        public event SocketEventHandler Recevied { add { base.Events.AddHandler(evtRecevie, value); } remove { base.Events.RemoveHandler(evtRecevie, value); } }
        public event SocketEventHandler Disconnected { add { base.Events.AddHandler(evtDisconnected, value); } remove { base.Events.RemoveHandler(evtDisconnected, value); } }
        public event SocketErrorHandler Error { add { base.Events.AddHandler(evtError, value); } remove { base.Events.RemoveHandler(evtError, value); } }
        protected virtual void fireEvent(object evt, object e)
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
                    else if (evt == evtPerformance)
                    {
                        (o as SocketPerformanceHandler)(this, (e as PerformanceCountArgs));
                    }
                    else
                    {
                        (o as SocketEventHandler)(this, (e as SocketEventArgs));
                    }
                }
            }
        }
        public virtual void Connect(System.Net.IPAddress iPAddress, int p)
        {
            Config.RemotePoint = new IPEndPoint(iPAddress, p);
            StartClient();
        }
        public virtual void Connect()
        {
            StartClient();
        }
        public virtual void Disconnect()
        {
            SocketAsyncEventArgs e = GetConnectAsyncEvents();
            if (!ClientSocket.Disconnect(e))
            {
                OnDisconnected(e);
            }
        }
        #endregion
        #region Performance
        private object evtPerformance;
        private Timer mbrPerformanceTimer;
        public int PerformanceInMin { get; set; }
        public int PerformanceAfterLimit { get; set; }
        public event SocketPerformanceHandler Performance { add { base.Events.AddHandler(evtPerformance, value); } remove { base.Events.RemoveHandler(evtPerformance, value); } }
        private int mbrPSendC,  // Send Count
            mbrPSendB,          // Bytes
            mbrPSendT,          // Total Count
            mbrPSendM,          // Max Package length
            mbrPSendL,          // Total Bytes
            mbrPSendI,          // In OutMessage Count
            mbrPReceiveC,       // Receve Count
            mbrPReceiveB,       // Bytes
            mbrPReceiveT,       // Total Count
            mbrPReceiveM,       // Max Package length
            mbrPReceiveL,       // Total Bytes
            mbrPReceiveI,       // In IncommeMessage Count
            mbrPConT,           // Total Connect
            mbrPConF,           // Connect fail
            mbrPDisC,           // Disconnect
            mbrPErrC;           // Errors
        private bool mbrPerformanceEnabled;
        public bool EnablePerformance
        {
            set
            {
                if (mbrPerformanceEnabled == value)
                {
                    return;
                }
                mbrPerformanceEnabled = value;
                if (value)
                {
                    mbrPerformanceTimer.Change(60 * 1000 * (PerformanceInMin > 0 ? PerformanceInMin : 1), Timeout.Infinite);
                }
                else
                {
                    mbrPerformanceTimer.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }
            get
            {
                return mbrPerformanceEnabled;
            }
        }
        #endregion
        protected MessagePool OutMessage { get; private set; }
        protected MessagePool IncommeMessage { get; private set; }
        public SocketConfigure Config { get; set; }
        protected virtual ISocketer ClientSocket { get; set; }
        public bool Connedted { get { return ClientSocket.Connected; } }
        public virtual int PreparSendMessage(byte[] msg) { return OutMessage.PushMessage(msg); }
        public virtual int PreparSendMessage(string msg) { return OutMessage.PushMessage(msg); }
        private bool mbrJustSended, mbrJustRecevied;
        protected EventSocketer()
        {
            evtConnecting = new object();
            evtConnected = new object();
            evtSend = new object();
            evtRecevie = new object();
            evtDisconnected = new object();
            evtError = new object();
            mbrPerformanceTimer = new Timer(
                (o) =>
                {
                    if (mbrPerformanceEnabled)
                    {
                        PerformanceCountArgs p = new PerformanceCountArgs();
                        p.ConnectFailed = mbrPConF;
                        p.Connectting = mbrPConT;
                        p.Disconnectting = mbrPDisC;
                        p.Errors = mbrPErrC;
                        p.MaxReceivedBytes = mbrPReceiveM;
                        p.MaxSendBytes = mbrPSendM;
                        p.ReceiveBytes = mbrPReceiveB;
                        p.ReceivedMessagesInPooler = IncommeMessage.Count;
                        p.ReceivePackages = mbrPReceiveC;
                        p.SendBytes = mbrPSendB;
                        p.SendMessagesInPooler = OutMessage.Count;
                        p.SendPackages = mbrPSendC;
                        p.TotalReceiveBytes = mbrPReceiveL;
                        p.TotalReceivePackages = mbrPReceiveT;
                        p.TotalSendBytes = mbrPSendL;
                        p.TotalSendPackages = mbrPSendT;
                        mbrPSendB = 0;
                        mbrPSendC = 0;
                        mbrPReceiveB = 0;
                        mbrPReceiveC = 0;
                        fireEvent(evtPerformance, p);
                    }
                }, null, Timeout.Infinite, Timeout.Infinite);
        }
        protected EventSocketer(SocketConfigure sc)
            : this()
        {
            Config = sc;
            OutMessage = new MessagePool();
            OutMessage.Config = Config;
            IncommeMessage = new MessagePool();
            IncommeMessage.Config = Config;
            mbrJustSended = false;
            mbrJustRecevied = false;
            switch (Config.Protocol)
            {
                case ProtocolType.Ggp:
                    break;
                case ProtocolType.IP:
                    break;
                case ProtocolType.IPSecAuthenticationHeader:
                    break;
                case ProtocolType.IPSecEncapsulatingSecurityPayload:
                    break;
                case ProtocolType.IPv4:
                    break;
                case ProtocolType.IPv6:
                    break;
                case ProtocolType.IPv6DestinationOptions:
                    break;
                case ProtocolType.IPv6FragmentHeader:
                    break;
                case ProtocolType.IPv6NoNextHeader:
                    break;
                case ProtocolType.IPv6RoutingHeader:
                    break;
                case ProtocolType.Icmp:
                    break;
                case ProtocolType.IcmpV6:
                    break;
                case ProtocolType.Idp:
                    break;
                case ProtocolType.Igmp:
                    break;
                case ProtocolType.Ipx:
                    break;
                case ProtocolType.ND:
                    break;
                case ProtocolType.Pup:
                    break;
                case ProtocolType.Raw:
                    break;
                case ProtocolType.Spx:
                    break;
                case ProtocolType.SpxII:
                    break;
                case ProtocolType.Tcp:
                    ClientSocket = new TcpSocketer(Config);
                    break;
                case ProtocolType.Udp:
                    ClientSocket = new UdpSocketer(Config);
                    break;
                case ProtocolType.Unknown:
                    break;
                default:
                    break;
            }
            foreach (IPAddress d in Dns.GetHostEntry(string.Empty).AddressList)
            {
                if (d.AddressFamily == ClientSocket.AddressFamily)
                {
                    ClientSocket.Bind(new IPEndPoint(d, 0));
                    break;
                }
            }
        }
        public void StartClient(System.Net.IPAddress iPAddress, int p)
        {
            Config.RemotePoint = new System.Net.IPEndPoint(iPAddress, p);
            StartClient();
        }
        public void StartClient(bool witeForSend)
        {
            fireEvent(evtConnecting, null);
            SocketAsyncEventArgs e = GetConnectAsyncEvents();
            if (witeForSend)
            {
                MessageFragment m = OutMessage.GetMessage();
                (e.UserToken as EventToken).MessageID = m.MessageIndex;
                GetSendBuffer().SetBuffer(e, m.Buffer.Length);
                Buffer.BlockCopy(m.Buffer, 0, e.Buffer, e.Offset, m.Buffer.Length);
            }
            if (!ClientSocket.Connect(e))
            {
                OnConnected(e);
            }
        }
        public void StartClient() { StartClient(true); }
        public virtual int Send(byte[] msg)
        {
            int i = PreparSendMessage(msg);
            if (!mbrJustSended)
            {
                Send();
            }
            return i;
        }
        public virtual void ReConnect()
        {
            ClientSocket.Shutdown(SocketShutdown.Both);
            StartClient();
        }
        protected virtual SocketAsyncEventArgs GetConnectAsyncEvents() { return null; }
        protected virtual SocketAsyncEventArgs GetReceiveAsyncEvents() { return null; }
        protected virtual SocketAsyncEventArgs GetSendAsyncEvents() { return null; }
        protected virtual BufferManager GetConnectBuffer() { return null; }
        protected virtual BufferManager GetRecevieBuffer() { return null; }
        protected virtual BufferManager GetSendBuffer() { return null; }
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
            if (mbrPerformanceEnabled)
            {
                int T = e.BytesTransferred;
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
            mbrJustSended = true;
            Send();
            if (!mbrJustRecevied)
            {
                Receive();
            }
        }
        protected virtual void OnReceived(SocketAsyncEventArgs e)
        {
            int T = e.BytesTransferred;
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
            mbrJustRecevied = true;
            Receive();
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
            Receive();
        }
        protected virtual void OnError(SocketAsyncEventArgs x)
        {
            if (mbrPerformanceEnabled)
            {
                mbrPErrC++;
            }
            SocketErrorArgs r = new SocketErrorArgs(x);
            fireEvent(evtError, r);
        }
        protected virtual void OnDisconnected(SocketAsyncEventArgs e)
        {
            if (mbrPerformanceEnabled)
            {
                mbrPDisC++;
            }
            SocketEventArgs a = new SocketEventArgs(e);
            fireEvent(evtDisconnected, e);
        }
        protected virtual void Receive()
        {
            if (ClientSocket != null)
            {
                SocketAsyncEventArgs e = GetReceiveAsyncEvents();
                GetRecevieBuffer().SetBuffer(e, Config.BufferSize);
                if (!ClientSocket.Receive(e))
                {
                    OnReceived(e);
                }
            }
        }
        protected virtual void Send()
        {
            SocketAsyncEventArgs e = GetSendAsyncEvents();
            MessageFragment m = OutMessage.GetMessage();
            (e.UserToken as EventToken).MessageID = m.MessageIndex;
            GetSendBuffer().SetBuffer(e, m.Buffer.Length);
            Buffer.BlockCopy(m.Buffer, 0, e.Buffer, e.Offset, m.Buffer.Length);
            if (!ClientSocket.Send(e))
            {
                OnSended(e);
            }
        }
    }
    public delegate void SocketEventHandler(object sender, SocketEventArgs e);
    public delegate void ServerSocketEventHandler(object sender, ServerSocketEventArgs e);
    public delegate void SocketErrorHandler(object sender, SocketErrorArgs e);
    public delegate void SocketPerformanceHandler(object sender, PerformanceCountArgs e);
    public class SocketEventArgs : EventArgs
    {
        public SocketEventArgs(SocketAsyncEventArgs e)
        {
            SocketStatus = e.SocketError;
            EventToken t = (e.UserToken as EventToken);
            if (t != null)
            {
                SessionID = t.SessionID;
                MessageIndex = t.MessageID;
                Buffer = new byte[e.BytesTransferred];
                System.Buffer.BlockCopy(e.Buffer, e.Offset, Buffer, 0, e.BytesTransferred);
                Remoter = e.RemoteEndPoint;
            }
        }
        public SocketError SocketStatus { get; set; }
        public int SessionID { get; set; }
        public int MessageIndex { get; set; }
        public byte[] Buffer { get; set; }
        public EndPoint Remoter { get; set; }
        public override string ToString()
        {
            string r = string.Format("Socket:[{0}][{1}][{2}][{3}]", SessionID, Remoter, MessageIndex, SocketStatus);
            string b = "", s = "";
            r = string.Format("{0}{1}Index \\ Offset  ", r, Environment.NewLine);
            int i = 0, j = 0;
            for (; i < 16; i++)
            {
                r = string.Format("{0} _{1:X}", r, i);
            }
            r = r + " [_____string_____]" + Environment.NewLine;
            i = Buffer.Length / 16;
            for (int k = 0; k < i; k++)
            {
                for (j = k * 16; j < (((k + 1) * 16 > Buffer.Length) ? Buffer.Length - 1 : ((k + 1) * 16)); j++)
                {
                    b = string.Format("{0} {1:X2}", b, Buffer[j]);
                    s = string.Format("{0}{1}", s, (Buffer[j] == 0 ? '.' : ((Buffer[j] == 0x0a || Buffer[j] == 0x0d || Buffer[j] == 0x08 || Buffer[j] == 0x7f) ? '_' : (char)Buffer[j])));
                }
                r = string.Format("{0}{1:X11}_ |  {2}  {3:50}{4}", r, k, b, s, Environment.NewLine);
                b = "";
                s = "";
                if (k == i - 1)
                {
                    for (j = (k + 1) * 16; j < Buffer.Length; j++)
                    {
                        b = string.Format("{0} {1:X2}", b, Buffer[j]);
                        s = string.Format("{0}{1}", s, (Buffer[j] == 0 ? '.' : ((Buffer[j] == 0x0a || Buffer[j] == 0x0d || Buffer[j] == 0x08 || Buffer[j] == 0x7f) ? '_' : (char)Buffer[j])));
                    }
                    for (j = 0; j < (i + 1) * 16 - Buffer.Length; j++)
                    {
                        b = string.Format("{0}   ", b);
                    }
                    k++;
                    r = string.Format("{0}{1:X11}_ |  {2}  {3:50}{4}", r, k, b, s, Environment.NewLine);
                }
            }
            return r;
        }
    }
    public class ServerSocketEventArgs : SocketEventArgs
    {
        public ServerSocketEventArgs(SocketAsyncEventArgs e) : base(e) { AcceptSocket = e.AcceptSocket; }
        public Socket AcceptSocket { get; set; }
    }
    public class SocketErrorArgs : EventArgs
    {
        public SocketError SocketError { get; set; }
        public Exception Exception { get; set; }
        public SocketAsyncOperation Operation { get; set; }
        public string Message { get; set; }
        public override string ToString()
        {
            return string.Format("Error:[{0}]{1}On {2} with {3} as : {4}", Message, Environment.NewLine, Operation, SocketError, Exception);
        }
        public SocketErrorArgs() { }
        public SocketErrorArgs(SocketAsyncEventArgs e)
        {
            SocketError = e.SocketError;
            Operation = e.LastOperation;
        }
    }
    public class PerformanceCountArgs : EventArgs
    {
        public int SendPackages { get; set; }
        public int SendBytes { get; set; }
        public int TotalSendPackages { get; set; }
        public int TotalSendBytes { get; set; }
        public int ReceivePackages { get; set; }
        public int ReceiveBytes { get; set; }
        public int TotalReceivePackages { get; set; }
        public int TotalReceiveBytes { get; set; }
        public int MaxSendBytes { get; set; }
        public int MaxReceivedBytes { get; set; }
        public int SendMessagesInPooler { get; set; }
        public int ReceivedMessagesInPooler { get; set; }
        public int Connectting { get; set; }
        public int ConnectFailed { get; set; }
        public int Disconnectting { get; set; }
        public int Errors { get; set; }
        public TimeSpan MaxSendTime { get; set; }
        public TimeSpan MaxReceiveTime { get; set; }
    }
    public class ServerPerformanceCountArgs : PerformanceCountArgs
    {
        public int AcceptedCount { get; set; }
        public int AcceptedError { get; set; }
    }
}