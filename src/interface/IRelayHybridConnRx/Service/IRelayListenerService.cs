using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using IRelayHybridConnRx.Model;

namespace IRelayHybridConnRx.Service
{
    public interface IRelayListenerService
    {
        Task<(IObservable<RelayListenerConnectionState> relayConnectionStateObservable, IObservable<IMessage> messageObservable)> 
            RelayListenerInitializeAsync(
                string relayNamespace,
                string connectionName,
                string keyName,
                string key,
                TimeSpan? timeout = null);
    }
}
