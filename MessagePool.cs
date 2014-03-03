using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace AsyncSocketer
{
    public class MessagePool
    {
        public MessagePool(int mx)
        {
            defaultMaxSize = mx;
            initPooler();
        }

        private void initPooler()
        {
            mbrPooler = new Pooler<MessageFragment>(defaultMaxSize);
        }
        private int defaultMaxSize;//= 2 ^ 24;
        public MessagePool()
            : this(2 ^ 24)
        {
        }
        public int Count { get { return mbrPooler.CurrentSize; } }
        #region Messager
        private Pooler<MessageFragment> mbrPooler;
        //public int PushMessage(string msg)
        //{
        //    return PushMessage(Config.Encoding.GetBytes(msg));
        //}
        public int PushMessage(byte[] msg)
        {
            if (mbrPooler.CurrentSize == defaultMaxSize)
            {
                mbrPooler.Popup();
            }
            MessageFragment m;
            m = new MessageFragment();
            m.IDentity = mbrPooler.NextIndex;
            m.Buffer = new byte[msg.Length];
            Buffer.BlockCopy(msg, 0, m.Buffer, 0, msg.Length);
            //lock (mbrPooler)
            //{
            Debuger.DebugInfo("mbrPooler.Count:" + mbrPooler.CurrentSize.ToString());
            return mbrPooler.Pushin(m);
            //}
        }
        public MessageFragment GetMessage()
        {
            //lock (mbrPooler)
            //{
                return mbrPooler.Popup();
            //}
        }
        #endregion
        internal void ForceClose()
        {
            mbrPooler.AbortWait();
        }
    }
}
