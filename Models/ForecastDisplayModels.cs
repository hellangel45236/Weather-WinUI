namespace WeatherApp.Models;

public enum WeatherEffectType
{
    ClearSunny,      // Nắng vàng ấm
    HighUvSunny,     // Nắng gắt / Cảnh báo UV cao
    ClearNight,      // Đêm quang mây, trăng sao
    PartlyCloudy,    // Mây rải rác
    Cloudy,          // Nhiều mây u ám
    Fog,             // Sương mù
    LightRain,       // Mưa phùn nhẹ hạt
    ModerateRain,    // Mưa vừa
    HeavyRain,       // Mưa to xối xả
    Thunderstorm,    // Dông sét (mưa nặng + chớp giật nhấp nháy)
    Snow             // Tuyết rơi
}

public class CurrentWeatherDisplay
{
    public string LocationName { get; set; } = string.Empty;
    public string TemperatureText { get; set; } = "--°C";
    public double TemperatureValue { get; set; }
    public string FeelsLikeText { get; set; } = "Cảm giác: --°C";
    public string ConditionText { get; set; } = "Đang tải dữ liệu...";
    public string IconGlyph { get; set; } = "\uf185";
    public string SvgIconPath { get; set; } = string.Empty;
    public string SvgIconFullPath { get; set; } = string.Empty;
    public Uri? SvgIconUri { get; set; }
    public string SvgContent { get; set; } = string.Empty;
    public WeatherEffectType WeatherEffect { get; set; } = WeatherEffectType.ClearSunny;
    public string WeatherAlertBadgeText { get; set; } = string.Empty;
    public bool HasWeatherAlertBadge => !string.IsNullOrEmpty(WeatherAlertBadgeText);
    public string WeatherAlertBadgeColor { get; set; } = "#F59E0B";

    public string MinMaxText { get; set; } = "Thấp nhất: --°C • Cao nhất: --°C";
    public string HumidityText { get; set; } = "--%";
    public string WindText { get; set; } = "-- km/h";
    public string WindDirectionText { get; set; } = "--";
    public string PressureText { get; set; } = "-- hPa";
    public string UvIndexText { get; set; } = "--";
    public string UvIndexDescription { get; set; } = "--";
    public string RainProbabilityText { get; set; } = "--%";
    public string PrecipitationText { get; set; } = "-- mm";
    public string SunriseText { get; set; } = "--:--";
    public string SunsetText { get; set; } = "--:--";
    public string UpdatedTimeText { get; set; } = string.Empty;
    public bool IsDay { get; set; } = true;
    public string HeroGradientStart { get; set; } = "#1E3C72";
    public string HeroGradientEnd { get; set; } = "#2A5298";

    // Các trường dữ liệu phục vụ trực quan hoá Visual Gauges (Bento Grid v3.0.6)
    public double WindDirectionDegrees { get; set; } = 0;
    public double UvIndexValue { get; set; } = 0;
    public double HumidityValue { get; set; } = 0;
    public double RainProbabilityValue { get; set; } = 0;
    public string DewPointText { get; set; } = "--°C";
    public string PressureTrendText { get; set; } = "Ổn định";
    public double UvProgressPercent => Math.Clamp(UvIndexValue / 11.0, 0.05, 1.0);
    public double HumidityProgressPercent => Math.Clamp(HumidityValue / 100.0, 0.05, 1.0);
    public double RainProgressPercent => Math.Clamp(RainProbabilityValue / 100.0, 0.0, 1.0);

    // Báo cáo Chất lượng không khí & Bụi mịn chuyên sâu (Air Quality Tracker)
    public int AqiValue { get; set; } = 0;
    public string AqiDescription { get; set; } = "Trong lành";
    public string AqiColor { get; set; } = "#10B981";
    public string AqiAdvice { get; set; } = "Chất lượng không khí trong lành, rất thích hợp cho các hoạt động ngoài trời.";
    public double AqiProgressPercent { get; set; } = 0.1;
    public string Pm25Text { get; set; } = "-- µg/m³";
    public string Pm25Status { get; set; } = "Bình thường";
    public string Pm10Text { get; set; } = "-- µg/m³";
    public string Pm10Status { get; set; } = "Bình thường";
    public string OzoneText { get; set; } = "-- µg/m³";
    public string OzoneStatus { get; set; } = "Bình thường";
    public string No2Text { get; set; } = "-- µg/m³";
    public string No2Status { get; set; } = "Bình thường";
    public string So2Text { get; set; } = "-- µg/m³";
    public string CoText { get; set; } = "-- µg/m³";
    public bool HasAqi => AqiValue > 0;

