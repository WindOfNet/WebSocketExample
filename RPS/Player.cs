using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RPS
{
    public class Player
    {
        public WebSocket Connection { get; }
        public Guid Id { get; }
        public Game Game { get; private set; }
        public Card Card { get; private set; }

        public Player(WebSocket ws)
        {
            this.Connection = ws;
            this.Id = Guid.NewGuid();
            this.SendMsg(ToClientMessageType.Id, this.Id.ToString()).Wait();
            this.Game = Game.GetFreeGame();
            this.Game.Join(this);
        }

        public void SetCard(string s)
        {
            Card c = null;
            switch (s)
            {
                case "Y": c = Card.Y; break;
                case "O": c = Card.O; break;
                case "W": c = Card.W; break;
            }

            this.Card = c;
        }

        public void ResetCard() => this.Card = null;

        public async Task Listen()
        {
            while (!this.Connection.CloseStatus.HasValue)
            {
                var buffer = new byte[1024 * 4];
                var result = await this.Connection.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string data = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    ClientMessage cm = JsonConvert.DeserializeObject<ClientMessage>(data);

                    switch (cm.Type)
                    {
                        case ClientMessageType.SendCard:
                            if (this.Card != null ||
                                false == (this.Game.Status == GameStatus.Start || this.Game.Status == GameStatus.WaittingAnother))
                                continue;

                            this.SetCard(cm.Message);
                            this.Game.ReciveCard(this).Wait();
                            break;
                    }
                }

                if (this.Game.Status == GameStatus.End) break;
            }

            Game.PlayerDisconnect(this).Wait();
        }

        public async Task SendMsg(ToClientMessageType type, string msg)
        {
            ToClientMessage tcm = new ToClientMessage(type, msg);
            var buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(tcm));
            await this.Connection.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    public enum ToClientMessageType { Id, Info, Result }
    public class ToClientMessage
    {
        public ToClientMessage(ToClientMessageType type, string msg)
        {
            Type = type;
            this.Message = msg;
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public ToClientMessageType Type { get; set; }
        public string Message { get; set; }
    }
    
    public enum ClientMessageType { SendCard }
    public class ClientMessage
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ClientMessageType Type { get; set; }
        public string Message { get; set; }
    }
}
