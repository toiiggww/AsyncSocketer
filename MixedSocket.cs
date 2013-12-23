using System;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AsyncSocketer
{
    public class MixedSocket : EventSocketer
    {
        public MixedSocket(SocketConfigure sc)
        {
            if (sc == null)
            {
                Config = SocketConfigure.Instance;
            }
            else
            {
                Config = sc;
            }
            SocketBuffer = new BufferManager(Config.MaxDataConnection);
            SocketPooler = new EventPool(Config.MaxDataConnection);
            //IncommeMessage.Config = Config;
            OutMessage.Config = Config;
            ClientSocket = new ISocketer(Config);
            for (int i = 0; i < Config.MaxDataConnection; i++)
            {
                SocketPooler.Push(NewSocket());
            }
        }
        private BufferManager SocketBuffer { get; set; }
        private EventPool SocketPooler { get; set; }
        private SocketAsyncEventArgs NewSocket()
        {
            SocketAsyncEventArgs e = new SocketAsyncEventArgs();
            //e.RemoteEndPoint = Config.RemotePoint;
            e.UserToken = new EventToken(SocketPooler.NextTokenID, Config);
            SocketBuffer.SetBuffer(e);
            e.Completed += (o, x) =>
                {
                    if (x.SocketError != SocketError.Success)
                    {
                        SocketErrorArgs err = new SocketErrorArgs(x);
                        lock (err)
                        {
                            fireEvent(evtError, err);
                        }
                        if (!Config.OnErrorContinue)
                        {
                            SocketBuffer.FreeBuffer(x);
                            SocketPooler.Push(x);
                            return;
                        }
                    }
                    SocketEventArgs a = new SocketEventArgs(x);
                    //a.SocketStatus = x.SocketError;
                    //a.Remoter = x.RemoteEndPoint;
                    //a.Buffer = new byte[x.BytesTransferred];
                    //Buffer.BlockCopy(x.Buffer, 0, a.Buffer, 0, x.BytesTransferred);
                    EventToken t = x.UserToken as EventToken;
                    if (x.LastOperation == SocketAsyncOperation.Connect)
                    {
                        //ThreadRecevie(x);
                        //ThreadSend(x);
                        //startReceive(x);
                        //startSend(x);
                    }
                    else if (x.LastOperation == SocketAsyncOperation.Send)
                    {
                        if (x.BytesTransferred == Config.BufferSize)
                        {
                            MessageFragment m = t.Next();
                            if (m == null || m.Buffer == null)
                            {
                                RecycleSocket(x);
                                return;
                            }
                            Buffer.BlockCopy(m.Buffer, 0, x.Buffer, 0, m.Buffer.Length);
                            x.AcceptSocket.SendAsync(x);
                            return;
                        }
                        else
                        {
                            //a.MessageIndex = (x.UserToken as EventToken).MessageID;
                            fireEvent(evtSend, a);
                        }
                    }
                    else if (x.LastOperation == SocketAsyncOperation.Receive)
                    {
                        ( x.UserToken as EventToken ).Next(x);
                        if (x.BytesTransferred == Config.BufferSize)
                        {
                            x.AcceptSocket.ReceiveAsync(x);
                        }
                        else
                        {
                            a.Buffer = ( x.UserToken as EventToken ).Concate();
                            fireEvent(evtRecevie, a);
                        }
                    }
                    else if (x.LastOperation == SocketAsyncOperation.Disconnect)
                    {
                        ClientSocket.Shutdown(SocketShutdown.Both);
                        fireEvent(evtDisconnected, a);
                    }
                    //else
                    //{
                    //    a.Buffer = new byte[1];
                    //    a.Buffer[0] = (byte)(x.LastOperation);
                    //}
                    RecycleSocket(x);
                };
            return e;
        }
        private void RecycleSocket(SocketAsyncEventArgs x)
        {
            ( x.UserToken as EventToken ).Reset();
            SocketBuffer.FreeBuffer(x);
            SocketPooler.Push(x);
        }
        protected override BufferManager GetConnectBuffer()
        {
            return SocketBuffer;
        }
    }
}
