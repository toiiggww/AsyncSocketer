﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.ComponentModel;
using System.Threading;

namespace AsyncSocketer
{
    public abstract class EventSocketer : Component
    {
        #region EventSystem
        protected object evtConnecting,
            evtConnected,
            evtSend,
            evtRecevie,
            evtDisconnected,
            evtError;
        //public SocketEventArgs ArgsConnecting { get; private set; }
        //public SocketEventArgs ArgsConnected { get; private set; }
        //public SocketEventArgs ArgsSend { get; private set; }
        //public SocketEventArgs ArgsRecevie { get; private set; }
        //public SocketEventArgs ArgsDisconnected { get; private set; }
        //public SockerErrorArgs ArgsSocketError { get; private set; }
        public event SocketEventHandler Connecting { add { base.Events.AddHandler(evtConnecting, value); } remove { base.Events.RemoveHandler(evtConnecting, value); } }
        public event SocketEventHandler Connected { add { base.Events.AddHandler(evtConnected, value); } remove { base.Events.RemoveHandler(evtConnected, value); } }
        public event SocketEventHandler Sended { add { base.Events.AddHandler(evtSend, value); } remove { base.Events.RemoveHandler(evtSend, value); } }
        public event SocketEventHandler Recevied { add { base.Events.AddHandler(evtRecevie, value); } remove { base.Events.RemoveHandler(evtRecevie, value); } }
        public event SocketEventHandler Disconnected { add { base.Events.AddHandler(evtDisconnected, value); } remove { base.Events.RemoveHandler(evtDisconnected, value); } }
        public event SockerErrorHandler Error { add { base.Events.AddHandler(evtError, value); } remove { base.Events.RemoveHandler(evtError, value); } }
        protected virtual void fireEvent(object evt, object e)
        {
            if (evt != null)
            {
                object o = base.Events[evt];
                if (o != null)
                {
                    if (evt == evtError)
                    {
                        (o as SockerErrorHandler)(this, (e as SocketErrorArgs));
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
            ClientSocket.Shutdown(SocketShutdown.Both);
        }
        #endregion
        //protected Thread SendThreader { get; set; }
        //protected Thread ReceiveThreader { get; set; }
        //protected void ThreadReset(Thread td)
        //{
        //    if (td != null && td.IsAlive)
        //    {
        //        td.Abort();
        //    }
        //}
        //protected void ThreadRecevie(SocketAsyncEventArgs ex)
        //{
        //    ThreadReset(ReceiveThreader);
        //    ParameterizedThreadStart tr = new ParameterizedThreadStart(startReceive);
        //    ReceiveThreader = new Thread(tr);
        //    ReceiveThreader.Start(ex);
        //}
        //protected void ThreadSend(SocketAsyncEventArgs ex)
        //{
        //    ThreadReset(SendThreader);
        //        ThreadStart tr = new  ThreadStart(StartSend);
        //    SendThreader = new Thread(tr);
        //    SendThreader.Start();
        //)
        //protected virtual void startReceive(object ex)
        //{
        //    SocketAsyncEventArgs o = ex as SocketAsyncEventArgs;
        //    if (o != null)
        //    {
        //        SocketAsyncEventArgs e = GetReceiveAsyncEvents();
        //        GetRecevieBuffer().SetBuffer(e);
        //        e.AcceptSocket = (o.ConnectSocket == null ? o.AcceptSocket : o.ConnectSocket);
        //        e.AcceptSocket.ReceiveAsync(e);
        //    }
        //}
        //protected virtual void startSend(object ex)
        //{
        //    SocketAsyncEventArgs o = ex as SocketAsyncEventArgs;
        //    if (o != null)
        //    {
        //        SocketAsyncEventArgs e = GetSendAsyncEvents();
        //        GetSendBuffer().SetBuffer(e);
        //        e.AcceptSocket = (o.ConnectSocket == null ? o.AcceptSocket : o.ConnectSocket);
        //        MessageFragment m = OutMessage.GetMessage();
        //        (e.UserToken as EventsToken).Reset(m);
        //        e.AcceptSocket.SendAsync(e);
        //    }
        //}
        protected MessagePool OutMessage { get; private set; }
        protected ManualResetEvent ReceviewLocker { get; private set; }
        protected MessagePool IncommeMessage { get; private set; }
        public SocketConfigure Config { get; set; }
        protected virtual Socket ClientSocket { get; set; }
        public bool Connedted { get { return ClientSocket.Connected; } }
        public virtual int PreparSendMessage(byte[] msg) { return OutMessage.PushMessage(msg); }
        public virtual int PreparSendMessage(string msg) { return OutMessage.PushMessage(msg); }
        private bool mbrJuestSended;
        protected EventSocketer()
        {
            evtConnecting = new object();
            evtConnected = new object();
            evtSend = new object();
            evtRecevie = new object();
            evtDisconnected = new object();
            evtError = new object();
        }
        protected EventSocketer(SocketConfigure sc)
            : this()
        {
            Config = sc;
            OutMessage = new MessagePool();
            OutMessage.Config = Config;
            OutMessage.MessageArrived += (o, e) => { if (ClientSocket!=null && ClientSocket.Connected) { Send(); } };
            IncommeMessage = new MessagePool();
            IncommeMessage.Config = Config;
            mbrJuestSended = false;
            ReceviewLocker = new ManualResetEvent(true);
        }
        public void StartClient(System.Net.IPAddress iPAddress, int p)
        {
            Config.RemotePoint = new System.Net.IPEndPoint(iPAddress, p);
            StartClient();
        }
        public void StartClient(bool witeForSend)
        {
            fireEvent(evtConnecting, null);
            ClientSocket = new Socket(Config.RemotePoint.AddressFamily, SocketType.Stream, Config.Protocol);
            foreach (IPAddress d in Dns.GetHostEntry(string.Empty).AddressList)
            {
                if (d.AddressFamily == AddressFamily.InterNetwork)
                {
                    ClientSocket.Bind(new IPEndPoint(d, 0));
                    break;
                }
            }
            //ClientSocket.Listen(Config.TimeOut);
            SocketAsyncEventArgs e = GetConnectAsyncEvents();
            if (witeForSend)
            {
                MessageFragment m = OutMessage.GetMessage();
                (e.UserToken as EventToken).MessageID = m.MessageIndex;
                GetSendBuffer().SetBuffer(e, m.Buffer.Length);
                Buffer.BlockCopy(m.Buffer, 0, e.Buffer, e.Offset, m.Buffer.Length);
            }
            //(e.UserToken as EventToken).ForceReceive = startReceive;
            if (!ClientSocket.ConnectAsync(e))
            {
                OnConnected(e);
            }
        }
        public void StartClient() { StartClient(true); }
        public virtual int Send(byte[] msg)
        {
            int i = PreparSendMessage(msg);
            if (!mbrJuestSended)
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
        //public virtual void StartRecevie(bool startReceive)
        //{
        //    //Console.Write(".");
        //    if (ClientSocket != null)
        //    {
        //        ReceviewLocker.WaitOne();
        //        SocketAsyncEventArgs e = GetReceiveAsyncEvents();
        //        (e.UserToken as EventsToken).ForceReceive = startReceive;
        //        ClientSocket.ReceiveAsync(e);
        //    }
        //}
        //public virtual void StartRecevie() { StartRecevie(true); }
        //public virtual void StartSend(bool startReceive)
        //{
        //    if (ClientSocket!=null)
        //    {
        //        SocketAsyncEventArgs e = GetSendAsyncEvents();
        //        (e.UserToken as EventsToken).ForceReceive = startReceive;
        //        ClientSocket.SendAsync(e);
        //        if (startReceive)
        //        {
        //            StartRecevie();
        //        }
        //    }
        //}
        //public virtual void StartSend() { StartSend(true); }
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
            SocketEventArgs a = new SocketEventArgs(e);
            //a.Buffer = new byte[e.BytesTransferred];
            //Buffer.BlockCopy(e.Buffer, e.Offset, a.Buffer, 0, e.BytesTransferred);
            //a.Remoter = e.RemoteEndPoint;
            //a.MessageIndex = (e.UserToken as EventToken).MessageID;
            fireEvent(evtSend, a);
            mbrJuestSended = true;
            //ClientSocket.Shutdown(SocketShutdown.Send);
            Send();
        }
        protected virtual void OnReceived(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred > 0)
            {
                SocketEventArgs a = new SocketEventArgs(e);
                //a.Buffer = new byte[e.BytesTransferred];
                //Buffer.BlockCopy(e.Buffer, e.Offset, a.Buffer, 0, e.BytesTransferred);
                //a.Remoter = e.RemoteEndPoint;
                //a.MessageIndex = (e.UserToken as EventToken).MessageID;
                IncommeMessage.PushMessage(a.Buffer, true);
                fireEvent(evtRecevie, a);
            }
            //ClientSocket.Shutdown(SocketShutdown.Receive);
            if (ClientSocket.Connected)
            {
                Receive();
            }
        }
        protected virtual void OnConnected(SocketAsyncEventArgs e)
        {
            SocketEventArgs a = new SocketEventArgs(e);
            //a.Remoter = ClientSocket.RemoteEndPoint;
            fireEvent(evtConnected, a);
            Receive();
        }
        protected virtual void OnError(SocketAsyncEventArgs x)
        {
            SocketErrorArgs r = new SocketErrorArgs(x);
            fireEvent(evtError, r);
        }
        protected virtual void Receive()
        {
            if (ClientSocket != null && ClientSocket.Connected)
            {
                SocketAsyncEventArgs e = GetReceiveAsyncEvents();
                GetRecevieBuffer().SetBuffer(e, Config.BufferSize);
                switch (Config.Protocol)
                {
                    case ProtocolType.Ggp:
                        break;
                    case ProtocolType.IP:
                        break;
                    //case ProtocolType.IPv6HopByHopOptions:
                    //    break;
                    //case ProtocolType.Unspecified:
                    //    break;
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
                        if (!ClientSocket.ReceiveAsync(e))
                        {
                            OnReceived(e);
                        }
                        break;
                    case ProtocolType.Udp:
                        if (!ClientSocket.ReceiveFromAsync(e))
                        {
                            OnReceived(e);
                        }
                        break;
                    case ProtocolType.Unknown:
                        break;
                    default:
                        break;
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
            switch (Config.Protocol)
            {
                case ProtocolType.Ggp:
                    break;
                case ProtocolType.IP:
                    break;
                //case ProtocolType.IPv6HopByHopOptions:
                //    break;
                //case ProtocolType.Unspecified:
                //    break;
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
                    if (!ClientSocket.SendAsync(e))
                    {
                        OnSended(e);
                    }
                    break;
                case ProtocolType.Udp:
                    if (!ClientSocket.SendToAsync(e))
                    {
                        OnSended(e);
                    }
                    break;
                case ProtocolType.Unknown:
                    break;
                default:
                    break;
            }
        }
    }
    public delegate void SocketEventHandler(object sender, SocketEventArgs e);
    public delegate void ServerSocketEventHandler(object sender, ServerSocketEventArgs e);
    public delegate void SockerErrorHandler(object sender, SocketErrorArgs e);
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
            if (Buffer != null)
            {
                for (int i = 0; i < Buffer.Length; i++)
                {
                    s = string.Format("{0}  {1}", s, (char)Buffer[i]);
                    b = string.Format("{0} {1:X2}", b, Buffer[i]);
                }
                b = string.Format("{0}{1}{2}", b, Environment.NewLine, s);
            }
            r = string.Format("{0}{1}{2}", r, Environment.NewLine, b);
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
}
