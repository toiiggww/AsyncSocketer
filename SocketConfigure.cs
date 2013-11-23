using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace AsyncSocketer
{
    public class SocketConfigure
    {
        private string mbrHost;
        public string Host
        {
            get { return mbrHost; }
            set
            {
                if (value.ToLower().Trim() != mbrHost.ToLower().Trim() && value.Trim() != string.Empty)
                {
                    lock (mbrIP)
                    {
                        if (resloveHost(value.Trim()))
                        {
                            mbrHost = value.Trim();
                            if (mbrPort > 0 && mbrPort < 65535)
                            {
                                mbrRemote = new IPEndPoint(mbrIP, mbrPort);
                            }
                        }
                    }
                }
            }
        }
        private IPAddress mbrIP;
        private bool resloveHost(string value)
        {
            bool f = false;
            if (value == null || value.Trim() == string.Empty)
            {
                return false;
            }
            value = value.Trim();
            try
            {
                f = IPAddress.TryParse(value, out mbrIP);
            }
            catch // (Exception e)
            {
                try
                {
                    foreach (IPAddress ip in Dns.GetHostEntry(value).AddressList)
                    {
                        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            f = true;
                            mbrIP = ip;
                            break;
                        }
                    }
                }
                catch
                {
                    throw new ArgumentNullException("Can't find host of " + value);
                }
            }
            return f;
        }
        private int mbrPort;
        public int Port
        {
            get { return mbrPort; }
            set
            {
                if (value < 0 || value > 65535)
                {
                    throw new ArgumentOutOfRangeException();
                }
                lock (mbrRemote)
                {
                    mbrPort = value;
                    if (mbrIP != null || resloveHost(mbrHost))
                    {
                        mbrRemote = new IPEndPoint(mbrIP, value);
                    }
                }
            }
        }
        public int BufferSize { get; set; }
        public int BufferShard { get; set; }
        public int MaxBufferCount { get; set; }
        public int MaxDataConnection { get; set; }
        public int TotalClientConnection { get; set; }
        public int ReceiveMessagePerConnection { get; set; }
        public int SendMessagePerConnection { get; set; }
        public int EventCount { get; set; }
        public bool OnErrorContinue { get; set; }
        public System.Net.Sockets.ProtocolType Protocol { get; set; }
        private IPEndPoint mbrRemote;
        public IPEndPoint RemotePoint
        {
            get { return mbrRemote; }
            set
            {
                lock (mbrIP)
                {
                    mbrPort = value.Port;
                    mbrIP = value.Address;
                    mbrRemote = value;
                }
            }
        }
        private static SocketConfigure mbrInstance = new SocketConfigure();
        public static SocketConfigure Instance { get { return mbrInstance; } }
        private Encoding mbrEncoding = Encoding.UTF8;
        public Encoding Encoding { get { return mbrEncoding; } set { if (mbrEncoding != value) mbrEncoding = value; } }
        public SocketConfigure()
        {
            IPAddress.TryParse("127.0.0.1",out mbrIP);
            MaxDataConnection = 32;
            BufferSize = 4916;
            OnErrorContinue = true;
            Protocol = System.Net.Sockets.ProtocolType.Tcp;
            ConnectCount = 3;
            MaxBufferCount = 1024;
        }

        public int ConnectCount { get; set; }

        public int TimeOut { get; set; }
    }
}
