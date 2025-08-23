using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessLogic
{
    public class King : Piece
    {
        public override PieceType Type => PieceType.King;
        public override Player Color { get; }

        public static readonly Direction[] directions = new Direction[]
        {
            Direction.North,
            Direction.South,
            Direction.East,
            Direction.West,
            Direction.NorthEast,
            Direction.NorthWest,
            Direction.SouthEast,
            Direction.SouthWest,
        };


        public King(Player color) => Color = color;

        public override Piece Copy()
        {
            King copy = new King(Color);
            copy.HasMoved = HasMoved;
            return copy;
        }

        // get all king moves
        private IEnumerable<Postion> MovePositions(Postion from, Board board)
        {
            foreach(Direction dir in directions)
            {
                Postion to = from + dir;
                if (!Board.IsInside(to)) continue;

                if(board.IsEmpty(to) || board[to].Color != Color)
                {
                    yield return to;
                }
            }
        }

        public override IEnumerable<Move> GetMoves(Postion from, Board board)
        {
            foreach(Postion to in MovePositions(from,board))
            {
                yield return new NormalMove(from, to);
            }

            if(CanCastleKingSide(from, board)) yield return new Castle(MoveType.CastleKS,from);
            if(CanCastleQueenSide(from, board)) yield return new Castle(MoveType.CastleQS,from);
        }

        public override bool CanCaptureOpponnentKing(Postion from, Board board)
        {
            return MovePositions(from, board).Any(to => {
                Piece piece = board[to];
                return piece != null &&  piece.Type == PieceType.King;
            });
        }


        private static bool IsUnMovedRook(Postion from, Board board)
        {
            if (board.IsEmpty(from)) return false;
            return !board[from].HasMoved && board[from].Type == PieceType.Rook;
        }
           
        
        private static bool AllEmpty(IEnumerable<Postion> postions, Board board)
        {
            return postions.All(pos => board.IsEmpty(pos));
        }

        private bool CanCastleKingSide(Postion from, Board board)
        {
            if(HasMoved)
            {
                return false;
            }
            Postion rookPos = new Postion(from.row, 7);

            Postion[] postions = new Postion[] { new(from.row, 6), new(from.row, 5) };

            return IsUnMovedRook(rookPos, board) && AllEmpty(postions,board);
        }

        private bool CanCastleQueenSide(Postion from, Board board)
        {
            if (HasMoved) return false;

            Postion rookPos = new Postion(from.row, 0);

            Postion[] postions = new Postion[] { new(from.row,1), new(from.row,2), new(from.row,3)};
            return IsUnMovedRook(rookPos,board) && AllEmpty(postions, board);
        }
    }
}