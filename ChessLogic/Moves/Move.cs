using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessLogic
{
    public abstract class Move
    {
        public abstract MoveType Type {get;}
        public abstract Postion from {get;}
        public abstract Postion to {get;}

        public abstract void Execute(Board board);
    }
}
