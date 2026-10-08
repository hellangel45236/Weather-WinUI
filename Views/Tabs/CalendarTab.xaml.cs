using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WeatherApp.Models;
using WeatherApp.ViewModels;

namespace WeatherApp.Views.Tabs;

public sealed partial class CalendarTab : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(MainViewModel), typeof(CalendarTab), new PropertyMetadata(null));

    public MainViewModel? ViewModel
    {
        get => (MainViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public CalendarTab()
    {
        this.InitializeComponent();
    }

    private void CalendarDayCell_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is CalendarDayItem day)
        {
            ViewModel?.OpenDayDetail(day);
        }
    }

    private void GoToTodayCalendar_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        var today = ViewModel.CalendarDays.FirstOrDefault(d => d.IsToday) ?? ViewModel.CalendarDays.FirstOrDefault(d => d.IsCurrentMonth);
        if (today != null)
        {
            ViewModel.OpenDayDetail(today);
        }
    }

    private void CalendarMonthComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Two-way binding updates ViewModel.SelectedMonthIndex and triggers GenerateCalendar()
    }

    private void CalendarYearComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Two-way binding updates ViewModel.SelectedYear and triggers GenerateCalendar()
    }

    private void SaveEventButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null || NewEventTitleBox == null) return;
        string title = NewEventTitleBox.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(title)) return;

        string category = "Ngoài trời";
        if (NewEventCategoryComboBox?.SelectedItem is ComboBoxItem item && item.Tag is string cat)
        {
            category = cat;
        }

        bool isOutdoor = NewEventIsOutdoorCheckBox?.IsChecked ?? true;
        bool hasTime = NewEventHasTimeCheckBox?.IsChecked ?? true;
        string eventTime = "09:00";
        if (NewEventTimePicker != null)
        {
            var t = NewEventTimePicker.Time;
            eventTime = $"{t.Hours:D2}:{t.Minutes:D2}";
        }

        int reminderMinutes = 30;
        if (NewEventReminderComboBox?.SelectedItem is ComboBoxItem remItem && remItem.Tag is string remTag && int.TryParse(remTag, out int parsedMins))
        {
            reminderMinutes = parsedMins;
        }

        ViewModel.AddCalendarUserEvent(title, category, isOutdoor, hasTime, eventTime, reminderMinutes);
        NewEventTitleBox.Text = string.Empty;
    }

    private void DeleteEventButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        if (sender is Button btn && btn.Tag is string eventId)
        {
            ViewModel.DeleteCalendarUserEvent(eventId);
        }
    }

    private void GoalCompletedCheckBox_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.ToggleSelectedDayGoal();
    }

    private void DeleteGoalButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.DeleteSelectedDayGoal();
        if (DayGoalInputTextBox != null) DayGoalInputTextBox.Text = string.Empty;
    }

    private void SaveGoalButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null || DayGoalInputTextBox == null) return;
        string text = DayGoalInputTextBox.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(text)) return;
        ViewModel.SetSelectedDayGoal(text);
    }

    private void QuickGoalChip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string quickGoal)
        {
            if (DayGoalInputTextBox != null) DayGoalInputTextBox.Text = quickGoal;
            ViewModel?.SetSelectedDayGoal(quickGoal);
        }
    }

    private void CopyDayInfoButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.CopySelectedDayInfoCommand.Execute(null);
    }

    private void ConvertSolarToLunar_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        if (SolarDatePicker != null)
        {
            ViewModel.ConverterSolarDate = SolarDatePicker.Date;
        }
        ViewModel.ConvertSolarToLunarCommand.Execute(null);
    }

    private void ConvertLunarToSolar_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        if (int.TryParse(LunarDayBox?.Text, out int d)) ViewModel.ConverterLunarDay = d;
        if (int.TryParse(LunarMonthBox?.Text, out int m)) ViewModel.ConverterLunarMonth = m;
        if (int.TryParse(LunarYearBox?.Text, out int y)) ViewModel.ConverterLunarYear = y;
        if (LunarLeapCheckBox != null) ViewModel.ConverterIsLunarLeap = LunarLeapCheckBox.IsChecked == true;
        ViewModel.ConvertLunarToSolarCommand.Execute(null);
    }
}
