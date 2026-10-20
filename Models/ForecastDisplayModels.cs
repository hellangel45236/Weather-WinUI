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
    public double Latitude { get; set; }
    public double Longitude { get; set; }
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
    public string HighLowTempText => MinMaxText;
    public string HumidityText { get; set; } = "--%";
    public string WindText { get; set; } = "-- km/h";
    public double WindSpeedValue { get; set; } = 12.0;
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
    public bool HasRainChance => RainProbabilityValue > 0;

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

    // Phục vụ giao diện Nothing Design System v3.5.0
    public string CityIataCode
    {
        get
        {
            if (string.IsNullOrEmpty(LocationName)) return "NDOT";
            var loc = LocationName.Trim();
            if (loc.Contains("Hà Nội", StringComparison.OrdinalIgnoreCase) || loc.Contains("Hanoi", StringComparison.OrdinalIgnoreCase)) return "HAN";
            if (loc.Contains("Hồ Chí Minh", StringComparison.OrdinalIgnoreCase) || loc.Contains("Ho Chi Minh", StringComparison.OrdinalIgnoreCase) || loc.Contains("Sài Gòn", StringComparison.OrdinalIgnoreCase) || loc.Contains("Saigon", StringComparison.OrdinalIgnoreCase)) return "SGN";
            if (loc.Contains("Đà Nẵng", StringComparison.OrdinalIgnoreCase) || loc.Contains("Da Nang", StringComparison.OrdinalIgnoreCase)) return "DAD";
            if (loc.Contains("Hải Phòng", StringComparison.OrdinalIgnoreCase) || loc.Contains("Hai Phong", StringComparison.OrdinalIgnoreCase)) return "HPH";
            if (loc.Contains("Cần Thơ", StringComparison.OrdinalIgnoreCase) || loc.Contains("Can Tho", StringComparison.OrdinalIgnoreCase)) return "VCA";
            if (loc.Contains("Nha Trang", StringComparison.OrdinalIgnoreCase)) return "CXR";
            if (loc.Contains("Huế", StringComparison.OrdinalIgnoreCase) || loc.Contains("Hue", StringComparison.OrdinalIgnoreCase)) return "HUI";
            if (loc.Contains("Đà Lạt", StringComparison.OrdinalIgnoreCase) || loc.Contains("Da Lat", StringComparison.OrdinalIgnoreCase)) return "DLI";
            if (loc.Contains("Quy Nhơn", StringComparison.OrdinalIgnoreCase) || loc.Contains("Quy Nhon", StringComparison.OrdinalIgnoreCase)) return "UIH";
            if (loc.Contains("Phú Quốc", StringComparison.OrdinalIgnoreCase) || loc.Contains("Phu Quoc", StringComparison.OrdinalIgnoreCase)) return "PQC";
            if (loc.Contains("Vinh", StringComparison.OrdinalIgnoreCase)) return "VII";
            if (loc.Contains("Tokyo", StringComparison.OrdinalIgnoreCase)) return "TYO";
            if (loc.Contains("Seoul", StringComparison.OrdinalIgnoreCase)) return "ICN";
            if (loc.Contains("Singapore", StringComparison.OrdinalIgnoreCase)) return "SIN";
            if (loc.Contains("Bangkok", StringComparison.OrdinalIgnoreCase)) return "BKK";
            if (loc.Contains("London", StringComparison.OrdinalIgnoreCase)) return "LON";
            if (loc.Contains("New York", StringComparison.OrdinalIgnoreCase)) return "NYC";
            if (loc.Contains("Paris", StringComparison.OrdinalIgnoreCase)) return "PAR";
            var letters = new string(loc.Where(char.IsLetter).ToArray());
            return letters.Length >= 3 ? letters.Substring(0, 3).ToUpperInvariant() : "MET";
        }
    }

    public string DaylightTelemetryText
    {
        get
        {
            if (IsDay)
            {
                int percent = (int)Math.Clamp(SunProgressPercent * 100, 0, 100);
                return $"DAYLIGHT ACTIVE // {percent}% ELAPSED";
            }
            return "NIGHT CYCLE // BELOW HORIZON";
        }
    }

    public string TemperatureNumberOnly
    {
        get
        {
            if (string.IsNullOrEmpty(TemperatureText)) return "--";
            var cleaned = TemperatureText.Replace("°C", "").Replace("°F", "").Replace("°", "").Trim();
            return string.IsNullOrEmpty(cleaned) ? "--" : cleaned;
        }
    }

    public string TemperatureUnitOnly => TemperatureText.Contains("°F") ? "°F" : "°C";

    public string FormattedCoordinates => (Latitude != 0 || Longitude != 0)
        ? $"LAT {Math.Abs(Latitude):F4}\u00B0 {(Latitude >= 0 ? "N" : "S")} // LON {Math.Abs(Longitude):F4}\u00B0 {(Longitude >= 0 ? "E" : "W")}"
        : "ACTIVE STATION // METEO-NDOT";

    public string FormattedConditionUppercase => string.IsNullOrEmpty(ConditionText) ? "WEATHER OBSERVATION" : ConditionText.ToUpperInvariant();

    public string GlyphConditionEnglish => WeatherEffect switch
    {
        WeatherEffectType.ClearSunny => IsDay ? "CLEAR SKY // SUNNY" : "CLEAR SKY // MOONLIT",
        WeatherEffectType.PartlyCloudy => IsDay ? "SCATTERED CLOUDS // FAIR" : "SCATTERED CLOUDS // NIGHT",
        WeatherEffectType.Cloudy => "OVERCAST // DENSE CLOUDS",
        WeatherEffectType.LightRain => "LIGHT RAIN // DRIZZLE",
        WeatherEffectType.HeavyRain => "HEAVY RAIN // DOWNPOUR",
        WeatherEffectType.Thunderstorm => "THUNDERSTORM // LIGHTNING",
        WeatherEffectType.Snow => "SNOW // BLIZZARD",
        WeatherEffectType.Fog => "MIST // DENSE FOG",
        _ => "LOCAL OBSERVATION"
    };

    public bool IsGlyphBar1Active => AqiValue > 0;
    public bool IsGlyphBar2Active => AqiValue >= 25;
    public bool IsGlyphBar3Active => AqiValue >= 50;
    public bool IsGlyphBar4Active => AqiValue >= 100;
    public bool IsGlyphBar5Active => AqiValue >= 150;
    public double GlyphBar1Opacity => IsGlyphBar1Active ? 1.0 : 0.15;
    public double GlyphBar2Opacity => IsGlyphBar2Active ? 1.0 : 0.15;
    public double GlyphBar3Opacity => IsGlyphBar3Active ? 1.0 : 0.15;
    public double GlyphBar4Opacity => IsGlyphBar4Active ? 1.0 : 0.15;
    public double GlyphBar5Opacity => IsGlyphBar5Active ? 1.0 : 0.15;
}

