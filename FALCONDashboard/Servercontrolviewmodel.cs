using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using FALCONDashboard.Mvvm;
using FALCONDashboard.Services;

namespace FALCONDashboard.ViewModels
{
    public enum ServerState
    {
        Idle,      // 停止中。「サーバー起動」ボタンを押せる
        Starting,  // 起動処理中。疎通確認できるまでボタンは押せない
        Running,   // 起動中。「サーバー停止」ボタンを押せる
        Stopping,  // 停止処理中。疎通が切れるまでボタンは押せない
    }

    /// <summary>
    /// ヘッダーの「起動中」インジケーターと、起動/停止を兼ねる1個のボタンの裏側。
    /// 5秒おきに/historyを叩いて生死を確認し、状態(ServerState)を遷移させる。
    /// </summary>
    public class ServerControlViewModel : INotifyPropertyChanged
    {
        private readonly FalconApiClient _api = new();
        private readonly ServerLauncherService _launcher = new();
        private readonly DispatcherTimer _pollTimer;

        private ServerState _state = ServerState.Idle;
        private string _statusMessage = "";

        public ServerState State
        {
            get => _state;
            private set
            {
                _state = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsServerRunning));
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(ButtonText));
                OnPropertyChanged(nameof(CanToggle));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // ヘッダーの丸ランプの色バインド用(Running中だけ点灯)
        public bool IsServerRunning => State == ServerState.Running;

        // ヘッダーの丸ランプの下に出す文言
        public string StatusText => State switch
        {
            ServerState.Starting => "起動処理中...",
            ServerState.Running => "接続中",
            ServerState.Stopping => "停止処理中...",
            _ => "停止中",
        };

        // ボタンのラベル。起動中は停止を、それ以外は起動を促す
        public string ButtonText => State == ServerState.Running ? "サーバー停止" : "サーバー起動";

        // 処理中(Starting/Stopping)はボタンを押せなくする
        public bool CanToggle => State == ServerState.Idle || State == ServerState.Running;

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public ICommand ToggleServerCommand { get; }

        public ServerControlViewModel()
        {
            ToggleServerCommand = new RelayCommand(_ => ToggleServer(), _ => CanToggle);

            _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _pollTimer.Tick += async (s, e) => await CheckStatusAsync();
            _pollTimer.Start();

            _ = CheckStatusAsync();
        }

        private async System.Threading.Tasks.Task CheckStatusAsync()
        {
            bool running = await _api.IsServerRunningAsync();

            // Starting中に疎通が取れたらRunningへ、Stopping中に疎通が切れたらIdleへ遷移する。
            // Idle/Running中の定期チェックでも、外部要因(手動でCtrl+C等)で状態が変わっていれば追従する。
            switch (State)
            {
                case ServerState.Starting:
                    if (running) State = ServerState.Running;
                    break;
                case ServerState.Stopping:
                    if (!running) State = ServerState.Idle;
                    break;
                default:
                    State = running ? ServerState.Running : ServerState.Idle;
                    break;
            }
        }

        private void ToggleServer()
        {
            if (State == ServerState.Running)
            {
                StopServer();
            }
            else
            {
                StartServer();
            }
        }

        private void StartServer()
        {
            try
            {
                StatusMessage = "";
                _launcher.StartServer();
                State = ServerState.Starting;
            }
            catch (Exception ex)
            {
                StatusMessage = $"起動に失敗しました: {ex.Message}";
            }
        }

        private async void StopServer()
        {
            StatusMessage = "";
            State = ServerState.Stopping;
            await _api.ShutdownServerAsync();
            // 実際にIdleへ落とすのは次のポーリング(CheckStatusAsync)任せにする。
            // /shutdownはHTTPレスポンスを返してからプロセスが落ちるまでにラグがあるため。
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}