using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessLogic
{
    public static class ChessAI
    {
        // Internal undo struct (kept compatible with your previous implementation)
        private struct UndoData
        {
            public Move Move;
            public Position From;
            public Position To;
            public Piece MovedPieceBefore;
            public bool MovedPieceHadMovedBefore;
            public Piece CapturedPiece;
            public Position CapturedAt;
            public bool IsCastle;
            public Position RookFrom;
            public Position RookTo;
            public bool RookHadMovedBefore;
            public bool IsPromotion;
            public Position PrevWhiteSkip;
            public Position PrevBlackSkip;
        }

        private const int DEFAULT_SEARCH_DEPTH = 5;

        // Material values (centipawns)
        private static readonly Dictionary<PieceType, int> PieceValues = new Dictionary<PieceType, int>
        {
            { PieceType.Pawn, 100 },
            { PieceType.Knight, 320 },
            { PieceType.Bishop, 330 },
            { PieceType.Rook, 500 },
            { PieceType.Queen, 900 },
            { PieceType.King, 20000 }
        };

        // Piece-square tables (simple examples, white's perspective; flipped for black).
        // Rows: 0 = top (rank 8), 7 = bottom (rank 1)
        private static readonly int[,] PawnPST = new int[8, 8] {
            { 0,  0,  0,   0,   0,  0,  0,  0 },
            { 5, 10, 10, -20, -20, 10, 10,  5 },
            { 5, -5,-10,   0,   0,-10, -5,  5 },
            { 0,  0,  0,  20,  20,  0,  0,  0 },
            { 5,  5, 10,  25,  25, 10,  5,  5 },
            {10, 10, 20,  30,  30, 20, 10, 10 },
            {50, 50, 50,  50,  50, 50, 50, 50 },
            { 0,  0,  0,   0,   0,  0,  0,  0 }
        };

        private static readonly int[,] KnightPST = new int[8, 8] {
            {-50,-40,-30,-30,-30,-30,-40,-50},
            {-40,-20,  0,  0,  0,  0,-20,-40},
            {-30,  0, 10, 15, 15, 10,  0,-30},
            {-30,  5, 15, 20, 20, 15,  5,-30},
            {-30,  0, 15, 20, 20, 15,  0,-30},
            {-30,  5, 10, 15, 15, 10,  5,-30},
            {-40,-20,  0,  5,  5,  0,-20,-40},
            {-50,-40,-30,-30,-30,-30,-40,-50}
        };

        private static readonly int[,] BishopPST = new int[8, 8] {
            {-20,-10,-10,-10,-10,-10,-10,-20},
            {-10,  0,  0,  0,  0,  0,  0,-10},
            {-10,  0,  5, 10, 10,  5,  0,-10},
            {-10,  5,  5, 10, 10,  5,  5,-10},
            {-10,  0, 10, 10, 10, 10,  0,-10},
            {-10, 10, 10, 10, 10, 10, 10,-10},
            {-10,  5,  0,  0,  0,  0,  5,-10},
            {-20,-10,-10,-10,-10,-10,-10,-20}
        };

        private static readonly int[,] RookPST = new int[8, 8] {
            { 0,  0,  0,  0,  0,  0,  0,  0 },
            { 5, 10, 10, 10, 10, 10, 10,  5 },
            {-5,  0,  0,  0,  0,  0,  0, -5 },
            {-5,  0,  0,  0,  0,  0,  0, -5 },
            {-5,  0,  0,  0,  0,  0,  0, -5 },
            {-5,  0,  0,  0,  0,  0,  0, -5 },
            {-5,  0,  0,  0,  0,  0,  0, -5 },
            { 0,  0,  0,  5,  5,  0,  0,  0 }
        };

        private static readonly int[,] QueenPST = new int[8, 8] {
            {-20,-10,-10, -5, -5,-10,-10,-20},
            {-10,  0,  0,  0,  0,  0,  0,-10},
            {-10,  0,  5,  5,  5,  5,  0,-10},
            { -5,  0,  5,  5,  5,  5,  0, -5},
            {  0,  0,  5,  5,  5,  5,  0, -5},
            {-10,  5,  5,  5,  5,  5,  0,-10},
            {-10,  0,  5,  0,  0,  0,  0,-10},
            {-20,-10,-10, -5, -5,-10,-10,-20}
        };

        private static readonly int[,] KingPST = new int[8, 8] {
            {-30,-40,-40,-50,-50,-40,-40,-30},
            {-30,-40,-40,-50,-50,-40,-40,-30},
            {-30,-40,-40,-50,-50,-40,-40,-30},
            {-30,-40,-40,-50,-50,-40,-40,-30},
            {-20,-30,-30,-40,-40,-30,-30,-20},
            {-10,-20,-20,-20,-20,-20,-20,-10},
            { 20, 20,  0,  0,  0,  0, 20, 20},
            { 20, 30, 10,  0,  0, 10, 30, 20}
        };

        // Transposition table
        private enum TTFlag { Exact, LowerBound, UpperBound }
        private struct TTEntry
        {
            public int Depth;
            public int Value;
            public TTFlag Flag;
            public Move BestMove;
        }
        private static readonly Dictionary<ulong, TTEntry> transpositionTable = new Dictionary<ulong, TTEntry>(1 << 20);

        // Killer moves: store top two killer moves per depth
        private static readonly Move[,] killers = new Move[64, 2];

        // History heuristic (move -> score)
        private static readonly Dictionary<ulong, int> historyTable = new Dictionary<ulong, int>();

        // Opening book instance
        private static readonly OpeningBook openingBook = new OpeningBook();

        // Cache for position-to-algebraic conversions
        private static readonly Dictionary<(int row, int col), string> posToAlgCache = new Dictionary<(int, int), string>();
        
        // Pre-populate position cache
        static ChessAI()
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    char file = (char)('a' + col);
                    char rank = (char)('1' + (7 - row));
                    posToAlgCache[(row, col)] = new string(new[] { file, rank });
                }
            }
        }

        // Optimized board hashing with reduced allocations
        private static ulong HashBoardSnapshot(Board board, Player toMove)
        {
            const ulong FNV_offset = 14695981039346656037UL;
            const ulong FNV_prime = 1099511628211UL;
            ulong hash = FNV_offset;
            
            unchecked
            {
                // Hash pieces directly without string allocation
                for (int row = 0; row < 8; row++)
                {
                    for (int col = 0; col < 8; col++)
                    {
                        Position pos = new Position(row, col);
                        Piece piece = board[pos];
                        byte pieceValue = PieceToByte(piece);
                        hash ^= pieceValue;
                        hash *= FNV_prime;
                    }
                }

                // Add side to move
                hash ^= (byte)(toMove == Player.White ? 1 : 0);
                hash *= FNV_prime;

                // Add en passant info if available
                try
                {
                    Position wp = board.GetPawnSkippedPosition(Player.White);
                    Position bp = board.GetPawnSkippedPosition(Player.Black);
                    if (wp != null)
                    {
                        hash ^= (byte)(wp.row * 8 + wp.column + 64);
                        hash *= FNV_prime;
                    }
                    if (bp != null)
                    {
                        hash ^= (byte)(bp.row * 8 + bp.column + 128);
                        hash *= FNV_prime;
                    }
                }
                catch { /* ignore */ }
            }
            
            return hash;
        }

        // Fast piece to byte conversion
        private static byte PieceToByte(Piece p)
        {
            if (p == null) return 0;
            
            byte typeValue = p.Type switch
            {
                PieceType.Pawn => 1,
                PieceType.Knight => 2,
                PieceType.Bishop => 3,
                PieceType.Rook => 4,
                PieceType.Queen => 5,
                PieceType.King => 6,
                _ => 0
            };
            
            return p.Color == Player.White ? typeValue : (byte)(typeValue + 8);
        }

        // Optimized move key generation
        private static ulong MoveToKey(Move m)
        {
            if (m?.from == null || m.to == null) return 0UL;
            
            ulong fromIdx = (ulong)(m.from.row * 8 + m.from.column);
            ulong toIdx = (ulong)(m.to.row * 8 + m.to.column);
            ulong promo = (m.Type == MoveType.PawnPromotion && m is PawnPromotion pp) ? (ulong)pp.PromotionType : 0UL;
            
            return fromIdx | (toIdx << 6) | (promo << 12);
        }

        // Public entry with optimized book move selection
        public static Move ChooseBestMove(GameState state, int maxDepth = DEFAULT_SEARCH_DEPTH, Action<string> progress = null)
        {
            if (state?.Board == null) return null;
            if (maxDepth <= 0) maxDepth = 1;

            // Quick book move check first - avoid expensive operations
            Move bookMove = openingBook.TrySelectBookMoveOptimized(state);
            if (bookMove != null)
            {
                progress?.Invoke($"AI book move {FormatMoveOptimized(bookMove)}");
                return bookMove;
            }

            // Clear heuristics before search
            Array.Clear(killers, 0, killers.Length);
            historyTable.Clear();
            transpositionTable.Clear();

            Move bestMove = null;
            Board rootBoard = state.Board.Copy();
            Player rootToMove = state.CurrentPlayer;

            // Iterative deepening
            for (int depth = 1; depth <= maxDepth; depth++)
            {
                int alpha = int.MinValue + 1;
                int beta = int.MaxValue - 1;
                int bestScore = int.MinValue;
                Move localBest = null;

                // Get and order root moves more efficiently
                var rootMoves = GetAllLegalMovesOptimized(rootBoard, rootToMove);
                if (rootMoves.Count == 0)
                {
                    progress?.Invoke($"AI depth {depth}: no legal moves available");
                    break;
                }

                // Move PV move to front if available
                if (bestMove != null)
                {
                    for (int i = 0; i < rootMoves.Count; i++)
                    {
                        if (MovesEqual(rootMoves[i], bestMove))
                        {
                            var mv = rootMoves[i];
                            rootMoves.RemoveAt(i);
                            rootMoves.Insert(0, mv);
                            break;
                        }
                    }
                }

                foreach (var move in rootMoves)
                {
                    if (move?.from == null || move.to == null) continue;
                    
                    UndoData undo;
                    if (!TryApplyMove(rootBoard, move, out undo)) continue;
                    
                    int score = -Negamax(rootBoard, depth - 1, -beta, -alpha, rootToMove.Opponent());
                    UndoMove(rootBoard, undo);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        localBest = move;
                    }
                    alpha = Math.Max(alpha, score);
                }

                if (localBest != null) bestMove = localBest;
                progress?.Invoke($"AI depth {depth} score {bestScore} best {FormatMoveOptimized(localBest)}");
            }

            return bestMove;
        }

        // Optimized negamax with better error handling
        private static int Negamax(Board board, int depth, int alpha, int beta, Player toMove)
        {
            if (board == null) return 0;
            
            // Transposition table lookup
            ulong key = HashBoardSnapshot(board, toMove);
            if (transpositionTable.TryGetValue(key, out TTEntry entry) && entry.Depth >= depth)
            {
                if (entry.Flag == TTFlag.Exact) return entry.Value;
                if (entry.Flag == TTFlag.LowerBound) alpha = Math.Max(alpha, entry.Value);
                else if (entry.Flag == TTFlag.UpperBound) beta = Math.Min(beta, entry.Value);
                if (alpha >= beta) return entry.Value;
            }

            if (depth == 0)
            {
                return Quiescence(board, alpha, beta, toMove);
            }

            var moves = GetAllLegalMovesOptimized(board, toMove);
            if (moves.Count == 0)
            {
                if (board.IsInCheck(toMove))
                    return -1000000000 + (DEFAULT_SEARCH_DEPTH - depth) * 10;
                return 0;
            }

            // Sort moves for better alpha-beta pruning
            SortMoves(board, moves, depth);

            int originalAlpha = alpha;
            Move bestLocal = null;
            int value = int.MinValue;

            foreach (var move in moves)
            {
                if (move?.from == null || move.to == null) continue;
                
                UndoData undo;
                if (!TryApplyMove(board, move, out undo)) continue;

                int score = -Negamax(board, depth - 1, -beta, -alpha, toMove.Opponent());
                UndoMove(board, undo);

                if (score > value)
                {
                    value = score;
                    bestLocal = move;
                }
                if (value > alpha)
                {
                    alpha = value;
                }
                if (alpha >= beta)
                {
                    RecordKiller(move, depth);
                    RecordHistory(move, depth);
                    break;
                }
            }

            // Store in transposition table
            TTFlag flag = TTFlag.Exact;
            if (value <= originalAlpha) flag = TTFlag.UpperBound;
            else if (value >= beta) flag = TTFlag.LowerBound;

            transpositionTable[key] = new TTEntry { Depth = depth, Value = value, Flag = flag, BestMove = bestLocal };

            return value;
        }

        
        private static List<Move> GetAllLegalMovesOptimized(Board board, Player player)
        {
            var result = new List<Move>(128); // Pre-allocate reasonable size
            
            if (board == null || player == Player.None) return result;

            var positions = board.PiecePositionsFor(player);
            foreach (Position pos in positions)
            {
                if (pos == null) continue;
                Piece piece = board[pos];
                if (piece == null) continue;

                var moves = piece.GetMoves(pos, board);
                foreach (Move move in moves)
                {
                    if (move != null && move.IsLegal(board))
                    {
                        result.Add(move);
                    }
                }
            }

            return result;
        }

        // Optimized move sorting
        private static void SortMoves(Board board, List<Move> moves, int depth)
        {
            
            for (int i = 1; i < moves.Count; i++)
            {
                Move current = moves[i];
                int currentScore = GetMoveScore(board, current, depth);
                int j = i - 1;

                while (j >= 0 && GetMoveScore(board, moves[j], depth) < currentScore)
                {
                    moves[j + 1] = moves[j];
                    j--;
                }
                moves[j + 1] = current;
            }
        }

        // Fast move scoring for ordering
        private static int GetMoveScore(Board board, Move move, int depth)
        {
            if (move?.from == null || move.to == null) return -1000000;

            int score = 0;

            // Captures (MVV-LVA)
            if (board.IsCapturingMove(move))
            {
                Piece victim = board[move.to];
                Piece attacker = board[move.from];
                int victimVal = victim != null ? PieceValues[victim.Type] : 
                               (move.Type == MoveType.EnPassant ? PieceValues[PieceType.Pawn] : 0);
                int attackerVal = attacker != null ? PieceValues[attacker.Type] : 0;
                score += 10000 + victimVal - attackerVal;
            }

            // Promotions
            if (board.IsPromotionMove(move)) score += 8000;

            // Killer moves
            if (depth < 64)
            {
                if (MovesEqual(move, killers[depth, 0])) score += 4000;
                else if (MovesEqual(move, killers[depth, 1])) score += 3000;
            }

            // History heuristic
            ulong key = MoveToKey(move);
            if (historyTable.TryGetValue(key, out int historyScore))
            {
                score += historyScore;
            }

            return score;
        }

        // Safe move application with better error handling
        private static bool TryApplyMove(Board board, Move move, out UndoData undo)
        {
            undo = default;
            
            try
            {
                undo = new UndoData
                {
                    Move = move,
                    From = move.from,
                    To = move.to,
                    MovedPieceBefore = board[move.from],
                    MovedPieceHadMovedBefore = board[move.from]?.HasMoved ?? false,
                    CapturedPiece = board[move.to],
                    CapturedAt = move.to,
                    PrevWhiteSkip = TryGetPawnSkip(board, Player.White),
                    PrevBlackSkip = TryGetPawnSkip(board, Player.Black)
                };

                switch (move.Type)
                {
                    case MoveType.CastleKS:
                    case MoveType.CastleQS:
                        undo.IsCastle = true;
                        move.Execute(board);
                        if (move.Type == MoveType.CastleKS)
                        {
                            undo.RookFrom = new Position(move.from.row, 7);
                            undo.RookTo = new Position(move.from.row, 5);
                        }
                        else
                        {
                            undo.RookFrom = new Position(move.from.row, 0);
                            undo.RookTo = new Position(move.from.row, 3);
                        }
                        undo.RookHadMovedBefore = board[undo.RookTo]?.HasMoved ?? false;
                        break;
                    case MoveType.EnPassant:
                        Position capturePos = new Position(move.from.row, move.to.column);
                        undo.CapturedAt = capturePos;
                        undo.CapturedPiece = board[capturePos];
                        move.Execute(board);
                        break;
                    case MoveType.PawnPromotion:
                        undo.IsPromotion = true;
                        move.Execute(board);
                        break;
                    default:
                        move.Execute(board);
                        break;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static Position TryGetPawnSkip(Board board, Player p)
        {
            try
            {
                return board.GetPawnSkippedPosition(p);
            }
            catch
            {
                return null;
            }
        }

        private static void UndoMove(Board board, UndoData undo)
        {
            // Restore pawn skipped squares
            try
            {
                board.SetPawnSkippedPosition(Player.White, undo.PrevWhiteSkip);
                board.SetPawnSkippedPosition(Player.Black, undo.PrevBlackSkip);
            }
            catch { /* ignore */ }

            switch (undo.Move.Type)
            {
                case MoveType.CastleKS:
                case MoveType.CastleQS:
                    Piece king = board[undo.To];
                    board[undo.From] = king;
                    board[undo.To] = null;
                    if (king != null) king.HasMoved = undo.MovedPieceHadMovedBefore;

                    Piece rook = board[undo.RookTo];
                    board[undo.RookFrom] = rook;
                    board[undo.RookTo] = null;
                    if (rook != null) rook.HasMoved = undo.RookHadMovedBefore;
                    break;
                case MoveType.EnPassant:
                    Piece pawn = board[undo.To];
                    board[undo.From] = pawn;
                    board[undo.To] = null;
                    if (pawn != null) pawn.HasMoved = undo.MovedPieceHadMovedBefore;
                    board[undo.CapturedAt] = undo.CapturedPiece;
                    break;
                case MoveType.PawnPromotion:
                    board[undo.To] = null;
                    board[undo.From] = new Pawn(undo.MovedPieceBefore.Color) { HasMoved = undo.MovedPieceHadMovedBefore };
                    break;
                default:
                    Piece moved = board[undo.To];
                    board[undo.From] = moved;
                    board[undo.To] = undo.CapturedPiece;
                    if (moved != null) moved.HasMoved = undo.MovedPieceHadMovedBefore;
                    break;
            }
        }

        // Optimized move formatting
        private static string FormatMoveOptimized(Move m)
        {
            if (m?.from == null || m.to == null) return "(null)";
            
            if (m.Type == MoveType.CastleKS) return "O-O";
            if (m.Type == MoveType.CastleQS) return "O-O-O";
            
            string from = posToAlgCache.TryGetValue((m.from.row, m.from.column), out string f) ? f : "??";
            string to = posToAlgCache.TryGetValue((m.to.row, m.to.column), out string t) ? t : "??";
            
            if (m.Type == MoveType.PawnPromotion && m is PawnPromotion pp)
            {
                char promo = pp.PromotionType switch
                {
                    PieceType.Queen => 'Q',
                    PieceType.Rook => 'R',
                    PieceType.Bishop => 'B',
                    PieceType.Knight => 'N',
                    _ => 'Q'
                };
                return from + to + "=" + promo;
            }
            
            return from + to;
        }

        private static void RecordKiller(Move move, int depth)
        {
            int idx = Math.Min(depth, 63);
            if (!MovesEqual(killers[idx, 0], move))
            {
                killers[idx, 1] = killers[idx, 0];
                killers[idx, 0] = move;
            }
        }

        private static void RecordHistory(Move move, int depth)
        {
            ulong key = MoveToKey(move);
            int add = depth * depth;
            historyTable[key] = historyTable.TryGetValue(key, out int cur) ? cur + add : add;
        }

        private static bool MovesEqual(Move a, Move b)
        {
            if (a == null || b == null) return false;
            if (a.from == null || b.from == null) return false;
            return a.from.row == b.from.row && a.from.column == b.from.column
                && a.to.row == b.to.row && a.to.column == b.to.column
                && a.Type == b.Type;
        }

        // Rest of the methods remain largely the same but with reduced exception handling overhead
        private static int Evaluate(Board board, Player perspective)
        {
            if (board == null) return 0;
            
            int material = 0;
            int pstScore = 0;

            foreach (Position pos in board.PiecePositions())
            {
                Piece p = board[pos];
                if (p == null) continue;
                
                int value = PieceValues[p.Type];
                int sign = p.Color == Player.White ? 1 : -1;
                material += sign * value;

                // PST contribution
                int pst = GetPST(p.Type, pos.row, pos.column);
                if (p.Color == Player.White) pstScore += pst;
                else pstScore -= pst;
            }

            int mobility = CountMobility(board, Player.White) - CountMobility(board, Player.Black);
            int scoreFromWhitePOV = material + pstScore + mobility;

            // Simplified endgame logic
            int totalNonKingMaterial = 0;
            foreach (Position pos in board.PiecePositions())
            {
                Piece p = board[pos];
                if (p != null && p.Type != PieceType.King)
                    totalNonKingMaterial += PieceValues[p.Type];
            }
            
            if (totalNonKingMaterial <= 1300)
            {
                Position whiteKing = FindKing(board, Player.White);
                Position blackKing = FindKing(board, Player.Black);
                if (whiteKing != null && blackKing != null)
                {
                    int advantage = material;
                    int blackToCorner = DistanceToNearestCorner(blackKing);
                    int whiteToCorner = DistanceToNearestCorner(whiteKing);
                    scoreFromWhitePOV += Math.Sign(advantage) * (whiteToCorner - blackToCorner) * 8;

                    int kingsDistance = Math.Abs(whiteKing.row - blackKing.row) + Math.Abs(whiteKing.column - blackKing.column);
                    scoreFromWhitePOV += Math.Sign(advantage) * (14 - kingsDistance);
                }
            }

            return perspective == Player.White ? scoreFromWhitePOV : -scoreFromWhitePOV;
        }

        // Fast PST lookup
        private static int GetPST(PieceType type, int row, int col)
        {
            return type switch
            {
                PieceType.Pawn => PawnPST[row, col],
                PieceType.Knight => KnightPST[row, col],
                PieceType.Bishop => BishopPST[row, col],
                PieceType.Rook => RookPST[row, col],
                PieceType.Queen => QueenPST[row, col],
                PieceType.King => KingPST[row, col],
                _ => 0
            };
        }

        private static int DistanceToNearestCorner(Position p)
        {
            int d1 = Math.Abs(p.row) + Math.Abs(p.column);
            int d2 = Math.Abs(p.row) + Math.Abs(p.column - 7);
            int d3 = Math.Abs(p.row - 7) + Math.Abs(p.column);
            int d4 = Math.Abs(p.row - 7) + Math.Abs(p.column - 7);
            return Math.Min(Math.Min(d1, d2), Math.Min(d3, d4));
        }

        private static Position FindKing(Board board, Player color)
        {
            foreach (Position pos in board.PiecePositionsFor(color))
            {
                Piece p = board[pos];
                if (p?.Type == PieceType.King) return pos;
            }
            return null;
        }

        private static int CountMobility(Board board, Player player)
        {
            return GetAllLegalMovesOptimized(board, player).Count * 2;
        }

        private static int Quiescence(Board board, int alpha, int beta, Player toMove)
        {
            if (board == null) return 0;
            
            int standPat = Evaluate(board, toMove);
            if (standPat >= beta) return beta;
            if (standPat > alpha) alpha = standPat;

            // Generate only tactical moves
            var tactical = new List<Move>();
            foreach (Position pos in board.PiecePositionsFor(toMove))
            {
                Piece piece = board[pos];
                if (piece == null) continue;

                var moves = piece.GetMoves(pos, board);
                foreach (Move move in moves)
                {
                    if (move != null && move.IsLegal(board) && 
                        (board.IsCapturingMove(move) || board.IsPromotionMove(move)))
                    {
                        tactical.Add(move);
                    }
                }
            }

            // Sort tactical moves
            SortMoves(board, tactical, 0);

            foreach (var move in tactical)
            {
                if (move?.from == null || move.to == null) continue;
                
                UndoData undo;
                if (!TryApplyMove(board, move, out undo)) continue;
                
                int score = -Quiescence(board, -beta, -alpha, toMove.Opponent());
                UndoMove(board, undo);

                if (score >= beta) return beta;
                if (score > alpha) alpha = score;
            }

            return alpha;
        }

        // Utility methods for external access
        public static (int entries, int depthAvg) GetTranspositionStats()
        {
            int count = transpositionTable.Count;
            int depthSum = 0;
            foreach (var v in transpositionTable.Values) depthSum += v.Depth;
            int avg = count == 0 ? 0 : depthSum / count;
            return (count, avg);
        }

        public static IEnumerable<Move> GetAllLegalMoves(Board board, Player player)
        {
            return GetAllLegalMovesOptimized(board, player);
        }

        // Opening book methods
        public static void LoadOpeningBook(string filePath)
        {
            openingBook.LoadOpeningBook(filePath);
        }

        public static void AddOpeningBook(string filePath)
        {
            openingBook.AddOpeningBook(filePath);
        }

        public static void LoadOpeningBooksFromFolder(string folderPath)
        {
            openingBook.LoadOpeningBooksFromFolder(folderPath);
        }

        public static void ClearOpeningBook()
        {
            openingBook.ClearOpeningBook();
        }

        public static void LoadOpeningSequencesFromFile(string filePath)
        {
            openingBook.LoadOpeningSequencesFromFile(filePath);
        }

        public static void LoadOpeningSequencesFromFolder(string folderPath)
        {
            openingBook.LoadOpeningSequencesFromFolder(folderPath);
        }
    }
}