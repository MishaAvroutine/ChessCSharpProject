using System.Windows.Media;
using System.Windows.Media.Imaging;
using ChessLogic;


namespace ChessUI
{
    public static class Images
    {

        // white image sources
        private static readonly Dictionary<PieceType, ImageSource> whitesources = new()
        {
            {PieceType.Pawn,LoadImage("Assets/PawnW.png") },
            {PieceType.Bishop,LoadImage("Assets/BishopW.png") },
            {PieceType.Knight,LoadImage("Assets/KnightW.png") },
            {PieceType.Rook,LoadImage("Assets/RookW.png") },
            {PieceType.Queen,LoadImage("Assets/QueenW.png") },
            {PieceType.King,LoadImage("Assets/KingW.png") }
        };

        // black image sources
        private static readonly Dictionary<PieceType, ImageSource> blacksources = new()
        {
            {PieceType.Pawn,LoadImage("Assets/PawnB.png") },
            {PieceType.Bishop,LoadImage("Assets/BishopB.png") },
            {PieceType.Knight,LoadImage("Assets/KnightB.png") },
            {PieceType.Rook,LoadImage("Assets/RookB.png") },
            {PieceType.Queen,LoadImage("Assets/QueenB.png") },
            {PieceType.King,LoadImage("Assets/KingB.png") }
        };


        /*
         * 
         * function to load the image with a bitmap into an imageSource
         * input: file path
         * output: the imageSource bitmap
        */
        private static ImageSource LoadImage(string filepath)
        {
            return new BitmapImage(new Uri(filepath,UriKind.Relative));
        }


        /*
         * function to get the imageSource based on the piece type and color
         * input: the player color and piece type
         * ouput: the image source
        */
        public static ImageSource GetImage(Player player,PieceType pieceType)
        {
            return player switch
            {
                Player.White => whitesources[pieceType],
                Player.Black => blacksources[pieceType],
                _ => null
            };
        }


        /*
         * overloaded get image
        */
        public static ImageSource GetImage(Piece piece)
        {
            if(piece == null) return null;

            return GetImage(piece.Color,piece.Type);
        }

    }
}
