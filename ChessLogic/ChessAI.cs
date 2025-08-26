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

        private const int DEFAULT_SEARCH_DEPTH = 4; // Reduced from 5 for better performance

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
        private struct TTEntry { public int Depth; public int Value; public TTFlag Flag; public Move BestMove; }
        private static readonly Dictionary<ulong, TTEntry> transpositionTable = new Dictionary<ulong, TTEntry>(1 << 20); // pre-sizing

        // Killer moves: store top two killer moves per depth
        private static readonly Move[,] killers = new Move[64, 2];

        // History heuristic (move -> score)
        private static readonly Dictionary<ulong, int> historyTable = new Dictionary<ulong, int>();

        // Opening book instance
        private static readonly OpeningBook openingBook = new OpeningBook();

        // Move ordering cache to avoid re-sorting
        private static readonly Dictionary<ulong, List<Move>> moveOrderCache = new Dictionary<ulong, List<Move>>();

        // Simple FNV-1a 64-bit hashing for board snapshot
        private static ulong HashBoardSnapshot(Board board, Player toMove)
        {
            // Build a 64-character representation for all squares (rank 8 -> 1, files a->h)
            char[] buf = new char[66]; // 64 squares + 1 side + maybe two pawn skips encoded
            for (int i = 0; i < 64; i++) buf[i] = '.'; // default empty

            foreach (Position pos in board.PiecePositions())
            {
                int index = pos.row * 8 + pos.column;
                Piece piece = board[pos];
                if (piece == null) continue;
                // map piece to single char (uppercase = white, lowercase = black)
                char c = PieceToChar(piece);
                buf[index] = c;
            }

            // side to move in position 64
            buf[64] = toMove == Player.White ? 'w' : 'b';

            // Pawn-skip (en-passant) positions: try to include them for disambiguation if available
            // Attempt to read via board.GetPawnSkippedPosition if present (you used this earlier)
            try
            {
                Position wp = board.GetPawnSkippedPosition(Player.White);
                Position bp = board.GetPawnSkippedPosition(Player.Black);
                // encode as characters if non-null
                buf[65] = (wp != null) ? (char)('A' + (wp.row * 8 + wp.column) % 26) : '0';
                // we only have space for one extra; we accept partial info — still helpful.
            }
            catch
            {
                // ignore if method not available or null
            }

            // FNV-1a 64-bit
            const ulong FNV_offset = 14695981039346656037UL;
            const ulong FNV_prime = 1099511628211UL;
            ulong hash = FNV_offset;
            unchecked
            {
                for (int i = 0; i < buf.Length; i++)
                {
                    hash ^= (byte)buf[i];
                    hash *= FNV_prime;
                }
            }
            return hash;
        }

        private static char PieceToChar(Piece p)
        {
            if (p == null) return '.';
            char c = p.Type switch
            {
                PieceType.Pawn => 'p',
                PieceType.Knight => 'n',
                PieceType.Bishop => 'b',
                PieceType.Rook => 'r',
                PieceType.Queen => 'q',
                PieceType.King => 'k',
                _ => '?'
            };
            return p.Color == Player.White ? char.ToUpper(c) : c;
        }

        // Utility to encode a move to a small key for history table (from,to,promotion)
        private static ulong MoveToKey(Move m)
        {
            if (m == null || m.from == null || m.to == null)
            {
                return 0UL;
            }
            // pack 6 bits from row/col into a 12-bit from, 12-bit to, 4-bit promo type
            int fromIdx = m.from.row * 8 + m.from.column;
            int toIdx = m.to.row * 8 + m.to.column;
            int promo = (m.Type == MoveType.PawnPromotion && m is PawnPromotion pp) ? (int)pp.PromotionType : 0;
            ulong key = ((ulong)(uint)fromIdx) | (((ulong)(uint)toIdx) << 6) | (((ulong)(uint)promo) << 12);
            return key;
        }

        // Public entry: choose best move with iterative deepening
        public static Move ChooseBestMove(GameState state, int maxDepth = DEFAULT_SEARCH_DEPTH, Action<string> progress = null)
        {
            try
            {
                if (state == null) throw new ArgumentNullException(nameof(state));
                if (maxDepth <= 0) maxDepth = 1;

                // clear heuristics before search
                Array.Clear(killers, 0, killers.Length);
                historyTable.Clear();
                transpositionTable.Clear();
                moveOrderCache.Clear();

                Move bestMove = null;
                Board rootBoard = state.Board.Copy();
                Player rootToMove = state.CurrentPlayer;

                // Sequence book: attempt to pick by exact FEN state
                Move seqBook = openingBook.TrySelectBookMove(state);
                if (seqBook != null)
                {
                    progress?.Invoke($"AI book move {FormatMove(seqBook)}");
                    return seqBook;
                }

                // Opening book: optionally pick randomized starting move if available
                Move bookMove = openingBook.TrySelectBookMove(rootBoard, rootToMove);
                if (bookMove != null)
                {
                    progress?.Invoke($"AI book move {FormatMove(bookMove)}");
                    return bookMove;
                }

                // Iterative deepening
                for (int depth = 1; depth <= maxDepth; depth++)
                {
                    try
                    {
                        int alpha = int.MinValue + 1;
                        int beta = int.MaxValue - 1;

                        // Principal variation search (alpha-beta) for root moves
                        int bestScore = int.MinValue;
                        Move localBest = null;

                        // gather root moves and order
                        var rootMoves = GetOrderedMoves(rootBoard, rootToMove, 0);

                        if (!rootMoves.Any())
                        {
                            progress?.Invoke($"AI depth {depth}: no legal moves available");
                            break;
                        }

                        // Try PV move from previous depth first (if available)
                        if (bestMove != null)
                        {
                            // move it to front if present
                            var idx = rootMoves.FindIndex(m => MovesEqual(m, bestMove));
                            if (idx > 0)
                            {
                                var mv = rootMoves[idx];
                                rootMoves.RemoveAt(idx);
                                rootMoves.Insert(0, mv);
                            }
                        }

                        foreach (var move in rootMoves)
                        {
                            if (move == null || move.from == null || move.to == null) continue;
                            
                            try
                            {
                                UndoData undo;
                                ApplyMove(rootBoard, move, out undo);
                                int score = -Negamax(rootBoard, depth - 1, -beta, -alpha, rootToMove.Opponent());
                                UndoMove(rootBoard, undo);

                                if (score > bestScore)
                                {
                                    bestScore = score;
                                    localBest = move;
                                }

                                alpha = Math.Max(alpha, score);
                            }
                            catch (Exception moveEx)
                            {
                                progress?.Invoke($"AI depth {depth} move {FormatMove(move)} failed: {moveEx.Message}");
                                continue;
                            }
                        }

                        if (localBest != null) bestMove = localBest;
                        if (progress != null)
                        {
                            string bestText = localBest != null ? FormatMove(localBest) : "(none)";
                            progress($"AI depth {depth} score {bestScore} best {bestText}");
                        }
                    }
                    catch (Exception depthEx)
                    {
                        progress?.Invoke($"AI depth {depth} failed: {depthEx.Message}");
                        break;
                    }
                    // Optionally: time control / cancellation can be added here
                }

                return bestMove;
            }
            catch (Exception ex)
            {
                progress?.Invoke($"AI search failed: {ex.Message}");
                return null;
            }
        }

        // Optimized move generation with caching
        private static List<Move> GetOrderedMoves(Board board, Player player, int depth)
        {
            ulong boardHash = HashBoardSnapshot(board, player);
            
            // Check cache first
            if (moveOrderCache.TryGetValue(boardHash, out var cachedMoves))
            {
                return cachedMoves;
            }

            // Generate and order moves
            var moves = GetAllLegalMoves(board, player).ToList();
            if (moves.Count > 1)
            {
                moves.Sort((a, b) => MoveOrderComparer(board, a, b));
            }

            // Cache the result
            moveOrderCache[boardHash] = moves;
            return moves;
        }

        // Negamax wrapper using transposition table & heuristics (returns score from side-to-move perspective)
        private static int Negamax(Board board, int depth, int alpha, int beta, Player toMove)
        {
            try
            {
                if (board == null) return 0;
                
                // transposition lookup
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
                    // Quiescence search instead of static evaluation to avoid horizon effects
                    return Quiescence(board, alpha, beta, toMove);
                }

                var moves = GetOrderedMoves(board, toMove, depth);
                if (!moves.Any())
                {
                    if (board.IsInCheck(toMove))
                        return -1000000000 + (DEFAULT_SEARCH_DEPTH - depth) * 10; // mate score negative
                    return 0; // stalemate
                }

                int originalAlpha = alpha;
                Move bestLocal = null;
                int value = int.MinValue;
                int moveCount = 0;

                foreach (var move in moves)
                {
                    if (move == null || move.from == null || move.to == null) continue;
                    
                    try
                    {
                        UndoData undo;
                        ApplyMove(board, move, out undo);

                        int score;
                        
                        // Late move reduction: reduce search depth for moves late in the list
                        if (depth >= 3 && moveCount >= 4 && !board.IsCapturingMove(move) && !board.IsPromotionMove(move))
                        {
                            // Reduce depth by 1 for quiet moves after the first few
                            score = -Negamax(board, depth - 2, -beta, -alpha, toMove.Opponent());
                            // If this move is promising, do a full search
                            if (score > alpha)
                            {
                                score = -Negamax(board, depth - 1, -beta, -alpha, toMove.Opponent());
                            }
                        }
                        else
                        {
                            score = -Negamax(board, depth - 1, -beta, -alpha, toMove.Opponent());
                        }

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
                            // Beta cutoff: record killer and history
                            RecordKiller(move, depth);
                            RecordHistory(move, depth);
                            break;
                        }
                        
                        moveCount++;
                    }
                    catch (Exception)
                    {
                        // Skip this move if it causes an error
                        continue;
                    }
                }

                // store in transposition table
                TTFlag flag = TTFlag.Exact;
                if (value <= originalAlpha) flag = TTFlag.UpperBound;
                else if (value >= beta) flag = TTFlag.LowerBound;

                transpositionTable[key] = new TTEntry { Depth = depth, Value = value, Flag = flag, BestMove = bestLocal };

                return value;
            }
            catch (Exception)
            {
                return 0; // Return neutral score on error
            }
        }

        // Replacement of original Minimax with Negamax wrapper for root; kept for compatibility
        private static int Minimax(Board board, int depth, int alpha, int beta, Player maximizingPlayer, Player toMove)
        {
            // We implement Negamax style above; this method kept as compatibility wrapper (not used).
            return Negamax(board, depth, alpha, beta, toMove);
        }

        // Move ordering comparator (higher => searched earlier)
        private static int MoveOrderComparer(Board board, Move a, Move b)
        {
            // Captures first: MVV-LVA heuristic
            int scoreA = 0, scoreB = 0;

            if (a == null || a.from == null || a.to == null) return 1;
            if (b == null || b.from == null || b.to == null) return -1;

            bool aCap = board.IsCapturingMove(a);
            bool bCap = board.IsCapturingMove(b);
            if (aCap || bCap)
            {
                if (aCap)
                {
                    Piece victim = board[a.to];
                    Piece attacker = board[a.from];
                    int victimVal = victim != null ? PieceValues[victim.Type] : (a.Type == MoveType.EnPassant ? PieceValues[PieceType.Pawn] : 0);
                    int attackerVal = attacker != null ? PieceValues[attacker.Type] : 0;
                    scoreA += 1000 + (victimVal - attackerVal);
                }
                if (bCap)
                {
                    Piece victim = board[b.to];
                    Piece attacker = board[b.from];
                    int victimVal = victim != null ? PieceValues[victim.Type] : (b.Type == MoveType.EnPassant ? PieceValues[PieceType.Pawn] : 0);
                    int attackerVal = attacker != null ? PieceValues[attacker.Type] : 0;
                    scoreB += 1000 + (victimVal - attackerVal);
                }
            }

            // Promotions next
            if (board.IsPromotionMove(a)) scoreA += 800;
            if (board.IsPromotionMove(b)) scoreB += 800;

            // Killer moves heuristic (if matches killer for this depth)
            // We don't have depth param here but killers array indexed by assumed search depth (we approximate)
            // For ordering we can check existence in killers table at common depths
            for (int d = 0; d < 64; d++)
            {
                if (!IsEmptyMove(killers[d, 0]) && MovesEqual(a, killers[d, 0])) scoreA += 400;
                if (!IsEmptyMove(killers[d, 1]) && MovesEqual(a, killers[d, 1])) scoreA += 300;
                if (!IsEmptyMove(killers[d, 0]) && MovesEqual(b, killers[d, 0])) scoreB += 400;
                if (!IsEmptyMove(killers[d, 1]) && MovesEqual(b, killers[d, 1])) scoreB += 300;
            }

            // History heuristic
            ulong ka = MoveToKey(a);
            ulong kb = MoveToKey(b);
            if (historyTable.TryGetValue(ka, out int hv)) scoreA += hv;
            if (historyTable.TryGetValue(kb, out int hv2)) scoreB += hv2;

            // final compare (descending)
            return scoreB - scoreA;
        }

        private static string FormatMove(Move m)
        {
            if (m == null || m.from == null || m.to == null) return "(null)";
            string from = PosToAlg(m.from);
            string to = PosToAlg(m.to);
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
            if (m.Type == MoveType.CastleKS) return "O-O";
            if (m.Type == MoveType.CastleQS) return "O-O-O";
            return from + to;
        }

        private static string PosToAlg(Position p)
        {
            char file = (char)('a' + p.column);
            char rank = (char)('1' + (7 - p.row));
            return new string(new[] { file, rank });
        }

        private static bool IsEmptyMove(Move m)
        {
            return m == null || m.from == null;
        }

        private static void RecordKiller(Move move, int depth)
        {
            int idx = Math.Min(depth, 63);
            if (IsEmptyMove(killers[idx, 0]) || !MovesEqual(killers[idx, 0], move))
            {
                // shift 0 -> 1, insert move as 0
                killers[idx, 1] = killers[idx, 0];
                killers[idx, 0] = move;
            }
        }

        private static void RecordHistory(Move move, int depth)
        {
            ulong key = MoveToKey(move);
            int add = depth * depth;
            if (historyTable.TryGetValue(key, out int cur)) historyTable[key] = cur + add;
            else historyTable[key] = add;
        }

        private static bool MovesEqual(Move a, Move b)
        {
            if (a == null || b == null) return false;
            if (a.from == null || b.from == null) return false;
            return a.from.row == b.from.row && a.from.column == b.from.column
                && a.to.row == b.to.row && a.to.column == b.to.column
                && a.Type == b.Type;
        }

        // Evaluate board from the perspective of 'perspective' player
        private static int Evaluate(Board board, Player perspective)
        {
            try
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

                    // PST contribution (white perspective in arrays)
                    int r = pos.row;
                    int c = pos.column;
                    int pst = 0;
                    switch (p.Type)
                    {
                        case PieceType.Pawn:
                            pst = PawnPST[r, c];
                            break;
                        case PieceType.Knight:
                            pst = KnightPST[r, c];
                            break;
                        case PieceType.Bishop:
                            pst = BishopPST[r, c];
                            break;
                        case PieceType.Rook:
                            pst = RookPST[r, c];
                            break;
                        case PieceType.Queen:
                            pst = QueenPST[r, c];
                            break;
                        case PieceType.King:
                            pst = KingPST[r, c];
                            break;
                    }

                    // For black pieces, flip table
                    if (p.Color == Player.White) pstScore += pst;
                    else pstScore -= pst;
                }

                // Mobility (light)
                int mobility = CountMobility(board, Player.White) - CountMobility(board, Player.Black);

                int scoreFromWhitePOV = material + pstScore + mobility;

                // Endgame heuristic: when non-king material is low, encourage forcing enemy king to corner
                int nonKingMaterial = Math.Abs(scoreFromWhitePOV) - (PieceValues[PieceType.King] * 0); // king excluded logically
                int totalNonKingMaterial = 0;
                foreach (Position pos in board.PiecePositions())
                {
                    Piece p = board[pos];
                    if (p == null || p.Type == PieceType.King) continue;
                    totalNonKingMaterial += PieceValues[p.Type];
                }
                if (totalNonKingMaterial <= 1300) // rough endgame threshold
                {
                    Position whiteKing = FindKing(board, Player.White);
                    Position blackKing = FindKing(board, Player.Black);
                    if (whiteKing != null && blackKing != null)
                    {
                        // Bonus for pushing the side with less material towards corners
                        int advantage = material; // + means white up material
                        int blackToCorner = DistanceToNearestCorner(blackKing);
                        int whiteToCorner = DistanceToNearestCorner(whiteKing);
                        int cornerWeight = 8; // mild
                        scoreFromWhitePOV += Math.Sign(advantage) * (whiteToCorner - blackToCorner) * cornerWeight;

                        // Encourage kings to approach each other slightly in won endgames
                        int kingsDistance = Math.Abs(whiteKing.row - blackKing.row) + Math.Abs(whiteKing.column - blackKing.column);
                        scoreFromWhitePOV += Math.Sign(advantage) * (14 - kingsDistance);
                    }
                }

                return perspective == Player.White ? scoreFromWhitePOV : -scoreFromWhitePOV;
            }
            catch (Exception)
            {
                return 0; // Return neutral score on error
            }
        }

        private static int DistanceToNearestCorner(Position p)
        {
            int d1 = Math.Abs(p.row - 0) + Math.Abs(p.column - 0);
            int d2 = Math.Abs(p.row - 0) + Math.Abs(p.column - 7);
            int d3 = Math.Abs(p.row - 7) + Math.Abs(p.column - 0);
            int d4 = Math.Abs(p.row - 7) + Math.Abs(p.column - 7);
            return Math.Min(Math.Min(d1, d2), Math.Min(d3, d4));
        }

        private static Position FindKing(Board board, Player color)
        {
            foreach (Position pos in board.PiecePositionsFor(color))
            {
                Piece p = board[pos];
                if (p != null && p.Type == PieceType.King) return pos;
            }
            return null;
        }

        private static int CountMobility(Board board, Player player)
        {
            // cheap mobility: count up to a cap
            int count = GetAllLegalMoves(board, player).Take(40).Count();
            return count * 2;
        }

        public static IEnumerable<Move> GetAllLegalMoves(Board board, Player player)
        {
            try
            {
                if (board == null) return Enumerable.Empty<Move>();
                
                IEnumerable<Move> moveCandidates = board.PiecePositionsFor(player).SelectMany(pos =>
                {
                    try
                    {
                        if (pos == null) return Enumerable.Empty<Move>();
                        Piece piece = board[pos];
                        if (piece == null) return Enumerable.Empty<Move>();
                        return piece.GetMoves(pos, board);
                    }
                    catch
                    {
                        return Enumerable.Empty<Move>();
                    }
                });
                return moveCandidates.Where(move => move != null && move.IsLegal(board));
            }
            catch
            {
                return Enumerable.Empty<Move>();
            }
        }

        // ApplyMove / UndoMove: kept essentially the same as your implementation, but adapted to this file
        private static void ApplyMove(Board board, Move move, out UndoData undo)
        {
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
                        {
                            undo.IsCastle = true;
                            Castle c = (Castle)move;
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
                        }
                    case MoveType.EnPassant:
                        {
                            EnPassant ep = (EnPassant)move;
                            Position capturePos = new Position(move.from.row, move.to.column);
                            undo.CapturedAt = capturePos;
                            undo.CapturedPiece = board[capturePos];
                            move.Execute(board);
                            break;
                        }
                    case MoveType.PawnPromotion:
                        {
                            undo.IsPromotion = true;
                            move.Execute(board);
                            break;
                        }
                    case MoveType.DoublePawn:
                    case MoveType.Normal:
                    default:
                        {
                            move.Execute(board);
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                throw; // Re-throw to be caught by the caller
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
            try
            {
                // Restore pawn skipped squares
                try
                {
                    board.SetPawnSkippedPosition(Player.White, undo.PrevWhiteSkip);
                    board.SetPawnSkippedPosition(Player.Black, undo.PrevBlackSkip);
                }
                catch
                {
                    // ignore if board doesn't expose this
                }

                switch (undo.Move.Type)
                {
                    case MoveType.CastleKS:
                    case MoveType.CastleQS:
                        {
                            Piece king = board[undo.To];
                            board[undo.From] = king;
                            board[undo.To] = null;
                            if (king != null) king.HasMoved = undo.MovedPieceHadMovedBefore;

                            Piece rook = board[undo.RookTo];
                            board[undo.RookFrom] = rook;
                            board[undo.RookTo] = null;
                            if (rook != null) rook.HasMoved = undo.RookHadMovedBefore;
                            break;
                        }
                    case MoveType.EnPassant:
                        {
                            Piece pawn = board[undo.To];
                            board[undo.From] = pawn;
                            board[undo.To] = null;
                            if (pawn != null) pawn.HasMoved = undo.MovedPieceHadMovedBefore;

                            board[undo.CapturedAt] = undo.CapturedPiece;
                            break;
                        }
                    case MoveType.PawnPromotion:
                        {
                            // Restore pawn at 'From' and remove promoted piece at 'To'
                            board[undo.To] = null;
                            board[undo.From] = new Pawn(undo.MovedPieceBefore.Color) { HasMoved = undo.MovedPieceHadMovedBefore };
                            break;
                        }
                    case MoveType.DoublePawn:
                    case MoveType.Normal:
                    default:
                        {
                            Piece moved = board[undo.To];
                            board[undo.From] = moved;
                            board[undo.To] = undo.CapturedPiece;
                            if (moved != null) moved.HasMoved = undo.MovedPieceHadMovedBefore;
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UndoMove failed for {undo.Move?.Type}: {ex.Message} at {ex.StackTrace}");
                throw; // Re-throw to be caught by the caller
            }
        }

        // Quiescence search: only explores tactical moves (captures/promotions) to avoid horizon effects
        private static int Quiescence(Board board, int alpha, int beta, Player toMove)
        {
            try
            {
                if (board == null) return 0;
                
                int standPat = Evaluate(board, toMove);
                if (standPat >= beta) return beta;
                if (standPat > alpha) alpha = standPat;

                // Generate only tactical moves: captures and promotions
                var tactical = GetAllLegalMoves(board, toMove)
                    .Where(m => board.IsCapturingMove(m) || board.IsPromotionMove(m))
                    .ToList();

                // Move ordering by MVV-LVA on tactical moves
                tactical.Sort((a, b) => MoveOrderComparer(board, a, b));

                foreach (var move in tactical)
                {
                    if (move == null || move.from == null || move.to == null) continue;
                    
                    try
                    {
                        UndoData undo;
                        ApplyMove(board, move, out undo);
                        int score = -Quiescence(board, -beta, -alpha, toMove.Opponent());
                        UndoMove(board, undo);

                        if (score >= beta) return beta;
                        if (score > alpha) alpha = score;
                    }
                    catch (Exception)
                    {
                        // Skip this move if it causes an error
                        continue;
                    }
                }

                return alpha;
            }
            catch (Exception)
            {
                return 0; // Return neutral score on error
            }
        }

        // Helper for debugging / tuning: prints transposition stats (optional)
        public static (int entries, int depthAvg) GetTranspositionStats()
        {
            int count = transpositionTable.Count;
            int depthSum = 0;
            foreach (var v in transpositionTable.Values) depthSum += v.Depth;
            int avg = count == 0 ? 0 : depthSum / count;
            return (count, avg);
        }

        // Opening book API (very light). Call once to load a file with lines like: e2e4, d2d4, c2c4, g1f3
        public static void LoadOpeningBook(string filePath)
        {
            openingBook.LoadOpeningBook(filePath);
        }

        // Append an additional opening file to the current book (does not clear existing)
        public static void AddOpeningBook(string filePath)
        {
            openingBook.AddOpeningBook(filePath);
        }

        // Load all *.txt books from a folder (clears first)
        public static void LoadOpeningBooksFromFolder(string folderPath)
        {
            openingBook.LoadOpeningBooksFromFolder(folderPath);
        }

        // Clear any loaded opening lines
        public static void ClearOpeningBook()
        {
            openingBook.ClearOpeningBook();
        }

        // Advanced: load sequence lines (comma or space separated moves) into FEN->moves map
        // Example line: e2e4, e7e5, g1f3, b8c6
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