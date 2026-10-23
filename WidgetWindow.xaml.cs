using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using WeatherApp.Helpers;
using WeatherApp.Models;
using WeatherApp.Services;
using WeatherApp.ViewModels;

namespace WeatherApp;

public sealed partial class WidgetWindow : Window
{
    // Quản lý tất cả Widget đang mở trên toàn hệ thống
    public static readonly List<WidgetWindow> ActiveWidgets = new();

    public static void CloseAllWidgets()
    {
        var list = ActiveWidgets.ToList();
        foreach (var w in list)
        {
            try { w.Close(); } catch { }
        }
        ActiveWidgets.Clear();
    }

    private readonly MainViewModel? _viewModel;
    private readonly IWeatherService _weatherService;
    private readonly ILocationService _locationService;
    private readonly SettingsService _settingsService;
    private readonly AppSettings _settings;

    private readonly DispatcherTimer _clockTimer = new();
    private readonly DispatcherTimer _weatherAutoRefreshTimer = new();
    private AppWindow? _appWindow;
    private bool _isAlwaysOnTop = true;
    private string _currentStyle = "GlassCard";
    private SeasonalSkinDefinition? _seasonalSkin;

    public string LocationName { get; private set; } = "Hà Nội";
    public double Latitude { get; private set; } = 21.0285;
    public double Longitude { get; private set; } = 105.8542;

    private CurrentWeatherDisplay? _currentWeather;
    private List<HourlyForecastItem> _hourlyForecast = new();
    private List<DailyForecastItem> _dailyForecast = new();

    // Trạng thái kéo thả mượt mà (True Drag and Drop)
    private bool _isDragging;
    private POINT _dragStartCursor;
    private Windows.Graphics.PointInt32 _dragStartWindow;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    [DllImport("user32.dll")]
    private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern IntPtr GetWindowLong32(IntPtr hWnd, int nIndex);

