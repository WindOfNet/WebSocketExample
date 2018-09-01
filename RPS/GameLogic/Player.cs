using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RPS.GameLogic
{
    public class Player
    {
        public Game Game { get; }
        public WebSocket WebSocket { get; }
        
        public Player(WebSocket webSocket)
        {
            this.Game = Game.GetFreeGame();
            this.WebSocket = webSocket;
        }

        public async Task HandleWebsocket()
        {                        
            this.Game.PlayerIn(this);

            while (!this.WebSocket.CloseStatus.HasValue)
            {
                try
                {
                    var buffer = new byte[1024];
                    var result = await this.WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var data = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Card card = (Card)typeof(Card).GetField(data).GetValue(null);
                        this.Game.PlayerExecuteAsync(this, card);
                    }
                }
                catch { break; }
            }

            Game.PlayerOut(this);            
        }
    }
}
