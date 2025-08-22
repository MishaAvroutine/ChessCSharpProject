using ChessLogic;
using System.Windows;
using System.Windows.Controls;


namespace ChessUI
{
    /// <summary>
    /// Interaction logic for GameOverMenu.xaml
    /// </summary>
    public partial class GameOverMenu : UserControl
    {
        public event Action<Option> OptionSelected;
        public GameOverMenu(GameState gameState)
        {
            InitializeComponent();

            Result result = gameState.Result;
            WinnerText.Text = GetWinnerText(result.Winner);
            ReasonText.Text = GetReasonText(result.Reason, gameState.CurrentPlayer);
        }

        private static string GetWinnerText(Player player)
        {
            return player switch
            {
                Player.White => "WHITE WINS!",
                Player.Black => "BLACK WINS!",
                _ => "IT'S A DRAW!"
            };
        }

        private static string PlayerString(Player player)
        {
            return player switch
            {
                Player.White => "WHITE",
                Player.Black => "BLACK",
                _ => ""
            };
        }

        public static string GetReasonText(EndGame reason,Player currentPlayer)
        {
            return reason switch
            {
                EndGame.Stalemate => $"STALEMATE - {PlayerString(currentPlayer)} CAN'T MOVE",
                EndGame.CheckMate => $"CHECKMATE - {PlayerString(currentPlayer)} CAN'T MOVE",
                EndGame.FiftyMoveRule => "FIFTY-RULE-MOVE",
                EndGame.InsufficientMaterial => "INSUFFICIENT MATERIAL",
                EndGame.ThreefoldRepetition => "THREEFOLD REPETITION",
                _ => ""
            };
        }


        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            OptionSelected?.Invoke(Option.Exit);
        }

        private void Restart_Click(object sender, RoutedEventArgs e)
        {
            OptionSelected?.Invoke(Option.Restart);
        }
    }
}
