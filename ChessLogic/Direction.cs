using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessLogic
{
    public class Direction
    {

        public readonly static Direction North = new Direction(-1, 0);
        public readonly static Direction West = new Direction(0, -1);
        public readonly static Direction South = new Direction(1, 0);
        public readonly static Direction East = new Direction(0, 1);
        public readonly static Direction NorthEast = North + East;
        public readonly static Direction NorthWest = North + West;
        public readonly static Direction SouthEast = South + East;
        public readonly static Direction SouthWest = South + West;


        public int RowDelta { get; }
        public int ColumnDelta { get; }

        public Direction(int rowDelta, int columnDelta)
        {
            this.RowDelta = rowDelta;
            this.ColumnDelta = columnDelta;
        }


        // calculates the new direction sum
        public static Direction operator +(Direction a, Direction b)
        {
            return new Direction(a.RowDelta + b.RowDelta, a.ColumnDelta + b.ColumnDelta);
        }


        // uses vector calculation of direction with a scalar
        public static Direction operator *(int scalar,Direction dir1)
        {
            return new Direction(dir1.RowDelta * scalar, dir1.ColumnDelta * scalar);
        }



    }
}
