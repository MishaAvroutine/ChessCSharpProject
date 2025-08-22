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

        public static Board Inital()
        {
            Board board = new Board();
            board.AddStartPieces();
            return board;
        }

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

        public static bool IsInside(Postion pos)
        {
            if (pos == null) return false;
            return pos.row < SIZE && pos.row >= 0 && pos.column < SIZE && pos.column >= 0;
        }

        public bool IsEmpty(Postion pos)
        {
            return this[pos] == null;
        }
    }
}
