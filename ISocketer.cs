﻿using System;
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
        public Socket ClientSocker { get; private set; }
        public bool IsClosed { get; private set; }
        protected bool mbrSocketUnAvailable;
        public void SetTimeOut()
        {
            //if (Config.TimeOut > 0)
            //{
            //    ClientSocker.SendTimeout = Config.TimeOut * 1000;
            //    ClientSocker.ReceiveTimeout = Config.TimeOut * 1000;
            //}
            //LingerOption lin = new LingerOption(true, 60);
            //ClientSocker.LingerState = lin;
        }
        public virtual bool? Connect(SocketAsyncEventArgs e)
        {
            //if (IsClosed)
            //{
            //    mbrSocketUnAvailable = true;
            //    return null;
            //}
            return ClientSocker.ConnectAsync(e);
        }
        public virtual bool? Accept(SocketAsyncEventArgs e)
        {
            if (IsClosed)
            {
                mbrSocketUnAvailable = true;
                return null;
            }
            return ClientSocker.AcceptAsync(e);
        }
        public virtual bool? Disconnect(SocketAsyncEventArgs e)
        {
            if (IsClosed)
            {
                return null;
            }
            else
            {
                IsClosed = true;
            }
            mbrSocketUnAvailable = true;
            return ClientSocker.DisconnectAsync(e);
        }
        public virtual bool? Receive(SocketAsyncEventArgs e)
        {
            if (IsClosed)
            {
                mbrSocketUnAvailable = true;
                return null;
            }
            return ClientSocker.ReceiveAsync(e);
        }
        public virtual bool? Send(SocketAsyncEventArgs e)
        {
            if (IsClosed)
            {
                mbrSocketUnAvailable = true;
                return null;
            }
            return ClientSocker.SendAsync(e);
        }
        public AddressFamily AddressFamily
        {
            get
            {
                try
                {
                    return ClientSocker == null ? AddressFamily.Unknown : ClientSocker.AddressFamily;
                }
                catch
                {
                    return AddressFamily.Unknown;
                }
            }
        }
        public bool Connected
        {
            get
            {
                try
                {
                    return ClientSocker == null ? false : ClientSocker.Connected;
                }
                catch
                {
                    return false;
                }
            }
        }
        public void Shutdown(SocketShutdown socketShutdown)
        {
            try
            {
                IsClosed = true;
                ClientSocker.Shutdown(socketShutdown);
                ClientSocker.Close();
            }
            catch { }
        }
        public virtual bool CanRead
        {
            get
            {
                try
                {
                    bool sr = ClientSocker.Poll(0, SelectMode.SelectRead),
                        se = ClientSocker.Poll(0, SelectMode.SelectError);
                    return sr;
                }
                catch { mbrSocketUnAvailable = true; }
                return mbrSocketUnAvailable;
            }
        }
        public virtual bool CanWrite
        {
            get
            {
                try
                {
                    mbrSocketUnAvailable = ClientSocker.Poll(0, SelectMode.SelectWrite);
                }
                catch { mbrSocketUnAvailable = true; }
                return true;
            }
        }
        public virtual int Available
        {
            get
            {
                try
                {
                    return ClientSocker == null || IsClosed ? -1 : ClientSocker.Available;
                }
                catch
                {
                    return -2;
                }
            }
        }
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
        public override bool? Connect(SocketAsyncEventArgs e)
        {
            return Send(e);
        }
        public override bool? Disconnect(SocketAsyncEventArgs e)
        {
            return Send(e);
        }
        public override bool? Send(SocketAsyncEventArgs e)
        {
            if (IsClosed)
            {
                mbrSocketUnAvailable = true;
                return null;
            }
            return ClientSocker.SendToAsync(e);
        }
        public override bool? Receive(SocketAsyncEventArgs e)
        {
            if (IsClosed)
            {
                mbrSocketUnAvailable = true;
                return null;
            }
            return ClientSocker.ReceiveFromAsync(e);
        }
        public override bool? Accept(SocketAsyncEventArgs e)
        {
            if (IsClosed)
            {
                mbrSocketUnAvailable = true;
                return null;
            }
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
