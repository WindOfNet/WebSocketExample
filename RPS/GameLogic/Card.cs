using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RPS.GameLogic
{
    public class Card : IComparable<Card>
    {
        enum cardValue
        {
            /// <summary>
            /// 石頭
            /// </summary>
            ROCK,
            /// <summary>
            /// 布
            /// </summary>
            PAPER,
            /// <summary>
            /// 剪刀
            /// </summary>
            SHOTGUN
        }
        cardValue value;

        private Card(cardValue value) { this.value = value; }

        public readonly static Card ROCK = new Card(cardValue.ROCK);
        public readonly static Card PAPER = new Card(cardValue.PAPER);
        public readonly static Card SHOTGUN = new Card(cardValue.SHOTGUN);
        
        public int CompareTo(Card other)
        {
            if (other.value == this.value) return 0;
            if (other.value == cardValue.PAPER && this.value == cardValue.SHOTGUN) return 1;
            if (other.value == cardValue.SHOTGUN && this.value == cardValue.ROCK) return 1;
            if (other.value == cardValue.ROCK && this.value == cardValue.PAPER) return 1;
            return -1;
        }

        public override string ToString()
        {
            return value.ToString();
        }
    }
}
