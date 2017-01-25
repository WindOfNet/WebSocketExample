using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Dynamic;

namespace BluffDice
{
    public class Player
    {
        WebSocket _ws;
        public Guid Id { get; }
        public Game Game { get; private set; }
        public Card Card { get; }
        public bool CanRoll { get; set; } = false;
        public bool CanBluff { get; set; } = false;
        public bool CanDebluff { get; set; } = false;

        public Player(WebSocket ws)
        {
            this._ws = ws;
            this.Id = Guid.NewGuid();
            this.Card = new Card();
            this.SendClient(ToClientMessageType.Id, this.Id.ToString()).Wait();
            this.Game = Game.GetGameRoom();
            this.Game.Join(this).Wait();
        }

        public void SetStatus(bool? roll = null, bool? bluff = null, bool? debluff = null)
        {
            if (roll != null) this.CanRoll = roll.Value;
            if (bluff != null) this.CanBluff = bluff.Value;
            if (debluff != null) this.CanDebluff = debluff.Value;
        }

        public async Task Listen()
        {
            try
            {
                while (false == this._ws.CloseStatus.HasValue)
                {
                    var buffer = new byte[1024];
                    var result = await this._ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await this._ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "server close", CancellationToken.None);
                        break;
                    }
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    ClientMessage cm = JsonConvert.DeserializeObject<ClientMessage>(message);

                    switch (cm.Type)
                    {
                        case ClientMessageType.Roll:
                            if (this.CanRoll == false)
                                continue;
                            await this.Game.PlayerRoll(this);
                            break;
                        case ClientMessageType.Bluff:
                            if (this.CanBluff == false)
                                continue;
                            await this.Game.Bluff(this, (int)cm.Message.Number, (int)cm.Message.Count);
                            break;
                        case ClientMessageType.Debluff:
                            if (this.CanDebluff == false)
                                continue;
                            await this.Game.Debluff(this);
                            break;
                    }
                }
            }
            catch (Exception ex) { Console.Write(ex); }
            finally { await this.Game.Leave(this); }
        }

        public async Task SendGameMessage(ToClientMessageType type, dynamic message = null)
        {
            await this.SendClient(type, message);
        }

        internal async Task ApponentBluff(dynamic message)
        {
            this.CanBluff = true;
            this.CanDebluff = true;
            await this.SendClient(ToClientMessageType.ApponentBluff, message);
        }

        async Task SendClient(ToClientMessageType type, dynamic message = null)
        {
            if (this._ws.CloseStatus.HasValue)
                return;

            var m = new ToClientMessage { Type = type, Message = message, CanRoll = this.CanRoll, CanBluff = this.CanBluff, CanDebluff = this.CanDebluff };
            var buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(m));

            await this._ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}
