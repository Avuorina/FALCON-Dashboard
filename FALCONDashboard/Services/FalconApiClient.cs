using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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