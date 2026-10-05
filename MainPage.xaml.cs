using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;
using WeatherApp.Helpers;
using WeatherApp.Models;
using WeatherApp.Services;
using WeatherApp.ViewModels;

namespace WeatherApp;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }

    private WeatherEffectRenderer? _weatherEffectRenderer;
    private Storyboard? _floatStoryboard;
    private bool _isSettingsSyncing = true;
    private bool _isDialogOpen;

    public MainPage()
    {
        _isSettingsSyncing = true;
        var httpClient = new HttpClient();
        var weatherService = new WeatherService(httpClient);
        var locationService = new LocationService(httpClient);
        var adviceService = new WeatherAdviceService();
        var settingsService = new SettingsService();

        ViewModel = new MainViewModel(weatherService, locationService, adviceService, settingsService);
        DataContext = ViewModel;

        InitializeComponent();

        SettingsDialog.Closing += (s, args) => AutoSaveSettings(true);

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // 1. Khởi tạo hiệu ứng thời tiết động nền (Mưa rơi, sấm sét, vầng nắng)
            _weatherEffectRenderer = new WeatherEffectRenderer(WeatherEffectsCanvas, LightningFlashOverlay);

            // 2. Kích hoạt chuyển động lơ lửng bồng bềnh cho icon thời tiết (Native Animation không tốn RAM)
            StartIconFloatingAnimation();

            // 3. Đồng bộ trạng thái điều khiển trên giao diện theo Settings
            SyncSettingsControls();

            // 4. Nạp dữ liệu thời tiết
            if (ViewModel != null)
            {
                // Chỉ lắng nghe HourlyForecastUpdated (được gọi duy nhất 1 lần sau khi danh sách nạp hoàn tất)
                // Tuyệt đối không lắng nghe CollectionChanged để tránh re-entrancy layout crash (0x800F1000)
                ViewModel.HourlyForecastUpdated += () =>
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        RenderHourlyTemperatureTrendline();
                        RenderSunArc();
                    });
                };

                if (SunArcCanvas != null)
                {
                    SunArcCanvas.SizeChanged += (s, e) => RenderSunArc();
                }

                await ViewModel.InitializeAsync();
                UpdateWeatherVisuals();
                RenderHourlyTemperatureTrendline();
                RenderSunArc();
                UpdateWidgetCardsVisuals(ViewModel.Settings.WidgetStyle);

                // 5. Khởi tạo khay hệ thống (System Tray Icon)
                if (App.MainWindow != null && ViewModel.TrayService == null)
                {
                    var trayService = new TrayIconService();
                    trayService.Initialize(App.MainWindow, ViewModel, () => DesktopWidgetButton_Click(this, new RoutedEventArgs()));
                    ViewModel.TrayService = trayService;
                }

                // 6. Tự động hiển thị Changelog nếu vừa cập nhật phiên bản mới
                CheckAndShowChangelog();
            }
        }
        catch { }
    }

    private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        double width = e.NewSize.Width;

        // 1. Tinh chỉnh Top Bar linh hoạt để thanh tìm kiếm luôn rộng rãi trên mọi độ phân giải
        if (width < 1220)
        {
            if (BtnTextWidget != null) BtnTextWidget.Visibility = Visibility.Collapsed;
        }
        else
        {
            if (BtnTextWidget != null) BtnTextWidget.Visibility = Visibility.Visible;
        }

        if (width < 1140)
        {
            if (AppSubtitleText != null) AppSubtitleText.Visibility = Visibility.Collapsed;
            if (ThemeComboBox != null) ThemeComboBox.Width = 92;
        }
        else
        {
            if (AppSubtitleText != null) AppSubtitleText.Visibility = Visibility.Visible;
            if (ThemeComboBox != null) ThemeComboBox.Width = 110;
        }

        // 2. Tinh chỉnh lề ngoài (Root Margin) gọn gàng trên màn hình nhỏ
        if (Content is Grid rootGrid)
        {
            rootGrid.Margin = width < 1250 ? new Thickness(14, 10, 14, 14) : new Thickness(24, 12, 24, 24);
        }
    }

    private void StartIconFloatingAnimation()
    {
        try
        {
            var animation = new DoubleAnimation
            {
                From = 0,
                To = -7,
                Duration = TimeSpan.FromSeconds(2.4),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };

            _floatStoryboard = new Storyboard();
            Storyboard.SetTarget(animation, WeatherIconFloatTransform);
            Storyboard.SetTargetProperty(animation, "Y");
            _floatStoryboard.Children.Add(animation);
            _floatStoryboard.Begin();
        }
        catch { }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.CurrentWeather) || e.PropertyName == nameof(ViewModel.HourlyForecast))
        {
            UpdateWeatherVisuals();
            RenderHourlyTemperatureTrendline();
            RenderSunArc();
        }
    }

    private void UpdateWeatherVisuals()
    {
        if (ViewModel?.CurrentWeather == null) return;

        // Cập nhật hiệu ứng thời tiết nền nếu người dùng bật
        if (ViewModel.Settings.EnableWeatherEffects)
        {
            _weatherEffectRenderer?.SetWeatherEffect(ViewModel.CurrentWeather.WeatherEffect);
            _weatherEffectRenderer?.Resume();
        }
        else
        {
            _weatherEffectRenderer?.Pause();
            WeatherEffectsCanvas.Children.Clear();
        }

        // Cập nhật hình nền thành phố nếu được bật (dùng ImageBrush để không phình kích thước thẻ)
        if (ViewModel.Settings.EnableCityBackground && !string.IsNullOrEmpty(ViewModel.CurrentWeather.CityImagePath))
        {
            if (CityHeroImageBrush != null)
            {
                CityHeroImageBrush.ImageSource = CityBackgroundHelper.LoadOptimizedBitmap(ViewModel.CurrentWeather.CityImagePath);
            }
            if (CityHeroBackgroundBorder != null)
            {
                CityHeroBackgroundBorder.Visibility = Visibility.Visible;
            }
        }
        else
        {
            if (CityHeroImageBrush != null)
            {
                CityHeroImageBrush.ImageSource = null;
            }
            if (CityHeroBackgroundBorder != null)
            {
                CityHeroBackgroundBorder.Visibility = Visibility.Collapsed;
            }
        }

        // Cập nhật icon SVG vector trong suốt tự nhiên, sắc nét
        UpdateWeatherIcon();
    }

    private void UpdateWeatherIcon()
    {
        if (ViewModel?.CurrentWeather == null) return;

        try
        {
            string? iconPath = ViewModel.CurrentWeather.SvgIconPath;
            if (string.IsNullOrEmpty(iconPath))
            {
                iconPath = ViewModel.CurrentWeather.SvgIconFullPath;
            }

            if (!string.IsNullOrEmpty(iconPath))
            {
                var uri = new Uri(iconPath);
                var svgSource = new SvgImageSource(uri)
                {
                    RasterizePixelWidth = 240,
                    RasterizePixelHeight = 240
                };

                svgSource.OpenFailed += (s, e) =>
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        WeatherSvgImage.Visibility = Visibility.Collapsed;
                        FallbackWeatherIcon.Visibility = Visibility.Visible;
                    });
                };

                WeatherSvgImage.Source = svgSource;
                WeatherSvgImage.Visibility = Visibility.Visible;
                FallbackWeatherIcon.Visibility = Visibility.Collapsed;
            }
            else
            {
                WeatherSvgImage.Visibility = Visibility.Collapsed;
                FallbackWeatherIcon.Visibility = Visibility.Visible;
            }
        }
        catch
        {
            WeatherSvgImage.Visibility = Visibility.Collapsed;
            FallbackWeatherIcon.Visibility = Visibility.Visible;
        }

        UpdateOccasionButtonsVisual();
    }

    private void ScrollToAdvice_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ViewModel?.ToggleAdviceExpandedCommand.Execute(null);
        }
        catch { }
    }


    #region Settings Dialog Handlers

    private void AutoSaveSettings(bool updateVisuals = false)
    {
        if (_isSettingsSyncing || ViewModel == null) return;
        try
        {
            if (SettingsUserNameBox != null && !string.IsNullOrWhiteSpace(SettingsUserNameBox.Text))
            {
                ViewModel.Settings.UserName = SettingsUserNameBox.Text.Trim();
            }
            if (FirstDayOfWeekComboBox?.SelectedItem is ComboBoxItem fItem && fItem.Tag is string fTag)
            {
                ViewModel.Settings.FirstDayOfWeek = fTag;
            }
            ViewModel.ApplySettings();
            if (updateVisuals)
            {
                UpdateWeatherVisuals();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AutoSaveSettings] Error: {ex.Message}");
        }
    }

    private void SyncSettingsControls()
    {
        _isSettingsSyncing = true;
        try
        {
            // Múi giờ
            for (int i = 0; i < TimezoneComboBox.Items.Count; i++)
            {
                if (TimezoneComboBox.Items[i] is ComboBoxItem item && item.Tag?.ToString() == ViewModel.Settings.TimezoneMode)
                {
                    TimezoneComboBox.SelectedIndex = i;
                    break;
                }
            }

            // Định dạng ngày
            for (int i = 0; i < DateFormatComboBox.Items.Count; i++)
            {
                if (DateFormatComboBox.Items[i] is ComboBoxItem item && item.Tag?.ToString() == ViewModel.Settings.DateFormat)
                {
                    DateFormatComboBox.SelectedIndex = i;
                    break;
                }
            }

            // Ngày bắt đầu tuần
            if (FirstDayOfWeekComboBox != null)
            {
                FirstDayOfWeekComboBox.SelectedIndex = ViewModel.Settings.FirstDayOfWeek == "Sunday" ? 1 : 0;
            }

            // Đơn vị nhiệt độ
            TempUnitComboBox.SelectedIndex = ViewModel.Settings.TemperatureUnit == "F" ? 1 : 0;

            // Đơn vị gió
            WindUnitComboBox.SelectedIndex = ViewModel.Settings.WindSpeedUnit switch
            {
                "ms" => 1,
                "mph" => 2,
                _ => 0
            };

            // Đơn vị áp suất
            PressureUnitComboBox.SelectedIndex = ViewModel.Settings.PressureUnit == "mmHg" ? 1 : 0;

            // Đơn vị mưa
            PrecipUnitComboBox.SelectedIndex = ViewModel.Settings.PrecipitationUnit == "inch" ? 1 : 0;

            // Theme trong settings
            SettingsThemeComboBox.SelectedIndex = ViewModel.Settings.ThemeMode switch
            {
                "Light" => 1,
                "Dark" => 2,
                _ => 0
            };

            // Khoảng thời gian làm mới
            for (int i = 0; i < RefreshIntervalComboBox.Items.Count; i++)
            {
                if (RefreshIntervalComboBox.Items[i] is ComboBoxItem item && item.Tag?.ToString() == ViewModel.Settings.AutoRefreshIntervalMinutes.ToString())
                {
                    RefreshIntervalComboBox.SelectedIndex = i;
                    break;
                }
            }

            // ThemeComboBox trên thanh điều khiển
            ThemeComboBox.SelectedIndex = ViewModel.Settings.ThemeMode switch
            {
                "Light" => 0,
                "Dark" => 1,
                _ => 2
            };

            // Tên người dùng
            SettingsUserNameBox.Text = ViewModel.Settings.UserName;

            // Hình nền thành phố
            CityBgToggle.IsOn = ViewModel.Settings.EnableCityBackground;
            CityBgControlsPanel.Visibility = ViewModel.Settings.EnableCityBackground ? Visibility.Visible : Visibility.Collapsed;
            CityImageModeComboBox.SelectedIndex = ViewModel.Settings.CityBackgroundMode switch
            {
                "Preset" => 1,
                "Custom" => 2,
                _ => 0
            };

            // Danh sách ảnh mẫu
            PresetCityImageComboBox.Items.Clear();
            var presets = CityBackgroundHelper.GetAvailablePresets();
            foreach (var p in presets)
            {
                PresetCityImageComboBox.Items.Add(new ComboBoxItem { Content = Path.GetFileNameWithoutExtension(p), Tag = p });
            }
            if (presets.Count > 0)
            {
                int idx = presets.IndexOf(ViewModel.Settings.SelectedCityImage);
                PresetCityImageComboBox.SelectedIndex = idx >= 0 ? idx : 0;
            }
            UpdateCityControlsVisibility();

            // Widget
            WidgetStyleComboBox.SelectedIndex = ViewModel.Settings.WidgetStyle switch
            {
                "BryanCDynamic" => 0,
                "GlassCard" => 1,
                "Compact" => 2,
                "MiniIsland" => 3,
                _ => 0
            };
            WidgetOpacitySlider.Value = Math.Round(ViewModel.Settings.WidgetOpacity * 100);
            if (WidgetOpacityValueText != null) WidgetOpacityValueText.Text = $"{(int)WidgetOpacitySlider.Value}%";

            // Giao diện & Icon Pack
            if (AppearanceThemeComboBox != null)
            {
                AppearanceThemeComboBox.SelectedIndex = ViewModel.Settings.ThemeMode switch
                {
                    "Light" => 1,
                    "Dark" => 2,
                    _ => 0
                };
            }
            UpdateIconPackCardVisuals();

            // Thông báo thông minh
            ToastNotificationToggle.IsOn = ViewModel.Settings.EnableToastNotifications;
            ToastOptionsPanel.Visibility = ViewModel.Settings.EnableToastNotifications ? Visibility.Visible : Visibility.Collapsed;
            RainAlarmCheckBox.IsChecked = ViewModel.Settings.EnableRainAlarm;
            UvAlertCheckBox.IsChecked = ViewModel.Settings.EnableUvAlert;
            MorningBriefingCheckBox.IsChecked = ViewModel.Settings.EnableMorningBriefing;

            // Nhắc nhở thời tiết đi làm & tan ca (v2.0)
            if (CommuteAlertToggle != null) CommuteAlertToggle.IsOn = ViewModel.Settings.EnableCommuteAlerts;
            if (CommuteOptionsPanel != null) CommuteOptionsPanel.Visibility = ViewModel.Settings.EnableCommuteAlerts ? Visibility.Visible : Visibility.Collapsed;
            if (MorningCommuteTimePicker != null && TimeSpan.TryParse(ViewModel.Settings.MorningCommuteTime, out var mTs))
            {
                MorningCommuteTimePicker.SelectedTime = mTs;
            }
            if (EveningCommuteTimePicker != null && TimeSpan.TryParse(ViewModel.Settings.EveningCommuteTime, out var eTs))
            {
                EveningCommuteTimePicker.SelectedTime = eTs;
            }
            if (CommuteLeadTimeComboBox != null)
            {
                for (int i = 0; i < CommuteLeadTimeComboBox.Items.Count; i++)
                {
                    if (CommuteLeadTimeComboBox.Items[i] is ComboBoxItem item && item.Tag?.ToString() == ViewModel.Settings.MorningCommuteLeadMinutes.ToString())
                    {
                        CommuteLeadTimeComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
            if (CommuteMonCheck != null) CommuteMonCheck.IsChecked = ViewModel.Settings.CommuteMon;
            if (CommuteTueCheck != null) CommuteTueCheck.IsChecked = ViewModel.Settings.CommuteTue;
            if (CommuteWedCheck != null) CommuteWedCheck.IsChecked = ViewModel.Settings.CommuteWed;
            if (CommuteThuCheck != null) CommuteThuCheck.IsChecked = ViewModel.Settings.CommuteThu;
            if (CommuteFriCheck != null) CommuteFriCheck.IsChecked = ViewModel.Settings.CommuteFri;
            if (CommuteSatCheck != null) CommuteSatCheck.IsChecked = ViewModel.Settings.CommuteSat;
            if (CommuteSunCheck != null) CommuteSunCheck.IsChecked = ViewModel.Settings.CommuteSun;

            // Khay hệ thống
            MinimizeToTrayToggle.IsOn = ViewModel.Settings.MinimizeToTray;
            CloseToTrayToggle.IsOn = ViewModel.Settings.CloseToTray;
        }
        finally
        {
            _isSettingsSyncing = false;
        }
    }

    private async void CheckAndPromptUserName()
    {
        if (ViewModel == null || !string.IsNullOrWhiteSpace(ViewModel.Settings.UserName)) return;

        try
        {
            var inputTextBox = new TextBox
            {
                PlaceholderText = "Nhập tên của bạn (Ví dụ: Tuấn, Linh, Alex...)",
                Height = 38,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var stack = new StackPanel { Spacing = 8 };
            stack.Children.Add(new TextBlock 
            { 
                Text = "Chào mừng bạn đến với Thời Tiết WinUI v1.1!\nHãy cho chúng tôi biết tên bạn để ứng dụng xưng hô thân mật hơn nhé 😊", 
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13
            });
            stack.Children.Add(inputTextBox);

            var dialog = new ContentDialog
            {
                Title = "👋 Chào Bạn Mới!",
                Content = stack,
                PrimaryButtonText = "Lưu tên",
                CloseButtonText = "Để sau",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(inputTextBox.Text))
            {
                ViewModel.Settings.UserName = inputTextBox.Text.Trim();
                ViewModel.ApplySettings();
            }
        }
        catch { }
    }

    private void CityBgToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isSettingsSyncing || ViewModel == null) return;
        ViewModel.Settings.EnableCityBackground = CityBgToggle.IsOn;
        CityBgControlsPanel.Visibility = CityBgToggle.IsOn ? Visibility.Visible : Visibility.Collapsed;
        ViewModel.UpdateCityBackground();
        UpdateWeatherVisuals();
        AutoSaveSettings(true);
    }

    private void CityImageModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSettingsSyncing || ViewModel == null) return;
        if (CityImageModeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            ViewModel.Settings.CityBackgroundMode = tag;
            UpdateCityControlsVisibility();
            ViewModel.UpdateCityBackground();
            UpdateWeatherVisuals();
            AutoSaveSettings(true);
        }
    }

    private void PresetCityImageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSettingsSyncing || ViewModel == null) return;
        if (PresetCityImageComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            ViewModel.Settings.SelectedCityImage = tag;
            ViewModel.UpdateCityBackground();
            UpdateWeatherVisuals();
            AutoSaveSettings(true);
        }
    }

    private void UpdateCityControlsVisibility()
    {
        string mode = ViewModel?.Settings.CityBackgroundMode ?? "Auto";
        PresetCityImageComboBox.Visibility = mode == "Preset" ? Visibility.Visible : Visibility.Collapsed;
        BrowseCustomImageButton.Visibility = mode == "Custom" ? Visibility.Visible : Visibility.Collapsed;
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
                ViewModel.UpdateCityBackground();
                UpdateWeatherVisuals();
                AutoSaveSettings(true);
            }
        }
        catch { }
    }

    private void WidgetStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSettingsSyncing || ViewModel == null) return;
        if (WidgetStyleComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            ViewModel.Settings.WidgetStyle = tag;
            _widgetWindow?.ApplyWidgetStyle(tag);
            foreach (var w in WidgetWindow.ActiveWidgets)
            {
                try { w.ApplyWidgetStyle(tag); } catch { }
            }
            AutoSaveSettings();
        }
    }

    private void WidgetOpacitySlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_isSettingsSyncing || ViewModel == null) return;
        double val = e.NewValue;
        ViewModel.Settings.WidgetOpacity = val / 100.0;
        if (WidgetOpacityValueText != null)
        {
            WidgetOpacityValueText.Text = $"{(int)val}%";
        }
        _widgetWindow?.SetOpacity(ViewModel.Settings.WidgetOpacity);
        foreach (var w in WidgetWindow.ActiveWidgets)
        {
            try { w.SetOpacity(ViewModel.Settings.WidgetOpacity); } catch { }
        }
        AutoSaveSettings();
    }

    private void ToastNotificationToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isSettingsSyncing || ViewModel == null) return;
        ViewModel.Settings.EnableToastNotifications = ToastNotificationToggle.IsOn;
        ToastOptionsPanel.Visibility = ToastNotificationToggle.IsOn ? Visibility.Visible : Visibility.Collapsed;
        AutoSaveSettings();
    }

    private void ToastOptionCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (_isSettingsSyncing || ViewModel == null) return;
        ViewModel.Settings.EnableRainAlarm = RainAlarmCheckBox.IsChecked == true;
        ViewModel.Settings.EnableUvAlert = UvAlertCheckBox.IsChecked == true;
        ViewModel.Settings.EnableMorningBriefing = MorningBriefingCheckBox.IsChecked == true;
        AutoSaveSettings();
    }

    private void CommuteAlertToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isSettingsSyncing || ViewModel == null) return;
        ViewModel.Settings.EnableCommuteAlerts = CommuteAlertToggle.IsOn;
        if (CommuteOptionsPanel != null)
        {
            CommuteOptionsPanel.Visibility = CommuteAlertToggle.IsOn ? Visibility.Visible : Visibility.Collapsed;
        }
        AutoSaveSettings();
    }

    private void CommuteTime_Changed(TimePicker sender, TimePickerSelectedValueChangedEventArgs args)
    {
        if (_isSettingsSyncing || ViewModel == null) return;
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
        AutoSaveSettings();
    }

    private void CommuteLeadTimeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSettingsSyncing || ViewModel == null) return;
        if (CommuteLeadTimeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag && int.TryParse(tag, out int mins))
        {
            ViewModel.Settings.MorningCommuteLeadMinutes = mins;
            ViewModel.Settings.EveningCommuteLeadMinutes = mins;
            AutoSaveSettings();
        }
    }

    private void CommuteDays_Click(object sender, RoutedEventArgs e)
    {
        if (_isSettingsSyncing || ViewModel == null) return;
        ViewModel.Settings.CommuteMon = CommuteMonCheck?.IsChecked ?? true;
        ViewModel.Settings.CommuteTue = CommuteTueCheck?.IsChecked ?? true;
        ViewModel.Settings.CommuteWed = CommuteWedCheck?.IsChecked ?? true;
        ViewModel.Settings.CommuteThu = CommuteThuCheck?.IsChecked ?? true;
        ViewModel.Settings.CommuteFri = CommuteFriCheck?.IsChecked ?? true;
        ViewModel.Settings.CommuteSat = CommuteSatCheck?.IsChecked ?? false;
        ViewModel.Settings.CommuteSun = CommuteSunCheck?.IsChecked ?? false;
        AutoSaveSettings();
    }

    private void TestCommuteAlertButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.TestCommuteNotificationCommand.Execute(null);
    }

    private void MinimizeToTrayToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isSettingsSyncing || ViewModel == null) return;
        ViewModel.Settings.MinimizeToTray = MinimizeToTrayToggle.IsOn;
        AutoSaveSettings();
    }

    private void CloseToTrayToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isSettingsSyncing || ViewModel == null) return;
        ViewModel.Settings.CloseToTray = CloseToTrayToggle.IsOn;
        AutoSaveSettings();
    }

    private WidgetWindow? _widgetWindow;

    private void DesktopWidgetButton_Click(object sender, RoutedEventArgs e)
    {
        OpenCurrentCityWidget_Click(sender, e);
    }

    private void OpenCurrentCityWidget_Click(object sender, RoutedEventArgs e)
    {
        if (_widgetWindow == null)
        {
            _widgetWindow = new WidgetWindow(ViewModel);
            _widgetWindow.Closed += (s, args) => _widgetWindow = null;
            _widgetWindow.Activate();
        }
        else
        {
            _widgetWindow.Activate();
        }
    }

    private async void OpenCustomCityWidget_Click(object sender, RoutedEventArgs e)
    {
        var inputTextBox = new TextBox
        {
            PlaceholderText = "Nhập tên thành phố (VD: Đà Nẵng, Tokyo, Paris, New York...)",
            Height = 38
        };
        var dialog = new ContentDialog
        {
            Title = "➕ Tạo Widget Mới Cho Thành Phố Khác",
            Content = inputTextBox,
            PrimaryButtonText = "Tạo Widget",
            CloseButtonText = "Hủy",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(inputTextBox.Text))
        {
            string city = inputTextBox.Text.Trim();
            try
            {
                var locService = new LocationService();
                var locs = await locService.SearchLocationsAsync(city);
                if (locs != null && locs.Count > 0)
                {
                    var first = locs[0];
                    var widget = new WidgetWindow(ViewModel, $"{first.Name}, {first.Country}", first.Latitude, first.Longitude);
                    widget.Activate();
                }
                else
                {
                    var widget = new WidgetWindow(ViewModel, city, 21.0285, 105.8542);
                    widget.Activate();
                }
            }
            catch
            {
                var widget = new WidgetWindow(ViewModel, city, 21.0285, 105.8542);
                widget.Activate();
            }
        }
    }

    private void CloseAllWidgets_Click(object sender, RoutedEventArgs e)
    {
        WidgetWindow.CloseAllWidgets();
        _widgetWindow = null;
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;

        ViewModel.UpdateRamUsage();
        SyncSettingsControls();
        ViewModel.SwitchNav("settings");
        if (MainNavView != null)
        {
            MainNavView.SelectedItem = MainNavView.FooterMenuItems.OfType<NavigationViewItem>().FirstOrDefault(i => i.Tag?.ToString() == "settings");
        }
    }

    private void SettingsDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
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
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Settings] Error saving username: {ex.Message}");
        }

        // Đợi dialog hoàn tất quá trình đóng trước khi cập nhật toàn diện và vẽ lại UI
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
        {
            try
            {
                if (ViewModel != null)
                {
                    ViewModel.ApplySettings();
                }
                UpdateWeatherVisuals();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] Error applying settings post-dialog: {ex.Message}");
            }
        });
    }

    private void SettingsNavListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SettingsNavListView == null) return;
        string tag = (SettingsNavListView.SelectedItem as ListViewItem)?.Tag?.ToString() ?? "0";

        if (PanelPersonalization != null) PanelPersonalization.Visibility = tag == "0" ? Visibility.Visible : Visibility.Collapsed;
        if (PanelAppearance != null) PanelAppearance.Visibility = tag == "7" ? Visibility.Visible : Visibility.Collapsed;
        if (PanelTimeUnits != null) PanelTimeUnits.Visibility = tag == "1" ? Visibility.Visible : Visibility.Collapsed;
        if (PanelCityBg != null) PanelCityBg.Visibility = tag == "2" ? Visibility.Visible : Visibility.Collapsed;
        if (PanelWidget != null) PanelWidget.Visibility = tag == "3" ? Visibility.Visible : Visibility.Collapsed;
        if (PanelNotifications != null) PanelNotifications.Visibility = tag == "4" ? Visibility.Visible : Visibility.Collapsed;
        if (PanelPerformance != null) PanelPerformance.Visibility = tag == "5" ? Visibility.Visible : Visibility.Collapsed;
        if (PanelAbout != null) PanelAbout.Visibility = tag == "6" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void CardIconPackMeteocons_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (ViewModel == null) return;
        ViewModel.UpdateIconPack("Meteocons");
        UpdateIconPackCardVisuals();
        UpdateWeatherIcon();
    }

    private void CardIconPackFluent3D_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (ViewModel == null) return;
        ViewModel.UpdateIconPack("Fluent3D");
        UpdateIconPackCardVisuals();
        UpdateWeatherIcon();
    }

    private void CardIconPackFontAwesome_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (ViewModel == null) return;
        ViewModel.UpdateIconPack("FontAwesome");
        UpdateIconPackCardVisuals();
        UpdateWeatherIcon();
    }

    private void AppearanceThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSettingsSyncing || ViewModel == null) return;
        if (AppearanceThemeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            ViewModel.ChangeTheme(tag);
            AutoSaveSettings(true);

            _isSettingsSyncing = true;
            SettingsThemeComboBox.SelectedIndex = AppearanceThemeComboBox.SelectedIndex;
            ThemeComboBox.SelectedIndex = tag switch { "light" => 0, "dark" => 1, _ => 2 };
            _isSettingsSyncing = false;
        }
    }

    private void UpdateIconPackCardVisuals()
    {
        if (ViewModel == null || CardIconPackMeteocons == null) return;

        string pack = ViewModel.Settings.SelectedIconPack ?? "Meteocons";
        var accentBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
        var strokeBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"];

        // Meteocons
        bool isMeteo = pack == "Meteocons";
        CardIconPackMeteocons.BorderBrush = isMeteo ? accentBrush : strokeBrush;
        CardIconPackMeteocons.BorderThickness = new Thickness(isMeteo ? 2 : 1);
        if (BadgeSelectedMeteocons != null) BadgeSelectedMeteocons.Visibility = isMeteo ? Visibility.Visible : Visibility.Collapsed;

        // Fluent3D
        bool isFluent = pack == "Fluent3D";
        CardIconPackFluent3D.BorderBrush = isFluent ? accentBrush : strokeBrush;
        CardIconPackFluent3D.BorderThickness = new Thickness(isFluent ? 2 : 1);
        if (BadgeSelectedFluent3D != null) BadgeSelectedFluent3D.Visibility = isFluent ? Visibility.Visible : Visibility.Collapsed;

        // FontAwesome
        bool isFA = pack == "FontAwesome";
        CardIconPackFontAwesome.BorderBrush = isFA ? accentBrush : strokeBrush;
        CardIconPackFontAwesome.BorderThickness = new Thickness(isFA ? 2 : 1);
        if (BadgeSelectedFontAwesome != null) BadgeSelectedFontAwesome.Visibility = isFA ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void OpenChangelogFromSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        SettingsDialog.Hide();
        await System.Threading.Tasks.Task.Delay(250);
        if (this.XamlRoot != null && !_isDialogOpen)
        {
            _isDialogOpen = true;
            try
            {
                ChangelogDialog.XamlRoot = this.XamlRoot;
                await ChangelogDialog.ShowAsync();
            }
            catch { }
            finally
            {
                _isDialogOpen = false;
            }
        }
    }

    private void ChangelogVersionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ChangelogVersionComboBox == null) return;
        if (ChangelogVersionComboBox.SelectedItem is ComboBoxItem item && item.Tag is string ver)
        {
            if (ChangelogContent_v223 != null) ChangelogContent_v223.Visibility = ver == "2.2.3" ? Visibility.Visible : Visibility.Collapsed;
            if (ChangelogContent_v221 != null) ChangelogContent_v221.Visibility = ver == "2.2.1" ? Visibility.Visible : Visibility.Collapsed;
            if (ChangelogContent_v22 != null) ChangelogContent_v22.Visibility = ver == "2.2" ? Visibility.Visible : Visibility.Collapsed;
            if (ChangelogContent_v21 != null) ChangelogContent_v21.Visibility = ver == "2.1" ? Visibility.Visible : Visibility.Collapsed;
            if (ChangelogContent_v20 != null) ChangelogContent_v20.Visibility = ver == "2.0" ? Visibility.Visible : Visibility.Collapsed;
            if (ChangelogContent_v19 != null) ChangelogContent_v19.Visibility = ver == "1.9" ? Visibility.Visible : Visibility.Collapsed;
            if (ChangelogContent_v18 != null) ChangelogContent_v18.Visibility = ver == "1.8" ? Visibility.Visible : Visibility.Collapsed;
            if (ChangelogContent_v17 != null) ChangelogContent_v17.Visibility = ver == "1.7" ? Visibility.Visible : Visibility.Collapsed;
            if (ChangelogContent_v16 != null) ChangelogContent_v16.Visibility = ver == "1.6" ? Visibility.Visible : Visibility.Collapsed;
            if (ChangelogContent_v15 != null) ChangelogContent_v15.Visibility = ver == "1.5" ? Visibility.Visible : Visibility.Collapsed;
            if (ChangelogContent_v14 != null) ChangelogContent_v14.Visibility = ver == "1.4" ? Visibility.Visible : Visibility.Collapsed;
            if (ChangelogContent_v13 != null) ChangelogContent_v13.Visibility = ver == "1.3" ? Visibility.Visible : Visibility.Collapsed;
            if (ChangelogContent_v12 != null) ChangelogContent_v12.Visibility = ver == "1.2" ? Visibility.Visible : Visibility.Collapsed;
            if (ChangelogContent_v11 != null) ChangelogContent_v11.Visibility = ver == "1.1" ? Visibility.Visible : Visibility.Collapsed;
            if (ChangelogContent_v10 != null) ChangelogContent_v10.Visibility = ver == "1.0" ? Visibility.Visible : Visibility.Collapsed;

            if (ChangelogHeaderTitle != null && ChangelogHeaderBadge != null && ChangelogHeaderSubtitle != null)
            {
                switch (ver)
                {
                    case "2.2.3":
                        ChangelogHeaderTitle.Text = "Có gì mới trong Weather WinUI 2.2.3?";
                        ChangelogHeaderBadge.Text = "v2.2.3 TÍNH NĂNG MỚI & SỬA LỖI";
                        ChangelogHeaderSubtitle.Text = "Khắc phục triệt để lỗi không lưu cài đặt và lỗi Widget Opacity bị reset về 20%, ra mắt tính năng Cảnh báo ngập úng triều cường đô thị tại TP.HCM & Hà Nội với dự báo đỉnh triều, lưu vực sông, các tuyến đường ngập trọng điểm và lời khuyên lưu thông an toàn.";
                        break;
                    case "2.2.1":
                        ChangelogHeaderTitle.Text = "Có gì mới trong Weather WinUI 2.2.1?";
                        ChangelogHeaderBadge.Text = "v2.2.1 SỬA LỖI & TỐI ƯU";
                        ChangelogHeaderSubtitle.Text = "Khắc phục triệt để hiển thị icon FontAwesome trên Windows 10, nâng cấp tìm kiếm địa phương 63 tỉnh thành Việt Nam, thiết kế Responsive thích ứng hoàn hảo các màn hình < FullHD (1366x768 & 1280x720) và tối ưu bộ cài chống báo nhầm bởi phần mềm diệt virus (Avast).";
                        break;
                    case "2.2":
                        ChangelogHeaderTitle.Text = "Có gì mới trong Weather WinUI 2.2?";
                        ChangelogHeaderBadge.Text = "v2.2.0 CHÍNH THỨC";
                        ChangelogHeaderSubtitle.Text = "Widget Dynamic phong cách BryanC Dribbble đổi màu và hiệu ứng khí quyển theo thời tiết, Tích hợp bộ biểu tượng động Meteocons Animated siêu đẹp, Triệt tiêu hoàn toàn viền trắng pop-up khay hệ thống (System Tray) và Mục Cài đặt Giao diện trực quan.";
                        break;
                    case "2.1":
                        ChangelogHeaderTitle.Text = "Có gì mới trong Weather WinUI 2.1?";
                        ChangelogHeaderBadge.Text = "v2.1.0 CHÍNH THỨC";
                        ChangelogHeaderSubtitle.Text = "Gợi ý trang phục thông minh (OOTD Hôm nay mặc gì), Chia sẻ thẻ thời tiết Full HD 1080p siêu nét, triệt tiêu hoàn toàn viền trắng widget và tương thích tuyệt đối Windows 10 & 11.";
                        break;
                    case "2.0":
                        ChangelogHeaderTitle.Text = "Có gì mới trong Weather WinUI 2.0?";
                        ChangelogHeaderBadge.Text = "v2.0.0 CHÍNH THỨC";
                        ChangelogHeaderSubtitle.Text = "Nhắc nhở sự kiện có giờ cụ thể, Mục tiêu cá nhân trong ngày (Daily Goals), Cảnh báo thời tiết giờ đi làm & tan ca, Đa Widget Desktop độc lập cho nhiều thành phố, triệt tiêu viền trắng và bảo mật cấp cao.";
                        break;
                    case "1.9":
                        ChangelogHeaderTitle.Text = "Có gì mới trong Weather WinUI 1.9?";
                        ChangelogHeaderBadge.Text = "v1.9.0 CHÍNH THỨC";
                        ChangelogHeaderSubtitle.Text = "Ghi chú & Lịch trình thông minh (tự cảnh báo xung đột thời tiết xấu), Chỉ số Sốc nhiệt & Biên độ ngày đêm, và Widget Mini Island dạng con nhộng nổi trên màn hình.";
                        break;
                    case "1.8":
                        ChangelogHeaderTitle.Text = "Có gì mới trong Weather WinUI 1.8?";
                        ChangelogHeaderBadge.Text = "v1.8.0 CHÍNH THỨC";
                        ChangelogHeaderSubtitle.Text = "Lịch Tháng Đa Niên (1950-2100+) tích hợp Âm Dương Lịch, Dự báo thời tiết 7 ngày với hiệu ứng Nắng/Mưa động, Tra cứu Ngày lễ Việt Nam, Pop-up xem chi tiết ngày và Tùy chọn ngày bắt đầu tuần.";
                        break;
                    case "1.7":
                        ChangelogHeaderTitle.Text = "Có gì mới trong Weather WinUI 1.7?";
                        ChangelogHeaderBadge.Text = "v1.7.0 CHÍNH THỨC";
                        ChangelogHeaderSubtitle.Text = "Báo cáo Chất lượng không khí & Bụi mịn chuyên sâu (AQI, PM2.5, PM10), Vòng cung Mặt Trời/Mặt Trăng, Lịch Âm & 24 Tiết Khí, Thanh đo nhiệt độ tuần, Thành phố yêu thích và Âm thanh thiên nhiên thư giãn.";
                        break;
                    case "1.6":
                        ChangelogHeaderTitle.Text = "Có gì mới trong Weather WinUI 1.6?";
                        ChangelogHeaderBadge.Text = "v1.6.0 CHÍNH THỨC";
                        ChangelogHeaderSubtitle.Text = "Tương thích 100% Windows 10 & 11, khắc phục triệt để lỗi ô vuông font icon, sửa dứt điểm crash khởi động và tối ưu hiệu năng.";
                        break;
                    case "1.5":
                        ChangelogHeaderTitle.Text = "Có gì mới trong Weather WinUI 1.5?";
                        ChangelogHeaderBadge.Text = "v1.5.0";
                        ChangelogHeaderSubtitle.Text = "Tối ưu khung thời tiết gọn gàng, 11 tùy chọn báo cáo thực tế, biểu đồ đường cong nhiệt độ 24h và trình xem lịch sử cập nhật.";
                        break;
                    case "1.4":
                        ChangelogHeaderTitle.Text = "Có gì mới trong Weather WinUI 1.4?";
                        ChangelogHeaderBadge.Text = "v1.4.0";
                        ChangelogHeaderSubtitle.Text = "Khắc phục triệt để lỗi văng app, tái thiết kế giao diện Cài đặt 2 cột responsive và tối ưu trình xem lời khuyên xổ xuống ngay trong thẻ.";
                        break;
                    case "1.3":
                        ChangelogHeaderTitle.Text = "Có gì mới trong Weather WinUI 1.3?";
                        ChangelogHeaderBadge.Text = "v1.3.0";
                        ChangelogHeaderSubtitle.Text = "Khắc phục hiện tượng đóng băng/crash khi báo cáo thời tiết và thiết kế lại layout lời khuyên thông minh chuyên nghiệp.";
                        break;
                    case "1.2":
                        ChangelogHeaderTitle.Text = "Có gì mới trong Weather WinUI 1.2?";
                        ChangelogHeaderBadge.Text = "v1.2.0";
                        ChangelogHeaderSubtitle.Text = "Chụp & chia sẻ ảnh thời tiết (Weather Share Card) vào Clipboard và tinh giản khung hiển thị.";
                        break;
                    case "1.1":
                        ChangelogHeaderTitle.Text = "Có gì mới trong Weather WinUI 1.1?";
                        ChangelogHeaderBadge.Text = "v1.1.0";
                        ChangelogHeaderSubtitle.Text = "Thông báo Toast Windows, Khay hệ thống System Tray, Widget Mini Desktop, Tên người dùng và Ảnh nền thành phố.";
                        break;
                    case "1.0":
                        ChangelogHeaderTitle.Text = "Weather WinUI 1.0 - Khởi Đầu Trải Nghiệm";
                        ChangelogHeaderBadge.Text = "v1.0.0 KHỞI ĐẦU";
                        ChangelogHeaderSubtitle.Text = "Ứng dụng thời tiết Fluent Design hiện đại, định vị GPS, tìm kiếm hơn 60 tỉnh thành và dự báo thời gian thực chuẩn xác.";
                        break;
                }
            }
        }
    }

    private void ChangelogDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (ViewModel != null)
        {
            ViewModel.Settings.LastSeenVersion = "2.2.3";
            var settingsService = new SettingsService();
            settingsService.SaveSettings(ViewModel.Settings);
        }
    }

    private async void CheckAndShowChangelog()
    {
        try
        {
            if (ViewModel != null && ViewModel.Settings.HasCompletedOnboarding && ViewModel.Settings.LastSeenVersion != "2.2.3")
            {
                await System.Threading.Tasks.Task.Delay(1200);
                if (this.XamlRoot != null && !_isDialogOpen)
                {
                    _isDialogOpen = true;
                    try
                    {
                        if (ChangelogVersionComboBox != null) ChangelogVersionComboBox.SelectedIndex = 0;
                        ChangelogDialog.XamlRoot = this.XamlRoot;
                        await ChangelogDialog.ShowAsync();
                    }
                    catch { }
                    finally
                    {
                        _isDialogOpen = false;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _isDialogOpen = false;
            System.Diagnostics.Debug.WriteLine($"[Changelog] Error: {ex.Message}");
        }
    }

    private void FloodDistrictComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox cb && cb.SelectedItem is string district && ViewModel != null)
        {
            ViewModel.FilterFloodRoadsByDistrict(district);
        }
    }

    #region OOTD & Full HD Share Handlers

    private void OccasionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag && ViewModel != null)
        {
            ViewModel.SelectOutfitOccasion(tag);
            UpdateOccasionButtonsVisual();
        }
    }

    private void UpdateOccasionButtonsVisual()
    {
        if (ViewModel == null) return;
        string current = ViewModel.SelectedOutfitOccasion;
        SetOccasionButtonState(OccasionWorkButton, current == "Work");
        SetOccasionButtonState(OccasionSchoolButton, current == "School");
        SetOccasionButtonState(OccasionCasualButton, current == "Casual");
    }

    private void SetOccasionButtonState(Button? btn, bool isSelected)
    {
        if (btn == null) return;
        btn.Background = isSelected
            ? (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AccentFillColorDefaultBrush"]
            : (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"];
        btn.Foreground = isSelected
            ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255))
            : (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];
    }

    private async void ShareWeatherCardButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel?.CurrentWeather == null) return;
        UpdateShareCardContent();
        if (ShareCardDialog != null)
        {
            ShareCardDialog.XamlRoot = this.XamlRoot;
            _ = RefreshShareCardBitmapAsync();
            await ShareCardDialog.ShowAsync();
        }
    }

    private async void ShareFormatRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (LandscapeCardContainer == null || StoryCardContainer == null || SharePreviewContainer == null) return;

        bool isStory = ShareFormatStoryRadio?.IsChecked == true;
        LandscapeCardContainer.Visibility = isStory ? Visibility.Collapsed : Visibility.Visible;
        StoryCardContainer.Visibility = isStory ? Visibility.Visible : Visibility.Collapsed;

        SharePreviewContainer.Width = isStory ? 1080 : 1920;
        SharePreviewContainer.Height = isStory ? 1920 : 1080;

        await RefreshShareCardBitmapAsync();
    }

    private void UpdateShareCardContent()
    {
        if (ViewModel?.CurrentWeather == null) return;
        var w = ViewModel.CurrentWeather;
        var o = ViewModel.CurrentOutfitAdvice;

        string location = ViewModel.LocationTitle.ToUpper();
        string nowTime = $"{DateTime.Now:dddd, dd/MM/yyyy • HH:mm}";
        string lunar = $"Âm lịch: {w.LunarDateText} • {w.SolarTermText}";
        string temp = w.TemperatureText.Replace("C", "").Trim();
        string feelsLike = $"{w.FeelsLikeText} • {w.MinMaxText}";
        string uv = $"{w.UvIndexText} - {w.UvIndexDescription}";
        string aqi = $"{w.AqiValue} - {w.AqiDescription}";
        string rain = w.RainProbabilityText;
        string wind = $"{w.WindText} {w.WindDirectionText}";
        string humidity = w.HumidityText;
        string sun = $"{w.SunriseText} • {w.SunsetText}";

        // 1. Cập nhật Khổ Ngang FHD (1920x1080)
        if (FhdLandscapeLocationText != null) FhdLandscapeLocationText.Text = location;
        if (FhdLandscapeDateText != null) FhdLandscapeDateText.Text = nowTime;
        if (FhdLandscapeLunarText != null) FhdLandscapeLunarText.Text = lunar;
        if (FhdLandscapeTempText != null) FhdLandscapeTempText.Text = temp;
        if (FhdLandscapeConditionText != null) FhdLandscapeConditionText.Text = w.ConditionText;
        if (FhdLandscapeFeelsLikeText != null) FhdLandscapeFeelsLikeText.Text = feelsLike;
        if (FhdLandscapeUvText != null) FhdLandscapeUvText.Text = uv;
        if (FhdLandscapeAqiText != null) FhdLandscapeAqiText.Text = aqi;
        if (FhdLandscapeRainProbText != null) FhdLandscapeRainProbText.Text = rain;
        if (FhdLandscapeWindText != null) FhdLandscapeWindText.Text = wind;
        if (FhdLandscapeHumidityText != null) FhdLandscapeHumidityText.Text = humidity;
        if (FhdLandscapeSunText != null) FhdLandscapeSunText.Text = sun;

        if (o != null)
        {
            if (FhdLandscapeOotdTitle != null) FhdLandscapeOotdTitle.Text = $"HÔM NAY MẶC GÌ? (OOTD ADVISOR) — {o.OccasionTitle.ToUpper()}";
            if (FhdLandscapeThermalTag != null) FhdLandscapeThermalTag.Text = o.ThermalComfortNotice;
            if (FhdLandscapeOotdHeadline != null) FhdLandscapeOotdHeadline.Text = o.Headline;
            if (FhdLandscapeOotdClothing != null) FhdLandscapeOotdClothing.Text = $"{o.TopClothing}, {o.BottomClothing}, {o.Footwear}";
            if (FhdLandscapeOotdAccessories != null) FhdLandscapeOotdAccessories.Text = string.Join(" • ", o.Accessories.Select(a => $"{a.Name}"));
            if (FhdLandscapeMotorbikeText != null) FhdLandscapeMotorbikeText.Text = o.MotorbikeWarning;
        }

        // Cập nhật 3 ngày dự báo
        if (ViewModel.DailyForecast != null && ViewModel.DailyForecast.Count >= 3)
        {
            var d1 = ViewModel.DailyForecast[0];
            var d2 = ViewModel.DailyForecast[1];
            var d3 = ViewModel.DailyForecast[2];

            if (FhdD1Day != null) FhdD1Day.Text = d1.DayName;
            if (FhdD1Temp != null) FhdD1Temp.Text = $"{d1.TempMinDisplay} / {d1.TempMaxDisplay}";
            if (FhdD2Day != null) FhdD2Day.Text = d2.DayName;
            if (FhdD2Temp != null) FhdD2Temp.Text = $"{d2.TempMinDisplay} / {d2.TempMaxDisplay}";
            if (FhdD3Day != null) FhdD3Day.Text = d3.DayName;
            if (FhdD3Temp != null) FhdD3Temp.Text = $"{d3.TempMinDisplay} / {d3.TempMaxDisplay}";
        }

        // 2. Cập nhật Khổ Dọc Story FHD (1080x1920)
        if (FhdStoryLocationText != null) FhdStoryLocationText.Text = location;
        if (FhdStoryDateText != null) FhdStoryDateText.Text = $"{DateTime.Now:dd/MM/yyyy • HH:mm}";
        if (FhdStoryLunarText != null) FhdStoryLunarText.Text = lunar;
        if (FhdStoryTempText != null) FhdStoryTempText.Text = temp;
        if (FhdStoryConditionText != null) FhdStoryConditionText.Text = w.ConditionText;
        if (FhdStoryFeelsLikeText != null) FhdStoryFeelsLikeText.Text = feelsLike;
        if (FhdStoryUvText != null) FhdStoryUvText.Text = uv;
        if (FhdStoryAqiText != null) FhdStoryAqiText.Text = aqi;
        if (FhdStoryRainProbText != null) FhdStoryRainProbText.Text = rain;
        if (FhdStoryWindText != null) FhdStoryWindText.Text = wind;
        if (FhdStoryHumidityText != null) FhdStoryHumidityText.Text = humidity;
        if (FhdStorySunText != null) FhdStorySunText.Text = sun;

        if (o != null)
        {
            if (FhdStoryOotdTitle != null) FhdStoryOotdTitle.Text = $"👗 HÔM NAY MẶC GÌ? — {o.OccasionTitle.ToUpper()}";
            if (FhdStoryOotdHeadline != null) FhdStoryOotdHeadline.Text = o.Headline;
            if (FhdStoryOotdAccessories != null) FhdStoryOotdAccessories.Text = $"🎒 Phụ kiện: {string.Join(" • ", o.Accessories.Select(a => a.Name))}";
            if (FhdStoryMotorbikeText != null) FhdStoryMotorbikeText.Text = o.MotorbikeWarning;
        }

        if (ViewModel.DailyForecast != null && ViewModel.DailyForecast.Count >= 3)
        {
            var d1 = ViewModel.DailyForecast[0];
            var d2 = ViewModel.DailyForecast[1];
            var d3 = ViewModel.DailyForecast[2];

            if (FhdStoryD1Day != null) FhdStoryD1Day.Text = d1.DayName;
            if (FhdStoryD1Temp != null) FhdStoryD1Temp.Text = $"{d1.TempMinDisplay} / {d1.TempMaxDisplay}";
            if (FhdStoryD2Day != null) FhdStoryD2Day.Text = d2.DayName;
            if (FhdStoryD2Temp != null) FhdStoryD2Temp.Text = $"{d2.TempMinDisplay} / {d2.TempMaxDisplay}";
            if (FhdStoryD3Day != null) FhdStoryD3Day.Text = d3.DayName;
            if (FhdStoryD3Temp != null) FhdStoryD3Temp.Text = $"{d3.TempMinDisplay} / {d3.TempMaxDisplay}";
        }

        // Hình nền thành phố nếu có
        if (w.HasCityImage && !string.IsNullOrEmpty(w.CityImagePath))
        {
            try
            {
                var bmp = CityBackgroundHelper.LoadOptimizedBitmap(w.CityImagePath);
                if (FhdLandscapeCityImage != null) FhdLandscapeCityImage.Source = bmp;
                if (FhdStoryCityImage != null) FhdStoryCityImage.Source = bmp;
            }
            catch { }
        }
    }

    private byte[]? _cachedSharePixels;
    private uint _cachedShareWidth;
    private uint _cachedShareHeight;
    private bool _isRenderingShareCard;

    public async Task RefreshShareCardBitmapAsync(bool? forceStory = null)
    {
        if (_isRenderingShareCard) return;
        _isRenderingShareCard = true;
        try
        {
            if (SharePreviewProgressRing != null)
            {
                SharePreviewProgressRing.Visibility = Visibility.Visible;
                SharePreviewProgressRing.IsActive = true;
            }

            bool isStory = forceStory ?? (ShareFormatStoryRadio?.IsChecked == true);
            if (LandscapeCardContainer != null) LandscapeCardContainer.Visibility = isStory ? Visibility.Collapsed : Visibility.Visible;
            if (StoryCardContainer != null) StoryCardContainer.Visibility = isStory ? Visibility.Visible : Visibility.Collapsed;
            if (SharePreviewContainer != null)
            {
                SharePreviewContainer.Width = isStory ? 1080 : 1920;
                SharePreviewContainer.Height = isStory ? 1920 : 1080;
            }

            UIElement? targetElement = isStory ? StoryCardContainer : LandscapeCardContainer;
            int targetWidth = isStory ? 1080 : 1920;
            int targetHeight = isStory ? 1920 : 1080;

            if (targetElement == null) return;

            // Đảm bảo layout 1:1 Full HD không bị co kéo hay scale bởi Viewbox
            targetElement.Measure(new Windows.Foundation.Size(targetWidth, targetHeight));
            targetElement.Arrange(new Windows.Foundation.Rect(0, 0, targetWidth, targetHeight));
            targetElement.UpdateLayout();

            await Task.Delay(150);

            var rtb = new RenderTargetBitmap();
            await rtb.RenderAsync(targetElement);

            var buffer = await rtb.GetPixelsAsync();
            _cachedSharePixels = buffer.ToArray();
            _cachedShareWidth = (uint)rtb.PixelWidth;
            _cachedShareHeight = (uint)rtb.PixelHeight;

            if (SharePreviewImage != null)
            {
                SharePreviewImage.Source = rtb;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RefreshShareCardBitmapAsync] Error: {ex.Message}");
        }
        finally
        {
            _isRenderingShareCard = false;
            if (SharePreviewProgressRing != null)
            {
                SharePreviewProgressRing.IsActive = false;
                SharePreviewProgressRing.Visibility = Visibility.Collapsed;
            }
        }
    }

    private async void CopyFhdShareCardButton_Click(object sender, RoutedEventArgs e)
    {
        bool isStory = ShareFormatStoryRadio?.IsChecked == true;
        UIElement targetElement = isStory ? StoryCardContainer : LandscapeCardContainer;
        int targetWidth = isStory ? 1080 : 1920;
        int targetHeight = isStory ? 1920 : 1080;

        await ExportCardAsync(targetElement, targetWidth, targetHeight, copyToClipboard: true, isStory: isStory);
    }

    private async void SaveFhdShareCardButton_Click(object sender, RoutedEventArgs e)
    {
        bool isStory = ShareFormatStoryRadio?.IsChecked == true;
        UIElement targetElement = isStory ? StoryCardContainer : LandscapeCardContainer;
        int targetWidth = isStory ? 1080 : 1920;
        int targetHeight = isStory ? 1920 : 1080;

        await ExportCardAsync(targetElement, targetWidth, targetHeight, copyToClipboard: false, isStory: isStory);
    }

    private async Task ExportCardAsync(UIElement element, int width, int height, bool copyToClipboard, bool isStory)
    {
        try
        {
            if (_cachedSharePixels == null || _cachedSharePixels.Length == 0)
            {
                await RefreshShareCardBitmapAsync(isStory);
            }

            if (_cachedSharePixels == null || _cachedSharePixels.Length == 0)
            {
                return;
            }

            uint outWidth = _cachedShareWidth > 0 ? _cachedShareWidth : (uint)width;
            uint outHeight = _cachedShareHeight > 0 ? _cachedShareHeight : (uint)height;

            if (copyToClipboard)
            {
                string tempFile = Path.Combine(Path.GetTempPath(), $"WeatherFHD_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                using (var fileStream = File.OpenWrite(tempFile))
                {
                    var randomAccessStream = fileStream.AsRandomAccessStream();
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, randomAccessStream);
                    encoder.SetPixelData(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Premultiplied,
                        outWidth,
                        outHeight,
                        96, 96,
                        _cachedSharePixels);
                    await encoder.FlushAsync();
                }

                var storageFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(tempFile);
                var dataPackage = new DataPackage();
                dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromFile(storageFile));
                dataPackage.SetText($"🌤️ Dự báo thời tiết {ViewModel.LocationTitle}: {ViewModel.CurrentWeather?.TemperatureText}, {ViewModel.CurrentWeather?.ConditionText} (Thẻ Full HD {outWidth}x{outHeight} từ Weather WinUI v2.2.2)");
                dataPackage.RequestedOperation = DataPackageOperation.Copy;
                Clipboard.SetContent(dataPackage);

                ShareCardDialog?.Hide();
                if (ShareSuccessInfoBar != null)
                {
                    ShareSuccessInfoBar.Title = "Đã sao chép ảnh Full HD thành công!";
                    ShareSuccessInfoBar.Message = $"Ảnh thời tiết {outWidth}x{outHeight} siêu nét đã lưu vào Clipboard. Bạn có thể nhấn Ctrl+V để dán trực tiếp vào Zalo, Facebook, Messenger.";
                    ShareSuccessInfoBar.IsOpen = true;
                }
            }
            else
            {
                var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hWnd);
                savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
                savePicker.FileTypeChoices.Add("PNG Image (*.png)", new List<string> { ".png" });
                savePicker.SuggestedFileName = $"Weather_{ViewModel.LocationTitle.Replace(" ", "_")}_{outWidth}x{outHeight}_{DateTime.Now:yyyyMMdd}";

                var file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    using (var fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
                    {
                        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);
                        encoder.SetPixelData(
                            BitmapPixelFormat.Bgra8,
                            BitmapAlphaMode.Premultiplied,
                            outWidth,
                            outHeight,
                            96, 96,
                            _cachedSharePixels);
                        await encoder.FlushAsync();
                    }

                    ShareCardDialog?.Hide();
                    if (ShareSuccessInfoBar != null)
                    {
                        ShareSuccessInfoBar.Title = "Đã lưu ảnh Full HD thành công!";
                        ShareSuccessInfoBar.Message = $"File ảnh chất lượng cao {outWidth}x{outHeight}px đã lưu tại: {file.Path}.";
                        ShareSuccessInfoBar.IsOpen = true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ExportCardAsync] Error: {ex.Message}");
        }
    }

    #endregion

    private void RenderHourlyTemperatureTrendline()
    {
        try
        {
            if (HourlyTrendlineCanvas == null || ViewModel?.HourlyForecast == null || ViewModel.HourlyForecast.Count == 0)
                return;

            HourlyTrendlineCanvas.Children.Clear();

            var items = ViewModel.HourlyForecast.ToList();
            int count = items.Count;
            if (count < 2) return;

            double colWidth = 92.0;
            double cardWidth = 82.0;
            double totalWidth = count * colWidth;
            HourlyTrendlineCanvas.Width = totalWidth;

            double canvasHeight = 74.0;
            HourlyTrendlineCanvas.Height = canvasHeight;

            double minTemp = items.Min(x => x.TempValue);
            double maxTemp = items.Max(x => x.TempValue);
            double tempRange = Math.Max(1.0, maxTemp - minTemp);

            double topPadding = 24.0;
            double bottomPadding = 14.0;
            double usableHeight = canvasHeight - topPadding - bottomPadding;

            var points = new List<Windows.Foundation.Point>();
            for (int i = 0; i < count; i++)
            {
                double x = i * colWidth + (cardWidth / 2.0);
                double normalized = (items[i].TempValue - minTemp) / tempRange;
                double y = topPadding + (1.0 - normalized) * usableHeight;
                points.Add(new Windows.Foundation.Point(x, y));
            }

            var strokeFigure = new Microsoft.UI.Xaml.Media.PathFigure
            {
                StartPoint = points[0],
                IsClosed = false
            };

            var fillFigure = new Microsoft.UI.Xaml.Media.PathFigure
            {
                StartPoint = new Windows.Foundation.Point(points[0].X, canvasHeight),
                IsClosed = true
            };
            fillFigure.Segments.Add(new Microsoft.UI.Xaml.Media.LineSegment { Point = points[0] });

            for (int i = 0; i < count - 1; i++)
            {
                var p0 = points[i];
                var p1 = points[i + 1];

                double dx = (p1.X - p0.X) / 2.2;
                var cp1 = new Windows.Foundation.Point(p0.X + dx, p0.Y);
                var cp2 = new Windows.Foundation.Point(p1.X - dx, p1.Y);

                var bezier = new Microsoft.UI.Xaml.Media.BezierSegment
                {
                    Point1 = cp1,
                    Point2 = cp2,
                    Point3 = p1
                };

                strokeFigure.Segments.Add(bezier);
                fillFigure.Segments.Add(bezier);
            }

            fillFigure.Segments.Add(new Microsoft.UI.Xaml.Media.LineSegment { Point = new Windows.Foundation.Point(points[count - 1].X, canvasHeight) });

            var fillGeometry = new Microsoft.UI.Xaml.Media.PathGeometry();
            fillGeometry.Figures.Add(fillFigure);

            // Dải Gradient bán trong suốt lấp lánh phản chiếu phía dưới
            var areaFill = new Microsoft.UI.Xaml.Shapes.Path
            {
                Data = fillGeometry,
                Fill = new Microsoft.UI.Xaml.Media.LinearGradientBrush
                {
                    StartPoint = new Windows.Foundation.Point(0, 0),
                    EndPoint = new Windows.Foundation.Point(0, 1),
                    GradientStops = new Microsoft.UI.Xaml.Media.GradientStopCollection
                    {
                        new Microsoft.UI.Xaml.Media.GradientStop { Color = Windows.UI.Color.FromArgb(95, 245, 158, 11), Offset = 0.0 },
                        new Microsoft.UI.Xaml.Media.GradientStop { Color = Windows.UI.Color.FromArgb(28, 245, 158, 11), Offset = 0.6 },
                        new Microsoft.UI.Xaml.Media.GradientStop { Color = Windows.UI.Color.FromArgb(0, 245, 158, 11), Offset = 1.0 }
                    }
                }
            };
            HourlyTrendlineCanvas.Children.Add(areaFill);

            var strokeGeometry = new Microsoft.UI.Xaml.Media.PathGeometry();
            strokeGeometry.Figures.Add(strokeFigure);

            // Đường cong Bézier cubic mượt mà chuẩn xác
            var strokePath = new Microsoft.UI.Xaml.Shapes.Path
            {
                Data = strokeGeometry,
                Stroke = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)),
                StrokeThickness = 2.5,
                StrokeStartLineCap = Microsoft.UI.Xaml.Media.PenLineCap.Round,
                StrokeEndLineCap = Microsoft.UI.Xaml.Media.PenLineCap.Round
            };
            HourlyTrendlineCanvas.Children.Add(strokePath);

            for (int i = 0; i < count; i++)
            {
                var pt = points[i];

                // 1. Chấm tròn trắng nổi bật ngay trên đỉnh sóng
                var dot = new Microsoft.UI.Xaml.Shapes.Ellipse
                {
                    Width = 7,
                    Height = 7,
                    Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
                    Stroke = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)),
                    StrokeThickness = 2
                };
                Canvas.SetLeft(dot, pt.X - 3.5);
                Canvas.SetTop(dot, pt.Y - 3.5);
                HourlyTrendlineCanvas.Children.Add(dot);

                // 2. Nhãn nhiệt độ nổi bật ngay trên đỉnh sóng
                var label = new TextBlock
                {
                    Text = items[i].TempDisplay,
                    FontSize = 11,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 254, 243, 199)),
                    HorizontalTextAlignment = TextAlignment.Center,
                    Width = 50
                };
                Canvas.SetLeft(label, pt.X - 25);
                Canvas.SetTop(label, pt.Y - 21);
                HourlyTrendlineCanvas.Children.Add(label);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HourlyTrendline] Render error: {ex.Message}");
        }
    }

    private void ReopenOnboardingButton_Click(object sender, RoutedEventArgs e)
    {
        SettingsDialog.Hide();
        Frame.Navigate(typeof(OnboardingPage));
    }

    private void TimezoneComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSettingsSyncing || ViewModel == null) return;
        if (TimezoneComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            ViewModel.Settings.TimezoneMode = tag;
            AutoSaveSettings();
        }
    }

    private void HourFormatToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isSettingsSyncing || ViewModel == null) return;
        ViewModel.Settings.Is24HourFormat = HourFormatToggle.IsOn;
        AutoSaveSettings();
    }

    private void DateFormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSettingsSyncing || ViewModel == null) return;
        if (DateFormatComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            ViewModel.Settings.DateFormat = tag;
            AutoSaveSettings();
        }
    }

    private void FirstDayOfWeekComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSettingsSyncing || ViewModel == null) return;
        if (FirstDayOfWeekComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            ViewModel.Settings.FirstDayOfWeek = tag;
            ViewModel.GenerateCalendar();
            AutoSaveSettings();
        }
    }

    private void StartupToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isSettingsSyncing || ViewModel == null) return;
        ViewModel.SetStartup(StartupToggle.IsOn);
        AutoSaveSettings();
    }

    private void TempUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSettingsSyncing || ViewModel == null) return;
        if (TempUnitComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            ViewModel.Settings.TemperatureUnit = tag;
            AutoSaveSettings(true);
        }
    }

    private void WindUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSettingsSyncing || ViewModel == null) return;
        if (WindUnitComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            ViewModel.Settings.WindSpeedUnit = tag;
            AutoSaveSettings(true);
        }
    }

    private void PressureUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSettingsSyncing || ViewModel == null) return;
        if (PressureUnitComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            ViewModel.Settings.PressureUnit = tag;
            AutoSaveSettings(true);
        }
    }

    private void PrecipUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSettingsSyncing || ViewModel == null) return;
        if (PrecipUnitComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            ViewModel.Settings.PrecipitationUnit = tag;
            AutoSaveSettings(true);
        }
    }

    private void SettingsThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSettingsSyncing || ViewModel == null) return;
        if (SettingsThemeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            ViewModel.ChangeTheme(tag);
            ThemeComboBox.SelectedIndex = tag switch
            {
                "light" => 0,
                "dark" => 1,
                _ => 2
            };
            AutoSaveSettings(true);
        }
    }

    private void RefreshIntervalComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSettingsSyncing || ViewModel == null) return;
        if (RefreshIntervalComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag && int.TryParse(tag, out int minutes))
        {
            ViewModel.Settings.AutoRefreshIntervalMinutes = minutes;
            AutoSaveSettings();
        }
    }

    private void EffectsToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isSettingsSyncing || ViewModel == null) return;
        ViewModel.Settings.EnableWeatherEffects = EffectsToggle.IsOn;
        UpdateWeatherVisuals();
        AutoSaveSettings(true);
    }

    #endregion

    #region Search and Theme Handlers

    private async void LocationSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (ViewModel == null) return;

        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            string query = sender.Text;
            if (!string.IsNullOrWhiteSpace(query) && query.Trim().Length >= 2)
            {
                await ViewModel.SearchLocationsCommand.ExecuteAsync(query);
                sender.ItemsSource = ViewModel.SearchResults;
            }
            else
            {
                sender.ItemsSource = null;
            }
        }
    }

    private void LocationSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is GeocodingItem item)
        {
            sender.Text = item.DisplayText;
        }
    }

    private async void LocationSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (ViewModel == null) return;

        if (args.ChosenSuggestion is GeocodingItem item)
        {
            sender.Text = item.DisplayText;
            await ViewModel.SelectLocationCommand.ExecuteAsync(item);
        }
        else if (!string.IsNullOrWhiteSpace(args.QueryText))
        {
            await ViewModel.SearchLocationsCommand.ExecuteAsync(args.QueryText);
            if (ViewModel.SearchResults.Count > 0)
            {
                var first = ViewModel.SearchResults[0];
                sender.Text = first.DisplayText;
                await ViewModel.SelectLocationCommand.ExecuteAsync(first);
            }
        }
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSettingsSyncing || ViewModel == null) return;

        if (ThemeComboBox?.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is string tag)
        {
            ViewModel.ChangeTheme(tag);
            AutoSaveSettings(true);
        }
    }

    #endregion

    #region Version 1.7 Handlers & Visuals

    private async void QuickLocationChip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is QuickLocationChipItem chip && ViewModel != null)
        {
            await ViewModel.SelectQuickChipAsync(chip);
            UpdateWeatherVisuals();
            RenderHourlyTemperatureTrendline();
            RenderSunArc();
        }
    }

    private async void FavoriteCityChip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is FavoriteLocationItem fav && ViewModel != null)
        {
            await ViewModel.SelectFavoriteLocationAsync(fav);
            UpdateWeatherVisuals();
            RenderHourlyTemperatureTrendline();
            RenderSunArc();
        }
    }

    private void RenderSunArc()
    {
        if (SunArcCanvas == null || ViewModel?.CurrentWeather == null) return;

        SunArcCanvas.Children.Clear();

        double width = SunArcCanvas.ActualWidth;
        double height = SunArcCanvas.ActualHeight;
        if (width <= 20 || height <= 10) return;

        double horizonY = height - 4;
        double leftX = 10;
        double rightX = width - 10;
        double peakY = 5;

        // 1. Đường chân trời chấm nét đứt
        var horizonLine = new Microsoft.UI.Xaml.Shapes.Line
        {
            X1 = 4,
            Y1 = horizonY,
            X2 = width - 4,
            Y2 = horizonY,
            Stroke = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(40, 255, 255, 255)),
            StrokeThickness = 1,
            StrokeDashArray = new Microsoft.UI.Xaml.Media.DoubleCollection { 2, 2 }
        };
        SunArcCanvas.Children.Add(horizonLine);

        // 2. Vòng cung quỹ đạo mặt trời (Cubic/Quadratic Bézier Path)
        var pathGeometry = new Microsoft.UI.Xaml.Media.PathGeometry();
        var figure = new Microsoft.UI.Xaml.Media.PathFigure
        {
            StartPoint = new Windows.Foundation.Point(leftX, horizonY),
            IsClosed = false
        };

        double midX = (leftX + rightX) / 2.0;
        var bezier = new Microsoft.UI.Xaml.Media.QuadraticBezierSegment
        {
            Point1 = new Windows.Foundation.Point(midX, peakY - (horizonY - peakY) * 0.9),
            Point2 = new Windows.Foundation.Point(rightX, horizonY)
        };
        figure.Segments.Add(bezier);
        pathGeometry.Figures.Add(figure);

        var arcPath = new Microsoft.UI.Xaml.Shapes.Path
        {
            Data = pathGeometry,
            Stroke = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(90, 255, 255, 255)),
            StrokeThickness = 1.6,
            StrokeDashArray = new Microsoft.UI.Xaml.Media.DoubleCollection { 3, 2.5 }
        };
        SunArcCanvas.Children.Add(arcPath);

        // 3. Vị trí Mặt Trời / Mặt Trăng hiện tại trên cung
        double progress = Math.Clamp(ViewModel.CurrentWeather.SunProgressPercent, 0.0, 1.0);
        double angleRad = Math.PI * (1.0 - progress);
        double radiusX = (rightX - leftX) / 2.0;
        double radiusY = horizonY - peakY;
        double currentX = midX + radiusX * Math.Cos(angleRad);
        double currentY = horizonY - radiusY * Math.Sin(angleRad);

        bool isSun = ViewModel.CurrentWeather.IsSunVisible;

        // Vòng phát sáng ngoại vi (Glow)
        var glow = new Microsoft.UI.Xaml.Shapes.Ellipse
        {
            Width = 18,
            Height = 18,
            Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(isSun
                ? Windows.UI.Color.FromArgb(70, 251, 191, 36)
                : Windows.UI.Color.FromArgb(60, 96, 165, 250))
        };
        Canvas.SetLeft(glow, currentX - 9);
        Canvas.SetTop(glow, currentY - 9);
        SunArcCanvas.Children.Add(glow);

        // Chấm tròn Mặt Trời / Mặt Trăng
        var celestialDot = new Microsoft.UI.Xaml.Shapes.Ellipse
        {
            Width = 10,
            Height = 10,
            Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(isSun
                ? Windows.UI.Color.FromArgb(255, 245, 158, 11)
                : Windows.UI.Color.FromArgb(255, 147, 197, 253)),
            Stroke = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
            StrokeThickness = 1.5
        };
        Canvas.SetLeft(celestialDot, currentX - 5);
        Canvas.SetTop(celestialDot, currentY - 5);
        SunArcCanvas.Children.Add(celestialDot);
    }

    #endregion

    #region Calendar Handlers

    private async void CalendarDayCell_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is CalendarDayItem day)
        {
            ViewModel?.OpenDayDetail(day);
            if (CalendarDayDetailDialog != null)
            {
                try
                {
                    CalendarDayDetailDialog.XamlRoot = this.XamlRoot;
                    await CalendarDayDetailDialog.ShowAsync();
                }
                catch { }
            }
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

    #endregion

    #region Version 3.0 Beta - Navigation, Time Scrubber, Tidal Wave & Radar

    private void MainNavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        try
        {
            if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
            {
                ViewModel?.SwitchNav(tag);
                if (tag == "flood")
                {
                    DispatcherQueue.TryEnqueue(() => RenderTidalSineWave());
                }
                else if (tag == "radar")
                {
                    DispatcherQueue.TryEnqueue(() => RenderRadarSimulation());
                }
                else if (tag == "overview")
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        RenderHourlyTemperatureTrendline();
                        RenderSunArc();
                    });
                }
            }
        }
        catch { }
    }

    private void TimeScrubberSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        try
        {
            int hour = (int)Math.Round(e.NewValue);
            ViewModel?.ScrubToHour(hour);
        }
        catch { }
    }

    private void ResetScrubberButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ViewModel?.ResetTimeScrubbing();
            UpdateWeatherVisuals();
        }
        catch { }
    }

    private void FloodSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            if (sender is TextBox tb && ViewModel != null)
            {
                ViewModel.FloodStreetSearchQuery = tb.Text ?? string.Empty;
            }
        }
        catch { }
    }

    private void TidalSineWaveCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        RenderTidalSineWave();
    }

    private void RadarSimulationCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        RenderRadarSimulation();
    }

    private void RenderTidalSineWave()
    {
        try
        {
            if (TidalSineWaveCanvas == null || ViewModel?.UrbanFloodWarning == null) return;
            TidalSineWaveCanvas.Children.Clear();

            double width = TidalSineWaveCanvas.ActualWidth;
            double height = TidalSineWaveCanvas.ActualHeight;
            if (width <= 20 || height <= 20) return;

            var points = ViewModel.UrbanFloodWarning.TideCurve24h;
            if (points == null || points.Count == 0) return;

            double minLevel = 0.70;
            double maxLevel = 1.80;
            double range = maxLevel - minLevel;

            double GetY(double level) => Math.Max(5, Math.Min(height - 20, height - 20 - ((level - minLevel) / range * (height - 30))));
            double GetX(int hour) => (hour / 23.0) * (width - 40) + 20;

            // 1. Alarm lines
            // BD 3: 1.60m
            double yBd3 = GetY(1.60);
            var lineBd3 = new Microsoft.UI.Xaml.Shapes.Line { X1 = 20, Y1 = yBd3, X2 = width - 20, Y2 = yBd3, Stroke = new SolidColorBrush(Color.FromArgb(160, 239, 68, 68)), StrokeDashArray = new DoubleCollection { 4, 3 }, StrokeThickness = 1 };
            TidalSineWaveCanvas.Children.Add(lineBd3);
            var labelBd3 = new TextBlock { Text = "Báo động III (1.60m)", FontSize = 10, Foreground = new SolidColorBrush(Color.FromArgb(220, 239, 68, 68)) };
            Canvas.SetLeft(labelBd3, 24);
            Canvas.SetTop(labelBd3, yBd3 - 14);
            TidalSineWaveCanvas.Children.Add(labelBd3);

            // BD 2: 1.55m
            double yBd2 = GetY(1.55);
            var lineBd2 = new Microsoft.UI.Xaml.Shapes.Line { X1 = 20, Y1 = yBd2, X2 = width - 20, Y2 = yBd2, Stroke = new SolidColorBrush(Color.FromArgb(140, 234, 88, 12)), StrokeDashArray = new DoubleCollection { 4, 3 }, StrokeThickness = 1 };
            TidalSineWaveCanvas.Children.Add(lineBd2);

            // BD 1: 1.40m
            double yBd1 = GetY(1.40);
            var lineBd1 = new Microsoft.UI.Xaml.Shapes.Line { X1 = 20, Y1 = yBd1, X2 = width - 20, Y2 = yBd1, Stroke = new SolidColorBrush(Color.FromArgb(140, 245, 158, 11)), StrokeDashArray = new DoubleCollection { 4, 3 }, StrokeThickness = 1 };
            TidalSineWaveCanvas.Children.Add(lineBd1);
            var labelBd1 = new TextBlock { Text = "Báo động I (1.40m)", FontSize = 10, Foreground = new SolidColorBrush(Color.FromArgb(180, 245, 158, 11)) };
            Canvas.SetLeft(labelBd1, Math.Max(20, width - 140));
            Canvas.SetTop(labelBd1, yBd1 - 14);
            TidalSineWaveCanvas.Children.Add(labelBd1);

            // 2. Fill Polygon
            var polygon = new Microsoft.UI.Xaml.Shapes.Polygon();
            var fillBrush = new LinearGradientBrush { StartPoint = new Windows.Foundation.Point(0, 0), EndPoint = new Windows.Foundation.Point(0, 1) };
            fillBrush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(90, 2, 132, 199), Offset = 0 });
            fillBrush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(15, 2, 132, 199), Offset = 1 });
            polygon.Fill = fillBrush;

            polygon.Points.Add(new Windows.Foundation.Point(GetX(0), height - 10));
            foreach (var p in points)
            {
                polygon.Points.Add(new Windows.Foundation.Point(GetX(p.Hour), GetY(p.LevelMeters)));
            }
            polygon.Points.Add(new Windows.Foundation.Point(GetX(23), height - 10));
            TidalSineWaveCanvas.Children.Add(polygon);

            // 3. Wave Line
            var polyline = new Microsoft.UI.Xaml.Shapes.Polyline
            {
                Stroke = new SolidColorBrush(Color.FromArgb(255, 56, 189, 248)),
                StrokeThickness = 2.5
            };
            foreach (var p in points)
            {
                polyline.Points.Add(new Windows.Foundation.Point(GetX(p.Hour), GetY(p.LevelMeters)));
            }
            TidalSineWaveCanvas.Children.Add(polyline);

            // 4. Markers
            int currentHour = DateTime.Now.Hour;
            foreach (var p in points)
            {
                double px = GetX(p.Hour);
                double py = GetY(p.LevelMeters);

                if (p.IsPeak)
                {
                    var peakDot = new Microsoft.UI.Xaml.Shapes.Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)), Stroke = new SolidColorBrush(Microsoft.UI.Colors.White), StrokeThickness = 1.5 };
                    Canvas.SetLeft(peakDot, px - 4);
                    Canvas.SetTop(peakDot, py - 4);
                    TidalSineWaveCanvas.Children.Add(peakDot);

                    var peakLabel = new TextBlock { Text = $"{p.LevelMeters:F2}m", FontSize = 9.5, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromArgb(230, 239, 68, 68)) };
                    Canvas.SetLeft(peakLabel, px - 12);
                    Canvas.SetTop(peakLabel, py - 18);
                    TidalSineWaveCanvas.Children.Add(peakLabel);
                }

                if (p.Hour == currentHour)
                {
                    var nowRing = new Microsoft.UI.Xaml.Shapes.Ellipse { Width = 18, Height = 18, Stroke = new SolidColorBrush(Color.FromArgb(160, 56, 189, 248)), StrokeThickness = 2, Fill = new SolidColorBrush(Color.FromArgb(40, 56, 189, 248)) };
                    Canvas.SetLeft(nowRing, px - 9);
                    Canvas.SetTop(nowRing, py - 9);
                    TidalSineWaveCanvas.Children.Add(nowRing);

                    var nowDot = new Microsoft.UI.Xaml.Shapes.Ellipse { Width = 10, Height = 10, Fill = new SolidColorBrush(Microsoft.UI.Colors.White), Stroke = new SolidColorBrush(Color.FromArgb(255, 2, 132, 199)), StrokeThickness = 2 };
                    Canvas.SetLeft(nowDot, px - 5);
                    Canvas.SetTop(nowDot, py - 5);
                    TidalSineWaveCanvas.Children.Add(nowDot);

                    var nowLabel = new TextBlock { Text = $"BÂY GIỜ: {p.LevelMeters:F2}m", FontSize = 10.5, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromArgb(255, 56, 189, 248)) };
                    Canvas.SetLeft(nowLabel, Math.Min(width - 110, Math.Max(10, px - 30)));
                    Canvas.SetTop(nowLabel, Math.Max(2, py - 20));
                    TidalSineWaveCanvas.Children.Add(nowLabel);
                }

                if (p.Hour % 4 == 0 || p.Hour == 23)
                {
                    var timeTxt = new TextBlock { Text = $"{p.Hour:D2}:00", FontSize = 9.5, Opacity = 0.65 };
                    Canvas.SetLeft(timeTxt, px - 12);
                    Canvas.SetTop(timeTxt, height - 16);
                    TidalSineWaveCanvas.Children.Add(timeTxt);
                }
            }
        }
        catch { }
    }

    private void RenderRadarSimulation()
    {
        try
        {
            if (RadarSimulationCanvas == null) return;
            RadarSimulationCanvas.Children.Clear();

            double w = RadarSimulationCanvas.ActualWidth;
            double h = RadarSimulationCanvas.ActualHeight;
            if (w <= 20 || h <= 20) return;

            double cx = w / 2;
            double cy = h / 2;
            double maxRadius = Math.Min(cx, cy) - 20;

            // 1. Range rings
            for (int i = 1; i <= 4; i++)
            {
                double r = maxRadius * (i / 4.0);
                var circle = new Microsoft.UI.Xaml.Shapes.Ellipse
                {
                    Width = r * 2,
                    Height = r * 2,
                    Stroke = new SolidColorBrush(Color.FromArgb(70, 56, 189, 248)),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 3, 3 }
                };
                Canvas.SetLeft(circle, cx - r);
                Canvas.SetTop(circle, cy - r);
                RadarSimulationCanvas.Children.Add(circle);

                var kmLabel = new TextBlock { Text = $"{i * 50}km", FontSize = 9, Opacity = 0.5 };
                Canvas.SetLeft(kmLabel, cx + 4);
                Canvas.SetTop(kmLabel, cy - r + 2);
                RadarSimulationCanvas.Children.Add(kmLabel);
            }

            // 2. Crosshairs
            var hLine = new Microsoft.UI.Xaml.Shapes.Line { X1 = cx - maxRadius, Y1 = cy, X2 = cx + maxRadius, Y2 = cy, Stroke = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)), StrokeThickness = 1 };
            var vLine = new Microsoft.UI.Xaml.Shapes.Line { X1 = cx, Y1 = cy - maxRadius, X2 = cx, Y2 = cy + maxRadius, Stroke = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)), StrokeThickness = 1 };
            RadarSimulationCanvas.Children.Add(hLine);
            RadarSimulationCanvas.Children.Add(vLine);

            // 3. Simulated clouds & precipitation echoes
            var blob1 = new Microsoft.UI.Xaml.Shapes.Ellipse { Width = 90, Height = 60, Fill = new SolidColorBrush(Color.FromArgb(80, 16, 185, 129)) };
            Canvas.SetLeft(blob1, cx + 20);
            Canvas.SetTop(blob1, cy - 70);
            RadarSimulationCanvas.Children.Add(blob1);

            var blob2 = new Microsoft.UI.Xaml.Shapes.Ellipse { Width = 60, Height = 45, Fill = new SolidColorBrush(Color.FromArgb(90, 245, 158, 11)) };
            Canvas.SetLeft(blob2, cx + 35);
            Canvas.SetTop(blob2, cy - 60);
            RadarSimulationCanvas.Children.Add(blob2);

            var blob3 = new Microsoft.UI.Xaml.Shapes.Ellipse { Width = 28, Height = 22, Fill = new SolidColorBrush(Color.FromArgb(120, 239, 68, 68)) };
            Canvas.SetLeft(blob3, cx + 48);
            Canvas.SetTop(blob3, cy - 50);
            RadarSimulationCanvas.Children.Add(blob3);

            // 4. Center beacon
            var centerDot = new Microsoft.UI.Xaml.Shapes.Ellipse { Width = 10, Height = 10, Fill = new SolidColorBrush(Color.FromArgb(255, 56, 189, 248)), Stroke = new SolidColorBrush(Microsoft.UI.Colors.White), StrokeThickness = 2 };
            Canvas.SetLeft(centerDot, cx - 5);
            Canvas.SetTop(centerDot, cy - 5);
            RadarSimulationCanvas.Children.Add(centerDot);

            var centerLabel = new TextBlock { Text = "VỊ TRÍ HIỆN TẠI", FontSize = 9.5, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromArgb(255, 56, 189, 248)) };
            Canvas.SetLeft(centerLabel, cx - 35);
            Canvas.SetTop(centerLabel, cy + 8);
            RadarSimulationCanvas.Children.Add(centerLabel);

            // 5. Radar sweep beam
            var sweepLine = new Microsoft.UI.Xaml.Shapes.Line { X1 = cx, Y1 = cy, X2 = cx + maxRadius * 0.7, Y2 = cy - maxRadius * 0.7, Stroke = new SolidColorBrush(Color.FromArgb(200, 56, 189, 248)), StrokeThickness = 2 };
            RadarSimulationCanvas.Children.Add(sweepLine);
        }
        catch { }
    }

    #endregion

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
            AutoSaveSettings(true);
        }
        catch { }
    }

    private void CardWidgetBryanC_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        SelectWidgetStyle("BryanCDynamic");
    }

    private void CardWidgetGlassCard_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        SelectWidgetStyle("GlassCard");
    }

    private void CardWidgetCompact_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        SelectWidgetStyle("Compact");
    }

    private void CardWidgetMiniIsland_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        SelectWidgetStyle("MiniIsland");
    }

    private void SelectWidgetStyle(string styleTag)
    {
        if (ViewModel == null) return;
        ViewModel.Settings.WidgetStyle = styleTag;
        ViewModel.ApplySettings();

        if (StudioWidgetStyleComboBox != null)
        {
            for (int i = 0; i < StudioWidgetStyleComboBox.Items.Count; i++)
            {
                if (StudioWidgetStyleComboBox.Items[i] is ComboBoxItem item && item.Tag?.ToString() == styleTag)
                {
                    StudioWidgetStyleComboBox.SelectedIndex = i;
                    break;
                }
            }
        }

        UpdateWidgetCardsVisuals(styleTag);
        AutoSaveSettings();
    }

    private void UpdateWidgetCardsVisuals(string selectedTag)
    {
        var accentBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
        var subtleBorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"];

        if (CardWidgetBryanC != null)
        {
            CardWidgetBryanC.BorderBrush = selectedTag == "BryanCDynamic" ? accentBrush : subtleBorderBrush;
            CardWidgetBryanC.BorderThickness = selectedTag == "BryanCDynamic" ? new Thickness(2) : new Thickness(1);
            if (BadgeWidgetBryanC != null) BadgeWidgetBryanC.Visibility = selectedTag == "BryanCDynamic" ? Visibility.Visible : Visibility.Collapsed;
        }
        if (CardWidgetGlassCard != null)
        {
            CardWidgetGlassCard.BorderBrush = selectedTag == "GlassCard" ? accentBrush : subtleBorderBrush;
            CardWidgetGlassCard.BorderThickness = selectedTag == "GlassCard" ? new Thickness(2) : new Thickness(1);
            if (BadgeWidgetGlassCard != null) BadgeWidgetGlassCard.Visibility = selectedTag == "GlassCard" ? Visibility.Visible : Visibility.Collapsed;
        }
        if (CardWidgetCompact != null)
        {
            CardWidgetCompact.BorderBrush = selectedTag == "Compact" ? accentBrush : subtleBorderBrush;
            CardWidgetCompact.BorderThickness = selectedTag == "Compact" ? new Thickness(2) : new Thickness(1);
            if (BadgeWidgetCompact != null) BadgeWidgetCompact.Visibility = selectedTag == "Compact" ? Visibility.Visible : Visibility.Collapsed;
        }
        if (CardWidgetMiniIsland != null)
        {
            CardWidgetMiniIsland.BorderBrush = selectedTag == "MiniIsland" ? accentBrush : subtleBorderBrush;
            CardWidgetMiniIsland.BorderThickness = selectedTag == "MiniIsland" ? new Thickness(2) : new Thickness(1);
            if (BadgeWidgetMiniIsland != null) BadgeWidgetMiniIsland.Visibility = selectedTag == "MiniIsland" ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
