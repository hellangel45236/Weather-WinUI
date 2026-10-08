using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using WeatherApp.Helpers;
using WeatherApp.ViewModels;

namespace WeatherApp.Views.Tabs;

public sealed partial class OverviewTab : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(MainViewModel), typeof(OverviewTab), new PropertyMetadata(null, OnViewModelChanged));

    public MainViewModel? ViewModel
    {
        get => (MainViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public event EventHandler? ShareRequested;

    private WeatherEffectRenderer? _weatherEffectRenderer;

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is OverviewTab tab)
        {
            if (e.OldValue is MainViewModel oldVm)
            {
                oldVm.PropertyChanged -= tab.ViewModel_PropertyChanged;
            }
            if (e.NewValue is MainViewModel newVm)
            {
                newVm.PropertyChanged -= tab.ViewModel_PropertyChanged;
                newVm.PropertyChanged += tab.ViewModel_PropertyChanged;
            }
            tab.UpdateWeatherVisuals();
            tab.RedrawCanvases();
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.CurrentWeather) ||
            e.PropertyName == nameof(MainViewModel.Settings) ||
            e.PropertyName == nameof(MainViewModel.LocationTitle))
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                UpdateWeatherVisuals();
                RedrawCanvases();
            });
        }
    }

    public OverviewTab()
    {
        this.InitializeComponent();

        _weatherEffectRenderer = new WeatherEffectRenderer(WeatherEffectsCanvas, LightningFlashOverlay);

        this.Loaded += (s, e) =>
        {
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
                ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
            UpdateWeatherVisuals();
            RedrawCanvases();
        };

        this.Unloaded += (s, e) =>
        {
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
        };

        if (SunArcCanvas != null)
        {
            SunArcCanvas.SizeChanged += (s, e) => RenderSunArc();
        }
    }

    public void RedrawCanvases()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            RenderHourlyTemperatureTrendline();
            RenderSunArc();
        });
    }

    public void StartEffects()
    {
        if (ViewModel?.Settings.EnableWeatherEffects == true)
        {
            _weatherEffectRenderer?.Resume();
        }
    }

    public void StopEffects()
    {
        _weatherEffectRenderer?.Pause();
    }

    public void UpdateWeatherVisuals()
    {
        if (ViewModel?.CurrentWeather == null) return;

        // Cập nhật hiệu ứng thời tiết nền nếu người dùng bật
        if (ViewModel.Settings.EnableWeatherEffects)
        {
            _weatherEffectRenderer?.SetWeatherEffect(ViewModel.CurrentWeather.WeatherEffect);
            _weatherEffectRenderer?.Resume();
        }
        else
        {
            _weatherEffectRenderer?.Pause();
            WeatherEffectsCanvas.Children.Clear();
        }

        // Cập nhật hình nền thành phố nếu được bật
        if (ViewModel.Settings.EnableCityBackground && !string.IsNullOrEmpty(ViewModel.CurrentWeather.CityImagePath))
        {
            if (CityHeroImageBrush != null)
            {
                CityHeroImageBrush.ImageSource = CityBackgroundHelper.LoadOptimizedBitmap(ViewModel.CurrentWeather.CityImagePath);
            }
            if (CityHeroBackgroundBorder != null)
            {
                CityHeroBackgroundBorder.Visibility = Visibility.Visible;
            }
            if (HeroGradientOverlay != null)
            {
                HeroGradientOverlay.Opacity = 0.65;
            }
        }
        else
        {
            if (CityHeroImageBrush != null)
            {
                CityHeroImageBrush.ImageSource = null;
            }
            if (CityHeroBackgroundBorder != null)
            {
                CityHeroBackgroundBorder.Visibility = Visibility.Collapsed;
            }
            if (HeroGradientOverlay != null)
            {
                HeroGradientOverlay.Opacity = 0.88;
            }
        }

        // Cập nhật icon SVG vector
        UpdateWeatherIcon();
    }

    private void UpdateWeatherIcon()
    {
        if (ViewModel?.CurrentWeather == null) return;

        try
        {
            string? iconPath = ViewModel.CurrentWeather.SvgIconPath;
            if (string.IsNullOrEmpty(iconPath))
            {
                iconPath = ViewModel.CurrentWeather.SvgIconFullPath;
            }

            if (!string.IsNullOrEmpty(iconPath))
            {
                var uri = new Uri(iconPath);
                var svgSource = new SvgImageSource(uri)
                {
                    RasterizePixelWidth = 240,
                    RasterizePixelHeight = 240
                };

                svgSource.OpenFailed += (s, e) =>
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        WeatherSvgImage.Visibility = Visibility.Collapsed;
                        FallbackWeatherIcon.Visibility = Visibility.Visible;
                    });
                };

                WeatherSvgImage.Source = svgSource;
                WeatherSvgImage.Visibility = Visibility.Visible;
                FallbackWeatherIcon.Visibility = Visibility.Collapsed;
            }
            else
            {
                WeatherSvgImage.Visibility = Visibility.Collapsed;
                FallbackWeatherIcon.Visibility = Visibility.Visible;
            }
        }
        catch
        {
            WeatherSvgImage.Visibility = Visibility.Collapsed;
            FallbackWeatherIcon.Visibility = Visibility.Visible;
        }
    }

    private void TimeScrubberSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        try
        {
            int hour = (int)Math.Round(e.NewValue);
            ViewModel?.ScrubToHour(hour);
        }
        catch { }
    }

    private void ResetScrubberButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ViewModel?.ResetTimeScrubbing();
            UpdateWeatherVisuals();
        }
        catch { }
    }

    private void ShareWeatherCardButton_Click(object sender, RoutedEventArgs e)
    {
        ShareRequested?.Invoke(this, EventArgs.Empty);
    }

    public void RenderHourlyTemperatureTrendline()
    {
        try
        {
            if (HourlyTrendlineCanvas == null || ViewModel?.HourlyForecast == null || ViewModel.HourlyForecast.Count == 0)
                return;

            HourlyTrendlineCanvas.Children.Clear();

            var items = ViewModel.HourlyForecast.ToList();
            int count = items.Count;
            if (count < 2) return;

            double colWidth = 92.0;
            double cardWidth = 82.0;
            double totalWidth = count * colWidth;
            HourlyTrendlineCanvas.Width = totalWidth;

            double canvasHeight = 74.0;
            HourlyTrendlineCanvas.Height = canvasHeight;

            double minTemp = items.Min(x => x.TempValue);
            double maxTemp = items.Max(x => x.TempValue);
            double tempRange = Math.Max(1.0, maxTemp - minTemp);

            double topPadding = 24.0;
            double bottomPadding = 14.0;
            double usableHeight = canvasHeight - topPadding - bottomPadding;

            var points = new List<Windows.Foundation.Point>();
            for (int i = 0; i < count; i++)
            {
                double x = i * colWidth + (cardWidth / 2.0);
                double normalized = (items[i].TempValue - minTemp) / tempRange;
                double y = topPadding + (1.0 - normalized) * usableHeight;
                points.Add(new Windows.Foundation.Point(x, y));
            }

            var strokeFigure = new PathFigure
            {
                StartPoint = points[0],
                IsClosed = false
            };

            var fillFigure = new PathFigure
            {
                StartPoint = new Windows.Foundation.Point(points[0].X, canvasHeight),
                IsClosed = true
            };
            fillFigure.Segments.Add(new LineSegment { Point = points[0] });

            for (int i = 0; i < count - 1; i++)
            {
                var p0 = i > 0 ? points[i - 1] : points[i];
                var p1 = points[i];
                var p2 = points[i + 1];
                var p3 = (i + 2 < count) ? points[i + 2] : p2;

                double tension = 0.5;
                var cp1 = new Windows.Foundation.Point(
                    p1.X + (p2.X - p0.X) * tension / 3.0,
                    p1.Y + (p2.Y - p0.Y) * tension / 3.0
                );
                var cp2 = new Windows.Foundation.Point(
                    p2.X - (p3.X - p1.X) * tension / 3.0,
                    p2.Y - (p3.Y - p1.Y) * tension / 3.0
                );

                var segment = new BezierSegment
                {
                    Point1 = cp1,
                    Point2 = cp2,
                    Point3 = p2
                };
                strokeFigure.Segments.Add(segment);
                fillFigure.Segments.Add(segment);
            }

            fillFigure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(points[count - 1].X, canvasHeight) });

            var fillGeo = new PathGeometry();
            fillGeo.Figures.Add(fillFigure);

            var fillBrush = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(0, 1)
            };
            fillBrush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(90, 56, 189, 248), Offset = 0.0 });
            fillBrush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(10, 56, 189, 248), Offset = 0.75 });
            fillBrush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(0, 56, 189, 248), Offset = 1.0 });

            var areaFill = new Microsoft.UI.Xaml.Shapes.Path
            {
                Data = fillGeo,
                Fill = fillBrush
            };
            HourlyTrendlineCanvas.Children.Add(areaFill);

            var strokeGeo = new PathGeometry();
            strokeGeo.Figures.Add(strokeFigure);

            var strokePath = new Microsoft.UI.Xaml.Shapes.Path
            {
                Data = strokeGeo,
                Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(220, 56, 189, 248)),
                StrokeThickness = 2.5
            };
            HourlyTrendlineCanvas.Children.Add(strokePath);

            for (int i = 0; i < count; i++)
            {
                var pt = points[i];
                var dot = new Microsoft.UI.Xaml.Shapes.Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
                    Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 56, 189, 248)),
                    StrokeThickness = 2
                };
                Canvas.SetLeft(dot, pt.X - 3);
                Canvas.SetTop(dot, pt.Y - 3);
                HourlyTrendlineCanvas.Children.Add(dot);

                var label = new TextBlock
                {
                    Text = items[i].TempDisplay,
                    FontSize = 10.5,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(220, 255, 255, 255)),
                    TextAlignment = TextAlignment.Center,
                    Width = 40
                };
                Canvas.SetLeft(label, pt.X - 20);
                Canvas.SetTop(label, pt.Y - 18);
                HourlyTrendlineCanvas.Children.Add(label);
            }
        }
        catch { }
    }

    public void RenderSunArc()
    {
        if (SunArcCanvas == null || ViewModel?.CurrentWeather == null) return;

        SunArcCanvas.Children.Clear();

        double width = SunArcCanvas.ActualWidth;
        double height = SunArcCanvas.ActualHeight;
        if (width <= 20 || height <= 10) return;

        double horizonY = height - 4;
        double leftX = 10;
        double rightX = width - 10;
        double peakY = 5;

        // 1. Đường chân trời chấm nét đứt
        var horizonLine = new Microsoft.UI.Xaml.Shapes.Line
        {
            X1 = 4,
            Y1 = horizonY,
            X2 = width - 4,
            Y2 = horizonY,
            Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(40, 255, 255, 255)),
            StrokeThickness = 1,
            StrokeDashArray = new DoubleCollection { 2, 2 }
        };
        SunArcCanvas.Children.Add(horizonLine);

        // 2. Vòng cung quỹ đạo mặt trời (Cubic/Quadratic Bézier Path)
        var pathGeometry = new PathGeometry();
        var figure = new PathFigure
        {
            StartPoint = new Windows.Foundation.Point(leftX, horizonY),
            IsClosed = false
        };

        double midX = (leftX + rightX) / 2.0;
        var bezier = new QuadraticBezierSegment
        {
            Point1 = new Windows.Foundation.Point(midX, peakY - (horizonY - peakY) * 0.9),
            Point2 = new Windows.Foundation.Point(rightX, horizonY)
        };
        figure.Segments.Add(bezier);
        pathGeometry.Figures.Add(figure);

        var arcPath = new Microsoft.UI.Xaml.Shapes.Path
        {
            Data = pathGeometry,
            Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(90, 255, 255, 255)),
            StrokeThickness = 1.6,
            StrokeDashArray = new DoubleCollection { 3, 2.5 }
        };
        SunArcCanvas.Children.Add(arcPath);

        // 3. Vị trí Mặt Trời / Mặt Trăng hiện tại trên cung
        double progress = Math.Clamp(ViewModel.CurrentWeather.SunProgressPercent, 0.0, 1.0);
        double angleRad = Math.PI * (1.0 - progress);
        double radiusX = (rightX - leftX) / 2.0;
        double radiusY = horizonY - peakY;
        double currentX = midX + radiusX * Math.Cos(angleRad);
        double currentY = horizonY - radiusY * Math.Sin(angleRad);

        bool isSun = ViewModel.CurrentWeather.IsSunVisible;

        // Vòng phát sáng ngoại vi (Glow)
        var glow = new Microsoft.UI.Xaml.Shapes.Ellipse
        {
            Width = 18,
            Height = 18,
            Fill = new SolidColorBrush(isSun
                ? Windows.UI.Color.FromArgb(70, 251, 191, 36)
                : Windows.UI.Color.FromArgb(60, 96, 165, 250))
        };
        Canvas.SetLeft(glow, currentX - 9);
        Canvas.SetTop(glow, currentY - 9);
        SunArcCanvas.Children.Add(glow);

        // Chấm tròn Mặt Trời / Mặt Trăng
        var celestialDot = new Microsoft.UI.Xaml.Shapes.Ellipse
        {
            Width = 10,
            Height = 10,
            Fill = new SolidColorBrush(isSun
                ? Windows.UI.Color.FromArgb(255, 245, 158, 11)
                : Windows.UI.Color.FromArgb(255, 147, 197, 253)),
            Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
            StrokeThickness = 1.5
        };
        Canvas.SetLeft(celestialDot, currentX - 5);
        Canvas.SetTop(celestialDot, currentY - 5);
        SunArcCanvas.Children.Add(celestialDot);
    }
}
