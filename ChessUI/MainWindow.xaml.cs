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
        public const int SIZE = 8;
        private readonly Image[,] PieceImages = new Image[SIZE, SIZE];

        private readonly Rectangle[,] HighLights = new Rectangle[SIZE, SIZE];
        private readonly Dictionary<Postion,Move> moveCache = new Dictionary<Postion,Move>();

        private GameState gameState;
        private Postion selecetedPos = null;
        public MainWindow()
        {
            InitializeComponent();
            InitilizeBoard();

            gameState = new GameState(Player.White, Board.Inital());
            DrawBoard(gameState.Board);
        }

        public void InitilizeBoard()
        {
            for(int row =0; row < SIZE;row++)
            {
                for(int col =0; col < SIZE;col++)
                {
                    Image image = new Image();
                    PieceImages[row,col] = image;
                    PieceGird.Children.Add(image);

                    Rectangle highlight = new Rectangle();
                    HighLights[row,col] = highlight;
                    HighLightGrid.Children.Add(highlight);
                }
            }
        }


        public void DrawBoard(Board board)
        {
            for (int row = 0; row < SIZE; row++)
            {
                for (int col = 0; col < SIZE; col++)
                {
                    Piece piece = board[row, col];
                    PieceImages[row, col].Source = Images.GetImage(piece);
                }
            }
        }

        private void BoardGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point point = e.GetPosition(BoardGrid);
            Postion pos = ToSquarePosition(point);

            if(selecetedPos == null)
            {
                OnFromPostionSelected(pos);
            }
            else
            {
                OnToPostionSelected(pos);
            }
        }

        private Postion ToSquarePosition(Point point)
        {
            double squareSize = BoardGrid.ActualWidth / SIZE;
            int row = (int)(point.Y / squareSize);
            int col = (int)(point.X / squareSize);

            return new Postion(row, col);
        }

        private void OnFromPostionSelected(Postion pos)
        {
            try
            {
                // Validate position is within bounds
                if (pos.row < 0 || pos.row >= SIZE || pos.column < 0 || pos.column >= SIZE)
                {
                    return;
                }

                IEnumerable<Move> moves = gameState.LegalMovesForPiece(pos);
                if(moves.Any())
                {
                    selecetedPos = pos;
                    CacheMoves(moves);
                    HighLightSquare();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error calculating moves: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                selecetedPos = null;
                HideHighLights();
            }
        }

        private void HandleMove(Move move)
        {
            try
            {
                gameState.MakeMove(move);
                DrawBoard(gameState.Board);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error executing move: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnToPostionSelected(Postion pos)
        {
            selecetedPos = null;
            HideHighLights();
            if (moveCache.TryGetValue(pos,out Move move))
            {
                HandleMove(move);
            }
        }


        private void CacheMoves(IEnumerable<Move> moves)
        {
            moveCache.Clear();

            foreach (Move move in moves)
            {
                moveCache[move.to] = move;
            }

        }


        private void HighLightSquare()
        {
            Color HighLightColor = Color.FromArgb(150, 125, 255, 125);
            foreach (Postion to in moveCache.Keys)
            {
                HighLights[to.row, to.column].Fill = new SolidColorBrush(HighLightColor); 
            }
        }

        private void HideHighLights()
        {
            foreach(Postion to in moveCache.Keys)
            {
                HighLights[to.row, to.column].Fill = Brushes.Transparent;
            }
        }



    }
}