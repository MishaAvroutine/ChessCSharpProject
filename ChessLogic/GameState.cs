using System;

namespace ChessLogic
{
    public class GameState
    {
        public Board Board { get; } 
        public Player CurrentPlayer { get; private set; }

        public GameState(Player player,Board board)
        {
            CurrentPlayer = player;
            Board = board;
        }

        /*
         * function to get all the possible legal moves for a piece
         * input: the position to move to
         * output: the list of legal moves
        */
        public IEnumerable<Move> LegalMovesForPiece(Postion pos)
        {
            if(Board.IsEmpty(pos) || Board[pos].Color != CurrentPlayer)
            {
                return Enumerable.Empty<Move>();
            }

            Piece piece = Board[pos];
            return piece.GetMoves(pos, Board);
        }

        /*
         * 
         * function to make move on the board
         * input: the move to make
         * ouput: None
        */
        public void MakeMove(Move move)
        {
            move.Execute(Board);
            CurrentPlayer = CurrentPlayer.Opponent();
        }
    }
}
