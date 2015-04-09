﻿using System;

namespace TEArts.Networking.AsyncSocketer
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
            return mbrPooler.Pushin(m);
        }
        public MessageFragment GetMessage()
        {
            return mbrPooler.Popup();
        }
        #endregion
        internal void ForceClose()
        {
            mbrPooler.AbortWait();
        }
    }
}
