using System.Windows;
using FALCONDashboard.ViewModels;

namespace FALCONDashboard
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new ServerControlViewModel();
        }
    }
}