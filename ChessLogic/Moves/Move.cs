namespace ChessLogic
{
    public abstract class Move
    {
        public abstract MoveType Type {get;}
        public abstract Postion from {get;}
        public abstract Postion to {get;}


        // abstract method to execute move
        public abstract void Execute(Board board);
    }
}
