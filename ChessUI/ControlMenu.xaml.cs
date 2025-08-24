using System;
using System.Collections.Generic;
using System.Linq;
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
        public string BackGroundImage { get; private set; } = "Assets/Board.png";

        public ControlMenu()
        {
            InitializeComponent();
            // Set default selection to Regular
            ModeComboBox.SelectedIndex = 0;
            ModeComboBox.IsEnabled = true;
            ModeComboBox.IsEditable = false;
            ModeComboBox.IsDropDownOpen = false;
            
            // Initialize Apply button as disabled
            ApplyBackgroundButton.IsEnabled = false;
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
            
            // Initialize Apply button as disabled
            ApplyBackgroundButton.IsEnabled = false;
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

        private void ApplyBackground_Click(object sender, RoutedEventArgs e)
        {
            option?.Invoke(Option.ChangeBackground);
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

        private void BoardBackGround_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BoardBackGround.SelectedItem is ComboBoxItem selectedItem)
            {
                switch (selectedItem.Content.ToString())
                {
                    case "Default":
                        BackGroundImage = "Assets/Board.png";
                        break;

                    case "White and Gray":
                        BackGroundImage = "Assets/Board1.png";
                        break;
                    case "Black and White":
                        BackGroundImage = "Assets/Board2.png";
                        break;
                    default:
                        BackGroundImage = "Assets/Board.png";
                        break;
                }

                // Enable the apply button when a selection is made
                ApplyBackgroundButton.IsEnabled = true;
                
                // Don't automatically invoke the option - let the user choose when to apply
                // option?.Invoke(Option.ChangeBackground);
            }
        }

    }
}
