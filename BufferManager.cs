using System.Collections.Generic;

namespace TEArts.Networking.AsyncSocketer
{
    public class BufferManager
    {
        public int Bytes { get; private set; }
        public byte[] Buffer { get; private set; }
        public Stack<int> BufferIndex { get; private set; }
        public int Index { get; private set; }
        public const int BufferSize = 8192;
        public int MaxCount { get; private set; }
        public string ManagerIdentity { get; set; }
        private int BufferedCount { get; set; }
        public int FragmentSize { get; private set; }
        public BufferManager(int instance)
            : this(instance, -1)
        {
        }
        public BufferManager(int instance,int fragmentsize)
            : this()
        {
            FragmentSize = (fragmentsize < 1 ? BufferSize : fragmentsize);
            Bytes = instance * FragmentSize;
            MaxCount = instance;
            Buffer = new byte[Bytes];
        }
        private BufferManager()
        {
            BufferIndex = new Stack<int>();
            Index = 0;
            BufferedCount = 0;
        }
        public bool SetBuffer(System.Net.Sockets.SocketAsyncEventArgs e)
        {
            if (BufferedCount < MaxCount && (e.UserToken as EventToken).BufferIndex < 0)
            {
                e.SetBuffer(Buffer, Index, FragmentSize);
                Index += FragmentSize;
                (e.UserToken as EventToken).BufferIndex = BufferedCount;
                BufferedCount++;
                return true;
            }
            return false;
        }
        //public bool SetBuffer(System.Net.Sockets.SocketAsyncEventArgs e, int size)
        //{
        //    if (Buffer == null)
        //    {
        //        Buffer = new byte[Bytes];
        //    }
        //    if (BufferIndex.Count > 0)
        //    {
        //        e.SetBuffer(Buffer, BufferIndex.Pop(), BufferSize);
        //    }
        //    else
        //    {
        //        if (Index + BufferSize > Bytes)
        //        {
        //            return false;
        //        }
        //        e.SetBuffer(Buffer, Index, 20);
        //        Index += BufferSize;
        //    }
        //    return true;
        //}
        //public void FreeBuffer(System.Net.Sockets.SocketAsyncEventArgs e)
        //{
        //    BufferIndex.Push(e.Offset);
        //    //e.SetBuffer(null, 0, 0);
        //    //SetBuffer(e, 1);
        //}

        //internal void Copy(byte[] b, System.Net.Sockets.SocketAsyncEventArgs ex)
        //{
        //    int o = 0;
        //    if (b.Length > BufferSize)
        //    {
        //        o = BufferSize;
        //    }
        //    else
        //    {
        //        o = b.Length;
        //    }
        //    System.Buffer.BlockCopy(b, 0, Buffer, ex.Offset, o);
        //}

        public int Count { get { return 0 - BufferIndex.Count; } }
    }
}
