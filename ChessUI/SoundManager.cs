using System;
using System.Media;
using System.IO;
using System.Windows;

namespace ChessUI
{
    public static class SoundManager
    {
        private static readonly string SoundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Sounds");
        
        public static void PlayMoveSound()
        {
            PlaySound("move-self.wav");
        }

        public static void PlayCaptureSound()
        {
            PlaySound("capture.wav");
        }

        public static void PlayGameEndSound()
        {
            PlaySound("game-end.wav");
        }
        public static void PlayeGameStartSound()
        {
            PlaySound("game-start.wav");
        }

        public static void PlayPromoteSound()
        {
            PlaySound("promote.wav");
        }   

        private static void PlaySound(string soundFileName)
        {
            try
            {
                string fullPath = Path.Combine(SoundPath, soundFileName);
                if (!File.Exists(fullPath))
                {
                    MessageBox.Show($"Sound file not found: {fullPath}", "Sound Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var player = new SoundPlayer(fullPath))
                {
                    player.Play();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error playing sound: {ex.Message}", "Sound Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
