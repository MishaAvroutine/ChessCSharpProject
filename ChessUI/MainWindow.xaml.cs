using ChessLogic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace ChessUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public enum FenParts
    {
        Placement,       
        StartPlayer,      
        Castling,        
        EnPassant,        
        HalfMoveClock,    
        FullMoveNumber 
    }


    public enum Modes
    {
        Regular = 30,
        Blitz = 5,
        Bullet = 1
    }

    /*
     * funny fen postion for testing
     * RRRRRRR1/7k/8/8/8/8/3K4/7R b - - 0 1
     * pppppppp/pppppppp/pppppppp/PppPPpPp/PppPPpPp/PPpPPpPP/pppppppp/pppppppp w - - 0 1
     * RRRRRRRK/pPBBBBPP/pbPBBPBP/pbbPPBBP/pbbppBBP/pbpbbpBP/ppbbbbpP/krrrrrrr w - - 0 1
     * Q2QQ1Q1/1Q4Q1/Q1Q3Q1/QQ4Q1/QQQ1Q3/7r/5k2/7K w - - 0 1
     * 
     * 
     * 
     */
    public partial class MainWindow : Window
    {
        private const string startFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"; 
        public const int SIZE = 8;
        private readonly Image[,] PieceImages = new Image[SIZE, SIZE];

        private readonly Rectangle[,] HighLights = new Rectangle[SIZE, SIZE];
        private readonly Dictionary<Position,Move> moveCache = new Dictionary<Position,Move>();

        private readonly List<string> fenHistory = new();
        // move history for undo/redo functionality
        private readonly List<string> moveHistory = new(); // Store actual moves like "e4", "Nf3", etc.

        private int replayIndex = -1;


        // the main gamestate variable with the board and move execusion
        private GameState gameState;



        private DispatcherTimer gameTimer;
        private TimeSpan whiteTime = TimeSpan.FromMinutes((int)Modes.Regular);
        private TimeSpan blackTime = TimeSpan.FromMinutes((int)Modes.Regular);
        private const int IncrementSeconds = 0x0;

        private int storedMinutes = (int)Modes.Regular;

        private Position selecetedPos = null;

        // AI settings
        private bool aiEnabled = false;
        private Player aiPlayer = Player.Black;
        private bool isAiThinking = false;

        // Console allocation for AI thinking logs
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();
        private bool consoleAllocated = false;
        private void EnsureConsole()
        {
            if (!consoleAllocated)
            {
                try { AllocConsole(); consoleAllocated = true; }
                catch { }
            }
        }

        
        public MainWindow()
        {
            InitializeComponent();
            InitilizeBoard();
            gameState = ParseFenToGameState(startFen);
            DrawBoard(gameState.Board);
            SetCursor(gameState.CurrentPlayer);
            
            var booksDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Books");
            ChessLogic.ChessAI.LoadOpeningSequencesFromFolder(booksDir);
            // Initialize and start the game timer
            gameTimer = new DispatcherTimer();
            gameTimer.Interval = TimeSpan.FromSeconds(1);
            gameTimer.Tick += OnTimerTick;
            gameTimer.Start();
            
            UpdateClocks();
            SoundManager.PlayeGameStartSound();
        }




        private void OnTimerTick(object sender, EventArgs e)
        {
            // Count down for the player whose turn it is (current player)
            if (gameState.CurrentPlayer == Player.White) // White's turn to move
            {
                whiteTime = whiteTime.Subtract(TimeSpan.FromSeconds(1));
                if (whiteTime.TotalSeconds <= 0)
                {
                    whiteTime = TimeSpan.Zero;
                    GameOverOnTime(Player.White);
                }
            }
            else // Black's turn to move
            {
                blackTime = blackTime.Subtract(TimeSpan.FromSeconds(1));
                if (blackTime.TotalSeconds <= 0)
                {
                    blackTime = TimeSpan.Zero;
                    GameOverOnTime(Player.Black);
                }
            }

            UpdateClocks();
        }

        private void UpdateClocks()
        {
            WhiteTimerTextBlock.Text = whiteTime.ToString(@"mm\:ss");
            BlackTimerTextBlock.Text = blackTime.ToString(@"mm\:ss");
        }



        private void GameOverOnTime(Player loser)
        {
            gameTimer.Stop();
            gameState.SetResult(Result.Win(EndGame.Timer, loser.Opponent()));
            
            ShowGameOver();
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
            Position pos = ToSquarePosition(point);

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
        private Position ToSquarePosition(Point point)
        {
            double squareSize = BoardGrid.ActualWidth / SIZE;
            int row = (int)(point.Y / squareSize);
            int col = (int)(point.X / squareSize);

            return new Position(row, col);
        }


        /*
         * function to choose the piece that we want to move and update the moves cache and highlight the possible moves for that piece
         * input: The position of the piece
         * ouput: None
        */
        private void OnFromPostionSelected(Position pos)
        {
            try
            {
                if (pos.row < 0 || pos.row >= SIZE || pos.column < 0 || pos.column >= SIZE)
                {
                    return;
                }

                IEnumerable<Move> moves = gameState.LegalMovesForPiece(pos);
                if (moves.Any())
                {
                    selecetedPos = pos;
                    CacheMoves(moves);
                    HighLightSquare();
                }
                if (gameState.Board.IsInCheck(gameState.CurrentPlayer))
                {
                    Position kingPos = gameState.Board.FindPiece(gameState.CurrentPlayer, PieceType.King);
                    HighLightCheck(kingPos);
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

                // Store the move in history before making it
                string moveNotation = GetMoveNotation(move);
                moveHistory.Add(moveNotation);

                gameState.MakeMove(move);
                DrawBoard(gameState.Board);
                SetCursor(gameState.CurrentPlayer);

                // Update replay index to current position (end of game)
                replayIndex = fenHistory.Count;

                // Update the move list display
                UpdateMoveList();

                if (isCapture || isEnPassant)
                {
                    SoundManager.PlayCaptureSound();
                }
                else if (isPromotion)
                {
                    SoundManager.PlayPromoteSound();
                }
                else
                {
                    SoundManager.PlayMoveSound();
                }

                // Add increment to the player who just moved (opposite of current player)
                if (gameState.CurrentPlayer == Player.Black) // White just moved
                {
                    whiteTime = whiteTime.Add(TimeSpan.FromSeconds(IncrementSeconds));
                }
                else // Black just moved
                {
                    blackTime = blackTime.Add(TimeSpan.FromSeconds(IncrementSeconds));
                }


                UpdateClocks();

                if (gameState.IsGameOver())
                {
                    ShowGameOver();
                    SoundManager.PlayGameEndSound();
                    gameTimer.Stop();
                }
                else
                {
                    // Trigger AI move if enabled and it's AI's turn
                    MaybeMakeAIMove();
                }

                if (gameState.Board.IsInCheck(gameState.CurrentPlayer))
                {
                    HighLightCheck(gameState.Board.FindPiece(gameState.CurrentPlayer,PieceType.King));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error executing move: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            fenHistory.Add(gameState.ToFen());
        }

        private async void MaybeMakeAIMove()
        {
            if (!aiEnabled || isAiThinking) return;
            if (gameState.IsGameOver()) return;
            if (gameState.CurrentPlayer != aiPlayer) return;

            try
            {
                isAiThinking = true;
                EnsureConsole();
                Move aiMove = await Task.Run(() => ChessAI.ChooseBestMove(
                    gameState,
                    3,
                    msg => Console.WriteLine(msg)
                ));
                if (aiMove != null && gameState.CurrentPlayer == aiPlayer && !gameState.IsGameOver())
                {
                    HandleMove(aiMove);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"AI move failed: {ex.Message}", "AI Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                isAiThinking = false;
            }
        }

        /// <summary>
        /// Converts a move to algebraic notation
        /// </summary>
        private string GetMoveNotation(Move move)
        {
            string fromSquare = PositionToString(move.from);
            string toSquare = PositionToString(move.to);
            
            switch (move.Type)
            {
                case MoveType.CastleKS:
                    return "O-O";
                case MoveType.CastleQS:
                    return "O-O-O";
                case MoveType.EnPassant:
                    return fromSquare.Substring(0, 1) + "x" + toSquare + " e.p.";
                case MoveType.PawnPromotion:
                    string promotionPiece = GetPromotionPieceSymbol(((PawnPromotion)move).PromotionType);
                    return toSquare + "=" + promotionPiece;
                default:
                    string pieceSymbol = GetPieceSymbol(gameState.Board[move.from]);
                    string captureSymbol = gameState.Board.IsCapturingMove(move) ? "x" : "";
                    return pieceSymbol + captureSymbol + toSquare;
            }
        }

        /// <summary>
        /// Converts a position to chess notation (e.g., "e4")
        /// </summary>
        private string PositionToString(Position pos)
        {
            char file = (char)('a' + pos.column);
            char rank = (char)('1' + (7 - pos.row));
            return file.ToString() + rank.ToString();
        }

        /// <summary>
        /// Gets the symbol for a piece
        /// </summary>
        private string GetPieceSymbol(Piece piece)
        {
            if (piece == null) return "";
            
            switch (piece.Type)
            {
                case PieceType.Pawn: return "";
                case PieceType.Rook: return "R";
                case PieceType.Knight: return "N";
                case PieceType.Bishop: return "B";
                case PieceType.Queen: return "Q";
                case PieceType.King: return "K";
                default: return "";
            }
        }

        /// <summary>
        /// Gets the symbol for a promotion piece
        /// </summary>
        private string GetPromotionPieceSymbol(PieceType type)
        {
            switch (type)
            {
                case PieceType.Queen: return "Q";
                case PieceType.Rook: return "R";
                case PieceType.Bishop: return "B";
                case PieceType.Knight: return "N";
                default: return "Q";
            }
        }

        /// <summary>
        /// Updates the move list display in the ListBox
        /// </summary>
        private void UpdateMoveList()
        {
            MoveListBox.Items.Clear();
            
            for (int i = 0; i < moveHistory.Count; i += 2)
            {
                string moveNumber = ((i / 2) + 1).ToString() + ".";
                string whiteMove = moveHistory[i];
                string blackMove = (i + 1 < moveHistory.Count) ? moveHistory[i + 1] : "";
                
                string moveText = $"{moveNumber} {whiteMove} {blackMove}".Trim();
                MoveListBox.Items.Add(moveText);
            }
            
            // Scroll to the bottom to show the latest move
            if (MoveListBox.Items.Count > 0)
            {
                MoveListBox.ScrollIntoView(MoveListBox.Items[MoveListBox.Items.Count - 1]);
            }
        }


        /*
         * function to finilize the move chosen and hide the highlights and make the move
         * input: the position of the move that ischosen to do
         * output: None
        */
        private void OnToPostionSelected(Position pos)
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
        private void HandlePromotion(Position from,Position to)
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


        public void HighLightCheck(Position kingPos)
        {
            Color CheckHighLight = Color.FromArgb(175, 94, 10, 5);
            HighLights[kingPos.row, kingPos.column].Fill = new SolidColorBrush(CheckHighLight);
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
            foreach (Position to in moveCache.Keys)
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
                    HighLights[r, c].Fill = new SolidColorBrush(Colors.Transparent);
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
        private Position ParsePosition(string pos)
        {
            if (pos == "-" || pos.Length != 2) return null;
            int col = pos[0] - 'a'; // 'a' = 0, 'b' = 1, etc.
            int row = 8 - (pos[1] - '0'); // '1' = 7, '2' = 6, etc.
            return new Position(row, col);
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
                    RestartGame(storedMinutes);
                }
                else
                {
                    Application.Current.Shutdown();
                }
            };
        }

        private void RestartGame(int startMinutes)
        {
            HideHighLights();
            moveCache.Clear();
            
            // Clear move history for new game
            ClearMoveHistory();
            
            // Reset timer
            whiteTime = TimeSpan.FromMinutes(startMinutes);
            blackTime = TimeSpan.FromMinutes(startMinutes);
            
            // Restart timer if it was stopped
            if (!gameTimer.IsEnabled)
            {
                gameTimer.Start();
            }
            
            gameState = ParseFenToGameState(startFen);
            DrawBoard(gameState.Board);
            SetCursor(gameState.CurrentPlayer);
            UpdateClocks();
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
            gameTimer.Stop();
            ControlMenu pauseMenu = new ControlMenu(storedMinutes);
            pauseMenu.FenTextBox.Text = gameState.ToFen();
            
            MenuContainer.Content = pauseMenu;

            

            pauseMenu.option += op =>
            {
                MenuContainer.Content = null;

                if (op == Option.Restart)
                {
                    RestartGame(storedMinutes);
                }
                else if(op == Option.Exit)
                {
                    Application.Current.Shutdown();
                }
                else if(op == Option.ChangeTime)
                {
                    if(pauseMenu.SelectedMode != null)
                    {
                        storedMinutes = (int)pauseMenu.SelectedMode;
                        RestartGame(storedMinutes);
                    }
                }
                else if(op == Option.Continue)
                {
                    // Resume
                    gameTimer.Start();
                }
                else if (op == Option.ChangeBackground)
                {
                    SafeChangeBackground(pauseMenu.BackGroundImage);
                    // Restart timer after background change
                    gameTimer.Start();
                }

                // Always restart timer if it was stopped and we're not exiting or restarting
                if (op != Option.Exit && op != Option.Restart && !gameTimer.IsEnabled)
                {
                    gameTimer.Start();
                }
            };
        }

        /*
         * function to safely change the background image
         * input: the path to the new background image
         * output: None
        */
        private void SafeChangeBackground(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath))
                    return;

                // Use Dispatcher.BeginInvoke to avoid blocking the UI thread
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        // Clean up the image path - remove any leading slash
                        string cleanImagePath = imagePath.TrimStart('/');
                        
                        // Get the application's base directory and construct the full path
                        string baseDir = System.AppDomain.CurrentDomain.BaseDirectory;
                        string fullPath = System.IO.Path.Combine(baseDir, cleanImagePath);
                        
                        // Create a new BitmapImage with the absolute path to the application directory
                        var newImageSource = new BitmapImage(new Uri(fullPath, UriKind.Absolute));
                        
                        // Apply the new background
                        BoardImage.ImageSource = newImageSource;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading background image: {ex.Message}", 
                            "Background Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error changing background: {ex.Message}", 
                    "Background Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            if (replayIndex > 0)
            {
                replayIndex--;
                LoadFenPosition(replayIndex);
                UpdateMoveListForReplay();
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (replayIndex < fenHistory.Count - 1)
            {
                replayIndex++;
                LoadFenPosition(replayIndex);
                UpdateMoveListForReplay();
            }
        }

        private void LoadFenPosition(int index)
        {
            string fen = fenHistory[index];
            gameState = ParseFenToGameState(fen);
            DrawBoard(gameState.Board);
            UpdateClocks();
        }

        /// <summary>
        /// Parses a FEN string and creates a GameState
        /// </summary>
        private GameState ParseFenToGameState(string fen)
        {
            string[] fenParts = fen.Split(' ');
            
            // Parse player
            Player startPlayer = fenParts[(int)FenParts.StartPlayer] == "w" ? Player.White : Player.Black;
            
            // Parse castling rights
            bool whiteKingside = fenParts[(int)FenParts.Castling].Contains('K');
            bool whiteQueenside = fenParts[(int)FenParts.Castling].Contains('Q');
            bool blackKingside = fenParts[(int)FenParts.Castling].Contains('k');
            bool blackQueenside = fenParts[(int)FenParts.Castling].Contains('q');
            
            // Parse en passant
            Position enPassant = ParsePosition(fenParts[(int)FenParts.EnPassant]);
            
            // Parse move counters
            int halfMoveClock = int.Parse(fenParts[(int)FenParts.HalfMoveClock]);
            int fullMoveNumber = int.Parse(fenParts[(int)FenParts.FullMoveNumber]);
            
            return new GameState(startPlayer, Board.Inital(fenParts[(int)FenParts.Placement]), 
                                whiteKingside, whiteQueenside, blackKingside, blackQueenside,
                                enPassant, halfMoveClock, fullMoveNumber);
        }


        /// <summary>
        /// Updates the move list display during replay mode
        /// </summary>
        private void UpdateMoveListForReplay()
        {
            MoveListBox.Items.Clear();
            
            for (int i = 0; i <= replayIndex && i < moveHistory.Count; i += 2)
            {
                string moveNumber = ((i / 2) + 1).ToString() + ".";
                string whiteMove = moveHistory[i];
                string blackMove = (i + 1 < moveHistory.Count && i + 1 <= replayIndex) ? moveHistory[i + 1] : "";
                
                string moveText = $"{moveNumber} {whiteMove} {blackMove}".Trim();
                MoveListBox.Items.Add(moveText);
            }
            
            // Highlight the current move
            if (MoveListBox.Items.Count > 0)
            {
                int currentMoveIndex = replayIndex / 2;
                if (currentMoveIndex < MoveListBox.Items.Count)
                {
                    MoveListBox.SelectedIndex = currentMoveIndex;
                    MoveListBox.ScrollIntoView(MoveListBox.Items[currentMoveIndex]);
                }
            }
        }

        /// <summary>
        /// Clears the move history when starting a new game
        /// </summary>
        private void ClearMoveHistory()
        {
            moveHistory.Clear();
            fenHistory.Clear();
            replayIndex = -1;
            MoveListBox.Items.Clear();
        }

        private void AIButton_Click(object sender, RoutedEventArgs e)
        {
            aiEnabled = !aiEnabled;
            AIbutton.Content = aiEnabled ? "AI: ON" : "AI: OFF";
            if (aiEnabled)
            {
                MaybeMakeAIMove();
            }
        }


        private void LoadFenPosition()
        {
            string fen = FenLoader.Text;
            gameState = ParseFenToGameState(fen);
            DrawBoard(gameState.Board);
            UpdateClocks();
        }
        private void LoadFen_Click(object sender, RoutedEventArgs e)
        {
            LoadFenPosition();
        }
    }
}