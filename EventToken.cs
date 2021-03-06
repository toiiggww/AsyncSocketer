﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace TEArts.Networking.AsyncSocketer
{
    public class EventToken
    {
        public int SessionID { get; private set; }
        public int CurrentIndex { get; private set; }
        public int MessageID { get; set; }
        public int EventID { get; set; }
        public int BufferIndex { get; set; }
        private Queue<MessageFragment> Messages { get; set; }
        public SocketConfigure Config { get; private set; }
        public EventToken(int id, SocketConfigure cfg)
        {
            SessionID = id;
            Messages = new Queue<MessageFragment>();
            Config = cfg;
            BufferIndex = -1;
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
            MessageID = msg.IDentity;
            byte[] p = msg.Buffer;
            int i = p.Length, l, o;
            while (i > 0)
            {
                l = (i - Config.BufferSize > 0 ? Config.BufferSize : i);
                MessageFragment m = new MessageFragment();
                m.Buffer = new byte[l];
                m.IDentity = (CurrentIndex++);
                m.IDentity = SessionID;
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
            byte[][] bs = (from m in Messages orderby m.IDentity select m.Buffer).ToArray();
            for (int i = 0; i < bs.Length; i++)
            {
                Buffer.BlockCopy(bs[i], 0, r, l, bs[i].Length);
                l += bs[i].Length;
            }
            return r;
        }
        public override string ToString()
        {
            return string.Format("SessionID:[{0}],CurrentIndex:[{1}],MessageID:[{2}],EventID:[{3}]{4}", SessionID, CurrentIndex, MessageID, EventID, Environment.NewLine);
        }
        internal void Next(System.Net.Sockets.SocketAsyncEventArgs x)
        {
            MessageFragment m = new MessageFragment();
            m.Buffer = new byte[x.BytesTransferred];
            m.IDentity = (CurrentIndex++);
            m.IDentity = SessionID;
            Buffer.BlockCopy(x.Buffer, x.Offset, m.Buffer, 0, x.BytesTransferred);
            Messages.Enqueue(m);
        }
    }
}
