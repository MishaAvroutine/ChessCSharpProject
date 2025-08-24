namespace ChessLogic
{
    public class DoublePawn : Move
    {

        public override MoveType Type => MoveType.DoublePawn;

        public override Position from { get; }
        public override Position to { get; }

        private readonly Position skippedPos;

        public DoublePawn(Position from,Position to)
        {
            this.from = from;
            this.to = to;
            skippedPos = new Position((from.row + to.row) / 2, from.column); // double move skipped position
        }

        public override bool Execute(Board board)
        {
            Player player = board[from].Color;

            board.SetPawnSkippedPosition(player, skippedPos);

            new NormalMove(from, to).Execute(board); 
            return true ;
        }
    }
}
