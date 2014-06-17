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
        private string mbrHostName;
        public string Host
        {
            get { return mbrHost; }
            set
            {
                if (mbrHost == null)
                {
                    mbrHost = "";
                }
                if (value.ToLower().Trim() != mbrHost.ToLower().Trim() && value.Trim() != string.Empty)
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
                if (!f)
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
                if (mbrPort != int.MinValue)
                {
                    mbrRemote = new IPEndPoint(mbrIP, mbrPort);
                }
            }
            catch // (Exception e)
            {
                throw new ArgumentNullException("Can't find host of " + value);
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
                mbrPort = value;
                if (mbrIP != null || resloveHost(mbrHost))
                {
                    mbrRemote = new IPEndPoint(mbrIP, value);
                }
            }
        }
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
        public string HostString
        {
            get { return string.Format("{0}:{1}", Host, Port); }
            set
            {
                string[] array = value.Trim().Split(new char[] { ':', ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (array.Length == 2)
                {
                    Host = array[0].Trim();
                    Port = int.Parse(array[1]);
                    mbrHostName = value.Trim();
                }
                else
                {
                    throw new ArgumentException("Host String {" + value + "} Error , it will be like [IP:Port|IP Port|IP,Port]");
                }
            }
        }
        public int MaxConnectCount { get; set; }
        public int TimeOut { get; set; }
        public int BufferSize { get; set; }
        public int BufferShard { get; set; }
        public int MaxDataConnection { get; set; }
        public int ListenPort { get; set; }
        public int ConnectBufferSize { get; set; }
        public bool OnErrorContinue { get; set; }
        public bool SendDataOnConnected { get; set; }
        public System.Net.Sockets.ProtocolType Protocol { get; set; }
        private Encoding mbrEncoding = Encoding.UTF8;
        public Encoding Encoding { get { return mbrEncoding; } set { if (mbrEncoding != value) mbrEncoding = value; } }
        public SocketConfigure()
        {
            IPAddress.TryParse("127.0.0.1", out mbrIP);
            MaxDataConnection = 32;
            BufferSize = 4916;
            OnErrorContinue = true;
            MaxConnectCount = 3;
            mbrPort = int.MinValue;
            SendDataOnConnected = true;
            ListenPort = 0;
            ConnectBufferSize = 4;
        }
    }
}
