using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace AsyncSocketer
{
    public class PartialSocket : EventSocketer
    {
        private EventPool mbrEventConnector, mbrEventSender, mbrEventRecevicer, mbrEventDisconnector;
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
        private EventPool InitEventPooler(EventPool pooler, BufferManager buffers, int length, SocketEvents fun)
        {
            if (pooler == null)
            {
                pooler = new EventPool(length);
                for (int i = 0; i < length; i++)
                {
                    SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                    e.RemoteEndPoint = Config.RemotePoint;
                    e.UserToken = new EventToken(pooler.NextTokenID, Config);
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
                            fun(x);
                        }
                        buffers.FreeBuffer(x);
                        pooler.Push(x);
                    };
                    //buffers.SetBuffer(e);
                    //GetConnectBuffer().SetBuffer(e, 4);
                    pooler.Push(e);
                }
            }
            return pooler;
        }
        protected override ISocketer CreateClientSocket()
        {
            if (Config.Protocol == ProtocolType.Tcp)
            {
                return new TcpSocketer(Config);
            }
            else if (Config.Protocol == ProtocolType.Udp)
            {
                return new UdpSocketer(Config);
            }
            return base.CreateClientSocket();
        }
        protected override SocketAsyncEventArgs GetConnectAsyncEvents()
        {
            SocketAsyncEventArgs s = InitEventPooler(mbrEventConnector, GetConnectBuffer(), Config.MaxConnectCount, OnConnected).Pop(Config);
            #region ///
            //if (mbrEventConnector == null)
            //{
            //    mbrEventConnector = new EventPool(Config.MaxDataConnection);
            //    for (int i = 0; i < Config.MaxDataConnection; i++)
            //    {
            //        SocketAsyncEventArgs e = new SocketAsyncEventArgs();
            //        e.RemoteEndPoint = Config.RemotePoint;
            //        e.UserToken = new EventToken(mbrEventConnector.NextTokenID, Config);
            //        e.Completed += (o, x) =>
            //            {
            //                if (x.SocketError != SocketError.Success)
            //                {
            //                    OnError(x);
            //                    if (!Config.OnErrorContinue)
            //                    {
            //                        return;
            //                    }
            //                }
            //                else
            //                {
            //                    OnConnected(x);
            //                }
            //                GetConnectBuffer().FreeBuffer(x);
            //                mbrEventConnector.Push(x);
            //            };
            //        GetConnectBuffer().SetBuffer(e);
            //        //GetConnectBuffer().SetBuffer(e, 4);
            //        mbrEventConnector.Push(e);
            //    }
            //}
            //s = mbrEventConnector.Pop(Config);
            //GetConnectBuffer().SetBuffer(s);
            #endregion
            return s;
        }
        protected override BufferManager GetConnectBuffer()
        {
            if (mbrBufferConnector == null)
            {
                mbrBufferConnector = new BufferManager(Config.MaxConnectCount);
            }
            return mbrBufferConnector;
        }
        protected override SocketAsyncEventArgs GetReceiveAsyncEvents()
        {
            SocketAsyncEventArgs s = InitEventPooler(mbrEventRecevicer, GetRecevieBuffer(), Config.MaxDataConnection, OnReceived).Pop(Config);
            #region ///
            //if (mbrEventRecevicer == null)
            //{
            //    mbrEventRecevicer = new EventPool(Config.MaxDataConnection);
            //    for (int i = 0; i < Config.MaxDataConnection; i++)
            //    {
            //        SocketAsyncEventArgs e = new SocketAsyncEventArgs();
            //        e.RemoteEndPoint = Config.RemotePoint;
            //        e.UserToken = new EventToken(mbrEventRecevicer.NextTokenID, Config);
            //        e.Completed += (o, x) =>
            //        {
            //            if (x.SocketError != SocketError.Success)
            //            {
            //                OnError(x);
            //                if (!Config.OnErrorContinue)
            //                {
            //                    return;
            //                }
            //            }
            //            else
            //            {
            //                OnReceived(x);
            //            }
            //            GetRecevieBuffer().FreeBuffer(x);
            //            mbrEventRecevicer.Push(x);
            //        };
            //        GetRecevieBuffer().SetBuffer(e);
            //        //GetRecevieBuffer().SetBuffer(e, 5);
            //        mbrEventRecevicer.Push(e);
            //    }
            //}
            //s = mbrEventRecevicer.Pop(Config);
            //GetConnectBuffer().SetBuffer(s);
            #endregion
            return s;
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
            SocketAsyncEventArgs s = InitEventPooler(mbrEventSender, GetSendBuffer(), Config.MaxDataConnection, OnSended).Pop(Config);
            #region ///
            //if (mbrEventSender == null)
            //{
            //    mbrEventSender = new EventPool(Config.MaxDataConnection);
            //    for (int i = 0; i < Config.MaxDataConnection; i++)
            //    {
            //        SocketAsyncEventArgs e = new SocketAsyncEventArgs();
            //        e.RemoteEndPoint = Config.RemotePoint;
            //        e.UserToken = new EventToken(mbrEventSender.NextTokenID, Config);
            //        e.Completed += (o, x) =>
            //        {
            //            if (x.SocketError != SocketError.Success)
            //            {
            //                OnError(x);
            //                if (!Config.OnErrorContinue)
            //                {
            //                    return;
            //                }
            //            }
            //            else
            //            {
            //                OnSended(x);
            //            }
            //            GetSendBuffer().FreeBuffer(x);
            //            mbrEventSender.Push(x);
            //        };
            //        GetSendBuffer().SetBuffer(e);
            //        //GetSendBuffer().SetBuffer(e, 3);
            //        mbrEventSender.Push(e);
            //    }
            //}
            //s = mbrEventSender.Pop(Config);
            //GetConnectBuffer().SetBuffer(s);
            #endregion
            return s;
        }
        protected override BufferManager GetSendBuffer()
        {
            if (mbrBufferSender == null)
            {
                mbrBufferSender = new BufferManager(Config.MaxDataConnection);
            }
            return mbrBufferSender;
        }
        protected override SocketAsyncEventArgs GetDisconnectAsyncEvents()
        {
            SocketAsyncEventArgs s = InitEventPooler(mbrEventDisconnector, GetDisonnectBuffer(), Config.MaxConnectCount, OnDisconnected).Pop(Config);
            #region ///
            //if (mbrEventDisconnector == null)
            //{
            //    mbrEventDisconnector = new EventPool(Config.MaxDataConnection);
            //    for (int i = 0; i < Config.MaxDataConnection; i++)
            //    {
            //        SocketAsyncEventArgs e = new SocketAsyncEventArgs();
            //        e.RemoteEndPoint = Config.RemotePoint;
            //        e.UserToken = new EventToken(mbrEventDisconnector.NextTokenID, Config);
            //        e.Completed += (o, x) =>
            //        {
            //            if (x.SocketError != SocketError.Success)
            //            {
            //                OnError(x);
            //                if (!Config.OnErrorContinue)
            //                {
            //                    return;
            //                }
            //            }
            //            else
            //            {
            //                OnDisconnected(x);
            //            }
            //            GetConnectBuffer().FreeBuffer(x);
            //            mbrEventDisconnector.Push(x);
            //        };
            //        GetConnectBuffer().SetBuffer(e);
            //        //GetConnectBuffer().SetBuffer(e, 4);
            //        mbrEventDisconnector.Push(e);
            //    }
            //}
            //s = mbrEventDisconnector.Pop(Config);
            //GetConnectBuffer().SetBuffer(s);
            #endregion
            return s;
        }
        protected override BufferManager GetDisonnectBuffer()
        {
            if (mbrBufferConnector == null)
            {
                mbrBufferConnector = new BufferManager(Config.MaxConnectCount);
            }
            return mbrBufferConnector;
        }
        protected override void ResetConnectAsyncEvents()
        {
            if (mbrEventConnector != null)
            {
                mbrEventConnector.ForceClose();
            }
        }
        protected override void ResetDisconnectAsyncEvents()
        {
            if (mbrEventDisconnector != null)
            {
                mbrEventDisconnector.ForceClose();
            }
        }
        protected override void ResetReceiveAsyncEvents()
        {
            if (mbrEventRecevicer != null)
            {
                mbrEventRecevicer.ForceClose();
            }
        }
        protected override void ResetSendAsyncEvents()
        {
            if (mbrEventSender != null)
            {
                mbrEventSender.ForceClose();
            }
        }
#if DEBUG
        protected override int EventPoolerSizeConnect { get { return mbrEventDisconnector == null ? -1 : mbrEventConnector.Count; } }
        protected override int EventPoolerSizeReceive { get { return mbrEventRecevicer == null ? -1 : mbrEventRecevicer.Count; } }
        protected override int EventPoolerSizeSend { get { return mbrEventSender == null ? -1 : mbrEventSender.Count; } }
        protected override int MessagePoolerSizeReceive { get { return mbrBufferRecevicer == null ? -1 : mbrBufferRecevicer.Count; } }
        protected override int MessagePoolerSizeSend { get { return mbrBufferSender == null ? -1 : mbrBufferSender.Count; } }
#endif
    }
}
