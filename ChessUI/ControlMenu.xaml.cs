using System;
using System.Windows;
using System.Windows.Controls;

namespace ChessUI
{
    /// <summary>
    /// Interaction logic for ControlMenu.xaml
    /// </summary>
    public partial class ControlMenu : UserControl
    {
        public event Action<Option> option;
        public ControlMenu()
        {
            InitializeComponent();
        }

        private void Restart_Click(object sender, RoutedEventArgs e)
        {
            option?.Invoke(Option.Restart);
        }

        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            option?.Invoke(Option.Continue);
        }
    }
}
