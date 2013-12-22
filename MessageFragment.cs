using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AsyncSocketer
{
    public class MessageFragment : IDentity
    {
        public byte[] Buffer { get; set; }
        public int MessageIndex { get; set; }
        public int IDentity { get; set; }
    }
}
