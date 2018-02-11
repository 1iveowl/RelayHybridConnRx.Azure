using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RelayHybridConnRx.Service;

namespace ConsoleTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var configDate = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(@"ConnectionSettings.json"));

            var relayNameSpace = (string)configDate.RelayNameSpace;
            var connectionName = (string) configDate.ConnectionName;
            var keyName = (string) configDate.KeyName;
            var key = (string) configDate.Key;

            var relayListener = new RelayListenerService();

            var relayClient = new RelayClientService();

            var listener = await relayListener.RelayListenerObservableAsync(relayNameSpace, connectionName, keyName, key);
            var clientObservable = await relayClient.RelayClintObservableAsync(relayNameSpace, connectionName, keyName, key);

            listener.relayConnectionStateObservable.Subscribe(state =>
                {
                    Console.WriteLine($"Listener state: {state}");
                },
                ex => Console.WriteLine($"Listener State exception: {ex}"),
                () => Console.WriteLine("Listener State Completed"));

            listener.messageObservable.Subscribe(async msg =>
                {
                    Console.WriteLine($"Listener Recieved Message: {msg.IncomingMessage}");
                    await msg.ResponseMessageAsync($"Received: {msg.IncomingMessage}");
                },
                ex => Console.WriteLine($"Listener exception: {ex}"),
                () => Console.WriteLine("Listener Completed"));

            await Task.Delay(TimeSpan.FromSeconds(2));


            clientObservable.Subscribe(msg =>
            {
                Console.WriteLine($"Client received: {msg}");
            },
            ex => Console.WriteLine($"Client exception: {ex}"),
            () => Console.WriteLine("Client Completed"));

            await Task.Delay(TimeSpan.FromSeconds(2));

            await relayClient.SendAsync("Test");

            await Task.Delay(TimeSpan.FromSeconds(2));

            await relayClient.SendAsync("Test2");

            Console.ReadLine();
        }
    }
}
