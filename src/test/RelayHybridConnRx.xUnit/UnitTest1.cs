using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using IRelayHybridConnRx.Model;
using Newtonsoft.Json;
using RelayHybridConnRx.Service;
using Xunit;

namespace RelayHybridConnRx.xUnit
{
    public class UnitTest1
    {
        private readonly string _relayNameSpace;
        private readonly string _connectionName;
        private readonly string _keyName;
        private readonly string _key;

        private RelayListenerService _relayListenerService1;
        private RelayListenerService _relayListenerService2;

        private RelayClientService _relayClientService1;
        private RelayClientService _relayClientService2;

        private (IObservable<RelayListenerConnectionState> relayConnectionStateObservable, IObservable<IMessage> messageObservable) _listener1;
        private (IObservable<RelayListenerConnectionState> relayConnectionStateObservable, IObservable<IMessage> messageObservable) _listener2;

        private IObservable<string> _clientObservable1;
        private IObservable<string> _clientObservable2;

        public UnitTest1()
        {
            var configDate = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(@"ConnectionSettings.json"));

            _relayNameSpace = (string)configDate.RelayNameSpace;
            _connectionName = (string)configDate.ConnectionName;
            _keyName = (string)configDate.KeyName;
            _key = (string)configDate.Key;
        }

        [Fact]
        public async Task ConnectionIsListening()
        {
            await Initialize(false);

            Exception exception = null;
            var isCompleted = false;

            RelayListenerConnectionState? result = null;

            var stateDisposable = _listener1.relayConnectionStateObservable.Subscribe(state =>
            {
                result = state;
            },
            ex => exception = ex,
            () => isCompleted = true);

            Assert.Equal(RelayListenerConnectionState.Listening, result);
            Assert.False(isCompleted);
            Assert.Null(exception);

            stateDisposable?.Dispose();
        }

        [Fact]
        public async Task MultipleConnectionIsListening()
        {
            await Initialize(false);

            Exception exception1 = null;
            Exception exception2 = null;

            var isCompleted1 = false;
            var isCompleted2 = false;

            RelayListenerConnectionState? result1 = null;
            RelayListenerConnectionState? result2 = null;

            string message1 = null;
            string message2 = null;

            var stateDisposable1 = _listener1.relayConnectionStateObservable.Subscribe(state =>
                {
                    result1 = state;
                },
                ex => exception1 = ex,
                () => isCompleted1 = true);

            var stateDisposable2 = _listener1.relayConnectionStateObservable.Subscribe(state =>
                {
                    result2 = state;
                },
                ex => exception2 = ex,
                () => isCompleted2 = true);

            var messageDisposable1 = _listener1.messageObservable.Subscribe(msg => { message1 = msg.IncomingMessage; });
            var messageDisposable2 = _listener1.messageObservable.Subscribe(msg => { message2 = msg.IncomingMessage; });

            const string testString1 = "Test1";

            await _relayClientService1.SendAsync(testString1);

            await Task.Delay(TimeSpan.FromSeconds(1));

            Assert.Equal(RelayListenerConnectionState.Receiving, result1);
            Assert.False(isCompleted1);
            Assert.Null(exception1);
            Assert.Equal(testString1, message1);

            Assert.Equal(RelayListenerConnectionState.Receiving, result2);
            Assert.False(isCompleted2);
            Assert.Null(exception2);
            Assert.Equal(testString1, message2);

            stateDisposable1?.Dispose();
            stateDisposable2?.Dispose();
            messageDisposable1?.Dispose();
            messageDisposable2?.Dispose();
        }

        [Fact]
        public async Task MultipleClientMessagesComeThrough()
        {
            await Initialize(false, true);

            Exception listenerException = null;

            var isCompletedListener = false;

            var messagesReceived = new List<string>();

            RelayListenerConnectionState? result = null;

            var stateDisposable = _listener1.relayConnectionStateObservable.Subscribe(state =>
                {
                    result = state;
                },
                ex => listenerException = ex,
                () => isCompletedListener = true);

            var messageDisposable = _listener1.messageObservable.Subscribe(msg =>
                {
                    messagesReceived.Add(msg.IncomingMessage);
                },
                ex => listenerException = ex,
                () => isCompletedListener = true);


            const string testString1 = "Test1";
            const string testString2 = "Test2";

            await _relayClientService1.SendAsync(testString1);

            await Task.Delay(TimeSpan.FromSeconds(1));
            await _relayClientService2.SendAsync(testString2);

            await Task.Delay(TimeSpan.FromSeconds(2));

            Assert.False(isCompletedListener);

            Assert.Null(listenerException);

            Assert.Equal(testString1, messagesReceived[0]);
            Assert.Equal(testString2, messagesReceived[1]);

            stateDisposable?.Dispose();
            messageDisposable?.Dispose();

        }


