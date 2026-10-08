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
        this.Loaded += (s, e) => UpdateCalendarComboBoxLabels();
        Services.LocalizationService.Instance.LanguageChanged += (s, e) => UpdateCalendarComboBoxLabels();
    }

    private void UpdateCalendarComboBoxLabels()
    {
        bool isVi = Services.LocalizationService.Instance.IsVietnamese;
        if (NewEventCategoryComboBox != null && NewEventCategoryComboBox.Items.Count >= 6)
        {
            ((ComboBoxItem)NewEventCategoryComboBox.Items[0]).Content = isVi ? "🌲 Ngoài trời" : "🌲 Outdoor";
            ((ComboBoxItem)NewEventCategoryComboBox.Items[1]).Content = isVi ? "💼 Công việc" : "💼 Work";
            ((ComboBoxItem)NewEventCategoryComboBox.Items[2]).Content = isVi ? "👨‍👩‍👧 Gia đình" : "👨‍👩‍👧 Family";
            ((ComboBoxItem)NewEventCategoryComboBox.Items[3]).Content = isVi ? "🏃 Thể thao" : "🏃 Sports";
            ((ComboBoxItem)NewEventCategoryComboBox.Items[4]).Content = isVi ? "🎉 Kỷ niệm" : "🎉 Celebration";
            ((ComboBoxItem)NewEventCategoryComboBox.Items[5]).Content = isVi ? "🕯️ Cúng lễ" : "🕯️ Spiritual";
        }
        if (NewEventReminderComboBox != null && NewEventReminderComboBox.Items.Count >= 4)
        {
            ((ComboBoxItem)NewEventReminderComboBox.Items[0]).Content = isVi ? "Không nhắc" : "No reminder";
            ((ComboBoxItem)NewEventReminderComboBox.Items[1]).Content = isVi ? "Trước 15p" : "15m before";
            ((ComboBoxItem)NewEventReminderComboBox.Items[2]).Content = isVi ? "Trước 30p" : "30m before";
            ((ComboBoxItem)NewEventReminderComboBox.Items[3]).Content = isVi ? "Trước 1h" : "1h before";
        }
        if (QuickGoalWorkoutText != null) QuickGoalWorkoutText.Text = isVi ? "Tập thể dục 🏃" : "Exercise 🏃";
        if (QuickGoalVegetarianText != null) QuickGoalVegetarianText.Text = isVi ? "Ăn chay 🥗" : "Vegetarian 🥗";
        if (QuickGoalReadingText != null) QuickGoalReadingText.Text = isVi ? "Đọc sách 📖" : "Reading 📖";
        if (QuickGoalHydrationText != null) QuickGoalHydrationText.Text = isVi ? "Uống nước 💧" : "Hydration 💧";
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
        if (sender is Button btn && btn.Tag is string tag)
        {
            bool isVi = Services.LocalizationService.Instance.IsVietnamese;
            string quickGoal = tag switch
            {
                "Workout" => isVi ? "Tập thể dục 30p 🏃" : "Exercise 30 mins 🏃",
                "Vegetarian" => isVi ? "Ăn chay mùng 1/rằm 🥗" : "Vegetarian day 🥗",
                "Reading" => isVi ? "Đọc sách 30p 📖" : "Read books 30 mins 📖",
                "Hydration" => isVi ? "Uống đủ 2L nước 💧" : "Drink 2L water 💧",
                _ => tag
            };
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
