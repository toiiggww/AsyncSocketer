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
            evtError;
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
            SocketAsyncEventArgs e = GetDisconnectAsyncEvents();
            if (!ClientSocket.Disconnect(e))
            {
                OnDisconnected(e);
            }
            OutMessage.ForceClose();
            IncommeMessage.ForceClose();
            ResetConnectAsyncEvents();
            ResetDisconnectAsyncEvents();
            ResetReceiveAsyncEvents();
            ResetSendAsyncEvents();
        }
        #endregion
        protected MessagePool OutMessage { get; private set; }
        protected MessagePool IncommeMessage { get; private set; }
        public SocketConfigure Config { get; set; }
        protected virtual ISocketer ClientSocket { get; set; }
        public string ClientIdentity { set { mbrReceverLocker = "EventSocketer.Receive" + value; mbrSenderLocker = "EventSocketer.Send:" + value; } }
        private string mbrReceverLocker, mbrSenderLocker;
        public bool Connected { get { return Config.Protocol == ProtocolType.Tcp ? ClientSocket.Connected : mbrWaitForDisconnect; } }
        public virtual int PreparSendMessage(byte[] msg)
        {
            return OutMessage.PushMessage(msg);
        }
        public virtual int PreparSendMessage(string msg) { return PreparSendMessage(Config.Encoding.GetBytes(msg)); }
        private bool mbrWaitForDisconnect;
        private Thread mbrSendThread, mbrReceiveThread;
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
            OutMessage = new MessagePool();
            IncommeMessage = new MessagePool();
            ClientSocket = CreateClientSocket();
            foreach (IPAddress d in Dns.GetHostEntry(string.Empty).AddressList)
            {
                if (d.AddressFamily == ClientSocket.AddressFamily)
                {
                    ClientSocket.Bind(new IPEndPoint(d, Config.ListenPort));
                    break;
                }
            }
        }
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
        public void StartClient(System.Net.IPAddress iPAddress, int p)
        {
            Config.RemotePoint = new System.Net.IPEndPoint(iPAddress, p);
            StartClient();
        }
        public void StartClient()
        {
            if (
                mbrSenderLocker == null || string.IsNullOrEmpty(mbrSenderLocker.Trim()) ||
                mbrReceverLocker == null || string.IsNullOrEmpty(mbrReceverLocker.Trim())
                )
            {
                Random r = new Random();
                r = new Random(r.Next() * 1000);
                ClientIdentity = string.Format("DefaultIdnetity_{0}", r.Next() * 10000);
            }
            fireEvent(evtConnecting, null);
            SocketAsyncEventArgs e = GetConnectAsyncEvents();
            if (Config.SendDataOnConnected)
            {
                MessageFragment m = OutMessage.GetMessage();
                (e.UserToken as EventToken).MessageID = m.IDentity;
                GetSendBuffer().SetBuffer(e, m.Buffer.Length);
                Buffer.BlockCopy(m.Buffer, 0, e.Buffer, e.Offset, m.Buffer.Length);
            }
            if (!ClientSocket.Connect(e))
            {
                OnConnected(e);
            }
        }
        public virtual void ReConnect()
        {
            ClientSocket.Shutdown(SocketShutdown.Both);
            StartClient();
        }
        protected virtual ISocketer CreateClientSocket() { throw new NotImplementedException(); }
        protected virtual SocketAsyncEventArgs GetConnectAsyncEvents() { throw new NotImplementedException(); }
        protected virtual SocketAsyncEventArgs GetReceiveAsyncEvents() { throw new NotImplementedException(); }
        protected virtual SocketAsyncEventArgs GetSendAsyncEvents() { throw new NotImplementedException(); }
        protected virtual SocketAsyncEventArgs GetDisconnectAsyncEvents() { throw new NotImplementedException(); }
        protected virtual BufferManager GetConnectBuffer() { throw new NotImplementedException(); }
        protected virtual BufferManager GetRecevieBuffer() { throw new NotImplementedException(); }
        protected virtual BufferManager GetSendBuffer() { throw new NotImplementedException(); }
        protected virtual BufferManager GetDisonnectBuffer() { throw new NotImplementedException(); }
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
            DebugInfo("OnSended:_" + T.ToString());
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
            DebugInfo("OnReceived:_" + T.ToString());
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
            mbrWaitForDisconnect = (e.ConnectSocket == null ? false : e.ConnectSocket.Connected);
            beginSendRec();
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
            OutMessage.ForceClose();
            mbrWaitForDisconnect = false;
            SocketEventArgs a = new SocketEventArgs(e);
            fireEvent(evtDisconnected, e);
        }
        protected virtual void Receive()
        {
            Monitor.Enter(mbrReceverLocker);
            while (mbrWaitForDisconnect)
            {
                SocketAsyncEventArgs e = GetReceiveAsyncEvents();
                GetRecevieBuffer().SetBuffer(e, Config.BufferSize);
                if (!ClientSocket.Receive(e))
                {
                    OnReceived(e);
                }
                DebugInfo("Wait for Receive Compleate");
            }
            Monitor.Exit(mbrReceverLocker);
        }
        protected virtual void Send()
        {
            Monitor.Enter(mbrSenderLocker);
            while (mbrWaitForDisconnect)
            {
                SocketAsyncEventArgs e = GetSendAsyncEvents();
                DebugInfo("Wait for Message");
                MessageFragment m = OutMessage.GetMessage();
                DebugInfo("Got Message");
                (e.UserToken as EventToken).MessageID = m.IDentity;
                GetSendBuffer().SetBuffer(e, m.Buffer.Length);
                Buffer.BlockCopy(m.Buffer, 0, e.Buffer, e.Offset, m.Buffer.Length);
                if (!ClientSocket.Send(e))
                {
                    OnSended(e);
                }
                DebugInfo("Wait for Send Compleate");
            }
            Monitor.Exit(mbrSenderLocker);
        }
        protected virtual void ResetConnectAsyncEvents() { }
        protected virtual void ResetReceiveAsyncEvents() { }
        protected virtual void ResetSendAsyncEvents() { }
        protected virtual void ResetDisconnectAsyncEvents() { }
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
    public class Debuger
    {
        private static object mbrDebugHandler = new object();
        public static void DebugInfo(object o)
        {
#if DEBUG
            lock (mbrDebugHandler)
            {
                if (o == null)
                {
                    DebugInfo("====]> NULL <[====");
                }
                else if (o.GetType() == typeof(byte[]))
                {
                    DebugInfo(SocketEventArgs.FormatArrayMatrix(o as byte[]));
                }
                else
                {
                    string[] s = o.ToString().Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    foreach (string x in s)
                    {
                        System.Console.WriteLine(string.Format("{0}\t{1}", DateTime.Now, x));
                    }
                }
            }
#endif
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
            r = string.Format("{0}{1}{2}", r, Environment.NewLine, FormatArrayMatrix(Buffer));
            return r;
        }
        public static string FormatArrayMatrix(byte[] array)
        {
            string r = "";
#if DEBUG
            string b = "", s = "";
            r = string.Format("{0}{1}Index \\ Offset  ", r, Environment.NewLine);
            int i = 0, j = 0;
            for (; i < 16; i++)
            {
                r = string.Format("{0} _{1:X}", r, i);
            }
            r = r + " [_____string_____]" + Environment.NewLine;
            i = array.Length / 16;
            for (int k = 0; k < i; k++)
            {
                for (j = k * 16; j < (((k + 1) * 16 > array.Length) ? array.Length - 1 : ((k + 1) * 16)); j++)
                {
                    b = string.Format("{0} {1:X2}", b, array[j]);
                    s = string.Format("{0}{1}", s, (array[j] == 0 ? '.' : ((array[j] == 0x0a || array[j] == 0x0d || array[j] == 0x08 || array[j] == 0x09 || array[j] == 0x7f) ? '_' : (char)array[j])));
                }
                r = string.Format("{0}{1:X11}_ |  {2}  {3:50}{4}", r, k, b, s, Environment.NewLine);
                b = "";
                s = "";
                if (k == i - 1)
                {
                    for (j = (k + 1) * 16; j < array.Length; j++)
                    {
                        b = string.Format("{0} {1:X2}", b, array[j]);
                        s = string.Format("{0}{1}", s, (array[j] == 0 ? '.' : ((array[j] == 0x0a || array[j] == 0x0d || array[j] == 0x08 || array[j] == 0x09 || array[j] == 0x7f) ? '_' : (char)array[j])));
                    }
                    for (j = 0; j < (i + 1) * 16 - array.Length; j++)
                    {
                        b = string.Format("{0}   ", b);
                    }
                    k++;
                    r = string.Format("{0}{1:X11}_ |  {2}  {3:50}{4}", r, k, b, s, Environment.NewLine);
                }
            }
#endif
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
