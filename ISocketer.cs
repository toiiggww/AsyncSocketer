using System;
using System.Net;
using System.Net.Sockets;

namespace TEArts.Networking.AsyncSocketer
{
    public class ISocketer
    {
        public ISocketer(SocketConfigure cfg)
        {
            Config = cfg;
        }
        public ISocketer(SocketConfigure cfg, Socket skt)
            : this(cfg)
        {
            if (skt == null)
            {
                ClientSocker = CreateSocket();
            }
            else
            {
                ClientSocker = skt;
            }
            SetTimeOut();
            //ClientSocker.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            ClientSocker.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, Config.SocketType == EventSocketType.Server);
        }
        protected SocketConfigure Config { get; private set; }
        public Socket ClientSocker { get;private set; }
        protected bool mbrSocketUnAvailable;
        public void SetTimeOut()
        {
            if (Config.TimeOut > 0)
            {
                ClientSocker.SendTimeout = Config.TimeOut * 1000;
                ClientSocker.ReceiveTimeout = Config.TimeOut * 1000;
            }
            LingerOption lin = new LingerOption(true, 60);
            //if (Config.Protocol == ProtocolType.Tcp)
            //{
            ClientSocker.LingerState = lin;
            //    //ClientSocker.SetSocketOption( SocketOptionLevel.Tcp, SocketOptionName)
            //}
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
            try
            {
                bool r = ClientSocker.DisconnectAsync(e);
                Shutdown(SocketShutdown.Both);
                ClientSocker = null;
                return r;
            }
            catch
            {
                return false;
            }
        }
        public virtual bool Receive(SocketAsyncEventArgs e)
        {
            return ClientSocker.ReceiveAsync(e);
        }
        public virtual bool Send(SocketAsyncEventArgs e)
        {
            return ClientSocker.SendAsync(e);
        }
        public AddressFamily AddressFamily { get { return ClientSocker.AddressFamily; } }
        public bool Connected { get { return ClientSocker != null && ClientSocker.Connected; } }
        public void Shutdown(SocketShutdown socketShutdown)
        {
            try { ClientSocker.Shutdown(socketShutdown); } catch { }
            ClientSocker.Close(20);
        }
        public virtual bool CanRead
        {
            get
            {
                if (Available == 0)
                {
                    try
                    {
                        mbrSocketUnAvailable = ClientSocker.Poll(100, SelectMode.SelectRead);
                    }
                    catch { mbrSocketUnAvailable = true; }
                }
                return Available > 0;
            }
        }
        public virtual bool CanWrite
        {
            get
            {
                try
                {
                    mbrSocketUnAvailable = ClientSocker.Poll(100, SelectMode.SelectWrite);
                }
                catch { mbrSocketUnAvailable = true; }
                return true;
            }
        }
        public virtual int Available { get { try { return ClientSocker == null ? -1 : ClientSocker.Available; } catch { return -2; } } }
        public virtual bool Bind(IPEndPoint iPEndPoint)
        {
            try
            {
                ClientSocker.Bind(iPEndPoint);
            }
            catch
            {
                try
                {
                    ClientSocker.Bind(new IPEndPoint(0, iPEndPoint.Port));
                }
                catch
                {
                    ClientSocker.Bind(new IPEndPoint(0, 0));
                }
            }
            return ClientSocker.IsBound;
        }
        public virtual void Listen(int blocking)
        {
            ClientSocker.Listen(blocking);
        }
        protected virtual Socket CreateSocket() { throw new NotImplementedException(); }
    }
    public class TcpSocketer : ISocketer
    {
        public TcpSocketer(SocketConfigure Config)
            : this(Config, null)
        {
        }
        public TcpSocketer(SocketConfigure cfg, Socket skt)
            : base(cfg, skt)
        {
        }
        protected override Socket CreateSocket()
        {
            return new Socket(Config.RemoteSocketPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }
        public static ISocketer CreateSocket(SocketConfigure Config)
        {
            return new TcpSocketer(Config);
        }
        public static ISocketer CreateSocket(SocketConfigure Config, Socket skt)
        {
            return new TcpSocketer(Config, skt);
        }
    }
    public class UdpSocketer : ISocketer
    {
        public UdpSocketer(SocketConfigure cfg)
            : this(cfg, null)
        {
        }
        public UdpSocketer(SocketConfigure cfg, Socket skt)
            : base(cfg, skt)
        {
        }
        public override bool CanRead
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
        public override bool Accept(SocketAsyncEventArgs e)
        {
            return ClientSocker.ReceiveFromAsync(e);
        }
        public override void Listen(int blocking)
        {
            //ClientSocker.ReceiveAsync(new SocketAsyncEventArgs());
        }
        protected override Socket CreateSocket()
        {
            return new Socket(Config.RemoteSocketPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        }
        public static ISocketer CreateSocket(SocketConfigure Config)
        {
            return new UdpSocketer(Config);
        }
        public static ISocketer CreateSocket(SocketConfigure Config, Socket skt)
        {
            return new UdpSocketer(Config, skt);
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
