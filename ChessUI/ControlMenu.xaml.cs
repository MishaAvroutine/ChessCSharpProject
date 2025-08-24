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
        
        public Modes? SelectedMode { get; private set; }
        
        public ControlMenu()
        {
            InitializeComponent();
            // Set default selection to Regular
            ModeComboBox.SelectedIndex = 0;
            ModeComboBox.IsEnabled = true;
            ModeComboBox.IsEditable = false;
            ModeComboBox.IsDropDownOpen = false;
        }
        
        public ControlMenu(int currentMinutes)
        {
            InitializeComponent();
            // Set selection based on current minutes
            switch (currentMinutes)
            {
                case 30:
                    ModeComboBox.SelectedIndex = 0; // Regular
                    break;
                case 5:
                    ModeComboBox.SelectedIndex = 1; // Blitz
                    break;
                case 1:
                    ModeComboBox.SelectedIndex = 2; // Bullet
                    break;
                default:
                    ModeComboBox.SelectedIndex = 0; // Regular as fallback
                    break;
            }
            ModeComboBox.IsEnabled = true;
            ModeComboBox.IsEditable = false;
            ModeComboBox.IsDropDownOpen = false;
        }

        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            option?.Invoke(Option.Continue);
        }
        
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            option?.Invoke(Option.Exit);
        }
        
        private void Restart_Click(object sender, RoutedEventArgs e)
        {
            option?.Invoke(Option.Restart);
        }

        private void ModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ModeComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is string tagValue)
            {
                if (int.TryParse(tagValue, out int minutes))
                {
                    SelectedMode = (Modes)minutes;
                    option?.Invoke(Option.ChangeTime);
                }
            }
        }
    }
}
