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
            SubscribeToNewBlocks(gethServer, token).Wait();
        }

        public static async Task SubscribeToNewBlocks(Uri server, CancellationToken token)
        {
            ClientWebSocket websock = websock = new ClientWebSocket();
            string subscriptionId = null;

            try
            {
                await websock.ConnectAsync(server, token);

                // Create a subscription for new block headers
                string request = "{\"id\": 1, \"method\": \"eth_subscribe\", \"params\": [\"newHeads\", {}]}";
                ArraySegment<byte> sub = new ArraySegment<byte>(Encoding.UTF8.GetBytes(request));
                await websock.SendAsync(sub, WebSocketMessageType.Text, false, token);

                // prepare buffer
                int bufferSize = 1024;
                byte[] receivedBytes = new byte[bufferSize];
                List<byte> compoundBuffer = new List<byte>();

                // Receive subscription id
                // expecting {"jsonrpc":"2.0","id":1,"result":"0xcd0c3e8af590364c09d0fa6a1210faf5"}
                WebSocketReceiveResult result = new WebSocketReceiveResult(0, WebSocketMessageType.Close, false);
                while (!result.EndOfMessage)
                {
                    result = await websock.ReceiveAsync(new ArraySegment<byte>(receivedBytes), token);

                    byte[] readBytes = new byte[result.Count];
                    Array.Copy(receivedBytes, readBytes, result.Count);
                    compoundBuffer.AddRange(readBytes);
                }

                byte[] respBytes = compoundBuffer.ToArray();
                string respString = Encoding.UTF8.GetString(respBytes);
                JObject subIdJson = (JObject)JsonConvert.DeserializeObject(respString);
                // We now have the id of the subscription. this will be useful later.
                subscriptionId = (string)subIdJson["result"];

                while (!token.IsCancellationRequested)
                {
                    // Wait to receive new block headers
                    receivedBytes = new byte[bufferSize];
                    compoundBuffer = new List<byte>();
                    result = new WebSocketReceiveResult(0, WebSocketMessageType.Text, false);
                    while (!result.EndOfMessage)
                    {
                        Console.WriteLine("hi");
                        result = await websock.ReceiveAsync(new ArraySegment<byte>(receivedBytes), token);
                        Console.WriteLine("hey");
                        byte[] readBytes = new byte[result.Count];
                        Array.Copy(receivedBytes, readBytes, result.Count);
                        compoundBuffer.AddRange(readBytes);
                        Console.WriteLine("heyyy");
                    }

                    respBytes = compoundBuffer.ToArray();
                    respString = Encoding.UTF8.GetString(respBytes);
                    JObject notification = (JObject)JsonConvert.DeserializeObject(respString);
                    JObject param = (JObject)notification["params"];
                    JObject param_result = (JObject)param["result"];
                    string parentHash = (string)param_result["parentHash"];
                    string hash = (string)param_result["hash"];
                    Console.WriteLine($"New block: {hash}, parent: {parentHash}");
                }
            }
            finally
            {
                // TODO use subscriptionId to send eth_unsubscribe
                // {"id": 1, "method": "eth_unsubscribe", "params": ["0xcd0c3e8af590364c09d0fa6a1210faf5"]}

                await websock.CloseAsync(WebSocketCloseStatus.Empty, null, token);
            }
        }
    }

}
