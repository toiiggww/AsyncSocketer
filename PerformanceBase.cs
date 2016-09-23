using System.ComponentModel;
using System.Threading;
using TEArts.Etc.CollectionLibrary;

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
            //if (e != null)
            //{
            //    Debuger.Loger.DebugInfo(DebugType.FuncationCall, e.ToString());
            //}
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
            /// <summary>
            /// // IncommeMessage Count
            /// </summary>
            mbrPImsgC,          // IncommeMessage Count
            /// <summary>
            /// // OutMessage Count
            /// </summary>
            mbrPOmsgC,          // OutMessage Count
            /// <summary>
            /// // Send Count
            /// </summary>
            mbrPSendC,          // Send Count
            /// <summary>
            /// // Bytes
            /// </summary>
            mbrPSendB,          // Bytes
            /// <summary>
            /// // Total Count
            /// </summary>
            mbrPSendT,          // Total Count
            /// <summary>
            /// // Max Package length
            /// </summary>
            mbrPSendM,          // Max Package length
            /// <summary>
            /// // Total Bytes
            /// </summary>
            mbrPSendL,          // Total Bytes
            /// <summary>
            /// // Receve Count
            /// </summary>
            mbrPReceiveC,       // Receve Count
            /// <summary>
            /// // Bytes
            /// </summary>
            mbrPReceiveB,       // Bytes
            /// <summary>
            /// // Total Count
            /// </summary>
            mbrPReceiveT,       // Total Count
            /// <summary>
            /// // Max Package length
            /// </summary>
            mbrPReceiveM,       // Max Package length
            /// <summary>
            /// // Total Bytes
            /// </summary>
            mbrPReceiveL,       // Total Bytes
            /// <summary>
            /// // Total Connect
            /// </summary>
            mbrPConT,           // Total Connect
            /// <summary>
            /// // Connect fail
            /// </summary>
            mbrPConF,           // Connect fail
            /// <summary>
            /// // Disconnect
            /// </summary>
            mbrPDisC,           // Disconnect
            /// <summary>
            /// // Accept Count
            /// </summary>
            mbrPAcpC,           // Accept Count
            /// <summary>
            /// // Errors
            /// </summary>
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
