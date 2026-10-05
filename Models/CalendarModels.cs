using System;
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

    // Can Chi & Tiết khí
    public string CanChiYear { get; set; } = string.Empty;
    public string SolarTerm { get; set; } = string.Empty;

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

    public void NotifyEventsChanged()
    {
        OnPropertyChanged(nameof(UserEvents));
        OnPropertyChanged(nameof(HasUserEvents));
        OnPropertyChanged(nameof(UserEventCount));
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
