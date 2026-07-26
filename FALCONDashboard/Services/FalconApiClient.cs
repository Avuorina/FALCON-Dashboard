using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FALCONDashboard.Services
{
    public class FalconApiClient
    {
        // ★接続先はここ1箇所だけ★ 将来PCを変えたり、ドメインが変わった時もここだけ直せばいい
        private const string BaseUrl = "https://desktop-0cl2p6m.taila09f6b.ts.net:8000";

        private readonly HttpClient _client;

        public FalconApiClient()
        {
            _client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        }

        /// <summary>
        /// サーバーが応答するかどうかだけを確認する。
        /// server.py側に専用の/healthが無い場合は、既存の/historyを軽く叩いて代用する。
        /// 起動直後はSSLハンドシェイクごと失敗するので、短いタイムアウトで素早く諦める。
        /// </summary>
        public async Task<bool> IsServerRunningAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                var response = await _client.GetAsync("/history", cts.Token);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// サーバーにgraceful shutdownを依頼する。
        /// server.py側は/shutdownを受けてからSIGINTを自分に送るので、
        /// このリクエスト自体は成功応答が返ってくる(その後で本当に落ちる)。
        /// 落ちるタイミングと重なって接続が切れることがあるので、例外は無視してよい。
        /// </summary>
        public async Task ShutdownServerAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                await _client.PostAsync("/shutdown", new StringContent(""), cts.Token);
            }
            catch
            {
                // 停止処理そのものは飛んでいる可能性が高いので握りつぶす。
                // 本当に届いていなければ、この後のポーリングでIsServerRunningがtrueのままになるので気づける。
            }
        }

        public async Task<string[]> GetMinecraftServersAsync()
        {
            var json = await _client.GetStringAsync("/minecraft/servers");
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("servers")
                .EnumerateArray()
                .Select(e => e.GetString() ?? "")
                .ToArray();
        }

        public async Task<string[]> GetMinecraftLogAsync(string server, int lines)
        {
            var json = await _client.GetStringAsync($"/minecraft/{Uri.EscapeDataString(server)}/log?lines={lines}");
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("error", out var errorProp))
            {
                throw new InvalidOperationException(errorProp.GetString());
            }

            return doc.RootElement.GetProperty("lines")
                .EnumerateArray()
                .Select(e => e.GetString() ?? "")
                .ToArray();
        }

        public async Task<(string[] Senders, string[] Texts)> GetHistoryAsync()
        {
            var json = await _client.GetStringAsync("/history");
            using var doc = JsonDocument.Parse(json);
            var entries = doc.RootElement.GetProperty("history").EnumerateArray().ToArray();

            var senders = entries.Select(e => e.GetProperty("sender").GetString() ?? "").ToArray();
            var texts = entries.Select(e => e.GetProperty("text").GetString() ?? "").ToArray();
            return (senders, texts);
        }

        public async Task<(string Reply, string? AlarmUrl)> SendChatAsync(string message)
        {
            var payload = JsonSerializer.Serialize(new { message });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/chat", content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            string reply = doc.RootElement.GetProperty("reply").GetString() ?? "";
            string? alarmUrl = doc.RootElement.TryGetProperty("alarm_url", out var a) && a.ValueKind != JsonValueKind.Null
                ? a.GetString()
                : null;

            return (reply, alarmUrl);
        }
    }
}