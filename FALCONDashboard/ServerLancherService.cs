using System;
using System.Diagnostics;
using System.IO;

namespace FALCONDashboard.Services
{
    /// <summary>
    /// FALCON本体(server.py)をローカルで起動するためのサービス。
    /// Dashboardとserver.pyが同じPC(Tailscaleホスト: desktop-0cl2p6m)で動いている前提。
    /// </summary>
    public class ServerLauncherService
    {
        // ★環境依存の設定はここ2つだけ★ 隼の環境に合わせて書き換えてくれ

        // FALCON本体(server.pyがあるフォルダ)の絶対パス
        private const string FalconRepoPath = @"E:\ゲームじゃない\FALCON\FALCON";

        // 証明書ファイル名 (FalconRepoPath直下にある前提。隼が手打ちしてたコマンドと同じもの)
        private const string CertFile = "desktop-0cl2p6m.taila09f6b.ts.net.crt";
        private const string KeyFile = "desktop-0cl2p6m.taila09f6b.ts.net.key";

        // .venv内のuvicorn.exeをフルパスで直接呼ぶ。
        // Activate.ps1経由だとPowerShellの実行ポリシーでブロックされることがあるので、
        // 「PATHが通ってるかどうか」を気にしなくていいこの方式にしてある。
        private static string UvicornExePath =>
            Path.Combine(FalconRepoPath, ".venv", "Scripts", "uvicorn.exe");

        private Process? _serverProcess;

        public bool IsProcessAlive => _serverProcess != null && !_serverProcess.HasExited;

        /// <summary>
        /// PowerShellウィンドウを開いてuvicornを起動する。
        /// ウィンドウを残すのは、ログを目視できるようにするため(隼が手打ちしてた時と同じ見え方)。
        /// </summary>
        public void StartServer()
        {
            if (IsProcessAlive)
            {
                throw new InvalidOperationException("既にこのDashboardから起動したサーバープロセスが動いています。");
            }

            if (!File.Exists(UvicornExePath))
            {
                throw new FileNotFoundException(
                    $".venv内にuvicorn.exeが見つかりません: {UvicornExePath}\n" +
                    "venvのフォルダ名がScriptsではなくbinだったり、そもそもuvicornがインストールされていない可能性があります。");
            }

            var uvicornCmd = $"& '{UvicornExePath}' server:app --host 0.0.0.0 --port 8000 " +
                              $"--ssl-certfile={CertFile} --ssl-keyfile={KeyFile}";

            var fullCommand = $"cd '{FalconRepoPath}'; {uvicornCmd}";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                // -NoExit: サーバーが落ちてもログが読めるようウィンドウを閉じない
                Arguments = $"-NoExit -Command \"{fullCommand}\"",
                UseShellExecute = true,
                WorkingDirectory = FalconRepoPath,
            };

            _serverProcess = Process.Start(psi);
        }

        /// <summary>
        /// Dashboardから起動したプロセスのウィンドウを閉じる。
        /// 手動でターミナルから起動した分は追跡していないので閉じられない。
        /// 通常はHTTP経由の/shutdown(FalconApiClient.ShutdownServerAsync)を使うので、
        /// これは/shutdownが届かない時の保険として残してある。
        /// </summary>
        public void StopServer()
        {
            if (IsProcessAlive)
            {
                _serverProcess!.CloseMainWindow();
            }
        }
    }
}