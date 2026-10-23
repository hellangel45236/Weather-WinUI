using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using WeatherApp.Helpers;

namespace WeatherApp.Models;

public class AppSettings
{
    // 1. Giao diện (Theme) & Chủ đề Mùa/Lễ hội (Seasonal & Festival Skins)
    public string ThemeMode { get; set; } = "System"; // "System", "Light", "Dark"
    public string DesignLanguage { get; set; } = "Fluent2"; // "Fluent2", "Nothing", "SpatialGlass"
    public string NavigationLayoutMode { get; set; } = "FloatingDock"; // "FloatingDock", "LeftSidebar"
    public bool EnableNothingEInkMode { get; set; } = false; // Nothing OS E-Ink Warm Paper Mode (#F4F1EA)
    public string SeasonalSkinMode { get; set; } = "Auto"; // "Auto", "Spring", "Summer", "Autumn", "Winter", "Tet", "MidAutumn", "Christmas", "Halloween"
    public bool EnableSeasonalParticles { get; set; } = true;
    public string SeasonalWidgetTheme { get; set; } = "Auto";
    public bool ShowSeasonalHeaderBadge { get; set; } = true;

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
    public string AppLanguage { get; set; } = "vi-VN"; // "vi-VN", "en-US"

    // 8. Cấu hình Widget Desktop
    public string WidgetStyle { get; set; } = "BryanCDynamic"; // "BryanCDynamic", "GlassCard", "Compact", "MiniIsland", "SeasonalCard", "BentoVivid", "BentoTactical", "RetroLcd", "NothingBento", "NothingSquare"
    public double WidgetOpacity { get; set; } = 1.0; // 0.3 - 1.0 (50% = 0.5)
    public int WidgetLastX { get; set; } = -1;
    public int WidgetLastY { get; set; } = -1;
    public bool WidgetAlwaysOnTop { get; set; } = true;
    public bool WidgetGhostClickThrough { get; set; } = false;
    public bool WidgetLockPosition { get; set; } = false;

    // Tùy biến thời trang OOTD cá nhân
    public string PreferredCommuteMethod { get; set; } = "Motorbike"; // "Motorbike", "Car", "Bus", "Walk"
    public string PreferredThermalSensitivity { get; set; } = "Normal"; // "ColdSensitive", "Normal", "HeatSensitive"

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

    // 14. Am thanh co hoc Nothing Tech (Mechanical Tactile SFX - v3.5.2)
    public bool EnableMechanicalSound { get; set; } = true;

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

    // 15. Gợi ý trang phục Hôm nay mặc gì (OOTD v3.0 Pro)
    public string SelectedOutfitOccasion { get; set; } = "Work"; // "Work", "School", "Casual", "Sport", "Travel"
    public string SelectedOutfitGender { get; set; } = "All"; // "All", "Men", "Women"
    public string ShareCardFormat { get; set; } = "Landscape"; // "Landscape" (1920x1080), "Story" (1080x1920)

    // 16. Bộ biểu tượng thời tiết (Icon Pack)
    public string SelectedIconPack { get; set; } = "Meteocons"; // "Meteocons", "Fluent3D", "FontAwesome"

