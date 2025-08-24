using ChessLogic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace ChessUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public enum FenParts
    {
        Placement,        // 0 - Board piece positions
        StartPlayer,      // 1 - Current player turn
        Castling,         // 2 - Castling availability
        EnPassant,        // 3 - En passant target square
        HalfMoveClock,    // 4 - Half-move clock
        FullMoveNumber    // 5 - Full move number
    }

    public partial class MainWindow : Window
    {
        private const string startFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"; 
        public const int SIZE = 8;
        private readonly Image[,] PieceImages = new Image[SIZE, SIZE];

        private readonly Rectangle[,] HighLights = new Rectangle[SIZE, SIZE];
        private readonly Dictionary<Postion,Move> moveCache = new Dictionary<Postion,Move>();


        // the main gamestate variable with the board and move execusion
        private GameState gameState;

        private Postion selecetedPos = null;
        public MainWindow()
        {
            InitializeComponent();
            InitilizeBoard();
            string[] fenParts = startFen.Split(' ');
            Player startPlayer = fenParts[(int)FenParts.StartPlayer] == "w" ? Player.White : Player.Black; 
            
            // Parse castling rights
            bool whiteKingside = fenParts[(int)FenParts.Castling].Contains('K');
            bool whiteQueenside = fenParts[(int)FenParts.Castling].Contains('Q');
            bool blackKingside = fenParts[(int)FenParts.Castling].Contains('k');
            bool blackQueenside = fenParts[(int)FenParts.Castling].Contains('q');
            
            // Parse en passant
            Postion enPassant = ParsePosition(fenParts[(int)FenParts.EnPassant]);
            
            // Parse move counters
            int halfMoveClock = int.Parse(fenParts[(int)FenParts.HalfMoveClock]);
            int fullMoveNumber = int.Parse(fenParts[(int)FenParts.FullMoveNumber]);
            
            gameState = new GameState(startPlayer, Board.Inital(fenParts[(int)FenParts.Placement]), 
                                     whiteKingside, whiteQueenside, blackKingside, blackQueenside,
                                     enPassant, halfMoveClock, fullMoveNumber);
            DrawBoard(gameState.Board);
            SetCursor(gameState.CurrentPlayer);
            SoundManager.PlayeGameStartSound();
        }


        /*
         * 
         * function to initialize the board in the ui with the highlight and image squars
         * input: None
         * output: None
        */
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

        
        /*
         *  function to draw and update the board based on the moves made and the image changes
         *  input: the board state
         *  output: None
        */
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


        /*
         * 
         * function to handle the clickdown event and is responsible with highlighting and choosing a piece
         * input: the object sender, the mouse event e
         * output: None
        */
        private void BoardGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsMenuOnScreen()) return;

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


        /*
         * function to get the position of the square that was choosen in the ui
         * input: the point in the ui
         * output: a new Position object which stores the position chosen in the chess board
        */
        private Postion ToSquarePosition(Point point)
        {
            double squareSize = BoardGrid.ActualWidth / SIZE;
            int row = (int)(point.Y / squareSize);
            int col = (int)(point.X / squareSize);

            return new Postion(row, col);
        }


        /*
         * function to choose the piece that we want to move and update the moves cache and highlight the possible moves for that piece
         * input: The position of the piece
         * ouput: None
        */
        private void OnFromPostionSelected(Postion pos)
        {
            try
            {
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


        /*
         * 
         * function to handle and make the move after it is selected
         * input: the move to make from->to
         * output: None
        */
        private void HandleMove(Move move)
        {
            try
            {
                bool isCapture = gameState.Board.IsCapturingMove(move);
                bool isPromotion = gameState.Board.IsPromotionMove(move);
                bool isEnPassant = move.Type == MoveType.EnPassant;
                gameState.MakeMove(move);
                DrawBoard(gameState.Board);
                SetCursor(gameState.CurrentPlayer);


                if (isCapture || isEnPassant)
                {
                    SoundManager.PlayCaptureSound();
                }
                else if(isPromotion)
                {
                    SoundManager.PlayPromoteSound();
                }
                else
                {
                    SoundManager.PlayMoveSound();
                }

                if (gameState.IsGameOver())
                {
                    ShowGameOver();
                    SoundManager.PlayGameEndSound();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error executing move: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }


        /*
         * function to finilize the move chosen and hide the highlights and make the move
         * input: the position of the move that ischosen to do
         * output: None
        */
        private void OnToPostionSelected(Postion pos)
        {
            selecetedPos = null;
            HideHighLights();
            if (moveCache.TryGetValue(pos,out Move move))
            {
                if(move.Type == MoveType.PawnPromotion)
                {
                    HandlePromotion(move.from,move.to);
                }
                else
                {
                    HandleMove(move);
                }

            }
        }
        private void HandlePromotion(Postion from,Postion to)
        {
            PieceImages[to.row, to.column].Source = Images.GetImage(gameState.CurrentPlayer, PieceType.Pawn);
            PieceImages[from.row, from.column].Source = null;

            PromotionMenu promMenu =  new PromotionMenu(gameState.CurrentPlayer);
            MenuContainer.Content = promMenu;

            promMenu.PieceSelected += type =>
            {
                MenuContainer.Content = null;
                Move promMove = new PawnPromotion(from, type, to);
                HandleMove(promMove);
            };
        }


        /*
         * 
         * function to cache the moves that a chosen piece can make
         * input: the list of moves possible
         * ouput: None
        */
        private void CacheMoves(IEnumerable<Move> moves)
        {
            moveCache.Clear();

            foreach (Move move in moves)
            {
                moveCache[move.to] = move;
            }

        }


        /*
         * 
         * function to highlight the squares from the cache
         * input: None
         * ouput: None
        */
        private void HighLightSquare()
        {
            HideHighLights();
            Color HighLightColor = Color.FromArgb(175, 49, 216, 229);
            Color CheckHighLight = Color.FromArgb(175, 94, 10, 5);
            Postion kingPos = gameState.Board.FindPiece(gameState.CurrentPlayer, PieceType.King);
            if(gameState.Board.IsInCheck(gameState.CurrentPlayer))
            {
                SolidColorBrush brush = new SolidColorBrush(CheckHighLight);
                HighLights[kingPos.row, kingPos.column].Fill = brush;

                var anim = new ColorAnimation
                {
                    From = Colors.Transparent,
                    To = CheckHighLight,
                    Duration = TimeSpan.FromSeconds(1),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                };
                brush.BeginAnimation(SolidColorBrush.ColorProperty, anim);
            }
            foreach (Postion to in moveCache.Keys)
            {
                HighLights[to.row, to.column].Fill = new SolidColorBrush(HighLightColor);
            }
        }



        /*
        * function to hide the highlighted squares
        * input: None
        * output: None
        */
        private void HideHighLights()
        {
            for (int r = 0; r < SIZE; r++)
            {
                for (int c = 0; c < SIZE; c++)
                {
                    if (HighLights[r, c].Fill is SolidColorBrush scb)
                    {
                        // Cancel any running animation
                        scb.BeginAnimation(SolidColorBrush.ColorProperty, null);
                        scb.Color = Colors.Transparent;
                    }
                    else
                    {
                        HighLights[r, c].Fill = new SolidColorBrush(Colors.Transparent);
                    }
                }
            }
        }


        /*
         * function to set the cursor of the current player move
         * input: the player who's turn is currently
         * output: None
        */
        private void SetCursor(Player player)
        {
            if(player == Player.White)
            {
                Cursor = ChessCursor.WhiteCursor;
            }
            else
            {
                Cursor = ChessCursor.BlackCursor;
            }
        }

        /*
         * function to check if any menu is on screen currently
         * input: None
         * output: True or False if the menu is on screen
        */
        private bool IsMenuOnScreen()
        {
            return MenuContainer.Content != null;
        }


        /*
         * function to parse algebraic notation position (e.g., "e4" -> Postion object)
         * input: algebraic notation string
         * output: Postion object or null if invalid
        */
        private Postion ParsePosition(string pos)
        {
            if (pos == "-" || pos.Length != 2) return null;
            int col = pos[0] - 'a'; // 'a' = 0, 'b' = 1, etc.
            int row = 8 - (pos[1] - '0'); // '1' = 7, '2' = 6, etc.
            return new Postion(row, col);
        }

        /*
         * function to display the current FEN string in a message box for debugging
         * input: none
         * output: none
        */
        private void ShowCurrentFen()
        {
            if (gameState != null)
            {
                string currentFen = gameState.ToFen();
                MessageBox.Show($"Current FEN: {currentFen}", "FEN String", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }


        /*
         * function to handle the game over situation
         * input: None
         * output: None
        */
        private void ShowGameOver()
        {
            GameOverMenu gameOverMenu = new GameOverMenu(gameState);
            MenuContainer.Content = gameOverMenu;

            gameOverMenu.OptionSelected += option =>
            {
                if (option == Option.Restart)
                {
                    MenuContainer.Content = null;
                    RestartGame();
                }
                else
                {
                    Application.Current.Shutdown();
                }
            };
        }

        private void RestartGame()
        {
            HideHighLights();
            moveCache.Clear();
            string[] fenParts = startFen.Split(' ');
            Player startPlayer = fenParts[(int)FenParts.StartPlayer] == "w" ? Player.White : Player.Black;
            
            // Parse castling rights
            bool whiteKingside = fenParts[(int)FenParts.Castling].Contains('K');
            bool whiteQueenside = fenParts[(int)FenParts.Castling].Contains('Q');
            bool blackKingside = fenParts[(int)FenParts.Castling].Contains('k');
            bool blackQueenside = fenParts[(int)FenParts.Castling].Contains('q');
            
            // Parse en passant
            Postion enPassant = ParsePosition(fenParts[(int)FenParts.EnPassant]);
            
            // Parse move counters
            int halfMoveClock = int.Parse(fenParts[(int)FenParts.HalfMoveClock]);
            int fullMoveNumber = int.Parse(fenParts[(int)FenParts.FullMoveNumber]);
            
            gameState = new GameState(startPlayer, Board.Inital(fenParts[(int)FenParts.Placement]), 
                                     whiteKingside, whiteQueenside, blackKingside, blackQueenside,
                                     enPassant, halfMoveClock, fullMoveNumber);
            DrawBoard(gameState.Board);
            SetCursor(gameState.CurrentPlayer);
            SoundManager.PlayeGameStartSound();
        }


        /*
         * handle the window keydown event of escape to show the pause menu
         * input: the object sender and the event
         * ouput: None
        */
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if(!IsMenuOnScreen())
            {
                if (e.Key == Key.Escape)
                {
                    ShowPauseMenu();
                }
                else if (e.Key == Key.F)
                {
                    ShowCurrentFen();
                }
            }
        }


        /*
         * function to show the pause menu on the screen
         * input: None
         * output:None
        */
        private void ShowPauseMenu()
        {
            ControlMenu pauseMenu = new ControlMenu();
            pauseMenu.FenTextBox.Text = gameState.ToFen();

            MenuContainer.Content = pauseMenu;

            pauseMenu.option += op =>
            {
                MenuContainer.Content = null;

                if (op == Option.Restart)
                {
                    RestartGame();
                }
            };
        }

    }
}