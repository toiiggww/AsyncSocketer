using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AsyncSocketer
{
    public class BufferManager
    {
        //public const int PageSize = 8192;
        public int Bytes { get; private set; }
        public byte[] Buffer { get; private set; }
        public Stack<int> BufferIndex { get; private set; }
        public int Index { get; private set; }
        public const int BufferSize = 8192;
        public BufferManager(int bytes, int size)
            : this()
        {
            Bytes = bytes;
            //BufferSize = size;
        }
        public BufferManager(int instance)
            : this()
        {
            Bytes = instance * BufferSize;
        }
        public BufferManager()
        {
            BufferIndex = new Stack<int>();
            Index = 0;
            //BufferSize = PageSize;
        }
        public bool SetBuffer(System.Net.Sockets.SocketAsyncEventArgs e)
        {
            return SetBuffer(e, BufferSize);
        }
        public bool SetBuffer(System.Net.Sockets.SocketAsyncEventArgs e, int size)
        {
            //e.SetBuffer(null, 0, 0);
            FreeBuffer(e);
            if (Buffer == null)
            {
                Buffer = new byte[Bytes];
            }
            if (BufferIndex.Count > 0)
            {
                e.SetBuffer(Buffer, BufferIndex.Pop(), size);
            }
            else
            {
                if (Bytes - size < Index)
                {
                    return false;
                }
                e.SetBuffer(Buffer, Index, size);
                Index += size;
            }
            return true;
        }
        public void FreeBuffer(System.Net.Sockets.SocketAsyncEventArgs e)
        {
            BufferIndex.Push(e.Offset);
            e.SetBuffer(null, 0, 0);
        }

        internal void Copy(byte[] b, System.Net.Sockets.SocketAsyncEventArgs ex)
        {
            int o = 0;
            if (b.Length > BufferSize)
            {
                o = BufferSize;
            }
            else
            {
                o = b.Length;
            }
            System.Buffer.BlockCopy(b, 0, Buffer, ex.Offset, o);
        }
    }
}
