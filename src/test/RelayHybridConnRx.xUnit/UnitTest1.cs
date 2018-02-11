using System;
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

        private RelayListenerService _relayListenerService;
        private RelayClientService _relayClientService;

        private (IObservable<RelayListenerConnectionState> relayConnectionStateObservable, IObservable<IMessage> messageObservable) _listener;
        private IObservable<string> _clientObservable;

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
            await Initialize();

            Exception exception = null;
            var isCompleted = false;

            RelayListenerConnectionState? result = null;

            _listener.relayConnectionStateObservable.Subscribe(state =>
            {
                result = state;
            },
            ex => exception = ex,
            () => isCompleted = true);

            Assert.Equal(RelayListenerConnectionState.Listening, result);
            Assert.False(isCompleted);
            Assert.Null(exception);

        }

        private async Task Initialize()
        {
            _relayListenerService = new RelayListenerService();

            _relayClientService = new RelayClientService();

            _listener = await _relayListenerService.RelayListenerObservableAsync(_relayNameSpace, _connectionName, _keyName, _key);
            _clientObservable = await _relayClientService.RelayClintObservableAsync(_relayNameSpace, _connectionName, _keyName, _key);
        }
    }
}
