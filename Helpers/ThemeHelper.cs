using Microsoft.UI.Xaml;

namespace WeatherApp.Helpers;

public static class ThemeHelper
{
    private static Window? _window;

    public static void Initialize(Window window)
    {
        _window = window;
    }

    public static ElementTheme CurrentTheme
    {
        get
        {
            try
            {
                if (_window?.Content is FrameworkElement rootElement)
                {
                    return rootElement.RequestedTheme;
                }
            }
            catch { }
            return ElementTheme.Default;
        }
    }

    public static event Action<ElementTheme>? ThemeChanged;

    public static void SetTheme(ElementTheme theme)
    {
        try
        {
            if (_window?.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = theme;
            }
            ThemeChanged?.Invoke(theme);
        }
        catch { }
    }

    public static bool IsDarkTheme(FrameworkElement? element = null)
    {
        try
        {
            if (_window?.Content is FrameworkElement root)
            {
                if (root.RequestedTheme == ElementTheme.Light) return false;
                if (root.RequestedTheme == ElementTheme.Dark) return true;
                if (root.ActualTheme == ElementTheme.Light) return false;
                if (root.ActualTheme == ElementTheme.Dark) return true;
            }
            if (element != null)
            {
                if (element.RequestedTheme == ElementTheme.Light) return false;
                if (element.RequestedTheme == ElementTheme.Dark) return true;
                return element.ActualTheme != ElementTheme.Light;
            }
            if (_window?.Content is FrameworkElement rootElem)
            {
                return rootElem.ActualTheme != ElementTheme.Light;
            }
            if (Application.Current?.RequestedTheme == ApplicationTheme.Light) return false;
        }
        catch { }
        return true;
    }
}
