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

            Uri gethServer = new Uri("ws://localhost:8546");

            using (GethPubSub pubsub = new GethPubSub(gethServer, token))
            {
                pubsub.SubscribeToNewBlocks(HandleNewBlock).Wait();
            }
        }

        static void HandleNewBlock(string hash, string parentHash)
        {
            Console.WriteLine($"New block: {hash}, parent: {parentHash}");

            // check for reorganization... if the new parenthash is shared with an existing block's parenthash, 
            // a reorg is occuring.
            List<(string, string)> sharedParents = BlockData.blocks.FindAll(b => b.Item2 == parentHash);
            if (sharedParents.Count != 0)
            {
                Console.WriteLine("***** Reorg found *****");
                Console.WriteLine($"Parent: {parentHash}");
                Console.WriteLine($"Candidate children: ");
                foreach ((string, string) candidate in sharedParents)
                    Console.WriteLine($"\t{candidate.Item1}");
            }

            BlockData.blocks.Add((hash, parentHash));
        }
    }

    public static class BlockData
    {
        // list of valuetuples, (hash, parentHash)
        public static List<(string, string)> blocks = new List<(string, string)>();
    }
}
