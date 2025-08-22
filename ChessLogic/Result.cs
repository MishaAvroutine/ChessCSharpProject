using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessLogic
{
    public class Result
    {
        public Player Winner { get; }

        public EndGame Reason { get; }


        public Result(Player player, EndGame reason)
        {
            this.Winner = player;
            this.Reason = reason;
        }

        public static Result Win(Player winner)
        {
            return new Result(winner, EndGame.CheckMate);
        }

        public static Result Draw(EndGame reason)
        {
            return new Result(Player.None, reason);
        }
    }
}
