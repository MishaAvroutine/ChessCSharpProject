using System;

namespace ChessLogic
{
    public class GameState
    {
        public const int MAX_MOVES_WITHOUT_ANY_CAPTURES = 50;
        public const int NUM_OF_PLAYERS = 2;
        public Board Board { get; } 
        public Player CurrentPlayer { get; private set; }

        public Result Result { get; private set; } = null;

        private int MoveCountWithoutAnyCapturesOrPawnMoves = 0;

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
            IEnumerable<Move> candidates = piece.GetMoves(pos, Board);
            IEnumerable<Move> legalMoves = candidates.Where(move => move.IsLegal(Board));
            return legalMoves;
        }

        /*
         * 
         * function to make move on the board
         * input: the move to make
         * ouput: None
        */
        public void MakeMove(Move move)
        {
            Board.SetPawnSkippedPosition(CurrentPlayer,null);
            bool captured = move.Execute(Board);
            if(captured)
            {
                MoveCountWithoutAnyCapturesOrPawnMoves = 0;
            }
            else
            {
                MoveCountWithoutAnyCapturesOrPawnMoves++;
            }
            CurrentPlayer = CurrentPlayer.Opponent();
            CheckForGameOver();
        }



        /*
         * 
         * function to get all the legal moves for one player
         * input: the player
         * output: all the legal moves of the player
        */
        public IEnumerable<Move> AllLegalMovesFor(Player player)
        {
            IEnumerable<Move> moveCandidates = Board.PiecePositionsFor(player).SelectMany(pos =>
            {
                Piece piece = Board[pos];
                return piece.GetMoves(pos, Board);
            });

            return moveCandidates.Where(move => move.IsLegal(Board));
        }


        /*
         * function to check if the game is over
         * input: none
         * ouput: None
        */
        public void CheckForGameOver()
        {
            if(!AllLegalMovesFor(CurrentPlayer).Any())
            {
                if(Board.IsInCheck(CurrentPlayer))
                {
                    Result = Result.Win(CurrentPlayer.Opponent());
                }
                else
                {
                    Result = Result.Draw(EndGame.Stalemate);
                }
            }
            else if(FiftyRuleMove())
            {
                Result = Result.Draw(EndGame.FiftyMoveRule);
            }
        }


        /*
         * function to check game is over by checking if result is set
         * input: None
         * output: None
        */
        public bool IsGameOver()
        {
            return Result != null;
        }

        private bool FiftyRuleMove()
        {
            int fullMoves = MoveCountWithoutAnyCapturesOrPawnMoves / NUM_OF_PLAYERS;
            return fullMoves == MAX_MOVES_WITHOUT_ANY_CAPTURES;
        }
    }
}
