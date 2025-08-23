using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessLogic
{
    public class Castle : Move
    {
        public override MoveType Type { get; }
        public override Postion from { get; }
        public override Postion to { get; }

        private readonly Direction kingMoveDir;
        private readonly Postion RookFromPos;
        private readonly Postion RookToPos;

        public Castle(MoveType type,Postion king)
        {
            this.Type = type;
            from = king;

            if(type == MoveType.CastleKS)
            {
                kingMoveDir = Direction.East;
                to = new Postion(king.row, 6);
                RookFromPos = new Postion(king.row, 7);
                RookToPos = new Postion(king.row, 5);
            }
            else if(type == MoveType.CastleQS)
            {
                kingMoveDir = Direction.West;
                to = new Postion(king.row, 2);
                RookFromPos = new Postion(king.row, 0);
                RookToPos = new Postion(king.row, 3);
            }
        }

        public override void Execute(Board board)
        {
            new NormalMove(from,to).Execute(board);
            new NormalMove(RookFromPos, RookToPos).Execute(board);
        }



        /*
         * function to check if the castle move is legal for the king by checking the positions in beetwenn the king and the rook
         * input: the board
         * ouput: True or false if it's a legal move
        */
        public override bool IsLegal(Board board)
        {
            Player player = board[from].Color;

            if (board.IsInCheck(player)) return false;
            Board copy = board.Copy();
            Postion kingCurrPos = from;

            for(int i=0;i<2;i++)
            {
                new NormalMove(kingCurrPos, kingCurrPos + kingMoveDir).Execute(copy);
                kingCurrPos = kingCurrPos + kingMoveDir;

                if (copy.IsInCheck(player)) return false;
            }
            return true;
        }

    }
}
