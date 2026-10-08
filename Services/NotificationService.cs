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
            string appAttribution = LocalizationService.Instance.IsVietnamese ? "Thời Tiết WinUI" : "WinUI Weather";
            string xml = $@"
<toast duration='short'>
    <visual>
        <binding template='ToastGeneric'>
            <text>{System.Security.SecurityElement.Escape(title)}</text>
            <text>{System.Security.SecurityElement.Escape(message)}</text>
            <text placement='attribution'>{appAttribution}</text>
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
        bool isVi = LocalizationService.Instance.IsVietnamese;
        string title = isVi ? $"⏰ Nhắc Nhở Lịch Trình: {eventTitle}" : $"⏰ Schedule Reminder: {eventTitle}";
        string msg = isVi ? $"{timeText} hôm nay • Phân loại: {category}" : $"{timeText} today • Category: {category}";
        if (!string.IsNullOrWhiteSpace(weatherInfo))
        {
            msg += isVi ? $"\nThời tiết: {weatherInfo}" : $"\nWeather: {weatherInfo}";
        }
        if (!string.IsNullOrWhiteSpace(advice))
        {
            msg += $"\n{advice}";
        }
        ShowToast(title, msg);
    }

    public void ShowCommuteToast(string commuteType, string timeText, string condition, string temp, string advice)
    {
        bool isVi = LocalizationService.Instance.IsVietnamese;
        string icon = commuteType.Contains("sáng", StringComparison.OrdinalIgnoreCase) || commuteType.Contains("đi", StringComparison.OrdinalIgnoreCase) || commuteType.Contains("morning", StringComparison.OrdinalIgnoreCase) || commuteType.Contains("work", StringComparison.OrdinalIgnoreCase) ? "🚗" : "🏠";
        string title = isVi ? $"{icon} Chuẩn Bị {commuteType} ({timeText})" : $"{icon} Commute Prep: {commuteType} ({timeText})";
        string msg = isVi ? $"Dự báo {condition}, nhiệt độ {temp}. {advice}" : $"Forecast: {condition}, temp {temp}. {advice}";
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
        bool isVi = LocalizationService.Instance.IsVietnamese;
        string defaultName = isVi ? "bạn" : "there";
        string userName = string.IsNullOrWhiteSpace(settings.UserName) ? defaultName : settings.UserName;

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
                string rainTitle = isVi ? "🌧️ Cảnh Báo Mưa Sắp Tới!" : "🌧️ Rain Alarm: Imminent Rain!";
                string rainMsg = isVi 
                    ? $"Dự báo sẽ có mưa to tại khu vực {current.LocationName} trong 20-30 phút tới. Nhớ đóng cửa sổ và chuẩn bị áo mưa nhé {userName}!"
                    : $"Heavy rain is forecast in {current.LocationName} within the next 20-30 minutes. Close windows and take rain gear, {userName}!";
                ShowToast(rainTitle, rainMsg);
            }
        }

        // 2. CẢNH BÁO NẮNG GẮT & TIA CỰC TÍM (UV ALERT: 11h - 14h, UV >= 8)
        if (settings.EnableUvAlert && now.Hour >= 11 && now.Hour <= 14 && _lastUvAlertDate.Date != now.Date)
        {
            if (double.TryParse(current.UvIndexText, out double uv) && uv >= 8.0)
            {
                _lastUvAlertDate = now.Date;
                string uvTitle = isVi ? "🔥 Cảnh Báo Nắng Gắt & Tia UV Rất Cao!" : "🔥 Intense Sun & High UV Alert!";
                string uvMsg = isVi 
                    ? $"Chỉ số UV tại {current.LocationName} đang ở mức rất cao ({uv:F1}). {userName} nên bôi kem chống nắng và hạn chế tiếp xúc trực tiếp ngoài trời!"
                    : $"UV index at {current.LocationName} is extremely high ({uv:F1}). {userName}, apply sunscreen and minimize direct outdoor exposure!";
                ShowToast(uvTitle, uvMsg);
            }
            else if (current.WeatherEffect == WeatherEffectType.HighUvSunny)
            {
                _lastUvAlertDate = now.Date;
                string uvTitle = isVi ? "🔥 Cảnh Báo Tia UV Rất Cao!" : "🔥 High UV Alert!";
                string uvMsg = isVi 
                    ? $"Thời điểm buổi trưa tại {current.LocationName} có nắng gắt. {userName} hãy uống nhiều nước và che chắn cẩn thận khi ra đường!"
                    : $"Midday sun is intense in {current.LocationName}. Stay hydrated and cover up well outdoors, {userName}!";
                ShowToast(uvTitle, uvMsg);
            }
        }

        // 3. TÓM TẮT THỜI TIẾT ĐẦU NGÀY (MORNING BRIEFING: 06h - 09h)
        if (settings.EnableMorningBriefing && now.Hour >= 6 && now.Hour <= 9 && _lastMorningBriefingDate.Date != now.Date)
        {
            _lastMorningBriefingDate = now.Date;
            string tempRange = current.MinMaxText;
            string morningTitle = isVi ? $"☀️ Chào buổi sáng, {userName}!" : $"☀️ Good morning, {userName}!";
            string morningMsg = isVi 
                ? $"Hôm nay tại {current.LocationName} {current.ConditionText.ToLowerInvariant()}, {tempRange.ToLowerInvariant()}. Chúc bạn một ngày mới tràn đầy năng lượng!"
                : $"Today in {current.LocationName}: {current.ConditionText.ToLowerInvariant()}, {tempRange.ToLowerInvariant()}. Have a wonderful, energizing day!";
            ShowToast(morningTitle, morningMsg);
        }
    }
}
