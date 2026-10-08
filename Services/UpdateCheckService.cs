using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace WeatherApp.Services;

public class UpdateInfo
{
    public bool HasUpdate { get; set; }
    public string CurrentVersion { get; set; } = "3.0.2";
    public string LatestVersion { get; set; } = string.Empty;
    public string ReleaseTitle { get; set; } = string.Empty;
    public string Changelog { get; set; } = string.Empty;
    public string ReleaseUrl { get; set; } = "https://github.com/hellangel45236/Weather-WinUI/releases";
    public string DownloadUrl { get; set; } = string.Empty;
    public DateTime? PublishedAt { get; set; }
    public bool IsCheckingSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}

public class UpdateCheckService
{
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(12)
    };

    public const string CurrentAppVersion = "3.0.2";
    private const string GitHubApiUrl = "https://api.github.com/repos/hellangel45236/Weather-WinUI/releases/latest";

    public async Task<UpdateInfo> CheckForUpdatesAsync()
    {
        var result = new UpdateInfo
        {
            CurrentVersion = CurrentAppVersion
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, GitHubApiUrl);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("WeatherApp-WinUI", CurrentAppVersion));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                result.IsCheckingSuccess = false;
                result.ErrorMessage = $"Mã lỗi máy chủ GitHub: {(int)response.StatusCode}";
                return result;
            }

            string json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string tagName = root.TryGetProperty("tag_name", out var tagEl) ? tagEl.GetString() ?? "" : "";
            string cleanTag = tagName.TrimStart('v', 'V').Trim();

            result.LatestVersion = string.IsNullOrEmpty(cleanTag) ? tagName : cleanTag;
            result.ReleaseTitle = root.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "" : "";
            result.Changelog = root.TryGetProperty("body", out var bodyEl) ? bodyEl.GetString() ?? "" : "";
            result.ReleaseUrl = root.TryGetProperty("html_url", out var urlEl) ? urlEl.GetString() ?? "" : result.ReleaseUrl;

            if (root.TryGetProperty("published_at", out var pubEl) && pubEl.TryGetDateTime(out var dt))
            {
                result.PublishedAt = dt;
            }

            // Tìm asset bộ cài Setup .exe hoặc Portable .zip
            if (root.TryGetProperty("assets", out var assetsEl) && assetsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assetsEl.EnumerateArray())
                {
                    string assetName = asset.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    if (assetName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        result.DownloadUrl = asset.TryGetProperty("browser_download_url", out var dl) ? dl.GetString() ?? "" : "";
                        break;
                    }
                    if (string.IsNullOrEmpty(result.DownloadUrl) && assetName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        result.DownloadUrl = asset.TryGetProperty("browser_download_url", out var dl) ? dl.GetString() ?? "" : "";
                    }
                }
            }

            if (string.IsNullOrEmpty(result.DownloadUrl))
            {
                result.DownloadUrl = result.ReleaseUrl;
            }

            // So sánh phiên bản
            if (Version.TryParse(result.LatestVersion, out var latestVer) && Version.TryParse(CurrentAppVersion, out var currVer))
            {
                result.HasUpdate = latestVer > currVer;
            }
            else
            {
                result.HasUpdate = !string.IsNullOrEmpty(result.LatestVersion) &&
                                  !string.Equals(result.LatestVersion, CurrentAppVersion, StringComparison.OrdinalIgnoreCase);
            }

            result.IsCheckingSuccess = true;
        }
        catch (Exception ex)
        {
            result.IsCheckingSuccess = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }
}
