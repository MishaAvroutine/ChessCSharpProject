namespace ChessLogic
{
    public abstract class Move
    {
        public abstract MoveType Type {get;}
        public abstract Postion from {get;}
        public abstract Postion to {get;}


        // abstract method to execute move
        public abstract void Execute(Board board);


        /*
         * function to check if a move is legal by making the move on a copy board and checking if it's legal
         * input: the board
         * ouput: True Or False if the move is legal
        */
        public virtual bool IsLegal(Board board)
        {
            Player player = board[from].Color;
            Board copy = board.Copy();
            Execute(copy);
            return !copy.IsInCheck(player);
        }
    }
}
