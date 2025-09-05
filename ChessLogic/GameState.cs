namespace ChessLogic
{
    public class GameState
    {
        public const int MAX_MOVES_WITHOUT_ANY_CAPTURES = 50;
        public const int NUM_OF_PLAYERS = 2;
        public Board Board { get; } 
        public Player CurrentPlayer { get; private set; }

        public Result Result { get; private set; } = null;


        public bool WhiteCanCastleKingside { get; private set; } = true;
        public bool WhiteCanCastleQueenside { get; private set; } = true;
        public bool BlackCanCastleKingside { get; private set; } = true;
        public bool BlackCanCastleQueenside { get; private set; } = true;

        public Position EnPassantTarget { get; private set; } = null;
        public int HalfMoveClock { get; private set; } = 0;
        public int FullMoveNumber { get; private set; } = 1;

        private readonly Dictionary<string,int> positionCounts = new(); // THREEFOLD REPETITION

        public GameState(Player player, Board board, bool whiteKingside = true, bool whiteQueenside = true, 
                         bool blackKingside = true, bool blackQueenside = true, Position enPassant = null, 
                         int halfMoveClock = 0, int fullMoveNumber = 1)
        {
            CurrentPlayer = player;
            Board = board;
            WhiteCanCastleKingside = whiteKingside;
            WhiteCanCastleQueenside = whiteQueenside;
            BlackCanCastleKingside = blackKingside;
            BlackCanCastleQueenside = blackQueenside;
            EnPassantTarget = enPassant;
            HalfMoveClock = halfMoveClock;
            FullMoveNumber = fullMoveNumber;
        }

        /*
         * function to get all the possible legal moves for a piece
         * input: the position to move to
         * output: the list of legal moves
        */
        public IEnumerable<Move> LegalMovesForPiece(Position pos)
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
            // Update castling rights before making the move
            UpdateCastlingRights(move);
            
            // Update en passant target
            UpdateEnPassantTarget(move);
            
            // Execute the move
            Board.SetPawnSkippedPosition(CurrentPlayer, null);
            bool captured = move.Execute(Board);
            
            // Update move counters
            if (captured || move.Type == MoveType.PawnPromotion || move.Type == MoveType.DoublePawn)
            {
                HalfMoveClock = 0;
            }
            else
            {
                HalfMoveClock++;
            }
            
            // Update full move number after Black's move
            if (CurrentPlayer == Player.Black)
            {
                FullMoveNumber++;
            }
            
            CurrentPlayer = CurrentPlayer.Opponent();

            RecordPosition();

            CheckForGameOver();
        }


        public void RecordPosition()
        {
            string key = GetPositionKey();
            if (positionCounts.ContainsKey(key))
                positionCounts[key]++;
            else
                positionCounts[key] = 1;
        }
        /*
         * function to update castling rights based on the move made
         * input: the move that was made
         * output: none
        */
        private void UpdateCastlingRights(Move move)
        {
            // If king moves, lose all castling rights for that player
            if (Board[move.from]?.Type == PieceType.King)
            {
                if (CurrentPlayer == Player.White)
                {
                    WhiteCanCastleKingside = false;
                    WhiteCanCastleQueenside = false;
                }
                else
                {
                    BlackCanCastleKingside = false;
                    BlackCanCastleQueenside = false;
                }
            }
            
            // If rook moves, lose castling rights for that side
            if (Board[move.from]?.Type == PieceType.Rook)
            {
                if (CurrentPlayer == Player.White)
                {
                    if (move.from.column == 0) // Queenside rook
                        WhiteCanCastleQueenside = false;
                    else if (move.from.column == 7) // Kingside rook
                        WhiteCanCastleKingside = false;
                }
                else
                {
                    if (move.from.column == 0) // Queenside rook
                        BlackCanCastleQueenside = false;
                    else if (move.from.column == 7) // Kingside rook
                        BlackCanCastleKingside = false;
                }
            }
        }

        /*
         * function to update en passant target based on the move made
         * input: the move that was made
         * output: none
        */
        private void UpdateEnPassantTarget(Move move)
        {
            // Clear previous en passant target
            EnPassantTarget = null;
            
            // Set new en passant target for double pawn moves
            if (move.Type == MoveType.DoublePawn)
            {
                // Calculate the square that was jumped over
                int direction = CurrentPlayer == Player.White ? -1 : 1;
                EnPassantTarget = new Position(move.from.row + direction, move.from.column);
            }
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
            if (!AllLegalMovesFor(CurrentPlayer).Any())
            {
                if (Board.IsInCheck(CurrentPlayer))
                {
                    Result = Result.Win(CurrentPlayer.Opponent());
                }
                else
                {
                    Result = Result.Draw(EndGame.Stalemate);
                }
            }
            else if (FiftyRuleMove())
            {
                Result = Result.Draw(EndGame.FiftyMoveRule);
            }
            else if(IsThreefoldRepetition())
            {
                Result = Result.Draw(EndGame.ThreefoldRepetition);
            }
        }

        /*
         * function to export the current game state to FEN format
         * input: none
         * output: FEN string representation of the current position
        */
        public string ToFen()
        {
            // Build placement string
            string placement = BuildPlacementString();
            
            // Current player
            string currentPlayer = CurrentPlayer == Player.White ? "w" : "b";
            
            // Castling rights
            string castling = BuildCastlingString();
            
            // En passant
            string enPassant = EnPassantTarget == null ? "-" : PositionToAlgebraic(EnPassantTarget);
            
            // Move counters
            string halfMoveClock = HalfMoveClock.ToString();
            string fullMoveNumber = FullMoveNumber.ToString();
            
            return $"{placement} {currentPlayer} {castling} {enPassant} {halfMoveClock} {fullMoveNumber}";
        }

        /*
         * function to build the placement part of FEN string
         * input: none
         * output: placement string (e.g., "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR")
        */
        private string BuildPlacementString()
        {
            string result = "";
            for (int row = 0; row < Board.SIZE; row++)
            {
                int emptyCount = 0;
                for (int col = 0; col < Board.SIZE; col++)
                {
                    Piece piece = Board[row, col];
                    if (piece == null)
                    {
                        emptyCount++;
                    }
                    else
                    {
                        if (emptyCount > 0)
                        {
                            result += emptyCount.ToString();
                            emptyCount = 0;
                        }
                        result += piece.ToFenChar();
                    }
                }
                if (emptyCount > 0)
                {
                    result += emptyCount.ToString();
                }
                if (row < Board.SIZE - 1)
                {
                    result += "/";
                }
            }
            return result;
        }

        /*
         * function to build the castling rights part of FEN string
         * input: none
         * output: castling string (e.g., "KQkq" or "-")
        */
        private string BuildCastlingString()
        {
            string result = "";
            if (WhiteCanCastleKingside) result += "K";
            if (WhiteCanCastleQueenside) result += "Q";
            if (BlackCanCastleKingside) result += "k";
            if (BlackCanCastleQueenside) result += "q";
            
            return result.Length > 0 ? result : "-";
        }

        /*
         * function to convert a position to algebraic notation
         * input: position
         * output: algebraic notation string (e.g., "e4")
        */
        private string PositionToAlgebraic(Position pos)
        {
            char file = (char)('a' + pos.column);
            char rank = (char)('0' + (8 - pos.row));
            return $"{file}{rank}";
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

        /*
         * function to handle the 50 rule move game end situation
         * input: None
         * ouput: True or False if we have reached a 50 move rule violation
        */
        private bool FiftyRuleMove()
        {
            return HalfMoveClock == MAX_MOVES_WITHOUT_ANY_CAPTURES;
        }

        /*
         * function to set the result of the game (used for timer-based game over)
         * input: the result to set
         * output: None
        */
        public void SetResult(Result result)
        {
            Result = result;
        }

        public GameState Copy()
        {
            return new GameState(CurrentPlayer,Board,
                WhiteCanCastleKingside, WhiteCanCastleQueenside
                , BlackCanCastleKingside, BlackCanCastleQueenside, 
                EnPassantTarget,HalfMoveClock, FullMoveNumber);
        }

        private string GetPositionKey()
        {
            string fen = ToFen();
            string[] parts = fen.Split(' ');
            return string.Join(' ', parts[0], parts[1], parts[2], parts[3]);
        }


        public bool IsThreefoldRepetition()
        {
            string key = GetPositionKey();
            return positionCounts.ContainsKey(key) && positionCounts[key] >= 3;
        }

    }
}
