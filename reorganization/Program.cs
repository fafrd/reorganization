using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace reorganization
{
    class Program
    {
        static void Main(string[] args)
        {
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;
            Console.CancelKeyPress += delegate { Console.WriteLine("cancellation requested"); tokenSource.Cancel(); };

            Uri gethServer = new Uri("ws://localhost:7546");

            using (GethPubSub pubsub = new GethPubSub(gethServer, token))
            {
                pubsub.SubscribeToNewBlocks(HandleNewBlock).Wait();
            }
        }

        static void HandleNewBlock(string hash, string parentHash)
        {
            // TODO where should this data go?
            Console.WriteLine($"New block: {hash}, parent: {parentHash}");

            BlockData.blocks.Add((hash, parentHash));
        }
    }

    public static class BlockData
    {
        // list of valuetuples
        public static List<(string, string)> blocks = new List<(string, string)>();
    }
}
