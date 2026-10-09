using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using WeatherApp.Helpers;
using WeatherApp.Models;
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
    private Storyboard? _heroIconStoryboard;
    private Storyboard? _windNeedleStoryboard;
    private double _currentWindAngle = 0;

    private Microsoft.UI.Xaml.Shapes.Ellipse? _uvDot;
    private TranslateTransform? _uvDotTransform;
    private Storyboard? _uvDotStoryboard;
    private double _currentUvX = -999;

    private Microsoft.UI.Xaml.Shapes.Ellipse? _aqiDot;
    private TranslateTransform? _aqiDotTransform;
    private Storyboard? _aqiDotStoryboard;
    private double _currentAqiX = -999;

    private bool _hoverPhysicsInitialized = false;

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
            e.PropertyName == nameof(MainViewModel.LocationTitle) ||
            e.PropertyName == nameof(MainViewModel.IsBatterySavingActive) ||
            e.PropertyName == nameof(MainViewModel.IsTimeScrubbingActive) ||
            e.PropertyName == nameof(MainViewModel.ScrubbedSliderValue))
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                UpdateWeatherVisuals();
                RedrawCanvases();
            });
        }
        else if (e.PropertyName == nameof(MainViewModel.IsAdviceExpanded))
        {
            if (ViewModel?.IsAdviceExpanded == true && AdviceDetailsPanel != null)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    AnimationHelper.SlideUpFadeIn(AdviceDetailsPanel, 12, 300);
                });
            }
        }
        else if (e.PropertyName == nameof(MainViewModel.IsMetricDetailOpen))
        {
            if (ViewModel?.IsMetricDetailOpen == true)
            {
                DispatcherQueue.TryEnqueue(ShowMetricDetailModal);
            }
            else
            {
                DispatcherQueue.TryEnqueue(HideMetricDetailModal);
            }
        }
        else if (e.PropertyName == nameof(MainViewModel.SelectedMetricDetail))
        {
            if (ViewModel?.IsMetricDetailOpen == true)
            {
                DispatcherQueue.TryEnqueue(ShowMetricDetailModal);
            }
        }
    }

    public OverviewTab()
    {
        this.InitializeComponent();

        _weatherEffectRenderer = new WeatherEffectRenderer(WeatherEffectsCanvas, LightningFlashOverlay);

        if (OverviewScrollViewer != null)
        {
            OverviewScrollViewer.ViewChanged += OverviewScrollViewer_ViewChanged;
        }

        this.KeyDown += (s, e) =>
        {
            if (e.Key == Windows.System.VirtualKey.Escape && ViewModel?.IsMetricDetailOpen == true)
            {
                HideMetricDetailModal();
                e.Handled = true;
            }
        };

        this.Loaded += (s, e) =>
        {
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
                ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
            SetupAllCardHoverPhysics();
            UpdateResponsiveLayout(this.ActualWidth);
            UpdateWeatherVisuals();
            RedrawCanvases();
            StartHeroIconAnimation();
        };

        this.Unloaded += (s, e) =>
        {
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
            StopHeroIconAnimation();
            _windNeedleStoryboard?.Stop();
            _uvDotStoryboard?.Stop();
            _aqiDotStoryboard?.Stop();
        };

        this.SizeChanged += (s, e) =>
        {
            UpdateResponsiveLayout(e.NewSize.Width);
            UpdateModalResponsiveSize();
            RenderVisualGauges();
        };

        if (SunArcCanvas != null)
        {
            SunArcCanvas.SizeChanged += (s, e) => RenderSunArc();
        }

        if (UvGaugeCanvas != null)
        {
            UvGaugeCanvas.SizeChanged += (s, e) => RenderVisualGauges();
        }

        if (AqiGaugeCanvas != null)
        {
            AqiGaugeCanvas.SizeChanged += (s, e) => RenderVisualGauges();
        }
    }

    public void RedrawCanvases()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            RenderHourlyTemperatureTrendline();
            RenderSunArc();
            RenderVisualGauges();
        });
    }

    public void StartEffects()
    {
        bool shouldRun = ViewModel?.Settings.EnableWeatherEffects == true &&
            !(ViewModel.Settings.EnableBatterySaverOptimization && ViewModel.IsBatterySavingActive);
        if (shouldRun)
        {
            _weatherEffectRenderer?.Resume();
            StartHeroIconAnimation();
        }
    }

    public void StopEffects()
    {
        _weatherEffectRenderer?.Pause();
        StopHeroIconAnimation();
    }

    public void UpdateWeatherVisuals()
    {
        if (ViewModel?.CurrentWeather == null) return;

        // Cập nhật hiệu ứng thời tiết nền nếu người dùng bật và không bị tiết kiệm pin
        bool shouldRunEffects = ViewModel.Settings.EnableWeatherEffects &&
            !(ViewModel.Settings.EnableBatterySaverOptimization && ViewModel.IsBatterySavingActive);

        if (shouldRunEffects)
        {
            _weatherEffectRenderer?.SetWeatherEffect(ViewModel.CurrentWeather.WeatherEffect);
            _weatherEffectRenderer?.Resume();
            StartHeroIconAnimation();
        }
        else
        {
            _weatherEffectRenderer?.Pause();
            WeatherEffectsCanvas.Children.Clear();
            StopHeroIconAnimation();
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

        // Cập nhật các chỉ số trực quan hoá (Visual Gauges & Compass)
        RenderVisualGauges();
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

    private void Flyout_Opened(object sender, object e)
    {
        if (ViewModel == null) return;
        FlyoutAmbientToggle.IsOn = ViewModel.IsAmbientSoundPlaying;
        FlyoutVolumeSlider.Value = ViewModel.Settings.AmbientSoundVolume * 100.0;
        FlyoutVolumeText.Text = $"{(int)FlyoutVolumeSlider.Value}%";
        UpdateSoundPresetButtonsHighlight();
    }

    private void FlyoutAmbientToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null && ViewModel.IsAmbientSoundPlaying != FlyoutAmbientToggle.IsOn)
        {
            ViewModel.ToggleAmbientSound();
        }
    }

    private void FlyoutVolumeSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (FlyoutVolumeText != null)
        {
            FlyoutVolumeText.Text = $"{(int)e.NewValue}%";
        }
        if (ViewModel != null)
        {
            ViewModel.ChangeAmbientSoundVolume(e.NewValue / 100.0);
        }
    }

    private void SoundPresetButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag && ViewModel != null)
        {
            ViewModel.ChangeAmbientSound(tag);
            FlyoutAmbientToggle.IsOn = ViewModel.IsAmbientSoundPlaying;
            UpdateSoundPresetButtonsHighlight();
        }
    }

    private void UpdateSoundPresetButtonsHighlight()
    {
        if (ViewModel == null) return;
        string activePreset = ViewModel.Settings.SelectedAmbientSound ?? "Auto";

        Button[] buttons = { BtnSoundAuto, BtnSoundRain, BtnSoundThunder, BtnSoundPineWind, BtnSoundOcean, BtnSoundCafeRain };
        foreach (var b in buttons)
        {
            if (b == null) continue;
            bool isActive = string.Equals(b.Tag as string, activePreset, StringComparison.OrdinalIgnoreCase);
            if (isActive)
            {
                b.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(50, 56, 189, 248));
                b.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 56, 189, 248));
                b.BorderThickness = new Thickness(1);
            }
            else
            {
                b.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(15, 255, 255, 255));
                b.BorderBrush = null;
                b.BorderThickness = new Thickness(0);
            }
        }
    }

    public enum HourlyChartMetricMode
    {
        Temperature,
        RainProbability,
        WindSpeed,
        UvIndex
    }

    private HourlyChartMetricMode _currentHourlyMetric = HourlyChartMetricMode.Temperature;

    private void HourlyMetricTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tagStr)
        {
            if (Enum.TryParse<HourlyChartMetricMode>(tagStr, true, out var mode))
            {
                if (_currentHourlyMetric == mode) return;
                _currentHourlyMetric = mode;
                UpdateHourlyMetricButtonsStyle();
                RenderHourlyTrendline();
            }
        }
    }

    private void UpdateHourlyMetricButtonsStyle()
    {
        Button[] buttons = { BtnHourlyTemp, BtnHourlyRain, BtnHourlyWind, BtnHourlyUv };
        HourlyChartMetricMode[] modes = { HourlyChartMetricMode.Temperature, HourlyChartMetricMode.RainProbability, HourlyChartMetricMode.WindSpeed, HourlyChartMetricMode.UvIndex };

        for (int i = 0; i < buttons.Length; i++)
        {
            var btn = buttons[i];
            if (btn == null) continue;
            bool isActive = modes[i] == _currentHourlyMetric;

            if (isActive)
            {
                btn.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(45, 56, 189, 248));
                btn.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 56, 189, 248));
                btn.BorderThickness = new Thickness(1);
            }
            else
            {
                btn.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                btn.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                btn.BorderThickness = new Thickness(1);
            }

            if (btn.Content is StackPanel sp && sp.Children.Count > 1 && sp.Children[1] is TextBlock tb)
            {
                tb.FontWeight = isActive ? Microsoft.UI.Text.FontWeights.Bold : Microsoft.UI.Text.FontWeights.Normal;
                tb.Opacity = isActive ? 1.0 : 0.75;
            }
        }
    }

    public void RenderHourlyTemperatureTrendline() => RenderHourlyTrendline();

    public void RenderHourlyTrendline()
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

            // 1. Trích xuất giá trị và nhãn tương ứng theo metric được chọn
            double GetItemValue(HourlyForecastItem it) => _currentHourlyMetric switch
            {
                HourlyChartMetricMode.Temperature => it.TempValue,
                HourlyChartMetricMode.RainProbability => (double)it.RainProbabilityValue,
                HourlyChartMetricMode.WindSpeed => it.WindSpeedValue,
                HourlyChartMetricMode.UvIndex => it.UvValue,
                _ => it.TempValue
            };

            string GetItemDisplay(HourlyForecastItem it) => _currentHourlyMetric switch
            {
                HourlyChartMetricMode.Temperature => it.TempDisplay,
                HourlyChartMetricMode.RainProbability => it.RainProbabilityText,
                HourlyChartMetricMode.WindSpeed => it.WindSpeedDisplay,
                HourlyChartMetricMode.UvIndex => it.UvDisplay,
                _ => it.TempDisplay
            };

            // Bảng màu stroke & fill tương ứng
            var (strokeColor, fillStartColor, fillEndColor) = _currentHourlyMetric switch
            {
                HourlyChartMetricMode.Temperature => (
                    Windows.UI.Color.FromArgb(230, 56, 189, 248),
                    Windows.UI.Color.FromArgb(90, 56, 189, 248),
                    Windows.UI.Color.FromArgb(10, 56, 189, 248)),
                HourlyChartMetricMode.RainProbability => (
                    Windows.UI.Color.FromArgb(240, 14, 165, 233),
                    Windows.UI.Color.FromArgb(100, 14, 165, 233),
                    Windows.UI.Color.FromArgb(12, 14, 165, 233)),
                HourlyChartMetricMode.WindSpeed => (
                    Windows.UI.Color.FromArgb(240, 16, 185, 129),
                    Windows.UI.Color.FromArgb(100, 16, 185, 129),
                    Windows.UI.Color.FromArgb(12, 16, 185, 129)),
                HourlyChartMetricMode.UvIndex => (
                    Windows.UI.Color.FromArgb(240, 245, 158, 11),
                    Windows.UI.Color.FromArgb(100, 245, 158, 11),
                    Windows.UI.Color.FromArgb(12, 245, 158, 11)),
                _ => (
                    Windows.UI.Color.FromArgb(230, 56, 189, 248),
                    Windows.UI.Color.FromArgb(90, 56, 189, 248),
                    Windows.UI.Color.FromArgb(10, 56, 189, 248))
            };

            double minVal = items.Min(GetItemValue);
            double maxVal = items.Max(GetItemValue);

            // Đảm bảo dải đo tối thiểu để biểu đồ không bị phẳng lì
            double rangeMinFloor = _currentHourlyMetric switch
            {
                HourlyChartMetricMode.RainProbability => 10.0,
                HourlyChartMetricMode.UvIndex => 1.0,
                HourlyChartMetricMode.WindSpeed => 2.0,
                _ => 1.0
            };
            double valRange = Math.Max(rangeMinFloor, maxVal - minVal);

            double topPadding = 24.0;
            double bottomPadding = 14.0;
            double usableHeight = canvasHeight - topPadding - bottomPadding;

            var points = new List<Windows.Foundation.Point>();
            for (int i = 0; i < count; i++)
            {
                double x = i * colWidth + (cardWidth / 2.0);
                double normalized = (GetItemValue(items[i]) - minVal) / valRange;
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
            fillBrush.GradientStops.Add(new GradientStop { Color = fillStartColor, Offset = 0.0 });
            fillBrush.GradientStops.Add(new GradientStop { Color = fillEndColor, Offset = 0.75 });
            fillBrush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(0, fillStartColor.R, fillStartColor.G, fillStartColor.B), Offset = 1.0 });

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
                Stroke = new SolidColorBrush(strokeColor),
                StrokeThickness = 2.5
            };
            HourlyTrendlineCanvas.Children.Add(strokePath);

            for (int i = 0; i < count; i++)
            {
                var pt = points[i];

                // Cột xác suất mưa mờ ở chân biểu đồ khi đang ở chế độ nhiệt độ hoặc chế độ mưa
                if ((_currentHourlyMetric == HourlyChartMetricMode.Temperature || _currentHourlyMetric == HourlyChartMetricMode.RainProbability) &&
                    items[i].HasRainChance && items[i].RainProbabilityValue > 0)
                {
                    double barH = Math.Clamp((items[i].RainProbabilityValue / 100.0) * 18.0, 3.0, 18.0);
                    var rainBar = new Microsoft.UI.Xaml.Shapes.Rectangle
                    {
                        Width = 12,
                        Height = barH,
                        RadiusX = 2.5,
                        RadiusY = 2.5,
                        Fill = new SolidColorBrush(_currentHourlyMetric == HourlyChartMetricMode.RainProbability 
                            ? Windows.UI.Color.FromArgb(120, 14, 165, 233) 
                            : Windows.UI.Color.FromArgb(65, 0, 153, 188))
                    };
                    Canvas.SetLeft(rainBar, pt.X - 6);
                    Canvas.SetTop(rainBar, canvasHeight - barH);
                    HourlyTrendlineCanvas.Children.Add(rainBar);
                }

                var dot = new Microsoft.UI.Xaml.Shapes.Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
                    Stroke = new SolidColorBrush(strokeColor),
                    StrokeThickness = 2
                };
                Canvas.SetLeft(dot, pt.X - 3);
                Canvas.SetTop(dot, pt.Y - 3);
                HourlyTrendlineCanvas.Children.Add(dot);

                var label = new TextBlock
                {
                    Text = GetItemDisplay(items[i]),
                    FontSize = 10.0,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(235, 255, 255, 255)),
                    TextAlignment = TextAlignment.Center,
                    Width = 56
                };
                Canvas.SetLeft(label, pt.X - 28);
                Canvas.SetTop(label, pt.Y - 18);
                HourlyTrendlineCanvas.Children.Add(label);
            }

            // Vạch kim thẳng đứng phát sáng nếu người dùng đang tua thời gian
            if (ViewModel?.IsTimeScrubbingActive == true)
            {
                int scrubbedHour = (int)Math.Round(ViewModel.ScrubbedSliderValue);
                int scrubbedIdx = items.FindIndex(x => x.HourNumber == scrubbedHour);
                if (scrubbedIdx >= 0 && scrubbedIdx < points.Count)
                {
                    var targetPt = points[scrubbedIdx];

                    var needle = new Microsoft.UI.Xaml.Shapes.Line
                    {
                        X1 = targetPt.X,
                        Y1 = 4,
                        X2 = targetPt.X,
                        Y2 = canvasHeight - 2,
                        Stroke = new SolidColorBrush(strokeColor),
                        StrokeThickness = 2,
                        StrokeDashArray = new DoubleCollection { 3, 2 }
                    };
                    HourlyTrendlineCanvas.Children.Add(needle);

                    var halo = new Microsoft.UI.Xaml.Shapes.Ellipse
                    {
                        Width = 16,
                        Height = 16,
                        Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(80, strokeColor.R, strokeColor.G, strokeColor.B))
                    };
                    Canvas.SetLeft(halo, targetPt.X - 8);
                    Canvas.SetTop(halo, targetPt.Y - 8);
                    HourlyTrendlineCanvas.Children.Add(halo);
                }
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

    private void HourlyCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is HourlyForecastItem item)
        {
            ViewModel?.ScrubToHour(item.HourNumber);
        }
    }

    private void UpdateResponsiveLayout(double width)
    {
        if (width <= 0) return;

        // 1. Tự động điều chỉnh cấu trúc 2 cột Bento Card của Hero
        bool isNarrow = width < 920;
        if (HeroBentoGrid != null && HeroBentoLeftCol != null && HeroBentoRightCol != null &&
            HeroBentoLeftCard != null && HeroBentoRightCard != null)
        {
            if (isNarrow)
            {
                HeroBentoLeftCol.Width = new GridLength(1, GridUnitType.Star);
                HeroBentoRightCol.Width = new GridLength(0);
                Grid.SetColumn(HeroBentoLeftCard, 0);
                Grid.SetRow(HeroBentoLeftCard, 0);
                Grid.SetColumn(HeroBentoRightCard, 0);
                Grid.SetRow(HeroBentoRightCard, 1);
            }
            else
            {
                HeroBentoLeftCol.Width = new GridLength(1.25, GridUnitType.Star);
                HeroBentoRightCol.Width = new GridLength(1, GridUnitType.Star);
                Grid.SetColumn(HeroBentoLeftCard, 0);
                Grid.SetRow(HeroBentoLeftCard, 0);
                Grid.SetColumn(HeroBentoRightCard, 1);
                Grid.SetRow(HeroBentoRightCard, 0);
            }
        }

        // 2. Tự động điều chỉnh lưới 8 chỉ số: 4 cột khi rộng, 2 cột khi hẹp để không bị ép chữ
        bool isMetricsNarrow = width < 960;
        if (MetricsGrid != null && MetricsCol2 != null && MetricsCol3 != null &&
            CardUv != null && CardAqi != null && CardWind != null && CardHumidity != null &&
            CardRain != null && CardPressure != null && CardSunMoon != null && CardPollutants != null)
        {
            if (isMetricsNarrow)
            {
                MetricsCol2.Width = new GridLength(0);
                MetricsCol3.Width = new GridLength(0);
                // 2 Cột x 4 Hàng
                Grid.SetRow(CardUv, 0); Grid.SetColumn(CardUv, 0);
                Grid.SetRow(CardAqi, 0); Grid.SetColumn(CardAqi, 1);
                Grid.SetRow(CardWind, 1); Grid.SetColumn(CardWind, 0);
                Grid.SetRow(CardHumidity, 1); Grid.SetColumn(CardHumidity, 1);
                Grid.SetRow(CardRain, 2); Grid.SetColumn(CardRain, 0);
                Grid.SetRow(CardPressure, 2); Grid.SetColumn(CardPressure, 1);
                Grid.SetRow(CardSunMoon, 3); Grid.SetColumn(CardSunMoon, 0);
                Grid.SetRow(CardPollutants, 3); Grid.SetColumn(CardPollutants, 1);
            }
            else
            {
                MetricsCol2.Width = new GridLength(1, GridUnitType.Star);
                MetricsCol3.Width = new GridLength(1, GridUnitType.Star);
                // 4 Cột x 2 Hàng
                Grid.SetRow(CardUv, 0); Grid.SetColumn(CardUv, 0);
                Grid.SetRow(CardAqi, 0); Grid.SetColumn(CardAqi, 1);
                Grid.SetRow(CardWind, 0); Grid.SetColumn(CardWind, 2);
                Grid.SetRow(CardHumidity, 0); Grid.SetColumn(CardHumidity, 3);
                Grid.SetRow(CardRain, 1); Grid.SetColumn(CardRain, 0);
                Grid.SetRow(CardPressure, 1); Grid.SetColumn(CardPressure, 1);
                Grid.SetRow(CardSunMoon, 1); Grid.SetColumn(CardSunMoon, 2);
                Grid.SetRow(CardPollutants, 1); Grid.SetColumn(CardPollutants, 3);
            }
        }
    }

    public void RenderVisualGauges()
    {
        var cw = ViewModel?.CurrentWeather;
        if (cw == null) return;

        // 1. La bàn gió xoay theo độ hướng gió thực tế (Shortest angular arc animation)
        AnimateWindNeedle(cw.WindDirectionDegrees);

        // 2. Con trỏ thanh quang phổ UV mượt mà
        if (UvGaugeCanvas != null)
        {
            double width = UvGaugeCanvas.ActualWidth;
            if (width > 20)
            {
                if (_uvDot == null || !UvGaugeCanvas.Children.Contains(_uvDot))
                {
                    UvGaugeCanvas.Children.Clear();
                    _uvDotTransform = new TranslateTransform();
                    _uvDot = new Microsoft.UI.Xaml.Shapes.Ellipse
                    {
                        Width = 8,
                        Height = 8,
                        Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
                        Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(220, 15, 23, 42)),
                        StrokeThickness = 1.5,
                        RenderTransform = _uvDotTransform
                    };
                    Canvas.SetLeft(_uvDot, 0);
                    Canvas.SetTop(_uvDot, 0);
                    UvGaugeCanvas.Children.Add(_uvDot);
                }

                double progress = Math.Clamp(cw.UvIndexValue / 11.0, 0.05, 0.95);
                double targetX = (progress * width) - 4;

                bool shouldAnimate = AnimationHelper.AreAnimationsEnabled &&
                    !(ViewModel?.Settings.EnableBatterySaverOptimization == true && ViewModel?.IsBatterySavingActive == true);

                if (!shouldAnimate || _currentUvX < -500)
                {
                    _uvDotStoryboard?.Stop();
                    _uvDotStoryboard = null;
                    _currentUvX = targetX;
                    if (_uvDotTransform != null) _uvDotTransform.X = targetX;
                }
                else if (Math.Abs(_currentUvX - targetX) > 0.5)
                {
                    _uvDotStoryboard?.Stop();
                    var anim = new DoubleAnimation
                    {
                        From = _currentUvX,
                        To = targetX,
                        Duration = new Duration(TimeSpan.FromMilliseconds(550)),
                        EasingFunction = new BackEase { Amplitude = 0.25, EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(anim, _uvDotTransform);
                    Storyboard.SetTargetProperty(anim, "X");

                    _uvDotStoryboard = new Storyboard();
                    _uvDotStoryboard.Children.Add(anim);
                    _uvDotStoryboard.Begin();
                    _currentUvX = targetX;
                }
            }
        }

        // 3. Con trỏ thanh quang phổ AQI mượt mà
        if (AqiGaugeCanvas != null)
        {
            double width = AqiGaugeCanvas.ActualWidth;
            if (width > 20)
            {
                if (_aqiDot == null || !AqiGaugeCanvas.Children.Contains(_aqiDot))
                {
                    AqiGaugeCanvas.Children.Clear();
                    _aqiDotTransform = new TranslateTransform();
                    _aqiDot = new Microsoft.UI.Xaml.Shapes.Ellipse
                    {
                        Width = 8,
                        Height = 8,
                        Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
                        Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(220, 15, 23, 42)),
                        StrokeThickness = 1.5,
                        RenderTransform = _aqiDotTransform
                    };
                    Canvas.SetLeft(_aqiDot, 0);
                    Canvas.SetTop(_aqiDot, 0);
                    AqiGaugeCanvas.Children.Add(_aqiDot);
                }

                double progress = Math.Clamp((double)cw.AqiValue / 300.0, 0.05, 0.95);
                double targetX = (progress * width) - 4;

                bool shouldAnimate = AnimationHelper.AreAnimationsEnabled &&
                    !(ViewModel?.Settings.EnableBatterySaverOptimization == true && ViewModel?.IsBatterySavingActive == true);

                if (!shouldAnimate || _currentAqiX < -500)
                {
                    _aqiDotStoryboard?.Stop();
                    _aqiDotStoryboard = null;
                    _currentAqiX = targetX;
                    if (_aqiDotTransform != null) _aqiDotTransform.X = targetX;
                }
                else if (Math.Abs(_currentAqiX - targetX) > 0.5)
                {
                    _aqiDotStoryboard?.Stop();
                    var anim = new DoubleAnimation
                    {
                        From = _currentAqiX,
                        To = targetX,
                        Duration = new Duration(TimeSpan.FromMilliseconds(550)),
                        EasingFunction = new BackEase { Amplitude = 0.25, EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(anim, _aqiDotTransform);
                    Storyboard.SetTargetProperty(anim, "X");

                    _aqiDotStoryboard = new Storyboard();
                    _aqiDotStoryboard.Children.Add(anim);
                    _aqiDotStoryboard.Begin();
                    _currentAqiX = targetX;
                }
            }
        }
    }

    private void AnimateWindNeedle(double targetAngle)
    {
        if (WindNeedleRotate == null) return;

        bool shouldAnimate = AnimationHelper.AreAnimationsEnabled &&
            !(ViewModel?.Settings.EnableBatterySaverOptimization == true && ViewModel?.IsBatterySavingActive == true);

        if (!shouldAnimate)
        {
            _windNeedleStoryboard?.Stop();
            _windNeedleStoryboard = null;
            _currentWindAngle = targetAngle;
            WindNeedleRotate.Angle = targetAngle;
            return;
        }

        double diff = AnimationHelper.CalculateShortestAngularDifference(_currentWindAngle, targetAngle);
        double toAngle = _currentWindAngle + diff;

        _windNeedleStoryboard?.Stop();
        var anim = new DoubleAnimation
        {
            From = _currentWindAngle,
            To = toAngle,
            Duration = new Duration(TimeSpan.FromMilliseconds(650)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        Storyboard.SetTarget(anim, WindNeedleRotate);
        Storyboard.SetTargetProperty(anim, "Angle");

        _windNeedleStoryboard = new Storyboard();
        _windNeedleStoryboard.Children.Add(anim);
        _windNeedleStoryboard.Completed += (s, e) =>
        {
            _currentWindAngle = (toAngle % 360 + 360) % 360;
            WindNeedleRotate.Angle = _currentWindAngle;
        };
        _windNeedleStoryboard.Begin();
        _currentWindAngle = toAngle;
    }

    public void StartHeroIconAnimation()
    {
        if (_heroIconStoryboard != null) return;
        if (WeatherIconFloatTransform == null) return;

        bool shouldAnimate = AnimationHelper.AreAnimationsEnabled &&
            !(ViewModel?.Settings.EnableBatterySaverOptimization == true && ViewModel?.IsBatterySavingActive == true) &&
            (ViewModel?.Settings.EnableWeatherEffects != false);

        if (!shouldAnimate)
        {
            WeatherIconFloatTransform.Y = 0;
            return;
        }

        var anim = new DoubleAnimation
        {
            From = 0.0,
            To = -5.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(2200)),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };

        Storyboard.SetTarget(anim, WeatherIconFloatTransform);
        Storyboard.SetTargetProperty(anim, "Y");

        _heroIconStoryboard = new Storyboard();
        _heroIconStoryboard.Children.Add(anim);
        _heroIconStoryboard.Begin();
    }

    public void StopHeroIconAnimation()
    {
        if (_heroIconStoryboard != null)
        {
            _heroIconStoryboard.Stop();
            _heroIconStoryboard = null;
        }
        if (WeatherIconFloatTransform != null)
        {
            WeatherIconFloatTransform.Y = 0;
        }
    }

    private void SetupAllCardHoverPhysics()
    {
        if (_hoverPhysicsInitialized) return;
        _hoverPhysicsInitialized = true;

        SetupCardHoverPhysics(HeroBentoLeftCard);
        SetupCardHoverPhysics(HeroBentoRightCard);
        SetupCardHoverPhysics(CardUv, true);
        SetupCardHoverPhysics(CardAqi, true);
        SetupCardHoverPhysics(CardWind, true);
        SetupCardHoverPhysics(CardHumidity, true);
        SetupCardHoverPhysics(CardRain, true);
        SetupCardHoverPhysics(CardPressure, true);
        SetupCardHoverPhysics(CardSunMoon, true);
        SetupCardHoverPhysics(CardPollutants, true);
    }

    private void SetupCardHoverPhysics(Border? card, bool isClickableMetric = false)
    {
        if (card == null) return;

        if (isClickableMetric)
        {
            card.PointerEntered += (s, e) =>
            {
                try { this.ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Hand); } catch { }
            };
            card.PointerExited += (s, e) =>
            {
                try { this.ProtectedCursor = null; } catch { }
            };
        }

        var scaleTransform = new ScaleTransform { ScaleX = 1.0, ScaleY = 1.0 };
        var translateTransform = new TranslateTransform { Y = 0 };
        var group = new TransformGroup();
        group.Children.Add(scaleTransform);
        group.Children.Add(translateTransform);
        card.RenderTransform = group;
        card.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);

        Storyboard? currentAnim = null;

        void AnimateTo(double targetScale, double targetY, double durationMs)
        {
            if (!AnimationHelper.AreAnimationsEnabled || (ViewModel?.Settings.EnableBatterySaverOptimization == true && ViewModel?.IsBatterySavingActive == true))
            {
                currentAnim?.Stop();
                currentAnim = null;
                scaleTransform.ScaleX = targetScale;
                scaleTransform.ScaleY = targetScale;
                translateTransform.Y = targetY;
                return;
            }

            currentAnim?.Stop();
            var sb = new Storyboard();

            var scaleXAnim = new DoubleAnimation
            {
                To = targetScale,
                Duration = new Duration(TimeSpan.FromMilliseconds(durationMs)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(scaleXAnim, scaleTransform);
            Storyboard.SetTargetProperty(scaleXAnim, "ScaleX");

            var scaleYAnim = new DoubleAnimation
            {
                To = targetScale,
                Duration = new Duration(TimeSpan.FromMilliseconds(durationMs)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(scaleYAnim, scaleTransform);
            Storyboard.SetTargetProperty(scaleYAnim, "ScaleY");

            var transAnim = new DoubleAnimation
            {
                To = targetY,
                Duration = new Duration(TimeSpan.FromMilliseconds(durationMs)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(transAnim, translateTransform);
            Storyboard.SetTargetProperty(transAnim, "Y");

            sb.Children.Add(scaleXAnim);
            sb.Children.Add(scaleYAnim);
            sb.Children.Add(transAnim);
            currentAnim = sb;
            sb.Begin();
        }

        bool isPointerDown = false;
        Windows.Foundation.Point pressPosition = default;
        long pressTimestamp = 0;

        Brush? defaultBorderBrush = card.BorderBrush;
        Brush? defaultBackground = card.Background;

        card.PointerEntered += (s, e) =>
        {
            AnimateTo(1.015, -2.5, 180);
            if (isClickableMetric && card.Tag is string tag)
            {
                var glowColor = GetMetricGlowColor(tag);
                card.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(190, glowColor.R, glowColor.G, glowColor.B));
                card.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(20, glowColor.R, glowColor.G, glowColor.B));
            }
        };

        card.PointerExited += (s, e) =>
        {
            isPointerDown = false;
            AnimateTo(1.0, 0, 220);
            if (isClickableMetric)
            {
                card.BorderBrush = defaultBorderBrush;
                card.Background = defaultBackground;
            }
        };

        card.PointerPressed += (s, e) =>
        {
            if (isClickableMetric)
            {
                isPointerDown = true;
                pressTimestamp = Environment.TickCount64;
                try { pressPosition = e.GetCurrentPoint(this).Position; } catch { }
            }
            AnimateTo(0.992, -0.5, 70);
        };

        card.PointerReleased += (s, e) =>
        {
            AnimateTo(1.015, -2.5, 150);

            if (isClickableMetric && isPointerDown)
            {
                isPointerDown = false;
                Windows.Foundation.Point releasePosition = default;
                try { releasePosition = e.GetCurrentPoint(this).Position; } catch { }
                long duration = Environment.TickCount64 - pressTimestamp;

                double dx = Math.Abs(releasePosition.X - pressPosition.X);
                double dy = Math.Abs(releasePosition.Y - pressPosition.Y);

                // Nếu thao tác là click chuột (< 20px dịch chuyển và nhả chuột trong vòng 1.2s)
                if (dx < 20 && dy < 20 && duration < 1200)
                {
                    if (card.Tag is string tag && !string.IsNullOrEmpty(tag))
                    {
                        TriggerMetricOpen(tag);
                    }
                }
            }
        };

        card.PointerCaptureLost += (s, e) =>
        {
            isPointerDown = false;
            AnimateTo(1.0, 0, 200);
            if (isClickableMetric)
            {
                card.BorderBrush = defaultBorderBrush;
                card.Background = defaultBackground;
            }
        };
    }

    private Windows.UI.Color GetMetricGlowColor(string? tag)
    {
        return tag switch
        {
            "UvIndex" => Windows.UI.Color.FromArgb(255, 245, 158, 11),       // Vàng cam ấm (#F59E0B)
            "AirQuality" => GetAqiGlowColor(),                               // Động theo AQI
            "Wind" => Windows.UI.Color.FromArgb(255, 16, 185, 129),          // Xanh lục bảo ngọc (#10B981)
            "Humidity" => Windows.UI.Color.FromArgb(255, 14, 165, 233),      // Xanh lam ngọc (#0EA5E9)
            "Rain" => Windows.UI.Color.FromArgb(255, 2, 132, 199),          // Xanh biển sâu (#0284C7)
            "Pressure" => Windows.UI.Color.FromArgb(255, 139, 92, 246),      // Tím thạch anh (#8B5CF6)
            "SunMoon" => Windows.UI.Color.FromArgb(255, 234, 179, 8),        // Vàng kim (#EAB308)
            "Pollutants" => Windows.UI.Color.FromArgb(255, 20, 184, 166),    // Xanh ngọc khói (#14B8A6)
            _ => Windows.UI.Color.FromArgb(255, 56, 189, 248)
        };
    }

    private Windows.UI.Color GetAqiGlowColor()
    {
        try
        {
            string? hex = ViewModel?.CurrentWeather?.AqiColor;
            if (!string.IsNullOrEmpty(hex))
            {
                hex = hex.TrimStart('#');
                if (hex.Length == 6)
                {
                    byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                    byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                    byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                    return Windows.UI.Color.FromArgb(255, r, g, b);
                }
            }
        }
        catch { }
        return Windows.UI.Color.FromArgb(255, 16, 185, 129);
    }

    #region DEEP-DIVE METRIC DETAIL MODAL ANIMATION & HANDLERS
    private Storyboard? _modalOpenStoryboard;
    private Storyboard? _modalCloseStoryboard;
    private long _lastMetricOpenTime = 0;

    private void TriggerMetricOpen(string? tag)
    {
        if (string.IsNullOrEmpty(tag) || ViewModel == null) return;

        long now = Environment.TickCount64;
        if (now - _lastMetricOpenTime < 250) return;
        _lastMetricOpenTime = now;

        DispatcherQueue.TryEnqueue(() =>
        {
            ViewModel.OpenMetricDetail(tag);
            ShowMetricDetailModal();
        });
    }

    private void MetricCard_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string tag)
        {
            TriggerMetricOpen(tag);
        }
    }

    private void CloseMetricModalButton_Click(object sender, RoutedEventArgs e)
    {
        HideMetricDetailModal();
    }

    private void MetricDetailBackdrop_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        HideMetricDetailModal();
    }

    private void UpdateModalResponsiveSize()
    {
        if (MetricDetailCard != null)
        {
            double availableHeight = this.ActualHeight - 48;
            if (availableHeight > 250)
            {
                MetricDetailCard.MaxHeight = Math.Min(680, availableHeight);
            }
            double availableWidth = this.ActualWidth - 32;
            if (availableWidth > 250)
            {
                MetricDetailCard.MaxWidth = Math.Min(660, availableWidth);
            }
        }
    }

    public void ShowMetricDetailModal()
    {
        if (MetricDetailOverlay == null || ModalScaleTransform == null || ModalTranslateTransform == null) return;

        try { this.ProtectedCursor = null; } catch { }
        UpdateModalResponsiveSize();

        _modalCloseStoryboard?.Stop();
        _modalOpenStoryboard?.Stop();

        MetricDetailOverlay.Visibility = Visibility.Visible;
        MetricDetailOverlay.IsHitTestVisible = true;

        bool shouldAnimate = AnimationHelper.AreAnimationsEnabled &&
            !(ViewModel?.Settings.EnableBatterySaverOptimization == true && ViewModel?.IsBatterySavingActive == true);

        if (!shouldAnimate)
        {
            MetricDetailOverlay.Opacity = 1.0;
            ModalScaleTransform.ScaleX = 1.0;
            ModalScaleTransform.ScaleY = 1.0;
            ModalTranslateTransform.Y = 0;
            return;
        }

        MetricDetailOverlay.Opacity = 0.0;
        ModalScaleTransform.ScaleX = 0.95;
        ModalScaleTransform.ScaleY = 0.95;
        ModalTranslateTransform.Y = 8;

        var sb = new Storyboard();

        // 1. Fade-in Overlay
        var fadeIn = new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(180)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(fadeIn, MetricDetailOverlay);
        Storyboard.SetTargetProperty(fadeIn, "Opacity");
        sb.Children.Add(fadeIn);

        // 2. Scale-up Card
        var scaleX = new DoubleAnimation
        {
            From = 0.95,
            To = 1.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(200)),
            EasingFunction = new BackEase { Amplitude = 0.12, EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(scaleX, ModalScaleTransform);
        Storyboard.SetTargetProperty(scaleX, "ScaleX");
        sb.Children.Add(scaleX);

        var scaleY = new DoubleAnimation
        {
            From = 0.95,
            To = 1.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(200)),
            EasingFunction = new BackEase { Amplitude = 0.12, EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(scaleY, ModalScaleTransform);
        Storyboard.SetTargetProperty(scaleY, "ScaleY");
        sb.Children.Add(scaleY);

        // 3. Translate Y Card
        var transY = new DoubleAnimation
        {
            From = 8,
            To = 0,
            Duration = new Duration(TimeSpan.FromMilliseconds(200)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(transY, ModalTranslateTransform);
        Storyboard.SetTargetProperty(transY, "Y");
        sb.Children.Add(transY);

        sb.Completed += (s, e) =>
        {
            MetricDetailOverlay.Opacity = 1.0;
            ModalScaleTransform.ScaleX = 1.0;
            ModalScaleTransform.ScaleY = 1.0;
            ModalTranslateTransform.Y = 0;
        };

        _modalOpenStoryboard = sb;
        sb.Begin();
    }

    public void HideMetricDetailModal()
    {
        if (MetricDetailOverlay == null || MetricDetailOverlay.Visibility != Visibility.Visible) return;

        bool shouldAnimate = AnimationHelper.AreAnimationsEnabled &&
            !(ViewModel?.Settings.EnableBatterySaverOptimization == true && ViewModel?.IsBatterySavingActive == true);

        if (!shouldAnimate)
        {
            _modalOpenStoryboard?.Stop();
            _modalCloseStoryboard?.Stop();
            MetricDetailOverlay.Visibility = Visibility.Collapsed;
            MetricDetailOverlay.IsHitTestVisible = false;
            MetricDetailOverlay.Opacity = 0.0;
            if (ViewModel != null) ViewModel.IsMetricDetailOpen = false;
            return;
        }

        _modalOpenStoryboard?.Stop();
        _modalCloseStoryboard?.Stop();

        var sb = new Storyboard();

        var fadeOut = new DoubleAnimation
        {
            To = 0.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(140)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        Storyboard.SetTarget(fadeOut, MetricDetailOverlay);
        Storyboard.SetTargetProperty(fadeOut, "Opacity");
        sb.Children.Add(fadeOut);

        var scaleX = new DoubleAnimation
        {
            To = 0.96,
            Duration = new Duration(TimeSpan.FromMilliseconds(140)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        Storyboard.SetTarget(scaleX, ModalScaleTransform);
        Storyboard.SetTargetProperty(scaleX, "ScaleX");
        sb.Children.Add(scaleX);

        var scaleY = new DoubleAnimation
        {
            To = 0.96,
            Duration = new Duration(TimeSpan.FromMilliseconds(140)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        Storyboard.SetTarget(scaleY, ModalScaleTransform);
        Storyboard.SetTargetProperty(scaleY, "ScaleY");
        sb.Children.Add(scaleY);

        var transY = new DoubleAnimation
        {
            To = 6,
            Duration = new Duration(TimeSpan.FromMilliseconds(140)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        Storyboard.SetTarget(transY, ModalTranslateTransform);
        Storyboard.SetTargetProperty(transY, "Y");
        sb.Children.Add(transY);

        sb.Completed += (s, e) =>
        {
            MetricDetailOverlay.Visibility = Visibility.Collapsed;
            MetricDetailOverlay.IsHitTestVisible = false;
            MetricDetailOverlay.Opacity = 0.0;
            if (ViewModel != null) ViewModel.IsMetricDetailOpen = false;
        };

        _modalCloseStoryboard = sb;
        sb.Begin();
    }
    #endregion

    #region MINI WEATHER BAR (SCROLL COMPACT HEADER)
    private bool _isMiniPillVisible = false;
    private Storyboard? _miniPillStoryboard;

    private void OverviewScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (OverviewScrollViewer == null) return;
        double offset = OverviewScrollViewer.VerticalOffset;

        if (offset > 240 && !_isMiniPillVisible)
        {
            _isMiniPillVisible = true;
            AnimateMiniPill(true);
        }
        else if (offset <= 190 && _isMiniPillVisible)
        {
            _isMiniPillVisible = false;
            AnimateMiniPill(false);
        }
    }

    private void AnimateMiniPill(bool show)
    {
        if (MiniWeatherPill == null || MiniPillTranslate == null) return;

        MiniWeatherPill.IsHitTestVisible = show;
        _miniPillStoryboard?.Stop();

        bool shouldAnimate = AnimationHelper.AreAnimationsEnabled &&
            !(ViewModel?.Settings.EnableBatterySaverOptimization == true && ViewModel?.IsBatterySavingActive == true);

        if (!shouldAnimate)
        {
            MiniWeatherPill.Opacity = show ? 1.0 : 0.0;
            MiniPillTranslate.Y = show ? 0 : -45;
            return;
        }

        var sb = new Storyboard();
        var fade = new DoubleAnimation
        {
            To = show ? 1.0 : 0.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(200)),
            EasingFunction = new CubicEase { EasingMode = show ? EasingMode.EaseOut : EasingMode.EaseIn }
        };
        Storyboard.SetTarget(fade, MiniWeatherPill);
        Storyboard.SetTargetProperty(fade, "Opacity");
        sb.Children.Add(fade);

        var slide = new DoubleAnimation
        {
            To = show ? 0 : -45,
            Duration = new Duration(TimeSpan.FromMilliseconds(220)),
            EasingFunction = new BackEase { Amplitude = show ? 0.2 : 0, EasingMode = show ? EasingMode.EaseOut : EasingMode.EaseIn }
        };
        Storyboard.SetTarget(slide, MiniPillTranslate);
        Storyboard.SetTargetProperty(slide, "Y");
        sb.Children.Add(slide);

        _miniPillStoryboard = sb;
        sb.Begin();
    }

    private void MiniWeatherPill_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        OverviewScrollViewer?.ChangeView(null, 0, null, false);
    }
    #endregion
}