    // 17. Chế độ Thư Giãn Toàn Cảnh (Zen Immersive Mode v3.1.0)
    public string ZenClockStyle { get; set; } = "Modern"; // "Modern", "Flip", "Stacked", "Analog"
    public bool ZenShowSeconds { get; set; } = true;
    public bool ZenShowWeatherInfo { get; set; } = true;
    public bool ZenShowHourlyForecast { get; set; } = true;
    public bool ZenShowAirQuality { get; set; } = true;
    public bool ZenShowRelaxQuote { get; set; } = true;
    public bool ZenShowAmbientSound { get; set; } = true;

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
    public string DisplayName
    {
        get
        {
            if (Services.LocalizationService.Instance.IsVietnamese)
            {
                return Name;
            }
            string n = (Name ?? string.Empty).Trim();
            if (n.Contains("Hồ Chí Minh", StringComparison.OrdinalIgnoreCase) || n.Equals("TP.HCM", StringComparison.OrdinalIgnoreCase))
                return "Ho Chi Minh City";
            if (n.Equals("Hà Nội", StringComparison.OrdinalIgnoreCase))
                return "Hanoi";
            if (n.Equals("Đà Nẵng", StringComparison.OrdinalIgnoreCase))
                return "Da Nang";
            if (n.Equals("Đà Lạt", StringComparison.OrdinalIgnoreCase))
                return "Da Lat";
            if (n.Equals("Hải Phòng", StringComparison.OrdinalIgnoreCase))
                return "Hai Phong";
            if (n.Equals("Cần Thơ", StringComparison.OrdinalIgnoreCase))
                return "Can Tho";
            if (n.Equals("Vũng Tàu", StringComparison.OrdinalIgnoreCase))
                return "Vung Tau";
            if (n.Equals("Quy Nhơn", StringComparison.OrdinalIgnoreCase))
                return "Quy Nhon";
            if (n.Equals("Phú Quốc", StringComparison.OrdinalIgnoreCase))
                return "Phu Quoc";
            if (n.Equals("Nha Trang", StringComparison.OrdinalIgnoreCase))
                return "Nha Trang";
            if (n.Equals("Huế", StringComparison.OrdinalIgnoreCase))
                return "Hue";
            if (n.Equals("Sa Pa", StringComparison.OrdinalIgnoreCase))
                return "Sa Pa";
            return n;
        }
    }
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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChipBackgroundHex))]
    [NotifyPropertyChangedFor(nameof(ChipBorderHex))]
    [NotifyPropertyChangedFor(nameof(ChipForegroundHex))]
    [NotifyPropertyChangedFor(nameof(IconColor))]
    private bool _isNothingDesign;

    public string IconGlyph => IsFavorite ? "\uf004" : "\uf3c5";
    public string IconColor => IsSelected
        ? (IsNothingDesign ? "#D71921" : (ThemeHelper.IsDarkTheme() ? "#38BDF8" : "#0284C7"))
        : (IsFavorite ? "#F43F5E" : (IsNothingDesign ? (ThemeHelper.IsDarkTheme() ? "#888888" : "#666666") : (ThemeHelper.IsDarkTheme() ? "#38BDF8" : "#0284C7")));

    public string ChipBackgroundHex => IsSelected
        ? (IsNothingDesign ? (ThemeHelper.IsDarkTheme() ? "#2A1416" : "#FEE2E2") : (ThemeHelper.IsDarkTheme() ? "#3538BDF8" : "#E0F2FE"))
        : (IsNothingDesign ? (ThemeHelper.IsDarkTheme() ? "#141416" : "#F4F4F6") : (ThemeHelper.IsDarkTheme() ? "#1AFFFFFF" : "#F1F5F9"));

    public string ChipBorderHex => IsSelected
        ? (IsNothingDesign ? "#D71921" : (ThemeHelper.IsDarkTheme() ? "#38BDF8" : "#0284C7"))
        : (IsNothingDesign ? (ThemeHelper.IsDarkTheme() ? "#28282C" : "#E2E2E6") : (ThemeHelper.IsDarkTheme() ? "#2EFFFFFF" : "#CBD5E1"));

    public string ChipForegroundHex => IsSelected
        ? (IsNothingDesign ? (ThemeHelper.IsDarkTheme() ? "#FFFFFF" : "#D71921") : (ThemeHelper.IsDarkTheme() ? "#38BDF8" : "#0369A1"))
        : (IsNothingDesign ? (ThemeHelper.IsDarkTheme() ? "#CCCCCC" : "#222222") : (ThemeHelper.IsDarkTheme() ? "#F1F5F9" : "#1E293B"));

    public string ChipFontWeight => IsSelected ? "Bold" : "SemiBold";
    public bool ActiveIndicatorVisibility => IsSelected;
}

public class NowcastBarItem
{
    public string TimeLabel { get; set; } = string.Empty;
    public double BarHeight { get; set; } = 8;
    public string BarColorHex { get; set; } = "#D71921";
    public string TooltipText { get; set; } = string.Empty;
    public double Opacity { get; set; } = 0.85;
    public string MinuteLabel => TimeLabel;
    public string IntensityLabel => TooltipText;
    public string FluentColorHex => BarColorHex;
    public string NothingColorHex => BarColorHex;
}
