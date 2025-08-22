using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessLogic
{
    public abstract class Piece
    {
        public abstract PieceType Type { get; }
        public abstract Player Color { get; }

        public bool HasMoved { get; set; } = false;

        public abstract Piece Copy();

        // abstarct method to get all the posibble moves from the start postion
        public abstract IEnumerable<Move> GetMoves(Postion from,Board board);
        

        /*
         * function to get all the possible moves in a certain direction
         * input: the postion from/start the board and the direction of the move
         * output: the list of possible moves
        */
        protected IEnumerable<Postion> MovesPositionsInDir(Postion from,Board board,Direction dir)
        {
            for(Postion pos = from + dir;Board.IsInside(pos);pos = pos + dir)
            {
                if(board.IsEmpty(pos))
                {
                    yield return pos;
                    continue;
                }
                Piece piece = board[pos];

                if(piece.Color != Color)
                {
                    yield return pos;
                }
                yield break;
            }
        }

        // returns the possible moves in multiple directions
        protected IEnumerable<Postion> MovesPositionInDirs(Postion from,Board board,Direction[] directions)
        {
            return directions.SelectMany(dir => MovesPositionsInDir(from, board, dir));
        }


        /*
         * 
         * virtual method to check if a certain player can capture a king
         * input: the from pos, the board
         * ouput: True or false if able to capture any king
        */
        public virtual bool CanCaptureOpponnentKing(Postion from,Board board)
        {
            return GetMoves(from,board).Any(move =>
            {
                Piece piece = board[move.to];
                return  piece != null && piece.Type == PieceType.King;
            });
        }
    }
}
