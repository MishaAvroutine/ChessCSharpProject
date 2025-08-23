namespace ChessLogic
{
    public class DoublePawn : Move
    {

        public override MoveType Type => MoveType.DoublePawn;

        public override Postion from { get; }
        public override Postion to { get; }

        private readonly Postion skippedPos;

        public DoublePawn(Postion from,Postion to)
        {
            this.from = from;
            this.to = to;
            skippedPos = new Postion((from.row + to.row) / 2, from.column);
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
