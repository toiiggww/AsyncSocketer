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
        private BufferManager mbrBufferConnector, mbrBufferSender, mbrBufferRecevicer, mbrBufferDisconnector;
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
        protected override ISocketer CreateClientSocket()
        {
            if (Config.Protocol == ProtocolType.Tcp)
            {
                return TcpSocketer.CreateSocket(Config);
            }
            else if (Config.Protocol == ProtocolType.Udp)
            {
                return UdpSocketer.CreateSocket(Config);
            }
            return base.CreateClientSocket();
        }
        protected override EventPool GetConnectEventsPooler()
        {
            if (mbrEventConnector == null)
            {
                mbrEventConnector = new EventPool(Config.MaxConnectCount);
                mbrEventConnector.PoolerIdentity = mbrReceverLocker + " mbrEventConnector";
                InitEventPooler(mbrEventConnector, GetConnectBuffer(), Config.MaxConnectCount, OnConnected);
            }
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
            return mbrEventConnector;
        }
        protected override BufferManager GetConnectBuffer()
        {
            if (mbrBufferConnector == null)
            {
                mbrBufferConnector = new BufferManager(Config.MaxConnectCount,Config.ConnectBufferSize);
                mbrBufferConnector.ManagerIdentity = "mbrBufferConnector";
            }
            return mbrBufferConnector;
        }
        protected override EventPool GetReceiveEventsPooler()
        {
            if (mbrEventRecevicer == null)
            {
                mbrEventRecevicer = new EventPool(Config.MaxDataConnection);
                mbrEventRecevicer.PoolerIdentity = mbrReceverLocker + " mbrEventRecevicer";
                InitEventPooler(mbrEventRecevicer, GetRecevieBuffer(), Config.MaxDataConnection, OnReceived);
            }
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
            return mbrEventRecevicer;
        }
        protected override BufferManager GetRecevieBuffer()
        {
            if (mbrBufferRecevicer == null)
            {
                mbrBufferRecevicer = new BufferManager(Config.MaxDataConnection);
                mbrBufferRecevicer.ManagerIdentity = mbrReceverLocker + " mbrBufferRecevicer";
            }
            return mbrBufferRecevicer;
        }
        protected override EventPool GetSendEventsPooler()
        {
            if (mbrEventSender == null)
            {
                mbrEventSender = new EventPool(Config.MaxDataConnection);
                mbrEventSender.PoolerIdentity = mbrSenderLocker + " mbrEventSender";
                InitEventPooler(mbrEventSender, GetSendBuffer(), Config.MaxDataConnection, OnSended);
            }
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
            return mbrEventSender;
        }
        protected override BufferManager GetSendBuffer()
        {
            if (mbrBufferSender == null)
            {
                mbrBufferSender = new BufferManager(Config.MaxDataConnection);
                mbrBufferSender.ManagerIdentity = mbrSenderLocker + " mbrBufferSender";
            }
            return mbrBufferSender;
        }
        protected override EventPool GetDisconnectEventsPooler()
        {
            if (mbrEventDisconnector == null)
            {
                mbrEventDisconnector = new EventPool(Config.MaxDataConnection);
                mbrEventDisconnector.PoolerIdentity = " mbrEventDisconnector";
                InitEventPooler(mbrEventDisconnector, GetDisonnectBuffer(), Config.MaxConnectCount, OnDisconnected);
            }
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
            return mbrEventDisconnector;
        }
        protected override BufferManager GetDisonnectBuffer()
        {
            if (mbrBufferDisconnector == null)
            {
                mbrBufferDisconnector = new BufferManager(Config.MaxConnectCount,Config.ConnectBufferSize);
                mbrBufferDisconnector.ManagerIdentity = "mbrBufferConnector";
            }
            return mbrBufferDisconnector;
        }
        //protected override void ResetConnectAsyncEvents()
        //{
        //    if (mbrEventConnector != null)
        //    {
        //        mbrEventConnector.ForceClose();
        //    }
        //}
        //protected override void ResetDisconnectAsyncEvents()
        //{
        //    if (mbrEventDisconnector != null)
        //    {
        //        mbrEventDisconnector.ForceClose();
        //    }
        //}
        //protected override void ResetReceiveAsyncEvents()
        //{
        //    if (mbrEventRecevicer != null)
        //    {
        //        mbrEventRecevicer.ForceClose();
        //    }
        //}
        //protected override void ResetSendAsyncEvents()
        //{
        //    if (mbrEventSender != null)
        //    {
        //        mbrEventSender.ForceClose();
        //    }
        //}
#if DEBUG
        protected override int EventPoolerSizeConnect { get { return mbrEventDisconnector == null ? -1 : mbrEventConnector.Count; } }
        protected override int EventPoolerSizeReceive { get { return mbrEventRecevicer == null ? -1 : mbrEventRecevicer.Count; } }
        protected override int EventPoolerSizeSend { get { return mbrEventSender == null ? -1 : mbrEventSender.Count; } }
        protected override int MessagePoolerSizeReceive { get { return mbrBufferRecevicer == null ? -1 : mbrBufferRecevicer.Count; } }
        protected override int MessagePoolerSizeSend { get { return mbrBufferSender == null ? -1 : mbrBufferSender.Count; } }
#endif
    }
}
