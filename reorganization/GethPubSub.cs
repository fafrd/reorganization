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
    public class GethPubSub : IDisposable
    {
        readonly CancellationToken _ctoken;
        readonly Uri _gethServer;
        readonly ClientWebSocket _websock;

        public GethPubSub(Uri gethServer, CancellationToken ctoken)
        {
            _ctoken = ctoken;
            _gethServer = gethServer;
            _websock = new ClientWebSocket();
        }

        public async Task SubscribeToNewBlocks()
        {
            string subscriptionId = null;

            try
            {
                await _websock.ConnectAsync(_gethServer, _ctoken);

                // Create a subscription for new block headers
                string request = "{\"id\": 1, \"method\": \"eth_subscribe\", \"params\": [\"newHeads\", {}]}";
                ArraySegment<byte> sub = new ArraySegment<byte>(Encoding.UTF8.GetBytes(request));
                await _websock.SendAsync(sub, WebSocketMessageType.Text, false, _ctoken);

                // Receive subscription id
                // expecting {"jsonrpc":"2.0","id":1,"result":"0xcd0c3e8af590364c09d0fa6a1210faf5"}
                byte[] respBytes = await ReceiveSocketData();

                // Parse id of the subscription (this will be useful later)
                string respString = Encoding.UTF8.GetString(respBytes);
                JObject subIdJson = (JObject)JsonConvert.DeserializeObject(respString);
                subscriptionId = (string)subIdJson["result"];

                while (!_ctoken.IsCancellationRequested)
                {
                    // Wait to receive new block headers
                    respBytes = await ReceiveSocketData();

                    // parse hash, parent hash
                    respString = Encoding.UTF8.GetString(respBytes);
                    JObject notification = (JObject)JsonConvert.DeserializeObject(respString);
                    JObject param = (JObject)notification["params"];
                    JObject param_result = (JObject)param["result"];
                    string parentHash = (string)param_result["parentHash"];
                    string hash = (string)param_result["hash"];

                    // TODO where should this data go?
                    Console.WriteLine($"New block: {hash}, parent: {parentHash}");
                }
            }
            finally
            {
                // TODO use subscriptionId to send eth_unsubscribe
                // {"id": 1, "method": "eth_unsubscribe", "params": ["0xcd0c3e8af590364c09d0fa6a1210faf5"]}

                await _websock.CloseAsync(WebSocketCloseStatus.Empty, null, _ctoken);
            }
        }

        public async Task<byte[]> ReceiveSocketData()
        {
            int bufferSize = 1024;
            byte[] receivedBytes = new byte[bufferSize];
            List<byte> compoundBuffer = new List<byte>();

            // Receive subscription id
            // expecting {"jsonrpc":"2.0","id":1,"result":"0xcd0c3e8af590364c09d0fa6a1210faf5"}
            WebSocketReceiveResult result = new WebSocketReceiveResult(0, WebSocketMessageType.Close, false);
            while (!result.EndOfMessage)
            {
                result = await _websock.ReceiveAsync(new ArraySegment<byte>(receivedBytes), _ctoken);

                byte[] readBytes = new byte[result.Count];
                Array.Copy(receivedBytes, readBytes, result.Count);
                compoundBuffer.AddRange(readBytes);
            }

            byte[] respBytes = compoundBuffer.ToArray();
            return respBytes;
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
