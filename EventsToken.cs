using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AsyncSocketer
{
    class EventToken
    {
        public int SessionID { get; private set; }
        public int CurrentIndex { get; private set; }
        public int MessageID { get; set; }
        public bool ForceReceive { get; set; }
        private Queue<MessageFragment> Messages { get;set; }
        public SocketConfigure Config { get; private set; }
        public EventToken(int id,SocketConfigure cfg)
        {
            SessionID = id;
            Messages = new Queue<MessageFragment>();
            Config = cfg;
        }
        public MessageFragment Next()
        {
            if (Messages.Count > 0)
            {
                CurrentIndex++;
                return Messages.Dequeue();
            }
            return null;
        }
        public void Reset()
        {
            CurrentIndex = 0;
            Messages.Clear();
        }
        public void Reset(MessageFragment msg)
        {
            Reset();
            MessageID = msg.MessageIndex;
            byte[] p = msg.Buffer;
            int i = p.Length,l,o;
            while (i > 0)
            {
                l = (i - Config.BufferSize > 0 ? Config.BufferSize : i);
                MessageFragment m = new MessageFragment();
                m.Buffer = new byte[l];
                m.MessageIndex = (CurrentIndex++);
                m.SessionID = SessionID;
                o = p.Length - i;
                Buffer.BlockCopy(p, o, m.Buffer, 0, l);
                Messages.Enqueue(m);
                i -= Config.BufferSize;
            }
        }
        internal byte[] Concate()
        {
            if (Messages.Count == 0)
            {
                return null;
            }
            if (Messages.Count == 0)
            {
                return Messages.Dequeue().Buffer;
            }
            int l = 0;
            foreach (MessageFragment m in Messages)
            {
                l += m.Buffer.Length;
            }
            byte[] r = new byte[l];
            l = 0;
            byte[][] bs = (from m in Messages orderby m.MessageIndex select m.Buffer).ToArray();
            for (int i = 0; i < bs.Length; i++)
            {
                Buffer.BlockCopy(bs[i], 0, r, l, bs[i].Length);
                l += bs[i].Length;
            }
            return r;
        }

        internal void Next(System.Net.Sockets.SocketAsyncEventArgs x)
        {
            MessageFragment m = new MessageFragment();
            m.Buffer = new byte[x.BytesTransferred];
            m.MessageIndex = (CurrentIndex++);
            m.SessionID = SessionID;
            Buffer.BlockCopy(x.Buffer, x.Offset, m.Buffer, 0, x.BytesTransferred);
            Messages.Enqueue(m);
        }
    }
}
