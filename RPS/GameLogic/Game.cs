using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RPS.GameLogic
{
    public enum GameStatus
    {
        Waitting,
        Gamming,
        WaittingPlayerExec,
        SafeAndKeepGamming,
        GameOver,
        GammingLeaveSomeone
    }

    public class Game
    {
        class GameLog
        {
            public Player Player { get; }
            public Card Card { get; }

            public GameLog(Player player, Card card)
            {
                this.Player = player;
                this.Card = card;
            }
        }

        static List<Game> games;
        static Game() { games = new List<Game>(); }
        public static Game GetFreeGame()
        {
            var freeRoom = games.FirstOrDefault(r => r.Players.Count < 2);
            if (freeRoom is null)
            {
                freeRoom = new Game();
                games.Add(freeRoom);
            }

            return freeRoom;
        }

        public GameStatus GameStatus { get; private set; }
        public List<Player> Players;
        private List<GameLog> gameLogs;

        Game()
        {
            this.GameStatus = GameStatus.Waitting;
            this.gameLogs = new List<GameLog>();
            this.Players = new List<Player>();
        }

        /// <summary>
        /// 玩家進入
        /// </summary>
        /// <param name="player"></param>
        public void PlayerIn(Player player)
        {
            this.Players.Add(player);

            if (this.Players.Count == 2)
            {
                this.GameStatus = GameStatus.Gamming;
                this.Players.ForEach(async p => await p.WebSocket.SendAsync((int)this.GameStatus));
            }
            else
            {
                this.GameStatus = GameStatus.Waitting;
                this.Players.ForEach(async p => await p.WebSocket.SendAsync((int)this.GameStatus));
            }
        }

        /// <summary>
        /// 玩家退出
        /// </summary>
        /// <param name="player"></param>
        public void PlayerOut(Player player)
        {
            this.Players.Remove(player);
            this.gameLogs.Clear();

            if (this.Players.Count == 0)
            {
                games.Remove(this);
            }
            else
            {
                this.GameStatus = GameStatus.GammingLeaveSomeone; this.Players.ForEach(async p => await p.WebSocket.SendAsync((int)this.GameStatus));
            }
        }

        /// <summary>
        /// 玩家出招
        /// </summary>
        /// <param name="player"></param>
        /// <param name="card"></param>
        public async void PlayerExecuteAsync(Player player, Card card)
        {
            // deny player exec 2times
            if (gameLogs.Any(gl => gl.Player == player)) return;

            GameLog log = new GameLog(player, card);
            gameLogs.Add(log);

            // 一方未出招
            if (gameLogs.Count < 2)
            {
                this.GameStatus = GameStatus.WaittingPlayerExec;
                this.Players.ForEach(async p => await p.WebSocket.SendAsync((int)this.GameStatus));
                return;
            }

            // 平手
            if (gameLogs.GroupBy(gl => gl.Card).Count() > 2 ||
                gameLogs.GroupBy(gl => gl.Card).Count() == 1)
            {
                this.GameStatus = GameStatus.SafeAndKeepGamming;
                this.Players.ForEach(
                    async p =>
                    await p.WebSocket.SendAsync($"{(int)this.GameStatus},{gameLogs.First(gl => gl.Player == p).Card},{gameLogs.First(gl => gl.Player != p).Card}"));
                this.gameLogs.Clear();
                return;
            }

            // 分出勝負
            Player winner = gameLogs
                .OrderByDescending(gl => gl.Card)
                .First()
                .Player;
            Player loser = gameLogs
                .SkipWhile(gl => gl.Player == winner)
                .First()
                .Player;

            this.GameStatus = GameStatus.GameOver;
            await winner.WebSocket.SendAsync($"{100},{this.gameLogs.First(gl => gl.Player == winner).Card},{this.gameLogs.First(gl => gl.Player == loser).Card}");
            await loser.WebSocket.SendAsync($"{101},{this.gameLogs.First(gl => gl.Player == loser).Card},{this.gameLogs.First(gl => gl.Player == winner).Card}");
            this.Players.Clear();
            this.gameLogs.Clear();
            games.Remove(this);
        }
    }
}