public class HourlyForecastItem : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    public int HourNumber { get; set; }
    public string TimeDisplay { get; set; } = string.Empty;
    public string TempDisplay { get; set; } = string.Empty;
    public double TempValue { get; set; }
    public string IconGlyph { get; set; } = "\uf185";
    public string SvgIconPath { get; set; } = string.Empty;
    public string ConditionText { get; set; } = string.Empty;
    public string RainProbabilityText { get; set; } = "0%";
    public int RainProbabilityValue { get; set; } = 0;
    public double WindSpeedValue { get; set; } = 0;
    public string WindSpeedDisplay { get; set; } = string.Empty;
    public double UvValue { get; set; } = 0;
    public string UvDisplay { get; set; } = string.Empty;
    public string UvLevelText { get; set; } = string.Empty;
    public string PrecipitationText { get; set; } = "0.0 mm";
    public string WindDirectionText { get; set; } = string.Empty;
    public double WindDirectionDegrees { get; set; } = 0;
    public double SurfacePressure { get; set; } = 1013.25;
    public bool IsNow { get; set; } = false;

    private bool _isSelected = false;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    private string _primaryMetricText = string.Empty;
    public string PrimaryMetricText
    {
        get => _primaryMetricText;
        set => SetProperty(ref _primaryMetricText, value);
    }

    private string _secondaryMetricText = string.Empty;
    public string SecondaryMetricText
    {
        get => _secondaryMetricText;
        set => SetProperty(ref _secondaryMetricText, value);
    }

    private string _secondaryIconGlyph = "\uf043";
    public string SecondaryIconGlyph
    {
        get => _secondaryIconGlyph;
        set => SetProperty(ref _secondaryIconGlyph, value);
    }

    private string _secondaryColor = "#0099BC";
    public string SecondaryColor
    {
        get => _secondaryColor;
        set => SetProperty(ref _secondaryColor, value);
    }

    private string _badgeText = string.Empty;
    public string BadgeText
    {
        get => _badgeText;
        set => SetProperty(ref _badgeText, value);
    }

    private string _badgeColor = string.Empty;
    public string BadgeColor
    {
        get => _badgeColor;
        set => SetProperty(ref _badgeColor, value);
    }

    public bool HasBadge => !string.IsNullOrEmpty(BadgeText);
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
    public string StarsDisplay => (ScoreProgress / 20.0) switch
    {
        >= 4.5 => "★★★★★",
        >= 3.5 => "★★★★☆",
        >= 2.5 => "★★★☆☆",
        >= 1.5 => "★★☆☆☆",
        _ => "★☆☆☆☆"
    };
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
