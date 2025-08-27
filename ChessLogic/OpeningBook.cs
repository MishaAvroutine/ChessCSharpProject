using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessLogic
{
    public class OpeningBook
    {
        // Simple opening book: HashSet for O(1) lookups instead of List
        private readonly HashSet<string> openingBookLongAlgebraic = new HashSet<string>();
        private readonly Random rng = new Random();

        // Sequence opening book: map FEN -> list of moves
        private readonly Dictionary<string, List<string>> fenToBookMoves = new Dictionary<string, List<string>>();

        // Cache for initial position detection (avoid repeated calculations)
        private bool? isInitialPositionCached = null;
        private ulong? lastBoardHash = null;

        // Pre-compiled initial position data for faster detection
        private static readonly (PieceType type, Player color)[] InitialSetup = new (PieceType, Player)[]
        {
            // Black back rank (row 0)
            (PieceType.Rook, Player.Black), (PieceType.Knight, Player.Black), (PieceType.Bishop, Player.Black), (PieceType.Queen, Player.Black),
            (PieceType.King, Player.Black), (PieceType.Bishop, Player.Black), (PieceType.Knight, Player.Black), (PieceType.Rook, Player.Black),
            // White back rank (row 7)  
            (PieceType.Rook, Player.White), (PieceType.Knight, Player.White), (PieceType.Bishop, Player.White), (PieceType.Queen, Player.White),
            (PieceType.King, Player.White), (PieceType.Bishop, Player.White), (PieceType.Knight, Player.White), (PieceType.Rook, Player.White)
        };

        public void LoadOpeningBook(string filePath)
        {
            if (!System.IO.File.Exists(filePath)) return;
            
            try
            {
                openingBookLongAlgebraic.Clear();
                
                // Read all lines at once and process efficiently
                string[] lines = System.IO.File.ReadAllLines(filePath);
                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    if (IsValidLongAlgebraic(trimmed))
                    {
                        openingBookLongAlgebraic.Add(trimmed.ToLowerInvariant());
                    }
                }
            }
            catch
            {
                // Ignore IO errors silently
            }
        }

        public void AddOpeningBook(string filePath)
        {
            if (!System.IO.File.Exists(filePath)) return;
            
            try
            {
                string[] lines = System.IO.File.ReadAllLines(filePath);
                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    if (IsValidLongAlgebraic(trimmed))
                    {
                        openingBookLongAlgebraic.Add(trimmed.ToLowerInvariant());
                    }
                }
            }
            catch
            {
                // Ignore IO errors silently  
            }
        }

        public void LoadOpeningBooksFromFolder(string folderPath)
        {
            if (!System.IO.Directory.Exists(folderPath)) return;
            
            try
            {
                openingBookLongAlgebraic.Clear();
                string[] files = System.IO.Directory.GetFiles(folderPath, "*.txt");
                foreach (string file in files)
                {
                    AddOpeningBook(file);
                }
            }
            catch
            {
                // Ignore IO errors silently
            }
        }

        public void ClearOpeningBook()
        {
            openingBookLongAlgebraic.Clear();
            fenToBookMoves.Clear();
            isInitialPositionCached = null;
            lastBoardHash = null;
        }

        public void LoadOpeningSequencesFromFile(string filePath)
        {
            if (!System.IO.File.Exists(filePath)) return;
            
            try
            {
                string[] lines = System.IO.File.ReadAllLines(filePath);
                foreach (string line in lines)
                {
                    ProcessSequenceLine(line.Trim());
                }
            }
            catch
            {
                // Ignore errors silently
            }
        }

        public void LoadOpeningSequencesFromFolder(string folderPath)
        {
            if (!System.IO.Directory.Exists(folderPath)) return;
            
            try
            {
                string[] extensions = { "*.pgn", "*.lines", "*.seq", "*.txt" };
                foreach (string extension in extensions)
                {
                    string[] files = System.IO.Directory.GetFiles(folderPath, extension);
                    foreach (string file in files)
                    {
                        LoadOpeningSequencesFromFile(file);
                    }
                }
            }
            catch
            {
                // Ignore errors silently
            }
        }

        // Optimized book move selection for GameState (sequence-based)
        public Move TrySelectBookMoveOptimized(GameState state)
        {
            if (state?.Board == null) return null;
            
            // Try sequence book first
            if (fenToBookMoves.Count > 0)
            {
                try
                {
                    string fen = state.ToFen();
                    if (fenToBookMoves.TryGetValue(fen, out var moveList) && moveList.Count > 0)
                    {
                        var legalMoves = GetLegalMovesEfficiently(state);
                        var candidates = new List<Move>();
                        
                        foreach (string longAlgebraic in moveList)
                        {
                            Move move = TryParseLongAlgebraic(state, longAlgebraic);
                            if (move != null && ContainsMove(legalMoves, move))
                            {
                                candidates.Add(move);
                            }
                        }
                        
                        if (candidates.Count > 0)
                        {
                            return candidates[rng.Next(candidates.Count)];
                        }
                    }
                }
                catch
                {
                    // Fall through to simple book
                }
            }

            // Try simple opening book
            return TrySelectSimpleBookMove(state.Board, state.CurrentPlayer);
        }

        // Original method kept for backward compatibility
        public Move TrySelectBookMove(GameState state)
        {
            return TrySelectBookMoveOptimized(state);
        }

        public Move TrySelectBookMove(Board board, Player toMove)
        {
            return TrySelectSimpleBookMove(board, toMove);
        }

        private Move TrySelectSimpleBookMove(Board board, Player toMove)
        {
            if (openingBookLongAlgebraic.Count == 0 || board == null) return null;

            // Fast initial position check with caching
            if (!IsInitialPositionFast(board)) return null;

            var legalMoves = GetLegalMovesEfficiently(board, toMove);
            var candidates = new List<Move>();
            
            foreach (Move move in legalMoves)
            {
                string longAlgebraic = FormatLongAlgebraic(move);
                if (openingBookLongAlgebraic.Contains(longAlgebraic))
                {
                    candidates.Add(move);
                }
            }
            
            return candidates.Count > 0 ? candidates[rng.Next(candidates.Count)] : null;
        }

        // Optimized initial position detection with caching
        private bool IsInitialPositionFast(Board board)
        {
            // Simple hash-based caching to avoid repeated expensive checks
            ulong boardHash = ComputeSimpleBoardHash(board);
            
            if (lastBoardHash.HasValue && lastBoardHash.Value == boardHash && isInitialPositionCached.HasValue)
            {
                return isInitialPositionCached.Value;
            }

            lastBoardHash = boardHash;
            isInitialPositionCached = CheckInitialPosition(board);
            return isInitialPositionCached.Value;
        }

        private bool CheckInitialPosition(Board board)
        {
            // Check pawns first (most likely to be moved)
            for (int col = 0; col < 8; col++)
            {
                Piece blackPawn = board[new Position(1, col)];
                Piece whitePawn = board[new Position(6, col)];
                
                if (!(blackPawn is Pawn bp && bp.Color == Player.Black && !bp.HasMoved) ||
                    !(whitePawn is Pawn wp && wp.Color == Player.White && !wp.HasMoved))
                {
                    return false;
                }
            }

            // Check back ranks
            for (int col = 0; col < 8; col++)
            {
                Piece blackPiece = board[new Position(0, col)];
                Piece whitePiece = board[new Position(7, col)];
                
                var expectedBlack = InitialSetup[col];
                var expectedWhite = InitialSetup[col + 8];
                
                if (blackPiece?.Type != expectedBlack.type || blackPiece.Color != expectedBlack.color ||
                    whitePiece?.Type != expectedWhite.type || whitePiece.Color != expectedWhite.color)
                {
                    return false;
                }
            }

            return true;
        }

        // Simple hash function for board state caching
        private ulong ComputeSimpleBoardHash(Board board)
        {
            ulong hash = 0;
            
            // Hash only the pieces that matter for initial position detection
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    Piece piece = board[new Position(row, col)];
                    if (piece != null)
                    {
                        hash ^= (ulong)((int)piece.Type + (int)piece.Color * 10) << ((row * 8 + col) % 60);
                    }
                }
            }
            
            return hash;
        }

        private void ProcessSequenceLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            
            // Split by common delimiters
            string[] tokens = line.Split(new[] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) return;

            try
            {
                Board board = Board.Inital("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR");
                GameState gameState = new GameState(Player.White, board);

                for (int i = 0; i < tokens.Length; i++)
                {
                    string fen = gameState.ToFen();
                    string longAlgebraic = tokens[i].Trim().ToLowerInvariant();
                    
                    if (!IsValidLongAlgebraic(longAlgebraic)) break;
                    
                    Move move = TryParseLongAlgebraic(gameState, longAlgebraic);
                    if (move == null) break;
                    
                    // Add to book
                    if (!fenToBookMoves.TryGetValue(fen, out var moveList))
                    {
                        moveList = new List<string>();
                        fenToBookMoves[fen] = moveList;
                    }
                    
                    if (!moveList.Contains(longAlgebraic))
                    {
                        moveList.Add(longAlgebraic);
                    }
                    
                    gameState.MakeMove(move);
                    if (gameState.IsGameOver()) break;
                }
            }
            catch
            {
                // Skip this line if parsing fails
            }
        }

        // Efficient legal move generation that avoids expensive LINQ operations
        private List<Move> GetLegalMovesEfficiently(GameState state)
        {
            return GetLegalMovesEfficiently(state.Board, state.CurrentPlayer);
        }

        private List<Move> GetLegalMovesEfficiently(Board board, Player player)
        {
            var result = new List<Move>();
            
            var positions = board.PiecePositionsFor(player);
            foreach (Position pos in positions)
            {
                Piece piece = board[pos];
                if (piece == null) continue;
                
                var moves = piece.GetMoves(pos, board);
                foreach (Move move in moves)
                {
                    if (move?.IsLegal(board) == true)
                    {
                        result.Add(move);
                    }
                }
            }
            
            return result;
        }

        // Fast move comparison without LINQ
        private bool ContainsMove(List<Move> moves, Move target)
        {
            if (target?.from == null || target.to == null) return false;
            
            foreach (Move move in moves)
            {
                if (MovesEqual(move, target)) return true;
            }
            
            return false;
        }

        private Move TryParseLongAlgebraic(GameState state, string token)
        {
            if (string.IsNullOrWhiteSpace(token) || token.Length < 4) return null;
            
            string normalized = token.ToLowerInvariant();
            
            // Parse from/to positions
            if (!TryParseSquare(normalized.Substring(0, 2), out Position fromPos) ||
                !TryParseSquare(normalized.Substring(2, 2), out Position toPos))
            {
                return null;
            }
            
            // Parse promotion if present
            PieceType promotionType = PieceType.Queen;
            if (normalized.Length >= 5)
            {
                char promo = normalized[4];
                promotionType = promo switch
                {
                    'q' => PieceType.Queen,
                    'r' => PieceType.Rook,
                    'b' => PieceType.Bishop,
                    'n' => PieceType.Knight,
                    _ => PieceType.Queen
                };
            }
            
            // Find matching legal move
            var legalMoves = state.LegalMovesForPiece(fromPos);
            foreach (Move move in legalMoves)
            {
                if (move.to.row == toPos.row && move.to.column == toPos.column)
                {
                    if (normalized.Length >= 5 && move is PawnPromotion pp)
                    {
                        if (pp.PromotionType == promotionType) return move;
                    }
                    else if (normalized.Length < 5)
                    {
                        return move;
                    }
                }
            }
            
            return null;
        }

        private bool TryParseSquare(string square, out Position position)
        {
            position = null;
            if (square.Length != 2) return false;
            
            char file = square[0];
            char rank = square[1];
            
            if (file < 'a' || file > 'h' || rank < '1' || rank > '8') return false;
            
            int col = file - 'a';
            int row = 7 - (rank - '1');
            
            position = new Position(row, col);
            return true;
        }

        private bool IsValidLongAlgebraic(string move)
        {
            if (string.IsNullOrWhiteSpace(move)) return false;
            
            move = move.ToLowerInvariant();
            
            // Must be at least 4 characters (e2e4)
            if (move.Length < 4) return false;
            
            // Check basic format: letter+digit+letter+digit
            return char.IsLetter(move[0]) && char.IsDigit(move[1]) &&
                   char.IsLetter(move[2]) && char.IsDigit(move[3]) &&
                   move[0] >= 'a' && move[0] <= 'h' &&
                   move[1] >= '1' && move[1] <= '8' &&
                   move[2] >= 'a' && move[2] <= 'h' &&
                   move[3] >= '1' && move[3] <= '8';
        }

        private string FormatLongAlgebraic(Move move)
        {
            if (move?.from == null || move.to == null) return "";
            
            string from = PositionToSquare(move.from);
            string to = PositionToSquare(move.to);
            
            if (move.Type == MoveType.PawnPromotion && move is PawnPromotion pp)
            {
                char promo = pp.PromotionType switch
                {
                    PieceType.Queen => 'q',
                    PieceType.Rook => 'r',
                    PieceType.Bishop => 'b',
                    PieceType.Knight => 'n',
                    _ => 'q'
                };
                return from + to + promo;
            }
            
            return from + to;
        }

        private string PositionToSquare(Position pos)
        {
            char file = (char)('a' + pos.column);
            char rank = (char)('1' + (7 - pos.row));
            return new string(new[] { file, rank });
        }

        private bool MovesEqual(Move a, Move b)
        {
            if (a == null || b == null || a.from == null || b.from == null) return false;
            
            return a.from.row == b.from.row && a.from.column == b.from.column &&
                   a.to.row == b.to.row && a.to.column == b.to.column &&
                   a.Type == b.Type;
        }
    }
}