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

    public static void SetTheme(ElementTheme theme)
    {
        try
        {
            if (_window?.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = theme;
            }
        }
        catch { }
    }
}
