using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RPS
{
    public enum GameStatus { Waitting, Start, WaittingAnother, End }
    
    public class Game
    {
        static List<Game> games = new List<Game>();

        public List<Player> Players { get; } = new List<Player>(2);
        public GameStatus Status { get; private set; } = GameStatus.Waitting;
        
        public static Game GetFreeGame()
        {
            Game game;
            game = games.Find(g => g.Status == GameStatus.Waitting);
            if (game == null)
                games.Add(game = new Game());

            return game;
        }
        
        public void Join(Player player)
        {
            this.Players.Add(player);

            if (this.Players.Count == 2)
            {
                SendData(ToClientMessageType.Info, "遊戲開始").Wait();
                this.Status = GameStatus.Start;
            }
            else
            {
                player.SendMsg(ToClientMessageType.Info, "等人中").Wait();
                this.Status = GameStatus.Waitting;
            }
        }

        public async Task PlayerDisconnect(Player player)
        {
            this.Players.Remove(player);

            if (this.Status == GameStatus.End)
                return;

            if (this.Status != GameStatus.Waitting)
            {
                await SendData(ToClientMessageType.Info, "玩家中離了");
                this.Restart();
            }
        }

        public void Restart()
        {
            this.Players.ForEach(p => p.ResetCard());
            this.Status = this.Players.Count == 2 ? GameStatus.Start : GameStatus.Waitting;
        }

        public async Task ReciveCard(Player player)
        {
            if (this.Players.Any(p => p.Card == null))
            {
                this.Status = GameStatus.WaittingAnother;
                await player.SendMsg(ToClientMessageType.Info, "等待另一位玩家");
                return;
            }

            Player win = null;
            if (this.Players.Select(p => p.Card.ToString()).Distinct().Count() > 1)
                win = Players.OrderByDescending(p => p.Card).First();

            await SendData(ToClientMessageType.Result,
                $@"{JsonConvert.SerializeObject(new
                {
                    Win = win?.Id,
                    Info = Players.Select(p => new { p.Id, Card = p.Card.ToString() })
                })}");

            if (win != null)
                this.Status = GameStatus.End;
            else
                this.Restart();
        }

        public async Task SendData(ToClientMessageType type, string msg)
        {
            foreach (var p in this.Players)
                await p.SendMsg(type, msg);
        }
    }
}
