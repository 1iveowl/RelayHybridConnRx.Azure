using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IRelayHybridConnRx.Model;
using IRelayHybridConnRx.Service;
using Microsoft.Azure.Relay;

namespace RelayHybridConnRx.Service
{
    public class RelayClientService : IRelayClientService
    {
        private HybridConnectionStream _relayConnection;

        public async Task<IObservable<string>> 
            RelayClintObservableAsync(
                string relayNamespace, 
                string connectionName, 
                string keyName, 
                string key)
        {

            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(keyName, key);
            var client = new HybridConnectionClient(new Uri($"sb://{relayNamespace}/{connectionName}"), tokenProvider);

            // Initiate the connection.
            _relayConnection = await client.CreateConnectionAsync();

            var observableMessages = Observable.Create<string>(obs =>
            {
                var disposable = Observable.Using(
                    () => new StreamReader(_relayConnection),
                    reader => reader.ReadLineAsync().ToObservable())
                    .Subscribe(stringLine =>
                    {
                        // If there's no input data, signal that 
                        // you will no longer send data on this connection.
                        // Then, break out of the processing loop.
                        if (string.IsNullOrEmpty(stringLine))
                        {
                            obs.OnCompleted();
                        }

                        obs.OnNext(stringLine);
                    },
                    ex =>
                    {
                        if (ex is IOException)
                        {
                            // Catch an I/O exception. This likely occurred when
                            // the client disconnected.
                        }
                        else
                        {
                            obs.OnError(ex);
                        }
                    },
                    obs.OnCompleted);

                return disposable;
            });

            return observableMessages;
        }

        public async Task SendAsync(string message)
        {
            using (var writer = new StreamWriter(_relayConnection) {AutoFlush = true})
            {
                await writer.WriteLineAsync(message).ConfigureAwait(false);
            }
        }
    }
}
