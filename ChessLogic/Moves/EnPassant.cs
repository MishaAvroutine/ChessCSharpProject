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

        public override Position from { get; }
        public override Position to { get; }

        private readonly Position capturePos;

        public EnPassant(Position from, Position to)
        {
            this.from = from;
            this.to = to;

            capturePos = new Position(from.row, to.column); // diagonal
        }


        public override bool Execute(Board board)
        {
            new NormalMove(from, to).Execute(board);
            board[capturePos] = null;
            return true;
        }


    }
}
