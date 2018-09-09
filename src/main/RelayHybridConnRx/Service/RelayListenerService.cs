using IRelayHybridConnRx.Model;
using IRelayHybridConnRx.Service;
using Microsoft.Azure.Relay;
using RelayHybridConnRx.CustomException;
using RelayHybridConnRx.Model;
using System;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace RelayHybridConnRx.Service
{
    public class RelayListenerService : IRelayListenerService
    {
        private bool _isInitialized;

        private readonly IObserver<RelayListenerConnectionState> _relayStateObserver;
        private readonly IObservable<RelayListenerConnectionState> _relayStateObservable;

        public RelayListenerService()
        {
            var relayStateSubject = new BehaviorSubject<RelayListenerConnectionState>(RelayListenerConnectionState.Offline);

            _relayStateObservable = relayStateSubject.AsObservable();
            _relayStateObserver = relayStateSubject.AsObserver();
        }

        public async Task<(IObservable<RelayListenerConnectionState> relayConnectionStateObservable, IObservable<IMessage> messageObservable)> 
            RelayListenerInitializeAsync(
                string relayNamespace, 
                string connectionName, 
                string keyName, 
                string key, 
                TimeSpan? timeout = null, 
                CancellationTokenSource cancellationTokenSource = null)
        {
            if (_isInitialized)
            {
                throw new RelayListenerException("Relay Listener can only be initialized once. Create a new instance if multiple listeners are needed.");
            }

            _isInitialized = true;

            // Set default timout to 10 seconds.
            var to = timeout ?? TimeSpan.FromSeconds(10);

            var cts = cancellationTokenSource ?? new CancellationTokenSource();

            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(keyName, key);

            var listener = new HybridConnectionListener(new Uri($"sb://{relayNamespace}/{connectionName}"), tokenProvider);

            // Subscribe to the connection status events.
            listener.Connecting += ConnectingHandler;
            listener.Offline += OfflineHandler;
            listener.Online += OnlineHandler;

            await listener.OpenAsync(cts.Token);
            
            _relayStateObserver.OnNext(RelayListenerConnectionState.Listening);

            var observableRelayMessages = Observable.Create<IMessage>(obs =>
            {
                IDisposable disposableRelayMessages = null;

                var disposableConnections = Observable.While(
                        () => !cts.IsCancellationRequested,
                        Observable.FromAsync(() => listener.AcceptConnectionAsync()))
                    .Subscribe(connection =>
                        {
                            if (connection != null)
                            {
                                disposableRelayMessages = _observableRelayStringLine(connection, cts.Token, to)
                                    .Subscribe(obs.OnNext, obs.OnError);
                            }
                        },
                        ex =>
                        {
                            _relayStateObserver.OnError(ex);
                            obs.OnError(ex);
                        },
                        () =>
                        {
                            _relayStateObserver.OnCompleted();
                            obs.OnCompleted();
                        });

                return Disposable.Create(() =>
                    {
                        disposableConnections?.Dispose();
                        disposableRelayMessages?.Dispose();

                        _relayStateObserver.OnNext(RelayListenerConnectionState.ExitingListener);

                        try
                        {
                            listener.CloseAsync(CancellationToken.None).Wait(new CancellationTokenSource(to).Token);
                        }
                        catch (Exception ex)
                        {
                            _relayStateObserver.OnError(ex);
                        }
                        finally
                        {
                            cts?.Dispose();
                        }

                        listener.Connecting -= ConnectingHandler;
                        listener.Offline -= OfflineHandler;
                        listener.Online -= OnlineHandler;
                        
                        _relayStateObserver.OnCompleted();

                    });

            }).Publish().RefCount();

            return (_relayStateObservable, observableRelayMessages);
        }

        private IObservable<IMessage> _observableRelayStringLine(HybridConnectionStream connection, CancellationToken ct, TimeSpan timeout) 
            => Observable.Create<IMessage>(
                obs =>
                {
                    _relayStateObserver.OnNext(RelayListenerConnectionState.Receiving);

                    var reader = new StreamReader(connection);

                    var writer = new StreamWriter(connection) { AutoFlush = true };

                    var readerObservable = Observable.FromAsync(reader.ReadLineAsync);
         
                    var disposableText = Observable.While(
                        () => !ct.IsCancellationRequested,
                        readerObservable)
                        .Subscribe(stringLine =>
                        {
                            // If there's no input data, signal that 
                            // you will no longer send data on this connection.
                            // Then, break out of the processing loop.

                            Debug.WriteLine(stringLine);

                            if (string.IsNullOrEmpty(stringLine))
                            {
                                writer?.Dispose();

                                try
                                {
                                    connection?.ShutdownAsync(ct)?.Wait(new CancellationTokenSource(timeout).Token);
                                }
                                catch (AggregateException)
                                {
                                    // Ignore if connection already closed
                                }
                                
                                obs.OnCompleted();
                            }

                            obs.OnNext(new Message(stringLine, writer));
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
                        disposableText,
                        Disposable.Create(() =>
                        {
                            _relayStateObserver.OnNext(RelayListenerConnectionState.Listening);

                            writer?.Dispose();
                            reader?.Dispose();

                            try
                            {
                                connection?.ShutdownAsync(ct)?.Wait(new CancellationTokenSource(timeout).Token);
                            }
                            catch (AggregateException)
                            {
                                // Ignore if connection already closed
                            }
                        }));
                });


        private void ConnectingHandler(object obj, EventArgs e)
        {
            _relayStateObserver.OnNext(RelayListenerConnectionState.Connecting);
        }

        private void OfflineHandler(object obj, EventArgs e)
        {
            _relayStateObserver.OnNext(RelayListenerConnectionState.Offline);
        }

        private void OnlineHandler(object obj, EventArgs e)
        {
            _relayStateObserver.OnNext(RelayListenerConnectionState.Online);
        }
    }
}
