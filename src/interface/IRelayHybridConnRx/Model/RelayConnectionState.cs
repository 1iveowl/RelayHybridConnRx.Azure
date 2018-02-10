using System;
using System.Collections.Generic;
using System.Text;

namespace IRelayHybridConnRx.Model
{
    public enum RelayConnectionState
    {
        Offline,
        Connecting,
        Online,
        Listening,
        ExitingListener
    }
}
