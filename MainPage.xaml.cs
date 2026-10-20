using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using WeatherApp.Helpers;
using WeatherApp.Models;
using WeatherApp.Services;
using WeatherApp.ViewModels;
using WeatherApp.Views.Tabs;

namespace WeatherApp;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }

    private WidgetWindow? _widgetWindow;
    private bool _isDialogOpen = false;

    // Full HD Share Card Cached fields
    private byte[]? _cachedSharePixels;
    private uint _cachedShareWidth;
    private uint _cachedShareHeight;
    private bool _isRenderingShareCard;

    public MainPage()
    {
        var httpClient = new HttpClient();
        var weatherService = new WeatherService(httpClient);
        var locationService = new LocationService(httpClient);
        var adviceService = new WeatherAdviceService();
        var settingsService = new SettingsService();

        ViewModel = new MainViewModel(weatherService, locationService, adviceService, settingsService);
        DataContext = ViewModel;

        InitializeComponent();

        this.KeyDown += (s, e) =>
        {
            if (e.Key == Windows.System.VirtualKey.F11)
            {
                ViewModel.ToggleZenModeCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Windows.System.VirtualKey.Escape && ViewModel.IsZenModeOpen)
            {
                ViewModel.CloseZenModeCommand.Execute(null);
                e.Handled = true;
            }
        };

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Sync theme combo box in top bar
            if (ThemeComboBox != null)
            {
                ThemeComboBox.SelectedIndex = ViewModel.Settings.ThemeMode switch
                {
                    "light" => 0,
                    "dark" => 1,
                    _ => 2
                };
            }
            UpdateThemeComboBoxLabels();
            ApplyDesignLanguageVisuals(ViewModel?.SelectedDesignLanguage);
            LocalizationService.Instance.LanguageChanged += (s, ev) => UpdateThemeComboBoxLabels();
            // Đồng bộ theme ngay khi ThemeHelper thay đổi hoặc hệ thống đổi Light/Dark
            ThemeHelper.ThemeChanged += (theme) =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    this.RequestedTheme = theme;
                    ApplyDesignLanguageVisuals(ViewModel?.SelectedDesignLanguage);
                    UpdateSeasonalThemeButtonStyle();
                    TabOverview?.UpdateNothingThemeVisuals();
                    TabOverview?.UpdateWeatherVisuals();
                    TabOverview?.RedrawCanvases();
                });
            };
            this.ActualThemeChanged += (s, e) =>
            {
                ApplyDesignLanguageVisuals(ViewModel?.SelectedDesignLanguage);
                TabOverview?.UpdateNothingThemeVisuals();
                TabOverview?.UpdateWeatherVisuals();
                TabOverview?.RedrawCanvases();
            };
            ApplyDesignLanguageVisuals(ViewModel?.SelectedDesignLanguage);

        // Nạp dữ liệu thời tiết
            if (ViewModel != null)
            {
                await ViewModel.InitializeAsync();

        // Khởi tạo khay hệ thống (System Tray Icon)
                if (App.MainWindow != null && ViewModel.TrayService == null)
                {
                    var trayService = new TrayIconService();
                    trayService.Initialize(App.MainWindow, ViewModel, () => DesktopWidgetButton_Click(this, new RoutedEventArgs()));
                    ViewModel.TrayService = trayService;
                }

                // Tự động hiển thị Changelog nếu vừa cập nhật phiên bản mới
                CheckAndShowChangelog();

        // Lắng nghe sự kiện đổi chủ đề Mùa/Lễ hội để đồng bộ hạt và ánh sáng Zen Mode
                ViewModel.SeasonalSkinChanged += (s, skin) =>
                {
                    UpdateZenGradientColor();
                    UpdateSeasonalThemeButtonStyle();
                    TabOverview?.UpdateNothingThemeVisuals();
                    TabOverview?.UpdateWeatherVisuals();
                    TabOverview?.RedrawCanvases();
                    if (_zenEffectRenderer != null && skin != null)
                    {
                        _zenEffectRenderer.SetSeasonalParticleType(skin.ParticleType, ViewModel.EnableSeasonalParticles);
                    }
                };

                ActualThemeChanged += (s, e) =>
                {
                    UpdateSeasonalThemeButtonStyle();
                    TabOverview?.UpdateNothingThemeVisuals();
                    TabOverview?.UpdateWeatherVisuals();
                    TabOverview?.RedrawCanvases();
                };

                // Hỗ trợ kiểm thử skin qua command-line (--skin=tet, midautumn, christmas, halloween, spring, summer, autumn, winter, auto)
                try
                {
                    var skinArg = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith("--skin=", StringComparison.OrdinalIgnoreCase));
                    if (skinArg != null)
                    {
                        string targetSkin = skinArg.Substring(7).Trim();
                        ViewModel.ChangeSeasonalSkin(targetSkin);
                    }
                }
                catch { }

                // Hỗ trợ kiểm thử Theme qua command-line (--theme=light, dark, system)
                try
                {
                    var themeArg = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith("--theme=", StringComparison.OrdinalIgnoreCase));
                    if (themeArg != null)
                    {
                        string targetTheme = themeArg.Substring(8).Trim().ToLowerInvariant();
                        ViewModel.ChangeTheme(targetTheme);
                        if (ThemeComboBox != null)
                        {
                            ThemeComboBox.SelectedIndex = targetTheme == "light" ? 0 : (targetTheme == "dark" ? 1 : 2);
                        }
                        UpdateSeasonalThemeButtonStyle();
                    }
                }
                catch { }

                // Hỗ trợ cuộn trang qua command-line (--scroll=hourly, bottom)
                try
                {
                    var scrollArg = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith("--scroll=", StringComparison.OrdinalIgnoreCase));
                    if (scrollArg != null)
                    {
                        string targetScroll = scrollArg.Substring(9).Trim().ToLowerInvariant();
                        if (targetScroll == "hourly")
                        {
                            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                            {
                                TabOverview?.ScrollToHourlySection();
                            });
                        }
                        else if (targetScroll == "seasonal" || targetScroll == "bottom")
                        {
                            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                            {
                                TabOverview?.ScrollToHourlySection();
                                (FindName(nameof(TabWidgetStudio)) as WidgetStudioTab)?.ScrollToSeasonalSection();
                            });
                        }
                    }
                }
                catch { }

                // Hỗ trợ điều hướng Tab qua command-line (--tab=overview, flood, radar, lifestyle, calendar, widget, zen)
                try
                {
                    var tabArg = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith("--tab=", StringComparison.OrdinalIgnoreCase));
                    if (tabArg != null)
                    {
                        string targetTab = tabArg.Substring(6).Trim().ToLowerInvariant();
                        if (targetTab == "zen")
                        {
                            ViewModel.ToggleZenModeCommand.Execute(null);
                        }
                        else
                        {
                            ViewModel.SwitchNav(targetTab);
                            if (MainNavView != null)
                            {
                                var allItems = MainNavView.MenuItems.OfType<NavigationViewItem>().Concat(MainNavView.FooterMenuItems.OfType<NavigationViewItem>());
                                foreach (var item in allItems)
                                {
                                    if (item.Tag is string t && t.Equals(targetTab, StringComparison.OrdinalIgnoreCase))
                                    {
                                        MainNavView.SelectedItem = item;
                                        break;
                                    }
                                }
                            }

                            switch (targetTab)
                            {
                                case "flood":
                                    var floodTab = FindName(nameof(TabFlood)) as UrbanFloodTab;
                                    floodTab?.RenderTidalSineWave();
                                    break;
                                case "radar":
                                    var radarTab = FindName(nameof(TabRadar)) as RadarTab;
                                    radarTab?.StartRadarSweep();
                                    break;
                                case "lifestyle":
                                    var lifestyleTab = FindName(nameof(TabLifestyle)) as LifestyleTab;
                                    lifestyleTab?.UpdateOccasionButtonsVisual();
                                    lifestyleTab?.UpdateGenderButtonsVisual();
                                    break;
                                case "calendar":
                                    _ = FindName(nameof(TabCalendar)) as CalendarTab;
                                    break;
                                case "widget":
                                    var wsTab = FindName(nameof(TabWidgetStudio)) as WidgetStudioTab;
                                    if (Environment.CommandLine.Contains("--scroll=bento", StringComparison.OrdinalIgnoreCase))
                                    {
                                        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                                        {
                                            wsTab?.ScrollToBentoSection();
                                        });
                                    }
                                    else if (Environment.CommandLine.Contains("--scroll=seasonal", StringComparison.OrdinalIgnoreCase) ||
                                        Environment.CommandLine.Contains("--scroll=bottom", StringComparison.OrdinalIgnoreCase))
                                    {
                                        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                                        {
                                            wsTab?.ScrollToSeasonalSection();
                                        });
                                    }
                                    break;
                                case "settings":
                                    var settingsTab = FindName(nameof(TabSettings)) as SettingsTab;
                                    settingsTab?.SyncAllSettings();
                                    break;
                            }
                        }
                    }

                    string fullCmd = Environment.CommandLine;
                    if (fullCmd.Contains("--design=", StringComparison.OrdinalIgnoreCase))
                    {
                        int dIdx = fullCmd.IndexOf("--design=", StringComparison.OrdinalIgnoreCase);
                        int endIdx = fullCmd.IndexOf(" ", dIdx);
                        string dMode = (endIdx > dIdx ? fullCmd.Substring(dIdx + 9, endIdx - dIdx - 9) : fullCmd.Substring(dIdx + 9)).Trim().Trim('"');
                        ViewModel.ApplyDesignLanguage(dMode, save: false);
                        (App.MainWindow as MainWindow)?.UpdateWindowTheme();
                    }
                    if (fullCmd.Contains("--theme=", StringComparison.OrdinalIgnoreCase))
                    {
                        int tIdx = fullCmd.IndexOf("--theme=", StringComparison.OrdinalIgnoreCase);
                        int endIdx = fullCmd.IndexOf(" ", tIdx);
                        string tMode = (endIdx > tIdx ? fullCmd.Substring(tIdx + 8, endIdx - tIdx - 8) : fullCmd.Substring(tIdx + 8)).Trim().Trim('"');
                        ViewModel.ChangeTheme(tMode);
                        if (ThemeComboBox != null)
                        {
                            ThemeComboBox.SelectedIndex = tMode.ToLowerInvariant() switch
                            {
                                "light" => 0,
                                "dark" => 1,
                                _ => 2
                            };
                        }
                        ApplyDesignLanguageVisuals(ViewModel.SelectedDesignLanguage);
                        (App.MainWindow as MainWindow)?.UpdateWindowTheme();
                    }
                    if (fullCmd.Contains("--openwidget", StringComparison.OrdinalIgnoreCase))
                    {
                        string skinMode = "Auto";
                        int skinIdx = fullCmd.IndexOf("--skin=", StringComparison.OrdinalIgnoreCase);
                        if (skinIdx >= 0)
                        {
                            int endIdx = fullCmd.IndexOf(' ', skinIdx);
                            skinMode = (endIdx > skinIdx ? fullCmd.Substring(skinIdx + 7, endIdx - skinIdx - 7) : fullCmd.Substring(skinIdx + 7)).Trim().Trim('"');
                        }

                        string? widgetStyle = null;
                        int styleIdx = fullCmd.IndexOf("--style=", StringComparison.OrdinalIgnoreCase);
                        if (styleIdx >= 0)
                        {
                            int endIdx = fullCmd.IndexOf(' ', styleIdx);
                            widgetStyle = (endIdx > styleIdx ? fullCmd.Substring(styleIdx + 8, endIdx - styleIdx - 8) : fullCmd.Substring(styleIdx + 8)).Trim().Trim('"');
                        }

                        if (_widgetWindow == null)
                        {
                            _widgetWindow = new WidgetWindow(ViewModel);
                            _widgetWindow.Closed += (s, args) => _widgetWindow = null;
                        }

                        if (!string.IsNullOrEmpty(widgetStyle))
                        {
                            _widgetWindow.ApplyWidgetStyle(widgetStyle);
                        }
                        else
                        {
                            var skin = Services.SeasonalSkinService.Instance.GetActiveSkin(skinMode);
                            _widgetWindow.ApplySeasonalTheme(skin);
                        }
                        _widgetWindow.Activate();
                    }
                }
                catch { }
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
            if (SeasonalThemeBtnText != null) SeasonalThemeBtnText.Visibility = Visibility.Collapsed;
        }
        else
        {
            if (BtnTextWidget != null) BtnTextWidget.Visibility = Visibility.Visible;
            if (SeasonalThemeBtnText != null) SeasonalThemeBtnText.Visibility = Visibility.Visible;
        }

        if (width < 1140)
        {
            if (AppSubtitleText != null) AppSubtitleText.Visibility = Visibility.Collapsed;
            if (ThemeComboBox != null) ThemeComboBox.Width = width < 980 ? 116 : 124;
        }
        else
        {
            if (AppSubtitleText != null) AppSubtitleText.Visibility = Visibility.Visible;
            if (ThemeComboBox != null) ThemeComboBox.Width = 124;
        }

        // Tự co giãn MaxWidth ô tìm kiếm khi cửa sổ hẹp
        if (LocationSearchBox != null)
        {
            LocationSearchBox.MaxWidth = width < 950 ? 280 : (width < 1140 ? 380 : 520);
        }

        // 2. Tinh chỉnh lề ngoài gọn gàng trên màn hình nhỏ (MainContentGrid)
        if (MainContentGrid != null)
        {
            var targetMargin = width < 1250 ? new Thickness(14, 10, 14, 14) : new Thickness(24, 12, 24, 24);
            if (MainContentGrid.Margin != targetMargin)
            {
                MainContentGrid.Margin = targetMargin;
            }
        }
    }

    private bool _isShareDialogOpen = false;

    private WeatherEffectRenderer? _zenEffectRenderer;

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.CurrentWeather))
        {
            if (_isShareDialogOpen)
            {
                UpdateShareCardContent();
            }
            if (ViewModel.IsZenModeOpen && _zenEffectRenderer != null && ViewModel.CurrentWeather != null)
            {
                _zenEffectRenderer.SetWind(ViewModel.CurrentWeather.WindSpeedValue);
                _zenEffectRenderer.SetWeatherEffect(ViewModel.CurrentWeather.WeatherEffect);
                if (ViewModel.CurrentSeasonalSkin != null)
                {
                    _zenEffectRenderer.SetSeasonalParticleType(ViewModel.CurrentSeasonalSkin.ParticleType, ViewModel.EnableSeasonalParticles);
                }
                UpdateZenGradientColor();
            }
        }
        else if (e.PropertyName == nameof(MainViewModel.SelectedDesignLanguage))
        {
            ApplyDesignLanguageVisuals(ViewModel.SelectedDesignLanguage);
        }
        else if (e.PropertyName == nameof(MainViewModel.IsZenModeOpen))
        {
            if (ViewModel.IsZenModeOpen)
            {
                try
                {
                    App.MainWindow?.AppWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);
                }
                catch { }

                UpdateZenGradientColor();
                ViewModel?.UpdateZenHourlyForecast();
                _zenEffectRenderer ??= new WeatherEffectRenderer(ZenWeatherCanvas, ZenLightningOverlay);
                if (ViewModel?.CurrentWeather != null && _zenEffectRenderer != null)
                {
                    _zenEffectRenderer.SetWind(ViewModel.CurrentWeather.WindSpeedValue);
                    _zenEffectRenderer.SetWeatherEffect(ViewModel.CurrentWeather.WeatherEffect);
                    if (ViewModel.CurrentSeasonalSkin != null)
                    {
                        _zenEffectRenderer.SetSeasonalParticleType(ViewModel.CurrentSeasonalSkin.ParticleType, ViewModel.EnableSeasonalParticles);
                    }
                    _zenEffectRenderer.Resume();
                }
            }
            else
            {
                try
                {
                    App.MainWindow?.AppWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.Default);
                }
                catch { }
                _zenEffectRenderer?.Pause();
            }
        }
        else if (e.PropertyName == nameof(MainViewModel.CurrentSeasonalSkin) ||
                 e.PropertyName == nameof(MainViewModel.EnableSeasonalParticles) ||
                 e.PropertyName == nameof(MainViewModel.SeasonalGreetingText))
        {
            TabOverview?.UpdateWeatherVisuals();
            TabOverview?.RedrawCanvases();
            if (ViewModel.IsZenModeOpen && _zenEffectRenderer != null && ViewModel.CurrentSeasonalSkin != null)
            {
                _zenEffectRenderer.SetSeasonalParticleType(ViewModel.CurrentSeasonalSkin.ParticleType, ViewModel.EnableSeasonalParticles);
                UpdateZenGradientColor();
            }
        }
    }

    private void ZenAmbientToggle_Toggled(object sender, RoutedEventArgs e)
    {
        ViewModel?.ToggleAmbientSoundCommand.Execute(null);
    }

    private void ZenToggleSeconds_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts && ViewModel != null && ts.IsOn != ViewModel.ZenShowSeconds)
            ViewModel.ToggleZenShowSecondsCommand.Execute(null);
    }

    private void ZenToggleWeatherInfo_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts && ViewModel != null && ts.IsOn != ViewModel.ZenShowWeatherInfo)
            ViewModel.ToggleZenShowWeatherInfoCommand.Execute(null);
    }

    private void ZenToggleHourly_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts && ViewModel != null && ts.IsOn != ViewModel.ZenShowHourlyForecast)
            ViewModel.ToggleZenShowHourlyForecastCommand.Execute(null);
    }

    private void ZenToggleAirQuality_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts && ViewModel != null && ts.IsOn != ViewModel.ZenShowAirQuality)
            ViewModel.ToggleZenShowAirQualityCommand.Execute(null);
    }

    private void ZenToggleQuote_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts && ViewModel != null && ts.IsOn != ViewModel.ZenShowRelaxQuote)
            ViewModel.ToggleZenShowRelaxQuoteCommand.Execute(null);
    }

    private void ZenToggleAmbient_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts && ViewModel != null && ts.IsOn != ViewModel.ZenShowAmbientSound)
            ViewModel.ToggleZenShowAmbientSoundCommand.Execute(null);
    }

    private void UpdateZenGradientColor()
    {
        if (ZenGradientOverlay != null && ViewModel != null)
        {
            try
            {
                string hex = ViewModel.CurrentSeasonalSkin?.PrimaryAccentHex?.TrimStart('#')
                    ?? ViewModel.CurrentWeather?.HeroGradientStart?.TrimStart('#')
                    ?? "38BDF8";

                if (hex.Length >= 6)
                {
                    byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                    byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                    byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                    var brush = new Microsoft.UI.Xaml.Media.LinearGradientBrush
                    {
                        StartPoint = new Windows.Foundation.Point(0, 0),
                        EndPoint = new Windows.Foundation.Point(1, 1)
                    };
                    brush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = Windows.UI.Color.FromArgb(170, r, g, b), Offset = 0 });
                    brush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = Windows.UI.Color.FromArgb(255, 9, 13, 22), Offset = 1 });
                    ZenGradientOverlay.Background = brush;
                }
            }
            catch { }
        }
    }

    private void UpdateSeasonalThemeButtonStyle()
    {
        if (SeasonalThemeButton == null || ViewModel?.CurrentSeasonalSkin == null) return;
        var skin = ViewModel.CurrentSeasonalSkin;
        bool isDark = ThemeHelper.IsDarkTheme(this);
        this.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;

        string bgHex = (!isDark && !string.IsNullOrEmpty(skin.LightBadgeBackgroundHex))
            ? skin.LightBadgeBackgroundHex
            : skin.BadgeBackgroundHex;
        string fgHex = (!isDark && !string.IsNullOrEmpty(skin.LightBadgeForegroundHex))
            ? skin.LightBadgeForegroundHex
            : skin.BadgeForegroundHex;
        string borderHex = skin.BorderStrokeHex ?? "#33FFFFFF";

        SeasonalThemeButton.Background = HexToBrush(bgHex, Windows.UI.Color.FromArgb(40, 225, 29, 72));
        SeasonalThemeButton.BorderBrush = HexToBrush(borderHex, Windows.UI.Color.FromArgb(90, 225, 29, 72));
        if (SeasonalThemeBtnText != null)
        {
            SeasonalThemeBtnText.Foreground = HexToBrush(fgHex, Windows.UI.Color.FromArgb(255, 159, 18, 57));
        }
    }

    private static Microsoft.UI.Xaml.Media.SolidColorBrush HexToBrush(string hex, Windows.UI.Color fallback)
    {
        try
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 6)
            {
                byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                return new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, r, g, b));
            }
            if (hex.Length == 8)
            {
                byte a = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                byte r = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                byte g = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                byte b = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
                return new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(a, r, g, b));
            }
        }
        catch { }
        return new Microsoft.UI.Xaml.Media.SolidColorBrush(fallback);
    }

    private void SeasonalSkinItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string skinMode && ViewModel != null)
        {
            ViewModel.ChangeSeasonalSkin(skinMode);
            if (SeasonalThemeButton?.Flyout != null)
            {
                SeasonalThemeButton.Flyout.Hide();
            }
            UpdateSeasonalThemeButtonStyle();
            TabOverview?.UpdateWeatherVisuals();
            TabOverview?.RedrawCanvases();
        }
    }

    #region Tab Navigation & Lazy Realization

    private void MainNavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        try
        {
            if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
            {
                ViewModel?.SwitchNav(tag);

                switch (tag)
                {
                    case "overview":
                        TabOverview?.UpdateWeatherVisuals();
                        TabOverview?.StartEffects();
                        TabOverview?.RedrawCanvases();
                        (FindName(nameof(TabRadar)) as RadarTab)?.StopRadarSweep();
                        AnimationHelper.SlideUpFadeIn(TabOverview);
                        break;

                    case "flood":
                        TabOverview?.StopEffects();
                        (FindName(nameof(TabRadar)) as RadarTab)?.StopRadarSweep();
                        var floodTab = FindName(nameof(TabFlood)) as UrbanFloodTab;
                        floodTab?.RenderTidalSineWave();
                        AnimationHelper.SlideUpFadeIn(floodTab);
                        break;

                    case "radar":
                        TabOverview?.StopEffects();
                        var radarTab = FindName(nameof(TabRadar)) as RadarTab;
                        radarTab?.StartRadarSweep();
                        AnimationHelper.SlideUpFadeIn(radarTab);
                        break;

                    case "lifestyle":
                        TabOverview?.StopEffects();
                        (FindName(nameof(TabRadar)) as RadarTab)?.StopRadarSweep();
                        var lifestyleTab = FindName(nameof(TabLifestyle)) as LifestyleTab;
                        lifestyleTab?.UpdateOccasionButtonsVisual();
                        lifestyleTab?.UpdateGenderButtonsVisual();
                        AnimationHelper.SlideUpFadeIn(lifestyleTab);
                        break;

                    case "calendar":
                        TabOverview?.StopEffects();
                        (FindName(nameof(TabRadar)) as RadarTab)?.StopRadarSweep();
                        var calendarTab = FindName(nameof(TabCalendar)) as CalendarTab;
                        AnimationHelper.SlideUpFadeIn(calendarTab);
                        break;

                    case "widget":
                        TabOverview?.StopEffects();
                        (FindName(nameof(TabRadar)) as RadarTab)?.StopRadarSweep();
                        var widgetTab = FindName(nameof(TabWidgetStudio)) as WidgetStudioTab;
                        AnimationHelper.SlideUpFadeIn(widgetTab);
                        break;

                    case "settings":
                        TabOverview?.StopEffects();
                        (FindName(nameof(TabRadar)) as RadarTab)?.StopRadarSweep();
                        var settingsTab = FindName(nameof(TabSettings)) as SettingsTab;
                        settingsTab?.SyncAllSettings();
                        AnimationHelper.SlideUpFadeIn(settingsTab);
                        break;
                }
            }
        }
        catch { }
    }

    private async void OverviewTab_ShareRequested(object? sender, EventArgs e)
    {
        if (ViewModel?.CurrentWeather == null) return;
        UpdateShareCardContent();
        if (ShareCardDialog != null && !_isShareDialogOpen)
        {
            _isShareDialogOpen = true;
            ShareCardDialog.XamlRoot = this.XamlRoot;
            _ = RefreshShareCardBitmapAsync();
            try
            {
                await ShareCardDialog.ShowAsync();
            }
            finally
            {
                _isShareDialogOpen = false;
            }
        }
    }

    private void SettingsTab_OpenChangelogRequested(object? sender, EventArgs e)
    {
        OpenChangelogDialog();
    }

    private void SettingsTab_ReopenOnboardingRequested(object? sender, EventArgs e)
    {
        Frame.Navigate(typeof(OnboardingPage));
    }

    #endregion

    #region Top Bar & Search Handlers

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
        if (ViewModel == null) return;
        if (ThemeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            ViewModel.ChangeTheme(tag);
            UpdateSeasonalThemeButtonStyle();
            ApplyDesignLanguageVisuals(ViewModel.SelectedDesignLanguage);
            (App.MainWindow as MainWindow)?.UpdateWindowTheme();
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                TabOverview?.UpdateWeatherVisuals();
                TabOverview?.RedrawCanvases();
            });
        }
    }

    public void ApplyDesignLanguageVisuals(string? designLanguage)
    {
        string dl = designLanguage ?? ViewModel?.SelectedDesignLanguage ?? "Fluent2";
        bool isNothing = string.Equals(dl, "Nothing", StringComparison.OrdinalIgnoreCase);
        bool isDark = ThemeHelper.IsDarkTheme(this);
        this.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;

        var tabs = new Microsoft.UI.Xaml.FrameworkElement?[] { TabOverview, TabFlood, TabRadar, TabLifestyle, TabCalendar, TabWidgetStudio, TabSettings };

        if (isNothing)
        {
            var cardBg = isDark ? Windows.UI.Color.FromArgb(255, 18, 18, 20) : Windows.UI.Color.FromArgb(255, 255, 255, 255);
            var cardBorder = isDark ? Windows.UI.Color.FromArgb(255, 36, 36, 40) : Windows.UI.Color.FromArgb(255, 226, 226, 230);
            var subtleBg = isDark ? Windows.UI.Color.FromArgb(255, 24, 24, 27) : Windows.UI.Color.FromArgb(255, 245, 245, 247);
            var subCardBorder = isDark ? Windows.UI.Color.FromArgb(255, 38, 38, 43) : Windows.UI.Color.FromArgb(255, 229, 229, 232);
            var paneBg = isDark ? Windows.UI.Color.FromArgb(255, 10, 10, 12) : Windows.UI.Color.FromArgb(255, 240, 240, 242);
            var glyphBoxBg = isDark ? Windows.UI.Color.FromArgb(255, 10, 10, 12) : Windows.UI.Color.FromArgb(255, 249, 249, 250);

            // Nothing Red Accents
            var redAccent = Windows.UI.Color.FromArgb(255, 215, 25, 33);
            var textPrimary = isDark ? Windows.UI.Color.FromArgb(255, 255, 255, 255) : Windows.UI.Color.FromArgb(255, 10, 10, 12);
            var textSecondary = isDark ? Windows.UI.Color.FromArgb(255, 136, 136, 136) : Windows.UI.Color.FromArgb(255, 102, 102, 102);
            var textMuted = isDark ? Windows.UI.Color.FromArgb(255, 82, 82, 91) : Windows.UI.Color.FromArgb(255, 161, 161, 170);
            var badgeBg = isDark ? Windows.UI.Color.FromArgb(255, 30, 30, 34) : Windows.UI.Color.FromArgb(255, 240, 240, 242);
            var pillBg = isDark ? Windows.UI.Color.FromArgb(255, 31, 31, 35) : Windows.UI.Color.FromArgb(255, 238, 238, 240);
            var pillBorder = isDark ? Windows.UI.Color.FromArgb(255, 51, 51, 51) : Windows.UI.Color.FromArgb(255, 225, 225, 228);

            // Set on MainPage.Resources
            this.Resources["CardBackgroundFillColorDefaultBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(cardBg);
            this.Resources["CardStrokeColorDefaultBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(cardBorder);
            this.Resources["SubtleFillColorSecondaryBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(subtleBg);
            this.Resources["CardBackgroundFillColorSecondaryBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(subtleBg);
            this.Resources["NavigationViewDefaultPaneBackground"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(paneBg);
            this.Resources["NavigationViewTopPaneBackground"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(paneBg);

            this.Resources["AccentFillColorDefaultBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(redAccent);
            this.Resources["AccentFillColorSecondaryBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 225, 45, 50));
            this.Resources["AccentFillColorTertiaryBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 180, 20, 25));
            this.Resources["AccentButtonBackground"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(redAccent);
            this.Resources["AccentButtonBackgroundPointerOver"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 235, 45, 52));
            this.Resources["AccentButtonBackgroundPressed"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 180, 20, 25));
            this.Resources["SystemAccentColor"] = redAccent;
            this.Resources["NavigationViewSelectionIndicatorForeground"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(redAccent);

            // Nothing OS Tokens on MainPage
            this.Resources["NothingCardBackgroundBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(cardBg);
            this.Resources["NothingCardBorderBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(cardBorder);
            this.Resources["NothingSubCardBackgroundBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(subtleBg);
            this.Resources["NothingSubCardBorderBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(subCardBorder);
            this.Resources["NothingGlyphBoxBackgroundBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(glyphBoxBg);
            this.Resources["NothingTextPrimaryBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(textPrimary);
            this.Resources["NothingTextSecondaryBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(textSecondary);
            this.Resources["NothingTextMutedBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(textMuted);
            this.Resources["NothingRedBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(redAccent);
            this.Resources["NothingGreenBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 0, 255, 102) : Windows.UI.Color.FromArgb(255, 22, 163, 74));
            this.Resources["NothingPillBackgroundBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(pillBg);
            this.Resources["NothingPillBorderBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(pillBorder);

            // Update Header Border
            if (FloatingIslandHeader != null)
            {
                FloatingIslandHeader.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(cardBg);
                FloatingIslandHeader.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(cardBorder);
            }

            // Explicitly style Nav Badges for Nothing Mode (Clean, consistent Nothing OS aesthetic)
            var nBadgeBgBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(badgeBg);
            var nBadgeFgBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(textPrimary);
            var nBadgeRedBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(redAccent);

            if (NavOverviewBadgeBorder != null) NavOverviewBadgeBorder.Background = nBadgeBgBrush;
            if (NavOverviewBadgeText != null) NavOverviewBadgeText.Foreground = nBadgeFgBrush;
            if (NavFloodBadgeBorder != null) NavFloodBadgeBorder.Background = nBadgeBgBrush;
            if (NavFloodBadgeText != null) NavFloodBadgeText.Foreground = nBadgeFgBrush;
            if (NavRadarBadgeBorder != null) NavRadarBadgeBorder.Background = nBadgeBgBrush;
            if (NavRadarBadgeText != null) NavRadarBadgeText.Foreground = isDark ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 255, 102)) : nBadgeRedBrush;
            if (NavLifestyleBadgeBorder != null) NavLifestyleBadgeBorder.Background = nBadgeBgBrush;
            if (NavLifestyleBadgeText != null) NavLifestyleBadgeText.Foreground = nBadgeFgBrush;
            if (NavCalendarBadgeBorder != null) NavCalendarBadgeBorder.Background = nBadgeBgBrush;
            if (NavCalendarBadgeText != null) NavCalendarBadgeText.Foreground = nBadgeFgBrush;
            if (NavWidgetBadgeBorder != null) NavWidgetBadgeBorder.Background = nBadgeBgBrush;
            if (NavWidgetBadgeText != null) NavWidgetBadgeText.Foreground = nBadgeFgBrush;

            // Set on tabs
            foreach (var tab in tabs)
            {
                if (tab != null)
                {
                    tab.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;
                    tab.Resources["CardBackgroundFillColorDefaultBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(cardBg);
                    tab.Resources["CardStrokeColorDefaultBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(cardBorder);
                    tab.Resources["SubtleFillColorSecondaryBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(subtleBg);
                    tab.Resources["AccentFillColorDefaultBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(redAccent);
                    tab.Resources["AccentButtonBackground"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(redAccent);
                    tab.Resources["AccentButtonBackgroundPointerOver"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 235, 45, 52));
                    tab.Resources["AccentButtonBackgroundPressed"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 180, 20, 25));

                    tab.Resources["NothingCardBackgroundBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(cardBg);
                    tab.Resources["NothingCardBorderBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(cardBorder);
                    tab.Resources["NothingSubCardBackgroundBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(subtleBg);
                    tab.Resources["NothingSubCardBorderBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(subCardBorder);
                    tab.Resources["NothingGlyphBoxBackgroundBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(glyphBoxBg);
                    tab.Resources["NothingTextPrimaryBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(textPrimary);
                    tab.Resources["NothingTextSecondaryBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(textSecondary);
                    tab.Resources["NothingTextMutedBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(textMuted);
                    tab.Resources["NothingPillBackgroundBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(pillBg);
                    tab.Resources["NothingPillBorderBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(pillBorder);
                }
            }
            TabOverview?.UpdateNothingThemeVisuals();
        }
        else
        {
            // Fluent 2 Mode: Restore authentic Fluent 2 theme brushes locally on this and tabs
            // NEVER mutate or remove from Application.Current.Resources!
            var fluentKeys = new string[]
            {
                "CardBackgroundFillColorDefaultBrush",
                "CardStrokeColorDefaultBrush",
                "SubtleFillColorSecondaryBrush",
                "CardBackgroundFillColorSecondaryBrush",
                "NavigationViewDefaultPaneBackground",
                "NavigationViewTopPaneBackground",
                "AccentFillColorDefaultBrush",
                "AccentFillColorSecondaryBrush",
                "AccentFillColorTertiaryBrush",
                "AccentButtonBackground",
                "AccentButtonBackgroundPointerOver",
                "AccentButtonBackgroundPressed",
                "SystemAccentColor",
                "NavigationViewSelectionIndicatorForeground",
                "NothingCardBackgroundBrush",
                "NothingCardBorderBrush",
                "NothingSubCardBackgroundBrush",
                "NothingSubCardBorderBrush",
                "NothingGlyphBoxBackgroundBrush",
                "NothingTextPrimaryBrush",
                "NothingTextSecondaryBrush",
                "NothingTextMutedBrush",
                "NothingPillBackgroundBrush",
                "NothingPillBorderBrush"
            };

            foreach (var k in fluentKeys)
            {
                this.Resources.Remove(k);
            }

            if (FloatingIslandHeader != null)
            {
                FloatingIslandHeader.ClearValue(Border.BackgroundProperty);
                FloatingIslandHeader.ClearValue(Border.BorderBrushProperty);
            }

            // Restore vibrant Fluent 2 navigation badges
            if (NavOverviewBadgeBorder != null) NavOverviewBadgeBorder.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 12, 74, 110) : Windows.UI.Color.FromArgb(255, 224, 242, 254));
            if (NavOverviewBadgeText != null) NavOverviewBadgeText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 56, 189, 248) : Windows.UI.Color.FromArgb(255, 2, 132, 199));

            if (NavFloodBadgeBorder != null) NavFloodBadgeBorder.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 12, 74, 110) : Windows.UI.Color.FromArgb(255, 224, 242, 254));
            if (NavFloodBadgeText != null) NavFloodBadgeText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 56, 189, 248) : Windows.UI.Color.FromArgb(255, 2, 132, 199));

            if (NavRadarBadgeBorder != null) NavRadarBadgeBorder.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 6, 78, 59) : Windows.UI.Color.FromArgb(255, 220, 252, 231));
            if (NavRadarBadgeText != null) NavRadarBadgeText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 52, 211, 153) : Windows.UI.Color.FromArgb(255, 22, 163, 74));

            if (NavLifestyleBadgeBorder != null) NavLifestyleBadgeBorder.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 112, 26, 117) : Windows.UI.Color.FromArgb(255, 252, 231, 243));
            if (NavLifestyleBadgeText != null) NavLifestyleBadgeText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 244, 114, 182) : Windows.UI.Color.FromArgb(255, 219, 39, 119));

            if (NavCalendarBadgeBorder != null) NavCalendarBadgeBorder.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 76, 29, 149) : Windows.UI.Color.FromArgb(255, 237, 233, 254));
            if (NavCalendarBadgeText != null) NavCalendarBadgeText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 167, 139, 250) : Windows.UI.Color.FromArgb(255, 124, 58, 237));

            if (NavWidgetBadgeBorder != null) NavWidgetBadgeBorder.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 30, 41, 59) : Windows.UI.Color.FromArgb(255, 241, 245, 249));
            if (NavWidgetBadgeText != null) NavWidgetBadgeText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 148, 163, 184) : Windows.UI.Color.FromArgb(255, 100, 116, 139));

            foreach (var tab in tabs)
            {
                if (tab != null)
                {
                    tab.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;
                    foreach (var k in fluentKeys)
                    {
                        tab.Resources.Remove(k);
                    }
                }
            }

            DispatcherQueue.TryEnqueue(() =>
            {
                TabOverview?.UpdateWeatherVisuals();
                TabOverview?.RedrawCanvases();
            });
        }
    }
    private void UpdateThemeComboBoxLabels()
    {
        bool isVi = LocalizationService.Instance.IsVietnamese;
        if (ThemeComboBox != null && ThemeComboBox.Items.Count >= 3)
        {
            ((ComboBoxItem)ThemeComboBox.Items[0]).Content = isVi ? "\u2600\uFE0F S\u00E1ng" : "\u2600\uFE0F Light";
            ((ComboBoxItem)ThemeComboBox.Items[1]).Content = isVi ? "\uD83C\uDF19 T\u1ED1i" : "\uD83C\uDF19 Dark";
            ((ComboBoxItem)ThemeComboBox.Items[2]).Content = isVi ? "\uD83D\uDCBB H\u1EC7 th\u1ED1ng" : "\uD83D\uDCBB System";
        }
    }

    private async void QuickLocationChip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is QuickLocationChipItem chip && ViewModel != null)
        {
            await ViewModel.SelectQuickChipAsync(chip);
        }
    }

    #endregion

    #region Desktop Widget Windows

    private void DesktopWidgetButton_Click(object sender, RoutedEventArgs e)
    {
        OpenCurrentCityWidget_Click(sender, e);
    }

    private void OpenCurrentCityWidget_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        string chosenStyle = ViewModel.Settings.WidgetStyle ?? "BryanCDynamic";
        double chosenOpacity = ViewModel.Settings.WidgetOpacity;
        if (chosenOpacity <= 0.05) chosenOpacity = 1.0;

        if (_widgetWindow == null)
        {
            _widgetWindow = new WidgetWindow(ViewModel);
            _widgetWindow.Closed += (s, args) => _widgetWindow = null;
        }

        _widgetWindow.ApplyWidgetStyle(chosenStyle);
        _widgetWindow.SetOpacity(chosenOpacity);
        _widgetWindow.Activate();
    }

    private async void OpenCustomCityWidget_Click(object sender, RoutedEventArgs e)
    {
        bool isVi = LocalizationService.Instance.IsVietnamese;
        var inputTextBox = new TextBox
        {

            PlaceholderText = isVi ? "Nh\u1EADp t\u00EAn th\u00E0nh ph\u1ED1 (VD: \u0110\u00E0 N\u1EB5ng, Tokyo, Paris, New York...)" : "Enter city name (e.g. Da Nang, Tokyo, Paris, New York...)",
            Height = 38
        };
        var dialog = new ContentDialog
        {
            Title = isVi ? "\u2795 T\u1EA1o Widget M\u1EDBi Cho Th\u00E0nh Ph\u1ED1 Kh\u00E1c" : "\u2795 Create New Widget For Another City",
            Content = inputTextBox,
            PrimaryButtonText = isVi ? "T\u1EA1o Widget" : "Create Widget",
            CloseButtonText = isVi ? "H\u1EE7y" : "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(inputTextBox.Text))
        {
            string city = inputTextBox.Text.Trim();
            string chosenStyle = ViewModel?.Settings.WidgetStyle ?? "BryanCDynamic";
            double chosenOpacity = ViewModel?.Settings.WidgetOpacity ?? 1.0;
            if (chosenOpacity <= 0.05) chosenOpacity = 1.0;

            try
            {
                var locService = new LocationService();
                var locs = await locService.SearchLocationsAsync(city);
                if (locs != null && locs.Count > 0)
                {
                    var first = locs[0];
                    var widget = new WidgetWindow(ViewModel, $"{first.Name}, {first.Country}", first.Latitude, first.Longitude);
                    widget.ApplyWidgetStyle(chosenStyle);
                    widget.SetOpacity(chosenOpacity);
                    widget.Activate();
                }
                else
                {
                    var widget = new WidgetWindow(ViewModel, city, 21.0285, 105.8542);
                    widget.ApplyWidgetStyle(chosenStyle);
                    widget.SetOpacity(chosenOpacity);
                    widget.Activate();
                }
            }
            catch
            {
                var widget = new WidgetWindow(ViewModel, city, 21.0285, 105.8542);
                widget.ApplyWidgetStyle(chosenStyle);
                widget.SetOpacity(chosenOpacity);
                widget.Activate();
            }
        }
    }

    private void CloseAllWidgets_Click(object sender, RoutedEventArgs e)
    {
        WidgetWindow.CloseAllWidgets();
        _widgetWindow = null;
    }

    #endregion

    #region Full HD Share Card Export

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

        bool isVi = LocalizationService.Instance.IsVietnamese;
        string location = ViewModel.LocationTitle.ToUpper();
        string nowTime = isVi ? $"{DateTime.Now:dddd, dd/MM/yyyy \u2022 HH:mm}" : $"{DateTime.Now:dddd, MM/dd/yyyy \u2022 HH:mm}";
        string lunar = isVi ? $"\\u00C2m l\\u1ECBch: {w.LunarDateText} \\u2022 {w.SolarTermText}" : $"Lunar: {w.LunarDateText} \\u2022 {w.SolarTermText}";
        string temp = w.TemperatureText.Replace("C", "").Trim();
        string feelsLike = $"{w.FeelsLikeText} \u2022 {w.MinMaxText}";
        string uv = $"{w.UvIndexText} - {w.UvIndexDescription}";
        string aqi = $"{w.AqiValue} - {w.AqiDescription}";
        string rain = w.RainProbabilityText;
        string wind = $"{w.WindText} {w.WindDirectionText}";
        string humidity = w.HumidityText;
        string sun = $"{w.SunriseText} \u2022 {w.SunsetText}";

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
            if (FhdLandscapeOotdTitle != null) FhdLandscapeOotdTitle.Text = isVi ? $"H\u00D4M NAY M\u1EB6C G\u00CC? (OOTD ADVISOR) \u2014 {o.OccasionTitle.ToUpper()}" : $"WHAT TO WEAR TODAY? (OOTD ADVISOR) \u2014 {o.OccasionTitle.ToUpper()}";
            if (FhdLandscapeThermalTag != null) FhdLandscapeThermalTag.Text = o.ThermalComfortNotice;
            if (FhdLandscapeOotdHeadline != null) FhdLandscapeOotdHeadline.Text = o.Headline;
            if (FhdLandscapeOotdClothing != null) FhdLandscapeOotdClothing.Text = $"{o.TopClothing}, {o.BottomClothing}, {o.Footwear}";
            if (FhdLandscapeOotdAccessories != null) FhdLandscapeOotdAccessories.Text = string.Join(" \u2022 ", o.Accessories.Select(a => $"{a.Name}"));
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
        if (FhdStoryDateText != null) FhdStoryDateText.Text = isVi ? $"{DateTime.Now:dd/MM/yyyy \u2022 HH:mm}" : $"{DateTime.Now:MM/dd/yyyy \u2022 HH:mm}";
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
            if (FhdStoryOotdTitle != null) FhdStoryOotdTitle.Text = isVi ? $"\uD83D\uDC57 H\u00D4M NAY M\u1EB6C G\u00CC? \u2014 {o.OccasionTitle.ToUpper()}" : $"\uD83D\uDC57 WHAT TO WEAR TODAY? \u2014 {o.OccasionTitle.ToUpper()}";
            if (FhdStoryOotdHeadline != null) FhdStoryOotdHeadline.Text = o.Headline;
            var accList = string.Join(" \u2022 ", o.Accessories.Select(a => a.Name));
            if (FhdStoryOotdAccessories != null) FhdStoryOotdAccessories.Text = isVi ? $"\uD83C\uDF92 Ph\u1EE5 ki\u1EC7n: {accList}" : $"\uD83C\uDF92 Accessories: {accList}";
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
                var isVi = LocalizationService.Instance.IsVietnamese;
                dataPackage.SetText(isVi
                    ? $"\uD83C\uDF24\uFE0F D\u1EF1 b\u00E1o th\u1EDDi ti\u1EBFt {ViewModel.LocationTitle}: {ViewModel.CurrentWeather?.TemperatureText}, {ViewModel.CurrentWeather?.ConditionText} (Th\u1EBB Full HD {outWidth}x{outHeight} t\u1EEB Weather WinUI v3.5.0)"
                    : $"\uD83C\uDF24\uFE0F Weather Forecast {ViewModel.LocationTitle}: {ViewModel.CurrentWeather?.TemperatureText}, {ViewModel.CurrentWeather?.ConditionText} (Full HD Card {outWidth}x{outHeight} from Weather WinUI v3.5.0)");
                dataPackage.RequestedOperation = DataPackageOperation.Copy;
                Clipboard.SetContent(dataPackage);

                ShareCardDialog?.Hide();
                if (ShareSuccessInfoBar != null)
                {
                    ShareSuccessInfoBar.Title = isVi ? "\u0110\u00E3 sao ch\u00E9p \u1EA3nh Full HD th\u00E0nh c\u00F4ng!" : "Full HD image copied successfully!";
                    ShareSuccessInfoBar.Message = isVi
                        ? $"\u1EA2nh th\u1EDDi ti\u1EBFt {outWidth}x{outHeight} si\u00EAu n\u00E9t \u0111\u00E3 l\u01B0u v\u00E0o Clipboard. B\u1EA1n c\u00F3 th\u1EC3 nh\u1EA5n Ctrl+V \u0111\u1EC3 d\u00E1n tr\u1EF1c ti\u1EBFp v\u00E0o Zalo, Facebook, Messenger."
                        : $"Ultra-sharp {outWidth}x{outHeight} weather card copied to Clipboard. You can press Ctrl+V to paste directly into social apps or chats.";
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
                        var isVi = LocalizationService.Instance.IsVietnamese;
                        ShareSuccessInfoBar.Title = isVi ? "\u0110\u00E3 l\u01B0u \u1EA3nh Full HD th\u00E0nh c\u00F4ng!" : "Full HD image saved successfully!";
                        ShareSuccessInfoBar.Message = isVi
                            ? $"File \u1EA3nh ch\u1EA5t l\u01B0\u1EE3ng cao {outWidth}x{outHeight}px \u0111\u00E3 l\u01B0u t\u1EA1i: {file.Path}."
                            : $"High quality {outWidth}x{outHeight}px image saved to: {file.Path}.";
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

    #region Changelog Dialog & Version Tracking

    private void CheckAndShowChangelog()
    {
        string currentVersion = "3.5.0";
        string lastSeen = ViewModel.Settings.LastSeenVersion ?? string.Empty;

        if (string.IsNullOrEmpty(lastSeen) || lastSeen != currentVersion)
        {
            ViewModel.Settings.LastSeenVersion = currentVersion;
            ViewModel.ApplySettings();

            _ = Task.Delay(1000).ContinueWith(_ =>
            {
                DispatcherQueue.TryEnqueue(async () =>
                {
                    await ShowChangelogDialogAsync();
                });
            });
        }
    }

    public async void OpenChangelogDialog()
    {
        await ShowChangelogDialogAsync();
    }

    private async Task ShowChangelogDialogAsync()
    {
        if (this.XamlRoot == null || _isDialogOpen) return;
        _isDialogOpen = true;
        try
        {
            if (ChangelogVersionComboBox != null)
            {
                ChangelogVersionComboBox.SelectedIndex = 0;
            }
            if (ChangelogDialog != null)
            {
                ChangelogDialog.PrimaryButtonText = LocalizationService.Instance.IsVietnamese ? "B\u1EAFt \u0111\u1EA7u tr\u1EA3i nghi\u1EC7m ngay \uD83D\uDE80" : "Get Started Now \uD83D\uDE80";
                ChangelogDialog.XamlRoot = this.XamlRoot;
                await ChangelogDialog.ShowAsync();
            }
        }
        catch { }
        finally
        {
            _isDialogOpen = false;
        }
    }

    private void ChangelogVersionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ChangelogVersionComboBox == null) return;
        if (ChangelogVersionComboBox.SelectedItem is ComboBoxItem item && item.Tag is string ver)
        {
            SwitchChangelogVersion(ver);
        }
    }

    private void SwitchChangelogVersion(string versionTag)
    {
        var panels = new[]
        {
            (ChangelogContent_v350, "3.5.0"),
            (ChangelogContent_v306, "3.0.6"),
            (ChangelogContent_v305, "3.0.5"),
            (ChangelogContent_v304, "3.0.4"),
            (ChangelogContent_v303, "3.0.3"),
            (ChangelogContent_v302, "3.0.2"),
            (ChangelogContent_v301, "3.0.1"),
            (ChangelogContent_v300, "3.0.0"),
            (ChangelogContent_v223, "2.2.3"),
            (ChangelogContent_v221, "2.2.1"),
            (ChangelogContent_v22, "2.2"),
            (ChangelogContent_v21, "2.1"),
            (ChangelogContent_v20, "2.0"),
            (ChangelogContent_v19, "1.9"),
            (ChangelogContent_v18, "1.8"),
            (ChangelogContent_v17, "1.7"),
            (ChangelogContent_v16, "1.6"),
            (ChangelogContent_v15, "1.5"),
            (ChangelogContent_v14, "1.4"),
            (ChangelogContent_v13, "1.3"),
            (ChangelogContent_v12, "1.2"),
            (ChangelogContent_v11, "1.1"),
            (ChangelogContent_v10, "1.0")
        };

        foreach (var (panel, tag) in panels)
        {
            if (panel != null)
            {
                panel.Visibility = (tag == versionTag) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        var isVi = LocalizationService.Instance.IsVietnamese;
        if (ChangelogHeaderTitle != null)
        {
            ChangelogHeaderTitle.Text = versionTag switch
            {
                "3.5.0" => isVi ? "Chi Ti\u1EBFt B\u1EA3n Ph\u00E1t H\u00E0nh Ch\u00EDnh Th\u1EE9c v3.5.0" : "Official Release Notes v3.5.0",
                "3.4.1" => isVi ? "Chi Ti\u1EBFt B\u1EA3n Ph\u00E1t H\u00E0nh Ch\u00EDnh Th\u1EE9c v3.4.1" : "Official Release Notes v3.4.1",
                "3.4.0" => isVi ? "Chi Ti\u1EBFt B\u1EA3n Ph\u00E1t H\u00E0nh Ch\u00EDnh Th\u1EE9c v3.4.0" : "Official Release Notes v3.4.0",
                "3.0.6" => isVi ? "Chi Ti\u1EBFt B\u1EA3n Ph\u00E1t H\u00E0nh Ch\u00EDnh Th\u1EE9c v3.0.6" : "Official Release Notes v3.0.6",
                "3.0.5" => isVi ? "Chi Ti\u1EBFt B\u1EA3n Ph\u00E1t H\u00E0nh Ch\u00EDnh Th\u1EE9c v3.0.5" : "Official Release Notes v3.0.5",
                "3.0.4" => isVi ? "Chi Ti\u1EBFt B\u1EA3n Ph\u00E1t H\u00E0nh Ch\u00EDnh Th\u1EE9c v3.0.4" : "Official Release Notes v3.0.4",
                "3.0.3" => isVi ? "Chi Ti\u1EBFt B\u1EA3n Ph\u00E1t H\u00E0nh Ch\u00EDnh Th\u1EE9c v3.0.3" : "Official Release Notes v3.0.3",
                "3.0.2" => isVi ? "Chi Ti\u1EBFt B\u1EA3n Ph\u00E1t H\u00E0nh Ch\u00EDnh Th\u1EE9c v3.0.2" : "Official Release Notes v3.0.2",
                "3.0.1" => isVi ? "Chi Ti\u1EBFt B\u1EA3n Ph\u00E1t H\u00E0nh Ch\u00EDnh Th\u1EE9c v3.0.1" : "Official Release Notes v3.0.1",
                "3.0.0" => isVi ? "Chi Ti\u1EBFt B\u1EA3n Ph\u00E1t H\u00E0nh Ch\u00EDnh Th\u1EE9c v3.0.0" : "Official Release Notes v3.0.0",
                "2.2.3" => isVi ? "Chi Ti\u1EBFt B\u1EA3n C\u1EADp Nh\u1EADt v2.2.3" : "Release Notes v2.2.3",
                "2.2.1" => isVi ? "Chi Ti\u1EBFt B\u1EA3n C\u1EADp Nh\u1EADt v2.2.1" : "Release Notes v2.2.1",
                "2.2" => isVi ? "Chi Ti\u1EBFt B\u1EA3n C\u1EADp Nh\u1EADt v2.2" : "Release Notes v2.2",
                "v2.1" => isVi ? "Chi Ti\u1EBFt B\u1EA3n C\u1EADp Nh\u1EADt v2.1" : "Release Notes v2.1",
                "v2.0" => isVi ? "Chi Ti\u1EBFt B\u1EA3n C\u1EADp Nh\u1EADt v2.0" : "Release Notes v2.0",
                "v1.9" => isVi ? "Chi Ti\u1EBFt B\u1EA3n C\u1EADp Nh\u1EADt v1.9" : "Release Notes v1.9",
                "v1.8" => isVi ? "Chi Ti\u1EBFt B\u1EA3n C\u1EADp Nh\u1EADt v1.8" : "Release Notes v1.8",
                "v1.7" => isVi ? "Chi Ti\u1EBFt B\u1EA3n C\u1EADp Nh\u1EADt v1.7" : "Release Notes v1.7",
                _ => isVi ? $"Chi Ti\u1EBFt B\u1EA3n C\u1EADp Nh\u1EADt {versionTag}" : $"Release Notes {versionTag}"
            };
        }

        if (ChangelogHeaderBadge != null)
        {
            if (versionTag == "3.5.0")
            {
                ChangelogHeaderBadge.Text = isVi ? "v3.5.0 B\u1EA2N CH\u00CDNH TH\u1EE8C" : "v3.5.0 OFFICIAL";
            }
            else if (versionTag == "3.4.1")
            {
                ChangelogHeaderBadge.Text = isVi ? "v3.4.1 B\u1EA2N CH\u00CDNH TH\u1EE8C" : "v3.4.1 OFFICIAL";
            }
            else if (versionTag == "3.4.0")
            {
                ChangelogHeaderBadge.Text = isVi ? "v3.4.0 B\u1EA2N CH\u00CDNH TH\u1EE8C" : "v3.4.0 OFFICIAL";
            }
            else if (versionTag == "3.0.6")
            {
                ChangelogHeaderBadge.Text = isVi ? "v3.0.6 B\u1EA2N CH\u00CDNH TH\u1EE8C" : "v3.0.6 OFFICIAL";
            }
            else if (versionTag == "3.0.5")
            {
                ChangelogHeaderBadge.Text = isVi ? "v3.0.5 B\u1EA2N CH\u00CDNH TH\u1EE8C" : "v3.0.5 OFFICIAL";
            }
        }

        if (ChangelogHeaderSubtitle != null)
        {
            if (versionTag == "3.5.0")
            {
                ChangelogHeaderSubtitle.Text = isVi
                    ? "B\u1EA3n c\u1EADp nh\u1EADt v3.5.0 gi\u1EDBi thi\u1EC7u phong c\u00E1ch thi\u1EBFt k\u1EBF Nothing Design \u0111\u1ED9t ph\u00E1 (Nothing OS Glyph Interface, ph\u00F4ng ch\u1EEF ma tr\u1EADn \u0111i\u1EC3m NDot, t\u01B0\u01A1ng ph\u1EA3n cao Monochrome Stealth & CMF Chalk), b\u1ED5 sung 2 m\u1EABu Desktop Widget Nothing Bento & Nothing Square, c\u00F9ng vi\u1EC7c kh\u1EAFc ph\u1EE5c tri\u1EC7t \u0111\u1EC3 l\u1ED7i m\u00E3 h\u00F3a ph\u00F4ng ch\u1EEF."
                    : "Version 3.5.0 introduces the groundbreaking Nothing Design language (Nothing OS Glyph Interface, dot-matrix typography NDot, monochrome stealth OLED & CMF chalk contrast), dual desktop widgets Nothing Bento & Nothing Square, and 100% unicode font fixes.";
            }
            else if (versionTag == "3.0.6")
            {
                ChangelogHeaderSubtitle.Text = isVi
                    ? "B\u1EA3n c\u1EADp nh\u1EADt v3.0.6 ra m\u1EAFt c\u1EA5u tr\u00FAc Bento Grid hi\u1EC7n \u0111\u1EA1i cho tab T\u1ED5ng quan, h\u1EC7 th\u1ED1ng ho\u1EA1t h\u1ECD\u00ADa Fluent Motion nh\u1ECBp nh\u00E0ng v\u00E0 chuy\u1EC3n tab m\u01B0\u1EE3t m\u00E0 100% tr\u00EAn Win 10 & 11."
                    : "Version 3.0.6 introduces a modern Bento Grid Overview layout, a comprehensive Fluent Motion animation system and seamless tab transitions on Win 10 & 11.";
            }
            else if (versionTag == "3.0.5")
            {
                ChangelogHeaderSubtitle.Text = isVi
                    ? "B\u1EA3n c\u1EADp nh\u1EADt v3.0.5 ho\u00E0n thi\u1EC7n 100% song ng\u1EEF Ti\u1EBFng Anh to\u00E0n b\u1ED9 \u1EE9ng d\u1EE5ng, \u0111\u1EA1i tu b\u1ED1 c\u1EE5c giao di\u1EC7n co gi\u00E3n Responsive ch\u1ED1ng tr\u00E0n c\u1EAFt ch\u1EEF tr\u00EAn Windows 10 & 11."
                    : "Version 3.0.5 delivers 100% comprehensive English localization across the entire app, responsive layout scaling overhaul preventing text clipping, and guaranteed visual compatibility on Windows 10 & 11.";
            }
        }
    }

    private void ChangelogDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        sender.Hide();
    }

    private void SettingsDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        sender.Hide();
    }

    #endregion
}
