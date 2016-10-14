using System;
using TEArts.Etc.CollectionLibrary;

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
            mbrPooler.Pushin(m);
            return m.IDentity;
        }
        public MessageFragment GetMessage()
        {
            return mbrPooler.Popup();
        }
        public MessageFragment[] Messages { get { return mbrPooler.Items; } }
        #endregion
        internal void ForceClose()
        {
            mbrPooler.AbortWait();
        }
        public override string ToString()
        {
            string bit = string.Empty;
            MessageFragment[] ms = Messages;
            for (int i = 0; i < ms.Length; i++)
            {
                bit = string.Format("{0}{1}Index : {2}, Values : {1}{3}", bit, Environment.NewLine, i, ms[i]);
            }
            return string.Format("MessageFragment Count : {0}, Messages :{1}{2}", Count, Environment.NewLine, bit);
        }
    }
}
