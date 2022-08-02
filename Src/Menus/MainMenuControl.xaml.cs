using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;

namespace VSGames.Menus
{
    /// <summary>
    /// Interaction logic for MainMenuControl.
    /// </summary>
    public partial class MainMenuControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainMenuControl"/> class.
        /// </summary>
        public MainMenuControl()
        {
            this.InitializeComponent();
        }

        private void fps_click(object sender, RoutedEventArgs e)
        {
            MainMenuCommand.OpenWindow<Games.FpsGame>();
        }

        private void snake_click(object sender, RoutedEventArgs e)
        {
            MainMenuCommand.OpenWindow<Games.FpsGame>();
        }
    }
}