using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessLogic
{
    public class Pawn : Piece
    {
        public override PieceType Type => PieceType.Pawn;
        public override Player Color { get; }

        private readonly Direction forward;

        public Pawn(Player color)
        {
            Color = color;
            if(color == Player.White)
            {
                forward = Direction.North;
            }
            else if( color == Player.Black)
            {
                forward = Direction.South;
            }
        }


        public override Piece Copy()
        {
            Pawn copy = new Pawn(Color);
            copy.HasMoved = HasMoved;
            return copy;
        }

        /*
         * 
         * function to check if the pawn can move to a certain position where it is in the board and also empty
         * input: the postion from object, the board object
         * output: true or false if it can move there
        */
        public static bool CanMoveTo(Postion pos,Board board)
        {
            return Board.IsInside(pos) && board.IsEmpty(pos);
        }

        

        /*
         * checks if a pawn can capture a piece at it's diagnal
         * 
        */
        private bool CanCapture(Postion pos,Board board)
        {
            if (!(Board.IsInside(pos) || board.IsEmpty(pos))) return false;

            return board[pos].Color != Color;
        }

        private IEnumerable<Move> ForwardMoves(Postion from, Board board)
        {
            Postion oneMovePos = from + forward;

            if (CanMoveTo(oneMovePos, board))
            {
                yield return new NormalMove(from, oneMovePos);

                Postion twoMovesPosition = oneMovePos + forward;
                if (!HasMoved && CanMoveTo(twoMovesPosition, board))
                {
                    yield return new NormalMove(from, twoMovesPosition);
                }
            }
        }

        private IEnumerable<Move> DiagonalMoves(Postion from,Board board)
        {
            foreach(Direction dir in new Direction[] {Direction.West,Direction.East})
            {
                Postion to = from + forward + dir;

                if(CanCapture(to,board))
                {
                    yield return new NormalMove(from, to);
                }
            }
        }

        protected IEnumerable<Move> GetMoves(Postion from, Board board)
        {
            return ForwardMoves(from,board).Concat(DiagonalMoves(from, board));
        }
    }
}
