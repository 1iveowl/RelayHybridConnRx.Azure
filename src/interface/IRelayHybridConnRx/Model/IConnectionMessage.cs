using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace IRelayHybridConnRx.Model
{
    public interface IMessage
    {
        string IncomingMessage { get; }

        Task ResponseMessageAsync(string responseMessage);
    }
}
