using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WeatherApp.Models;

namespace WeatherApp.Services;

public class SettingsService
{
    private readonly string _settingsFilePath;
    private readonly string _settingsBackupPath;
    private readonly string _portableSettingsPath;
    private static readonly object _lock = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public SettingsService()
    {
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WeatherAppWinUI");
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
        _settingsFilePath = Path.Combine(folder, "settings.json");
        _settingsBackupPath = Path.Combine(folder, "settings.json.bak");
        _portableSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
    }

    public AppSettings LoadSettings()
    {
        lock (_lock)
        {
            // 1. Nạp từ file chính trong LocalAppData
            if (File.Exists(_settingsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    if (!string.IsNullOrWhiteSpace(json) && json.Length > 10)
                    {
                        var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                        if (settings != null)
                        {
                            settings.LaunchAtStartup = StartupService.IsStartupEnabled();
                            return settings;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SettingsService.LoadSettings] Main file error: {ex.Message}");
                }
            }

            // 2. Thử nạp từ file backup (.bak) nếu file chính bị lỗi hoặc trống
            if (File.Exists(_settingsBackupPath))
            {
                try
                {
                    string json = File.ReadAllText(_settingsBackupPath);
                    if (!string.IsNullOrWhiteSpace(json) && json.Length > 10)
                    {
                        var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                        if (settings != null)
                        {
                            settings.LaunchAtStartup = StartupService.IsStartupEnabled();
                            try { File.Copy(_settingsBackupPath, _settingsFilePath, overwrite: true); } catch { }
                            return settings;
                        }
                    }
                }
                catch { }
            }

            // 3. Thử nạp từ file cùng thư mục chạy ứng dụng (Portable mode hoặc di chuyển máy)
            if (File.Exists(_portableSettingsPath))
            {
                try
                {
                    string json = File.ReadAllText(_portableSettingsPath);
                    if (!string.IsNullOrWhiteSpace(json) && json.Length > 10)
                    {
                        var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                        if (settings != null)
                        {
                            settings.LaunchAtStartup = StartupService.IsStartupEnabled();
                            try { File.Copy(_portableSettingsPath, _settingsFilePath, overwrite: true); } catch { }
                            return settings;
                        }
                    }
                }
                catch { }
            }

            var defaultSettings = new AppSettings();
            defaultSettings.LaunchAtStartup = StartupService.IsStartupEnabled();
            return defaultSettings;
        }
    }

    public void SaveSettings(AppSettings settings)
    {
        lock (_lock)
        {
            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    string json = JsonSerializer.Serialize(settings, JsonOptions);
                    string tmpPath = _settingsFilePath + ".tmp";
                    File.WriteAllText(tmpPath, json);

                    // Sao lưu file hiện tại sang .bak trước khi ghi đè
                    if (File.Exists(_settingsFilePath))
                    {
                        try { File.Copy(_settingsFilePath, _settingsBackupPath, overwrite: true); } catch { }
                    }

                    File.Move(tmpPath, _settingsFilePath, overwrite: true);
                    return;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SettingsService.SaveSettings] Attempt {attempt} error: {ex.Message}");
                    if (attempt == 2)
                    {
                        try
                        {
                            string json = JsonSerializer.Serialize(settings, JsonOptions);
                            File.WriteAllText(_settingsFilePath, json);
                        }
                        catch { }
                    }
                    Thread.Sleep(50);
                }
            }
        }
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                string json = JsonSerializer.Serialize(settings, JsonOptions);
                string tmpPath = _settingsFilePath + ".tmp";
                await File.WriteAllTextAsync(tmpPath, json);

                if (File.Exists(_settingsFilePath))
                {
                    try { File.Copy(_settingsFilePath, _settingsBackupPath, overwrite: true); } catch { }
                }

                File.Move(tmpPath, _settingsFilePath, overwrite: true);
                return;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsService.SaveSettingsAsync] Attempt {attempt} error: {ex.Message}");
                if (attempt == 2)
                {
                    try
                    {
                        string json = JsonSerializer.Serialize(settings, JsonOptions);
                        await File.WriteAllTextAsync(_settingsFilePath, json);
                    }
                    catch { }
                }
                await Task.Delay(50);
            }
        }
    }
}
