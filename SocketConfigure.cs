using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace AsyncSocketer
{
    public class SocketConfigure
    {
        public int MaxConnectCount { get; set; }
        public int TimeOut { get; set; }
        public int BufferSize { get; set; }
        public int BufferShard { get; set; }
        public int MaxDataConnection { get; set; }
        public int ListenPort { get; set; }
        public int RemotePort { get; set; }
        public int ConnectBufferSize { get; set; }
        public bool OnErrorContinue { get; set; }
        public bool SendDataOnConnected { get; set; }
        public EventSocketType SocketType { get; set; }
        public System.Net.Sockets.ProtocolType Protocol { get; set; }
        public IPAddress ListenAddress { get; set; }
        public IPAddress RemoteAddress { get; set; }
        public IPEndPoint RemotePoint
        {
            get
            {
                if (mbrRemotePoint != null && RemoteAddress != null && mbrRemotePoint.Address == RemoteAddress && mbrRemotePoint.Port == RemotePort)
                {
                    return mbrRemotePoint;
                }
                else if (RemoteAddress != null && 0 < RemotePort && RemotePort < 65535)
                {
                    mbrRemotePoint = new IPEndPoint(RemoteAddress, RemotePort);
                    return mbrRemotePoint;
                }
                else
                {
                    throw new ArgumentNullException("RemoteAddress, RemotePort");
                }
            }
            set
            {
                mbrRemotePoint = value;
                RemotePort = value.Port;
                RemoteAddress = value.Address;
            }
        }
        public IPEndPoint LocalPoint
        {
            get
            {
                if (mbrLocalPoint != null && ListenAddress != null && mbrLocalPoint.Address == ListenAddress && mbrLocalPoint.Port == ListenPort)
                {
                    return mbrLocalPoint;
                }
                else if (ListenAddress != null && 0 < ListenPort && ListenPort < 65535)
                {
                    mbrLocalPoint = new IPEndPoint(ListenAddress, ListenPort);
                    return mbrLocalPoint;
                }
                else
                {
                    throw new ArgumentNullException("RemoteAddress, RemotePort");
                }
            }
            set
            {
                mbrLocalPoint = value;
                ListenPort = value.Port;
                ListenAddress = value.Address;
            }
        }
        private Encoding mbrEncoding = Encoding.UTF8;
        private IPEndPoint mbrRemotePoint,mbrLocalPoint;
        public Encoding Encoding { get { return mbrEncoding; } set { if (mbrEncoding != value) mbrEncoding = value; } }
        public SocketConfigure()
        {
            MaxDataConnection = 32;
            BufferSize = 4916;
            OnErrorContinue = true;
            MaxConnectCount = 3;
            SendDataOnConnected = true;
            ListenPort = 0;
            ConnectBufferSize = 4;
            SocketType = EventSocketType.Client;
            ListenAddress = IPAddress.Any;
        }
    }
    public enum EventSocketType { Server, Client }
}
