using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using FALCONDashboard.Mvvm;
using FALCONDashboard.Services;

namespace FALCONDashboard.ViewModels
{
    public class MinecraftLogViewModel : INotifyPropertyChanged
    {
        private readonly FalconApiClient _api = new();
        private readonly DispatcherTimer _pollTimer;

        private ObservableCollection<string> _serverNames = new();
        private string? _selectedServer;
        private string _logText = "";
        private string _statusMessage = "";

        public ObservableCollection<string> ServerNames
        {
            get => _serverNames;
            set { _serverNames = value; OnPropertyChanged(); }
        }

        public string? SelectedServer
        {
            get => _selectedServer;
            set
            {
                if (_selectedServer == value) return;
                _selectedServer = value;
                OnPropertyChanged();
                _ = PollLogAsync(); // サーバーを切り替えたら即座に1回取得し直す
            }
        }

        public string LogText
        {
            get => _logText;
            set { _logText = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public ICommand RefreshServersCommand { get; }

        public MinecraftLogViewModel()
        {
            RefreshServersCommand = new RelayCommand(async _ => await LoadServersAsync());

            // ★ポーリング★ 3秒おきにログを取得し直す。リアルタイム配信(WebSocket)はやらない設計
            _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _pollTimer.Tick += async (s, e) => await PollLogAsync();

            _ = LoadServersAsync();
        }

        private async Task LoadServersAsync()
        {
            try
            {
                var names = await _api.GetMinecraftServersAsync();
                ServerNames = new ObservableCollection<string>(names);

                if (SelectedServer == null && ServerNames.Count > 0)
                {
                    SelectedServer = ServerNames[0]; // setterの中でPollLogAsyncが自動的に走る
                }

                StatusMessage = "";
                _pollTimer.Start();
            }
            catch (Exception ex)
            {
                StatusMessage = $"サーバー一覧の取得に失敗しました: {ex.Message}";
            }
        }

        private async Task PollLogAsync()
        {
            if (string.IsNullOrEmpty(SelectedServer)) return;

            try
            {
                var lines = await _api.GetMinecraftLogAsync(SelectedServer, 200);
                LogText = string.Join(Environment.NewLine, lines);
                StatusMessage = "";
            }
            catch (Exception ex)
            {
                StatusMessage = $"ログ取得に失敗しました: {ex.Message}";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}