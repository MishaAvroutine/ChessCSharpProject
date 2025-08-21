using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using ChessLogic;

namespace ChessUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Image[,] PieceImages = new Image[8, 8];

        private GameState gameState;
        public MainWindow()
        {
            InitializeComponent();
            InitilizeBoard();

            gameState = new GameState(Player.White, Board.Inital());
            DrawBoard(gameState.Board);
        }

        public void InitilizeBoard()
        {
            for(int row =0; row < 8;row++)
            {
                for(int col =0; col < 8;col++)
                {
                    Image image = new Image();
                    PieceImages[row,col] = image;
                    PieceGird.Children.Add(image);
                }
            }
        }


        public void DrawBoard(Board board)
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    Piece piece = board[row, col];
                    PieceImages[row, col].Source = Images.GetImage(piece);
                }
            }
        }
    }
}