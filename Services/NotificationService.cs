using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using WeatherApp.Models;

namespace WeatherApp.Services;

public class NotificationService
{
    public const string AppId = "WeatherApp.WinUI.Station";
    private static DateTime _lastRainAlertTime = DateTime.MinValue;
    private static DateTime _lastUvAlertDate = DateTime.MinValue;
    private static DateTime _lastMorningBriefingDate = DateTime.MinValue;
    private static DateTime _lastFloodAlertTime = DateTime.MinValue;
    private static bool _isAumidRegistered;

    public static void EnsureAumidRegistered()
    {
        if (_isAumidRegistered) return;
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey($@"Software\Classes\AppUserModelId\{AppId}");
            if (key != null)
            {
                key.SetValue("DisplayName", "Thời Tiết WinUI");
                string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
                if (File.Exists(iconPath))
                {
                    key.SetValue("IconUri", iconPath);
                }
                key.SetValue("ShowInSettings", 1, Microsoft.Win32.RegistryValueKind.DWord);
            }
            _isAumidRegistered = true;
        }
        catch { }
    }

    public void ShowToast(string title, string message)
    {
        EnsureAumidRegistered();
        bool sent = false;

        try
        {
            string xml = $@"
<toast duration='short'>
    <visual>
        <binding template='ToastGeneric'>
            <text>{System.Security.SecurityElement.Escape(title)}</text>
            <text>{System.Security.SecurityElement.Escape(message)}</text>
            <text placement='attribution'>Thời Tiết WinUI</text>
        </binding>
    </visual>
    <audio src='ms-winsoundevent:Notification.Default' />
</toast>";

            var doc = new XmlDocument();
            doc.LoadXml(xml);

            var toast = new ToastNotification(doc)
            {
                ExpirationTime = DateTimeOffset.Now.AddMinutes(15)
            };

            var notifier = ToastNotificationManager.CreateToastNotifier(AppId);
            notifier.Show(toast);
            sent = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Toast] Standard toast error: {ex.Message}");
        }

        if (!sent)
        {
            _ = Task.Run(() =>
            {
                try
                {
                    // VÁ BẢO MẬT: Mã hóa Base64 Unicode cho Title và Message để triệt tiêu 100% nguy cơ Command Injection qua PowerShell
                    byte[] titleBytes = System.Text.Encoding.Unicode.GetBytes(title ?? "");
                    byte[] msgBytes = System.Text.Encoding.Unicode.GetBytes(message ?? "");
                    string b64Title = Convert.ToBase64String(titleBytes);
                    string b64Msg = Convert.ToBase64String(msgBytes);

                    string psScript = 
                        "$t = [System.Text.Encoding]::Unicode.GetString([System.Convert]::FromBase64String('" + b64Title + "')); " +
                        "$m = [System.Text.Encoding]::Unicode.GetString([System.Convert]::FromBase64String('" + b64Msg + "')); " +
                        "[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null; " +
                        "$template = [Windows.UI.Notifications.ToastNotificationManager]::GetTemplateContent([Windows.UI.Notifications.ToastTemplateType]::ToastText02); " +
                        "$nodes = $template.GetElementsByTagName('text'); " +
                        "$nodes.Item(0).AppendChild($template.CreateTextNode($t)) | Out-Null; " +
                        "$nodes.Item(1).AppendChild($template.CreateTextNode($m)) | Out-Null; " +
                        "$toast = [Windows.UI.Notifications.ToastNotification]::new($template); " +
                        $"[Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('{AppId}').Show($toast);";

                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psScript}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    Process.Start(psi);
                }
                catch { }
            });
        }
    }

    public void ShowEventReminderToast(string eventTitle, string timeText, string category, string weatherInfo, string advice)
    {
        string title = $"⏰ Nhắc Nhở Lịch Trình: {eventTitle}";
        string msg = $"{timeText} hôm nay • Phân loại: {category}";
        if (!string.IsNullOrWhiteSpace(weatherInfo))
        {
            msg += $"\nThời tiết: {weatherInfo}";
        }
        if (!string.IsNullOrWhiteSpace(advice))
        {
            msg += $"\n{advice}";
        }
        ShowToast(title, msg);
    }

    public void ShowCommuteToast(string commuteType, string timeText, string condition, string temp, string advice)
    {
        string icon = commuteType.Contains("sáng", StringComparison.OrdinalIgnoreCase) || commuteType.Contains("đi", StringComparison.OrdinalIgnoreCase) ? "🚗" : "🏠";
        string title = $"{icon} Chuẩn Bị {commuteType} ({timeText})";
        string msg = $"Dự báo {condition}, nhiệt độ {temp}. {advice}";
        ShowToast(title, msg);
    }

    public void TriggerFloodWarningToast(UrbanFloodWarning floodWarning, string locationName)
    {
        if (floodWarning == null || floodWarning.RiskLevel < 2) return;
        DateTime now = DateTime.Now;
        if ((now - _lastFloodAlertTime).TotalHours < 3) return;

        _lastFloodAlertTime = now;
        string icon = floodWarning.RiskLevel == 3 ? "🚨" : "🌊";
        string title = $"{icon} {floodWarning.Headline}";
        string msg = $"{floodWarning.Summary}\n{floodWarning.MotorbikeAdvice}";
        ShowToast(title, msg);
    }

    public void CheckAndTriggerAlerts(CurrentWeatherDisplay current, AppSettings settings, DailyWeatherDto? daily, HourlyWeatherDto? hourly)
    {
        if (!settings.EnableToastNotifications || current == null) return;

        DateTime now = DateTime.Now;
        string userName = string.IsNullOrWhiteSpace(settings.UserName) ? "bạn" : settings.UserName;

        // 1. CẢNH BÁO MƯA SẮP ĐẾN (RAIN ALARM)
        if (settings.EnableRainAlarm && (now - _lastRainAlertTime).TotalHours >= 2)
        {
            bool isRainUpcoming = false;
            if (hourly?.PrecipitationProbability != null && hourly.PrecipitationProbability.Count > 1)
            {
                // Kiểm tra 2 giờ kế tiếp
                int probNext1 = hourly.PrecipitationProbability.Count > 1 ? hourly.PrecipitationProbability[1] : 0;
                int probNext2 = hourly.PrecipitationProbability.Count > 2 ? hourly.PrecipitationProbability[2] : 0;

                if (probNext1 >= 60 || probNext2 >= 70 || current.WeatherEffect == WeatherEffectType.Thunderstorm)
                {
                    isRainUpcoming = true;
                }
            }
            else if (current.WeatherEffect is WeatherEffectType.ModerateRain or WeatherEffectType.HeavyRain or WeatherEffectType.Thunderstorm)
            {
                isRainUpcoming = true;
            }

            if (isRainUpcoming)
            {
                _lastRainAlertTime = now;
                ShowToast("🌧️ Cảnh Báo Mưa Sắp Tới!",
                    $"Dự báo sẽ có mưa to tại khu vực {current.LocationName} trong 20-30 phút tới. Nhớ đóng cửa sổ và chuẩn bị áo mưa nhé {userName}!");
            }
        }

        // 2. CẢNH BÁO NẮNG GẮT & TIA CỰC TÍM (UV ALERT: 11h - 14h, UV >= 8)
        if (settings.EnableUvAlert && now.Hour >= 11 && now.Hour <= 14 && _lastUvAlertDate.Date != now.Date)
        {
            if (double.TryParse(current.UvIndexText, out double uv) && uv >= 8.0)
            {
                _lastUvAlertDate = now.Date;
                ShowToast("🔥 Cảnh Báo Nắng Gắt & Tia UV Rất Cao!",
                    $"Chỉ số UV tại {current.LocationName} đang ở mức rất cao ({uv:F1}). {userName} nên bôi kem chống nắng và hạn chế tiếp xúc trực tiếp ngoài trời!");
            }
            else if (current.WeatherEffect == WeatherEffectType.HighUvSunny)
            {
                _lastUvAlertDate = now.Date;
                ShowToast("🔥 Cảnh Báo Tia UV Rất Cao!",
                    $"Thời điểm buổi trưa tại {current.LocationName} có nắng gắt. {userName} hãy uống nhiều nước và che chắn cẩn thận khi ra đường!");
            }
        }

        // 3. TÓM TẮT THỜI TIẾT ĐẦU NGÀY (MORNING BRIEFING: 06h - 09h)
        if (settings.EnableMorningBriefing && now.Hour >= 6 && now.Hour <= 9 && _lastMorningBriefingDate.Date != now.Date)
        {
            _lastMorningBriefingDate = now.Date;
            string tempRange = current.MinMaxText;
            ShowToast($"☀️ Chào buổi sáng, {userName}!",
                $"Hôm nay tại {current.LocationName} {current.ConditionText.ToLowerInvariant()}, {tempRange.ToLowerInvariant()}. Chúc bạn một ngày mới tràn đầy năng lượng!");
        }
    }
}
