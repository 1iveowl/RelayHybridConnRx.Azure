using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ConsoleTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var configDate = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(@"ConnectionSettings.json"));

            var RelayNameSpace = (string)configDate.RelayNameSpace;
            var ConnectionName = (string) configDate.ConnectionName;
            var KeyName = (string) configDate.KeyName;
            var Key = (string) configDate.Key;

            Console.WriteLine($"Hello World! {RelayNameSpace}");

            Console.ReadLine();
        }
    }
}
