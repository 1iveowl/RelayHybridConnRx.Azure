using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IRelayHybridConnRx.Model;

namespace IRelayHybridConnRx.Service
{
    public interface IRelayClientService
    {
        Task<IObservable<string>>
            RelayClintObservableAsync(
                string relayNamespace,
                string connectionName,
                string keyName,
                string key,
                TimeSpan? timeout = null);

        Task SendAsync(string message);
    }
}
