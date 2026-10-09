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
            SetWindowLongPtr(hWnd, GWL_EXSTYLE, new IntPtr(exStyle));

            SetWindowPos(hWnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);

            int noBorderColor = DWMWA_COLOR_NONE;
            DwmSetWindowAttribute(hWnd, DWMWA_BORDER_COLOR, ref noBorderColor, sizeof(int));

            int roundPref = DWMWCP_DONOTROUND;
            DwmSetWindowAttribute(hWnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref roundPref, sizeof(int));
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

    public void ApplyWidgetStyle(string style)
    {
        _currentStyle = style;
        bool isBryanC = style == "BryanCDynamic";
        bool isIsland = style == "MiniIsland";
        bool isCompact = style == "Compact";
        bool isGlass = style == "GlassCard";

        if (!isBryanC && !isIsland && !isCompact && !isGlass)
        {
            isBryanC = true;
            _currentStyle = "BryanCDynamic";
        }

        if (BryanCDynamicView != null) BryanCDynamicView.Visibility = isBryanC ? Visibility.Visible : Visibility.Collapsed;
        if (GlassCardView != null) GlassCardView.Visibility = isGlass ? Visibility.Visible : Visibility.Collapsed;
        if (CompactBarView != null) CompactBarView.Visibility = isCompact ? Visibility.Visible : Visibility.Collapsed;
        if (MiniIslandView != null) MiniIslandView.Visibility = isIsland ? Visibility.Visible : Visibility.Collapsed;

        int cornerRadiusDip = (isBryanC || isIsland) ? 20 : 14;
        WidgetRoot.CornerRadius = new CornerRadius(cornerRadiusDip);

        IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        uint dpi = GetDpiForWindow(hWnd);
        if (dpi == 0) dpi = 96;
        double scale = dpi / 96.0;

        int widthDip = isBryanC ? 340 : (isIsland ? 290 : (isCompact ? 300 : 385));
        int heightDip = isBryanC ? 200 : (isIsland ? 40 : (isCompact ? 54 : 230));

        int physicalWidth = (int)Math.Round(widthDip * scale);
        int physicalHeight = (int)Math.Round(heightDip * scale);

        _appWindow?.Resize(new Windows.Graphics.SizeInt32(physicalWidth, physicalHeight));

        if (isBryanC && _currentWeather != null)
        {
            ApplyBryanCWeatherTheme(_currentWeather.WeatherEffect, _currentWeather.IsDay);
        }
        else if (!isBryanC)
        {
            // Khôi phục background kính tối chuẩn cho GlassCard/Compact
            var brush = new Microsoft.UI.Xaml.Media.LinearGradientBrush { StartPoint = new Windows.Foundation.Point(0, 0), EndPoint = new Windows.Foundation.Point(1, 1) };
            brush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = Windows.UI.Color.FromArgb(0xF2, 0x0F, 0x17, 0x2A), Offset = 0 });
            brush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = Windows.UI.Color.FromArgb(0xF2, 0x1E, 0x29, 0x3B), Offset = 1 });
            WidgetRoot.Background = brush;
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
    }

    private void WidgetRoot_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        WidgetRoot.Opacity = Math.Clamp(_settings.WidgetOpacity, 0.2, 1.0);
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

        // Áp dụng màu sắc & hiệu ứng đồ họa động theo thời tiết cho phong cách BryanC
        if (_currentStyle == "BryanCDynamic")
        {
            ApplyBryanCWeatherTheme(_currentWeather.WeatherEffect, _currentWeather.IsDay);
        }

        // Cập nhật dự báo giờ
        UpdateHourlyForecastRow();
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
}
