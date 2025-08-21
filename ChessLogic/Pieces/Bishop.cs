using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessLogic
{
    public class Bishop : Piece
    {
        public override PieceType Type => PieceType.Bishop;
        public override Player Color { get; }

        public static readonly Direction[] directions = new Direction[]
        {
            Direction.NorthWest,
            Direction.SouthEast,
            Direction.SouthWest,
            Direction.NorthEast,
        };


        public Bishop(Player color) => Color = color;

        public override Piece Copy()
        {
            Bishop copy = new Bishop(Color);
            copy.HasMoved = HasMoved;
            return copy;
        }


        public override IEnumerable<Move> GetMoves(Postion from, Board board)
        {
            return MovesPositionInDirs(from, board, directions).Select(to => new NormalMove(from, to));
        }
    }
}
