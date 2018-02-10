using System;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using IRelayHybridConnRx.Model;
using IRelayHybridConnRx.Service;
using Microsoft.Azure.Relay;

namespace RelayHybridConnRx.Service
{
    public class RelayListenerService : IRelayListenerService
    {
        private readonly IObserver<RelayConnectionState> _relayStateObserver;
        private readonly IObservable<RelayConnectionState> _relayStateObservable;

        public RelayListenerService()
        {
            var relayStateSubject = new BehaviorSubject<RelayConnectionState>(RelayConnectionState.Offline);

            _relayStateObservable = relayStateSubject.AsObservable();
            _relayStateObserver = relayStateSubject.AsObserver();
        }

        public async Task<(IObservable<RelayConnectionState> relayConnectionStateObservable, IObservable<string> message)> 
            RelayListenerObservable(string relayNamespace, string connectionName, string keyName, string key, TimeSpan? timeout = null)
        {
            // Set default timout to 10 seconds.
            timeout = timeout ?? TimeSpan.FromSeconds(10);

            var cts = new CancellationTokenSource(timeout.Value);

            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(keyName, key);
            var listener = new HybridConnectionListener(
                new Uri(string.Format($"sb://{relayNamespace}/{connectionName}")), tokenProvider);

            // Subscribe to the status events.
            listener.Connecting += ConnectingHandler;
            listener.Offline += OfflineHandler;
            listener.Online += OnlineHandler;

            await listener.OpenAsync(cts.Token);
            _relayStateObserver.OnNext(RelayConnectionState.Listening);

            var observableRelayMessages = Observable.Create<string>(obs =>
            {
                IDisposable disposableRelayMessages = null;

                var disposableConnections = Observable.While(
                        () => !cts.IsCancellationRequested,
                        listener.AcceptConnectionAsync().ToObservable())
                    .Subscribe(connection =>
                        {
                            var reader = new StreamReader(connection);

                            disposableRelayMessages = _observableRelayStringLine(reader)
                                .Subscribe(
                                    obs.OnNext,
                                    obs.OnError);
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

                return new CompositeDisposable(
                    disposableConnections,
                    disposableRelayMessages,
                    Disposable.Create(() =>
                    {
                        _relayStateObserver.OnNext(RelayConnectionState.ExitingListener);

                        try
                        {
                            listener.CloseAsync(CancellationToken.None).Wait(cts.Token);
                        }
                        catch (Exception ex)
                        {
                            _relayStateObserver.OnError(ex);
                        }

                        listener.Connecting -= ConnectingHandler;
                        listener.Offline -= OfflineHandler;
                        listener.Online -= OnlineHandler;

                        _relayStateObserver.OnCompleted();

                    }));
            });

            return (_relayStateObservable, observableRelayMessages);
        }

        private IObservable<string> _observableRelayStringLine(TextReader reader) => Observable.Create<string>(obs =>
        {
            var lineObservable = reader.ReadLineAsync().ToObservable();

            var disposableText = lineObservable.Subscribe(stringLine =>
                {
                    // Null data, signals that 
                    // no more data is send on this connection
                    // and hence the connecton is complete.
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

            return disposableText;
        });


        private void ConnectingHandler(object obj, EventArgs e)
        {
            _relayStateObserver.OnNext(RelayConnectionState.Connecting);
        }

        private void OfflineHandler(object obj, EventArgs e)
        {
            _relayStateObserver.OnNext(RelayConnectionState.Offline);
        }

        private void OnlineHandler(object obj, EventArgs e)
        {
            _relayStateObserver.OnNext(RelayConnectionState.Online);
        }
    }
}
