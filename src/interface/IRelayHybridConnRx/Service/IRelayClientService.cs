using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IRelayHybridConnRx.Model;

namespace IRelayHybridConnRx.Service
{
    public interface IRelayClientService
    {
        Task<(IObservable<RelayClientConnectionState> relayConnectionStateObservable, IObservable<string> messageObservable)>
            RelayClintObservableAsync(
                string relayNamespace,
                string connectionName,
                string keyName,
                string key,
                TimeSpan? timeout = null);

        Task SendAsync(string message, TimeSpan? timeout = null);
    }
}
