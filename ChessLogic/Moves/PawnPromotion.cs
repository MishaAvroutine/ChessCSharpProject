namespace ChessLogic
{
    public class PawnPromotion : Move
    {
        public override MoveType Type => MoveType.PawnPromotion;
        public override Postion from { get; }
        public override Postion to { get; }

        private readonly PieceType newType;


        public PawnPromotion(Postion fr, PieceType newType,Postion to)
        {
            this.from = fr;
            this.newType = newType;
            this.to = to;
        }



        private Piece CreatePromotionPiece(Player player)
        {
            return newType switch
            {
                PieceType.Knight => new Knight(player),
                PieceType.Rook => new Rook(player),
                PieceType.Queen => new Queen(player),
                _ => new Bishop(player),
            };
        }

        public override void Execute(Board board)
        {
            Piece pawn = board[from];
            board[from] = null;
            Piece promotionPiece = CreatePromotionPiece(pawn.Color);
            promotionPiece.HasMoved = true;
            board[to] = promotionPiece;
        }

    }
}
