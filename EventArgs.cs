using System;
using System.Net;
using System.Net.Sockets;

namespace TEArts.Networking.AsyncSocketer
{
    public delegate void SocketEvents(SocketAsyncEventArgs e);
    public delegate void SocketEventHandler(object sender, SocketEventArgs e);
    public delegate void ServerSocketEventHandler(object sender, ServerSocketEventArgs e);
    public delegate void SocketErrorHandler(object sender, SocketErrorArgs e);
    public delegate void SocketPerformanceHandler(object sender, PerformanceCountArgs e);
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
                if (e.BytesTransferred > 0)
                {
                    Buffer = new byte[e.BytesTransferred];
                    System.Buffer.BlockCopy(e.Buffer, e.Offset, Buffer, 0, e.BytesTransferred);
                }
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
            r = string.Format("{0}{1}{2}", r, Environment.NewLine, FormatArrayMatrix(Buffer));
            return r;
        }
        public static string FormatArrayMatrix(byte[] array)
        {
            string r = "";
            if (r == null)
            {
                return r;
            }
#if DEBUG
            string b = "", s = "";
            r = string.Format("{0}{1}Index \\ Offset  ", r, Environment.NewLine);
            int i = 0, j = 0;
            for (; i < 16; i++)
            {
                r = string.Format("{0} _{1:X}", r, i);
            }
            r = r + " [_____string_____]" + Environment.NewLine;
            i = array.Length / 16;
            if (i > 0)
            {
                for (int k = 0; k < i; k++)
                {
                    for (j = k * 16; j < (((k + 1) * 16 > array.Length) ? array.Length - 1 : ((k + 1) * 16)); j++)
                    {
                        b = string.Format("{0} {1:X2}", b, array[j]);
                        s = string.Format("{0}{1}", s, (array[j] == 0 ? '.' : ((array[j] == 0x0a || array[j] == 0x0d || array[j] == 0x08 || array[j] == 0x09 || array[j] == 0x7f) ? '_' : (char)array[j])));
                    }
                    r = string.Format("{0}{1:X11}_ |  {2}  {3:50}{4}", r, k, b, s, Environment.NewLine);
                    b = "";
                    s = "";
                    if (i > 0 && k == i - 1)
                    {
                        for (j = (k + 1) * 16; j < array.Length; j++)
                        {
                            b = string.Format("{0} {1:X2}", b, array[j]);
                            s = string.Format("{0}{1}", s, (array[j] == 0 ? '.' : ((array[j] == 0x0a || array[j] == 0x0d || array[j] == 0x08 || array[j] == 0x09 || array[j] == 0x7f) ? '_' : (char)array[j])));
                        }
                        for (j = 0; j < (i + 1) * 16 - array.Length; j++)
                        {
                            b = string.Format("{0}   ", b);
                        }
                        k++;
                        r = string.Format("{0}{1:X11}_ |  {2}  {3:50}{4}", r, k, b, s, Environment.NewLine);
                    }
                }
            }
            else
            {
                for (int l = 0; l < array.Length; l++)
                {
                    b = string.Format("{0} {1:X2}", b, array[l]);
                    s = string.Format("{0}{1}", s, (array[l] == 0 ? '.' : ((array[l] == 0x0a || array[l] == 0x0d || array[l] == 0x08 || array[l] == 0x09 || array[l] == 0x7f) ? '_' : (char)array[l])));
                }
                for (int l = 0; l < 16-array.Length; l++)
                {
                    b = string.Format("{0}   ", b);
                }
                r = string.Format("{0}{1:X11}_ |  {2}  {3:50}{4}", r, 0, b, s, Environment.NewLine);
            }
#endif
            return r;
        }
    }
    public class ServerSocketEventArgs : SocketEventArgs
    {
        public ServerSocketEventArgs(SocketAsyncEventArgs e) : base(e) { }
        public EventSocketer AcceptSocket { get; set; }
    }
    public class SocketErrorArgs : EventArgs
    {
        public SocketError SocketError { get; set; }
        public Exception Exception { get; set; }
        public SocketAsyncOperation Operation { get; set; }
        public string Message { get; set; }
        public EndPoint RemoteEndPoint { get; set; }
        public override string ToString()
        {
            return string.Format("Error:[{0}]{1}On {2} with {3} as : {4}", Message, Environment.NewLine, Operation, SocketError, Exception);
        }
        public SocketErrorArgs() { }
        public SocketErrorArgs(SocketAsyncEventArgs e)
        {
            SocketError = e.SocketError;
            Operation = e.LastOperation;
            RemoteEndPoint = e.RemoteEndPoint;
        }
    }
    public class PerformanceCountArgs : EventArgs
    {
        public int SendPackages { get; set; }
        public int SendBytes { get; set; }
        public int TotalSendPackages { get; set; }
        public int TotalSendBytes { get; set; }
        public int ReceivePackages { get; set; }
        public int ReceiveBytes { get; set; }
        public int TotalReceivePackages { get; set; }
        public int TotalReceiveBytes { get; set; }
        public int MaxSendBytes { get; set; }
        public int MaxReceivedBytes { get; set; }
        public int SendMessagesInPooler { get; set; }
        public int ReceivedMessagesInPooler { get; set; }
        public int Connectting { get; set; }
        public int ConnectFailed { get; set; }
        public int Disconnectting { get; set; }
        public int Errors { get; set; }
        public TimeSpan MaxSendTime { get; set; }
        public TimeSpan MaxReceiveTime { get; set; }
    }
    public class ServerPerformanceCountArgs : PerformanceCountArgs
    {
        public int AcceptedCount { get; set; }
        public int AcceptedError { get; set; }
    }
}
