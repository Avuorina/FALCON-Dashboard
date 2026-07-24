using System.Windows;
using System.Windows.Controls;
using FALCONDashboard.ViewModels;

namespace FALCONDashboard
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MinecraftLogViewModel();
        }

        // ログが更新されるたびに一番下までスクロールする(直近の行を見失わないため)
        private void LogTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LogTextBox.CaretIndex = LogTextBox.Text.Length;
            LogTextBox.ScrollToEnd();
        }
    }
}