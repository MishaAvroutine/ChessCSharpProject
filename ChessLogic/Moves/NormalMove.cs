using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessLogic
{
    public class NormalMove : Move
    {
        public override MoveType Type => MoveType.Normal;

        public override Postion from { get; }
        public override Postion to { get; }

        public NormalMove(Postion from, Postion to)
        {
            this.to = to;
            this.from = from;
        }

        public override void Execute(Board board)
        {
            
        }
    }
}
