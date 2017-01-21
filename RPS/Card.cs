using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RPS
{
    public class Card : IComparable
    {
        public readonly static Card Y = new Card(CardValue.Y);
        public readonly static Card O = new Card(CardValue.O);
        public readonly static Card W = new Card(CardValue.W);

        enum CardValue
        { Y, O, W }

        CardValue value;

        Card(CardValue v) { this.value = v; }

        public int CompareTo(object obj)
        {
            var v2 = obj as Card;
            if (this.value == v2.value) return 0;
            if (this.value == CardValue.O && v2.value == CardValue.Y) return 1;
            if (this.value == CardValue.W && v2.value == CardValue.O) return 1;
            if (this.value == CardValue.Y && v2.value == CardValue.W) return 1;

            return -1;
        }

        public override string ToString()
        {
            return this.value.ToString();
        }
    }
}
