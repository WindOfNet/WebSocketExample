using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BluffDice
{
    public class Game
    {
        public static List<Game> Games { get; } = new List<Game>();

        public Player Player1 { get; private set; }
        public Player Player2 { get; private set; }
        public int Round { get; private set; } = 0;
        public List<KeyValuePair<Player, Bluff>> Bluffs { get; } = new List<KeyValuePair<Player, Bluff>>();
        public KeyValuePair<Player, Bluff>? LastBluff { get { if (this.Bluffs.Count == 0) return null; return this.Bluffs.Last(); } }

        public static Game GetGameRoom()
        {
            Game result;
            result = Games.Find(g => g.Player1 == null || g.Player2 == null);
            if (result == null)
                Games.Add(result = new Game());

            return result;
        }

        public async Task Join(Player player)
        {
            if (this.Player1 == null) this.Player1 = player;
            else
            if (this.Player2 == null) this.Player2 = player;

            if (this.Player1 != null && this.Player2 != null)
            {
                Player1.CanRoll = Player2.CanRoll = true;
                await Player1.SendGameMessage(ToClientMessageType.WaitRoll);
                await Player2.SendGameMessage(ToClientMessageType.WaitRoll);
            }
        }

        public async Task Leave(Player player)
        {
            if (this.Player1 == player) Player1 = null;
            if (this.Player2 == player) Player2 = null;
            if (Player1 != null)
                await Player1.SendGameMessage(ToClientMessageType.PlayerLeave);
            if (Player2 != null)
                await Player2.SendGameMessage(ToClientMessageType.PlayerLeave);

            Games.Remove(this);
        }

        public async Task PlayerRoll(Player player)
        {
            player.Card.RandomCard();
            player.CanRoll = false;
            await player.SendGameMessage(ToClientMessageType.RollResult, player.Card.Dice);
            if (this.Player1.Card.HasRoll && this.Player2.Card.HasRoll)
            {
                // who first
                Random rnd = new Random(Guid.NewGuid().GetHashCode());
                int index = rnd.Next(1, 3);
                if (index == 1)
                {
                    Player1.SetStatus(null, true, false);
                    Player2.SetStatus(null, false, true);
                    await Player1.SendGameMessage(ToClientMessageType.WaitBluff);
                }
                if (index == 2)
                {
                    Player1.SetStatus(null, false, true);
                    Player2.SetStatus(null, true, false);
                    await Player2.SendGameMessage(ToClientMessageType.WaitBluff);
                }
            }
        }

        public async Task Bluff(Player player, int number, int count)
        {
            bool isValid = true;

            // 若有吹牛紀錄
            if (this.LastBluff.HasValue)
                // 若上一位喊的是自己
                if (this.LastBluff.Value.Key == player)
                    isValid = false;
                else
                {
                    var lastBluff = this.LastBluff.Value.Value;
                    // 驗證 x 個 y
                    if (count < lastBluff.Count)
                        isValid = false;
                    else if (count == lastBluff.Count)
                        isValid = number > lastBluff.Number;
                }

            if (false == isValid)
            {
                await player.SendGameMessage(ToClientMessageType.BluffNotValid);
                return;
            }

            this.Bluffs.Add(new KeyValuePair<Player, Bluff>(player, new Bluff { Number = number, Count = count }));
            var message = new { Number = number, Count = count, Round = ++this.Round };
            if (Player1 == player)
            {
                await Player2.ApponentBluff(message);
                Player1.SetStatus(null, false, false);
                await Player1.SendGameMessage(ToClientMessageType.BluffResult);
                Player2.SetStatus(null, true, true);
            }
            if (Player2 == player)
            {
                await Player1.ApponentBluff(message);
                Player2.CanBluff = false;
                Player2.CanDebluff = false;
                await Player2.SendGameMessage(ToClientMessageType.BluffResult);
                Player1.CanBluff = true;
                Player1.CanDebluff = true;
            }
        }

        public async Task Debluff(Player player)
        {
            //1是否特殊, 都沒喊過1才當作特殊
            bool spOne = Bluffs.Select(q => q.Value).All(b => b.Number != 1);
            Bluff lastBluff = this.Bluffs.Last().Value;

            int countOfNumber = Player1.Card.Dice.Concat(Player2.Card.Dice).Where(n => n == lastBluff.Number || (spOne ? n == 1 : false)).Count();
            bool isLastBluff = countOfNumber < lastBluff.Count;

            Player winner = null;
            if (Player1 == player)
                winner = isLastBluff ? Player1 : Player2;
            if (Player2 == player)
                winner = isLastBluff ? Player2 : Player1;

            if (winner == Player1)
            {
                await Player1.SendGameMessage(ToClientMessageType.WinResult, new
                {
                    Win = true,
                    AppoentDice = Player2.Card.Dice
                });
                await Player2.SendGameMessage(ToClientMessageType.WinResult, new
                {
                    Win = false,
                    AppoentDice = Player1.Card.Dice
                });
            }

            if (winner == Player2)
            {
                await Player2.SendGameMessage(ToClientMessageType.WinResult, new
                {
                    Win = true,
                    AppoentDice = Player1.Card.Dice
                });
                await Player1.SendGameMessage(ToClientMessageType.WinResult, new
                {
                    Win = false,
                    AppoentDice = Player2.Card.Dice
                });
            }

            this.Player1 = null;
            this.Player2 = null;
            Games.Remove(this);
        }
    }
}
