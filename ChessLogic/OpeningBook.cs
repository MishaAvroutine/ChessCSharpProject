using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessLogic
{
    public class OpeningBook
    {
        // Opening book (very light-weight): list of long-algebraic strings like "e2e4"; randomized selection
        private readonly List<string> openingBookLongAlgebraic = new List<string>();
        private readonly Random rng = new Random();

        // Sequence opening book: map FEN -> list of long-algebraic moves playable from that position
        private readonly Dictionary<string, List<string>> fenToBookMoves = new Dictionary<string, List<string>>();

        // Opening book API (very light). Call once to load a file with lines like: e2e4, d2d4, c2c4, g1f3
        public void LoadOpeningBook(string filePath)
        {
            try
            {
                if (!System.IO.File.Exists(filePath)) return;
                var lines = System.IO.File.ReadAllLines(filePath)
                    .Select(l => l.Trim().ToLowerInvariant())
                    .Where(l => l.Length >= 4 && l.All(ch => char.IsLetterOrDigit(ch)))
                    .ToList();
                openingBookLongAlgebraic.Clear();
                openingBookLongAlgebraic.AddRange(lines);
            }
            catch
            {
                // ignore any IO errors
            }
        }

        // Append an additional opening file to the current book (does not clear existing)
        public void AddOpeningBook(string filePath)
        {
            try
            {
                if (!System.IO.File.Exists(filePath)) return;
                var lines = System.IO.File.ReadAllLines(filePath)
                    .Select(l => l.Trim().ToLowerInvariant())
                    .Where(l => l.Length >= 4 && l.All(ch => char.IsLetterOrDigit(ch)))
                    .ToList();
                openingBookLongAlgebraic.AddRange(lines);
            }
            catch
            {
                // ignore any IO errors
            }
        }

        // Load all *.txt books from a folder (clears first)
        public void LoadOpeningBooksFromFolder(string folderPath)
        {
            try
            {
                if (!System.IO.Directory.Exists(folderPath)) return;
                openingBookLongAlgebraic.Clear();
                foreach (var file in System.IO.Directory.EnumerateFiles(folderPath, "*.txt"))
                {
                    AddOpeningBook(file);
                }
            }
            catch
            {
                // ignore any IO errors
            }
        }

        // Clear any loaded opening lines
        public void ClearOpeningBook()
        {
            openingBookLongAlgebraic.Clear();
            fenToBookMoves.Clear();
        }

        // Advanced: load sequence lines (comma or space separated moves) into FEN->moves map
        // Example line: e2e4, e7e5, g1f3, b8c6
        public void LoadOpeningSequencesFromFile(string filePath)
        {
            if (!System.IO.File.Exists(filePath)) return;
            foreach (var raw in System.IO.File.ReadAllLines(filePath))
            {
                string line = raw.Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;
                // split by comma or whitespace
                var tokens = line.Split(new[] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                                  .Select(t => t.Trim().ToLowerInvariant())
                                  .ToList();
                if (tokens.Count == 0) continue;
                // Build from initial position
                Board b = Board.Inital("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR");
                GameState gs = new GameState(Player.White, b);

                for (int i = 0; i < tokens.Count; i++)
                {
                    string fenKey = gs.ToFen();
                    string la = tokens[i];
                    Move mv = TryParseLongAlgebraic(gs, la);
                    if (mv == null) break; // stop this line if illegal
                    if (!fenToBookMoves.TryGetValue(fenKey, out var list))
                    {
                        list = new List<string>();
                        fenToBookMoves[fenKey] = list;
                    }
                    if (!list.Contains(la)) list.Add(la);
                    gs.MakeMove(mv);
                    if (gs.IsGameOver()) break;
                }
            }
        }

        public void LoadOpeningSequencesFromFolder(string folderPath)
        {
            if (!System.IO.Directory.Exists(folderPath)) return;
            foreach (var file in System.IO.Directory.EnumerateFiles(folderPath, "*.pgn")
                .Concat(System.IO.Directory.EnumerateFiles(folderPath, "*.lines"))
                .Concat(System.IO.Directory.EnumerateFiles(folderPath, "*.seq"))
                .Concat(System.IO.Directory.EnumerateFiles(folderPath, "*.txt")))
            {
                LoadOpeningSequencesFromFile(file);
            }
        }

        public Move TrySelectBookMove(GameState state)
        {
            if (fenToBookMoves.Count == 0) return null;
            string fen = state.ToFen();
            if (!fenToBookMoves.TryGetValue(fen, out var list) || list.Count == 0) return null;
            var legal = ChessAI.GetAllLegalMoves(state.Board, state.CurrentPlayer).ToList();
            var candidates = new List<Move>();
            foreach (var la in list)
            {
                Move m = TryParseLongAlgebraic(state, la);
                if (m != null && legal.Any(x => MovesEqual(x, m))) candidates.Add(m);
            }
            if (candidates.Count == 0) return null;
            return candidates[rng.Next(candidates.Count)];
        }

        public Move TrySelectBookMove(Board board, Player toMove)
        {
            // Only attempt from the initial position (simple but effective to avoid repetitive first move)
            if (!openingBookLongAlgebraic.Any()) return null;

            // Detect initial setup heuristically: both sides have full back ranks and pawns in place
            bool likelyInitial = true;
            for (int c = 0; c < 8; c++)
            {
                if (!(board[new Position(1, c)] is Pawn pw && pw.Color == Player.Black)) likelyInitial = false;
                if (!(board[new Position(6, c)] is Pawn pb && pb.Color == Player.White)) likelyInitial = false;
            }
            if (!(board[new Position(0, 4)] is King bk) || bk.Color != Player.Black) likelyInitial = false;
            if (!(board[new Position(7, 4)] is King wk) || wk.Color != Player.White) likelyInitial = false;
            if (!likelyInitial) return null;

            var legal = ChessAI.GetAllLegalMoves(board, toMove).ToList();
            var candidates = new List<Move>();
            foreach (var mv in legal)
            {
                string la = PosToAlg(mv.from) + PosToAlg(mv.to); // long algebraic like e2e4
                if (openingBookLongAlgebraic.Contains(la.ToLowerInvariant())) candidates.Add(mv);
            }
            if (candidates.Count == 0) return null;
            int idx = rng.Next(candidates.Count);
            return candidates[idx];
        }

        private Move TryParseLongAlgebraic(GameState state, string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;
            string t = token.ToLowerInvariant();
            // support e2e4 or e7e8q
            if (t.Length < 4) return null;
            string from = t.Substring(0, 2);
            string to = t.Substring(2, 2);
            char promo = t.Length >= 5 ? t[4] : '\0';

            int FromFile(char f) => f - 'a';
            int FromRank(char r) => 7 - (r - '1');

            Position fromPos = new Position(FromRank(from[1]), FromFile(from[0]));
            Position toPos = new Position(FromRank(to[1]), FromFile(to[0]));

            var legal = state.LegalMovesForPiece(fromPos).Where(m => m.to.row == toPos.row && m.to.column == toPos.column);
            if (promo != '\0')
            {
                PieceType pt = promo switch { 'q' => PieceType.Queen, 'r' => PieceType.Rook, 'b' => PieceType.Bishop, 'n' => PieceType.Knight, _ => PieceType.Queen };
                legal = legal.Where(m => m is PawnPromotion pp && pp.PromotionType == pt);
            }
            return legal.FirstOrDefault();
        }

        private static string PosToAlg(Position p)
        {
            char file = (char)('a' + p.column);
            char rank = (char)('1' + (7 - p.row));
            return new string(new[] { file, rank });
        }

        private static bool MovesEqual(Move a, Move b)
        {
            if (a == null || b == null) return false;
            if (a.from == null || b.from == null) return false;
            return a.from.row == b.from.row && a.from.column == b.from.column
                && a.to.row == b.to.row && a.to.column == b.to.column
                && a.Type == b.Type;
        }
    }
} 