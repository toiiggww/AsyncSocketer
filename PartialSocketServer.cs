using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AsyncSocketer
{
    class PartialSocketServer:PartialSocket
    {
        protected object evtAccept;
        public event SocketEventHandler Accept { add { base.Events.AddHandler(evtAccept, value); } remove { base.Events.RemoveHandler(evtAccept, value); } }
    }
}
