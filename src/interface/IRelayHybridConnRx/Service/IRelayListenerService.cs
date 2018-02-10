using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IRelayHybridConnRx.Model;

namespace IRelayHybridConnRx.Service
{
    public interface IRelayListenerService
    {
        Task<(IObservable<RelayConnectionState> relayConnectionStateObservable, IObservable<string> message)> 
            RelayListenerObservable(
                string relayNamespace,
                string connectionName,
                string keyName,
                string key,
                TimeSpan? timeout = null);
    }
}
