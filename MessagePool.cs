using System;
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
        }
        public MessagePool()
        {
            Config = SocketConfigure.Instance;
            Messages = new Queue<MessageFragment>();
            MessageLocker = new AutoResetEvent(false);
            MessageIndex = int.MinValue + 1;
        }
        #region Messager
        private Queue<MessageFragment> Messages { get; set; }
        private AutoResetEvent MessageLocker { get; set; }
        private int MessageIndex;
        private bool mbrForceClose;
        public SocketConfigure Config { get; set; }
        public int PushMessage(string msg)
        {
           return PushMessage(Config.Encoding.GetBytes(msg),false);
        }
        public int PushMessage(byte[] msg)
        {
            return PushMessage(msg, false);
        }
        public int PushMessage(byte[] msg, bool check)
        {
            if (check && Config.MaxBufferCount > 0 && Messages.Count >= Config.MaxBufferCount)
            {
                Messages.Dequeue();
            }
            MessageFragment m;
            m = new MessageFragment();
            Interlocked.CompareExchange(ref MessageIndex, int.MinValue + 1, int.MaxValue);
            m.MessageIndex = Interlocked.Increment(ref MessageIndex);
            m.Buffer = new byte[msg.Length];
            Buffer.BlockCopy(msg, 0, m.Buffer, 0, msg.Length);
            Messages.Enqueue(m);
            if (Messages.Count == 1)
            {
                MessageLocker.Set();
            }
            return m.MessageIndex;
        }
        public MessageFragment GetMessage()
        {
            while (Messages.Count == 0)
            {
                MessageLocker.WaitOne();
                if (mbrForceClose)
                {
                    return null;
                }
                //MessageLocker.Reset();
            }
            return Messages.Dequeue();
        }
        #endregion
        internal void ForceClose()
        {
            MessageLocker.Set();
            mbrForceClose = true;
        }
    }
}
