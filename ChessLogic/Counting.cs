using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessLogic
{
    public class Counting
    {
        private readonly Dictionary<PieceType,int> whiteCount = new();
        private readonly Dictionary<PieceType,int> blackCount = new();

        public int TotalCount { get; set; }

        public Counting()
        {
            foreach(PieceType piece in Enum.GetValues(typeof(PieceType)))
            {
                whiteCount[piece] = 0;
                blackCount[piece] = 0;
            }
        }

        public void Increment(Player player,PieceType piece)
        {
            if(player == Player.White) whiteCount[piece]++;
            else if(player == Player.Black) blackCount[piece]++;
            TotalCount++;
        }

        public int White(PieceType piece)
        {
            return whiteCount[piece];
        }

        public int Black(PieceType piece)
        {
            return blackCount[piece];
        }


    }
}
