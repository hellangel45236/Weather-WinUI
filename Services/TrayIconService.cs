using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using WeatherApp.ViewModels;

namespace WeatherApp.Services;

public class TrayIconService : IDisposable
{
    private IntPtr _hWnd;
    private Window? _mainWindow;
    private MainViewModel? _viewModel;
    private QuickPeekWindow? _quickPeekWindow;
    private Action? _toggleWidgetAction;

    private const uint WM_APP = 0x8000;
    private const uint WM_TRAYICON = WM_APP + 101;
    private const uint NIM_ADD = 0x00000000;
    private const uint NIM_MODIFY = 0x00000001;
    private const uint NIM_DELETE = 0x00000002;
    private const uint NIF_MESSAGE = 0x00000001;
    private const uint NIF_ICON = 0x00000002;
    private const uint NIF_TIP = 0x00000004;
    private const uint NIF_INFO = 0x00000010;
    private const uint NIIF_INFO = 0x00000001;

    private const uint WM_SYSCOMMAND = 0x0112;
    private const nuint SC_MINIMIZE = 0xF020;
    private const nuint SC_CLOSE = 0xF060;

    private const uint WM_LBUTTONUP = 0x0202;
    private const uint WM_LBUTTONDBLCLK = 0x0203;
    private const uint WM_RBUTTONUP = 0x0205;

    private const uint MF_STRING = 0x00000000;
    private const uint MF_SEPARATOR = 0x00000800;
    private const uint MF_GRAYED = 0x00000001;
    private const uint TPM_BOTTOMALIGN = 0x0020;
    private const uint TPM_RIGHTALIGN = 0x0008;
    private const uint TPM_RETURNCMD = 0x0100;

