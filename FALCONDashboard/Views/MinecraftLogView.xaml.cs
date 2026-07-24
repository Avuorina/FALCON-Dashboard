using System.Windows.Controls;
using FALCONDashboard.ViewModels;

namespace FALCONDashboard.Views
{
    public partial class MinecraftLogView : UserControl
    {
        public MinecraftLogView()
        {
            InitializeComponent();
            DataContext = new MinecraftLogViewModel();
        }

        private void LogTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LogTextBox.CaretIndex = LogTextBox.Text.Length;
            LogTextBox.ScrollToEnd();
        }
    }
}