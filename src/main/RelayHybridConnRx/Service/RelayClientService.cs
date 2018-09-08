using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using IRelayHybridConnRx.Service;
using Microsoft.Azure.Relay;
using RelayHybridConnRx.CustomException;

namespace RelayHybridConnRx.Service
{
    public class RelayClientService : IRelayClientService
    {
        private bool _isInitialized;
        private HybridConnectionStream _relayConnection;

        private StreamWriter _writer;

        public async Task<IObservable<string>> 
            RelayClintObservableInitializeAsync(
                string relayNamespace, 
                string connectionName, 
                string keyName, 
                string key)
        {
            if (_isInitialized)
            {
                throw new RelayClientException("Relay client can only be initialized once. Create a new instance, if multiple clients are needed.");
            }

            _isInitialized = true;

            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(keyName, key);
            var client = new HybridConnectionClient(new Uri($"sb://{relayNamespace}/{connectionName}"), tokenProvider);

            // Initiate the connection.
            _relayConnection = await client.CreateConnectionAsync();

            _writer = new StreamWriter(_relayConnection) { AutoFlush = true };

            var reader = new StreamReader(_relayConnection);

            var readerObservable = Observable.FromAsync(reader.ReadLineAsync);

            var observableMessages = Observable.Create<string>(obs =>
            {
                var disposableReader = Observable.While(
                    () => true,
                    readerObservable)
                    .Subscribe(stringLine =>
                    {
                        Debug.WriteLine(stringLine);
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
                        Debug.WriteLine(ex.ToString());

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
            }).Publish().RefCount();

            return observableMessages;
        }

        public async Task SendAsync(string message)
        {
            await _writer.WriteLineAsync(message);
        }
    }
}