    private const int CMD_TITLE = 1001;
    private const int CMD_QUICKPEEK = 1002;
    private const int CMD_OPENMAIN = 1003;
    private const int CMD_TOGGLEWIDGET = 1004;
    private const int CMD_REFRESH = 1005;
    private const int CMD_EXIT = 1006;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public uint uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpdata);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, uint uIDNewItem, string lpNewItem);

    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll")]
    private static extern uint TrackPopupMenuEx(IntPtr hmenu, uint fuFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_RESTORE = 9;
    private const int SW_SHOW = 5;

    private delegate IntPtr SubclassProcDelegate(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, UIntPtr uIdSubclass, IntPtr dwRefData);

    [DllImport("comctl32.dll", SetLastError = true)]
    private static extern bool SetWindowSubclass(IntPtr hWnd, SubclassProcDelegate pfnSubclass, UIntPtr uIdSubclass, IntPtr dwRefData);

    [DllImport("comctl32.dll", SetLastError = true)]
    private static extern bool RemoveWindowSubclass(IntPtr hWnd, SubclassProcDelegate pfnSubclass, UIntPtr uIdSubclass);

    [DllImport("comctl32.dll", SetLastError = true)]
    private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    private NOTIFYICONDATA _nid;
    private SubclassProcDelegate? _subclassProc;
    private IntPtr _currentIconHandle = IntPtr.Zero;
    private bool _isAdded;

    public void Initialize(Window mainWindow, MainViewModel viewModel, Action? toggleWidgetAction = null)
    {
        _mainWindow = mainWindow;
        _viewModel = viewModel;
        _toggleWidgetAction = toggleWidgetAction;
        _hWnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);

        _subclassProc = new SubclassProcDelegate(WindowSubclassProc);
        SetWindowSubclass(_hWnd, _subclassProc, new UIntPtr(101), IntPtr.Zero);

        _nid = new NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _hWnd,
            uID = 1001,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
            uCallbackMessage = WM_TRAYICON,
            szTip = "Thời Tiết WinUI 3"
        };

        LoadDefaultIcon();
        _isAdded = Shell_NotifyIcon(NIM_ADD, ref _nid);
    }

    private void LoadDefaultIcon()
    {
        try
        {
            string icoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
            if (File.Exists(icoPath))
            {
                IntPtr hIcon = LoadImage(IntPtr.Zero, icoPath, 1, 16, 16, 0x0010);
                if (hIcon != IntPtr.Zero)
                {
                    _currentIconHandle = hIcon;
                    _nid.hIcon = hIcon;
                }
            }
        }
        catch { }
    }

    public void UpdateWeather(string temp, string condition, string location)
    {
        if (!_isAdded) return;

        try
        {
            string tip = $"{location}: {temp} • {condition}";
            if (tip.Length > 120) tip = tip.Substring(0, 117) + "...";
            _nid.szTip = tip;
            _nid.uFlags = NIF_TIP;
            Shell_NotifyIcon(NIM_MODIFY, ref _nid);
        }
        catch { }
    }

    public void ShowBalloon(string title, string message)
    {
        if (!_isAdded) return;
        try
        {
            _nid.uFlags = NIF_INFO | NIF_TIP;
            _nid.szInfoTitle = title.Length > 63 ? title.Substring(0, 60) + "..." : title;
            _nid.szInfo = message.Length > 255 ? message.Substring(0, 250) + "..." : message;
            _nid.dwInfoFlags = NIIF_INFO;
            Shell_NotifyIcon(NIM_MODIFY, ref _nid);
        }
        catch { }
    }

    private IntPtr WindowSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, UIntPtr uIdSubclass, IntPtr dwRefData)
    {
        if (uMsg == WM_SYSCOMMAND)
        {
            nuint sc = (nuint)(wParam.ToInt64() & 0xFFF0);
            if (sc == SC_MINIMIZE && _viewModel?.Settings.MinimizeToTray == true)
            {
                _mainWindow?.DispatcherQueue.TryEnqueue(() =>
                {
                    ShowWindow(_hWnd, 0 /* SW_HIDE */);
                });
                return IntPtr.Zero;
            }
            if (sc == SC_CLOSE && _viewModel?.Settings.CloseToTray == true)
            {
                _mainWindow?.DispatcherQueue.TryEnqueue(() =>
                {
                    ShowWindow(_hWnd, 0 /* SW_HIDE */);
                });
                return IntPtr.Zero;
            }
        }

        if (uMsg == WM_TRAYICON)
        {
            uint mouseMsg = (uint)(lParam.ToInt64() & 0xFFFF);

            if (mouseMsg == WM_LBUTTONUP)
            {
                // Nhấp chuột trái: Bật Flyout Dự Báo Nhanh (Quick Peek)
                ShowQuickPeek();
                return IntPtr.Zero;
            }
            else if (mouseMsg == WM_LBUTTONDBLCLK)
            {
                // Nhấp đúp: Mở cửa sổ chính
                RestoreMainWindow();
                return IntPtr.Zero;
            }
            else if (mouseMsg == WM_RBUTTONUP)
            {
                // Chuột phải: Hiện context menu
                ShowContextMenu();
                return IntPtr.Zero;
            }
        }

        return DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    private void ShowQuickPeek()
    {
        if (_viewModel == null) return;

        _mainWindow?.DispatcherQueue.TryEnqueue(() =>
        {
            if (_quickPeekWindow == null)
            {
                _quickPeekWindow = new QuickPeekWindow(_viewModel, RestoreMainWindow);
                _quickPeekWindow.Closed += (s, e) => _quickPeekWindow = null;
                _quickPeekWindow.Activate();
            }
            else
            {
                _quickPeekWindow.Activate();
            }
        });
    }

    public void RestoreMainWindow()
    {
        _mainWindow?.DispatcherQueue.TryEnqueue(() =>
        {
            if (_mainWindow != null)
            {
                if (_mainWindow.AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
                {
                    presenter.Restore();
                }
                _mainWindow.AppWindow.Show();
                ShowWindow(_hWnd, SW_RESTORE);
                SetForegroundWindow(_hWnd);
            }
        });
    }

    private void ShowContextMenu()
    {
        GetCursorPos(out POINT pt);
        SetForegroundWindow(_hWnd);

        IntPtr hMenu = CreatePopupMenu();
        if (hMenu == IntPtr.Zero) return;

        string locationTitle = _viewModel?.LocationTitle ?? "Thời Tiết WinUI";
        string currentTemp = _viewModel?.CurrentWeather?.TemperatureText ?? "--";
        string currentCond = _viewModel?.CurrentWeather?.ConditionText ?? "";

        AppendMenu(hMenu, MF_STRING | MF_GRAYED, CMD_TITLE, $"📍 {locationTitle}: {currentTemp} ({currentCond})");
        AppendMenu(hMenu, MF_SEPARATOR, 0, string.Empty);
        AppendMenu(hMenu, MF_STRING, CMD_QUICKPEEK, "📅 Xem Dự Báo Nhanh (Quick Peek)");
        AppendMenu(hMenu, MF_STRING, CMD_OPENMAIN, "🖥️ Mở Ứng Dụng Chính");
        AppendMenu(hMenu, MF_STRING, CMD_TOGGLEWIDGET, "📌 Bật / Tắt Mini Widget Desktop");
        AppendMenu(hMenu, MF_STRING, CMD_REFRESH, "🔄 Làm Mới Dữ Liệu Thời Tiết");
        AppendMenu(hMenu, MF_SEPARATOR, 0, string.Empty);
        AppendMenu(hMenu, MF_STRING, CMD_EXIT, "❌ Thoát Hoàn Toàn Ứng Dụng");

        uint cmd = TrackPopupMenuEx(hMenu, TPM_RETURNCMD | TPM_BOTTOMALIGN | TPM_RIGHTALIGN, pt.X, pt.Y, _hWnd, IntPtr.Zero);
        DestroyMenu(hMenu);

        switch (cmd)
        {
            case CMD_QUICKPEEK:
                ShowQuickPeek();
                break;
            case CMD_OPENMAIN:
                RestoreMainWindow();
                break;
            case CMD_TOGGLEWIDGET:
                _toggleWidgetAction?.Invoke();
                break;
            case CMD_REFRESH:
                _mainWindow?.DispatcherQueue.TryEnqueue(async () =>
                {
                    if (_viewModel != null) await _viewModel.RefreshAsync();
                });
                break;
            case CMD_EXIT:
                Dispose();
                Environment.Exit(0);
                break;
        }
    }

    public void Dispose()
    {
        if (_isAdded)
        {
            Shell_NotifyIcon(NIM_DELETE, ref _nid);
            _isAdded = false;
        }

        if (_subclassProc != null && _hWnd != IntPtr.Zero)
        {
            RemoveWindowSubclass(_hWnd, _subclassProc, new UIntPtr(101));
            _subclassProc = null;
        }

        if (_currentIconHandle != IntPtr.Zero)
        {
            DestroyIcon(_currentIconHandle);
            _currentIconHandle = IntPtr.Zero;
        }
    }
}
