using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace AsyncSocketer
{
    public class PartialSocket : EventSocketer
    {
        private EventPool mbrEventConnector, mbrEventSender, mbrEventRecevicer;
        private BufferManager mbrBufferConnector, mbrBufferSender, mbrBufferRecevicer;
        protected PartialSocket()
        {
        }
        public PartialSocket(SocketConfigure sc)
            : base(sc)
        {
            //mbrBufferRecevicer = new BufferManager(Config.MaxDataConnection);
            //mbrBufferSender = new BufferManager(Config.MaxDataConnection);
            //mbrBufferConnector = new BufferManager(3);
            //Config = sc;
            //mbrEventConnector = new EventPool(Config.MaxDataConnection);
            //mbrEventRecevicer = new EventPool(Config.MaxDataConnection);
            //mbrEventSender = new EventPool(3);
        }
        protected override SocketAsyncEventArgs GetConnectAsyncEvents()
        {
            if (mbrEventConnector == null)
            {
                mbrEventConnector = new EventPool(Config.MaxDataConnection);
                for (int i = 0; i < Config.MaxDataConnection; i++)
                {
                    SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                    e.RemoteEndPoint = Config.RemotePoint;
                    e.UserToken = new EventToken(mbrEventConnector.NextTokenID, Config);
                    e.Completed += (o, x) =>
                        {
                            if (x.SocketError != SocketError.Success)
                            {
                                OnError(x);
                                if (!Config.OnErrorContinue)
                                {
                                    return;
                                }
                            }
                            else
                            {
                                OnConnected(x);
                            }
                            GetConnectBuffer().FreeBuffer(x);
                            mbrEventConnector.Push(x);
                        };
                    GetConnectBuffer().SetBuffer(e);
                    GetConnectBuffer().SetBuffer(e, 4);
                    mbrEventConnector.Push(e);
                }
            }
            return mbrEventConnector.Pop(Config);
        }
        protected override BufferManager GetConnectBuffer()
        {
            if (mbrBufferConnector == null)
            {
                mbrBufferConnector = new BufferManager(Config.ConnectCount);
            }
            return mbrBufferConnector;
        }
        protected override SocketAsyncEventArgs GetReceiveAsyncEvents()
        {
            if (mbrEventRecevicer == null)
            {
                mbrEventRecevicer = new EventPool(Config.MaxDataConnection);
                for (int i = 0; i < Config.MaxDataConnection; i++)
                {
                    SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                    e.RemoteEndPoint = Config.RemotePoint;
                    e.UserToken = new EventToken(mbrEventRecevicer.NextTokenID, Config);
                    e.Completed += (o, x) =>
                    {
                        Console.Write("/");
                        if (x.SocketError != SocketError.Success)
                        {
                            OnError(x);
                            if (!Config.OnErrorContinue)
                            {
                                return;
                            }
                        }
                        else
                        {
                            OnReceived(x);
                        }
                        mbrEventRecevicer.Push(x);
                        //ReceviewLocker.Reset();
                    };
                    GetRecevieBuffer().SetBuffer(e);
                    GetRecevieBuffer().SetBuffer(e, 5);
                    mbrEventRecevicer.Push(e);
                }
            }
            return mbrEventRecevicer.Pop(Config);
        }
        protected override BufferManager GetRecevieBuffer()
        {
            if (mbrBufferRecevicer == null)
            {
                mbrBufferRecevicer = new BufferManager(Config.MaxDataConnection);
            }
            return mbrBufferRecevicer;
        }
        protected override SocketAsyncEventArgs GetSendAsyncEvents()
        {
            if (mbrEventSender == null)
            {
                mbrEventSender = new EventPool(Config.MaxDataConnection);
                for (int i = 0; i < Config.MaxDataConnection; i++)
                {
                    SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                    e.RemoteEndPoint = Config.RemotePoint;
                    e.UserToken = new EventToken(mbrEventSender.NextTokenID, Config);
                    e.Completed += (o, x) =>
                    {
                        if (x.SocketError != SocketError.Success)
                        {
                            OnError(x);
                            if (!Config.OnErrorContinue)
                            {
                                return;
                            }
                        }
                        else
                        {
                            OnSended(x);
                        }
                    };
                    GetSendBuffer().SetBuffer(e);
                    GetSendBuffer().SetBuffer(e, 3);
                    mbrEventSender.Push(e);
                }
            }
            return mbrEventSender.Pop(Config);
        }
        protected override BufferManager GetSendBuffer()
        {
            if (mbrBufferSender == null)
            {
                mbrBufferSender = new BufferManager(Config.MaxDataConnection);
            }
            return mbrBufferSender;
        }
    }
}
