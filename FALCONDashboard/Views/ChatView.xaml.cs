using System.Windows.Controls;
using FALCONDashboard.ViewModels;

namespace FALCONDashboard.Views
{
    public partial class ChatView : UserControl
    {
        public ChatView()
        {
            InitializeComponent();
            var vm = new ChatViewModel();
            DataContext = vm;

            // メッセージが増えるたびに一番下までスクロールする
            vm.Messages.CollectionChanged += (s, e) =>
            {
                Dispatcher.BeginInvoke(() => ChatScroll.ScrollToEnd());
            };
        }
    }
}