        [Fact]
        public async Task MultipleClientRecievedIndividualEcho()
        {
            await Initialize(false, true);

            Exception listenerException = null;

            var isCompletedListener = false;

            var messagesReceived = new List<string>();

            RelayListenerConnectionState? result = null;

            var stateDisposable = _listener1.relayConnectionStateObservable.Subscribe(state =>
                {
                    result = state;
                },
                ex => listenerException = ex,
                () => isCompletedListener = true);

            var messageDisposable = _listener1.messageObservable.Subscribe(async msg =>
                {
                    messagesReceived.Add(msg.IncomingMessage);
                    await msg.ResponseMessageAsync($"Echo: {msg.IncomingMessage}");
                },
                ex => listenerException = ex,
                () => isCompletedListener = true);

            string client1Echo = null;
            string client2Echo = null;

            var clientMessageDisposable1 = _clientObservable1.Subscribe(msg =>
            {
                client1Echo = msg;
            });

            var clientMessageDisposable2 = _clientObservable2.Subscribe(msg =>
            {
                client2Echo = msg;
            });
            
            const string testString1 = "Test1";
            const string testString2 = "Test2";
            
            await _relayClientService2.SendAsync(testString2);

            await Task.Delay(TimeSpan.FromSeconds(2));

            await _relayClientService1.SendAsync(testString1);

            await Task.Delay(TimeSpan.FromSeconds(1));

            Assert.False(isCompletedListener);

            Assert.Null(listenerException);

            Assert.Equal($"Echo: {testString1}", client1Echo);
            Assert.Equal($"Echo: {testString2}", client2Echo);

            stateDisposable?.Dispose();
            messageDisposable?.Dispose();
            clientMessageDisposable1?.Dispose();
            clientMessageDisposable2?.Dispose();

        }

        [Fact]
        public async Task MultipleClientSubscriptions()
        {
            await Initialize(false, true);

            Exception listenerException = null;

            var isCompletedListener = false;

            var messagesReceived = new List<string>();

            RelayListenerConnectionState? result = null;

            var stateDisposable = _listener1.relayConnectionStateObservable.Subscribe(state =>
            {
                result = state;
            },
                ex => listenerException = ex,
                () => isCompletedListener = true);

            var messageDisposable = _listener1.messageObservable.Subscribe(async msg =>
            {
                messagesReceived.Add(msg.IncomingMessage);
                await msg.ResponseMessageAsync($"Echo: {msg.IncomingMessage}");
            },
                ex => listenerException = ex,
                () => isCompletedListener = true);

            string client1Echo = null;
            string client2Echo = null;

            var clientMessageDisposable1 = _clientObservable1.Subscribe(msg =>
            {
                client1Echo = msg;
            });

            var clientMessageDisposable2 = _clientObservable1.Subscribe(msg =>
            {
                client2Echo = msg;
            });

            const string testString = "Test for two subscriptions";

            await _relayClientService1.SendAsync(testString);

            await Task.Delay(TimeSpan.FromSeconds(1));

            Assert.False(isCompletedListener);

            Assert.Null(listenerException);

            Assert.Equal($"Echo: {testString}", client1Echo);
            Assert.Equal($"Echo: {testString}", client2Echo);

            stateDisposable?.Dispose();
            messageDisposable?.Dispose();
            clientMessageDisposable1?.Dispose();
            clientMessageDisposable2?.Dispose();

        }

        private async Task Initialize(bool hasMultipleListeners = false, bool hasMultipleClients = false)
        {
        
            _relayListenerService1 = new RelayListenerService();

            _relayClientService1 = new RelayClientService();

            _listener1 = await _relayListenerService1.RelayListenerInitializeAsync(_relayNameSpace, _connectionName, _keyName, _key);
            _clientObservable1 = await _relayClientService1.RelayClintObservableInitializeAsync(_relayNameSpace, _connectionName, _keyName, _key);

            if (hasMultipleListeners)
            {
                _relayListenerService2 = new RelayListenerService();


                _listener2 = await _relayListenerService2.RelayListenerInitializeAsync(_relayNameSpace, _connectionName, _keyName, _key);
            }

            if (hasMultipleClients)
            {

                _relayClientService2 = new RelayClientService();

                _clientObservable2 = await _relayClientService2.RelayClintObservableInitializeAsync(_relayNameSpace, _connectionName, _keyName, _key);
            }
        }
    }
}
