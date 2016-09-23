using System;
using System.Net;
using System.Text;

namespace TEArts.Networking.AsyncSocketer
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
        public bool PreBind { get; set; }
        public bool MoreInfo { get; set; }
        public bool ThreadMode { get; set; }
        public bool EnableTimeoutCheck { get; set; }
        public EventSocketType SocketType { get; set; }
        public System.Net.Sockets.ProtocolType Protocol { get; set; }
        public IPAddress IPAddress { get; set; }
        //public SocketEvents ConnectedCallBackForTransfer { get; set; }
        //public SocketEvents SendCallBackForTransfer { get; set; }
        //public SocketEvents ReceiveCallBackForTransfer { get; set; }
        //public SocketEvents DisconnectCallBackForTransfer { get; set; }
        public IPEndPoint LocalSocketPoint { get; set; }
        public IPEndPoint RemoteSocketPoint { get; set; }
        //{
        //    get
        //    {
        //        if (SocketType == EventSocketType.Server)
        //        {
        //            if (mbrLocalPoint != null && IPAddress != null && mbrLocalPoint.Address == IPAddress && mbrLocalPoint.Port == Port)
        //            {
        //                return mbrLocalPoint;
        //            }
        //            else if (IPAddress != null && -1 < Port && Port < 65535)
        //            {
        //                mbrLocalPoint = new IPEndPoint(IPAddress, Port);
        //            }
        //            else
        //            {
        //                throw new ArgumentNullException("RemoteAddress, RemotePort");
        //            }
        //        }
        //        else
        //        {
        //            Port = 0;
        //            if (mbrLocalPoint == null)
        //            {
        //                mbrLocalPoint = new IPEndPoint(IPAddress, Port);
        //            }
        //            else
        //            {
        //                mbrLocalPoint.Port = Port;
        //            }
        //        }
        //        return mbrLocalPoint;
        //    }
        //    set
        //    {
        //        mbrLocalPoint = value;
        //        if (SocketType == EventSocketType.Server)
        //        {
        //            Port = value.Port;
        //        }
        //        else
        //        {
        //            Port = 0;
        //        }
        //        IPAddress = value.Address;
        //    }
        //}
        private Encoding mbrEncoding = Encoding.UTF8;
        //private IPEndPoint mbrRemotePoint,mbrLocalPoint;
        public Encoding Encoding { get { return mbrEncoding; } set { if (mbrEncoding != value) mbrEncoding = value; } }
        public SocketConfigure()
        {
            AsyncSendReceiveEventInstance = 32;
            BufferSize = 4916;
            OnErrorContinue = true;
            AsyncConnectEventInstance = 3;
            SendDataOnConnected = true;
            //Port = 0;
            ConnectBufferSize = 4;
            SocketType = EventSocketType.Client;
            IPAddress = IPAddress.Any;
            PreBind = true;
            EnableTimeoutCheck = true;
            TimeOut = 300;
        }
    }
    public enum EventSocketType { Server = 1, Client=2, Both = 3, Accepted = 4 }
}
