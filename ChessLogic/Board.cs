namespace ChessLogic
{
    public class Board
    {
        private const string startFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR";
        public const int SIZE = 8; 
        private readonly Piece[,] board = new Piece[SIZE,SIZE];


        private readonly Dictionary<Player, Postion> pawnSkipPositions = new Dictionary<Player, Postion>()
        {
            {Player.White, null }
            , {Player.Black, null }
        };

        // get the piece at the position using row,col
        public Piece this[int row,int col] { 
            get {  
                if (row < 0 || row >= SIZE || col < 0 || col >= SIZE)
                    return null;
                return board[row,col]; 
            } 
            set { 
                if (row < 0 || row >= SIZE || col < 0 || col >= SIZE)
                    return;
                board[row, col] = value; 
            } 
        }

        // get the piece at the position using the postion class object
        public Piece this[Postion pos]
        {
            get { 
                if (pos == null) return null;
                return this[pos.row, pos.column]; 
            }
            set { 
                if (pos == null) return;
                this[pos.row, pos.column] = value; 
            }
        }


        public Postion GetPawnSkippedPosition(Player player)
        {
            return pawnSkipPositions[player];
        }

        public void SetPawnSkippedPosition(Player player,Postion pos)
        {
            pawnSkipPositions[player] = pos;
        }


        /*
         * function to initalize the start board
         * input:None
         * output:Noe
        */
        public static Board Inital()
        {
            Board board = new Board();
            board.AddStartPieces(startFen);
            return board;
        }


        /*
         * 
         * function to get the piece based on the piece type from the fen string
         * input: char PieceType
         * output: the new piece from that type
        */ 
        public static Piece GetPiece(char pieceType)
        {
            Player pieceColor = char.IsLower(pieceType) ? Player.Black : Player.White;
            switch (char.ToLower(pieceType))
            {
                case 'r':
                    return new Rook(pieceColor);
                case 'n':
                    return new Knight(pieceColor);
                case 'b':
                    return new Bishop(pieceColor);
                case 'q':
                    return new Queen(pieceColor);
                case 'k':
                    return new King(pieceColor);
                case 'p':
                    return new Pawn(pieceColor);
                default:
                    return null;
            }
        }


        /*
         * 
         * function to add the pieces based on a provided fen
         * input: the fen string
         * ouput:None
        */
        public void AddStartPieces(string fen)
        {
            int rank = 0;
            int file = 0;
            foreach(char ch in fen)
            {
                if (ch == '/')
                {
                    rank++;
                    file = 0;
                }
                else if(char.IsDigit(ch))
                {
                    file += (int)char.GetNumericValue(ch);
                }
                else
                {
                    this[rank,file] = GetPiece(ch);
                    file++;
                }
            }
        }

        /*
         * function to check if a position is inside the board
         * input: the position
         * ouput: True or False if inside the board
        */
        public static bool IsInside(Postion pos)
        {
            if (pos == null) return false;
            return pos.row < SIZE && pos.row >= 0 && pos.column < SIZE && pos.column >= 0;
        }


        /*
         * function to check if a certain position on the board is empty
         * input: the position
         * ouput: True or False if the board at that position is empty
        */
        public bool IsEmpty(Postion pos)
        {
            return this[pos] == null;
        }


        /*
         * function to get all the pieces positions on the board
         * input: None
         * ouput: all the locations of all the pieces on the board
        */
        public IEnumerable<Postion> PiecePositions()
        {
            for (int i = 0; i < SIZE; i++)
            {
                for (int j = 0; j < SIZE; j++)
                {
                    if (board[i, j] == null) continue;
                    yield return new Postion(i, j);
                }
            }
        }


        /*
         * function to get all the pieces for a certain color
         * input: the player
         * ouput: all the pieces location of a singal player
        */
        public IEnumerable<Postion> PiecePositionsFor(Player player)
        {
            return PiecePositions().Where(pos => this[pos].Color == player);
        }


        /*
         * function to check if a player is in check
         * input: the player
         * ouput: True or False if the player is in check
        */
        public bool IsInCheck(Player player)
        {
            return PiecePositionsFor(player.Opponent()).Any(pos =>
            {
                Piece piece = this[pos];
                return piece.CanCaptureOpponnentKing(pos,this);
            });
        }


        /*
         * function to copy the board
         * input: None
         * ouput: a copy of the board
        */
        public Board Copy()
        {
            Board copy = new Board();

            foreach (Postion pos in PiecePositions())
            {
                copy[pos] = this[pos].Copy();
            }
            return copy;
        }
    }


}