    // Hình nền địa điểm
    public string? CityImagePath { get; set; }
    public bool HasCityImage => !string.IsNullOrEmpty(CityImagePath);

    // Tính năng mới v1.7: Vòng cung Mặt Trời & Mặt Trăng (Sun & Moon Arc Tracker)
    public double SunProgressPercent { get; set; } = 0.5; // 0.0 -> 1.0
    public string SunStatusText { get; set; } = string.Empty;
    public bool IsSunVisible { get; set; } = true;

    // Tính năng mới v1.7: Lịch Âm & 24 Tiết Khí Việt Nam
    public string LunarDateText { get; set; } = string.Empty;
    public string SolarTermText { get; set; } = string.Empty;

    // Tính năng mới v1.7: Câu tóm tắt thông minh đầu ngày (Smart Summary)
    public string SmartSummaryText { get; set; } = string.Empty;
}

public class HourlyForecastItem
{
    public int HourNumber { get; set; }
    public string TimeDisplay { get; set; } = string.Empty;
    public string TempDisplay { get; set; } = string.Empty;
    public double TempValue { get; set; }
    public string IconGlyph { get; set; } = "\uf185";
    public string SvgIconPath { get; set; } = string.Empty;
    public string ConditionText { get; set; } = string.Empty;
    public string RainProbabilityText { get; set; } = "0%";
    public bool HasRainChance => !string.IsNullOrEmpty(RainProbabilityText) && RainProbabilityText != "0%";
}

public class DailyForecastItem
{
    public string DayName { get; set; } = string.Empty;
    public string DateDisplay { get; set; } = string.Empty;
    public string IconGlyph { get; set; } = "\uf185";
    public string SvgIconPath { get; set; } = string.Empty;
    public string ConditionText { get; set; } = string.Empty;
    public string TempMaxDisplay { get; set; } = "--°";
    public string TempMinDisplay { get; set; } = "--°";
    public string RainProbabilityText { get; set; } = "0%";

    // Tính năng mới v1.7: Thanh đo nhiệt độ tuần trực quan (Weekly Min-Max Range Bar)
    public double MinTemp { get; set; }
    public double MaxTemp { get; set; }
    public double BarLeftMargin { get; set; } = 0;
    public double BarWidth { get; set; } = 50.0;
    public Microsoft.UI.Xaml.Thickness BarMarginThickness => new Microsoft.UI.Xaml.Thickness(BarLeftMargin, 0, 0, 0);
    public bool IsToday { get; set; } = false;
    public double CurrentTempIndicatorOffset { get; set; } = 0;
    public Microsoft.UI.Xaml.Thickness CurrentTempDotMargin => new Microsoft.UI.Xaml.Thickness(Math.Max(0, BarLeftMargin + CurrentTempIndicatorOffset - 3), 0, 0, 0);
    public bool ShowCurrentIndicator { get; set; } = false;
}

public class LifestyleIndexItem
{
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Rating { get; set; } = string.Empty;
    public string RatingColor { get; set; } = "#10B981";
    public string Advice { get; set; } = string.Empty;
    public string IconGlyph { get; set; } = "\uf44b";
    public double ScoreProgress { get; set; } = 80;
    public string MetricBadge { get; set; } = string.Empty;
}

public class WorkoutWindowItem
{
    public string TimeRange { get; set; } = "06:00 - 08:30";
    public string Label { get; set; } = "Sáng sớm • Giờ Vàng";
    public string Temperature { get; set; } = "24°C";
    public string Condition { get; set; } = "Mát mẻ, tạnh ráo";
    public string Status { get; set; } = "Rất lý tưởng";
    public string StatusColor { get; set; } = "#10B981";
    public string IconGlyph { get; set; } = "\uf185";
    public string Advice { get; set; } = "Thời điểm chạy bộ & đạp xe tốt nhất";
}

public class SkinDefenseModel
{
    public double UvIndex { get; set; } = 0;
    public string UvLevelText { get; set; } = "Thấp (An toàn)";
    public string UvColor { get; set; } = "#10B981";
    public string RecommendedSpf { get; set; } = "SPF 30+";
    public string MaxSafeSunTime { get; set; } = "Không giới hạn";
    public string MaskRecommendation { get; set; } = "Khẩu trang thông thường";
    public string MaskColor { get; set; } = "#10B981";
    public string MaskIcon { get; set; } = "\uf6cf";
    public string SkincareAdvice { get; set; } = "Bôi kem chống nắng nhẹ khi ra đường.";
}
