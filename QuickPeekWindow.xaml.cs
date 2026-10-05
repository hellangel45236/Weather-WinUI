using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WeatherApp.ViewModels;

namespace WeatherApp;

public sealed partial class QuickPeekWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly Action _openMainWindowAction;
    private AppWindow? _appWindow;

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);

    [DllImport("gdi32.dll")]
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

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

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

    public QuickPeekWindow(MainViewModel viewModel, Action openMainWindowAction)
    {
        _viewModel = viewModel;
        _openMainWindowAction = openMainWindowAction;
        InitializeComponent();

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
            presenter.IsAlwaysOnTop = true;
            presenter.IsResizable = false;
            presenter.IsMinimizable = false;
            presenter.IsMaximizable = false;
            presenter.SetBorderAndTitleBar(false, false);
        }

        // Triệt tiêu 100% khung viền trắng của DWM & Win32 tương tự WidgetWindow
        StripWindowBorders(hWnd);

        uint dpi = GetDpiForWindow(hWnd);
        if (dpi == 0) dpi = 96;
        double scale = dpi / 96.0;

        int physicalWidth = (int)Math.Round(330 * scale);
        int physicalHeight = (int)Math.Round(250 * scale);
        _appWindow?.Resize(new Windows.Graphics.SizeInt32(physicalWidth, physicalHeight));

        // Đặt ở góc dưới bên phải màn hình (cạnh Taskbar / System Tray)
        var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
        if (displayArea != null && _appWindow != null)
        {
            int x = displayArea.WorkArea.Width - physicalWidth - 16;
            int y = displayArea.WorkArea.Height - physicalHeight - 16;
            _appWindow.Move(new Windows.Graphics.PointInt32(x, y));
        }

        UpdateContent();
    }

    private void StripWindowBorders(IntPtr hWnd)
    {
        try
        {
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

    public void UpdateContent()
    {
        LocationText.Text = _viewModel.LocationTitle;
        if (_viewModel.CurrentWeather != null)
        {
            CurrentTempText.Text = _viewModel.CurrentWeather.TemperatureText;
            CurrentConditionText.Text = _viewModel.CurrentWeather.ConditionText;
            CurrentFeelsLikeText.Text = _viewModel.CurrentWeather.FeelsLikeText;

            // Nạp icon SVG
            bool svgLoaded = false;
            try
            {
                string? iconPath = _viewModel.CurrentWeather.SvgIconPath;
                if (!string.IsNullOrEmpty(iconPath))
                {
                    var svgSource = new Microsoft.UI.Xaml.Media.Imaging.SvgImageSource(new Uri(iconPath))
                    {
                        RasterizePixelWidth = 72,
                        RasterizePixelHeight = 72
                    };
                    svgSource.OpenFailed += (s, e) =>
                    {
                        CurrentSvgImage.Visibility = Visibility.Collapsed;
                        CurrentFallbackIcon.Visibility = Visibility.Visible;
                    };
                    CurrentSvgImage.Source = svgSource;
                    CurrentSvgImage.Visibility = Visibility.Visible;
                    CurrentFallbackIcon.Visibility = Visibility.Collapsed;
                    svgLoaded = true;
                }
            }
            catch { }

            if (!svgLoaded)
            {
                CurrentSvgImage.Visibility = Visibility.Collapsed;
                CurrentFallbackIcon.Glyph = string.IsNullOrEmpty(_viewModel.CurrentWeather.IconGlyph) ? "\uf185" : _viewModel.CurrentWeather.IconGlyph;
                CurrentFallbackIcon.Visibility = Visibility.Visible;
            }
        }

        if (_viewModel.DailyForecast != null)
        {
            // Lấy 3 ngày đầu tiên
            ForecastItemsList.ItemsSource = _viewModel.DailyForecast.Take(3).ToList();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void OpenAppButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
        _openMainWindowAction?.Invoke();
    }
}
