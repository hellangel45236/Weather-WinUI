using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WeatherApp.Models;

public partial class CalendarDayItem : ObservableObject
{
    public DateTime Date { get; set; }

    public int SolarDay => Date.Day;
    public int SolarMonth => Date.Month;
    public int SolarYear => Date.Year;

    public int LunarDay { get; set; }
    public int LunarMonth { get; set; }
    public int LunarYear { get; set; }
    public bool IsLeapLunar { get; set; }

    public string LunarDisplay { get; set; } = string.Empty;
    public string LunarFullSummary => Services.LocalizationService.Instance.IsVietnamese
        ? $"🌙 Ngày {LunarDay} Tháng {LunarMonth} ÂL ({CanChiYear})"
        : $"🌙 Lunar Day {LunarDay}, Month {LunarMonth} ({CanChiYear})";
    public bool IsSpecialLunarDay => LunarDay == 1 || LunarDay == 15;

    public bool IsCurrentMonth { get; set; } = true;
    public bool IsToday { get; set; } = false;
    public bool IsWeekend { get; set; } = false;
    public string DayOfWeekName { get; set; } = string.Empty;

    // Ngày lễ
    public bool HasHoliday { get; set; }
    public string HolidayName { get; set; } = string.Empty;
    public string HolidayBadge { get; set; } = string.Empty;
    public string HolidayDescription { get; set; } = string.Empty;
    public bool IsOfficialDayOff { get; set; }

    // Can Chi Ba Trụ & Tiết khí
    public string CanChiYear { get; set; } = string.Empty;
    public string CanChiMonth { get; set; } = string.Empty;
    public string CanChiDay { get; set; } = string.Empty;
    public string SolarTerm { get; set; } = string.Empty;

    // Hoàng Đạo / Hắc Đạo & Giờ Hoàng Đạo
    public bool IsAuspiciousDay { get; set; } = true;
    public string AuspiciousDayName { get; set; } = "Hoàng Đạo (Tốt)";
    public string AuspiciousDayColor { get; set; } = "#10B981";
    public string AuspiciousHoursFormatted { get; set; } = string.Empty;
    public List<string> AuspiciousHoursList { get; set; } = new();

    // Tuần Trăng (Moon Phase)
    public string MoonPhaseIcon { get; set; } = "🌕";
    public string MoonPhaseName { get; set; } = "Trăng Tròn";
    public int MoonIllumination { get; set; } = 100;

    // Dự báo thời tiết (7 ngày tới)
    [ObservableProperty]
    private bool _hasWeatherForecast;

    [ObservableProperty]
    private string _weatherIconGlyph = "\uf185";

    [ObservableProperty]
    private string _weatherCondition = string.Empty;

    [ObservableProperty]
    private string _tempRangeText = string.Empty;

    [ObservableProperty]
    private double _tempMax;

    [ObservableProperty]
    private double _tempMin;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSunny))]
    [NotifyPropertyChangedFor(nameof(IsRainy))]
    [NotifyPropertyChangedFor(nameof(HasAnimation))]
    private string _weatherEffect = "Normal"; // "Sunny", "Rain", "Normal"

    public bool IsSunny => WeatherEffect == "Sunny";
    public bool IsRainy => WeatherEffect == "Rain";
    public bool HasAnimation => IsSunny || IsRainy;

    [ObservableProperty]
    private string _rainProbabilityText = "--%";

    [ObservableProperty]
    private string _humidityText = "--%";

    [ObservableProperty]
    private string _windText = "-- km/h";

    [ObservableProperty]
    private string _uvText = "--";

    // Ghi chú & Sự kiện cá nhân v1.9
    [ObservableProperty]
    private System.Collections.ObjectModel.ObservableCollection<CalendarUserEvent> _userEvents = new();

    public bool HasUserEvents => UserEvents != null && UserEvents.Count > 0;
    public int UserEventCount => UserEvents?.Count ?? 0;

    // Cảnh báo xung đột thời tiết với sự kiện ngoài trời (Weather-Aware Alert)
    [ObservableProperty]
    private bool _hasWeatherConflictWarning;

    [ObservableProperty]
    private string _weatherConflictTitle = string.Empty;

    [ObservableProperty]
    private string _weatherConflictMessage = string.Empty;

    // Mục tiêu trong ngày v2.0 (Daily Goals & Focus)
    [ObservableProperty]
    private bool _hasGoal;

    [ObservableProperty]
    private string _goalText = string.Empty;

    [ObservableProperty]
    private bool _isGoalCompleted;

    [ObservableProperty]
    private string _goalIcon = "🎯";

    public string UserEventsTooltip => Services.LocalizationService.Instance.IsVietnamese
        ? "Có sự kiện / kế hoạch ghi chú"
        : "Has scheduled events / notes";

    public string WeatherConflictTooltip => !string.IsNullOrEmpty(WeatherConflictTitle)
        ? WeatherConflictTitle
        : (Services.LocalizationService.Instance.IsVietnamese
            ? "Cảnh báo: Thời tiết xung đột với kế hoạch ngoài trời!"
            : "Warning: Weather conflict with outdoor plans!");

    public void NotifyEventsChanged()
    {
        OnPropertyChanged(nameof(UserEvents));
        OnPropertyChanged(nameof(HasUserEvents));
        OnPropertyChanged(nameof(UserEventCount));
        OnPropertyChanged(nameof(UserEventsTooltip));
        OnPropertyChanged(nameof(WeatherConflictTooltip));
        OnPropertyChanged(nameof(HasGoal));
        OnPropertyChanged(nameof(GoalText));
        OnPropertyChanged(nameof(IsGoalCompleted));
        OnPropertyChanged(nameof(GoalIcon));
    }

    public void NotifyWeatherChanged()
    {
        OnPropertyChanged(nameof(HasWeatherForecast));
        OnPropertyChanged(nameof(WeatherIconGlyph));
        OnPropertyChanged(nameof(WeatherCondition));
        OnPropertyChanged(nameof(TempRangeText));
        OnPropertyChanged(nameof(TempMax));
        OnPropertyChanged(nameof(TempMin));
        OnPropertyChanged(nameof(WeatherEffect));
        OnPropertyChanged(nameof(IsSunny));
        OnPropertyChanged(nameof(IsRainy));
        OnPropertyChanged(nameof(HasAnimation));
        OnPropertyChanged(nameof(RainProbabilityText));
        OnPropertyChanged(nameof(HumidityText));
        OnPropertyChanged(nameof(WindText));
        OnPropertyChanged(nameof(UvText));
        OnPropertyChanged(nameof(HasWeatherConflictWarning));
        OnPropertyChanged(nameof(WeatherConflictTitle));
        OnPropertyChanged(nameof(WeatherConflictMessage));
    }
}

public class CalendarFestivalCountdown
{
    public string Title { get; set; } = string.Empty;
    public string LunarDateText { get; set; } = string.Empty;
    public string SolarDateText { get; set; } = string.Empty;
    public int DaysRemaining { get; set; }
    public string DaysRemainingText { get; set; } = string.Empty;
    public string IconGlyph { get; set; } = "\uf06b";
    public string AccentColor { get; set; } = "#DC2626";
}
