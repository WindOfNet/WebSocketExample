using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BluffDice
{
    public class Card
    {
        public int[] Dice { get; }
        public bool HasRoll { get { return this.Dice.All(n => n > 0); } }

        public Card()
        {
            Dice = new int[5];
        }

        public void RandomCard()
        {
            Random rnd = new Random(Guid.NewGuid().GetHashCode());
            for (int i = 0; i < this.Dice.Length; i++)
                this.Dice[i] = rnd.Next(1, 7);
        }
    }
}
