using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace WeatherApp.Helpers;

public static class AnimationHelper
{
    private static readonly Windows.UI.ViewManagement.UISettings? _uiSettings = TryGetUiSettings();

    private static Windows.UI.ViewManagement.UISettings? TryGetUiSettings()
    {
        try
        {
            return new Windows.UI.ViewManagement.UISettings();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Kiểm tra xem Windows có cho phép hiển thị animation hay người dùng đã bật Reduce Motion
    /// </summary>
    public static bool AreAnimationsEnabled
    {
        get
        {
            try
            {
                return _uiSettings?.AnimationsEnabled ?? true;
            }
            catch
            {
                return true;
            }
        }
    }

    /// <summary>
    /// Tính toán khoảng cách góc quay ngắn nhất giữa hai góc (tránh quay vòng 350 độ khi đổi từ 355 sang 5)
    /// </summary>
    public static double CalculateShortestAngularDifference(double fromAngle, double toAngle)
    {
        double diff = (toAngle - fromAngle) % 360.0;
        if (diff > 180.0) diff -= 360.0;
        else if (diff < -180.0) diff += 360.0;
        return diff;
    }

    /// <summary>
    /// Áp dụng hiệu ứng Fluent Slide-Up & Fade-In cho một phần tử giao diện
    /// </summary>
    public static void SlideUpFadeIn(UIElement? element, double fromY = 10.0, double durationMs = 180.0)
    {
        if (element == null) return;

        if (!AreAnimationsEnabled)
        {
            element.Opacity = 1.0;
            if (element.RenderTransform is TranslateTransform ttNoAnim)
            {
                ttNoAnim.Y = 0;
            }
            return;
        }

        // Bảo đảm phần tử có TranslateTransform
        TranslateTransform tt;
        if (element.RenderTransform is TranslateTransform existingTt)
        {
            tt = existingTt;
        }
        else
        {
            tt = new TranslateTransform();
            element.RenderTransform = tt;
        }

        tt.Y = fromY;
        element.Opacity = 0.0;

        var sb = new Storyboard();

        var opacityAnim = new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(durationMs),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(opacityAnim, element);
        Storyboard.SetTargetProperty(opacityAnim, "Opacity");
        sb.Children.Add(opacityAnim);

        var slideAnim = new DoubleAnimation
        {
            From = fromY,
            To = 0.0,
            Duration = TimeSpan.FromMilliseconds(durationMs),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(slideAnim, tt);
        Storyboard.SetTargetProperty(slideAnim, "Y");
        sb.Children.Add(slideAnim);

        sb.Begin();
    }
}
