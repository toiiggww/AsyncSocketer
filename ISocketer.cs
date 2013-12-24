using System;
using System.Net;
using System.Net.Sockets;

namespace AsyncSocketer
{
    public abstract class ISocketer
    {
        public ISocketer(SocketConfigure cfg)
        {
            Config = cfg;
        }
        protected SocketConfigure Config { get; private set; }
        protected Socket ClientSocker { get; set; }
        public virtual bool Connect(SocketAsyncEventArgs e)
        {
            return ClientSocker.ConnectAsync(e);
        }
        public virtual bool Accept(SocketAsyncEventArgs e)
        {
            return ClientSocker.AcceptAsync(e);
        }
        public virtual bool Disconnect(SocketAsyncEventArgs e)
        {
            return ClientSocker.DisconnectAsync(e);
        }
        public virtual bool Receive(SocketAsyncEventArgs e)
        {
            return ClientSocker.ReceiveAsync(e);
        }
        public virtual bool Send(SocketAsyncEventArgs e)
        {
            return ClientSocker.SendAsync(e);
        }
        public static void CancelConnect(SocketAsyncEventArgs e)
        {
            Socket.CancelConnectAsync(e);
        }
        public static bool Connect(SocketType socketType, ProtocolType protocolType, SocketAsyncEventArgs e)
        {
            return Socket.ConnectAsync(socketType, protocolType, e);
        }
        public AddressFamily AddressFamily { get { return ClientSocker.AddressFamily; } }
        public bool Connected { get { return ClientSocker.Connected; } }
        internal void Shutdown(SocketShutdown socketShutdown)
        {
            ClientSocker.Shutdown(socketShutdown);
        }
        internal bool Bind(IPEndPoint iPEndPoint)
        {
            ClientSocker.Bind(iPEndPoint);
            return ClientSocker.IsBound;
        }
        internal void Listen(int port)
        {
            ClientSocker.Listen(port);
        }
    }
    public class TcpSocketer : ISocketer
    {
        public TcpSocketer(SocketConfigure cfg)
            : base(cfg)
        {
            ClientSocker = new Socket(Config.RemotePoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }
        public override bool Send(SocketAsyncEventArgs e)
        {
            return ClientSocker.Connected ? base.Send(e) : false;
        }
        public override bool Receive(SocketAsyncEventArgs e)
        {
            return ClientSocker.Connected ? base.Receive(e) : false;
        }
    }
    public class UdpSocketer : ISocketer
    {
        public UdpSocketer(SocketConfigure cfg)
            : base(cfg)
        {
            ClientSocker = new Socket(Config.RemotePoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        }
        public override bool Connect(SocketAsyncEventArgs e)
        {
            return Send(e);
        }
        public override bool Disconnect(SocketAsyncEventArgs e)
        {
            return Send(e);
        }
        public override bool Send(SocketAsyncEventArgs e)
        {
            return ClientSocker.SendToAsync(e);
        }
        public override bool Receive(SocketAsyncEventArgs e)
        {
            return ClientSocker.ReceiveFromAsync(e);
        }
    }
}
