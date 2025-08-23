using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessLogic
{
    public class NormalMove : Move
    {
        public override MoveType Type => MoveType.Normal;

        public override Postion from { get; }
        public override Postion to { get; }

        public NormalMove(Postion from, Postion to)
        {
            this.to = to;
            this.from = from;
        }


        /*
         *function that executes the base moves by moving the given from loaction to the end location
         *input: the board
         *ouput: none
        */
        public override bool Execute(Board board)
        {
            Piece piece = board[from];
            bool capture = !board.IsEmpty(to);
            board[to] = piece;
            board[from] = null;
            piece.HasMoved = true;
            return capture || piece.Type == PieceType.Pawn;
        }
    }
}
