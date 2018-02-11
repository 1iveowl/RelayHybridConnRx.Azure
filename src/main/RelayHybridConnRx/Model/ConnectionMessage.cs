using System.IO;
using System.Threading.Tasks;
using IRelayHybridConnRx.Model;

namespace RelayHybridConnRx.Model
{
    public class Message : IMessage
    {
        private readonly StreamWriter _writer;
        public string IncomingMessage { get; }

        public Message(string incomingMessage, StreamWriter writer)
        {
            _writer = writer;

            IncomingMessage = incomingMessage;
        }

        public async Task ResponseMessageAsync(string responseMessage)
        {
            await _writer.WriteLineAsync(responseMessage);
        }
    }
}
