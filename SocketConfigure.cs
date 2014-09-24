using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace AsyncSocketer
{
    public class SocketConfigure
    {
        public int AsyncConnectEventInstance { get; set; }
        public int MaxConnectCount { get; set; }
        public int TimeOut { get; set; }
        public int BufferSize { get; set; }
        public int BufferShard { get; set; }
        public int AsyncSendReceiveEventInstance { get; set; }
        public int Port { get; set; }
        public int ConnectBufferSize { get; set; }
        public bool OnErrorContinue { get; set; }
        public bool SendDataOnConnected { get; set; }
        public EventSocketType SocketType { get; set; }
        public System.Net.Sockets.ProtocolType Protocol { get; set; }
        public IPAddress IPAddress { get; set; }
        public IPEndPoint SocketPoint
        {
            get
            {
                if (mbrLocalPoint != null && IPAddress != null && mbrLocalPoint.Address == IPAddress && mbrLocalPoint.Port == Port)
                {
                    return mbrLocalPoint;
                }
                else if (IPAddress != null && -1 < Port && Port < 65535)
                {
                    mbrLocalPoint = new IPEndPoint(IPAddress, Port);
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
                Port = value.Port;
                IPAddress = value.Address;
            }
        }
        private Encoding mbrEncoding = Encoding.UTF8;
        private IPEndPoint mbrRemotePoint,mbrLocalPoint;
        public Encoding Encoding { get { return mbrEncoding; } set { if (mbrEncoding != value) mbrEncoding = value; } }
        public SocketConfigure()
        {
            AsyncSendReceiveEventInstance = 32;
            BufferSize = 4916;
            OnErrorContinue = true;
            AsyncConnectEventInstance = 3;
            SendDataOnConnected = false;
            Port = 0;
            ConnectBufferSize = 4;
            SocketType = EventSocketType.Client;
            IPAddress = IPAddress.Any;
        }
    }
    public enum EventSocketType { Server = 1, Client=2, Both = 3 }
}
