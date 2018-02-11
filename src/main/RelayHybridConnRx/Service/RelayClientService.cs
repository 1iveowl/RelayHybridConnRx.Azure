using System;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using IRelayHybridConnRx.Service;
using Microsoft.Azure.Relay;

namespace RelayHybridConnRx.Service
{
    public class RelayClientService : IRelayClientService
    {
        private HybridConnectionStream _relayConnection;

        private StreamWriter _writer;

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

            var reader = new StreamReader(_relayConnection);

            _writer = new StreamWriter(_relayConnection) {AutoFlush = true};

            var readerObservable = Observable.FromAsync(reader.ReadLineAsync);

            var observableMessages = Observable.Create<string>(obs =>
            {
                var disposableReader = Observable.While(
                    () => true,
                    readerObservable)
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

                return new CompositeDisposable(
                    disposableReader,
                    Disposable.Create(() =>
                    {
                        reader?.Dispose();
                        _writer?.Dispose();
                        _relayConnection?.Dispose();
                    }));
            });

            return observableMessages;
        }

        public async Task SendAsync(string message)
        {
            await _writer.WriteLineAsync(message);
        }
    }
}
