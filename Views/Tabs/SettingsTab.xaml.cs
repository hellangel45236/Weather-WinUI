using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WeatherApp.Helpers;
using WeatherApp.Models;
using WeatherApp.Services;
using WeatherApp.ViewModels;

namespace WeatherApp.Views.Tabs;

public sealed partial class SettingsTab : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(MainViewModel), typeof(SettingsTab), new PropertyMetadata(null, OnViewModelChanged));

    public MainViewModel? ViewModel
    {
        get => (MainViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public event EventHandler? OpenChangelogRequested;
    public event EventHandler? ReopenOnboardingRequested;

    private bool _isSyncing = false;

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SettingsTab tab)
        {
            tab.SyncAllSettings();
        }
    }

    public SettingsTab()
    {
        this.InitializeComponent();
        this.Loaded += (s, e) => SyncAllSettings();
    }

    public void SyncAllSettings()
    {
        if (ViewModel == null) return;
        _isSyncing = true;
        try
        {
            var s = ViewModel.Settings;

            // User name
            if (SettingsUserNameBox != null) SettingsUserNameBox.Text = s.UserName ?? "";

            // Navigation panel selection default
            if (SettingsNavListView != null && SettingsNavListView.SelectedIndex < 0)
            {
                SettingsNavListView.SelectedIndex = 0;
            }

            // Theme Combos
            int themeIdx = s.ThemeMode switch
            {
                "light" => 0,
                "dark" => 1,
                _ => 2
            };
            if (SettingsThemeComboBox != null) SettingsThemeComboBox.SelectedIndex = themeIdx;
            if (AppearanceThemeComboBox != null) AppearanceThemeComboBox.SelectedIndex = themeIdx;

            // Icon Packs
            UpdateIconPackVisuals(s.SelectedIconPack ?? "Meteocons");

            // City Background
            if (CityBgToggle != null) CityBgToggle.IsOn = s.EnableCityBackground;
            if (CityBgControlsPanel != null) CityBgControlsPanel.Visibility = s.EnableCityBackground ? Visibility.Visible : Visibility.Collapsed;
            if (CityImageModeComboBox != null)
            {
                for (int i = 0; i < CityImageModeComboBox.Items.Count; i++)
                {
                    if (CityImageModeComboBox.Items[i] is ComboBoxItem item && item.Tag?.ToString() == s.CityBackgroundMode)
                    {
                        CityImageModeComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
            PopulatePresetCityImageComboBox();
            UpdateCityControlsVisibility();
            UpdateCityPreview();

            // Widget
            if (WidgetStyleComboBox != null)
            {
                for (int i = 0; i < WidgetStyleComboBox.Items.Count; i++)
                {
                    if (WidgetStyleComboBox.Items[i] is ComboBoxItem item && item.Tag?.ToString() == s.WidgetStyle)
                    {
                        WidgetStyleComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
            if (WidgetOpacitySlider != null)
            {
                double op = s.WidgetOpacity;
                WidgetOpacitySlider.Value = op > 1.0 ? op : op * 100.0;
            }
            if (WidgetOpacityValueText != null)
            {
                int percent = (int)(s.WidgetOpacity > 1.0 ? s.WidgetOpacity : s.WidgetOpacity * 100);
                WidgetOpacityValueText.Text = $"{percent}%";
            }

            // Units & Time
            if (HourFormatToggle != null) HourFormatToggle.IsOn = s.Is24HourFormat;
            SelectComboByTag(DateFormatComboBox, s.DateFormat);
            SelectComboByTag(FirstDayOfWeekComboBox, s.FirstDayOfWeek);
            SelectComboByTag(TempUnitComboBox, s.TemperatureUnit);
            SelectComboByTag(WindUnitComboBox, s.WindSpeedUnit);
            SelectComboByTag(PrecipUnitComboBox, s.PrecipitationUnit);
            SelectComboByTag(PressureUnitComboBox, s.PressureUnit);
            SelectComboByTag(TimezoneComboBox, s.TimezoneMode);
            SelectComboByTag(RefreshIntervalComboBox, s.AutoRefreshIntervalMinutes.ToString());

            // Notifications
            if (ToastNotificationToggle != null) ToastNotificationToggle.IsOn = s.EnableToastNotifications;
            if (ToastOptionsPanel != null) ToastOptionsPanel.Visibility = s.EnableToastNotifications ? Visibility.Visible : Visibility.Collapsed;
            if (RainAlarmCheckBox != null) RainAlarmCheckBox.IsChecked = s.EnableRainAlarm;
            if (UvAlertCheckBox != null) UvAlertCheckBox.IsChecked = s.EnableUvAlert;
            if (MorningBriefingCheckBox != null) MorningBriefingCheckBox.IsChecked = s.EnableMorningBriefing;

            // Commute
            if (CommuteAlertToggle != null) CommuteAlertToggle.IsOn = s.EnableCommuteAlerts;
            if (CommuteOptionsPanel != null) CommuteOptionsPanel.Visibility = s.EnableCommuteAlerts ? Visibility.Visible : Visibility.Collapsed;
            SelectComboByTag(CommuteLeadTimeComboBox, s.MorningCommuteLeadMinutes.ToString());

            if (MorningCommuteTimePicker != null && TimeSpan.TryParse(s.MorningCommuteTime, out var mTime))
            {
                MorningCommuteTimePicker.Time = mTime;
            }
            if (EveningCommuteTimePicker != null && TimeSpan.TryParse(s.EveningCommuteTime, out var eTime))
            {
                EveningCommuteTimePicker.Time = eTime;
            }

            if (CommuteMonCheck != null) CommuteMonCheck.IsChecked = s.CommuteMon;
            if (CommuteTueCheck != null) CommuteTueCheck.IsChecked = s.CommuteTue;
            if (CommuteWedCheck != null) CommuteWedCheck.IsChecked = s.CommuteWed;
            if (CommuteThuCheck != null) CommuteThuCheck.IsChecked = s.CommuteThu;
            if (CommuteFriCheck != null) CommuteFriCheck.IsChecked = s.CommuteFri;
            if (CommuteSatCheck != null) CommuteSatCheck.IsChecked = s.CommuteSat;
            if (CommuteSunCheck != null) CommuteSunCheck.IsChecked = s.CommuteSun;

            // Performance & Effects
            if (StartupToggle != null) StartupToggle.IsOn = s.LaunchAtStartup;
            if (MinimizeToTrayToggle != null) MinimizeToTrayToggle.IsOn = s.MinimizeToTray;
            if (CloseToTrayToggle != null) CloseToTrayToggle.IsOn = s.CloseToTray;
            if (AppearanceEffectsToggle != null) AppearanceEffectsToggle.IsOn = s.EnableWeatherEffects;
            if (EffectsToggle != null) EffectsToggle.IsOn = s.EnableWeatherEffects;
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private void SelectComboByTag(ComboBox? cb, string? tagVal)
    {
        if (cb == null || tagVal == null) return;
        for (int i = 0; i < cb.Items.Count; i++)
        {
            if (cb.Items[i] is ComboBoxItem item && item.Tag?.ToString() == tagVal)
            {
                cb.SelectedIndex = i;
                break;
            }
        }
    }

    private void AutoSave()
    {
        if (_isSyncing || ViewModel == null) return;
        ViewModel.ApplySettings();
    }

    private void SettingsNavListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SettingsNavListView == null) return;
        string tag = (SettingsNavListView.SelectedItem as ListViewItem)?.Tag?.ToString() ?? "0";

        var panels = new[]
        {
            (PanelPersonalization, "0"),
            (PanelAppearance, "7"),
            (PanelTimeUnits, "1"),
            (PanelCityBg, "2"),
            (PanelWidget, "3"),
            (PanelNotifications, "4"),
            (PanelPerformance, "5"),
            (PanelAbout, "6")
        };

        foreach (var (panel, pTag) in panels)
        {
            if (panel != null)
            {
                panel.Visibility = (pTag == tag) ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }

    private void SettingsThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncing || ViewModel == null) return;
        if (SettingsThemeComboBox?.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            ViewModel.ChangeTheme(tag);
            _isSyncing = true;
            if (AppearanceThemeComboBox != null)
            {
                AppearanceThemeComboBox.SelectedIndex = SettingsThemeComboBox.SelectedIndex;
            }
            _isSyncing = false;
        }
    }

    private void AppearanceThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncing || ViewModel == null) return;
        if (AppearanceThemeComboBox?.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            ViewModel.ChangeTheme(tag);
            _isSyncing = true;
            if (SettingsThemeComboBox != null)
            {
                SettingsThemeComboBox.SelectedIndex = AppearanceThemeComboBox.SelectedIndex;
            }
            _isSyncing = false;
        }
    }

    private void CardIconPackFontAwesome_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        SelectIconPack("FontAwesome");
    }

    private void CardIconPackMeteocons_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        SelectIconPack("Meteocons");
    }

    private void CardIconPackFluent3D_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        SelectIconPack("Fluent3D");
    }

    private void SelectIconPack(string packTag)
    {
        if (ViewModel == null) return;
        ViewModel.Settings.SelectedIconPack = packTag;
        UpdateIconPackVisuals(packTag);
        AutoSave();
    }

    private void UpdateIconPackVisuals(string selectedTag)
    {
        var accentBrush = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
        var subtleBorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"];

        if (CardIconPackMeteocons != null)
        {
            CardIconPackMeteocons.BorderBrush = selectedTag == "Meteocons" ? accentBrush : subtleBorderBrush;
            CardIconPackMeteocons.BorderThickness = selectedTag == "Meteocons" ? new Thickness(2) : new Thickness(1);
            if (BadgeSelectedMeteocons != null) BadgeSelectedMeteocons.Visibility = selectedTag == "Meteocons" ? Visibility.Visible : Visibility.Collapsed;
        }
        if (CardIconPackFontAwesome != null)
        {
            CardIconPackFontAwesome.BorderBrush = selectedTag == "FontAwesome" ? accentBrush : subtleBorderBrush;
            CardIconPackFontAwesome.BorderThickness = selectedTag == "FontAwesome" ? new Thickness(2) : new Thickness(1);
            if (BadgeSelectedFontAwesome != null) BadgeSelectedFontAwesome.Visibility = selectedTag == "FontAwesome" ? Visibility.Visible : Visibility.Collapsed;
        }
        if (CardIconPackFluent3D != null)
        {
            CardIconPackFluent3D.BorderBrush = selectedTag == "Fluent3D" ? accentBrush : subtleBorderBrush;
            CardIconPackFluent3D.BorderThickness = selectedTag == "Fluent3D" ? new Thickness(2) : new Thickness(1);
            if (BadgeSelectedFluent3D != null) BadgeSelectedFluent3D.Visibility = selectedTag == "Fluent3D" ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void CityBgToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isSyncing || ViewModel == null) return;
        ViewModel.Settings.EnableCityBackground = CityBgToggle?.IsOn ?? false;
        if (CityBgControlsPanel != null)
        {
            CityBgControlsPanel.Visibility = (CityBgToggle?.IsOn == true) ? Visibility.Visible : Visibility.Collapsed;
        }
        ViewModel.UpdateCityBackground();
        UpdateCityPreview();
        AutoSave();
    }

    private void CityImageModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncing || ViewModel == null) return;
        if (CityImageModeComboBox?.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            ViewModel.Settings.CityBackgroundMode = tag;
            UpdateCityControlsVisibility();
            ViewModel.UpdateCityBackground();
            UpdateCityPreview();
            PopulatePresetGallery();
            AutoSave();
        }
    }

    private void PresetCityImageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncing || ViewModel == null) return;
        if (PresetCityImageComboBox?.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            ViewModel.Settings.SelectedCityImage = tag;
            ViewModel.UpdateCityBackground();
            UpdateCityPreview();
            PopulatePresetGallery();
            AutoSave();
        }
    }

    private void PopulatePresetCityImageComboBox()
    {
        if (PresetCityImageComboBox == null) return;

        PresetCityImageComboBox.Items.Clear();
        var presets = CityBackgroundHelper.GetAvailablePresets();
        foreach (var p in presets)
        {
            string name = Path.GetFileNameWithoutExtension(p);
            PresetCityImageComboBox.Items.Add(new ComboBoxItem
            {
                Content = $"🌆 {name}",
                Tag = p
            });
        }

        if (presets.Count > 0)
        {
            string current = ViewModel?.Settings.SelectedCityImage ?? "";
            int idx = presets.IndexOf(current);
            PresetCityImageComboBox.SelectedIndex = idx >= 0 ? idx : 0;
        }

        PopulatePresetGallery();
    }

    private void PopulatePresetGallery()
    {
        if (CityPresetItemsControl == null) return;

        CityPresetItemsControl.Items.Clear();
        var presets = CityBackgroundHelper.GetAvailablePresets();
        string current = ViewModel?.Settings.SelectedCityImage ?? "";

        foreach (var p in presets)
        {
            string name = Path.GetFileNameWithoutExtension(p);
            bool isSelected = (ViewModel?.Settings.CityBackgroundMode == "Preset" && string.Equals(current, p, StringComparison.OrdinalIgnoreCase));

            var btn = new Button
            {
                Padding = new Thickness(10, 6, 10, 6),
                CornerRadius = new CornerRadius(8),
                Tag = p,
                Background = isSelected ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(40, 56, 189, 248)) : null,
                BorderBrush = isSelected ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(180, 56, 189, 248)) : null,
                BorderThickness = new Thickness(isSelected ? 1.5 : 1)
            };

            var stack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            stack.Children.Add(new FontIcon
            {
                FontFamily = (Microsoft.UI.Xaml.Media.FontFamily)Application.Current.Resources["FontAwesomeSolid"],
                Glyph = isSelected ? "\uf058" : "\uf03e",
                FontSize = 12,
                Foreground = isSelected ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 56, 189, 248)) : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(180, 255, 255, 255))
            });
            stack.Children.Add(new TextBlock
            {
                Text = name,
                FontSize = 12,
                FontWeight = isSelected ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal
            });

            btn.Content = stack;
            btn.Click += (s, e) =>
            {
                if (ViewModel == null) return;
                ViewModel.Settings.CityBackgroundMode = "Preset";
                ViewModel.Settings.SelectedCityImage = p;
                SelectComboByTag(CityImageModeComboBox, "Preset");
                SelectComboByTag(PresetCityImageComboBox, p);
                UpdateCityControlsVisibility();
                ViewModel.UpdateCityBackground();
                UpdateCityPreview();
                PopulatePresetGallery();
                AutoSave();
            };

            CityPresetItemsControl.Items.Add(btn);
        }
    }

    private void UpdateCityPreview()
    {
        if (ViewModel == null) return;
        var s = ViewModel.Settings;

        if (!s.EnableCityBackground)
        {
            if (PreviewCityImageBrush != null) PreviewCityImageBrush.ImageSource = null;
            if (PreviewCityImageBorder != null) PreviewCityImageBorder.Visibility = Visibility.Collapsed;
            if (PreviewModeBadgeText != null) PreviewModeBadgeText.Text = "Đang Tắt";
            if (PreviewCitySubText != null) PreviewCitySubText.Text = "Hình nền mờ theo địa điểm đang TẮT";
            return;
        }

        if (PreviewCityImageBorder != null) PreviewCityImageBorder.Visibility = Visibility.Visible;

        string? imgPath = CityBackgroundHelper.ResolveImagePath(ViewModel.LocationTitle, s);
        if (!string.IsNullOrEmpty(imgPath) && File.Exists(imgPath))
        {
            if (PreviewCityImageBrush != null)
            {
                PreviewCityImageBrush.ImageSource = CityBackgroundHelper.LoadOptimizedBitmap(imgPath, 450);
            }

            string fileName = Path.GetFileName(imgPath);
            string cityName = Path.GetFileNameWithoutExtension(fileName);

            string modeLabel = s.CityBackgroundMode switch
            {
                "Preset" => $"Ảnh mẫu: {fileName}",
                "Custom" => $"Tùy chỉnh: {fileName}",
                _ => $"Tự động theo: {ViewModel.LocationTitle}"
            };

            if (PreviewModeBadgeText != null) PreviewModeBadgeText.Text = s.CityBackgroundMode switch
            {
                "Preset" => "Ảnh mẫu",
                "Custom" => "Tùy chỉnh",
                _ => "Tự động"
            };

            if (PreviewCityTitleText != null) PreviewCityTitleText.Text = s.CityBackgroundMode == "Auto" ? ViewModel.LocationTitle : cityName;
            if (PreviewCitySubText != null) PreviewCitySubText.Text = modeLabel;
        }
        else
        {
            if (PreviewCityImageBrush != null) PreviewCityImageBrush.ImageSource = null;
            if (PreviewCityImageBorder != null) PreviewCityImageBorder.Visibility = Visibility.Collapsed;
            if (PreviewCitySubText != null) PreviewCitySubText.Text = "Chưa có ảnh phù hợp";
        }

        if (CustomImageInfoPanel != null)
        {
            CustomImageInfoPanel.Visibility = (s.CityBackgroundMode == "Custom") ? Visibility.Visible : Visibility.Collapsed;
            if (CustomImagePathText != null)
            {
                CustomImagePathText.Text = string.IsNullOrEmpty(s.CustomCityImagePath) ? "Chưa chọn tệp ảnh nào từ máy tính" : s.CustomCityImagePath;
            }
        }
    }

    private void UpdateCityControlsVisibility()
    {
        string mode = ViewModel?.Settings.CityBackgroundMode ?? "Auto";
        if (PresetCityImageComboBox != null)
        {
            PresetCityImageComboBox.Visibility = mode == "Preset" ? Visibility.Visible : Visibility.Collapsed;
        }
        if (BrowseCustomImageButton != null)
        {
            BrowseCustomImageButton.Visibility = mode == "Custom" ? Visibility.Visible : Visibility.Collapsed;
        }
        if (CustomImageInfoPanel != null)
        {
            CustomImageInfoPanel.Visibility = mode == "Custom" ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private async void BrowseCustomImageButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        try
        {
            var openPicker = new Windows.Storage.Pickers.FileOpenPicker();
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);
            openPicker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            openPicker.FileTypeFilter.Add(".jpg");
            openPicker.FileTypeFilter.Add(".jpeg");
            openPicker.FileTypeFilter.Add(".png");
            openPicker.FileTypeFilter.Add(".webp");

            var file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                ViewModel.Settings.CustomCityImagePath = file.Path;
                ViewModel.Settings.CityBackgroundMode = "Custom";
                SelectComboByTag(CityImageModeComboBox, "Custom");
                UpdateCityControlsVisibility();
                ViewModel.UpdateCityBackground();
                UpdateCityPreview();
                PopulatePresetGallery();
                AutoSave();
            }
        }
        catch { }
    }

    private void WidgetStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncing || ViewModel == null) return;
        if (WidgetStyleComboBox?.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            ViewModel.Settings.WidgetStyle = tag;
            AutoSave();
        }
    }

    private void WidgetOpacitySlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_isSyncing || ViewModel == null) return;
        double val = e.NewValue;
        double normalizedOpacity = val > 1.0 ? val / 100.0 : val;
        normalizedOpacity = Math.Clamp(normalizedOpacity, 0.2, 1.0);
        ViewModel.Settings.WidgetOpacity = normalizedOpacity;
        if (WidgetOpacityValueText != null)
        {
            WidgetOpacityValueText.Text = $"{(int)(normalizedOpacity * 100)}%";
        }
        AutoSave();
    }

    private void DateFormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncing || ViewModel == null) return;
        if (DateFormatComboBox?.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            ViewModel.Settings.DateFormat = tag;
            AutoSave();
        }
    }

    private void FirstDayOfWeekComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncing || ViewModel == null) return;
        if (FirstDayOfWeekComboBox?.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            ViewModel.Settings.FirstDayOfWeek = tag;
            AutoSave();
        }
    }

    private void HourFormatToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isSyncing || ViewModel == null) return;
        ViewModel.Settings.Is24HourFormat = HourFormatToggle?.IsOn ?? true;
        AutoSave();
    }

    private void TempUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncing || ViewModel == null) return;
        if (TempUnitComboBox?.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            ViewModel.Settings.TemperatureUnit = tag;
            AutoSave();
        }
    }

    private void WindUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncing || ViewModel == null) return;
        if (WindUnitComboBox?.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            ViewModel.Settings.WindSpeedUnit = tag;
            AutoSave();
        }
    }

    private void PrecipUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncing || ViewModel == null) return;
        if (PrecipUnitComboBox?.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            ViewModel.Settings.PrecipitationUnit = tag;
            AutoSave();
        }
    }

    private void PressureUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncing || ViewModel == null) return;
        if (PressureUnitComboBox?.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            ViewModel.Settings.PressureUnit = tag;
            AutoSave();
        }
    }

    private void TimezoneComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncing || ViewModel == null) return;
        if (TimezoneComboBox?.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            ViewModel.Settings.TimezoneMode = tag;
            AutoSave();
        }
    }

    private void RefreshIntervalComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncing || ViewModel == null) return;
        if (RefreshIntervalComboBox?.SelectedItem is ComboBoxItem item && item.Tag is string tag && int.TryParse(tag, out int interval))
        {
            ViewModel.Settings.AutoRefreshIntervalMinutes = interval;
            AutoSave();
        }
    }

    private void StartupToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isSyncing || ViewModel == null) return;
        ViewModel.SetStartup(StartupToggle?.IsOn ?? false);
        AutoSave();
    }

    private void MinimizeToTrayToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isSyncing || ViewModel == null) return;
        ViewModel.Settings.MinimizeToTray = MinimizeToTrayToggle?.IsOn ?? false;
        AutoSave();
    }

    private void CloseToTrayToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isSyncing || ViewModel == null) return;
        ViewModel.Settings.CloseToTray = CloseToTrayToggle?.IsOn ?? false;
        AutoSave();
    }

    private void ToastNotificationToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isSyncing || ViewModel == null) return;
        bool isOn = ToastNotificationToggle?.IsOn ?? false;
        ViewModel.Settings.EnableToastNotifications = isOn;
        if (ToastOptionsPanel != null) ToastOptionsPanel.Visibility = isOn ? Visibility.Visible : Visibility.Collapsed;
        AutoSave();
    }

    private void ToastOptionCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (_isSyncing || ViewModel == null) return;
        ViewModel.Settings.EnableRainAlarm = RainAlarmCheckBox?.IsChecked ?? true;
        ViewModel.Settings.EnableUvAlert = UvAlertCheckBox?.IsChecked ?? true;
        ViewModel.Settings.EnableMorningBriefing = MorningBriefingCheckBox?.IsChecked ?? true;
        AutoSave();
    }

    private void CommuteAlertToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isSyncing || ViewModel == null) return;
        bool isOn = CommuteAlertToggle?.IsOn ?? false;
        ViewModel.Settings.EnableCommuteAlerts = isOn;
        if (CommuteOptionsPanel != null) CommuteOptionsPanel.Visibility = isOn ? Visibility.Visible : Visibility.Collapsed;
        AutoSave();
    }

    private void CommuteDays_Click(object sender, RoutedEventArgs e)
    {
        if (_isSyncing || ViewModel == null) return;
        ViewModel.Settings.CommuteMon = CommuteMonCheck?.IsChecked ?? true;
        ViewModel.Settings.CommuteTue = CommuteTueCheck?.IsChecked ?? true;
        ViewModel.Settings.CommuteWed = CommuteWedCheck?.IsChecked ?? true;
        ViewModel.Settings.CommuteThu = CommuteThuCheck?.IsChecked ?? true;
        ViewModel.Settings.CommuteFri = CommuteFriCheck?.IsChecked ?? true;
        ViewModel.Settings.CommuteSat = CommuteSatCheck?.IsChecked ?? false;
        ViewModel.Settings.CommuteSun = CommuteSunCheck?.IsChecked ?? false;
        AutoSave();
    }

    private void CommuteTime_Changed(TimePicker sender, TimePickerSelectedValueChangedEventArgs args)
    {
        if (_isSyncing || ViewModel == null) return;
        if (sender == MorningCommuteTimePicker && MorningCommuteTimePicker.SelectedTime.HasValue)
        {
            var t = MorningCommuteTimePicker.SelectedTime.Value;
            ViewModel.Settings.MorningCommuteTime = $"{t.Hours:D2}:{t.Minutes:D2}";
        }
        else if (sender == EveningCommuteTimePicker && EveningCommuteTimePicker.SelectedTime.HasValue)
        {
            var t = EveningCommuteTimePicker.SelectedTime.Value;
            ViewModel.Settings.EveningCommuteTime = $"{t.Hours:D2}:{t.Minutes:D2}";
        }
        AutoSave();
    }

    private void CommuteLeadTimeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncing || ViewModel == null) return;
        if (CommuteLeadTimeComboBox?.SelectedItem is ComboBoxItem item && item.Tag is string tag && int.TryParse(tag, out int mins))
        {
            ViewModel.Settings.MorningCommuteLeadMinutes = mins;
            ViewModel.Settings.EveningCommuteLeadMinutes = mins;
            AutoSave();
        }
    }

    private void TestCommuteAlertButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.TestCommuteNotificationCommand.Execute(null);
    }

    private void EffectsToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isSyncing || ViewModel == null) return;
        bool isOn = (sender as ToggleSwitch)?.IsOn ?? true;
        ViewModel.Settings.EnableWeatherEffects = isOn;
        _isSyncing = true;
        if (AppearanceEffectsToggle != null) AppearanceEffectsToggle.IsOn = isOn;
        if (EffectsToggle != null) EffectsToggle.IsOn = isOn;
        _isSyncing = false;
        AutoSave();
    }

    private void OpenChangelogFromSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        OpenChangelogRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ReopenOnboardingButton_Click(object sender, RoutedEventArgs e)
    {
        ReopenOnboardingRequested?.Invoke(this, EventArgs.Empty);
    }

    private void SaveSettingsTab_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (ViewModel != null && SettingsUserNameBox != null)
            {
                ViewModel.Settings.UserName = SettingsUserNameBox.Text?.Trim() ?? "";
            }
            if (ViewModel != null && FirstDayOfWeekComboBox?.SelectedItem is ComboBoxItem fItem && fItem.Tag is string fTag)
            {
                ViewModel.Settings.FirstDayOfWeek = fTag;
            }
            ViewModel?.ApplySettings();
        }
        catch { }
    }
}
