using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WeatherApp.Models;

public partial class CalendarUserEvent : ObservableObject
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Date { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "Ngoài trời"; // "Ngoài trời", "Công việc", "Gia đình", "Thể thao", "Kỷ niệm"
    public bool IsOutdoor { get; set; } = true;
    public string ColorHex { get; set; } = "#8B5CF6";
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Thời gian cụ thể & Nhắc nhở (v2.0)
    public bool HasSpecificTime { get; set; } = true;
    public string EventTime { get; set; } = "09:00"; // Định dạng HH:mm
    public int ReminderMinutesBefore { get; set; } = 30; // -1: Không nhắc, 0: Đúng giờ, 15, 30, 60, 120
    public bool IsReminded { get; set; } = false;

    public string TimeDisplay => HasSpecificTime && !string.IsNullOrWhiteSpace(EventTime)
        ? EventTime
        : (Services.LocalizationService.Instance.IsVietnamese ? "Cả ngày" : "All day");

    public string OutdoorDisplay => Services.LocalizationService.Instance.IsVietnamese ? "🌲 Hoạt động ngoài trời" : "🌲 Outdoor activity";

    public string CategoryDisplay => Category switch
    {
        "Ngoài trời" or "Outdoor" => Services.LocalizationService.Instance.IsVietnamese ? "Ngoài trời" : "Outdoor",
        "Công việc" or "Work" => Services.LocalizationService.Instance.IsVietnamese ? "Công việc" : "Work",
        "Gia đình" or "Family" => Services.LocalizationService.Instance.IsVietnamese ? "Gia đình" : "Family",
        "Thể thao" or "Sport" => Services.LocalizationService.Instance.IsVietnamese ? "Thể thao" : "Sports",
        "Kỷ niệm" or "Ceremony" => Services.LocalizationService.Instance.IsVietnamese ? "Kỷ niệm" : "Celebration",
        "Cúng lễ" or "Spiritual" => Services.LocalizationService.Instance.IsVietnamese ? "Cúng lễ" : "Spiritual",
        _ => Category
    };

    public string ReminderDisplay => ReminderMinutesBefore switch
    {
        < 0 => Services.LocalizationService.Instance.IsVietnamese ? "Không nhắc" : "No reminder",
        0 => Services.LocalizationService.Instance.IsVietnamese ? "Đúng giờ" : "On time",
        15 => Services.LocalizationService.Instance.IsVietnamese ? "Trước 15p" : "15m before",
        30 => Services.LocalizationService.Instance.IsVietnamese ? "Trước 30p" : "30m before",
        60 => Services.LocalizationService.Instance.IsVietnamese ? "Trước 1h" : "1h before",
        120 => Services.LocalizationService.Instance.IsVietnamese ? "Trước 2h" : "2h before",
        _ => Services.LocalizationService.Instance.IsVietnamese ? $"Trước {ReminderMinutesBefore}p" : $"{ReminderMinutesBefore}m before"
    };
}

public class CalendarDayGoal
{
    public string DateKey { get; set; } = string.Empty; // "yyyy-MM-dd"
    public string GoalText { get; set; } = string.Empty;
    public bool IsCompleted { get; set; } = false;
    public string GoalIcon { get; set; } = "🎯"; // "🎯", "💧", "🏃", "📚", "💼", "🧘"
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
