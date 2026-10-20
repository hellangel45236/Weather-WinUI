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
            LocalizationService.Instance.LanguageChanged += (s, ev) => UpdateThemeComboBoxLabels();

            // Náº¡p dá»¯ liá»‡u thá»i tiáº¿t
            if (ViewModel != null)
            {
                await ViewModel.InitializeAsync();

                // Khá»Ÿi táº¡o khay há»‡ thá»‘ng (System Tray Icon)
                if (App.MainWindow != null && ViewModel.TrayService == null)
                {
                    var trayService = new TrayIconService();
                    trayService.Initialize(App.MainWindow, ViewModel, () => DesktopWidgetButton_Click(this, new RoutedEventArgs()));
                    ViewModel.TrayService = trayService;
                }

                // Tá»± Ä‘á»™ng hiá»ƒn thá»‹ Changelog náº¿u vá»«a cáº­p nháº­t phiÃªn báº£n má»›i
                CheckAndShowChangelog();

                // Láº¯ng nghe sá»± kiá»‡n Ä‘á»•i chá»§ Ä‘á» MÃ¹a/Lá»… há»™i Ä‘á»ƒ Ä‘á»“ng bá»™ háº¡t vÃ  Ã¡nh sÃ¡ng Zen Mode
                ViewModel.SeasonalSkinChanged += (s, skin) =>
                {
                    UpdateZenGradientColor();
                    UpdateSeasonalThemeButtonStyle();
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
                    TabOverview?.UpdateWeatherVisuals();
                    TabOverview?.RedrawCanvases();
                };

                // Há»— trá»£ kiá»ƒm thá»­ skin qua command-line (--skin=tet, midautumn, christmas, halloween, spring, summer, autumn, winter, auto)
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

                // Há»— trá»£ kiá»ƒm thá»­ Theme qua command-line (--theme=light, dark, system)
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

                // Há»— trá»£ cuá»™n trang qua command-line (--scroll=hourly, bottom)
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

                // Há»— trá»£ Ä‘iá»u hÆ°á»›ng Tab qua command-line (--tab=overview, flood, radar, lifestyle, calendar, widget, zen)
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

        // 1. Tinh chá»‰nh Top Bar linh hoáº¡t Ä‘á»ƒ thanh tÃ¬m kiáº¿m luÃ´n rá»™ng rÃ£i trÃªn má»i Ä‘á»™ phÃ¢n giáº£i
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

        // Tá»± co giÃ£n MaxWidth Ã´ tÃ¬m kiáº¿m khi cá»­a sá»• háº¹p
        if (LocationSearchBox != null)
        {
            LocationSearchBox.MaxWidth = width < 950 ? 280 : (width < 1140 ? 380 : 520);
        }

        // 2. Tinh chá»‰nh lá» ngoÃ i gá»n gÃ ng trÃªn mÃ n hÃ¬nh nhá» (MainContentGrid)
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
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
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
            ((ComboBoxItem)ThemeComboBox.Items[0]).Content = isVi ? "â˜€ï¸ SÃ¡ng" : "â˜€ï¸ Light";
            ((ComboBoxItem)ThemeComboBox.Items[1]).Content = isVi ? "ðŸŒ™ Tá»‘i" : "ðŸŒ™ Dark";
            ((ComboBoxItem)ThemeComboBox.Items[2]).Content = isVi ? "ðŸ’» Há»‡ thá»‘ng" : "ðŸ’» System";
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
        var inputTextBox = new TextBox
        {
            PlaceholderText = "Nháº­p tÃªn thÃ nh phá»‘ (VD: ÄÃ  Náºµng, Tokyo, Paris, New York...)",
            Height = 38
        };
        var dialog = new ContentDialog
        {
            Title = "âž• Táº¡o Widget Má»›i Cho ThÃ nh Phá»‘ KhÃ¡c",
            Content = inputTextBox,
            PrimaryButtonText = "Táº¡o Widget",
            CloseButtonText = "Há»§y",
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
        string nowTime = isVi ? $"{DateTime.Now:dddd, dd/MM/yyyy â€¢ HH:mm}" : $"{DateTime.Now:dddd, MM/dd/yyyy â€¢ HH:mm}";
        string lunar = isVi ? $"Ã‚m lá»‹ch: {w.LunarDateText} â€¢ {w.SolarTermText}" : $"Lunar: {w.LunarDateText} â€¢ {w.SolarTermText}";
        string temp = w.TemperatureText.Replace("C", "").Trim();
        string feelsLike = $"{w.FeelsLikeText} â€¢ {w.MinMaxText}";
        string uv = $"{w.UvIndexText} - {w.UvIndexDescription}";
        string aqi = $"{w.AqiValue} - {w.AqiDescription}";
        string rain = w.RainProbabilityText;
        string wind = $"{w.WindText} {w.WindDirectionText}";
        string humidity = w.HumidityText;
        string sun = $"{w.SunriseText} â€¢ {w.SunsetText}";

        // 1. Cáº­p nháº­t Khá»• Ngang FHD (1920x1080)
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
            if (FhdLandscapeOotdTitle != null) FhdLandscapeOotdTitle.Text = isVi ? $"HÃ”M NAY Máº¶C GÃŒ? (OOTD ADVISOR) â€” {o.OccasionTitle.ToUpper()}" : $"WHAT TO WEAR TODAY? (OOTD ADVISOR) â€” {o.OccasionTitle.ToUpper()}";
            if (FhdLandscapeThermalTag != null) FhdLandscapeThermalTag.Text = o.ThermalComfortNotice;
            if (FhdLandscapeOotdHeadline != null) FhdLandscapeOotdHeadline.Text = o.Headline;
            if (FhdLandscapeOotdClothing != null) FhdLandscapeOotdClothing.Text = $"{o.TopClothing}, {o.BottomClothing}, {o.Footwear}";
            if (FhdLandscapeOotdAccessories != null) FhdLandscapeOotdAccessories.Text = string.Join(" â€¢ ", o.Accessories.Select(a => $"{a.Name}"));
            if (FhdLandscapeMotorbikeText != null) FhdLandscapeMotorbikeText.Text = o.MotorbikeWarning;
        }

        // Cáº­p nháº­t 3 ngÃ y dá»± bÃ¡o
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

        // 2. Cáº­p nháº­t Khá»• Dá»c Story FHD (1080x1920)
        if (FhdStoryLocationText != null) FhdStoryLocationText.Text = location;
        if (FhdStoryDateText != null) FhdStoryDateText.Text = isVi ? $"{DateTime.Now:dd/MM/yyyy â€¢ HH:mm}" : $"{DateTime.Now:MM/dd/yyyy â€¢ HH:mm}";
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
            if (FhdStoryOotdTitle != null) FhdStoryOotdTitle.Text = isVi ? $"ðŸ‘— HÃ”M NAY Máº¶C GÃŒ? â€” {o.OccasionTitle.ToUpper()}" : $"ðŸ‘— WHAT TO WEAR TODAY? â€” {o.OccasionTitle.ToUpper()}";
            if (FhdStoryOotdHeadline != null) FhdStoryOotdHeadline.Text = o.Headline;
            if (FhdStoryOotdAccessories != null) FhdStoryOotdAccessories.Text = isVi ? $"ðŸŽ’ Phá»¥ kiá»‡n: {string.Join(" â€¢ ", o.Accessories.Select(a => a.Name))}" : $"ðŸŽ’ Accessories: {string.Join(" â€¢ ", o.Accessories.Select(a => a.Name))}";
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

        // HÃ¬nh ná»n thÃ nh phá»‘ náº¿u cÃ³
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

            // Äáº£m báº£o layout 1:1 Full HD khÃ´ng bá»‹ co kÃ©o hay scale bá»Ÿi Viewbox
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
                    ? $"ðŸŒ¤ï¸ Dá»± bÃ¡o thá»i tiáº¿t {ViewModel.LocationTitle}: {ViewModel.CurrentWeather?.TemperatureText}, {ViewModel.CurrentWeather?.ConditionText} (Tháº» Full HD {outWidth}x{outHeight} tá»« Weather WinUI v3.0.5)"
                    : $"ðŸŒ¤ï¸ Weather Forecast {ViewModel.LocationTitle}: {ViewModel.CurrentWeather?.TemperatureText}, {ViewModel.CurrentWeather?.ConditionText} (Full HD Card {outWidth}x{outHeight} from Weather WinUI v3.0.5)");
                dataPackage.RequestedOperation = DataPackageOperation.Copy;
                Clipboard.SetContent(dataPackage);

                ShareCardDialog?.Hide();
                if (ShareSuccessInfoBar != null)
                {
                    ShareSuccessInfoBar.Title = isVi ? "ÄÃ£ sao chÃ©p áº£nh Full HD thÃ nh cÃ´ng!" : "Full HD image copied successfully!";
                    ShareSuccessInfoBar.Message = isVi
                        ? $"áº¢nh thá»i tiáº¿t {outWidth}x{outHeight} siÃªu nÃ©t Ä‘Ã£ lÆ°u vÃ o Clipboard. Báº¡n cÃ³ thá»ƒ nháº¥n Ctrl+V Ä‘á»ƒ dÃ¡n trá»±c tiáº¿p vÃ o Zalo, Facebook, Messenger."
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
                        ShareSuccessInfoBar.Title = isVi ? "ÄÃ£ lÆ°u áº£nh Full HD thÃ nh cÃ´ng!" : "Full HD image saved successfully!";
                        ShareSuccessInfoBar.Message = isVi
                            ? $"File áº£nh cháº¥t lÆ°á»£ng cao {outWidth}x{outHeight}px Ä‘Ã£ lÆ°u táº¡i: {file.Path}."
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
        string currentVersion = "3.0.6";
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
                ChangelogDialog.PrimaryButtonText = LocalizationService.Instance.IsVietnamese
                    ? "Báº¯t Ä‘áº§u tráº£i nghiá»‡m ngay ðŸš€"
                    : "Get Started Now ðŸš€";
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
                "3.0.6" => isVi ? "Chi Tiáº¿t Báº£n PhÃ¡t HÃ nh ChÃ­nh Thá»©c v3.0.6" : "Official Release Notes v3.0.6",
                "3.0.5" => isVi ? "Chi Tiáº¿t Báº£n PhÃ¡t HÃ nh ChÃ­nh Thá»©c v3.0.5" : "Official Release Notes v3.0.5",
                "3.0.4" => isVi ? "Chi Tiáº¿t Báº£n PhÃ¡t HÃ nh ChÃ­nh Thá»©c v3.0.4" : "Official Release Notes v3.0.4",
                "3.0.3" => isVi ? "Chi Tiáº¿t Báº£n PhÃ¡t HÃ nh ChÃ­nh Thá»©c v3.0.3" : "Official Release Notes v3.0.3",
                "3.0.2" => isVi ? "Chi Tiáº¿t Báº£n PhÃ¡t HÃ nh ChÃ­nh Thá»©c v3.0.2" : "Official Release Notes v3.0.2",
                "3.0.1" => isVi ? "Chi Tiáº¿t Báº£n PhÃ¡t HÃ nh ChÃ­nh Thá»©c v3.0.1" : "Official Release Notes v3.0.1",
                "3.0.0" => isVi ? "Chi Tiáº¿t Báº£n PhÃ¡t HÃ nh ChÃ­nh Thá»©c v3.0.0" : "Official Release Notes v3.0.0",
                "2.2.3" => isVi ? "Chi Tiáº¿t Báº£n Cáº­p Nháº­t v2.2.3" : "Release Notes v2.2.3",
                "2.2.1" => isVi ? "Chi Tiáº¿t Báº£n Cáº­p Nháº­t v2.2.1" : "Release Notes v2.2.1",
                "2.2" => isVi ? "Chi Tiáº¿t Báº£n Cáº­p Nháº­t v2.2" : "Release Notes v2.2",
                "v2.1" => isVi ? "Chi Tiáº¿t Báº£n Cáº­p Nháº­t v2.1" : "Release Notes v2.1",
                "v2.0" => isVi ? "Chi Tiáº¿t Báº£n Cáº­p Nháº­t v2.0" : "Release Notes v2.0",
                "v1.9" => isVi ? "Chi Tiáº¿t Báº£n Cáº­p Nháº­t v1.9" : "Release Notes v1.9",
                "v1.8" => isVi ? "Chi Tiáº¿t Báº£n Cáº­p Nháº­t v1.8" : "Release Notes v1.8",
                "v1.7" => isVi ? "Chi Tiáº¿t Báº£n Cáº­p Nháº­t v1.7" : "Release Notes v1.7",
                _ => isVi ? $"Chi Tiáº¿t Báº£n Cáº­p Nháº­t {versionTag}" : $"Release Notes {versionTag}"
            };
        }

        if (ChangelogHeaderBadge != null)
        {
            if (versionTag == "3.0.6")
            {
                ChangelogHeaderBadge.Text = isVi ? "v3.0.6 Báº¢N CHÃNH THá»¨C" : "v3.0.6 OFFICIAL";
            }
            else if (versionTag == "3.0.5")
            {
                ChangelogHeaderBadge.Text = isVi ? "v3.0.5 Báº¢N CHÃNH THá»¨C" : "v3.0.5 OFFICIAL";
            }
        }

        if (ChangelogHeaderSubtitle != null)
        {
            if (versionTag == "3.0.6")
            {
                ChangelogHeaderSubtitle.Text = isVi
                    ? "Báº£n cáº­p nháº­t v3.0.6 ra máº¯t cáº¥u trÃºc Bento Grid hiá»‡n Ä‘áº¡i cho tab Tá»•ng quan, há»‡ thá»‘ng hoáº¡t há»a Fluent Motion nhá»‹p nhÃ ng (kim la bÃ n xoay ngáº¯n nháº¥t, UV/AQI lÆ°á»›t Ãªm, biá»ƒu tÆ°á»£ng thá»Ÿ lÆ¡ lá»­ng, váº­t lÃ½ vi mÃ´ tháº» card) vÃ  chuyá»ƒn tab mÆ°á»£t mÃ  100% trÃªn Win 10 & 11."
                    : "Version 3.0.6 introduces a modern Bento Grid Overview layout, a comprehensive Fluent Motion animation system (shortest-arc compass, gliding UV/AQI indicators, breathing hero icon, card hover physics) and seamless tab transitions on Win 10 & 11.";
            }
            else if (versionTag == "3.0.5")
            {
                ChangelogHeaderSubtitle.Text = isVi
                    ? "Báº£n cáº­p nháº­t v3.0.5 hoÃ n thiá»‡n 100% song ngá»¯ Tiáº¿ng Anh toÃ n bá»™ á»©ng dá»¥ng, Ä‘áº¡i tu bá»‘ cá»¥c giao diá»‡n co giÃ£n Responsive chá»‘ng trÃ n cáº¯t chá»¯ vÃ  Ä‘áº£m báº£o Ä‘á»™ tÆ°Æ¡ng thÃ­ch hiá»ƒn thá»‹ tuyá»‡t Ä‘á»‘i trÃªn Windows 10 & 11."
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
