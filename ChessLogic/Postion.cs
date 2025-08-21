using System;


namespace ChessLogic
{
    public class Postion
    {
        public int row { get; }
        public int column { get; }


        // constructor
        public Postion(int row, int column)
        {
            this.row = row;
            this.column = column;
        }
            
        /*
         * function to check the square color by cheking if the sum of the row and column is even or odd
         * input: none
         * output: the sqaure color white or black (1 or 2)
        */
        public Player SquareColor()
        {
            return (row + column) % 2 == 0 ? Player.White : Player.Black;
        }

        public override bool Equals(object obj)
        {
            return obj is Postion postion &&
                   row == postion.row &&
                   column == postion.column;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(row, column);
        }

        public static bool operator ==(Postion left, Postion right)
        {
            return EqualityComparer<Postion>.Default.Equals(left, right);
        }

        public static bool operator !=(Postion left, Postion right)
        {
            return !(left == right);
        }

        public static Postion operator +(Postion pos,Direction direction)
        {
           return new Postion(pos.row + direction.RowDelta, pos.column + direction.ColumnDelta);
        }

    }
}
