using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading;

namespace TEArts.Networking.AsyncSocketer
{
    public class PerformanceBase : Component
    {
        public PerformanceBase()
        {
            mbrPerformanceTimer = new Timer(
                (o) =>
                {
                    if (mbrPerformanceEnabled)
                    {
                        PerformanceCountArgs p = new PerformanceCountArgs();
                        p.ConnectFailed = mbrPConF;
                        p.Connectting = mbrPConT;
                        p.Disconnectting = mbrPDisC;
                        p.Errors = mbrPErrC;
                        p.MaxReceivedBytes = mbrPReceiveM;
                        p.MaxSendBytes = mbrPSendM;
                        p.ReceiveBytes = mbrPReceiveB;
                        p.ReceivedMessagesInPooler = mbrPImsgC;
                        p.ReceivePackages = mbrPReceiveC;
                        p.SendBytes = mbrPSendB;
                        p.SendMessagesInPooler = mbrPOmsgC;
                        p.SendPackages = mbrPSendC;
                        p.TotalReceiveBytes = mbrPReceiveL;
                        p.TotalReceivePackages = mbrPReceiveT;
                        p.TotalSendBytes = mbrPSendL;
                        p.TotalSendPackages = mbrPSendT;
                        mbrPSendB = 0;
                        mbrPSendC = 0;
                        mbrPReceiveB = 0;
                        mbrPReceiveC = 0;
                        fireEvent(evtPerformance, p);
                    }
                }, null, Timeout.Infinite, Timeout.Infinite);
        }
        protected virtual void fireEvent(object evt, object e)
        {
            if (evt != null)
            {
                object o = base.Events[evt];
                if (o != null)
                {
                    if (evt == evtPerformance)
                    {
                        (o as SocketPerformanceHandler)(this, (e as PerformanceCountArgs));
                    }
                }
            }
        }
        #region Performance
        private object evtPerformance;
        private Timer mbrPerformanceTimer;
        public int PerformanceInMin { get; set; }
        public int PerformanceAfterLimit { get; set; }
        public event SocketPerformanceHandler Performance { add { base.Events.AddHandler(evtPerformance, value); } remove { base.Events.RemoveHandler(evtPerformance, value); } }
        protected int
            mbrPImsgC,          // IncommeMessage Count
            mbrPOmsgC,          // OutMessage Count
            mbrPSendC,          // Send Count
            mbrPSendB,          // Bytes
            mbrPSendT,          // Total Count
            mbrPSendM,          // Max Package length
            mbrPSendL,          // Total Bytes
            mbrPReceiveC,       // Receve Count
            mbrPReceiveB,       // Bytes
            mbrPReceiveT,       // Total Count
            mbrPReceiveM,       // Max Package length
            mbrPReceiveL,       // Total Bytes
            mbrPConT,           // Total Connect
            mbrPConF,           // Connect fail
            mbrPDisC,           // Disconnect
            mbrPAcpC,           // Accept Count
            mbrPErrC;           // Errors
        protected bool mbrPerformanceEnabled;
        public bool EnablePerformance
        {
            set
            {
                if (mbrPerformanceEnabled == value)
                {
                    return;
                }
                mbrPerformanceEnabled = value;
                if (value)
                {
                    mbrPerformanceTimer.Change(60 * 1000 * (PerformanceInMin > 0 ? PerformanceInMin : 1), Timeout.Infinite);
                }
                else
                {
                    mbrPerformanceTimer.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }
            get
            {
                return mbrPerformanceEnabled;
            }
        }
        #endregion
    }
}
