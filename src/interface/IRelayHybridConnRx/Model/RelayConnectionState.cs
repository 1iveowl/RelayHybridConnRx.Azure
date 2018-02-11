using System;
using System.Collections.Generic;
using System.Text;

namespace IRelayHybridConnRx.Model
{
    public enum RelayListenerConnectionState
    {
        Offline,
        Connecting,
        Online,
        Listening,
        ExitingListener,
        Receiving
    }
}
