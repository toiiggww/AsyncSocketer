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
        protected bool mbrSocketUnAvailable;
        public void SetTimeOut()
        {
            if (Config.TimeOut > 0)
            {
                ClientSocker.SendTimeout = Config.TimeOut * 1000;
                ClientSocker.ReceiveTimeout = Config.TimeOut * 1000;
            }
        }
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
        public void Shutdown(SocketShutdown socketShutdown)
        {
            ClientSocker.Shutdown(socketShutdown);
            ClientSocker.Close();
        }
        public virtual bool SocketUnAvailable
        {
            get
            {
                if (Available == 0)
                {
                    try
                    {
                        mbrSocketUnAvailable = ClientSocker.Poll(10, SelectMode.SelectRead);
                    }
                    catch { mbrSocketUnAvailable = true; }
                }
                return mbrSocketUnAvailable;
            }
        }
        public virtual int Available { get { return ClientSocker.Available; } }
        public bool Bind(EndPoint iPEndPoint)
        {
            ClientSocker.Bind(iPEndPoint);
            return ClientSocker.IsBound;
        }
        public void Listen(int port)
        {
            ClientSocker.Listen(port);
        }

        public static ISocketer CreateSocket(SocketConfigure Config)
        {
            throw new NotImplementedException();
        }
    }
    public class TcpSocketer : ISocketer
    {
        public TcpSocketer(SocketConfigure cfg)
            : base(cfg)
        {
            ClientSocker = new Socket(Config.RemotePoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            SetTimeOut();
        }
        public override bool Send(SocketAsyncEventArgs e)
        {
            return ClientSocker.Connected ? base.Send(e) : false;
        }
        public override bool Receive(SocketAsyncEventArgs e)
        {
            return ClientSocker.Connected ? base.Receive(e) : false;
        }
        public static ISocketer CreateSocket(SocketConfigure Config)
        {
            return new TcpSocketer(Config);
        }
    }
    public class UdpSocketer : ISocketer
    {
        public UdpSocketer(SocketConfigure cfg)
            : base(cfg)
        {
            ClientSocker = new Socket(Config.RemotePoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            SetTimeOut();
        }
        public override bool Connect(SocketAsyncEventArgs e)
        {
            mbrSocketUnAvailable = false;
            return Send(e);
        }
        public override bool Disconnect(SocketAsyncEventArgs e)
        {
            mbrSocketUnAvailable = true;
            return Send(e);
        }
        public override bool Send(SocketAsyncEventArgs e)
        {
            return ClientSocker.SendToAsync(e);
        }
        public override bool Receive(SocketAsyncEventArgs e)
        {
            e.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            return ClientSocker.ReceiveFromAsync(e);
        }
        public override bool SocketUnAvailable
        {
            get
            {
                if (mbrSocketUnAvailable)
                {
                    mbrSocketUnAvailable = false;
                }
                System.Threading.Thread.Sleep(5);
                return mbrSocketUnAvailable;
            }
        }
        public static ISocketer CreateSocket(SocketConfigure Config)
        {
            return new UdpSocketer(Config);
        }
        public override int Available
        {
            get
            {
                return 1;
            }
        }
    }
}
