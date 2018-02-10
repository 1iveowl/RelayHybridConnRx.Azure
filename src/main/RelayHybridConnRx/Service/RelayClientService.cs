using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IRelayHybridConnRx.Model;
using IRelayHybridConnRx.Service;
using Microsoft.Azure.Relay;

namespace RelayHybridConnRx.Service
{
    public class RelayClientService : IRelayClientService
    {
        public async Task<(IObservable<RelayClientConnectionState> relayConnectionStateObservable, IObservable<string> messageObservable)> 
            RelayClintObservableAsync(
                string relayNamespace, 
                string connectionName, 
                string keyName, 
                string key,
                TimeSpan? timeout = null)
        {
            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(keyName, key);
            var client = new HybridConnectionClient(new Uri($"sb://{relayNamespace}/{connectionName}"), tokenProvider);

            // Initiate the connection.
            var relayConnection = await client.CreateConnectionAsync();

            throw new NotImplementedException();
        }

        public async Task SendAsync(string message, TimeSpan? timeout = null)
        {
            throw new NotImplementedException();
        }
    }
}
