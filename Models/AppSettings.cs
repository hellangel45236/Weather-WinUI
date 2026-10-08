using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WeatherApp.Models;

public class AppSettings
{
    // 1. Giao diện (Theme)
    public string ThemeMode { get; set; } = "System"; // "System", "Light", "Dark"

    // 2. Thời gian & Múi giờ
    public string TimezoneMode { get; set; } = "Auto"; // "Auto", "GMT+7", "GMT+8", "GMT+9", "GMT+0", "GMT-5"
    public bool Is24HourFormat { get; set; } = true;
    public string DateFormat { get; set; } = "dd/MM/yyyy"; // "dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd"
    public string FirstDayOfWeek { get; set; } = "Monday"; // "Monday" (Thứ Hai) hoặc "Sunday" (Chủ Nhật)

    // 3. Đơn vị đo lường
    public string TemperatureUnit { get; set; } = "C"; // "C" (°C) hoặc "F" (°F)
    public string WindSpeedUnit { get; set; } = "kmh"; // "kmh" (km/h), "ms" (m/s), "mph" (mph)
    public string PressureUnit { get; set; } = "hPa"; // "hPa" (hPa), "mmHg" (mmHg)
    public string PrecipitationUnit { get; set; } = "mm"; // "mm" (mm), "inch" (in)

    // 4. Khởi động cùng hệ thống
    public bool LaunchAtStartup { get; set; } = false;

    // 5. Tần suất cập nhật & Tối ưu tài nguyên
    public int AutoRefreshIntervalMinutes { get; set; } = 30; // 0 = Thủ công, 15, 30, 60, 120 phút
    public bool EnableWeatherEffects { get; set; } = true; // Bật/tắt hiệu ứng mưa rơi, sấm sét để tiết kiệm RAM/CPU máy yếu
    public bool PauseEffectsWhenInactive { get; set; } = true; // Tạm dừng hiệu ứng khi thu nhỏ cửa sổ
    public bool AutoDetectLocationOnStart { get; set; } = true;

    // 6. Trạng thái khởi động lần đầu
    public bool HasCompletedOnboarding { get; set; } = false;

    // 7. Thông tin cá nhân hóa người dùng
    public string UserName { get; set; } = string.Empty;

    // 8. Cấu hình Widget Desktop
    public string WidgetStyle { get; set; } = "BryanCDynamic"; // "BryanCDynamic", "GlassCard", "Compact", "MiniIsland"
    public double WidgetOpacity { get; set; } = 1.0; // 0.3 - 1.0 (50% = 0.5)

    // 9. Cấu hình Hình nền Thành phố (City Picture)
    public bool EnableCityBackground { get; set; } = true;
    public string CityBackgroundMode { get; set; } = "Auto"; // "Auto", "Preset", "Custom"
    public string SelectedCityImage { get; set; } = "Auto";
    public string CustomCityImagePath { get; set; } = string.Empty;
    public bool EnableDynamicDayNightWallpaper { get; set; } = true;

    // Cấu hình Âm thanh Thiên nhiên Thư giãn (Ambient Soundscapes)
    public bool EnableAmbientSound { get; set; } = false;
    public double AmbientSoundVolume { get; set; } = 0.5;
    public string SelectedAmbientSound { get; set; } = "Auto"; // "Auto", "Rain", "Thunderstorm", "PineWind", "OceanWaves", "CafeRain"

    // 10. Cảnh báo thông minh (Windows Toast Notifications)
    public bool EnableToastNotifications { get; set; } = true;
    public bool EnableRainAlarm { get; set; } = true;
    public bool EnableUvAlert { get; set; } = true;
    public bool EnableMorningBriefing { get; set; } = true;

    // 11. Khay hệ thống (System Tray)
    public bool MinimizeToTray { get; set; } = true;
    public bool CloseToTray { get; set; } = false;
    public bool StartMinimizedToTray { get; set; } = false;

    // 12. Quản lý phiên bản & Tự động hóa hệ thống (v3.0.1)
    public string LastSeenVersion { get; set; } = string.Empty;
    public bool AutoCheckForUpdates { get; set; } = true;
    public bool EnableBatterySaverOptimization { get; set; } = true;

