using System;
using System.Linq;
using System.Net.Http;
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

            // FALCON側が {"error": "..."} を返した場合はここで例外に変換する
            if (doc.RootElement.TryGetProperty("error", out var errorProp))
            {
                throw new InvalidOperationException(errorProp.GetString());
            }

            return doc.RootElement.GetProperty("lines")
                .EnumerateArray()
                .Select(e => e.GetString() ?? "")
                .ToArray();
        }
    }
}