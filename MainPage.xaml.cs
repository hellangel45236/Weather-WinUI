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
    private bool _isApplyingVisuals = false;
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
            if (e.Key == Windows.System.VirtualKey.K &&
                Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
            {
                LocationSearchBox?.Focus(FocusState.Programmatic);
                Helpers.MechanicalSoundHelper.PlayClick();
                e.Handled = true;
                return;
            }
            if (e.Key == Windows.System.VirtualKey.F11)
            {
                ViewModel.ToggleZenModeCommand.Execute(null);
                Helpers.MechanicalSoundHelper.PlayClick();
                e.Handled = true;
                return;
            }
            if (e.Key == Windows.System.VirtualKey.Escape && ViewModel.IsZenModeOpen)
            {
                ViewModel.CloseZenModeCommand.Execute(null);
                Helpers.MechanicalSoundHelper.PlayClick();
                e.Handled = true;
                return;
            }

            // Neu dang go chu trong o nhap lieu thi bo qua phim tat
            if (IsTextInputActive()) return;

            switch (e.Key)
            {
                case Windows.System.VirtualKey.Number1:
                case Windows.System.VirtualKey.NumberPad1:
                    SelectNavigationTabByTag("overview");
                    e.Handled = true;
                    break;
                case Windows.System.VirtualKey.Number2:
                case Windows.System.VirtualKey.NumberPad2:
                    SelectNavigationTabByTag("flood");
                    e.Handled = true;
                    break;
                case Windows.System.VirtualKey.Number3:
                case Windows.System.VirtualKey.NumberPad3:
                    SelectNavigationTabByTag("radar");
                    e.Handled = true;
                    break;
                case Windows.System.VirtualKey.Number4:
                case Windows.System.VirtualKey.NumberPad4:
                    SelectNavigationTabByTag("lifestyle");
                    e.Handled = true;
                    break;
                case Windows.System.VirtualKey.Number5:
                case Windows.System.VirtualKey.NumberPad5:
                    SelectNavigationTabByTag("calendar");
                    e.Handled = true;
                    break;
                case Windows.System.VirtualKey.Number6:
                case Windows.System.VirtualKey.NumberPad6:
                    SelectNavigationTabByTag("widget");
                    e.Handled = true;
                    break;
                case Windows.System.VirtualKey.Number7:
                case Windows.System.VirtualKey.NumberPad7:
                    SelectNavigationTabByTag("settings");
                    e.Handled = true;
                    break;
                case Windows.System.VirtualKey.R:
                    ViewModel.RefreshCommand.Execute(null);
                    Helpers.MechanicalSoundHelper.PlayShutter();
                    e.Handled = true;
                    break;
                case Windows.System.VirtualKey.Z:
                    ViewModel.ToggleZenModeCommand.Execute(null);
                    Helpers.MechanicalSoundHelper.PlayClick();
                    e.Handled = true;
                    break;
            }
        };

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            ApplyDesignLanguageVisuals(ViewModel?.SelectedDesignLanguage);
            // Đồng bộ theme ngay khi ThemeHelper thay đổi hoặc hệ thống đổi Light/Dark (Chỉ đăng ký 1 lần duy nhất)
            ThemeHelper.ThemeChanged += (theme) =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    ApplyDesignLanguageVisuals(ViewModel?.SelectedDesignLanguage);
                    UpdateSeasonalThemeButtonStyle();
                    TabOverview?.UpdateNothingThemeVisuals();
                    TabRadar?.UpdateNothingVisuals();
                    TabFlood?.UpdateNothingVisuals();
                    TabOverview?.UpdateWeatherVisuals();
                    TabOverview?.RedrawCanvases();
                });
            };
            this.ActualThemeChanged += (s, e) =>
            {
                ApplyDesignLanguageVisuals(ViewModel?.SelectedDesignLanguage);
                UpdateSeasonalThemeButtonStyle();
                TabOverview?.UpdateNothingThemeVisuals();
                TabRadar?.UpdateNothingVisuals();
                TabFlood?.UpdateNothingVisuals();
                TabOverview?.UpdateWeatherVisuals();
                TabOverview?.RedrawCanvases();
            };

        // Nạp dữ liệu thời tiết
            if (ViewModel != null)
            {
                // Xử lý cờ CLI ngay lập tức trước khi tải mạng để UI phản hồi tức thì
                try
                {
                    string fullCmd = Environment.CommandLine;
                    if (fullCmd.Contains("--eink", StringComparison.OrdinalIgnoreCase))
                    {
                        ViewModel.IsNothingEInkMode = true;
                        ViewModel.Settings.EnableNothingEInkMode = true;
                        ViewModel.ChangeTheme("light");
                    }
                    if (fullCmd.Contains("--dock", StringComparison.OrdinalIgnoreCase))
                    {
                        ViewModel.NavigationLayoutMode = "FloatingDock";
                    }
                    else if (fullCmd.Contains("--sidebar", StringComparison.OrdinalIgnoreCase))
                    {
                        ViewModel.NavigationLayoutMode = "LeftSidebar";
                    }
                    if (fullCmd.Contains("--design=", StringComparison.OrdinalIgnoreCase))
                    {
                        int dIdx = fullCmd.IndexOf("--design=", StringComparison.OrdinalIgnoreCase);
                        int endIdx = fullCmd.IndexOf(" ", dIdx);
                        string dMode = (endIdx > dIdx ? fullCmd.Substring(dIdx + 9, endIdx - dIdx - 9) : fullCmd.Substring(dIdx + 9)).Trim().Trim('"');
                        if (dMode.Equals("spatial", StringComparison.OrdinalIgnoreCase) || dMode.Equals("aurora", StringComparison.OrdinalIgnoreCase))
                            dMode = "SpatialGlass";
                        ViewModel.ApplyDesignLanguage(dMode, save: false);
                        ApplyDesignLanguageVisuals(ViewModel.SelectedDesignLanguage);
                        (App.MainWindow as MainWindow)?.UpdateWindowTheme();
                    }
                    if (fullCmd.Contains("--theme=", StringComparison.OrdinalIgnoreCase))
                    {
                        int tIdx = fullCmd.IndexOf("--theme=", StringComparison.OrdinalIgnoreCase);
                        int endIdx = fullCmd.IndexOf(" ", tIdx);
                        string tMode = (endIdx > tIdx ? fullCmd.Substring(tIdx + 8, endIdx - tIdx - 8) : fullCmd.Substring(tIdx + 8)).Trim().Trim('"');
                        ViewModel.ChangeTheme(tMode);
                        ApplyDesignLanguageVisuals(ViewModel.SelectedDesignLanguage);
                        (App.MainWindow as MainWindow)?.UpdateWindowTheme();
                    }
                    var skinArg = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith("--skin=", StringComparison.OrdinalIgnoreCase));
                    if (skinArg != null)
                    {
                        string targetSkin = skinArg.Substring(7).Trim();
                        ViewModel.ChangeSeasonalSkin(targetSkin);
                    }
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
                            SelectNavigationTabByTag(targetTab);
                        }
                    }
                }
                catch { }

                // Nạp dữ liệu thời tiết
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
                StartZenChromeAutoHideTimer();
                    UpdateSeasonalThemeButtonStyle();
                    TabOverview?.UpdateNothingThemeVisuals();
                    TabRadar?.UpdateNothingVisuals();
                    TabFlood?.UpdateNothingVisuals();
                    TabOverview?.UpdateWeatherVisuals();
                    TabOverview?.RedrawCanvases();
                    if (_zenEffectRenderer != null && skin != null)
                    {
                        _zenEffectRenderer.SetSeasonalParticleType(skin.ParticleType, ViewModel.EnableSeasonalParticles);
                    }
                };


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

                try
                {
                    string fullCmd = Environment.CommandLine;
                    if (fullCmd.Contains("--timemachine=", StringComparison.OrdinalIgnoreCase))
                    {
                        int tmIdx = fullCmd.IndexOf("--timemachine=", StringComparison.OrdinalIgnoreCase);
                        int endIdx = fullCmd.IndexOf(" ", tmIdx);
                        string tmVal = (endIdx > tmIdx ? fullCmd.Substring(tmIdx + 14, endIdx - tmIdx - 14) : fullCmd.Substring(tmIdx + 14)).Trim().Trim('"');
                        if (int.TryParse(tmVal, out int dayIdx) && ViewModel.DailyForecast != null && dayIdx >= 0 && dayIdx < ViewModel.DailyForecast.Count)
                        {
                            ViewModel.SelectDailyForecastForHourly(ViewModel.DailyForecast[dayIdx]);
                        }
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

                    if (fullCmd.Contains("--screenshot=", StringComparison.OrdinalIgnoreCase))
                    {
                        int ssIdx = fullCmd.IndexOf("--screenshot=", StringComparison.OrdinalIgnoreCase) + 13;
                        string ssPath;
                        if (ssIdx < fullCmd.Length && fullCmd[ssIdx] == '"')
                        {
                            int endQ = fullCmd.IndexOf('"', ssIdx + 1);
                            ssPath = endQ > ssIdx ? fullCmd.Substring(ssIdx + 1, endQ - ssIdx - 1) : fullCmd.Substring(ssIdx + 1).Trim('"');
                        }
                        else
                        {
                            int endS = fullCmd.IndexOf(' ', ssIdx);
                            ssPath = (endS > ssIdx ? fullCmd.Substring(ssIdx, endS - ssIdx) : fullCmd.Substring(ssIdx)).Trim().Trim('"');
                        }

                        await Task.Delay(1800);
                        TabOverview?.RedrawCanvases();
                        await Task.Delay(400);

                        UIElement targetEl = (_widgetWindow?.Content as UIElement) ?? this;
                        var rtb = new RenderTargetBitmap();
                        await rtb.RenderAsync(targetEl);
                        var pixelBuffer = await rtb.GetPixelsAsync();
                        byte[] pixels = pixelBuffer.ToArray();

                        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ssPath)!);
                        using (var fs = new FileStream(ssPath, FileMode.Create, FileAccess.Write))
                        {
                            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fs.AsRandomAccessStream());
                            encoder.SetPixelData(
                                BitmapPixelFormat.Bgra8,
                                BitmapAlphaMode.Premultiplied,
                                (uint)rtb.PixelWidth,
                                (uint)rtb.PixelHeight,
                                96, 96,
                                pixels);
                            await encoder.FlushAsync();
                        }
                        Application.Current.Exit();
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

        // 1. Tinh chỉnh Top Bar thích ứng đa tầng (Responsive Command Header)
        bool isCompactTop = width < 1400;
        bool isUltraCompactTop = width < 1220;

        if (ZenButtonText != null) ZenButtonText.Visibility = isCompactTop ? Visibility.Collapsed : Visibility.Visible;
        if (SeasonalThemeBtnText != null) SeasonalThemeBtnText.Visibility = isCompactTop ? Visibility.Collapsed : Visibility.Visible;
        if (BtnTextWidget != null) BtnTextWidget.Visibility = isCompactTop ? Visibility.Collapsed : Visibility.Visible;
        if (DockToggleText != null) DockToggleText.Visibility = isCompactTop ? Visibility.Collapsed : Visibility.Visible;
        if (NothingGlyphBadgeText != null) NothingGlyphBadgeText.Visibility = isCompactTop ? Visibility.Collapsed : Visibility.Visible;
        if (NothingGlyphBadgeBorder != null) NothingGlyphBadgeBorder.Visibility = isUltraCompactTop ? Visibility.Collapsed : Visibility.Visible;

        // Tinh chỉnh dải thành phố ghim nhanh (Pinned Cities Strip)
        if (PinnedCitiesItemsControl != null)
        {
            if (isUltraCompactTop)
            {
                // Màn hình hẹp: Ẩn dải chip ghim, giữ nút GPS và nút Flyout "+ X" để mở danh sách 1 click
                PinnedCitiesItemsControl.Visibility = Visibility.Collapsed;
            }
            else if (isCompactTop)
            {
                PinnedCitiesItemsControl.Visibility = Visibility.Visible;
                PinnedCitiesItemsControl.MaxWidth = 220; // Giới hạn tối đa ~2 thành phố
            }
            else
            {
                PinnedCitiesItemsControl.Visibility = Visibility.Visible;
                PinnedCitiesItemsControl.MaxWidth = double.PositiveInfinity;
            }
        }

        if (AppSubtitleText != null) AppSubtitleText.Visibility = width < 1140 ? Visibility.Collapsed : Visibility.Visible;

        if (LocationSearchBox != null)
        {
            LocationSearchBox.MaxWidth = width < 1000 ? 250 : (width < 1250 ? 340 : 480);
            LocationSearchBox.MinWidth = 160;
        }

        // 2. Tinh chỉnh Floating Dock linh hoạt trên màn hình hẹp (< 1240px)
        bool isCompactDock = width < 1240;
        if (DockFloodBadgeText != null) DockFloodBadgeText.Visibility = isCompactDock ? Visibility.Collapsed : Visibility.Visible;
        if (DockCalendarBadgeText != null) DockCalendarBadgeText.Visibility = isCompactDock ? Visibility.Collapsed : Visibility.Visible;
        if (DockWidgetBadgeBorder != null) DockWidgetBadgeBorder.Visibility = isCompactDock ? Visibility.Collapsed : Visibility.Visible;

        // 3. Tinh chỉnh lề ngoài gọn gàng trên màn hình nhỏ (MainContentGrid)
        if (MainContentGrid != null)
        {
            var targetMargin = width < 1250 ? new Thickness(14, 8, 14, 12) : new Thickness(24, 12, 24, 18);
            if (MainContentGrid.Margin != targetMargin)
            {
                MainContentGrid.Margin = targetMargin;
            }
        }
    }

    private bool _isShareDialogOpen = false;

    private WeatherEffectRenderer? _zenEffectRenderer;
    private DispatcherTimer? _zenChromeTimer;

    private void StartZenChromeAutoHideTimer()
    {
        if (_zenChromeTimer == null)
        {
            _zenChromeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(2600) };
            _zenChromeTimer.Tick += (s, e) =>
            {
                _zenChromeTimer.Stop();
                if (ZenTopChrome != null) ZenTopChrome.Opacity = 0.08;
                if (ZenBottomChrome != null) ZenBottomChrome.Opacity = 0.08;
            };
        }
        if (ZenTopChrome != null) ZenTopChrome.Opacity = 1.0;
        if (ZenBottomChrome != null) ZenBottomChrome.Opacity = 1.0;
        _zenChromeTimer.Stop();
        _zenChromeTimer.Start();
    }

    private void ZenOverlay_PointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        StartZenChromeAutoHideTimer();
    }

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
                StartZenChromeAutoHideTimer();
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
                StartZenChromeAutoHideTimer();
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
                _zenChromeTimer?.Stop();
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
                StartZenChromeAutoHideTimer();
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

    private bool IsTextInputActive()
    {
        try
        {
            if (this.XamlRoot == null) return false;
            var focused = Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(this.XamlRoot);
            return focused is TextBox || focused is AutoSuggestBox || focused is PasswordBox;
        }
        catch { return false; }
    }

    private string _currentActiveTabTag = "overview";

    private void FloatingDockTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag)
        {
            Helpers.MechanicalSoundHelper.PlayClick();
            SelectNavigationTabByTag(tag);
        }
    }

    private void HeaderNavLayoutToggle_Click(object sender, RoutedEventArgs e)
    {
        Helpers.MechanicalSoundHelper.PlayClick();
        ViewModel?.ToggleNavigationLayoutCommand.Execute(null);
        UpdateFloatingDockVisuals(_currentActiveTabTag);
    }

    private void HeaderEInkToggle_Click(object sender, RoutedEventArgs e)
    {
        Helpers.MechanicalSoundHelper.PlayClick();
        ViewModel?.ToggleNothingEInkModeCommand.Execute(null);
        ApplyDesignLanguageVisuals(ViewModel?.SelectedDesignLanguage);
        (App.MainWindow as MainWindow)?.UpdateWindowTheme();
    }

    public void SelectNavigationTabByTag(string targetTab)
    {
        _currentActiveTabTag = targetTab;
        UpdateFloatingDockVisuals(targetTab);
        ViewModel?.SwitchNav(targetTab);
        ActivateTab(targetTab);

        if (MainNavView == null) return;
        var allItems = MainNavView.MenuItems.OfType<NavigationViewItem>().Concat(MainNavView.FooterMenuItems.OfType<NavigationViewItem>());
        foreach (var item in allItems)
        {
            if (string.Equals(item.Tag?.ToString(), targetTab, StringComparison.OrdinalIgnoreCase))
            {
                MainNavView.SelectedItem = item;
                break;
            }
        }
    }

    #region Tab Navigation & Lazy Realization

    private void ActivateTab(string tag)
    {
        try
        {
            switch (tag)
            {
                case "overview":
                    TabOverview?.UpdateWeatherVisuals();
                    TabOverview?.StartEffects();
                    TabOverview?.RedrawCanvases();
                    TabRadar?.StopRadarSweep();
                    AnimationHelper.SlideUpFadeIn(TabOverview);
                    break;

                case "flood":
                    TabOverview?.StopEffects();
                    TabRadar?.StopRadarSweep();
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
                    TabRadar?.StopRadarSweep();
                    var lifestyleTab = FindName(nameof(TabLifestyle)) as LifestyleTab;
                    lifestyleTab?.UpdateOccasionButtonsVisual();
                    lifestyleTab?.UpdateGenderButtonsVisual();
                    AnimationHelper.SlideUpFadeIn(lifestyleTab);
                    break;

                case "calendar":
                    TabOverview?.StopEffects();
                    TabRadar?.StopRadarSweep();
                    var calendarTab = FindName(nameof(TabCalendar)) as CalendarTab;
                    AnimationHelper.SlideUpFadeIn(calendarTab);
                    break;

                case "widget":
                    TabOverview?.StopEffects();
                    TabRadar?.StopRadarSweep();
                    var widgetTab = FindName(nameof(TabWidgetStudio)) as WidgetStudioTab;
                    AnimationHelper.SlideUpFadeIn(widgetTab);
                    break;

                case "settings":
                    TabOverview?.StopEffects();
                    TabRadar?.StopRadarSweep();
                    var settingsTab = FindName(nameof(TabSettings)) as SettingsTab;
                    AnimationHelper.SlideUpFadeIn(settingsTab);
                    DispatcherQueue?.TryEnqueue(() => settingsTab?.SyncAllSettings());
                    break;
            }
        }
        catch { }
    }

    private void MainNavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        try
        {
            string? tag = null;
            if (args.IsSettingsSelected)
            {
                tag = "settings";
            }
            else if (args.SelectedItem is NavigationViewItem item && item.Tag is string itemTag)
            {
                tag = itemTag;
            }

            if (!string.IsNullOrEmpty(tag))
            {
                _currentActiveTabTag = tag;
                ViewModel?.SwitchNav(tag);
                UpdateFloatingDockVisuals(tag);
                ActivateTab(tag);
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

    private void HeaderChangelogButton_Click(object sender, RoutedEventArgs e) => OpenChangelogDialog();

    private void SettingsTab_OpenChangelogRequested(object? sender, EventArgs e)
    {
        OpenChangelogDialog();
    }

    private void SettingsTab_ReopenOnboardingRequested(object? sender, EventArgs e)
    {
        Frame.Navigate(typeof(OnboardingPage));
    }

    public void OnWindowStateChanged(bool isMinimized)
    {
        DispatcherQueue?.TryEnqueue(() =>
        {
            if (isMinimized)
            {
                TabOverview?.StopEffects();
                TabRadar?.StopRadarSweep();
                WeatherApp.Helpers.MemoryOptimizer.DeepCleanup();
            }
            else
            {
                if (string.Equals(_currentActiveTabTag, "overview", StringComparison.OrdinalIgnoreCase))
                {
                    TabOverview?.StartEffects();
                }
                else if (string.Equals(_currentActiveTabTag, "radar", StringComparison.OrdinalIgnoreCase))
                {
                    TabRadar?.StartRadarSweep();
                }
            }
        });
    }

    #endregion

    #region Top Bar & Search Handlers

    private DispatcherTimer? _searchDebounceTimer;

    private void LocationSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (ViewModel == null) return;

        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            _searchDebounceTimer?.Stop();
            string query = sender.Text;
            if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
            {
                sender.ItemsSource = null;
                return;
            }

            _searchDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(280) };
            _searchDebounceTimer.Tick += async (s, e) =>
            {
                _searchDebounceTimer?.Stop();
                if (ViewModel == null) return;
                await ViewModel.SearchLocationsCommand.ExecuteAsync(query);
                sender.ItemsSource = ViewModel.SearchResults;
            };
            _searchDebounceTimer.Start();
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
            string rawCmd = args.QueryText.Trim();
            if (rawCmd.StartsWith(">") || TryMatchDirectCommand(rawCmd))
            {
                ExecuteQuickCommand(rawCmd.TrimStart('>').Trim());
                sender.Text = string.Empty;
                return;
            }
            await ViewModel.SearchLocationsCommand.ExecuteAsync(args.QueryText);
            if (ViewModel.SearchResults.Count > 0)
            {
                var first = ViewModel.SearchResults[0];
                sender.Text = first.DisplayText;
                await ViewModel.SelectLocationCommand.ExecuteAsync(first);
            }
        }
    }


    public void ApplyDesignLanguageVisuals(string? designLanguage)
    {
        if (_isApplyingVisuals) return;
        _isApplyingVisuals = true;
        try
        {
            string dl = designLanguage ?? ViewModel?.SelectedDesignLanguage ?? "Fluent2";
            bool isNothing = string.Equals(dl, "Nothing", StringComparison.OrdinalIgnoreCase);
            bool isSpatialGlass = string.Equals(dl, "SpatialGlass", StringComparison.OrdinalIgnoreCase);
            string savedTheme = ViewModel?.Settings?.ThemeMode ?? "Dark";
            bool isDarkRequested = savedTheme.Equals("Dark", StringComparison.OrdinalIgnoreCase) ||
                (!savedTheme.Equals("Light", StringComparison.OrdinalIgnoreCase) && ThemeHelper.IsDarkTheme());

            if (isDarkRequested && ViewModel?.IsNothingEInkMode == true)
            {
                ViewModel.IsNothingEInkMode = false;
                ViewModel.Settings.EnableNothingEInkMode = false;
            }

            bool isEInk = isNothing && (ViewModel?.IsNothingEInkMode == true) && !isDarkRequested;
            bool isDark = isEInk ? false : (isDarkRequested || ThemeHelper.IsDarkTheme(this));

            if (isNothing)
            {
                if (isEInk && this.RequestedTheme != ElementTheme.Light)
                {
                    this.RequestedTheme = ElementTheme.Light;
                }
                else if (!isEInk)
                {
                    if (isDark && this.RequestedTheme != ElementTheme.Dark)
                        this.RequestedTheme = ElementTheme.Dark;
                    else if (!isDark && this.RequestedTheme != ElementTheme.Light)
                        this.RequestedTheme = ElementTheme.Light;
                }
                isDark = isEInk ? false : (this.RequestedTheme == ElementTheme.Dark || ThemeHelper.IsDarkTheme(this));

                var cardBg = isEInk
                    ? Windows.UI.Color.FromArgb(255, 250, 247, 240)
                    : (isDark ? Windows.UI.Color.FromArgb(255, 18, 18, 20) : Windows.UI.Color.FromArgb(255, 255, 255, 255));
                var cardBorder = isEInk
                    ? Windows.UI.Color.FromArgb(255, 214, 209, 196)
                    : (isDark ? Windows.UI.Color.FromArgb(255, 36, 36, 40) : Windows.UI.Color.FromArgb(255, 226, 226, 230));
                var subtleBg = isEInk
                    ? Windows.UI.Color.FromArgb(255, 238, 234, 224)
                    : (isDark ? Windows.UI.Color.FromArgb(255, 24, 24, 27) : Windows.UI.Color.FromArgb(255, 245, 245, 247));
                var subCardBorder = isEInk
                    ? Windows.UI.Color.FromArgb(255, 210, 204, 190)
                    : (isDark ? Windows.UI.Color.FromArgb(255, 38, 38, 43) : Windows.UI.Color.FromArgb(255, 229, 229, 232));
                var paneBg = isEInk
                    ? Windows.UI.Color.FromArgb(255, 244, 241, 234)
                    : (isDark ? Windows.UI.Color.FromArgb(255, 10, 10, 12) : Windows.UI.Color.FromArgb(255, 240, 240, 242));
                this.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(paneBg);
                var glyphBoxBg = isEInk
                    ? Windows.UI.Color.FromArgb(255, 242, 238, 229)
                    : (isDark ? Windows.UI.Color.FromArgb(255, 10, 10, 12) : Windows.UI.Color.FromArgb(255, 249, 249, 250));

                var redAccent = Windows.UI.Color.FromArgb(255, 215, 25, 33);
                var textPrimary = isEInk
                    ? Windows.UI.Color.FromArgb(255, 20, 20, 19)
                    : (isDark ? Windows.UI.Color.FromArgb(255, 255, 255, 255) : Windows.UI.Color.FromArgb(255, 10, 10, 12));
                var textSecondary = isEInk
                    ? Windows.UI.Color.FromArgb(255, 87, 85, 78)
                    : (isDark ? Windows.UI.Color.FromArgb(255, 136, 136, 136) : Windows.UI.Color.FromArgb(255, 102, 102, 102));
                var textMuted = isEInk
                    ? Windows.UI.Color.FromArgb(255, 138, 134, 124)
                    : (isDark ? Windows.UI.Color.FromArgb(255, 82, 82, 91) : Windows.UI.Color.FromArgb(255, 161, 161, 170));
                var badgeBg = isDark ? Windows.UI.Color.FromArgb(255, 30, 30, 34) : Windows.UI.Color.FromArgb(255, 240, 240, 242);
                var pillBg = isEInk
                    ? Windows.UI.Color.FromArgb(255, 236, 231, 220)
                    : (isDark ? Windows.UI.Color.FromArgb(255, 31, 31, 35) : Windows.UI.Color.FromArgb(255, 238, 238, 240));
                var pillBorder = isEInk
                    ? Windows.UI.Color.FromArgb(255, 206, 200, 186)
                    : (isDark ? Windows.UI.Color.FromArgb(255, 51, 51, 51) : Windows.UI.Color.FromArgb(255, 225, 225, 228));

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

                if (FloatingIslandHeader != null)
                {
                    FloatingIslandHeader.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(cardBg);
                    FloatingIslandHeader.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(cardBorder);
                }

                if (MainNavView != null)
                {
                    MainNavView.Resources["NavigationViewDefaultPaneBackground"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(paneBg);
                    MainNavView.Resources["NavigationViewTopPaneBackground"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(paneBg);
                }

                var nBadgeBgBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
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

                var ndotFont = App.Current.Resources.TryGetValue("NdotFontFamily", out var nf) ? (Microsoft.UI.Xaml.Media.FontFamily)nf : new Microsoft.UI.Xaml.Media.FontFamily("Assets/Fonts/Ndot77JPExtended.ttf#Ndot77JPExtended, Cascadia Mono");
                var textPrimaryBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(textPrimary);

                // Cập nhật biểu tượng thanh điều hướng Navigation View sang Nothing Dot Glyphs
                if (NavOverviewIcon != null) { NavOverviewIcon.FontFamily = ndotFont; NavOverviewIcon.Glyph = NothingGlyphs.House; NavOverviewIcon.Foreground = textPrimaryBrush; }
                if (NavFloodIcon != null) { NavFloodIcon.FontFamily = ndotFont; NavFloodIcon.Glyph = NothingGlyphs.Waves; NavFloodIcon.Foreground = textPrimaryBrush; }
                if (NavRadarIcon != null) { NavRadarIcon.FontFamily = ndotFont; NavRadarIcon.Glyph = NothingGlyphs.RadarTarget; NavRadarIcon.Foreground = textPrimaryBrush; }
                if (NavLifestyleIcon != null) { NavLifestyleIcon.FontFamily = ndotFont; NavLifestyleIcon.Glyph = NothingGlyphs.Smiley; NavLifestyleIcon.Foreground = textPrimaryBrush; }
                if (NavCalendarIcon != null) { NavCalendarIcon.FontFamily = ndotFont; NavCalendarIcon.Glyph = NothingGlyphs.Clock; NavCalendarIcon.Foreground = textPrimaryBrush; }
                if (NavWidgetIcon != null) { NavWidgetIcon.FontFamily = ndotFont; NavWidgetIcon.Glyph = NothingGlyphs.WidgetRobot; NavWidgetIcon.Foreground = textPrimaryBrush; }
                if (NavSettingsIcon != null) { NavSettingsIcon.FontFamily = ndotFont; NavSettingsIcon.Glyph = NothingGlyphs.SettingsDial; NavSettingsIcon.Foreground = textPrimaryBrush; }

                // Cập nhật biểu tượng nút chức năng Floating Header sang Nothing Dot Glyphs
                if (GpsButtonIcon != null) { GpsButtonIcon.FontFamily = ndotFont; GpsButtonIcon.Glyph = NothingGlyphs.GpsTarget; }
                if (ZenButtonIcon != null) { ZenButtonIcon.FontFamily = ndotFont; ZenButtonIcon.Glyph = NothingGlyphs.ZenSun; }
                if (RefreshButtonIcon != null) { RefreshButtonIcon.FontFamily = ndotFont; RefreshButtonIcon.Glyph = NothingGlyphs.AdvanceRefresh; }
                if (UnitButtonIcon != null) { UnitButtonIcon.FontFamily = ndotFont; UnitButtonIcon.Glyph = NothingGlyphs.Percent; }
                if (DesktopWidgetButtonIcon != null) { DesktopWidgetButtonIcon.FontFamily = ndotFont; DesktopWidgetButtonIcon.Glyph = NothingGlyphs.WidgetDropdown; }

                TabOverview?.UpdateNothingThemeVisuals();
                TabRadar?.UpdateNothingVisuals();
                TabFlood?.UpdateNothingVisuals();
            }
            else
            {
                this.Background = null;
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
                    if (this.Resources.ContainsKey(k)) this.Resources.Remove(k);
                }

                if (FloatingIslandHeader != null)
                {
                    FloatingIslandHeader.ClearValue(Border.BackgroundProperty);
                    FloatingIslandHeader.ClearValue(Border.BorderBrushProperty);
                }

                if (MainNavView != null)
                {
                    MainNavView.Resources.Remove("NavigationViewDefaultPaneBackground");
                    MainNavView.Resources.Remove("NavigationViewTopPaneBackground");
                }

                var transparentBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
                if (NavOverviewBadgeBorder != null) NavOverviewBadgeBorder.Background = transparentBrush;
                if (NavOverviewBadgeText != null) NavOverviewBadgeText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 56, 189, 248) : Windows.UI.Color.FromArgb(255, 2, 132, 199));
                if (NavFloodBadgeBorder != null) NavFloodBadgeBorder.Background = transparentBrush;
                if (NavFloodBadgeText != null) NavFloodBadgeText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 56, 189, 248) : Windows.UI.Color.FromArgb(255, 2, 132, 199));
                if (NavRadarBadgeBorder != null) NavRadarBadgeBorder.Background = transparentBrush;
                if (NavRadarBadgeText != null) NavRadarBadgeText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 52, 211, 153) : Windows.UI.Color.FromArgb(255, 22, 163, 74));
                if (NavLifestyleBadgeBorder != null) NavLifestyleBadgeBorder.Background = transparentBrush;
                if (NavLifestyleBadgeText != null) NavLifestyleBadgeText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 244, 114, 182) : Windows.UI.Color.FromArgb(255, 219, 39, 119));
                if (NavCalendarBadgeBorder != null) NavCalendarBadgeBorder.Background = transparentBrush;
                if (NavCalendarBadgeText != null) NavCalendarBadgeText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 167, 139, 250) : Windows.UI.Color.FromArgb(255, 124, 58, 237));
                if (NavWidgetBadgeBorder != null) NavWidgetBadgeBorder.Background = transparentBrush;
                if (NavWidgetBadgeText != null) NavWidgetBadgeText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 148, 163, 184) : Windows.UI.Color.FromArgb(255, 100, 116, 139));

                var faFont = App.Current.Resources.TryGetValue("FontAwesomeSolid", out var ff) ? (Microsoft.UI.Xaml.Media.FontFamily)ff : new Microsoft.UI.Xaml.Media.FontFamily("Assets/Fonts/fa-solid-900.ttf#Font Awesome 6 Free Solid");

                // Khôi phục biểu tượng thanh điều hướng Navigation View sang Fluent 2 FontAwesome
                if (NavOverviewIcon != null) { NavOverviewIcon.FontFamily = faFont; NavOverviewIcon.Glyph = "\uf185"; NavOverviewIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)); }
                if (NavFloodIcon != null) { NavFloodIcon.FontFamily = faFont; NavFloodIcon.Glyph = "\uf773"; NavFloodIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 2, 132, 199)); }
                if (NavRadarIcon != null) { NavRadarIcon.FontFamily = faFont; NavRadarIcon.Glyph = "\uf57d"; NavRadarIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)); }
                if (NavLifestyleIcon != null) { NavLifestyleIcon.FontFamily = faFont; NavLifestyleIcon.Glyph = "\uf553"; NavLifestyleIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 236, 72, 153)); }
                if (NavCalendarIcon != null) { NavCalendarIcon.FontFamily = faFont; NavCalendarIcon.Glyph = "\uf073"; NavCalendarIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 139, 92, 246)); }
                if (NavWidgetIcon != null) { NavWidgetIcon.FontFamily = faFont; NavWidgetIcon.Glyph = "\uf2d0"; NavWidgetIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 56, 189, 248)); }
                if (NavSettingsIcon != null) { NavSettingsIcon.FontFamily = faFont; NavSettingsIcon.Glyph = "\uf013"; NavSettingsIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 148, 163, 184)); }

                // Khôi phục biểu tượng nút chức năng Floating Header sang Fluent 2 FontAwesome
                if (GpsButtonIcon != null) { GpsButtonIcon.FontFamily = faFont; GpsButtonIcon.Glyph = "\uf3c5"; }
                if (ZenButtonIcon != null) { ZenButtonIcon.FontFamily = faFont; ZenButtonIcon.Glyph = "\uf6c4"; }
                if (RefreshButtonIcon != null) { RefreshButtonIcon.FontFamily = faFont; RefreshButtonIcon.Glyph = "\uf2f1"; }
                if (UnitButtonIcon != null) { UnitButtonIcon.FontFamily = faFont; UnitButtonIcon.Glyph = "\uf2c9"; }
                if (DesktopWidgetButtonIcon != null) { DesktopWidgetButtonIcon.FontFamily = faFont; DesktopWidgetButtonIcon.Glyph = "\uf2d0"; }

                TabOverview?.UpdateNothingThemeVisuals();
                TabRadar?.UpdateNothingVisuals();
                TabFlood?.UpdateNothingVisuals();

                // ==================== SPATIAL VISION (AURORA GLASS 3.7) OVERLAYS ====================
                if (isSpatialGlass)
                {
                    if (SpatialGradStop1 != null && SpatialGradStop2 != null && SpatialGradStop3 != null)
                    {
                        if (isDark)
                        {
                            SpatialGradStop1.Color = Windows.UI.Color.FromArgb(255, 5, 9, 20);
                            SpatialGradStop2.Color = Windows.UI.Color.FromArgb(255, 12, 25, 56);
                            SpatialGradStop3.Color = Windows.UI.Color.FromArgb(255, 7, 34, 59);
                        }
                        else
                        {
                            SpatialGradStop1.Color = Windows.UI.Color.FromArgb(255, 224, 242, 254);
                            SpatialGradStop2.Color = Windows.UI.Color.FromArgb(255, 237, 233, 254);
                            SpatialGradStop3.Color = Windows.UI.Color.FromArgb(255, 240, 253, 250);
                        }
                    }

                    Microsoft.UI.Xaml.Media.LinearGradientBrush CreateSpecularBorderBrush()
                    {
                        var b = new Microsoft.UI.Xaml.Media.LinearGradientBrush
                        {
                            StartPoint = new Windows.Foundation.Point(0, 0),
                            EndPoint = new Windows.Foundation.Point(1, 1)
                        };
                        if (isDark)
                        {
                            b.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = Windows.UI.Color.FromArgb(125, 255, 255, 255), Offset = 0.0 });
                            b.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = Windows.UI.Color.FromArgb(55, 56, 189, 248), Offset = 0.5 });
                            b.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = Windows.UI.Color.FromArgb(95, 168, 85, 247), Offset = 1.0 });
                        }
                        else
                        {
                            b.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = Windows.UI.Color.FromArgb(245, 255, 255, 255), Offset = 0.0 });
                            b.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = Windows.UI.Color.FromArgb(110, 56, 189, 248), Offset = 0.5 });
                            b.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = Windows.UI.Color.FromArgb(110, 139, 92, 246), Offset = 1.0 });
                        }
                        return b;
                    }

                    var glassFillColor = isDark
                        ? Windows.UI.Color.FromArgb(46, 15, 23, 42)
                        : Windows.UI.Color.FromArgb(195, 255, 255, 255);
                    var subtleGlassColor = isDark
                        ? Windows.UI.Color.FromArgb(36, 255, 255, 255)
                        : Windows.UI.Color.FromArgb(165, 241, 245, 249);

                    this.Resources["CardBackgroundFillColorDefaultBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(glassFillColor);
                    this.Resources["CardStrokeColorDefaultBrush"] = CreateSpecularBorderBrush();
                    this.Resources["SubtleFillColorSecondaryBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(subtleGlassColor);

                    if (FloatingIslandHeader != null)
                    {
                        FloatingIslandHeader.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                            isDark ? Windows.UI.Color.FromArgb(160, 8, 15, 32) : Windows.UI.Color.FromArgb(215, 255, 255, 255));
                        FloatingIslandHeader.BorderBrush = CreateSpecularBorderBrush();
                    }
                }

                TabOverview?.UpdateWeatherVisuals();
                TabOverview?.RedrawCanvases();
            }

            UpdateFloatingDockVisuals(_currentActiveTabTag);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ApplyDesignLanguageVisuals] Error: {ex.Message}");
        }
        finally
        {
            _isApplyingVisuals = false;
        }
    }

    public void UpdateFloatingDockVisuals(string? activeTag = null)
    {
        try
        {
            string tag = (activeTag ?? _currentActiveTabTag ?? "overview").ToLowerInvariant();
            bool isFloatingDock = ViewModel?.IsFloatingDockMode ?? true;
            if (NavContentRootGrid != null)
            {
                NavContentRootGrid.Margin = isFloatingDock ? new Thickness(0, 0, 0, 80) : new Thickness(12, 0, 0, 0);
            }

            if (FloatingBottomDock == null) return;

            string dl = ViewModel?.SelectedDesignLanguage ?? "Fluent2";
            bool isNothing = string.Equals(dl, "Nothing", StringComparison.OrdinalIgnoreCase);
            bool isSpatialGlass = string.Equals(dl, "SpatialGlass", StringComparison.OrdinalIgnoreCase);
            bool isEInk = isNothing && (ViewModel?.IsNothingEInkMode == true);
            bool isDark = isEInk ? false : ThemeHelper.IsDarkTheme(this);

            if (isNothing)
            {
                var dockBg = isEInk
                    ? Windows.UI.Color.FromArgb(248, 250, 247, 240)
                    : (isDark ? Windows.UI.Color.FromArgb(244, 12, 12, 15) : Windows.UI.Color.FromArgb(246, 246, 246, 248));
                var dockBorder = isEInk
                    ? Windows.UI.Color.FromArgb(255, 200, 194, 180)
                    : (isDark ? Windows.UI.Color.FromArgb(255, 46, 46, 52) : Windows.UI.Color.FromArgb(255, 212, 212, 218));
                FloatingBottomDock.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(dockBg);
                FloatingBottomDock.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(dockBorder);
            }
            else if (isSpatialGlass)
            {
                var dockBg = isDark
                    ? Windows.UI.Color.FromArgb(215, 7, 14, 30)
                    : Windows.UI.Color.FromArgb(230, 245, 250, 255);
                var specBorder = new Microsoft.UI.Xaml.Media.LinearGradientBrush
                {
                    StartPoint = new Windows.Foundation.Point(0, 0),
                    EndPoint = new Windows.Foundation.Point(1, 1)
                };
                specBorder.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = Windows.UI.Color.FromArgb(180, 255, 255, 255), Offset = 0.0 });
                specBorder.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = Windows.UI.Color.FromArgb(140, 56, 189, 248), Offset = 0.5 });
                specBorder.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = Windows.UI.Color.FromArgb(150, 168, 85, 247), Offset = 1.0 });
                FloatingBottomDock.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(dockBg);
                FloatingBottomDock.BorderBrush = specBorder;
            }
            else
            {
                var dockBg = isDark
                    ? Windows.UI.Color.FromArgb(236, 15, 23, 42)
                    : Windows.UI.Color.FromArgb(242, 255, 255, 255);
                var dockBorder = isDark
                    ? Windows.UI.Color.FromArgb(90, 56, 189, 248)
                    : Windows.UI.Color.FromArgb(140, 148, 163, 184);
                FloatingBottomDock.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(dockBg);
                FloatingBottomDock.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(dockBorder);
            }

            var activeColor = isNothing
                ? Windows.UI.Color.FromArgb(255, 215, 25, 33)
                : (isDark ? Windows.UI.Color.FromArgb(255, 56, 189, 248) : Windows.UI.Color.FromArgb(255, 2, 132, 199));
            var activePillBg = isNothing
                ? (isDark ? Windows.UI.Color.FromArgb(55, 215, 25, 33) : Windows.UI.Color.FromArgb(32, 215, 25, 33))
                : (isDark ? Windows.UI.Color.FromArgb(48, 56, 189, 248) : Windows.UI.Color.FromArgb(35, 2, 132, 199));
            var activePillBorder = isNothing
                ? Windows.UI.Color.FromArgb(160, 215, 25, 33)
                : (isDark ? Windows.UI.Color.FromArgb(140, 56, 189, 248) : Windows.UI.Color.FromArgb(120, 2, 132, 199));

            var dockItems = new (Button? Btn, Border? Ind, TextBlock? Txt, string ItemTag)[]
            {
                (DockBtnOverview, DockIndOverview, DockTextOverview, "overview"),
                (DockBtnFlood, DockIndFlood, DockTextFlood, "flood"),
                (DockBtnRadar, DockIndRadar, DockTextRadar, "radar"),
                (DockBtnLifestyle, DockIndLifestyle, DockTextLifestyle, "lifestyle"),
                (DockBtnCalendar, DockIndCalendar, DockTextCalendar, "calendar"),
                (DockBtnWidget, DockIndWidget, DockTextWidget, "widget"),
                (DockBtnSettings, DockIndSettings, DockTextSettings, "settings")
            };

            var transparent = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
            var activeIndBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(activeColor);
            var activeBgBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(activePillBg);
            var activeBorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(activePillBorder);

            foreach (var (btn, ind, txt, itemTag) in dockItems)
            {
                bool isSel = itemTag == tag;
                if (btn != null)
                {
                    btn.Background = isSel ? activeBgBrush : transparent;
                    btn.BorderBrush = isSel ? activeBorderBrush : transparent;
                }
                if (ind != null)
                {
                    ind.Background = isSel ? activeIndBrush : transparent;
                }
                if (txt != null)
                {
                    txt.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                        isDark ? Windows.UI.Color.FromArgb(255, 248, 250, 252) : Windows.UI.Color.FromArgb(255, 20, 20, 19));
                    txt.Opacity = isSel ? 1.0 : 0.78;
                }
            }
        }
        catch { }
    }

    private async void QuickLocationChip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is QuickLocationChipItem chip && ViewModel != null)
        {
            Helpers.MechanicalSoundHelper.PlayClick();
            if (AllCitiesFlyoutBtn?.Flyout != null) AllCitiesFlyoutBtn.Flyout.Hide();
            await ViewModel.SelectQuickChipAsync(chip);
        }
    }

    private static bool TryMatchDirectCommand(string input)
    {
        string c = input.Trim().ToLowerInvariant();
        return c == "nothing" || c == "fluent" || c == "spatial" || c == "aurora" ||
               c == "eink" || c == "dock" || c == "sidebar" || c == "dark" || c == "light" ||
               c == "zen" || c == "pomodoro" || c == "focus" || c == "widget" ||
               c == "duel" || c == "compare";
    }

    private void QuickCommand_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string cmd)
        {
            CommandPaletteFlyout?.Hide();
            DesignSwitcherFlyout?.Hide();
            ExecuteQuickCommand(cmd);
        }
    }

    private void ExecuteQuickCommand(string cmd)
    {
        if (ViewModel == null || string.IsNullOrWhiteSpace(cmd)) return;
        Helpers.MechanicalSoundHelper.PlayClick();
        string norm = cmd.Trim().ToLowerInvariant();
        switch (norm)
        {
            case "nothing":
                ViewModel.ApplyDesignLanguage("Nothing");
                ApplyDesignLanguageVisuals("Nothing");
                (App.MainWindow as MainWindow)?.UpdateWindowTheme();
                break;
            case "spatial":
            case "aurora":
            case "spatialglass":
                ViewModel.ApplyDesignLanguage("SpatialGlass");
                ApplyDesignLanguageVisuals("SpatialGlass");
                (App.MainWindow as MainWindow)?.UpdateWindowTheme();
                break;
            case "fluent":
            case "fluent2":
                ViewModel.ApplyDesignLanguage("Fluent2");
                ApplyDesignLanguageVisuals("Fluent2");
                (App.MainWindow as MainWindow)?.UpdateWindowTheme();
                break;
            case "eink":
                ViewModel.ApplyDesignLanguage("Nothing");
                ViewModel.ToggleNothingEInkModeCommand.Execute(null);
                ApplyDesignLanguageVisuals("Nothing");
                (App.MainWindow as MainWindow)?.UpdateWindowTheme();
                break;
            case "dock":
            case "sidebar":
                ViewModel.ToggleNavigationLayoutCommand.Execute(null);
                UpdateFloatingDockVisuals(_currentActiveTabTag);
                break;
            case "dark":
                ViewModel.ChangeTheme("Dark");
                ApplyDesignLanguageVisuals(ViewModel.SelectedDesignLanguage);
                (App.MainWindow as MainWindow)?.UpdateWindowTheme();
                break;
            case "light":
                ViewModel.ChangeTheme("Light");
                ApplyDesignLanguageVisuals(ViewModel.SelectedDesignLanguage);
                (App.MainWindow as MainWindow)?.UpdateWindowTheme();
                break;
            case "zen":
            case "standby":
                ViewModel.ToggleZenModeCommand.Execute(null);
                break;
            case "pomodoro":
            case "focus":
                if (!ViewModel.IsZenModeOpen) ViewModel.ToggleZenModeCommand.Execute(null);
                if (!ViewModel.IsPomodoroRunning) ViewModel.TogglePomodoroRunCommand.Execute(null);
                break;
            case "widget":
                SelectNavigationTabByTag("widget");
                break;
            case "radar":
                SelectNavigationTabByTag("radar");
                break;
            case "flood":
                SelectNavigationTabByTag("flood");
                break;
            case "duel":
            case "compare":
                SelectNavigationTabByTag("overview");
                _ = ViewModel.OpenWeatherComparisonAsync();
                break;
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
        if (OffscreenExportHost != null) OffscreenExportHost.Visibility = Visibility.Visible;
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
            if (OffscreenExportHost != null) OffscreenExportHost.Visibility = Visibility.Collapsed;
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
        string currentVersion = "3.7.1";
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
            (ChangelogContent_v370, "3.7.0"),
            (ChangelogContent_v360, "3.6.1"),
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
                "3.7.0" => isVi ? "Siêu Cập Nhật Weather WinUI v3.7.0" : "Official Release Notes v3.7.0",
                "3.6.1" => isVi ? "Siêu Cập Nhật Weather WinUI v3.6.1" : "Official Release Notes v3.6.1",
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
            if (versionTag == "3.6.1")
            {
                ChangelogHeaderBadge.Text = isVi ? "v3.6.1 BẢN CHÍNH THỨC" : "v3.6.1 OFFICIAL";
            }
            else if (versionTag == "3.5.0")
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
            if (versionTag == "3.6.1")
            {
                ChangelogHeaderSubtitle.Text = isVi
                    ? "Đột phá toàn diện UI/UX Zero-Chrome & Spatial Bento, Command Palette (Ctrl+K), Radar Mưa 60 Phút Tới, Đồ Thị Nhiệt Độ 24h Tương Tác, Zen Mode 2.0 Nothing StandBy + Pomodoro, và Bộ 3 Widget 3.0 (Dynamic Island, Field Deck, Pure Typo HUD + Ghost Mode)."
                    : "Comprehensive overhaul featuring Zero-Chrome Command Header (Ctrl+K), 60-Min Precipitation Nowcast, Interactive 24h Curve Scrubber, Zen Mode 2.0 Nothing StandBy + Pomodoro, and Desktop Widgets 3.0.";
            }
            else if (versionTag == "3.5.0")
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