    // 13. Tính năng mới v1.7
    public List<FavoriteLocationItem> FavoriteLocations { get; set; } = new()
    {
        new FavoriteLocationItem { Name = "Hà Nội", Country = "Việt Nam", Latitude = 21.0285, Longitude = 105.8542 },
        new FavoriteLocationItem { Name = "Thành phố Hồ Chí Minh", Country = "Việt Nam", Latitude = 10.823, Longitude = 106.6296 },
        new FavoriteLocationItem { Name = "Đà Lạt", Country = "Việt Nam", Latitude = 11.9404, Longitude = 108.4583 },
        new FavoriteLocationItem { Name = "Đà Nẵng", Country = "Việt Nam", Latitude = 16.0544, Longitude = 108.2022 }
    };
    public bool EnableRadarMap { get; set; } = true;
    public bool EnableLifestyleIndices { get; set; } = true;
    public bool EnableLunarCalendar { get; set; } = true;

    // 14. Thông báo lịch trình Đi làm / Đi học / Tan ca (Commute Weather Alerts v2.0)
    public bool EnableCommuteAlerts { get; set; } = true;
    public string MorningCommuteTime { get; set; } = "07:30"; // Giờ đi sáng (HH:mm)
    public int MorningCommuteLeadMinutes { get; set; } = 30; // Báo trước 15, 30, 45, 60 phút
    public string EveningCommuteTime { get; set; } = "17:30"; // Giờ tan ca chiều (HH:mm)
    public int EveningCommuteLeadMinutes { get; set; } = 30; // Báo trước 15, 30, 45, 60 phút
    public bool CommuteMon { get; set; } = true;
    public bool CommuteTue { get; set; } = true;
    public bool CommuteWed { get; set; } = true;
    public bool CommuteThu { get; set; } = true;
    public bool CommuteFri { get; set; } = true;
    public bool CommuteSat { get; set; } = false;
    public bool CommuteSun { get; set; } = false;

    // 15. Gợi ý trang phục Hôm nay mặc gì (OOTD v2.1)
    public string SelectedOutfitOccasion { get; set; } = "Work"; // "Work", "School", "Casual"
    public string ShareCardFormat { get; set; } = "Landscape"; // "Landscape" (1920x1080), "Story" (1080x1920)

    // 16. Bộ biểu tượng thời tiết (Icon Pack)
    public string SelectedIconPack { get; set; } = "Meteocons"; // "Meteocons", "Fluent3D", "FontAwesome"

    // Vị trí lưu lại lần trước
    public string LastLocationName { get; set; } = string.Empty;
    public double LastLatitude { get; set; } = 10.823;
    public double LastLongitude { get; set; } = 106.6296;
}

public class FavoriteLocationItem
{
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public partial class QuickLocationChipItem : ObservableObject
{
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsFavorite { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChipBackgroundHex))]
    [NotifyPropertyChangedFor(nameof(ChipBorderHex))]
    [NotifyPropertyChangedFor(nameof(ChipForegroundHex))]
    [NotifyPropertyChangedFor(nameof(ChipFontWeight))]
    [NotifyPropertyChangedFor(nameof(ActiveIndicatorVisibility))]
    [NotifyPropertyChangedFor(nameof(IconColor))]
    private bool _isSelected;

    public string IconGlyph => IsFavorite ? "\uf004" : "\uf3c5";
    public string IconColor => IsSelected ? "#38BDF8" : (IsFavorite ? "#F43F5E" : "#38BDF8");

    public string ChipBackgroundHex => IsSelected ? "#3538BDF8" : "#1AFFFFFF";
    public string ChipBorderHex => IsSelected ? "#38BDF8" : "#2EFFFFFF";
    public string ChipForegroundHex => IsSelected ? "#38BDF8" : "#F1F5F9";
    public string ChipFontWeight => IsSelected ? "Bold" : "SemiBold";
    public bool ActiveIndicatorVisibility => IsSelected;
}
