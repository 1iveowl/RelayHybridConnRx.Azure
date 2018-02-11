# Relay Hybrid Connection library for Azure and Reactive Extensions
[![NuGet Badge](https://buildstats.info/nuget/RelayHybridConnRx.AzureSimpleHttpListener)](https://www.nuget.org/packages/RelayHybridConnRx.Azure)

[![.NET Standard](http://img.shields.io/badge/.NET_Standard-v2.0-green.svg)](https://docs.microsoft.com/en-us/dotnet/articles/standard/library)
[![System.Reactive](http://img.shields.io/badge/Rx-v3.1.1-ff69b4.svg)](http://reactivex.io/) 

*Please star this project if you find it useful. Thank you.*

## Background

I wanted to make it super easy to set up a Relay Hybrid Connection for Azure. And I wanted to base it on Reactive Extensions (Rx). Rx is an API for asynchronous programming with observable streams and make perfect sense in a relay scenario. 

For an introduction to Azure Relay Hybrid Connects see: [Get started with Relay Hybrid Connections](https://docs.microsoft.com/en-us/azure/service-bus-relay/relay-hybrid-connections-dotnet-get-started)

## How to use
Using this library is super easy. Follow these examples or see the [console sample application](https://github.com/1iveowl/RelayHybridConnRx.Azure/tree/master/src/test/ConsoleTest)

### Creating a listener

```cs
var relayListener = new RelayListenerService();

var listener = await relayListener.RelayListenerInitializeAsync(relayNameSpace, connectionName, keyName, key);

listener.relayConnectionStateObservable.Subscribe(state =>
    {
        Console.WriteLine($"Listener state: {state}");
    },
    ex => Console.WriteLine($"State Listener State exception: {ex}"),
    () => Console.WriteLine("State Listener Completed"));


listener.messageObservable.Subscribe(async msg =>
    {
        Console.WriteLine($"Listener Recieved Message: {msg.IncomingMessage}");

		// Send an echo back to the client
        await msg.ResponseMessageAsync($"Echo: {msg.IncomingMessage}");
    },
    ex => Console.WriteLine($"Message Listener exception: {ex}"),
    () => Console.WriteLine("Message Listener Completed"));
```

### Creating a client 

```cs
var relayClient = new RelayClientService();

var clientObservable = await relayClient.RelayClintObservableInitializeAsync(relayNameSpace, connectionName, keyName, key);

clientObservable.Subscribe(msg =>
{
    Console.WriteLine($"Client received: {msg}");
},
ex => Console.WriteLine($"Client exception: {ex}"),
() => Console.WriteLine("Client Completed"));

```

### Sending a message from the client
```cs
await relayClient.SendAsync("Test");
```

