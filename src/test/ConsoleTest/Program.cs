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

            var listener = await relayListener.RelayListenerInitializeAsync(relayNameSpace, connectionName, keyName, key);
            var clientObservable = await relayClient.RelayClintObservableInitializeAsync(relayNameSpace, connectionName, keyName, key);

            var disposableState = listener.relayConnectionStateObservable.Subscribe(state =>
                {
                    Console.WriteLine($"Listener state: {state}");
                },
                ex => Console.WriteLine($"Listener State exception: {ex}"),
                () => Console.WriteLine("Listener State Completed"));

            var disposableMessages = listener.messageObservable.Subscribe(async msg =>
                {
                    Console.WriteLine($"Listener Received Message: {msg.IncomingMessage}");
                    await msg.ResponseMessageAsync($"Received: {msg.IncomingMessage}");
                },
                ex => Console.WriteLine($"Listener exception: {ex}"),
                () => Console.WriteLine("Listener Completed"));

            await Task.Delay(TimeSpan.FromSeconds(2));


            var disposableClient = clientObservable.Subscribe(msg =>
            {
                Console.WriteLine($"Client received: {msg}");
            },
            ex => Console.WriteLine($"Client exception: {ex}"),
            () => Console.WriteLine("Client Completed"));

            await Task.Delay(TimeSpan.FromSeconds(12));

            //await relayClient.SendAsync("Test");

            await Task.Delay(TimeSpan.FromSeconds(12));

            //await relayClient.SendAsync("Test2");


            await Task.Delay(TimeSpan.FromSeconds(1000000));

            Console.WriteLine("Press anykey to stop listening...");
            Console.ReadLine();

            disposableClient?.Dispose();
            disposableState?.Dispose();
            disposableMessages?.Dispose();

            Console.WriteLine("Press anykey to exit...");

            Console.ReadLine();
        }
    }
}
