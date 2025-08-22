using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessLogic
{
    public class Board
    {
        public const int SIZE = 8; 
        private readonly Piece[,] board = new Piece[SIZE,SIZE]; 

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


        /*
         * function to initalize the start board
         * input:None
         * output:Noe
        */
        public static Board Inital()
        {
            Board board = new Board();
            board.AddStartPieces();
            return board;
        }


        /*
         * 
         * function to add the start pieces
         * input: None
         * ouput:None
        */
        public void AddStartPieces()
        {
            this[0, 0] = new Rook(Player.Black);
            this[0,1] = new Knight(Player.Black);
            this[0,2] = new Bishop(Player.Black);
            this[0,3] = new Queen(Player.Black);
            this[0, 4] = new King(Player.Black);
            this[0, 5] = new Bishop(Player.Black);
            this[0, 6] = new Knight(Player.Black);
            this[0, 7] = new Rook(Player.Black);

            for(int col = 0;col < SIZE;col++)
            {
                this[1,col] = new Pawn(Player.Black);
            }



            this[7, 0] = new Rook(Player.White);
            this[7, 1] = new Knight(Player.White);
            this[7, 2] = new Bishop(Player.White);
            this[7, 3] = new Queen(Player.White);
            this[7, 4] = new King(Player.White);
            this[7, 5] = new Bishop(Player.White);
            this[7, 6] = new Knight(Player.White);
            this[7, 7] = new Rook(Player.White);


            for (int col = 0; col < SIZE; col++)
            {
                this[6, col] = new Pawn(Player.White);
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
