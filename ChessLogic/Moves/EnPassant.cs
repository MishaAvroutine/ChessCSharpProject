using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessLogic
{
    public class EnPassant : Move
    {
        public override MoveType Type => MoveType.EnPassant;

        public override Postion from { get; }
        public override Postion to { get; }

        private readonly Postion capturePos;

        public EnPassant(Postion from, Postion to)
        {
            this.from = from;
            this.to = to;

            capturePos = new Postion(from.row, to.column); // diagonal
        }


        public override bool Execute(Board board)
        {
            new NormalMove(from, to).Execute(board);
            board[capturePos] = null;
            return true;
        }


    }
}
