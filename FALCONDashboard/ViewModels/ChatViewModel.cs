using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using FALCONDashboard.Mvvm;
using FALCONDashboard.Services;

namespace FALCONDashboard.ViewModels
{
    public class ChatMessage
    {
        public string Sender { get; set; } = "";
        public string Text { get; set; } = "";
    }

    public class ChatViewModel : INotifyPropertyChanged
    {
        private readonly FalconApiClient _api = new();

        private ObservableCollection<ChatMessage> _messages = new();
        private string _inputText = "";
        private bool _isSending = false;
        private string _statusMessage = "";

        public ObservableCollection<ChatMessage> Messages
        {
            get => _messages;
            set { _messages = value; OnPropertyChanged(); }
        }

        public string InputText
        {
            get => _inputText;
            set { _inputText = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public ICommand SendCommand { get; }

        public ChatViewModel()
        {
            SendCommand = new RelayCommand(async _ => await SendAsync());
            _ = LoadHistoryAsync();
        }

        private async Task LoadHistoryAsync()
        {
            try
            {
                var (senders, texts) = await _api.GetHistoryAsync();
                Messages = new ObservableCollection<ChatMessage>();
                for (int i = 0; i < senders.Length; i++)
                {
                    Messages.Add(new ChatMessage { Sender = senders[i], Text = texts[i] });
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"履歴の取得に失敗しました: {ex.Message}";
            }
        }

        private async Task SendAsync()
        {
            var message = InputText.Trim();
            if (string.IsNullOrEmpty(message) || _isSending) return;

            Messages.Add(new ChatMessage { Sender = "隼", Text = message });
            InputText = "";
            _isSending = true;
            StatusMessage = "";

            try
            {
                var (reply, alarmUrl) = await _api.SendChatAsync(message);
                Messages.Add(new ChatMessage { Sender = "FALCON", Text = reply });

                // ダッシュボード(デスクトップ)ではアラームURLを開いても意味が無い(iPhone側の機能なので)
                // とりあえず参考情報として表示だけしておく
                if (alarmUrl != null)
                {
                    StatusMessage = $"(iPhone側で開くURLが生成されました: {alarmUrl})";
                }
            }
            catch (Exception ex)
            {
                Messages.Add(new ChatMessage { Sender = "システム", Text = $"エラー: {ex.Message}" });
            }
            finally
            {
                _isSending = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}