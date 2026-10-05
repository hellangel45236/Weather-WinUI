using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using WeatherApp.Models;

namespace WeatherApp.Services;

public class CalendarEventService
{
    private readonly string _eventsFilePath;
    private readonly string _goalsFilePath;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public CalendarEventService()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appFolder = Path.Combine(localAppData, "WeatherAppWinUI");
        if (!Directory.Exists(appFolder))
        {
            Directory.CreateDirectory(appFolder);
        }

        _eventsFilePath = Path.Combine(appFolder, "user_calendar_events.json");
        _goalsFilePath = Path.Combine(appFolder, "user_calendar_goals.json");

        // Di chuyển an toàn từ thư mục cũ nếu có dữ liệu trước đây
        try
        {
            string oldFolder = Path.Combine(localAppData, "WeatherWinUI");
            string oldEventsPath = Path.Combine(oldFolder, "user_calendar_events.json");
            if (File.Exists(oldEventsPath) && !File.Exists(_eventsFilePath))
            {
                File.Copy(oldEventsPath, _eventsFilePath, overwrite: true);
            }
        }
        catch { }
    }

    #region Sự Kiện & Ghi Chú

    public List<CalendarUserEvent> LoadEvents()
    {
        try
        {
            if (File.Exists(_eventsFilePath))
            {
                string json = File.ReadAllText(_eventsFilePath);
                return JsonSerializer.Deserialize<List<CalendarUserEvent>>(json) ?? new List<CalendarUserEvent>();
            }
        }
        catch { }
        return new List<CalendarUserEvent>();
    }

    public void SaveEvents(List<CalendarUserEvent> events)
    {
        try
        {
            string json = JsonSerializer.Serialize(events, JsonOptions);
            string tmpPath = _eventsFilePath + ".tmp";
            File.WriteAllText(tmpPath, json);
            File.Move(tmpPath, _eventsFilePath, overwrite: true);
        }
        catch { }
    }

    public void AddEvent(CalendarUserEvent ev)
    {
        var list = LoadEvents();
        list.Add(ev);
        SaveEvents(list);
    }

    public void UpdateEvent(CalendarUserEvent ev)
    {
        var list = LoadEvents();
        int idx = list.FindIndex(e => e.Id == ev.Id);
        if (idx >= 0)
        {
            list[idx] = ev;
            SaveEvents(list);
        }
    }

    public void DeleteEvent(string id)
    {
        var list = LoadEvents();
        list.RemoveAll(e => e.Id == id);
        SaveEvents(list);
    }

    #endregion

    #region Mục Tiêu Trong Ngày (Daily Goals)

    public List<CalendarDayGoal> LoadGoals()
    {
        try
        {
            if (File.Exists(_goalsFilePath))
            {
                string json = File.ReadAllText(_goalsFilePath);
                return JsonSerializer.Deserialize<List<CalendarDayGoal>>(json) ?? new List<CalendarDayGoal>();
            }
        }
        catch { }
        return new List<CalendarDayGoal>();
    }

    public void SaveGoals(List<CalendarDayGoal> goals)
    {
        try
        {
            string json = JsonSerializer.Serialize(goals, JsonOptions);
            string tmpPath = _goalsFilePath + ".tmp";
            File.WriteAllText(tmpPath, json);
            File.Move(tmpPath, _goalsFilePath, overwrite: true);
        }
        catch { }
    }

    public void SetGoal(string dateKey, string text, string icon = "🎯")
    {
        var list = LoadGoals();
        var existing = list.FirstOrDefault(g => g.DateKey == dateKey);
        if (existing != null)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                list.Remove(existing);
            }
            else
            {
                existing.GoalText = text.Trim();
                existing.GoalIcon = icon;
                existing.UpdatedAt = DateTime.Now;
            }
        }
        else if (!string.IsNullOrWhiteSpace(text))
        {
            list.Add(new CalendarDayGoal
            {
                DateKey = dateKey,
                GoalText = text.Trim(),
                GoalIcon = icon,
                IsCompleted = false,
                UpdatedAt = DateTime.Now
            });
        }
        SaveGoals(list);
    }

    public bool ToggleGoal(string dateKey)
    {
        var list = LoadGoals();
        var existing = list.FirstOrDefault(g => g.DateKey == dateKey);
        if (existing != null)
        {
            existing.IsCompleted = !existing.IsCompleted;
            existing.UpdatedAt = DateTime.Now;
            SaveGoals(list);
            return existing.IsCompleted;
        }
        return false;
    }

    public void DeleteGoal(string dateKey)
    {
        var list = LoadGoals();
        list.RemoveAll(g => g.DateKey == dateKey);
        SaveGoals(list);
    }

    #endregion
}
