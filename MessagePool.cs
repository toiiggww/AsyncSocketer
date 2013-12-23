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
        public MessagePool(SocketConfigure cfg)
            : this()
        {
            Config = cfg;
            initPooler();
        }

        private void initPooler()
        {
            mbrPooler = new Pooler<MessageFragment>(Config.MaxBufferCount > 0 ? Config.MaxBufferCount : defaultMaxSize);
        }
        private int defaultMaxSize = 2 ^ 24;
        public MessagePool()
        {
            Config = SocketConfigure.Instance;
            initPooler();
        }
        #region Messager
        private Pooler<MessageFragment> mbrPooler;
        public SocketConfigure Config { get; set; }
        public int PushMessage(string msg)
        {
            return PushMessage(Config.Encoding.GetBytes(msg));
        }
        public int PushMessage(byte[] msg)
        {
            if (mbrPooler.CurrentSize == (Config.MaxBufferCount > 0 ? Config.MaxBufferCount : defaultMaxSize))
            {
                mbrPooler.Popup();
            }
            MessageFragment m;
            m = new MessageFragment();
            m.MessageIndex = mbrPooler.NextIndex;
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
