using System;

namespace WeatherApp.Models;

public class CalendarUserEvent
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

    public string TimeDisplay => HasSpecificTime && !string.IsNullOrWhiteSpace(EventTime) ? EventTime : "Cả ngày";

    public string ReminderDisplay => ReminderMinutesBefore switch
    {
        < 0 => "Không nhắc",
        0 => "Đúng giờ",
        15 => "Trước 15p",
        30 => "Trước 30p",
        60 => "Trước 1h",
        120 => "Trước 2h",
        _ => $"Trước {ReminderMinutesBefore}p"
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