    private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
    {
        return IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, nIndex) : GetWindowLong32(hWnd, nIndex);
    }

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern IntPtr SetWindowLong32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        return IntPtr.Size == 8 ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong) : SetWindowLong32(hWnd, nIndex, dwNewLong);
    }

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private const int GWL_STYLE = -16;
    private const int GWL_EXSTYLE = -20;
    private const uint WS_POPUP = 0x80000000;
    private const uint WS_CAPTION = 0x00C00000;
    private const uint WS_THICKFRAME = 0x00040000;
    private const uint WS_MINIMIZEBOX = 0x00020000;
    private const uint WS_MAXIMIZEBOX = 0x00010000;
    private const uint WS_BORDER = 0x00800000;
    private const uint WS_DLGFRAME = 0x00400000;

    private const uint WS_EX_WINDOWEDGE = 0x00000100;
    private const uint WS_EX_CLIENTEDGE = 0x00000200;
    private const uint WS_EX_STATICEDGE = 0x00020000;
    private const uint WS_EX_DLGMODALFRAME = 0x00000001;
    private const uint WS_EX_TRANSPARENT = 0x00000020;
    private const uint WS_EX_LAYERED = 0x00080000;

    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_FRAMECHANGED = 0x0020;

    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWCP_DONOTROUND = 1;
    private const int DWMWA_BORDER_COLOR = 34;
    private const int DWMWA_COLOR_NONE = unchecked((int)0xFFFFFFFE);

    public WidgetWindow(MainViewModel viewModel)
        : this(viewModel.LocationTitle, viewModel.Settings.LastLatitude, viewModel.Settings.LastLongitude, viewModel.Settings, viewModel)
    {
    }

    public WidgetWindow(MainViewModel? viewModel, string locationName, double latitude, double longitude)
        : this(locationName, latitude, longitude, viewModel?.Settings, viewModel)
    {
    }

    public WidgetWindow(string locationName, double latitude, double longitude, AppSettings? settings = null, MainViewModel? viewModel = null)
    {
        _viewModel = viewModel;
        _settingsService = new SettingsService();
        _settings = settings ?? _settingsService.LoadSettings();
        _weatherService = new WeatherService();
        _locationService = new LocationService();

        LocationName = string.IsNullOrWhiteSpace(locationName) ? (_settings.LastLocationName ?? "Hà Nội") : locationName;
        Latitude = latitude != 0 ? latitude : _settings.LastLatitude;
        Longitude = longitude != 0 ? longitude : _settings.LastLongitude;
        _isAlwaysOnTop = _settings.WidgetAlwaysOnTop;

        InitializeComponent();

        ActiveWidgets.Add(this);
        this.Closed += (s, e) => ActiveWidgets.Remove(this);

        try
        {
            this.SystemBackdrop = new WinUIEx.TransparentTintBackdrop();
        }
        catch { }

        IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        if (_appWindow?.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = _isAlwaysOnTop;
            presenter.IsResizable = false;
            presenter.IsMinimizable = false;
            presenter.IsMaximizable = false;
            presenter.SetBorderAndTitleBar(false, false);
        }

        // TRIỆT TIÊU TOÀN BỘ KHUNG VIỀN TRẮNG CỦA CỬA SỔ
        StripWindowBorders(hWnd);

        // Khởi tạo giao diện theo style và độ mờ đã lưu trong Settings (Mặc định: BryanCDynamic phong cách Dribbble tuyệt đẹp)
        _currentStyle = _settings.WidgetStyle ?? "BryanCDynamic";
        ApplyWidgetStyle(_currentStyle);
        WidgetRoot.Opacity = Math.Clamp(_settings.WidgetOpacity, 0.2, 1.0);

        // Đặt vị trí thông minh (tự lệch vị trí nếu đã có widget khác trên Desktop)
        PositionWidgetWindow(windowId, hWnd);

        UpdatePinButtonVisual();

        // Đồng hồ số
        _clockTimer.Interval = TimeSpan.FromSeconds(1);
        _clockTimer.Tick += (s, e) => UpdateClockText();
        _clockTimer.Start();
        UpdateClockText();

        // Auto Refresh định kỳ độc lập cho Widget (mặc định 30 phút)
        int refreshInterval = _settings.AutoRefreshIntervalMinutes > 0 ? _settings.AutoRefreshIntervalMinutes : 30;
        _weatherAutoRefreshTimer.Interval = TimeSpan.FromMinutes(refreshInterval);
        _weatherAutoRefreshTimer.Tick += async (s, e) => await ReloadWeatherAsync();
        _weatherAutoRefreshTimer.Start();

        // Nếu có ViewModel và khớp địa điểm thì đồng bộ lần đầu
        if (_viewModel != null && _viewModel.CurrentWeather != null && LocationName == _viewModel.LocationTitle)
        {
            _currentWeather = _viewModel.CurrentWeather;
            _hourlyForecast = _viewModel.HourlyForecast?.ToList() ?? new List<HourlyForecastItem>();
            _dailyForecast = _viewModel.DailyForecast?.ToList() ?? new List<DailyForecastItem>();
            UpdateWidgetData();
        }
        else
        {
            _ = ReloadWeatherAsync();
        }
    }

    private void StripWindowBorders(IntPtr hWnd)
    {
        try
        {
            // XÓA TRIỆT ĐỂ GDI WINDOW REGION:
            // DWM trong Windows khi gặp Window Region sẽ tự vẽ viền DWM trắng 1px xung quanh đường biên region (gây ra hiện tượng viền trắng răng cưa).
            // Khi gỡ Region và dùng TransparentTintBackdrop, DirectX DirectComposition sẽ hiển thị độ trong suốt và khử răng cưa mượt 100%.
            SetWindowRgn(hWnd, IntPtr.Zero, true);

            long style = GetWindowLongPtr(hWnd, GWL_STYLE).ToInt64();
            style &= ~(WS_CAPTION | WS_THICKFRAME | WS_BORDER | WS_DLGFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX);
            style |= WS_POPUP;
            SetWindowLongPtr(hWnd, GWL_STYLE, new IntPtr(style));

            long exStyle = GetWindowLongPtr(hWnd, GWL_EXSTYLE).ToInt64();
            exStyle &= ~(WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE | WS_EX_STATICEDGE | WS_EX_DLGMODALFRAME);
            if (_settings.WidgetGhostClickThrough)
            {
                exStyle |= (WS_EX_TRANSPARENT | WS_EX_LAYERED);
            }
            else
            {
                exStyle &= ~WS_EX_TRANSPARENT;
            }
            SetWindowLongPtr(hWnd, GWL_EXSTYLE, new IntPtr(exStyle));

            SetWindowPos(hWnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);

            if (Environment.OSVersion.Version.Build >= 22000)
            {
                int noBorderColor = DWMWA_COLOR_NONE;
                DwmSetWindowAttribute(hWnd, DWMWA_BORDER_COLOR, ref noBorderColor, sizeof(int));

                int roundPref = DWMWCP_DONOTROUND;
                DwmSetWindowAttribute(hWnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref roundPref, sizeof(int));
            }
        }
        catch { }
    }

    private void PositionWidgetWindow(Microsoft.UI.WindowId windowId, IntPtr hWnd)
    {
        var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
        if (displayArea != null && _appWindow != null)
        {
            uint dpi = GetDpiForWindow(hWnd);
            if (dpi == 0) dpi = 96;
            double scale = dpi / 96.0;
            int physicalWidth = (int)Math.Round(385 * scale);

            int offsetCount = Math.Max(0, ActiveWidgets.Count - 1);
            if (offsetCount == 0 && _settings.WidgetLastX >= 0 && _settings.WidgetLastY >= 0)
            {
                int x = _settings.WidgetLastX;
                int y = _settings.WidgetLastY;
                if (x < displayArea.WorkArea.X + displayArea.WorkArea.Width - 50 &&
                    y < displayArea.WorkArea.Y + displayArea.WorkArea.Height - 50 &&
                    x >= displayArea.WorkArea.X - 100 &&
                    y >= displayArea.WorkArea.Y)
                {
                    _appWindow.Move(new Windows.Graphics.PointInt32(x, y));
                    return;
                }
            }

            int defaultX = displayArea.WorkArea.Width - physicalWidth - 30 - (offsetCount * 30);
            int defaultY = 60 + (offsetCount * 70);

            _appWindow.Move(new Windows.Graphics.PointInt32(defaultX, defaultY));
        }
    }

    public void ApplySeasonalTheme(SeasonalSkinDefinition skin)
    {
        _seasonalSkin = skin;
        _currentStyle = "SeasonalCard";
        _settings.SeasonalWidgetTheme = skin.Id.ToString();
        _settings.WidgetStyle = "SeasonalCard";
        _settingsService.SaveSettings(_settings);
        if (_viewModel != null)
        {
            _viewModel.Settings.SeasonalWidgetTheme = skin.Id.ToString();
            _viewModel.Settings.WidgetStyle = "SeasonalCard";
        }
        ApplyWidgetStyle("SeasonalCard");
        ApplySeasonalSkinVisuals(skin);
    }

    private void ApplySeasonalSkinVisuals(SeasonalSkinDefinition skin)
    {
        try
        {
            var brush = new Microsoft.UI.Xaml.Media.LinearGradientBrush { StartPoint = new Windows.Foundation.Point(0, 0), EndPoint = new Windows.Foundation.Point(1, 1) };
            brush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = ParseHexColor(skin.WidgetGradientStartHex), Offset = 0 });
            brush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = ParseHexColor(skin.WidgetGradientEndHex), Offset = 1 });
            WidgetRoot.Background = brush;

            WidgetRoot.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(ParseHexColor(skin.WidgetBorderHex));

            if (SeasonalEmojiText != null) SeasonalEmojiText.Text = skin.BadgeEmoji;
            if (SeasonalTagText != null)
            {
                SeasonalTagText.Text = skin.DisplayName;
                try { SeasonalTagText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(ParseHexColor(skin.WidgetTitleColorHex)); } catch { }
            }
            if (SeasonalGreetingText != null) SeasonalGreetingText.Text = skin.WidgetSeasonalGreeting;

            UpdateSeasonalArtLayer(skin.Id);
        }
        catch { }
    }

    private void UpdateSeasonalArtLayer(SeasonalSkinId skinId)
    {
        if (SeasonalTetArt == null) return;
        SeasonalTetArt.Visibility = Visibility.Collapsed;
        SeasonalChristmasArt.Visibility = Visibility.Collapsed;
        SeasonalHalloweenArt.Visibility = Visibility.Collapsed;
        SeasonalMidAutumnArt.Visibility = Visibility.Collapsed;
        SeasonalAutumnArt.Visibility = Visibility.Collapsed;
        SeasonalSpringArt.Visibility = Visibility.Collapsed;
        SeasonalSummerArt.Visibility = Visibility.Collapsed;
        SeasonalWinterArt.Visibility = Visibility.Collapsed;

        switch (skinId)
        {
            case SeasonalSkinId.Tet:
                SeasonalTetArt.Visibility = Visibility.Visible;
                break;
            case SeasonalSkinId.Christmas:
                SeasonalChristmasArt.Visibility = Visibility.Visible;
                break;
            case SeasonalSkinId.Halloween:
                SeasonalHalloweenArt.Visibility = Visibility.Visible;
                break;
            case SeasonalSkinId.MidAutumn:
                SeasonalMidAutumnArt.Visibility = Visibility.Visible;
                break;
            case SeasonalSkinId.Autumn:
                SeasonalAutumnArt.Visibility = Visibility.Visible;
                break;
            case SeasonalSkinId.Spring:
                SeasonalSpringArt.Visibility = Visibility.Visible;
                break;
            case SeasonalSkinId.Summer:
                SeasonalSummerArt.Visibility = Visibility.Visible;
                break;
            case SeasonalSkinId.Winter:
                SeasonalWinterArt.Visibility = Visibility.Visible;
                break;
            default:
                SeasonalTetArt.Visibility = Visibility.Visible;
                break;
        }
    }

    public void ApplyWidgetStyle(string style)
    {
        _currentStyle = style;
        bool isBryanC = style == "BryanCDynamic";
        bool isIsland = style == "MiniIsland";
        bool isCompact = style == "Compact";
        bool isGlass = style == "GlassCard";
        bool isSeasonal = style == "SeasonalCard" || style.StartsWith("Seasonal");
        bool isBentoVivid = style == "BentoVivid";
        bool isBentoTactical = style == "BentoTactical";
        bool isRetroLcd = style == "RetroLcd";
        bool isNothingBento = style == "NothingBento";
        bool isNothingSquare = style == "NothingSquare";
        bool isNothingCircular = style == "NothingCircular";
        bool isNothingPill = style == "NothingPill";
        bool isDynamicIsland = style == "DynamicIsland";
        bool isTeenageDeck = style == "TeenageDeck";
        bool isPureTypo = style == "PureTypoHud";
        bool isTaskbarStrip = style == "TaskbarStrip";
        bool isSpatialCube = style == "SpatialCube";
        bool isOotdCapsule = style == "OotdCapsule";

        if (!isBryanC && !isIsland && !isCompact && !isGlass && !isSeasonal && !isBentoVivid && !isBentoTactical && !isRetroLcd && !isNothingBento && !isNothingSquare && !isNothingCircular && !isNothingPill && !isDynamicIsland && !isTeenageDeck && !isPureTypo && !isTaskbarStrip && !isSpatialCube && !isOotdCapsule)
        {
            isBryanC = true;
            _currentStyle = "BryanCDynamic";
        }

        if (BryanCDynamicView != null) BryanCDynamicView.Visibility = isBryanC ? Visibility.Visible : Visibility.Collapsed;
        if (GlassCardView != null) GlassCardView.Visibility = isGlass ? Visibility.Visible : Visibility.Collapsed;
        if (CompactBarView != null) CompactBarView.Visibility = isCompact ? Visibility.Visible : Visibility.Collapsed;
        if (MiniIslandView != null) MiniIslandView.Visibility = isIsland ? Visibility.Visible : Visibility.Collapsed;
        if (SeasonalFestivalView != null) SeasonalFestivalView.Visibility = isSeasonal ? Visibility.Visible : Visibility.Collapsed;
        if (BentoVividView != null) BentoVividView.Visibility = isBentoVivid ? Visibility.Visible : Visibility.Collapsed;
        if (BentoTacticalView != null) BentoTacticalView.Visibility = isBentoTactical ? Visibility.Visible : Visibility.Collapsed;
        if (RetroLcdView != null) RetroLcdView.Visibility = isRetroLcd ? Visibility.Visible : Visibility.Collapsed;
        if (NothingBentoView != null) NothingBentoView.Visibility = isNothingBento ? Visibility.Visible : Visibility.Collapsed;
        if (NothingSquareView != null) NothingSquareView.Visibility = isNothingSquare ? Visibility.Visible : Visibility.Collapsed;
        if (NothingCircularView != null) NothingCircularView.Visibility = isNothingCircular ? Visibility.Visible : Visibility.Collapsed;
        if (NothingPillView != null) NothingPillView.Visibility = isNothingPill ? Visibility.Visible : Visibility.Collapsed;
        if (DynamicIslandView != null) DynamicIslandView.Visibility = isDynamicIsland ? Visibility.Visible : Visibility.Collapsed;
        if (TeenageDeckView != null) TeenageDeckView.Visibility = isTeenageDeck ? Visibility.Visible : Visibility.Collapsed;
        if (PureTypoHudView != null) PureTypoHudView.Visibility = isPureTypo ? Visibility.Visible : Visibility.Collapsed;
        if (TaskbarStripView != null) TaskbarStripView.Visibility = isTaskbarStrip ? Visibility.Visible : Visibility.Collapsed;
        if (SpatialCubeView != null) SpatialCubeView.Visibility = isSpatialCube ? Visibility.Visible : Visibility.Collapsed;
        if (OotdCapsuleView != null) OotdCapsuleView.Visibility = isOotdCapsule ? Visibility.Visible : Visibility.Collapsed;
        if (DynamicIslandExpandedPanel != null) DynamicIslandExpandedPanel.Visibility = Visibility.Collapsed;

        int cornerRadiusDip = style switch
        {
            "NothingCircular" => 98,
            "NothingPill" => 22,
            "DynamicIsland" => 24,
            "TeenageDeck" => 16,
            "PureTypoHud" => 0,
            "TaskbarStrip" => 20,
            "SpatialCube" => 22,
            "OotdCapsule" => 18,
            "NothingBento" => 24,
            "NothingSquare" => 22,
            "RetroLcd" => 16,
            "GlassCard" => 14,
            _ => 20
        };
        WidgetRoot.CornerRadius = new CornerRadius(cornerRadiusDip);

        IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        uint dpi = GetDpiForWindow(hWnd);
        if (dpi == 0) dpi = 96;
        double scale = dpi / 96.0;

        int widthDip = style switch
        {
            "NothingCircular" => 196,
            "NothingPill" => 250,
            "DynamicIsland" => 254,
            "TeenageDeck" => 286,
            "PureTypoHud" => 272,
            "TaskbarStrip" => 236,
            "SpatialCube" => 276,
            "OotdCapsule" => 310,
            "NothingBento" => 320,
            "NothingSquare" => 190,
            "BentoVivid" or "BentoTactical" => 365,
            "RetroLcd" => 265,
            "SeasonalCard" => 385,
            "BryanCDynamic" => 340,
            "MiniIsland" => 290,
            "Compact" => 300,
            _ => 385
        };

        int heightDip = style switch
        {
            "NothingCircular" => 196,
            "NothingPill" => 48,
            "DynamicIsland" => 50,
            "TeenageDeck" => 172,
            "PureTypoHud" => 136,
            "TaskbarStrip" => 42,
            "SpatialCube" => 220,
            "OotdCapsule" => 98,
            "NothingBento" => 160,
            "NothingSquare" => 190,
            "BentoVivid" or "BentoTactical" => 175,
            "RetroLcd" => 295,
            "SeasonalCard" => 215,
            "BryanCDynamic" => 200,
            "MiniIsland" => 40,
            "Compact" => 54,
            _ => 230
        };

        int physicalWidth = (int)Math.Round(widthDip * scale);
        int physicalHeight = (int)Math.Round(heightDip * scale);

        _appWindow?.Resize(new Windows.Graphics.SizeInt32(physicalWidth, physicalHeight));

        if (isSeasonal)
        {
            if (_seasonalSkin == null)
            {
                string theme = _settings.SeasonalWidgetTheme;
                if (string.IsNullOrEmpty(theme) || theme == "Auto")
                {
                    theme = _settings.SeasonalSkinMode;
                }
                _seasonalSkin = SeasonalSkinService.Instance.GetActiveSkin(theme);
            }
            ApplySeasonalSkinVisuals(_seasonalSkin);
            UpdateWidgetData();
        }
        else if (isBryanC && _currentWeather != null)
        {
            ApplyBryanCWeatherTheme(_currentWeather.WeatherEffect, _currentWeather.IsDay);
        }
        else if (isBentoVivid || isBentoTactical || isRetroLcd || isNothingBento || isNothingSquare || isNothingCircular || isNothingPill || isDynamicIsland || isTeenageDeck || isPureTypo || isTaskbarStrip || isSpatialCube || isOotdCapsule)
        {
            WidgetRoot.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0x00, 0x00, 0x00, 0x00));
            WidgetRoot.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0x00, 0x00, 0x00, 0x00));
            UpdateWidgetData();
        }
        else
        {
            // Khôi phục background kính tối chuẩn cho GlassCard/Compact
            var brush = new Microsoft.UI.Xaml.Media.LinearGradientBrush { StartPoint = new Windows.Foundation.Point(0, 0), EndPoint = new Windows.Foundation.Point(1, 1) };
            brush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = Windows.UI.Color.FromArgb(0xF2, 0x0F, 0x17, 0x2A), Offset = 0 });
            brush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = Windows.UI.Color.FromArgb(0xF2, 0x1E, 0x29, 0x3B), Offset = 1 });
            WidgetRoot.Background = brush;
            WidgetRoot.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(34, 255, 255, 255));
        }

        // Đảm bảo không có viền DWM trắng hay khung viền Win32 xuất hiện
        StripWindowBorders(hWnd);
    }

    private void ToggleStyleButton_Click(object sender, RoutedEventArgs e)
    {
        string newStyle = _currentStyle switch
        {
            "BryanCDynamic" => "GlassCard",
            "GlassCard" => "Compact",
            "Compact" => "MiniIsland",
            "MiniIsland" => "BentoVivid",
            "BentoVivid" => "BentoTactical",
            "BentoTactical" => "RetroLcd",
            "RetroLcd" => "NothingBento",
            "NothingBento" => "NothingSquare",
            "NothingSquare" => "NothingCircular",
            "NothingCircular" => "NothingPill",
            "NothingPill" => "DynamicIsland",
            "DynamicIsland" => "TeenageDeck",
            "TeenageDeck" => "PureTypoHud",
            "PureTypoHud" => "TaskbarStrip",
            "TaskbarStrip" => "SpatialCube",
            "SpatialCube" => "OotdCapsule",
            "OotdCapsule" => "SeasonalCard",
            _ => "BryanCDynamic"
        };

        _currentStyle = newStyle;
        _settings.WidgetStyle = newStyle;
        _settingsService.SaveSettings(_settings);

        if (_viewModel != null)
        {
            _viewModel.Settings.WidgetStyle = newStyle;
        }

        ApplyWidgetStyle(newStyle);
    }

    public void SetLockPosition(bool locked)
    {
        _settings.WidgetLockPosition = locked;
    }

    public void SetGhostClickThrough(bool enabled)
    {
        _settings.WidgetGhostClickThrough = enabled;
        try
        {
            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            StripWindowBorders(hWnd);
        }
        catch { }
    }

    private void DynamicIsland_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_currentStyle != "DynamicIsland" || _appWindow == null) return;
        try
        {
            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            uint dpi = GetDpiForWindow(hWnd);
            if (dpi == 0) dpi = 96;
            double scale = dpi / 96.0;
            if (DynamicIslandExpandedPanel != null) DynamicIslandExpandedPanel.Visibility = Visibility.Visible;
            if (DynamicIslandBorder != null) DynamicIslandBorder.CornerRadius = new CornerRadius(22);
            _appWindow.Resize(new Windows.Graphics.SizeInt32((int)Math.Round(324 * scale), (int)Math.Round(152 * scale)));
        }
        catch { }
    }

    private void DynamicIsland_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_currentStyle != "DynamicIsland" || _appWindow == null) return;
        try
        {
            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            uint dpi = GetDpiForWindow(hWnd);
            if (dpi == 0) dpi = 96;
            double scale = dpi / 96.0;
            if (DynamicIslandExpandedPanel != null) DynamicIslandExpandedPanel.Visibility = Visibility.Collapsed;
            if (DynamicIslandBorder != null) DynamicIslandBorder.CornerRadius = new CornerRadius(23);
            _appWindow.Resize(new Windows.Graphics.SizeInt32((int)Math.Round(254 * scale), (int)Math.Round(50 * scale)));
        }
        catch { }
    }

    private void CloseWidgetButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    public void SetOpacity(double opacity)
    {
        _settings.WidgetOpacity = opacity;
        WidgetRoot.Opacity = Math.Clamp(opacity, 0.2, 1.0);
    }

    private void WidgetRoot_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        WidgetRoot.Opacity = 1.0;
        WidgetRoot.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(140, 56, 189, 248));
    }

    private void WidgetRoot_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        WidgetRoot.Opacity = Math.Clamp(_settings.WidgetOpacity, 0.2, 1.0);
        WidgetRoot.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(34, 255, 255, 255));
    }

    private void BryanCActionsOverlay_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (BryanCActionsOverlay != null) BryanCActionsOverlay.Opacity = 1.0;
    }

    private void BryanCActionsOverlay_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (BryanCActionsOverlay != null) BryanCActionsOverlay.Opacity = 0.25;
    }

    private void UpdateClockText()
    {
        string clock = DateTime.Now.ToString(_settings.Is24HourFormat ? "HH:mm" : "hh:mm tt");
        WidgetClockText.Text = clock;
        CompactClockText.Text = clock;
        if (BryanCClockText != null) BryanCClockText.Text = clock;
        if (SeasonalClockText != null) SeasonalClockText.Text = clock;
        if (NothingBentoClockText != null) NothingBentoClockText.Text = clock;
        if (NothingSquareTimeText != null) NothingSquareTimeText.Text = clock;

        // Cập nhật đồng hồ kim cho BentoVivid & BentoTactical
        double hourAngle = ((DateTime.Now.Hour % 12) + DateTime.Now.Minute / 60.0) * 30.0;
        double minAngle = (DateTime.Now.Minute + DateTime.Now.Second / 60.0) * 6.0;

        if (BentoVividHourTransform != null) BentoVividHourTransform.Angle = hourAngle;
        if (BentoVividMinuteTransform != null) BentoVividMinuteTransform.Angle = minAngle;

        if (BentoTacticalHourTransform != null) BentoTacticalHourTransform.Angle = hourAngle;
        if (BentoTacticalMinuteTransform != null) BentoTacticalMinuteTransform.Angle = minAngle;

        if (BentoVividDayOfWeekText != null) BentoVividDayOfWeekText.Text = DateTime.Now.ToString("ddd");
        if (BentoVividDayNumText != null) BentoVividDayNumText.Text = DateTime.Now.ToString("dd");
        if (BentoVividClockSubText != null) BentoVividClockSubText.Text = DateTime.Now.ToString("ddd, MMM dd");
        if (BentoVividTzText != null)
        {
            var offset = TimeZoneInfo.Local.BaseUtcOffset;
            BentoVividTzText.Text = $"UTC {(offset.TotalHours >= 0 ? "+" : "")}{offset.Hours}";
        }

        if (BryanCDateText != null)
        {
            try
            {
                string dayOfWeek = DateTime.Now.ToString("ddd", new System.Globalization.CultureInfo("vi-VN")).ToUpperInvariant();
                string datePart = DateTime.Now.ToString("dd/MM");
                BryanCDateText.Text = $"{dayOfWeek} • {datePart}";
            }
            catch
            {
                BryanCDateText.Text = DateTime.Now.ToString("ddd dd-MM").ToUpperInvariant();
            }
        }
    }

    public async Task ReloadWeatherAsync()
    {
        try
        {
            WidgetCityText.Text = "Đang tải...";
            if (BryanCCityText != null) BryanCCityText.Text = "Đang tải...";

            var raw = await _weatherService.GetWeatherDataAsync(Latitude, Longitude);
            if (raw != null)
            {
                var aqi = await _weatherService.GetAirQualityAsync(Latitude, Longitude);
                _currentWeather = _weatherService.CreateCurrentWeatherDisplay(raw, LocationName, _settings, aqi);

                if (raw.Hourly != null)
                {
                    _hourlyForecast = _weatherService.CreateHourlyForecast(raw, _settings);
                }
                if (raw.Daily != null)
                {
                    _dailyForecast = _weatherService.CreateDailyForecast(raw, _settings);
                }

                DispatcherQueue.TryEnqueue(UpdateWidgetData);
            }
            else
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    WidgetCityText.Text = LocationName;
                    if (BryanCCityText != null) BryanCCityText.Text = LocationName;
                });
            }
        }
        catch
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                WidgetCityText.Text = LocationName;
                if (BryanCCityText != null) BryanCCityText.Text = LocationName;
            });
        }
    }

    public void UpdateWidgetData()
    {
        if (_currentWeather == null) return;

        string displayCity = LocationName;
        if (displayCity.EndsWith(", Việt Nam"))
        {
            displayCity = displayCity.Substring(0, displayCity.Length - 10).Trim();
        }
        WidgetCityText.Text = string.IsNullOrEmpty(displayCity) ? "Đang định vị..." : displayCity;

        WidgetTempText.Text = _currentWeather.TemperatureText;
        WidgetConditionText.Text = _currentWeather.ConditionText;
        WidgetFeelsLikeText.Text = _currentWeather.FeelsLikeText;

        // 5 Thẻ thông số nhanh
        var loc = LocalizationService.Instance;
        if (WidgetHumidityLabel != null) WidgetHumidityLabel.Text = loc.Humidity;
        if (WidgetWindLabel != null) WidgetWindLabel.Text = loc.Wind;
        if (WidgetRainLabel != null) WidgetRainLabel.Text = loc.IsVietnamese ? "Mưa" : "Rain";

        WidgetHumidityText.Text = _currentWeather.HumidityText;
        WidgetWindText.Text = _currentWeather.WindText;
        WidgetUvText.Text = _currentWeather.UvIndexText;
        WidgetRainProbText.Text = _currentWeather.RainProbabilityText;

        // AQI
        if (_currentWeather.HasAqi)
        {
            WidgetAqiText.Text = _currentWeather.AqiValue.ToString();
            try
            {
                if (!string.IsNullOrEmpty(_currentWeather.AqiColor))
                {
                    WidgetAqiText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(ParseHexColor(_currentWeather.AqiColor));
                }
            }
            catch { }
        }
        else
        {
            WidgetAqiText.Text = "--";
        }

        // SVG Icon thời tiết chính
        bool svgLoaded = false;
        try
        {
            string fileName = Path.GetFileName(_currentWeather.SvgIconPath);
            string localPath = Path.Combine(AppContext.BaseDirectory, "Assets", "weather-icons-main", "static", fileName);

            if (File.Exists(localPath))
            {
                var svgSource = new SvgImageSource(new Uri(localPath))
                {
                    RasterizePixelWidth = 96,
                    RasterizePixelHeight = 96
                };
                svgSource.OpenFailed += (s, e) =>
                {
                    WidgetWeatherIcon.Visibility = Visibility.Collapsed;
                    WidgetFallbackIcon.Visibility = Visibility.Visible;
                };
                WidgetWeatherIcon.Source = svgSource;
                WidgetWeatherIcon.Visibility = Visibility.Visible;
                WidgetFallbackIcon.Visibility = Visibility.Collapsed;
                svgLoaded = true;
            }
            else if (!string.IsNullOrEmpty(_currentWeather.SvgIconPath))
            {
                var uri = new Uri(_currentWeather.SvgIconPath);
                if (uri.IsAbsoluteUri)
                {
                    var svgSource = new SvgImageSource(uri)
                    {
                        RasterizePixelWidth = 96,
                        RasterizePixelHeight = 96
                    };
                    svgSource.OpenFailed += (s, e) =>
                    {
                        WidgetWeatherIcon.Visibility = Visibility.Collapsed;
                        WidgetFallbackIcon.Visibility = Visibility.Visible;
                    };
                    WidgetWeatherIcon.Source = svgSource;
                    WidgetWeatherIcon.Visibility = Visibility.Visible;
                    WidgetFallbackIcon.Visibility = Visibility.Collapsed;
                    svgLoaded = true;
                }
            }
        }
        catch { }

        if (!svgLoaded)
        {
            WidgetWeatherIcon.Visibility = Visibility.Collapsed;
            WidgetFallbackIcon.Glyph = string.IsNullOrEmpty(_currentWeather.IconGlyph) ? "\uf185" : _currentWeather.IconGlyph;
            WidgetFallbackIcon.Visibility = Visibility.Visible;
        }

        // Cập nhật Compact Bar
        CompactCityText.Text = displayCity;
        CompactTempText.Text = _currentWeather.TemperatureText;
        CompactCondText.Text = _currentWeather.ConditionText;

        if (svgLoaded)
        {
            CompactSvgImage.Source = WidgetWeatherIcon.Source;
            CompactSvgImage.Visibility = Visibility.Visible;
            CompactFallbackIcon.Visibility = Visibility.Collapsed;
        }
        else
        {
            CompactSvgImage.Visibility = Visibility.Collapsed;
            CompactFallbackIcon.Glyph = WidgetFallbackIcon.Glyph;
            CompactFallbackIcon.Visibility = Visibility.Visible;
        }

        // Cập nhật Mini Island Capsule
        IslandTempText.Text = _currentWeather.TemperatureText;
        IslandConditionText.Text = _currentWeather.ConditionText;

        if (_currentWeather.HasAqi && !string.IsNullOrEmpty(_currentWeather.AqiColor))
        {
            try
            {
                IslandAqiDot.Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(ParseHexColor(_currentWeather.AqiColor));
                IslandAqiDot.Visibility = Visibility.Visible;
            }
            catch
            {
                IslandAqiDot.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            IslandAqiDot.Visibility = Visibility.Collapsed;
        }

        if (!string.IsNullOrEmpty(_currentWeather.RainProbabilityText) && 
            _currentWeather.RainProbabilityText != "0%" && 
            _currentWeather.RainProbabilityText != "--%")
        {
            IslandRainBadge.Visibility = Visibility.Visible;
            IslandRainText.Text = _currentWeather.RainProbabilityText;
        }
        else
        {
            IslandRainBadge.Visibility = Visibility.Collapsed;
        }

        if (svgLoaded)
        {
            IslandWeatherIcon.Source = WidgetWeatherIcon.Source;
            IslandWeatherIcon.Visibility = Visibility.Visible;
            IslandFallbackIcon.Visibility = Visibility.Collapsed;
        }
        else
        {
            IslandWeatherIcon.Visibility = Visibility.Collapsed;
            IslandFallbackIcon.Glyph = WidgetFallbackIcon.Glyph;
            IslandFallbackIcon.Visibility = Visibility.Visible;
        }

        // Cập nhật cho BryanCDynamicView (Dribbble Weather Widget by BryanC)
        if (BryanCCityText != null) BryanCCityText.Text = displayCity;
        string tempOnly = _currentWeather.TemperatureText.Replace("°C", "°").Replace("°F", "°");
        if (BryanCTempText != null) BryanCTempText.Text = tempOnly;
        if (BryanCConditionText != null) BryanCConditionText.Text = _currentWeather.ConditionText;

        if (BryanCRangeText != null)
        {
            if (_dailyForecast != null && _dailyForecast.Count > 0)
            {
                var today = _dailyForecast[0];
                string max = today.TempMaxDisplay.Replace("°C", "°").Replace("°F", "°");
                string min = today.TempMinDisplay.Replace("°C", "°").Replace("°F", "°");
                BryanCRangeText.Text = $"{max} / {min}";
            }
            else
            {
                BryanCRangeText.Text = $"{tempOnly} / --°";
            }
        }

        if (svgLoaded && BryanCConditionIcon != null)
        {
            BryanCConditionIcon.Source = WidgetWeatherIcon.Source;
            BryanCConditionIcon.Visibility = Visibility.Visible;
            if (BryanCFallbackIcon != null) BryanCFallbackIcon.Visibility = Visibility.Collapsed;
        }
        else if (BryanCConditionIcon != null)
        {
            BryanCConditionIcon.Visibility = Visibility.Collapsed;
            if (BryanCFallbackIcon != null)
            {
                BryanCFallbackIcon.Glyph = WidgetFallbackIcon.Glyph;
                BryanCFallbackIcon.Visibility = Visibility.Visible;
            }
        }

        // Cập nhật dock 4 ngày dự báo tiếp theo
        UpdateBryanCForecastDock();

        // Cập nhật SeasonalFestivalView (Festive Seasonal Card)
        if (SeasonalCityText != null) SeasonalCityText.Text = displayCity;
        if (SeasonalTempText != null) SeasonalTempText.Text = tempOnly;
        if (SeasonalConditionText != null) SeasonalConditionText.Text = _currentWeather.ConditionText;
        if (SeasonalDateText != null) SeasonalDateText.Text = DateTime.Now.ToString("dd/MM");
        if (SeasonalLunarText != null) SeasonalLunarText.Text = _currentWeather.LunarDateText;
        if (SeasonalRangeText != null)
        {
            if (_dailyForecast != null && _dailyForecast.Count > 0)
            {
                var today = _dailyForecast[0];
                string max = today.TempMaxDisplay.Replace("°C", "°").Replace("°F", "°");
                string min = today.TempMinDisplay.Replace("°C", "°").Replace("°F", "°");
                SeasonalRangeText.Text = $"{max} / {min}";
            }
            else
            {
                SeasonalRangeText.Text = $"{tempOnly} / --°";
            }
        }
        if (SeasonalAqiText != null) SeasonalAqiText.Text = _currentWeather.HasAqi ? $"AQI {_currentWeather.AqiValue}" : "AQI --";
        if (SeasonalWindText != null) SeasonalWindText.Text = _currentWeather.WindText;
        if (SeasonalHumidityText != null) SeasonalHumidityText.Text = _currentWeather.HumidityText;

        if (svgLoaded && SeasonalWeatherIcon != null)
        {
            SeasonalWeatherIcon.Source = WidgetWeatherIcon.Source;
            SeasonalWeatherIcon.Visibility = Visibility.Visible;
            if (SeasonalFallbackIcon != null) SeasonalFallbackIcon.Visibility = Visibility.Collapsed;
        }
        else if (SeasonalWeatherIcon != null)
        {
            SeasonalWeatherIcon.Visibility = Visibility.Collapsed;
            if (SeasonalFallbackIcon != null)
            {
                SeasonalFallbackIcon.Glyph = WidgetFallbackIcon.Glyph;
                SeasonalFallbackIcon.Visibility = Visibility.Visible;
            }
        }

        if (_seasonalSkin != null)
        {
            if (SeasonalEmojiText != null) SeasonalEmojiText.Text = _seasonalSkin.BadgeEmoji;
            if (SeasonalTagText != null)
            {
                SeasonalTagText.Text = _seasonalSkin.DisplayName;
                try { SeasonalTagText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(ParseHexColor(_seasonalSkin.WidgetTitleColorHex)); } catch { }
            }
            if (SeasonalGreetingText != null) SeasonalGreetingText.Text = _seasonalSkin.WidgetSeasonalGreeting;
        }

        // Áp dụng màu sắc & hiệu ứng đồ họa động theo thời tiết cho phong cách BryanC
                // Bento Vivid
        if (BentoVividCityText != null) BentoVividCityText.Text = displayCity;
        if (BentoVividTempText != null) BentoVividTempText.Text = tempOnly;
        if (BentoVividConditionText != null) BentoVividConditionText.Text = _currentWeather.ConditionText;
        if (svgLoaded && BentoVividWeatherIcon != null)
        {
            BentoVividWeatherIcon.Source = WidgetWeatherIcon.Source;
            BentoVividWeatherIcon.Visibility = Visibility.Visible;
            if (BentoVividFallbackIcon != null) BentoVividFallbackIcon.Visibility = Visibility.Collapsed;
        }
        else if (BentoVividFallbackIcon != null)
        {
            if (BentoVividWeatherIcon != null) BentoVividWeatherIcon.Visibility = Visibility.Collapsed;
            BentoVividFallbackIcon.Glyph = WidgetFallbackIcon.Glyph;
            BentoVividFallbackIcon.Visibility = Visibility.Visible;
        }

        // Bento Tactical
        if (BentoTacticalCityText != null) BentoTacticalCityText.Text = displayCity;
        if (BentoTacticalTempText != null) BentoTacticalTempText.Text = tempOnly;
        if (BentoTacticalConditionText != null) BentoTacticalConditionText.Text = _currentWeather.ConditionText;
        if (svgLoaded && BentoTacticalWeatherIcon != null)
        {
            BentoTacticalWeatherIcon.Source = WidgetWeatherIcon.Source;
            BentoTacticalWeatherIcon.Visibility = Visibility.Visible;
            if (BentoTacticalFallbackIcon != null) BentoTacticalFallbackIcon.Visibility = Visibility.Collapsed;
        }
        else if (BentoTacticalFallbackIcon != null)
        {
            if (BentoTacticalWeatherIcon != null) BentoTacticalWeatherIcon.Visibility = Visibility.Collapsed;
            BentoTacticalFallbackIcon.Glyph = WidgetFallbackIcon.Glyph;
            BentoTacticalFallbackIcon.Visibility = Visibility.Visible;
        }
        if (BentoTacticalCityCodeText != null)
        {
            BentoTacticalCityCodeText.Text = GetCityCode(displayCity);
        }

        // Barometer
        double pressHpa = 1013.25;
        if (!string.IsNullOrEmpty(_currentWeather.PressureText))
        {
            var pMatch = System.Text.RegularExpressions.Regex.Match(_currentWeather.PressureText, @"\d+(\.\d+)?");
            if (pMatch.Success && double.TryParse(pMatch.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedP))
            {
                pressHpa = parsedP;
            }
        }
        double baroVal = pressHpa / 100.0;
        if (BentoTacticalBaroText != null) BentoTacticalBaroText.Text = $"{baroVal:00.000}";
        if (BentoTacticalBaroUnitText != null) BentoTacticalBaroUnitText.Text = $"{pressHpa:0} hPa";

        // Sunset
        string sunsetTime = string.IsNullOrEmpty(_currentWeather.SunsetText) || _currentWeather.SunsetText == "--:--" ? "18:25" : _currentWeather.SunsetText;
        // Handled via _currentWeather.SunsetText
        {
        // Handled via _currentWeather.SunsetText
        }
        if (BentoVividSunsetText != null) BentoVividSunsetText.Text = sunsetTime;
        if (BentoTacticalSunsetText != null) BentoTacticalSunsetText.Text = sunsetTime;

        // Retro LCD
        int tVal = 22;
        var match = System.Text.RegularExpressions.Regex.Match(tempOnly, @"-?\d+");
        if (match.Success && int.TryParse(match.Value, out int parsedT))
        {
            tVal = Math.Clamp(Math.Abs(parsedT), 0, 99);
        }
        SetSevenSegmentDigit(tVal / 10, RetroDigit1A, RetroDigit1B, RetroDigit1C, RetroDigit1D, RetroDigit1E, RetroDigit1F, RetroDigit1G);
        SetSevenSegmentDigit(tVal % 10, RetroDigit2A, RetroDigit2B, RetroDigit2C, RetroDigit2D, RetroDigit2E, RetroDigit2F, RetroDigit2G);

        // Nothing Bento & Nothing Square (v3.5.0)
        string upperCity = displayCity.ToUpperInvariant();
        string cityCode = GetCityCode(displayCity);
        string nothingCity = $"{upperCity} [{cityCode}]";
        int aqiVal = _currentWeather.HasAqi ? _currentWeather.AqiValue : 42;
        int humVal = (int)Math.Clamp(_currentWeather.HumidityValue > 0 ? _currentWeather.HumidityValue : 75, 0, 100);
        string rangeStr = !string.IsNullOrEmpty(_currentWeather.HighLowTempText) ? _currentWeather.HighLowTempText : $"{tempOnly}\u00B0";

        if (NothingBentoCityText != null) NothingBentoCityText.Text = nothingCity;
        if (NothingBentoTempText != null) NothingBentoTempText.Text = tempOnly;
        if (NothingBentoWeatherIcon != null && !string.IsNullOrEmpty(_currentWeather.NothingGlyph))
            NothingBentoWeatherIcon.Glyph = _currentWeather.NothingGlyph;
        if (NothingBentoConditionText != null) NothingBentoConditionText.Text = _currentWeather.ConditionText.ToUpperInvariant();
        if (NothingBentoRangeText != null) NothingBentoRangeText.Text = rangeStr;
        if (NothingBentoAqiText != null) NothingBentoAqiText.Text = $"AQI {aqiVal}";
        if (NothingBentoSunsetText != null) NothingBentoSunsetText.Text = sunsetTime;
        if (NothingBentoHumidityText != null) NothingBentoHumidityText.Text = $"{humVal}%";

        if (NothingGlyph1 != null) NothingGlyph1.Opacity = humVal >= 20 ? 1.0 : 0.2;
        if (NothingGlyph2 != null) NothingGlyph2.Opacity = humVal >= 40 ? 1.0 : 0.2;
        if (NothingGlyph3 != null) NothingGlyph3.Opacity = humVal >= 60 ? 1.0 : 0.2;
        if (NothingGlyph4 != null) NothingGlyph4.Opacity = humVal >= 80 ? 1.0 : 0.2;
        if (NothingGlyph5 != null) NothingGlyph5.Opacity = humVal >= 90 ? 1.0 : 0.2;

        if (NothingSquareCityText != null) NothingSquareCityText.Text = nothingCity;
        if (NothingSquareWeatherIcon != null && !string.IsNullOrEmpty(_currentWeather.NothingGlyph))
            NothingSquareWeatherIcon.Glyph = _currentWeather.NothingGlyph;
        if (NothingSquareTempText != null) NothingSquareTempText.Text = $"{tempOnly}\u00B0";
        if (NothingSquareConditionText != null) NothingSquareConditionText.Text = _currentWeather.ConditionText.ToUpperInvariant();
        if (NothingSquareAqiText != null) NothingSquareAqiText.Text = $"{aqiVal}";
        if (NothingSquareHumText != null) NothingSquareHumText.Text = $"{humVal}%";
        if (NothingSquareWindText != null) NothingSquareWindText.Text = $"{_currentWeather.WindSpeedValue:F0}k";

        if (NothingCircularCityText != null) NothingCircularCityText.Text = nothingCity;
        if (NothingCircularWeatherIcon != null && !string.IsNullOrEmpty(_currentWeather.NothingGlyph))
            NothingCircularWeatherIcon.Glyph = _currentWeather.NothingGlyph;
        if (NothingCircularTempText != null) NothingCircularTempText.Text = tempOnly;
        if (NothingCircularConditionText != null) NothingCircularConditionText.Text = _currentWeather.ConditionText.ToUpperInvariant();
        if (NothingCircularAqiText != null) NothingCircularAqiText.Text = $"AQI {aqiVal}";
        if (NothingCircularWindText != null) NothingCircularWindText.Text = $"{_currentWeather.WindSpeedValue:F0} km/h";

        if (NothingPillCityText != null) NothingPillCityText.Text = nothingCity;
        if (NothingPillWeatherIcon != null && !string.IsNullOrEmpty(_currentWeather.NothingGlyph))
            NothingPillWeatherIcon.Glyph = _currentWeather.NothingGlyph;
        if (NothingPillTempText != null) NothingPillTempText.Text = tempOnly;
        if (NothingPillAqiText != null) NothingPillAqiText.Text = $"AQI {aqiVal}";

        // Style 13: Dynamic Island (v3.6.0)
        if (DynamicIslandCityText != null) DynamicIslandCityText.Text = nothingCity;
        if (DynamicIslandWeatherIcon != null && !string.IsNullOrEmpty(_currentWeather.NothingGlyph))
            DynamicIslandWeatherIcon.Glyph = _currentWeather.NothingGlyph;
        if (DynamicIslandTempText != null) DynamicIslandTempText.Text = tempOnly.TrimEnd('°');
        if (DynamicIslandAqiText != null) DynamicIslandAqiText.Text = $"AQI {aqiVal}";
        if (DynamicIslandConditionText != null) DynamicIslandConditionText.Text = _currentWeather.ConditionText.ToUpperInvariant();
        if (DynamicIslandTelemetryText != null) DynamicIslandTelemetryText.Text = $"HUM {humVal}% • WIND {_currentWeather.WindSpeedValue:F0} KM/H";
        var dynTimes = new[] { DynHTime0, DynHTime1, DynHTime2, DynHTime3 };
        var dynTemps = new[] { DynHTemp0, DynHTemp1, DynHTemp2, DynHTemp3 };
        for (int i = 0; i < 4; i++)
        {
            if (_hourlyForecast != null && i < _hourlyForecast.Count)
            {
                if (dynTimes[i] != null) dynTimes[i].Text = i == 0 ? "NOW" : _hourlyForecast[i].TimeDisplay;
                if (dynTemps[i] != null) dynTemps[i].Text = _hourlyForecast[i].TempDisplay;
            }
        }

        // Style 14: Teenage Engineering Field Deck (v3.6.0)
        if (TeenageDeckCityText != null) TeenageDeckCityText.Text = $"{cityCode} // {Latitude:0.0}N";
        if (TeenageDeckWeatherIcon != null && !string.IsNullOrEmpty(_currentWeather.NothingGlyph))
            TeenageDeckWeatherIcon.Glyph = _currentWeather.NothingGlyph;
        if (TeenageDeckTempText != null) TeenageDeckTempText.Text = _currentWeather.TemperatureText;
        if (TeenageDeckConditionText != null) TeenageDeckConditionText.Text = $"{_currentWeather.ConditionText.ToUpperInvariant()} // NOMINAL";
        if (TeenageDeckAqiText != null) TeenageDeckAqiText.Text = $"{aqiVal}";
        if (TeenageDeckWindText != null) TeenageDeckWindText.Text = $"{_currentWeather.WindSpeedValue:F0} KM/H";
        if (TeenageDeckHumText != null) TeenageDeckHumText.Text = $"HUM {humVal}%";
        if (TeenageDeckPressText != null) TeenageDeckPressText.Text = $"{pressHpa:0} HPA";

        // Style 15: Pure Typography Wallpaper HUD (v3.6.0)
        if (PureTypoCityText != null) PureTypoCityText.Text = nothingCity;
        if (PureTypoTempText != null) PureTypoTempText.Text = tempOnly;
        if (PureTypoWeatherIcon != null && !string.IsNullOrEmpty(_currentWeather.NothingGlyph))
            PureTypoWeatherIcon.Glyph = _currentWeather.NothingGlyph;
        if (PureTypoConditionText != null) PureTypoConditionText.Text = _currentWeather.ConditionText.ToUpperInvariant();
        if (PureTypoTelemetryText != null) PureTypoTelemetryText.Text = $"AQI {aqiVal} // HUM {humVal}% // WIND {_currentWeather.WindSpeedValue:F0} KM/H";

        // Style 16: Ultra-Slim Taskbar Strip (v3.7.0)
        if (TaskbarStripCityText != null) TaskbarStripCityText.Text = displayCity;
        if (TaskbarStripTempText != null) TaskbarStripTempText.Text = _currentWeather.TemperatureText;
        if (TaskbarStripWeatherIcon != null && !string.IsNullOrEmpty(_currentWeather.IconGlyph))
            TaskbarStripWeatherIcon.Glyph = _currentWeather.IconGlyph;
        if (TaskbarStripSubText != null) TaskbarStripSubText.Text = $"AQI {aqiVal}";

        // Style 17: Spatial Aurora Glass Cube + Live 12h Spline Curve (v3.7.0)
        if (SpatialCubeCityText != null) SpatialCubeCityText.Text = upperCity;
        if (SpatialCubeAqiText != null) SpatialCubeAqiText.Text = $"AQI {aqiVal}";
        if (SpatialCubeTempText != null) SpatialCubeTempText.Text = _currentWeather.TemperatureText;
        if (SpatialCubeConditionText != null) SpatialCubeConditionText.Text = _currentWeather.ConditionText;
        if (SpatialCubeRangeText != null) SpatialCubeRangeText.Text = $"{rangeStr} • Ẩm {humVal}%";
        RenderWidgetSpatialCurve();

        // Style 18: Mini OOTD Capsule (v3.7.1)
        if (OotdCapsuleCityText != null) OotdCapsuleCityText.Text = upperCity;
        if (OotdCapsuleTempText != null) OotdCapsuleTempText.Text = _currentWeather.TemperatureText;
        if (OotdCapsuleWeatherIcon != null)
        {
            OotdCapsuleWeatherIcon.Glyph = string.IsNullOrEmpty(_currentWeather.IconGlyph) ? "\uf185" : _currentWeather.IconGlyph;
        }

        string commuteMethod = _settings.PreferredCommuteMethod ?? "Motorbike";
        if (OotdCapsuleCommuteText != null)
        {
            OotdCapsuleCommuteText.Text = commuteMethod switch
            {
                "Car" => "🚗 Ô tô",
                "Bus" => "🚌 Xe buýt",
                "Walk" => "🚶 Đi bộ",
                _ => "🏍️ Xe máy"
            };
        }

        var advice = _viewModel?.CurrentOutfitAdvice ?? new OutfitAdvisorService().GenerateOutfitAdvice(
            _currentWeather,
            null,
            "Work",
            "All",
            commuteMethod,
            _settings.PreferredThermalSensitivity ?? "Normal");

        if (advice != null)
        {
            if (OotdCapsuleTagText != null) OotdCapsuleTagText.Text = string.IsNullOrEmpty(advice.FashionStyleTag) ? "Smart Casual" : advice.FashionStyleTag;
            if (OotdCapsuleAdviceText != null)
            {
                string rec = !string.IsNullOrEmpty(advice.TopClothing) && !string.IsNullOrEmpty(advice.BottomClothing)
                    ? $"{advice.TopClothing} • {advice.BottomClothing}"
                    : (string.IsNullOrEmpty(advice.QuickSummary) ? advice.Headline : advice.QuickSummary);
                OotdCapsuleAdviceText.Text = string.IsNullOrEmpty(rec) ? "Trang phục thoải mái, thoáng mát" : rec;
            }

            if (OotdCapsuleAccessoryText != null && OotdCapsuleAccessoryIcon != null)
            {
                if (advice.Accessories != null && advice.Accessories.Count > 0)
                {
                    var acc = advice.Accessories.FirstOrDefault(a => a.IsEssential) ?? advice.Accessories[0];
                    OotdCapsuleAccessoryText.Text = acc.Name;
                    OotdCapsuleAccessoryIcon.Glyph = string.IsNullOrEmpty(acc.IconGlyph) ? "\uf0e9" : acc.IconGlyph;
                }
                else
                {
                    OotdCapsuleAccessoryText.Text = "Mang phụ kiện cơ bản";
                    OotdCapsuleAccessoryIcon.Glyph = "\uf0e9";
                }
            }

            if (OotdCapsuleSubNoticeText != null)
            {
                OotdCapsuleSubNoticeText.Text = !string.IsNullOrEmpty(advice.ThermalComfortNotice)
                    ? advice.ThermalComfortNotice
                    : $"Thoáng khí {advice.BreathabilityScore}% • {advice.UvDefenseRating}";
            }
        }

        double windMs = 8.0;
        if (_currentWeather.WindSpeedValue > 0) windMs = _currentWeather.WindSpeedValue / 3.6;
        if (RetroWindText != null) RetroWindText.Text = $"{windMs:0} M/S";

        if (RetroWindNeedleTransform != null)
        {
            double needleAngle = Math.Clamp(-90 + (windMs / 25.0) * 180.0, -90, 90);
            RetroWindNeedleTransform.Angle = needleAngle;
        }
        if (RetroPressureVal != null) RetroPressureVal.Text = $"{pressHpa:0} HPA";
        if (RetroUfIndexVal != null) RetroUfIndexVal.Text = $"UV {_currentWeather.UvIndexText}";

        if (_currentStyle == "BryanCDynamic")
        {
            ApplyBryanCWeatherTheme(_currentWeather.WeatherEffect, _currentWeather.IsDay);
        }

        // Cập nhật dự báo giờ
        UpdateHourlyForecastRow();
    }

    private void RenderWidgetSpatialCurve()
    {
        if (WidgetSpatialCurveCanvas == null) return;
        try
        {
            WidgetSpatialCurveCanvas.Children.Clear();
            var slice = (_hourlyForecast != null && _hourlyForecast.Count >= 4)
                ? _hourlyForecast.Take(10).ToList()
                : new List<HourlyForecastItem>();
            if (slice.Count < 2) return;

            double w = 222.0;
            double h = 56.0;
            var temps = slice.Select(x => x.TempValue).ToList();
            double minT = temps.Min();
            double maxT = temps.Max();
            double range = Math.Max(2.0, maxT - minT);

            var pts = new List<Windows.Foundation.Point>();
            for (int i = 0; i < slice.Count; i++)
            {
                double x = 8.0 + (i / (double)(slice.Count - 1)) * (w - 16.0);
                double norm = (temps[i] - minT) / range;
                double y = 44.0 - norm * 28.0;
                pts.Add(new Windows.Foundation.Point(x, y));
            }

            // Fill path (separate BezierSegment & LineSegment instances per WinUI 3 DependencyObject rule)
            var fillGeo = new Microsoft.UI.Xaml.Media.PathGeometry();
            var fillFig = new Microsoft.UI.Xaml.Media.PathFigure { StartPoint = pts[0], IsClosed = true };
            var strokeGeo = new Microsoft.UI.Xaml.Media.PathGeometry();
            var strokeFig = new Microsoft.UI.Xaml.Media.PathFigure { StartPoint = pts[0], IsClosed = false };

            for (int i = 0; i < pts.Count - 1; i++)
            {
                var p0 = pts[i];
                var p1 = pts[i + 1];
                double midX = (p0.X + p1.X) / 2.0;
                fillFig.Segments.Add(new Microsoft.UI.Xaml.Media.BezierSegment
                {
                    Point1 = new Windows.Foundation.Point(midX, p0.Y),
                    Point2 = new Windows.Foundation.Point(midX, p1.Y),
                    Point3 = p1
                });
                strokeFig.Segments.Add(new Microsoft.UI.Xaml.Media.BezierSegment
                {
                    Point1 = new Windows.Foundation.Point(midX, p0.Y),
                    Point2 = new Windows.Foundation.Point(midX, p1.Y),
                    Point3 = p1
                });
            }

            fillFig.Segments.Add(new Microsoft.UI.Xaml.Media.LineSegment { Point = new Windows.Foundation.Point(pts[^1].X, h) });
            fillFig.Segments.Add(new Microsoft.UI.Xaml.Media.LineSegment { Point = new Windows.Foundation.Point(pts[0].X, h) });
            fillGeo.Figures.Add(fillFig);
            strokeGeo.Figures.Add(strokeFig);

            var fillBrush = new Microsoft.UI.Xaml.Media.LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(0, 1)
            };
            fillBrush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = Windows.UI.Color.FromArgb(95, 56, 189, 248), Offset = 0.0 });
            fillBrush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = Windows.UI.Color.FromArgb(5, 168, 85, 247), Offset = 1.0 });

            WidgetSpatialCurveCanvas.Children.Add(new Microsoft.UI.Xaml.Shapes.Path
            {
                Data = fillGeo,
                Fill = fillBrush
            });

            var strokeBrush = new Microsoft.UI.Xaml.Media.LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(1, 0)
            };
            strokeBrush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = Windows.UI.Color.FromArgb(255, 56, 189, 248), Offset = 0.0 });
            strokeBrush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = Windows.UI.Color.FromArgb(255, 192, 132, 252), Offset = 1.0 });

            WidgetSpatialCurveCanvas.Children.Add(new Microsoft.UI.Xaml.Shapes.Path
            {
                Data = strokeGeo,
                Stroke = strokeBrush,
                StrokeThickness = 2.2
            });

            for (int i = 0; i < pts.Count; i += 3)
            {
                var dot = new Microsoft.UI.Xaml.Shapes.Ellipse
                {
                    Width = i == 0 ? 7 : 5,
                    Height = i == 0 ? 7 : 5,
                    Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
                    Stroke = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 56, 189, 248)),
                    StrokeThickness = 1.5
                };
                double r = (i == 0 ? 7 : 5) / 2.0;
                Canvas.SetLeft(dot, pts[i].X - r);
                Canvas.SetTop(dot, pts[i].Y - r);
                WidgetSpatialCurveCanvas.Children.Add(dot);
            }

            if (SpatialHTime0 != null && slice.Count > 0) SpatialHTime0.Text = "NOW";
            if (SpatialHTime1 != null && slice.Count > 3) SpatialHTime1.Text = slice[3].TimeDisplay;
            if (SpatialHTime2 != null && slice.Count > 6) SpatialHTime2.Text = slice[6].TimeDisplay;
            if (SpatialHTime3 != null && slice.Count > 9) SpatialHTime3.Text = slice[9].TimeDisplay;
        }
        catch { }
    }

    private void UpdateBryanCForecastDock()
    {
        if (_dailyForecast == null || _dailyForecast.Count == 0) return;

        var dayBlocks = new[] { BryanCDay0, BryanCDay1, BryanCDay2, BryanCDay3 };
        var iconImages = new[] { BryanCIcon0, BryanCIcon1, BryanCIcon2, BryanCIcon3 };
        var fallbackIcons = new[] { BryanCFallback0, BryanCFallback1, BryanCFallback2, BryanCFallback3 };

        // Lấy 4 ngày kế tiếp (từ index 1 trở đi nếu có >= 5 ngày, hoặc từ index 0)
        int startIndex = _dailyForecast.Count >= 5 ? 1 : 0;

        for (int i = 0; i < 4; i++)
        {
            int dataIndex = startIndex + i;
            if (dataIndex < _dailyForecast.Count && i < dayBlocks.Length)
            {
                var item = _dailyForecast[dataIndex];
                if (dayBlocks[i] != null)
                {
                    dayBlocks[i].Text = item.DayName.Length > 3 ? item.DayName.Substring(0, 3).ToUpperInvariant() : item.DayName.ToUpperInvariant();
                }

                bool loaded = false;
                try
                {
                    if (!string.IsNullOrEmpty(item.SvgIconPath) && iconImages[i] != null)
                    {
                        var svgSource = new SvgImageSource(new Uri(item.SvgIconPath))
                        {
                            RasterizePixelWidth = 36,
                            RasterizePixelHeight = 36
                        };
                        int capturedIndex = i;
                        svgSource.OpenFailed += (s, e) =>
                        {
                            if (iconImages[capturedIndex] != null) iconImages[capturedIndex].Visibility = Visibility.Collapsed;
                            if (fallbackIcons[capturedIndex] != null) fallbackIcons[capturedIndex].Visibility = Visibility.Visible;
                        };
                        iconImages[i].Source = svgSource;
                        iconImages[i].Visibility = Visibility.Visible;
                        if (fallbackIcons[i] != null) fallbackIcons[i].Visibility = Visibility.Collapsed;
                        loaded = true;
                    }
                }
                catch { }

                if (!loaded)
                {
                    if (iconImages[i] != null) iconImages[i].Visibility = Visibility.Collapsed;
                    if (fallbackIcons[i] != null)
                    {
                        fallbackIcons[i].Glyph = string.IsNullOrEmpty(item.IconGlyph) ? "\uf185" : item.IconGlyph;
                        fallbackIcons[i].Visibility = Visibility.Visible;
                    }
                }
            }
        }
    }

    private void ApplyBryanCWeatherTheme(WeatherEffectType effect, bool isDay)
    {
        if (BryanCSunArcs == null) return;

        // Ẩn tất cả các layer đồ họa trước
        BryanCSunArcs.Visibility = Visibility.Collapsed;
        BryanCMoonArc.Visibility = Visibility.Collapsed;
        BryanCRainStreaks.Visibility = Visibility.Collapsed;
        BryanCCloudWaves.Visibility = Visibility.Collapsed;
        BryanCSnowDots.Visibility = Visibility.Collapsed;
        BryanCMistBands.Visibility = Visibility.Collapsed;

        // Chọn bộ màu Gradient và hiển thị layer đồ họa tương ứng
        (Windows.UI.Color c1, Windows.UI.Color c2) palette;

        switch (effect)
        {
            case WeatherEffectType.ClearSunny:
            case WeatherEffectType.HighUvSunny:
                if (isDay)
                {
                    // Sunny Warm Amber/Orange (BryanC Dribbble Card 1)
                    palette = (ParseHexColor("#FF5757"), ParseHexColor("#FF9233"));
                    BryanCSunArcs.Visibility = Visibility.Visible;
                }
                else
                {
                    // Sunny Royal Navy Blue (BryanC Dribbble Card 2)
                    palette = (ParseHexColor("#1B3B6F"), ParseHexColor("#21295C"));
                    BryanCMoonArc.Visibility = Visibility.Visible;
                }
                break;

            case WeatherEffectType.LightRain:
            case WeatherEffectType.ModerateRain:
                // Rainy Purple (BryanC Dribbble Card 3)
                palette = (ParseHexColor("#5B3A70"), ParseHexColor("#432B58"));
                BryanCRainStreaks.Visibility = Visibility.Visible;
                break;

            case WeatherEffectType.HeavyRain:
                // Heavy Rain Charcoal/Slate (BryanC Dribbble Card 4)
                palette = (ParseHexColor("#263238"), ParseHexColor("#1B2327"));
                BryanCRainStreaks.Visibility = Visibility.Visible;
                break;

            case WeatherEffectType.Cloudy:
                // Cloudy Sky Blue (BryanC Dribbble Card 5)
                palette = (ParseHexColor("#1E88E5"), ParseHexColor("#42A5F5"));
                BryanCCloudWaves.Visibility = Visibility.Visible;
                break;

            case WeatherEffectType.PartlyCloudy:
                // Mostly Cloudy Ocean Blue (BryanC Dribbble Card 6)
                palette = (ParseHexColor("#1565C0"), ParseHexColor("#0D47A1"));
                BryanCCloudWaves.Visibility = Visibility.Visible;
                break;

            case WeatherEffectType.Snow:
                // Snow Ice Cyan (BryanC Dribbble Card 7)
                palette = (ParseHexColor("#0288D1"), ParseHexColor("#26C6DA"));
                BryanCSnowDots.Visibility = Visibility.Visible;
                break;

            case WeatherEffectType.Fog:
                // Hazy / Fog Amber (BryanC Dribbble Card 9)
                palette = (ParseHexColor("#E67E22"), ParseHexColor("#F39C12"));
                BryanCMistBands.Visibility = Visibility.Visible;
                break;

            case WeatherEffectType.Thunderstorm:
                // Thunderstorm / Hail Midnight Slate (BryanC Dribbble Card 10)
                palette = (ParseHexColor("#181824"), ParseHexColor("#2B2544"));
                BryanCRainStreaks.Visibility = Visibility.Visible;
                break;

            case WeatherEffectType.ClearNight:
                palette = (ParseHexColor("#1B3B6F"), ParseHexColor("#21295C"));
                BryanCMoonArc.Visibility = Visibility.Visible;
                break;

            default:
                palette = isDay ? (ParseHexColor("#1E88E5"), ParseHexColor("#42A5F5")) : (ParseHexColor("#1B3B6F"), ParseHexColor("#21295C"));
                BryanCCloudWaves.Visibility = Visibility.Visible;
                break;
        }

        var gradient = new Microsoft.UI.Xaml.Media.LinearGradientBrush { StartPoint = new Windows.Foundation.Point(0, 0), EndPoint = new Windows.Foundation.Point(1, 1) };
        gradient.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = palette.c1, Offset = 0 });
        gradient.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = palette.c2, Offset = 1 });
        WidgetRoot.Background = gradient;
    }

    private void UpdateHourlyForecastRow()
    {
        var items = _hourlyForecast;
        if (items == null || items.Count == 0) return;

        var timeBlocks = new[] { HTime0, HTime1, HTime2, HTime3, HTime4 };
        var iconImages = new[] { HIcon0, HIcon1, HIcon2, HIcon3, HIcon4 };
        var fallbackIcons = new[] { HFallback0, HFallback1, HFallback2, HFallback3, HFallback4 };
        var tempBlocks = new[] { HTemp0, HTemp1, HTemp2, HTemp3, HTemp4 };

        for (int i = 0; i < 5; i++)
        {
            if (i < items.Count)
            {
                var item = items[i];
                timeBlocks[i].Text = item.TimeDisplay;
                tempBlocks[i].Text = item.TempDisplay;

                bool loaded = false;
                try
                {
                    string fileName = Path.GetFileName(item.SvgIconPath);
                    string localPath = Path.Combine(AppContext.BaseDirectory, "Assets", "weather-icons-main", "static", fileName);

                    if (File.Exists(localPath))
                    {
                        var svgSource = new SvgImageSource(new Uri(localPath))
                        {
                            RasterizePixelWidth = 44,
                            RasterizePixelHeight = 44
                        };
                        svgSource.OpenFailed += (s, e) =>
                        {
                            iconImages[i].Visibility = Visibility.Collapsed;
                            fallbackIcons[i].Visibility = Visibility.Visible;
                        };
                        iconImages[i].Source = svgSource;
                        iconImages[i].Visibility = Visibility.Visible;
                        fallbackIcons[i].Visibility = Visibility.Collapsed;
                        loaded = true;
                    }
                }
                catch { }

                if (!loaded)
                {
                    iconImages[i].Visibility = Visibility.Collapsed;
                    fallbackIcons[i].Glyph = string.IsNullOrEmpty(item.IconGlyph) ? "\uf185" : item.IconGlyph;
                    fallbackIcons[i].Visibility = Visibility.Visible;
                }
            }
        }
    }

    public void SetAlwaysOnTop(bool isAlwaysOnTop)
    {
        _isAlwaysOnTop = isAlwaysOnTop;
        if (_appWindow?.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = _isAlwaysOnTop;
        }
        UpdatePinButtonVisual();
    }

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        _isAlwaysOnTop = !_isAlwaysOnTop;
        if (_appWindow?.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = _isAlwaysOnTop;
        }
        _settings.WidgetAlwaysOnTop = _isAlwaysOnTop;
        _settingsService.SaveSettings(_settings);
        UpdatePinButtonVisual();
    }

    private void UpdatePinButtonVisual()
    {
        var loc = LocalizationService.Instance;
        if (_isAlwaysOnTop)
        {
            PinButton.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0x35, 0x00, 0x78, 0xD4));
            PinIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0x38, 0xBD, 0xF8));
            CompactPinIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0x38, 0xBD, 0xF8));
            if (BryanCPinIcon != null) BryanCPinIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0x38, 0xBD, 0xF8));
            ToolTipService.SetToolTip(PinButton, loc.WidgetWindowPinTooltipActive);
        }
        else
        {
            PinButton.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
            PinIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0x94, 0xA3, 0xB8));
            CompactPinIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0x94, 0xA3, 0xB8));
            if (BryanCPinIcon != null) BryanCPinIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White);
            ToolTipService.SetToolTip(PinButton, loc.WidgetWindowPinTooltipInactive);
        }
    }

    private async void WidgetRefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await ReloadWeatherAsync();
    }

    private void AddWidgetButton_Click(object sender, RoutedEventArgs e)
    {
        ShowCityPickerFlyout(sender as FrameworkElement ?? WidgetRoot);
    }

    private void CityPickerButton_Click(object sender, RoutedEventArgs e)
    {
        ShowCityPickerFlyout(sender as FrameworkElement ?? WidgetRoot);
    }

    private void SeasonalThemePickerButton_Click(object sender, RoutedEventArgs e)
    {
        ShowSeasonalThemePickerFlyout(sender as FrameworkElement ?? WidgetRoot);
    }

    private void ShowSeasonalThemePickerFlyout(FrameworkElement target)
    {
        var flyout = new Flyout();
        var panel = new StackPanel { Width = 230, Spacing = 5, Padding = new Thickness(4) };

        panel.Children.Add(new TextBlock
        {
            Text = "🎆 Chọn Chủ Đề Mùa & Lễ Hội",
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            FontSize = 13,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
            Margin = new Thickness(0, 0, 0, 4)
        });

        var themes = new (string tag, string label)[]
        {
            ("Tet", "🧧 Tết Cổ Truyền"),
            ("MidAutumn", "🥮 Tết Trung Thu"),
            ("Christmas", "🎄 Giáng Sinh (Noel)"),
            ("Halloween", "🎃 Lễ Hội Halloween"),
            ("Spring", "🌸 Mùa Xuân"),
            ("Summer", "☀️ Mùa Hè"),
            ("Autumn", "🍂 Mùa Thu"),
            ("Winter", "❄️ Mùa Đông")
        };

        foreach (var (tag, label) in themes)
        {
            var btn = new Button
            {
                Content = label,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                FontSize = 11.5,
                Padding = new Thickness(8, 5, 8, 5),
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF)),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(6)
            };
            btn.Click += (s, e) =>
            {
                var skin = SeasonalSkinService.Instance.GetActiveSkin(tag);
                ApplySeasonalTheme(skin);
                flyout.Hide();
            };
            panel.Children.Add(btn);
        }

        flyout.Content = panel;
        flyout.ShowAt(target);
    }

    #region City Picker & Multi-Widget Spawning

    private void ShowCityPickerFlyout(FrameworkElement target)
    {
        var loc = LocalizationService.Instance;
        var flyout = new Flyout();
        var rootPanel = new StackPanel { Width = 260, Spacing = 8, Padding = new Thickness(4) };

        rootPanel.Children.Add(new TextBlock 
        { 
            Text = loc.WidgetWindowCityPickerTitle, 
            FontWeight = Microsoft.UI.Text.FontWeights.Bold, 
            FontSize = 13, 
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White) 
        });

        rootPanel.Children.Add(new TextBlock 
        { 
            Text = $"{loc.WidgetWindowCityPickerCurrent} {LocationName}", 
            FontSize = 11, 
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0x38, 0xBD, 0xF8)) 
        });

        var searchBox = new AutoSuggestBox
        {
            PlaceholderText = loc.WidgetWindowCityPickerSearchPlaceholder,
            QueryIcon = new SymbolIcon(Symbol.Find)
        };

        GeocodingItem? selectedItem = null;

        searchBox.TextChanged += async (s, args) =>
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput && s.Text.Trim().Length >= 2)
            {
                var results = await _locationService.SearchLocationsAsync(s.Text);
                s.ItemsSource = results;
            }
        };

        searchBox.SuggestionChosen += (s, args) =>
        {
            if (args.SelectedItem is GeocodingItem item)
            {
                selectedItem = item;
                s.Text = $"{item.Name}, {item.Country}";
            }
        };

        rootPanel.Children.Add(searchBox);

        rootPanel.Children.Add(new TextBlock 
        { 
            Text = loc.WidgetWindowCityPickerQuick, 
            FontSize = 10.5, 
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0x94, 0xA3, 0xB8)) 
        });

        // Quick Pick Buttons
        var quickGrid = new Grid { ColumnSpacing = 4, RowSpacing = 4 };
        quickGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        quickGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        quickGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        quickGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var btnHanoi = new Button { Content = "Hà Nội", HorizontalAlignment = HorizontalAlignment.Stretch, FontSize = 11, Padding = new Thickness(4) };
        btnHanoi.Click += (s, e) => ApplySelectedCity("Hà Nội, Việt Nam", 21.0285, 105.8542, flyout);
        Grid.SetRow(btnHanoi, 0); Grid.SetColumn(btnHanoi, 0);
        quickGrid.Children.Add(btnHanoi);

        var btnHcm = new Button { Content = "TP. Hồ Chí Minh", HorizontalAlignment = HorizontalAlignment.Stretch, FontSize = 11, Padding = new Thickness(4) };
        btnHcm.Click += (s, e) => ApplySelectedCity("TP. Hồ Chí Minh, Việt Nam", 10.823, 106.6296, flyout);
        Grid.SetRow(btnHcm, 0); Grid.SetColumn(btnHcm, 1);
        quickGrid.Children.Add(btnHcm);

        var btnDanang = new Button { Content = "Đà Nẵng", HorizontalAlignment = HorizontalAlignment.Stretch, FontSize = 11, Padding = new Thickness(4) };
        btnDanang.Click += (s, e) => ApplySelectedCity("Đà Nẵng, Việt Nam", 16.0544, 108.2022, flyout);
        Grid.SetRow(btnDanang, 1); Grid.SetColumn(btnDanang, 0);
        quickGrid.Children.Add(btnDanang);

        var btnDalat = new Button { Content = "Đà Lạt", HorizontalAlignment = HorizontalAlignment.Stretch, FontSize = 11, Padding = new Thickness(4) };
        btnDalat.Click += (s, e) => ApplySelectedCity("Đà Lạt, Việt Nam", 11.9404, 108.4583, flyout);
        Grid.SetRow(btnDalat, 1); Grid.SetColumn(btnDalat, 1);
        quickGrid.Children.Add(btnDalat);

        rootPanel.Children.Add(quickGrid);

        // Nút hành động
        var actionPanel = new Grid { ColumnSpacing = 6, Margin = new Thickness(0, 6, 0, 0) };
        actionPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        actionPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var applyBtn = new Button 
        { 
            Content = "Đổi vị trí này", 
            HorizontalAlignment = HorizontalAlignment.Stretch, 
            FontSize = 11, 
            Style = Application.Current.Resources["AccentButtonStyle"] as Style 
        };
        applyBtn.Click += (s, e) =>
        {
            if (selectedItem != null)
            {
                ApplySelectedCity($"{selectedItem.Name}, {selectedItem.Country}", selectedItem.Latitude, selectedItem.Longitude, flyout);
            }
            else
            {
                flyout.Hide();
            }
        };
        Grid.SetColumn(applyBtn, 0);
        actionPanel.Children.Add(applyBtn);

        var spawnBtn = new Button 
        { 
            Content = "➕ Mở thêm", 
            HorizontalAlignment = HorizontalAlignment.Stretch, 
            FontSize = 11 
        };
        spawnBtn.Click += (s, e) =>
        {
            if (selectedItem != null)
            {
                var newWidget = new WidgetWindow($"{selectedItem.Name}, {selectedItem.Country}", selectedItem.Latitude, selectedItem.Longitude, _settings);
                newWidget.Activate();
                flyout.Hide();
            }
            else
            {
                var newWidget = new WidgetWindow(LocationName, Latitude, Longitude, _settings);
                newWidget.Activate();
                flyout.Hide();
            }
        };
        Grid.SetColumn(spawnBtn, 1);
        actionPanel.Children.Add(spawnBtn);

        rootPanel.Children.Add(actionPanel);

        flyout.Content = rootPanel;
        flyout.ShowAt(target);
    }

    private void ApplySelectedCity(string name, double lat, double lon, Flyout flyout)
    {
        LocationName = name;
        Latitude = lat;
        Longitude = lon;
        flyout.Hide();
        _ = ReloadWeatherAsync();
    }

    #endregion

    // ==================== XỬ LÝ KÉO THẢ DRAG-AND-DROP ====================

    private void WidgetRoot_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_settings.WidgetLockPosition) return;

        var properties = e.GetCurrentPoint(WidgetRoot).Properties;
        if (properties.IsLeftButtonPressed)
        {
            _isDragging = true;
            GetCursorPos(out _dragStartCursor);

            if (_appWindow != null)
            {
                _dragStartWindow = _appWindow.Position;
            }

            WidgetRoot.CapturePointer(e.Pointer);
            e.Handled = true;
        }
    }

    private void WidgetRoot_PointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_isDragging && _appWindow != null)
        {
            GetCursorPos(out POINT currentCursor);
            int deltaX = currentCursor.X - _dragStartCursor.X;
            int deltaY = currentCursor.Y - _dragStartCursor.Y;

            _appWindow.Move(new Windows.Graphics.PointInt32(
                _dragStartWindow.X + deltaX,
                _dragStartWindow.Y + deltaY
            ));
            e.Handled = true;
        }
    }

    private void WidgetRoot_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            WidgetRoot.ReleasePointerCapture(e.Pointer);
            e.Handled = true;

            // Lưu tọa độ desktop đã kéo thả
            if (_appWindow != null)
            {
                _settings.WidgetLastX = _appWindow.Position.X;
                _settings.WidgetLastY = _appWindow.Position.Y;
                _settingsService.SaveSettings(_settings);
            }
        }
    }

    private void WidgetRoot_PointerCaptureLost(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _isDragging = false;
    }

    private void WidgetRoot_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        ToggleStyleButton_Click(sender, e);
    }

    private static Windows.UI.Color ParseHexColor(string hex)
    {
        hex = hex.Replace("#", "");
        if (hex.Length == 6)
        {
            return Windows.UI.Color.FromArgb(255,
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16));
        }
        else if (hex.Length == 8)
        {
            return Windows.UI.Color.FromArgb(
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16),
                Convert.ToByte(hex.Substring(6, 2), 16));
        }
        return Windows.UI.Color.FromArgb(255, 56, 189, 248);
    }
    private void SetSevenSegmentDigit(int digit, Microsoft.UI.Xaml.Shapes.Rectangle? segA, Microsoft.UI.Xaml.Shapes.Rectangle? segB, Microsoft.UI.Xaml.Shapes.Rectangle? segC, Microsoft.UI.Xaml.Shapes.Rectangle? segD, Microsoft.UI.Xaml.Shapes.Rectangle? segE, Microsoft.UI.Xaml.Shapes.Rectangle? segF, Microsoft.UI.Xaml.Shapes.Rectangle? segG)
    {
        if (segA == null || segB == null || segC == null || segD == null || segE == null || segF == null || segG == null) return;
        bool a = digit is 0 or 2 or 3 or 5 or 6 or 7 or 8 or 9;
        bool b = digit is 0 or 1 or 2 or 3 or 4 or 7 or 8 or 9;
        bool c = digit is 0 or 1 or 3 or 4 or 5 or 6 or 7 or 8 or 9;
        bool d = digit is 0 or 2 or 3 or 5 or 6 or 8 or 9;
        bool e = digit is 0 or 2 or 6 or 8;
        bool f = digit is 0 or 4 or 5 or 6 or 8 or 9;
        bool g = digit is 2 or 3 or 4 or 5 or 6 or 8 or 9;

        segA.Opacity = a ? 1.0 : 0.08;
        segB.Opacity = b ? 1.0 : 0.08;
        segC.Opacity = c ? 1.0 : 0.08;
        segD.Opacity = d ? 1.0 : 0.08;
        segE.Opacity = e ? 1.0 : 0.08;
        segF.Opacity = f ? 1.0 : 0.08;
        segG.Opacity = g ? 1.0 : 0.08;
    }

    private static string GetCityCode(string cityName)
    {
        if (string.IsNullOrWhiteSpace(cityName)) return "SGN";
        string lower = cityName.ToLowerInvariant();
        if (lower.Contains("\u0068\u00E0\u0020\u006E\u1ED9\u0069") || lower.Contains("hanoi")) return "HAN";
        if (lower.Contains("\u0068\u1ED3\u0020\u0063\u0068\u00ED\u0020\u006D\u0069\u006E\u0068") || lower.Contains("ho chi minh") || lower.Contains("\u0073\u00E0\u0069\u0020\u0067\u00F2\u006E") || lower.Contains("saigon")) return "SGN";
        if (lower.Contains("\u0111\u00E0\u0020\u006E\u1EB5\u006E\u0067") || lower.Contains("da nang")) return "DAD";
        if (lower.Contains("\u0111\u00E0\u0020\u006E\u1EB5\u006E\u0067") || lower.Contains("da nang")) return "DAD";
        if (lower.Contains("\u0063\u1EA7\u006E\u0020\u0074\u0068\u01A1") || lower.Contains("can tho")) return "VCA";
        if (lower.Contains("\u0111\u00E0\u0020\u006E\u1EB5\u006E\u0067") || lower.Contains("da nang")) return "DAD";
        if (lower.Contains("\u0068\u0075\u1EBF") || lower.Contains("hue")) return "HUI";
        if (lower.Contains("\u0076\u0169\u006E\u0067\u0020\u0074\u00E0\u0075") || lower.Contains("vung tau")) return "VTG";
        if (lower.Contains("\u0111\u00E0\u0020\u006C\u1EA1\u0074") || lower.Contains("da lat")) return "DLI";
        if (lower.Contains("\u0070\u0068\u00FA\u0020\u0071\u0075\u1ED1\u0063") || lower.Contains("phu quoc")) return "PQC";
        if (lower.Contains("sa pa") || lower.Contains("sapa")) return "SPA";
        if (lower.Contains("quy nhÆ¡n") || lower.Contains("quy nhon")) return "UIH";
        if (lower.Contains("new york")) return "NYC";
        if (lower.Contains("bogot")) return "BOG";
        if (lower.Contains("tokyo")) return "TYO";
        if (lower.Contains("seoul")) return "ICN";
        if (lower.Contains("london")) return "LON";
        if (lower.Contains("paris")) return "PAR";
        var letters = cityName.Where(char.IsLetter).ToArray();
        return letters.Length >= 3 ? new string(letters, 0, 3).ToUpperInvariant() : "WTH";
    }
}
