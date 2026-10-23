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

    private DispatcherTimer? _typewriterTimer;
    private string _fullBriefingText = string.Empty;
    private int _typewriterIndex = 0;
    private bool _hoverPhysicsInitialized = false;

    // Hiệu ứng thở hào quang Ambient Backlight Glow (v3.1.0)
    private Storyboard? _heroAmbientGlowStoryboard;

    // Con trỏ từ tính Magnetic Hover Scrubber (v3.1.0)
    private readonly List<Windows.Foundation.Point> _cachedHourlyPoints = new();
    private readonly List<HourlyForecastItem> _cachedHourlyItems = new();
    private Windows.UI.Color _cachedHourlyStrokeColor = Windows.UI.Color.FromArgb(230, 56, 189, 248);
    private int _hoveredHourlyIndex = -1;
    private Microsoft.UI.Xaml.Shapes.Line? _magneticGuideline;
    private Microsoft.UI.Xaml.Shapes.Ellipse? _magneticHalo;
    private Microsoft.UI.Xaml.Shapes.Ellipse? _magneticDot;
    private Border? _magneticTooltip;
    private TextBlock? _magneticTimeText;
    private TextBlock? _magneticValText;

    // Layout & Render Cycle Guards (v3.1.0)
    private bool _isRenderingSparkline = false;
    private bool _isRenderingSunArc = false;
    private bool _isRenderingVisualGauges = false;
    private bool _isRenderingTrendline = false;
    private bool? _wasNarrowHero = null;
    private bool? _wasNarrowMetrics = null;

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
            e.PropertyName == nameof(MainViewModel.ScrubbedSliderValue) ||
            e.PropertyName == nameof(MainViewModel.HourlyForecast) ||
            e.PropertyName == nameof(MainViewModel.CurrentSeasonalSkin) ||
            e.PropertyName == nameof(MainViewModel.EnableSeasonalParticles) ||
            e.PropertyName == nameof(MainViewModel.SeasonalGreetingText) ||
            e.PropertyName == nameof(MainViewModel.IsNothingDesign) ||
            e.PropertyName == nameof(MainViewModel.IsFluent2Design))
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (ViewModel?.IsNothingDesign == true)
                {
                    _weatherEffectRenderer?.Pause();
                    UpdateNothingThemeVisuals();
                    if (WeatherEffectsCanvas != null) WeatherEffectsCanvas.Visibility = Visibility.Collapsed;
                    if (HeroAmbientGlow != null) HeroAmbientGlow.Visibility = Visibility.Collapsed;
                }
                else
                {
                    if (WeatherEffectsCanvas != null) WeatherEffectsCanvas.Visibility = Visibility.Visible;
                    if (HeroAmbientGlow != null) HeroAmbientGlow.Visibility = Visibility.Visible;
                    _weatherEffectRenderer?.Resume();
                }
                UpdateHourlyItemsForCurrentMetric();
                UpdateHourlyMetricButtonsStyle();
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

        if (WeatherEffectsCanvas != null)
        {
            WeatherEffectsCanvas.SizeChanged += (s, e) =>
            {
                if (e.NewSize.Width > 0 && e.NewSize.Height > 0)
                {
                    WeatherEffectsCanvas.Clip = new RectangleGeometry
                    {
                        Rect = new Windows.Foundation.Rect(0, 0, e.NewSize.Width, e.NewSize.Height)
                    };
                }
            };
        }

        if (OverviewScrollViewer != null)
        {
            OverviewScrollViewer.ViewChanged += OverviewScrollViewer_ViewChanged;
        }

        this.KeyDown += (s, e) =>
        {
            if (e.Key == Windows.System.VirtualKey.Escape)
            {
                if (ViewModel?.IsComparisonOpen == true)
                {
                    ViewModel.CloseWeatherComparison();
                    e.Handled = true;
                }
                else if (ViewModel?.IsMetricDetailOpen == true)
                {
                    HideMetricDetailModal();
                    e.Handled = true;
                }
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
            UpdateHourlyItemsForCurrentMetric();
            UpdateNothingThemeVisuals();
            UpdateWeatherVisuals();
            RedrawCanvases();
            StartHeroIconAnimation();
            StartAmbientGlowAnimation();
        };

        this.Unloaded += (s, e) =>
        {
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
            StopHeroIconAnimation();
            StopAmbientGlowAnimation();
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

        this.ActualThemeChanged += (s, e) =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                UpdateNothingThemeVisuals();
                UpdateHourlyMetricButtonsStyle();
                UpdateWeatherVisuals();
                RedrawCanvases();
            });
        };

        if (SunArcCanvas != null)
        {
            SunArcCanvas.SizeChanged += (s, e) =>
            {
                if (Math.Abs(e.NewSize.Width - e.PreviousSize.Width) > 1 || Math.Abs(e.NewSize.Height - e.PreviousSize.Height) > 1)
                {
                    DispatcherQueue.TryEnqueue(RenderSunArc);
                }
            };
        }

        if (UvGaugeCanvas != null)
        {
            UvGaugeCanvas.SizeChanged += (s, e) =>
            {
                if (Math.Abs(e.NewSize.Width - e.PreviousSize.Width) > 1)
                {
                    DispatcherQueue.TryEnqueue(RenderVisualGauges);
                }
            };
        }

        if (AqiGaugeCanvas != null)
        {
            AqiGaugeCanvas.SizeChanged += (s, e) =>
            {
                if (Math.Abs(e.NewSize.Width - e.PreviousSize.Width) > 1)
                {
                    DispatcherQueue.TryEnqueue(RenderVisualGauges);
                }
            };
        }

        if (PressureSparklineCanvas != null)
        {
            PressureSparklineCanvas.SizeChanged += (s, e) =>
            {
                if (Math.Abs(e.NewSize.Width - e.PreviousSize.Width) > 1 || Math.Abs(e.NewSize.Height - e.PreviousSize.Height) > 1)
                {
                    DispatcherQueue.TryEnqueue(RenderPressureSparkline);
                }
            };
        }
    }

    public void RedrawCanvases()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            RenderHourlyTemperatureTrendline();
            RenderNothingHourlyCurve();
            RenderSunArc();
            RenderPressureSparkline();
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
            StartAmbientGlowAnimation();
        }
    }

    public void StopEffects()
    {
        _weatherEffectRenderer?.Pause();
        StopHeroIconAnimation();
        StopAmbientGlowAnimation();
    }

    public void StartBriefingTypewriter(string? text)
    {
        if (AiBriefingSummaryText == null || string.IsNullOrEmpty(text)) return;
        _fullBriefingText = text;
        _typewriterIndex = 0;
        AiBriefingSummaryText.Text = "";

        _typewriterTimer?.Stop();
        _typewriterTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(18) };
        _typewriterTimer.Tick += (s, e) =>
        {
            if (_typewriterIndex < _fullBriefingText.Length)
            {
                int charsToAdd = Math.Min(3, _fullBriefingText.Length - _typewriterIndex);
                AiBriefingSummaryText.Text += _fullBriefingText.Substring(_typewriterIndex, charsToAdd);
                _typewriterIndex += charsToAdd;
            }
            else
            {
                _typewriterTimer?.Stop();
            }
        };
        _typewriterTimer.Start();
    }

    public void UpdateWeatherVisuals()
    {
        if (ViewModel?.CurrentWeather == null) return;

        if (ViewModel.IsNothingDesign)
        {
            _weatherEffectRenderer?.Pause();
            if (WeatherEffectsCanvas != null)
            {
                WeatherEffectsCanvas.Children.Clear();
                WeatherEffectsCanvas.Visibility = Visibility.Collapsed;
            }
            if (HeroAmbientGlow != null) HeroAmbientGlow.Visibility = Visibility.Collapsed;
            if (CityHeroBackgroundBorder != null) CityHeroBackgroundBorder.Visibility = Visibility.Collapsed;
            StopAmbientGlowAnimation();
            return;
        }

        UpdateHeroCardSeasonalColors();

        // Cập nhật hiệu ứng thời tiết nền nếu người dùng bật và không bị tiết kiệm pin
        bool shouldRunEffects = ViewModel.Settings.EnableWeatherEffects &&
            !(ViewModel.Settings.EnableBatterySaverOptimization && ViewModel.IsBatterySavingActive);

        if (shouldRunEffects)
        {
            _weatherEffectRenderer?.SetWind(ViewModel.CurrentWeather.WindSpeedValue);
            _weatherEffectRenderer?.SetWeatherEffect(ViewModel.CurrentWeather.WeatherEffect);

            // Đồng bộ hiệu ứng hạt Mùa & Lễ hội (mai đào, tuyết rơi, đom đóm, lá phong...)
            if (ViewModel.CurrentSeasonalSkin != null)
            {
                _weatherEffectRenderer?.SetSeasonalParticleType(
                    ViewModel.CurrentSeasonalSkin.ParticleType, 
                    ViewModel.Settings.EnableSeasonalParticles);
            }

            _weatherEffectRenderer?.Resume();
            StartHeroIconAnimation();
            StartAmbientGlowAnimation();
        }
        else
        {
            _weatherEffectRenderer?.Pause();
            WeatherEffectsCanvas.Children.Clear();
            StopHeroIconAnimation();
            StopAmbientGlowAnimation();
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
            RenderHourlyTrendline();
        }
        catch { }
    }

    public void ScrollToHourlySection()
    {
        OverviewScrollViewer?.ChangeView(null, 650, null, false);
    }

    private void HourlyScrollViewer_PointerWheelChanged(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        try
        {
            var props = e.GetCurrentPoint(HourlyScrollViewer).Properties;
            int delta = props.MouseWheelDelta;
            if (delta != 0 && HourlyScrollViewer != null)
            {
                HourlyScrollViewer.ChangeView(HourlyScrollViewer.HorizontalOffset - delta, null, null, false);
                e.Handled = true;
            }
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
                UpdateHourlyItemsForCurrentMetric();
                RenderHourlyTrendline();
            }
        }
    }

    public void UpdateHourlyItemsForCurrentMetric()
    {
        if (ViewModel?.HourlyForecast == null) return;

        foreach (var it in ViewModel.HourlyForecast)
        {
            switch (_currentHourlyMetric)
            {
                case HourlyChartMetricMode.Temperature:
                    it.PrimaryMetricText = it.TempDisplay;
                    it.SecondaryMetricText = it.RainProbabilityText;
                    it.SecondaryIconGlyph = "\uf043"; // Giọt nước
                    it.SecondaryColor = "#0099BC";
                    break;

                case HourlyChartMetricMode.RainProbability:
                    it.PrimaryMetricText = it.RainProbabilityText;
                    it.SecondaryMetricText = it.PrecipitationText;
                    it.SecondaryIconGlyph = "\uf73d"; // Mây mưa
                    it.SecondaryColor = "#0EA5E9";
                    break;

                case HourlyChartMetricMode.WindSpeed:
                    it.PrimaryMetricText = it.WindSpeedDisplay;
                    it.SecondaryMetricText = it.WindDirectionText;
                    it.SecondaryIconGlyph = "\uf14e"; // La bàn
                    it.SecondaryColor = "#10B981";
                    break;

                case HourlyChartMetricMode.UvIndex:
                    it.PrimaryMetricText = it.UvDisplay;
                    it.SecondaryMetricText = it.UvLevelText;
                    it.SecondaryIconGlyph = "\uf185"; // Mặt trời
                    it.SecondaryColor = "#F59E0B";
                    break;
            }
        }
    }

    private void UpdateHourlyMetricButtonsStyle()
    {
        Button[] buttons = { BtnHourlyTemp, BtnHourlyRain, BtnHourlyWind, BtnHourlyUv };
        HourlyChartMetricMode[] modes = { HourlyChartMetricMode.Temperature, HourlyChartMetricMode.RainProbability, HourlyChartMetricMode.WindSpeed, HourlyChartMetricMode.UvIndex };
        bool isDark = ActualTheme != ElementTheme.Light;

        for (int i = 0; i < buttons.Length; i++)
        {
            var btn = buttons[i];
            if (btn == null) continue;
            bool isActive = modes[i] == _currentHourlyMetric;

            if (isActive)
            {
                if (isDark)
                {
                    btn.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(50, 56, 189, 248));
                    btn.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(160, 56, 189, 248));
                    btn.BorderThickness = new Thickness(1.5);
                }
                else
                {
                    // Light mode: Bold Sky-600 with white text for maximum readability
                    btn.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 2, 132, 199));
                    btn.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 3, 105, 161));
                    btn.BorderThickness = new Thickness(1.5);
                }
            }
            else
            {
                btn.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                btn.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                btn.BorderThickness = new Thickness(1);
            }

            if (btn.Content is StackPanel sp && sp.Children.Count > 1)
            {
                if (sp.Children[0] is FontIcon fi)
                {
                    if (isActive && !isDark)
                    {
                        fi.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
                    }
                    else
                    {
                        fi.Foreground = modes[i] switch
                        {
                            HourlyChartMetricMode.Temperature => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 56, 189, 248)),
                            HourlyChartMetricMode.RainProbability => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 14, 165, 233)),
                            HourlyChartMetricMode.WindSpeed => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)),
                            HourlyChartMetricMode.UvIndex => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)),
                            _ => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 56, 189, 248))
                        };
                    }
                }

                if (sp.Children[1] is TextBlock tb)
                {
                    tb.FontWeight = isActive ? Microsoft.UI.Text.FontWeights.Bold : Microsoft.UI.Text.FontWeights.Medium;
                    tb.Opacity = 1.0;

                    if (isActive)
                    {
                        tb.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
                    }
                    else
                    {
                        tb.Foreground = isDark
                            ? new SolidColorBrush(Windows.UI.Color.FromArgb(200, 148, 163, 184))
                            : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 65, 85));
                    }
                }
            }
        }
    }

        public void UpdateNothingThemeVisuals()
    {
        bool isDark = ThemeHelper.IsDarkTheme(this);
        var cardBg = isDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 18, 18, 20)) : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));
        var cardBorder = isDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 36, 36, 40)) : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 226, 226, 230));
        var subtleBg = isDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 24, 24, 27)) : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 245, 247));
        var subCardBorder = isDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 38, 38, 43)) : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 229, 229, 232));
        var glyphBoxBg = isDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 10, 10, 12)) : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 249, 249, 250));
        var textPrimary = isDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)) : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 10, 10, 12));
        var textSecondary = isDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136)) : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 102, 102, 102));
        var textMuted = isDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 82, 82, 91)) : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 161, 161, 170));
        var pillBg = isDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 31, 31, 35)) : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 234, 234, 234));
        var pillBorder = isDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 51, 51)) : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 221, 221, 221));

        // Update local resources
        this.Resources["NothingCardBackgroundBrush"] = cardBg;
        this.Resources["NothingCardBorderBrush"] = cardBorder;
        this.Resources["NothingSubCardBackgroundBrush"] = subtleBg;
        this.Resources["NothingSubCardBorderBrush"] = subCardBorder;
        this.Resources["NothingGlyphBoxBackgroundBrush"] = glyphBoxBg;
        this.Resources["NothingTextPrimaryBrush"] = textPrimary;
        this.Resources["NothingTextSecondaryBrush"] = textSecondary;
        this.Resources["NothingTextMutedBrush"] = textMuted;
        this.Resources["NothingPillBackgroundBrush"] = pillBg;
        this.Resources["NothingPillBorderBrush"] = pillBorder;

        // Update outer card containers
        if (NothingHeaderBorder != null) { NothingHeaderBorder.Background = cardBg; NothingHeaderBorder.BorderBrush = cardBorder; }
        if (NothingHeroBorder != null) { NothingHeroBorder.Background = cardBg; NothingHeroBorder.BorderBrush = cardBorder; }
        if (NothingGlyphBoxBorder != null) { NothingGlyphBoxBorder.Background = glyphBoxBg; NothingGlyphBoxBorder.BorderBrush = subCardBorder; }
        if (NothingAqiBorder != null) { NothingAqiBorder.Background = cardBg; NothingAqiBorder.BorderBrush = cardBorder; }
        if (NothingSunsetBorder != null) { NothingSunsetBorder.Background = cardBg; NothingSunsetBorder.BorderBrush = cardBorder; }
        if (NothingNowcastBorder != null) { NothingNowcastBorder.Background = cardBg; NothingNowcastBorder.BorderBrush = cardBorder; }
        if (NothingNowcastHeadlineText != null) { NothingNowcastHeadlineText.Foreground = textPrimary; }
        if (NothingNowcastSubText != null) { NothingNowcastSubText.Foreground = textSecondary; }
        if (NothingHourlyBorder != null)
        {
            NothingHourlyBorder.Background = cardBg;
            NothingHourlyBorder.BorderBrush = cardBorder;
            NothingHourlyBorder.Resources["NothingSubCardBackgroundBrush"] = subtleBg;
            NothingHourlyBorder.Resources["NothingSubCardBorderBrush"] = subCardBorder;
            NothingHourlyBorder.Resources["NothingTextPrimaryBrush"] = textPrimary;
            NothingHourlyBorder.Resources["NothingTextSecondaryBrush"] = textSecondary;
            NothingHourlyBorder.Resources["NothingTextMutedBrush"] = textMuted;
        }
        if (NothingDailyBorder != null)
        {
            NothingDailyBorder.Background = cardBg;
            NothingDailyBorder.BorderBrush = cardBorder;
            NothingDailyBorder.Resources["NothingSubCardBackgroundBrush"] = subtleBg;
            NothingDailyBorder.Resources["NothingSubCardBorderBrush"] = subCardBorder;
            NothingDailyBorder.Resources["NothingTextPrimaryBrush"] = textPrimary;
            NothingDailyBorder.Resources["NothingTextSecondaryBrush"] = textSecondary;
            NothingDailyBorder.Resources["NothingTextMutedBrush"] = textMuted;
        }

        // Re-template dynamic items so they pick up the fresh theme resources!
        if (NothingHourlyItemsControl != null && ViewModel != null)
        {
            var hourly = ViewModel.HourlyForecast;
            NothingHourlyItemsControl.ItemsSource = null;
            NothingHourlyItemsControl.ItemsSource = hourly;
        }
        if (NothingDailyItemsControl != null && ViewModel != null)
        {
            var daily = ViewModel.DailyForecast;
            NothingDailyItemsControl.ItemsSource = null;
            NothingDailyItemsControl.ItemsSource = daily;
        }

        // Mini compact weather pill
        if (MiniWeatherPill != null)
        {
            MiniWeatherPill.Background = cardBg;
            MiniWeatherPill.BorderBrush = cardBorder;
        }

        if (NothingHeroIcon != null && ViewModel?.CurrentWeather != null)
        {
            if (App.Current.Resources.TryGetValue("NdotFontFamily", out var nf) && nf is Microsoft.UI.Xaml.Media.FontFamily ndot)
            {
                NothingHeroIcon.FontFamily = ndot;
            }
            if (!string.IsNullOrEmpty(ViewModel.CurrentWeather.NothingGlyph))
            {
                NothingHeroIcon.Glyph = ViewModel.CurrentWeather.NothingGlyph;
            }
        }

        // Update static pills & borders
        if (NothingHeaderPill1 != null) { NothingHeaderPill1.Background = pillBg; NothingHeaderPill1.BorderBrush = pillBorder; }
        if (NothingHeaderStationSub != null) { NothingHeaderStationSub.Foreground = textSecondary; }
        if (NothingHeaderPill2 != null) { NothingHeaderPill2.Background = pillBg; NothingHeaderPill2.BorderBrush = pillBorder; }
        if (NothingHeaderCoordSub != null) { NothingHeaderCoordSub.Foreground = textSecondary; }
        if (NothingHeaderTimePill != null) { NothingHeaderTimePill.Background = pillBg; NothingHeaderTimePill.BorderBrush = pillBorder; }
        if (NothingHeaderTimeVal != null) { NothingHeaderTimeVal.Foreground = textPrimary; }
        if (NothingHeroSensorPill != null) { NothingHeroSensorPill.Background = pillBg; NothingHeroSensorPill.BorderBrush = pillBorder; }
        if (NothingHeroFeelsLikePill != null) { NothingHeroFeelsLikePill.Background = pillBg; NothingHeroFeelsLikePill.BorderBrush = pillBorder; }
        if (NothingHeroConditionPill != null) { NothingHeroConditionPill.Background = pillBg; }
        if (NothingAqiPm25Pill != null) { NothingAqiPm25Pill.Background = pillBg; NothingAqiPm25Pill.BorderBrush = pillBorder; }

        // Update dividing separator borders
        if (NothingHeroMetricsSeparator != null) { NothingHeroMetricsSeparator.BorderBrush = subCardBorder; }
        if (NothingAqiAdviceSeparator != null) { NothingAqiAdviceSeparator.BorderBrush = subCardBorder; }
        if (NothingSunProgressSeparator != null) { NothingSunProgressSeparator.BorderBrush = subCardBorder; }

        // Update static typography
        if (NothingStationTitle != null) { NothingStationTitle.Foreground = textPrimary; }
        if (NothingTempNumber != null) { NothingTempNumber.Foreground = textPrimary; }
        if (NothingFeelsLikeValue != null) { NothingFeelsLikeValue.Foreground = textPrimary; }
        if (NothingHighLowText != null) { NothingHighLowText.Foreground = textSecondary; }
        if (NothingHeroIcon != null) { NothingHeroIcon.Foreground = textPrimary; }
        if (NothingConditionTitle != null) { NothingConditionTitle.Foreground = textPrimary; }

        // Telemetry strip labels & values
        if (NothingHumidityVal != null) { NothingHumidityVal.Foreground = textPrimary; }
        if (NothingHumidityProgress != null) { NothingHumidityProgress.Foreground = textPrimary; NothingHumidityProgress.Background = subCardBorder; }
        if (NothingWindVal != null) { NothingWindVal.Foreground = textPrimary; }
        if (NothingWindProgress != null) { NothingWindProgress.Foreground = textPrimary; NothingWindProgress.Background = subCardBorder; }
        if (NothingUvProgress != null) { NothingUvProgress.Foreground = textPrimary; }
        if (NothingPressureVal != null) { NothingPressureVal.Foreground = textPrimary; }
        if (NothingBarometerProgress != null) { NothingBarometerProgress.Foreground = textPrimary; NothingBarometerProgress.Background = subCardBorder; }

        // AQI Card
        if (NothingAqiVal != null) { NothingAqiVal.Foreground = textPrimary; }
        if (NothingAqiDot != null) { NothingAqiDot.Fill = textPrimary; }
        if (NothingAqiPm25Val != null) { NothingAqiPm25Val.Foreground = textPrimary; }
        if (NothingAqiAdviceText != null) { NothingAqiAdviceText.Foreground = textSecondary; }
        if (NothingAqiGlyphBar1 != null) { NothingAqiGlyphBar1.Background = textPrimary; }
        if (NothingAqiGlyphBar2 != null) { NothingAqiGlyphBar2.Background = textPrimary; }
        if (NothingAqiGlyphBar3 != null) { NothingAqiGlyphBar3.Background = textPrimary; }
        if (NothingAqiGlyphBar4 != null) { NothingAqiGlyphBar4.Background = textPrimary; }
        if (NothingAqiGlyphBar5 != null) { NothingAqiGlyphBar5.Background = textPrimary; }

        // Sunset Card
        if (NothingSunsetValue != null) { NothingSunsetValue.Foreground = textPrimary; }
        if (NothingSunriseValue != null) { NothingSunriseValue.Foreground = textSecondary; }
        if (NothingSunriseSubValue != null) { NothingSunriseSubValue.Foreground = textSecondary; }
        if (NothingSunsetSubValue != null) { NothingSunsetSubValue.Foreground = textSecondary; }
        if (NothingCompassNeedle != null) { NothingCompassNeedle.Background = textPrimary; }
        if (NothingCompassDial != null) { NothingCompassDial.BorderBrush = textSecondary; }
        if (NothingSunProgressBar != null) { NothingSunProgressBar.Background = subCardBorder; }
        RenderNothingHourlyCurve();
    }

    public void RenderHourlyTemperatureTrendline() => RenderHourlyTrendline();

    private double GetItemValue(HourlyForecastItem it) => _currentHourlyMetric switch
    {
        HourlyChartMetricMode.Temperature => it.TempValue,
        HourlyChartMetricMode.RainProbability => (double)it.RainProbabilityValue,
        HourlyChartMetricMode.WindSpeed => it.WindSpeedValue,
        HourlyChartMetricMode.UvIndex => it.UvValue,
        _ => it.TempValue
    };

    private string GetItemDisplay(HourlyForecastItem it) => _currentHourlyMetric switch
    {
        HourlyChartMetricMode.Temperature => it.TempDisplay,
        HourlyChartMetricMode.RainProbability => it.RainProbabilityText,
        HourlyChartMetricMode.WindSpeed => it.WindSpeedDisplay,
        HourlyChartMetricMode.UvIndex => it.UvDisplay,
        _ => it.TempDisplay
    };

    public void RenderHourlyTrendline()
    {
        if (_isRenderingTrendline) return;
        try
        {
            _isRenderingTrendline = true;
            if (HourlyTrendlineCanvas == null || ViewModel?.HourlyForecast == null || ViewModel.HourlyForecast.Count == 0)
                return;

            HourlyTrendlineCanvas.Children.Clear();

            var items = ViewModel.HourlyForecast.ToList();
            int count = items.Count;
            if (count < 2) return;

            double colWidth = 92.0;
            double cardWidth = 82.0;
            double totalWidth = count * colWidth;
            if (Math.Abs(HourlyTrendlineCanvas.Width - totalWidth) > 0.5)
            {
                HourlyTrendlineCanvas.Width = totalWidth;
            }

            double canvasHeight = 80.0;
            if (Math.Abs(HourlyTrendlineCanvas.Height - canvasHeight) > 0.5)
            {
                HourlyTrendlineCanvas.Height = canvasHeight;
            }

            bool isDark = ActualTheme != ElementTheme.Light;
            var skin = ViewModel?.CurrentSeasonalSkin;

            // Bảng màu stroke & fill tương ứng thích ứng Dark / Light Mode & Theme mùa/lễ hội
            Windows.UI.Color strokeColor, fillStartColor, fillEndColor;

            if (_currentHourlyMetric == HourlyChartMetricMode.Temperature && skin != null)
            {
                var baseStroke = ParseHexColor(skin.HourlyCurveStrokeHex);
                strokeColor = isDark
                    ? Windows.UI.Color.FromArgb(240, baseStroke.R, baseStroke.G, baseStroke.B)
                    : Windows.UI.Color.FromArgb(255, (byte)Math.Max(0, baseStroke.R - 25), (byte)Math.Max(0, baseStroke.G - 25), (byte)Math.Max(0, baseStroke.B - 25));
                fillStartColor = ParseHexColor(skin.HourlyGradientTopHex);
                fillEndColor = ParseHexColor(skin.HourlyGradientBottomHex);
            }
            else
            {
                (strokeColor, fillStartColor, fillEndColor) = _currentHourlyMetric switch
                {
                    HourlyChartMetricMode.Temperature => isDark
                        ? (Windows.UI.Color.FromArgb(230, 56, 189, 248),
                           Windows.UI.Color.FromArgb(90, 56, 189, 248),
                           Windows.UI.Color.FromArgb(10, 56, 189, 248))
                        : (Windows.UI.Color.FromArgb(255, 2, 132, 199),  // Sky-600
                           Windows.UI.Color.FromArgb(50, 2, 132, 199),
                           Windows.UI.Color.FromArgb(5, 2, 132, 199)),
                    HourlyChartMetricMode.RainProbability => isDark
                        ? (Windows.UI.Color.FromArgb(240, 14, 165, 233),
                           Windows.UI.Color.FromArgb(100, 14, 165, 233),
                           Windows.UI.Color.FromArgb(12, 14, 165, 233))
                        : (Windows.UI.Color.FromArgb(255, 14, 165, 233),
                           Windows.UI.Color.FromArgb(50, 14, 165, 233),
                           Windows.UI.Color.FromArgb(5, 14, 165, 233)),
                    HourlyChartMetricMode.WindSpeed => isDark
                        ? (Windows.UI.Color.FromArgb(240, 16, 185, 129),
                           Windows.UI.Color.FromArgb(100, 16, 185, 129),
                           Windows.UI.Color.FromArgb(12, 16, 185, 129))
                        : (Windows.UI.Color.FromArgb(255, 5, 150, 105),  // Emerald-600
                           Windows.UI.Color.FromArgb(50, 5, 150, 105),
                           Windows.UI.Color.FromArgb(5, 5, 150, 105)),
                    HourlyChartMetricMode.UvIndex => isDark
                        ? (Windows.UI.Color.FromArgb(240, 245, 158, 11),
                           Windows.UI.Color.FromArgb(100, 245, 158, 11),
                           Windows.UI.Color.FromArgb(12, 245, 158, 11))
                        : (Windows.UI.Color.FromArgb(255, 217, 119, 6),  // Amber-600
                           Windows.UI.Color.FromArgb(50, 217, 119, 6),
                           Windows.UI.Color.FromArgb(5, 217, 119, 6)),
                    _ => (
                        Windows.UI.Color.FromArgb(230, 56, 189, 248),
                        Windows.UI.Color.FromArgb(90, 56, 189, 248),
                        Windows.UI.Color.FromArgb(10, 56, 189, 248))
                };
            }

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

            double topPadding = 26.0;
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
                fillFigure.Segments.Add(new BezierSegment { Point1 = cp1, Point2 = cp2, Point3 = p2 });
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
            foreach (double gy in new[] { topPadding, topPadding + usableHeight * 0.5, topPadding + usableHeight })
            {
                var guideLine = new Microsoft.UI.Xaml.Shapes.Line
                {
                    X1 = 12,
                    Y1 = gy,
                    X2 = totalWidth - 12,
                    Y2 = gy,
                    Stroke = new SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(26, 255, 255, 255) : Windows.UI.Color.FromArgb(26, 15, 23, 42)),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 2, 4 },
                    IsHitTestVisible = false
                };
                HourlyTrendlineCanvas.Children.Add(guideLine);
            }
            HourlyTrendlineCanvas.Children.Add(areaFill);

            var strokeGeo = new PathGeometry();
            strokeGeo.Figures.Add(strokeFigure);

            var strokePath = new Microsoft.UI.Xaml.Shapes.Path
            {
                Data = strokeGeo,
                Stroke = new SolidColorBrush(strokeColor),
                StrokeThickness = isDark ? 2.5 : 2.8
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
                            ? (isDark ? Windows.UI.Color.FromArgb(120, 14, 165, 233) : Windows.UI.Color.FromArgb(160, 2, 132, 199))
                            : (isDark ? Windows.UI.Color.FromArgb(65, 0, 153, 188) : Windows.UI.Color.FromArgb(100, 2, 132, 199)))
                    };
                    Canvas.SetLeft(rainBar, pt.X - 6);
                    Canvas.SetTop(rainBar, canvasHeight - barH);
                    HourlyTrendlineCanvas.Children.Add(rainBar);
                }

                bool isNow = items[i].IsNow || i == 0;
                bool isItemSel = items[i].IsSelected;

                var dot = new Microsoft.UI.Xaml.Shapes.Ellipse
                {
                    Width = isItemSel ? 10 : (isNow ? 8 : 6),
                    Height = isItemSel ? 10 : (isNow ? 8 : 6),
                    Fill = isItemSel
                        ? new SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 56, 189, 248) : Windows.UI.Color.FromArgb(255, 2, 132, 199))
                        : (isNow
                            ? new SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 245, 158, 11) : Windows.UI.Color.FromArgb(255, 180, 83, 9))
                            : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255))),
                    Stroke = new SolidColorBrush(strokeColor),
                    StrokeThickness = isItemSel ? 2.5 : (isDark ? 2.0 : 2.2)
                };
                double dotOffset = (isItemSel ? 10 : (isNow ? 8 : 6)) / 2.0;
                Canvas.SetLeft(dot, pt.X - dotOffset);
                Canvas.SetTop(dot, pt.Y - dotOffset);
                HourlyTrendlineCanvas.Children.Add(dot);

                var label = new TextBlock
                {
                    Text = GetItemDisplay(items[i]),
                    FontSize = isItemSel ? 11.5 : (isNow ? 11.0 : 10.0),
                    FontWeight = (isItemSel || isNow) ? Microsoft.UI.Text.FontWeights.Bold : Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = isItemSel
                        ? new SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 56, 189, 248) : Windows.UI.Color.FromArgb(255, 2, 132, 199))
                        : (isNow
                            ? new SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 245, 158, 11) : Windows.UI.Color.FromArgb(255, 180, 83, 9))
                            : new SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(235, 255, 255, 255) : Windows.UI.Color.FromArgb(255, 15, 23, 42))),
                    TextAlignment = TextAlignment.Center,
                    Width = 60
                };
                Canvas.SetLeft(label, pt.X - 30);
                Canvas.SetTop(label, pt.Y - 20);
                HourlyTrendlineCanvas.Children.Add(label);
            }

            // Vạch kim thẳng đứng phát sáng nếu người dùng đang tua thời gian
            if (ViewModel?.IsTimeScrubbingActive == true)
            {
                int scrubbedIdx = items.FindIndex(x => x.IsSelected);
                if (scrubbedIdx < 0)
                {
                    int scrubbedHour = (int)Math.Round(ViewModel.ScrubbedSliderValue);
                    scrubbedIdx = items.FindIndex(x => x.HourNumber == scrubbedHour);
                }
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
                        Width = 18,
                        Height = 18,
                        Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(80, strokeColor.R, strokeColor.G, strokeColor.B))
                    };
                    Canvas.SetLeft(halo, targetPt.X - 9);
                    Canvas.SetTop(halo, targetPt.Y - 9);
                    HourlyTrendlineCanvas.Children.Add(halo);
                }
            }

            // Cập nhật bộ đệm cho con trỏ từ tính Magnetic Hover Scrubber (v3.1.0)
            _cachedHourlyPoints.Clear();
            _cachedHourlyPoints.AddRange(points);
            _cachedHourlyItems.Clear();
            _cachedHourlyItems.AddRange(items);
            _cachedHourlyStrokeColor = strokeColor;

            EnsureMagneticElementsCreated();
            UpdateMagneticScrubberOverlay();
        }
        catch { }
        finally
        {
            _isRenderingTrendline = false;
        }
    }

    private void EnsureMagneticElementsCreated()
    {
        if (HourlyTrendlineCanvas == null) return;

        if (_magneticGuideline == null)
        {
            _magneticGuideline = new Microsoft.UI.Xaml.Shapes.Line
            {
                Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(200, 56, 189, 248)),
                StrokeThickness = 1.5,
                StrokeDashArray = new DoubleCollection { 3, 2 },
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false
            };
        }

        if (_magneticHalo == null)
        {
            _magneticHalo = new Microsoft.UI.Xaml.Shapes.Ellipse
            {
                Width = 22,
                Height = 22,
                Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(70, 56, 189, 248)),
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false
            };
        }

        if (_magneticDot == null)
        {
            _magneticDot = new Microsoft.UI.Xaml.Shapes.Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
                Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 56, 189, 248)),
                StrokeThickness = 2,
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false
            };
        }

        if (_magneticTooltip == null)
        {
            _magneticTimeText = new TextBlock
            {
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 56, 189, 248))
            };

            var dotText = new TextBlock
            {
                Text = "•",
                FontSize = 11,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(120, 255, 255, 255)),
                Margin = new Thickness(4, 0, 4, 0)
            };

            _magneticValText = new TextBlock
            {
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255))
            };

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            panel.Children.Add(_magneticTimeText);
            panel.Children.Add(dotText);
            panel.Children.Add(_magneticValText);

            _magneticTooltip = new Border
            {
                CornerRadius = new CornerRadius(7),
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(230, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 56, 189, 248)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 3, 8, 3),
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false
            };
            _magneticTooltip.Child = panel;
        }

        if (!HourlyTrendlineCanvas.Children.Contains(_magneticGuideline))
            HourlyTrendlineCanvas.Children.Add(_magneticGuideline);
        if (!HourlyTrendlineCanvas.Children.Contains(_magneticHalo))
            HourlyTrendlineCanvas.Children.Add(_magneticHalo);
        if (!HourlyTrendlineCanvas.Children.Contains(_magneticDot))
            HourlyTrendlineCanvas.Children.Add(_magneticDot);
        if (!HourlyTrendlineCanvas.Children.Contains(_magneticTooltip))
            HourlyTrendlineCanvas.Children.Add(_magneticTooltip);
    }

    private void UpdateMagneticScrubberOverlay()
    {
        if (HourlyTrendlineCanvas == null || _magneticGuideline == null || _magneticHalo == null ||
            _magneticDot == null || _magneticTooltip == null) return;

        if (_hoveredHourlyIndex >= 0 && _hoveredHourlyIndex < _cachedHourlyPoints.Count &&
            _hoveredHourlyIndex < _cachedHourlyItems.Count)
        {
            var pt = _cachedHourlyPoints[_hoveredHourlyIndex];
            var item = _cachedHourlyItems[_hoveredHourlyIndex];
            double canvasH = HourlyTrendlineCanvas.ActualHeight > 0 ? HourlyTrendlineCanvas.ActualHeight : 80;

            _magneticGuideline.X1 = pt.X;
            _magneticGuideline.Y1 = 4;
            _magneticGuideline.X2 = pt.X;
            _magneticGuideline.Y2 = canvasH - 4;
            _magneticGuideline.Stroke = new SolidColorBrush(_cachedHourlyStrokeColor);
            _magneticGuideline.Visibility = Visibility.Visible;

            _magneticHalo.Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(80, _cachedHourlyStrokeColor.R, _cachedHourlyStrokeColor.G, _cachedHourlyStrokeColor.B));
            Canvas.SetLeft(_magneticHalo, pt.X - 11);
            Canvas.SetTop(_magneticHalo, pt.Y - 11);
            _magneticHalo.Visibility = Visibility.Visible;

            _magneticDot.Stroke = new SolidColorBrush(_cachedHourlyStrokeColor);
            Canvas.SetLeft(_magneticDot, pt.X - 4);
            Canvas.SetTop(_magneticDot, pt.Y - 4);
            _magneticDot.Visibility = Visibility.Visible;

            if (_magneticTimeText != null) _magneticTimeText.Text = item.TimeDisplay;
            if (_magneticValText != null) _magneticValText.Text = GetItemDisplay(item);

            double tipTop = (pt.Y > 34) ? (pt.Y - 32) : (pt.Y + 12);
            double tipLeft = Math.Max(2, pt.X - 35);
            Canvas.SetLeft(_magneticTooltip, tipLeft);
            Canvas.SetTop(_magneticTooltip, tipTop);
            _magneticTooltip.Visibility = Visibility.Visible;
        }
        else
        {
            _magneticGuideline.Visibility = Visibility.Collapsed;
            _magneticHalo.Visibility = Visibility.Collapsed;
            _magneticDot.Visibility = Visibility.Collapsed;
            _magneticTooltip.Visibility = Visibility.Collapsed;
        }
    }

    private void HourlyTrendlineCanvas_PointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_cachedHourlyPoints.Count == 0 || HourlyTrendlineCanvas == null) return;
        var pos = e.GetCurrentPoint(HourlyTrendlineCanvas).Position;

        int closestIdx = -1;
        double minDistance = double.MaxValue;
        for (int i = 0; i < _cachedHourlyPoints.Count; i++)
        {
            double d = Math.Abs(_cachedHourlyPoints[i].X - pos.X);
            if (d < minDistance)
            {
                minDistance = d;
                closestIdx = i;
            }
        }

        if (closestIdx != _hoveredHourlyIndex)
        {
            _hoveredHourlyIndex = closestIdx;
            UpdateMagneticScrubberOverlay();
        }
    }

    private void HourlyTrendlineCanvas_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _hoveredHourlyIndex = -1;
        UpdateMagneticScrubberOverlay();
    }

    private void HourlyTrendlineCanvas_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_hoveredHourlyIndex >= 0 && _hoveredHourlyIndex < _cachedHourlyItems.Count)
        {
            var item = _cachedHourlyItems[_hoveredHourlyIndex];
            ViewModel?.ScrubToItem(item);
        }
    }

    // ---------------------------------------------------------------------------------
    // v3.6.0: NOTHING OS DOT-MATRIX 24-HOUR INTERACTIVE TELEMETRY CURVE SCRUBBER
    // ---------------------------------------------------------------------------------
    private readonly List<Windows.Foundation.Point> _nothingCurvePoints = new();
    private readonly List<HourlyForecastItem> _nothingCurveItems = new();
    private int _nothingHoveredIndex = -1;
    private Microsoft.UI.Xaml.Shapes.Line? _nothingHoverNeedle;
    private Microsoft.UI.Xaml.Shapes.Ellipse? _nothingHoverRing;
    private Microsoft.UI.Xaml.Shapes.Ellipse? _nothingHoverDot;

    public void RenderNothingHourlyCurve()
    {
        if (NothingHourlyCurveCanvas == null || ViewModel?.HourlyForecast == null || ViewModel.HourlyForecast.Count == 0)
            return;

        try
        {
            NothingHourlyCurveCanvas.Children.Clear();
            _nothingCurvePoints.Clear();
            _nothingCurveItems.Clear();

            var items = ViewModel.HourlyForecast.ToList();
            int count = items.Count;
            if (count == 0) return;

            const double cardWidth = 96.0;
            const double cardSpacing = 12.0;
            double totalWidth = count * cardWidth + (count - 1) * cardSpacing;
            double canvasHeight = 72.0;

            NothingHourlyCurveCanvas.Width = totalWidth;
            NothingHourlyCurveCanvas.Height = canvasHeight;

            bool isDark = ThemeHelper.IsDarkTheme(this);
            var primaryColor = isDark ? Windows.UI.Color.FromArgb(255, 255, 255, 255) : Windows.UI.Color.FromArgb(255, 17, 17, 17);
            var redColor = Windows.UI.Color.FromArgb(255, 215, 25, 33);
            var gridColor = isDark ? Windows.UI.Color.FromArgb(30, 255, 255, 255) : Windows.UI.Color.FromArgb(30, 0, 0, 0);

            // Subtle technical reference lines
            foreach (double gy in new[] { 18.0, 38.0, 58.0 })
            {
                NothingHourlyCurveCanvas.Children.Add(new Microsoft.UI.Xaml.Shapes.Line
                {
                    X1 = 0, Y1 = gy, X2 = totalWidth, Y2 = gy,
                    Stroke = new SolidColorBrush(gridColor),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 2, 4 }
                });
            }

            var temps = items.Select(x => x.TempValue).ToList();
            double minT = temps.Min();
            double maxT = temps.Max();
            double range = Math.Max(2.0, maxT - minT);

            for (int i = 0; i < count; i++)
            {
                double x = i * (cardWidth + cardSpacing) + (cardWidth / 2.0);
                double norm = (temps[i] - minT) / range;
                double y = 56.0 - norm * 36.0;
                _nothingCurvePoints.Add(new Windows.Foundation.Point(x, y));
                _nothingCurveItems.Add(items[i]);
            }

            // Smooth technical bezier spline
            if (_nothingCurvePoints.Count > 1)
            {
                var pathGeo = new PathGeometry();
                var fig = new PathFigure { StartPoint = _nothingCurvePoints[0], IsClosed = false };
                for (int i = 0; i < _nothingCurvePoints.Count - 1; i++)
                {
                    var p0 = _nothingCurvePoints[i];
                    var p1 = _nothingCurvePoints[i + 1];
                    double midX = (p0.X + p1.X) / 2.0;
                    fig.Segments.Add(new BezierSegment
                    {
                        Point1 = new Windows.Foundation.Point(midX, p0.Y),
                        Point2 = new Windows.Foundation.Point(midX, p1.Y),
                        Point3 = p1
                    });
                }
                pathGeo.Figures.Add(fig);

                NothingHourlyCurveCanvas.Children.Add(new Microsoft.UI.Xaml.Shapes.Path
                {
                    Data = pathGeo,
                    Stroke = new SolidColorBrush(redColor),
                    StrokeThickness = 1.8,
                    StrokeDashArray = new DoubleCollection { 3, 2 }
                });
            }

            // Matrix nodes + temperature labels
            for (int i = 0; i < _nothingCurvePoints.Count; i++)
            {
                var pt = _nothingCurvePoints[i];
                bool isNow = items[i].IsNow;

                var node = new Microsoft.UI.Xaml.Shapes.Ellipse
                {
                    Width = isNow ? 9 : 6,
                    Height = isNow ? 9 : 6,
                    Fill = new SolidColorBrush(isNow ? redColor : primaryColor),
                    Stroke = new SolidColorBrush(redColor),
                    StrokeThickness = isNow ? 2.0 : 1.2
                };
                double r = (isNow ? 9 : 6) / 2.0;
                Canvas.SetLeft(node, pt.X - r);
                Canvas.SetTop(node, pt.Y - r);
                NothingHourlyCurveCanvas.Children.Add(node);

                var lbl = new TextBlock
                {
                    Text = items[i].TempDisplay,
                    FontSize = 10,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    FontFamily = new FontFamily("Cascadia Mono, Consolas, Segoe UI"),
                    Foreground = new SolidColorBrush(isNow ? redColor : primaryColor),
                    TextAlignment = TextAlignment.Center,
                    Width = 48
                };
                Canvas.SetLeft(lbl, pt.X - 24);
                Canvas.SetTop(lbl, Math.Max(1, pt.Y - 17));
                NothingHourlyCurveCanvas.Children.Add(lbl);
            }

            // Create hover crosshair elements
            _nothingHoverNeedle = new Microsoft.UI.Xaml.Shapes.Line
            {
                Stroke = new SolidColorBrush(redColor),
                StrokeThickness = 1.5,
                StrokeDashArray = new DoubleCollection { 2, 2 },
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false
            };
            _nothingHoverRing = new Microsoft.UI.Xaml.Shapes.Ellipse
            {
                Width = 18, Height = 18,
                Stroke = new SolidColorBrush(redColor),
                StrokeThickness = 1.5,
                Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(45, 215, 25, 33)),
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false
            };
            _nothingHoverDot = new Microsoft.UI.Xaml.Shapes.Ellipse
            {
                Width = 7, Height = 7,
                Fill = new SolidColorBrush(redColor),
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false
            };

            NothingHourlyCurveCanvas.Children.Add(_nothingHoverNeedle);
            NothingHourlyCurveCanvas.Children.Add(_nothingHoverRing);
            NothingHourlyCurveCanvas.Children.Add(_nothingHoverDot);
        }
        catch { }
    }

    private void NothingHourlyCurveCanvas_PointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_nothingCurvePoints.Count == 0 || NothingHourlyCurveCanvas == null) return;
        var pos = e.GetCurrentPoint(NothingHourlyCurveCanvas).Position;

        int closest = -1;
        double bestDist = double.MaxValue;
        for (int i = 0; i < _nothingCurvePoints.Count; i++)
        {
            double d = Math.Abs(_nothingCurvePoints[i].X - pos.X);
            if (d < bestDist)
            {
                bestDist = d;
                closest = i;
            }
        }

        if (closest >= 0 && closest < _nothingCurvePoints.Count && closest != _nothingHoveredIndex)
        {
            _nothingHoveredIndex = closest;
            var pt = _nothingCurvePoints[closest];
            var item = _nothingCurveItems[closest];

            if (_nothingHoverNeedle != null)
            {
                _nothingHoverNeedle.X1 = pt.X;
                _nothingHoverNeedle.Y1 = 2;
                _nothingHoverNeedle.X2 = pt.X;
                _nothingHoverNeedle.Y2 = 70;
                _nothingHoverNeedle.Visibility = Visibility.Visible;
            }
            if (_nothingHoverRing != null)
            {
                Canvas.SetLeft(_nothingHoverRing, pt.X - 9);
                Canvas.SetTop(_nothingHoverRing, pt.Y - 9);
                _nothingHoverRing.Visibility = Visibility.Visible;
            }
            if (_nothingHoverDot != null)
            {
                Canvas.SetLeft(_nothingHoverDot, pt.X - 3.5);
                Canvas.SetTop(_nothingHoverDot, pt.Y - 3.5);
                _nothingHoverDot.Visibility = Visibility.Visible;
            }
            if (NothingCurveHoverPill != null && NothingCurveHoverText != null)
            {
                NothingCurveHoverText.Text = $"{item.TimeDisplay} // {item.TempDisplay} // RAIN {item.RainProbabilityText} // WIND {item.WindSpeedDisplay}";
                NothingCurveHoverPill.Visibility = Visibility.Visible;
            }
        }
    }

    private void NothingHourlyCurveCanvas_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _nothingHoveredIndex = -1;
        if (_nothingHoverNeedle != null) _nothingHoverNeedle.Visibility = Visibility.Collapsed;
        if (_nothingHoverRing != null) _nothingHoverRing.Visibility = Visibility.Collapsed;
        if (_nothingHoverDot != null) _nothingHoverDot.Visibility = Visibility.Collapsed;
        if (NothingCurveHoverPill != null) NothingCurveHoverPill.Visibility = Visibility.Collapsed;
    }

    private void NothingHourlyCurveCanvas_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_nothingHoveredIndex >= 0 && _nothingHoveredIndex < _nothingCurveItems.Count)
        {
            ViewModel?.ScrubToItem(_nothingCurveItems[_nothingHoveredIndex]);
        }
    }

    private void RenderPressureSparkline()
    {
        if (_isRenderingSparkline) return;
        if (PressureSparklineCanvas == null || ViewModel?.HourlyForecast == null) return;

        double width = PressureSparklineCanvas.ActualWidth;
        double height = PressureSparklineCanvas.ActualHeight;
        if (width <= 20 || height <= 6) return;

        var items = ViewModel.HourlyForecast.Take(6).ToList();
        if (items.Count < 2) return;

        try
        {
            _isRenderingSparkline = true;
            PressureSparklineCanvas.Children.Clear();

            var vals = items.Select(x => x.SurfacePressure > 0 ? x.SurfacePressure : 1013.25).ToList();
            double min = vals.Min();
            double max = vals.Max();
            double range = Math.Max(2.0, max - min);

            double padTop = 3.0;
            double padBottom = 3.0;
            double usableH = height - padTop - padBottom;

            var pts = new List<Windows.Foundation.Point>();
            for (int i = 0; i < vals.Count; i++)
            {
                double x = i * (width - 12) / (vals.Count - 1) + 6;
                double norm = (vals[i] - min) / range;
                double y = padTop + (1.0 - norm) * usableH;
                pts.Add(new Windows.Foundation.Point(x, y));
            }

            // Path diện tích gradient mờ dưới biểu đồ
            var fillGeo = new PathGeometry();
            var fillFig = new PathFigure { StartPoint = new Windows.Foundation.Point(pts[0].X, height), IsClosed = true };
            fillFig.Segments.Add(new LineSegment { Point = pts[0] });
            for (int i = 0; i < pts.Count - 1; i++)
            {
                var midX = (pts[i].X + pts[i + 1].X) / 2.0;
                fillFig.Segments.Add(new BezierSegment
                {
                    Point1 = new Windows.Foundation.Point(midX, pts[i].Y),
                    Point2 = new Windows.Foundation.Point(midX, pts[i + 1].Y),
                    Point3 = pts[i + 1]
                });
            }
            fillFig.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(pts[^1].X, height) });
            fillGeo.Figures.Add(fillFig);

            bool isDark = ActualTheme != ElementTheme.Light;
            var fillStartColor = isDark ? Windows.UI.Color.FromArgb(90, 167, 139, 250) : Windows.UI.Color.FromArgb(50, 124, 58, 237);
            var fillEndColor = isDark ? Windows.UI.Color.FromArgb(5, 167, 139, 250) : Windows.UI.Color.FromArgb(5, 124, 58, 237);
            var strokeColor = isDark ? Windows.UI.Color.FromArgb(230, 192, 132, 252) : Windows.UI.Color.FromArgb(255, 124, 58, 237);
            var dotStroke = isDark ? Windows.UI.Color.FromArgb(255, 167, 139, 250) : Windows.UI.Color.FromArgb(255, 124, 58, 237);

            var fillBrush = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(0, 1)
            };
            fillBrush.GradientStops.Add(new GradientStop { Color = fillStartColor, Offset = 0 });
            fillBrush.GradientStops.Add(new GradientStop { Color = fillEndColor, Offset = 1 });

            var fillPath = new Microsoft.UI.Xaml.Shapes.Path { Data = fillGeo, Fill = fillBrush };
            PressureSparklineCanvas.Children.Add(fillPath);

            // Đường stroke vi mô mượt mà
            var strokeGeo = new PathGeometry();
            var strokeFig = new PathFigure { StartPoint = pts[0], IsClosed = false };
            for (int i = 0; i < pts.Count - 1; i++)
            {
                var midX = (pts[i].X + pts[i + 1].X) / 2.0;
                strokeFig.Segments.Add(new BezierSegment
                {
                    Point1 = new Windows.Foundation.Point(midX, pts[i].Y),
                    Point2 = new Windows.Foundation.Point(midX, pts[i + 1].Y),
                    Point3 = pts[i + 1]
                });
            }
            strokeGeo.Figures.Add(strokeFig);

            var strokePath = new Microsoft.UI.Xaml.Shapes.Path
            {
                Data = strokeGeo,
                Stroke = new SolidColorBrush(strokeColor),
                StrokeThickness = isDark ? 1.8 : 2.0
            };
            PressureSparklineCanvas.Children.Add(strokePath);

            // Chấm tròn hiện tại (điểm đầu tiên)
            var curDot = new Microsoft.UI.Xaml.Shapes.Ellipse
            {
                Width = 5,
                Height = 5,
                Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
                Stroke = new SolidColorBrush(dotStroke),
                StrokeThickness = 1.2
            };
            Canvas.SetLeft(curDot, pts[0].X - 2.5);
            Canvas.SetTop(curDot, pts[0].Y - 2.5);
            PressureSparklineCanvas.Children.Add(curDot);
        }
        catch { }
        finally
        {
            _isRenderingSparkline = false;
        }
    }

    private void UpdateHeroCardSeasonalColors()
    {
        if (ViewModel == null) return;
        try
        {
            var skin = ViewModel.CurrentSeasonalSkin;
            string startHex = skin?.HeroGradientStartHex ?? ViewModel.CurrentWeather?.HeroGradientStart ?? "#0284C7";
            string endHex = skin?.HeroGradientEndHex ?? ViewModel.CurrentWeather?.HeroGradientEnd ?? "#0F172A";
            string glow0Hex = skin?.PrimaryAccentHex ?? ViewModel.CurrentWeather?.HeroGradientStart ?? "#1E3A8A";
            string glow1Hex = skin?.SecondaryAccentHex ?? ViewModel.CurrentWeather?.HeroGradientEnd ?? "#0EA5E9";
            string borderHex = skin?.BorderStrokeHex ?? "#33FFFFFF";

            // 1. Cập nhật Gradient Thẻ Hero
            if (HeroGradStop0 != null && !string.IsNullOrEmpty(startHex))
            {
                var h = startHex.TrimStart('#');
                if (h.Length == 6)
                {
                    HeroGradStop0.Color = Windows.UI.Color.FromArgb(255,
                        Convert.ToByte(h[..2], 16),
                        Convert.ToByte(h[2..4], 16),
                        Convert.ToByte(h[4..6], 16));
                }
            }
            if (HeroGradStop1 != null && !string.IsNullOrEmpty(endHex))
            {
                var h = endHex.TrimStart('#');
                if (h.Length == 6)
                {
                    HeroGradStop1.Color = Windows.UI.Color.FromArgb(255,
                        Convert.ToByte(h[..2], 16),
                        Convert.ToByte(h[2..4], 16),
                        Convert.ToByte(h[4..6], 16));
                }
            }

            // 2. Cập nhật Hào quang Backlight Glow
            if (HeroAmbientStop0 != null && !string.IsNullOrEmpty(glow0Hex))
            {
                var h = glow0Hex.TrimStart('#');
                if (h.Length == 6)
                {
                    HeroAmbientStop0.Color = Windows.UI.Color.FromArgb(255,
                        Convert.ToByte(h[..2], 16),
                        Convert.ToByte(h[2..4], 16),
                        Convert.ToByte(h[4..6], 16));
                }
            }
            if (HeroAmbientStop1 != null && !string.IsNullOrEmpty(glow1Hex))
            {
                var h = glow1Hex.TrimStart('#');
                if (h.Length == 6)
                {
                    HeroAmbientStop1.Color = Windows.UI.Color.FromArgb(255,
                        Convert.ToByte(h[..2], 16),
                        Convert.ToByte(h[2..4], 16),
                        Convert.ToByte(h[4..6], 16));
                }
            }

            // 3. Cập nhật Viền Thẻ Hero
            if (HeroCardBorder != null && !string.IsNullOrEmpty(borderHex))
            {
                var h = borderHex.TrimStart('#');
                if (h.Length == 6)
                {
                    HeroCardBorder.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(180,
                        Convert.ToByte(h[..2], 16),
                        Convert.ToByte(h[2..4], 16),
                        Convert.ToByte(h[4..6], 16)));
                }
                else if (h.Length == 8)
                {
                    HeroCardBorder.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(
                        Convert.ToByte(h[..2], 16),
                        Convert.ToByte(h[2..4], 16),
                        Convert.ToByte(h[4..6], 16),
                        Convert.ToByte(h[6..8], 16)));
                }
            }

            // 4. Cập nhật Banner Mùa & Lễ hội
            if (HeroBannerStop0 != null && skin != null && !string.IsNullOrEmpty(skin.PrimaryAccentHex))
            {
                var h = skin.PrimaryAccentHex.TrimStart('#');
                if (h.Length == 6)
                {
                    HeroBannerStop0.Color = Windows.UI.Color.FromArgb(100,
                        Convert.ToByte(h[..2], 16),
                        Convert.ToByte(h[2..4], 16),
                        Convert.ToByte(h[4..6], 16));
                }
            }
            if (HeroBannerStop1 != null && skin != null && !string.IsNullOrEmpty(skin.SecondaryAccentHex))
            {
                var h = skin.SecondaryAccentHex.TrimStart('#');
                if (h.Length == 6)
                {
                    HeroBannerStop1.Color = Windows.UI.Color.FromArgb(60,
                        Convert.ToByte(h[..2], 16),
                        Convert.ToByte(h[2..4], 16),
                        Convert.ToByte(h[4..6], 16));
                }
            }

            // 5. Cập nhật viền Bento Grid Cards theo Theme Mùa / Đại Lễ Hội
            if (skin != null && !string.IsNullOrEmpty(skin.BentoCardBorderAccentHex))
            {
                var bentoBrush = new SolidColorBrush(ParseHexColor(skin.BentoCardBorderAccentHex));
                Border?[] bentoCards = { CardUv, CardAqi, CardWind, CardHumidity, CardRain, CardPressure, CardSunMoon, CardPollutants };
                foreach (var card in bentoCards)
                {
                    if (card != null) card.BorderBrush = bentoBrush;
                }
            }

            // 6. Vẽ lại biểu đồ nhiệt độ 24h & cung mặt trời theo theme mùa
            RenderHourlyTrendline();
            RenderSunArc();
        }
        catch { }
    }

    private void HeroSeasonalItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string skinMode && ViewModel != null)
        {
            ViewModel.ChangeSeasonalSkin(skinMode);
            if (HeroSeasonalSwitchButton?.Flyout != null)
            {
                HeroSeasonalSwitchButton.Flyout.Hide();
            }
            UpdateWeatherVisuals();
            RedrawCanvases();
        }
    }

    private void StartAmbientGlowAnimation()
    {
        if (_heroAmbientGlowStoryboard != null || HeroAmbientGlow == null) return;

        bool shouldAnimate = AnimationHelper.AreAnimationsEnabled &&
            !(ViewModel?.Settings.EnableBatterySaverOptimization == true && ViewModel?.IsBatterySavingActive == true) &&
            (ViewModel?.Settings.EnableWeatherEffects != false);

        if (!shouldAnimate)
        {
            HeroAmbientGlow.Opacity = 0.45;
            return;
        }

        var anim = new DoubleAnimation
        {
            From = 0.35,
            To = 0.58,
            Duration = new Duration(TimeSpan.FromMilliseconds(3500)),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };

        Storyboard.SetTarget(anim, HeroAmbientGlow);
        Storyboard.SetTargetProperty(anim, "Opacity");

        _heroAmbientGlowStoryboard = new Storyboard();
        _heroAmbientGlowStoryboard.Children.Add(anim);
        _heroAmbientGlowStoryboard.Begin();
    }

    private void StopAmbientGlowAnimation()
    {
        if (_heroAmbientGlowStoryboard != null)
        {
            _heroAmbientGlowStoryboard.Stop();
            _heroAmbientGlowStoryboard = null;
        }
        if (HeroAmbientGlow != null)
        {
            HeroAmbientGlow.Opacity = 0.45;
        }
    }

    public void RenderSunArc()
    {
        if (_isRenderingSunArc) return;
        if (SunArcCanvas == null || ViewModel?.CurrentWeather == null) return;

        double width = SunArcCanvas.ActualWidth;
        double height = SunArcCanvas.ActualHeight;
        if (width <= 20 || height <= 10) return;

        try
        {
            _isRenderingSunArc = true;
            SunArcCanvas.Children.Clear();

            double horizonY = height - 4;
            double leftX = 10;
            double rightX = width - 10;
            double peakY = 5;

            bool isDark = ActualTheme != ElementTheme.Light;

            // 1. Đường chân trời chấm nét đứt
            var horizonLine = new Microsoft.UI.Xaml.Shapes.Line
            {
                X1 = 4,
                Y1 = horizonY,
                X2 = width - 4,
                Y2 = horizonY,
                Stroke = new SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(40, 255, 255, 255) : Windows.UI.Color.FromArgb(90, 100, 116, 139)),
                StrokeThickness = isDark ? 1 : 1.2,
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
                Stroke = new SolidColorBrush(GetSunArcStrokeColor(isDark)),
                StrokeThickness = isDark ? 1.8 : 2.2,
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
                    ? (isDark ? Windows.UI.Color.FromArgb(70, 251, 191, 36) : Windows.UI.Color.FromArgb(90, 245, 158, 11))
                    : (isDark ? Windows.UI.Color.FromArgb(60, 96, 165, 250) : Windows.UI.Color.FromArgb(80, 2, 132, 199)))
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
                    ? (isDark ? Windows.UI.Color.FromArgb(255, 245, 158, 11) : Windows.UI.Color.FromArgb(255, 217, 119, 6))
                    : (isDark ? Windows.UI.Color.FromArgb(255, 147, 197, 253) : Windows.UI.Color.FromArgb(255, 2, 132, 199))),
                Stroke = new SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 255, 255, 255) : Windows.UI.Color.FromArgb(255, 15, 23, 42)),
                StrokeThickness = 1.5
            };
            Canvas.SetLeft(celestialDot, currentX - 5);
            Canvas.SetTop(celestialDot, currentY - 5);
            SunArcCanvas.Children.Add(celestialDot);
        }
        catch { }
        finally
        {
            _isRenderingSunArc = false;
        }
    }

    private Windows.UI.Color GetSunArcStrokeColor(bool isDark)
    {
        var skin = ViewModel?.CurrentSeasonalSkin;
        if (skin != null && !string.IsNullOrEmpty(skin.SunArcStrokeHex))
        {
            var parsed = ParseHexColor(skin.SunArcStrokeHex);
            return isDark
                ? Windows.UI.Color.FromArgb(170, parsed.R, parsed.G, parsed.B)
                : Windows.UI.Color.FromArgb(220, (byte)Math.Max(0, parsed.R - 20), (byte)Math.Max(0, parsed.G - 20), (byte)Math.Max(0, parsed.B - 20));
        }

        return isDark
            ? Windows.UI.Color.FromArgb(90, 255, 255, 255)
            : Windows.UI.Color.FromArgb(170, 180, 83, 9);
    }

    private static Windows.UI.Color ParseHexColor(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return Windows.UI.Color.FromArgb(255, 56, 189, 248);
        try
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 6)
            {
                byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                return Windows.UI.Color.FromArgb(255, r, g, b);
            }
            if (hex.Length == 8)
            {
                byte a = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                byte r = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                byte g = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                byte b = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
                return Windows.UI.Color.FromArgb(a, r, g, b);
            }
        }
        catch { }
        return Windows.UI.Color.FromArgb(255, 56, 189, 248);
    }

    private void HourlyCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is HourlyForecastItem item)
        {
            ViewModel?.ScrubToItem(item);
            RenderHourlyTrendline();
        }
    }

    private void UpdateResponsiveLayout(double width)
    {
        if (width <= 0) return;

        // 1. Tự động điều chỉnh cấu trúc 2 cột Bento Card của Hero
        bool isNarrow = width < 920;
        if (isNarrow != _wasNarrowHero)
        {
            _wasNarrowHero = isNarrow;
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
        }

        // 2. Tự động điều chỉnh lưới 8 chỉ số: 4 cột khi rộng, 2 cột khi hẹp để không bị ép chữ
        bool isMetricsNarrow = width < 960;
        if (isMetricsNarrow != _wasNarrowMetrics)
        {
            _wasNarrowMetrics = isMetricsNarrow;
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
    }

    public void RenderVisualGauges()
    {
        if (_isRenderingVisualGauges) return;
        var cw = ViewModel?.CurrentWeather;
        if (cw == null) return;

        try
        {
            _isRenderingVisualGauges = true;

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

            // 4. Biểu đồ Sparkline áp suất vi mô 6 giờ (v3.1.0)
            RenderPressureSparkline();
        }
        catch { }
        finally
        {
            _isRenderingVisualGauges = false;
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

        // Fluent Reveal Glow: viền phát sáng bám theo vị trí con trỏ chuột (v3.1.0)
        card.PointerMoved += (s, e) =>
        {
            if (isClickableMetric && card.Tag is string tag)
            {
                var glowColor = GetMetricGlowColor(tag);
                var pos = e.GetCurrentPoint(card).Position;
                double w = Math.Max(10, card.ActualWidth);
                double h = Math.Max(10, card.ActualHeight);
                double cx = Math.Clamp(pos.X / w, 0.0, 1.0);
                double cy = Math.Clamp(pos.Y / h, 0.0, 1.0);

                var revealBrush = new RadialGradientBrush
                {
                    Center = new Windows.Foundation.Point(cx, cy),
                    GradientOrigin = new Windows.Foundation.Point(cx, cy),
                    RadiusX = 0.85,
                    RadiusY = 0.85
                };
                revealBrush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(230, glowColor.R, glowColor.G, glowColor.B), Offset = 0.0 });
                revealBrush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(100, glowColor.R, glowColor.G, glowColor.B), Offset = 0.5 });
                revealBrush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(40, 255, 255, 255), Offset = 1.0 });
                card.BorderBrush = revealBrush;
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
        double availableHeight = this.ActualHeight - 48;
        double availableWidth = this.ActualWidth - 32;

        if (MetricDetailCard != null)
        {
            if (availableHeight > 250)
            {
                MetricDetailCard.MaxHeight = Math.Min(680, availableHeight);
            }
            if (availableWidth > 250)
            {
                MetricDetailCard.MaxWidth = Math.Min(660, availableWidth);
            }
        }

        if (WeatherComparisonCard != null)
        {
            if (availableHeight > 250)
            {
                WeatherComparisonCard.MaxHeight = Math.Min(720, availableHeight);
            }
            if (availableWidth > 250)
            {
                WeatherComparisonCard.MaxWidth = Math.Min(740, availableWidth);
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

    #region WEATHER DUEL COMPARISON & FX PREVIEW HANDLERS (v3.0.9)
    private void WeatherComparisonBackdrop_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        ViewModel?.CloseWeatherComparison();
    }

    private async void PresetCompareCity_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && ViewModel != null)
        {
            var item = (fe.DataContext as QuickCompareCityItem) ?? (fe.Tag as QuickCompareCityItem);
            if (item != null)
            {
                await ViewModel.SelectPresetCompareCityAsync(item);
            }
        }
    }

    private async void ComparisonSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput && ViewModel != null)
        {
            await ViewModel.SearchComparisonCitiesAsync(sender.Text);
        }
    }

    private async void ComparisonSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (ViewModel == null) return;

        if (args.ChosenSuggestion is GeocodingItem item)
        {
            await ViewModel.SelectSearchCompareCityAsync(item);
        }
        else if (!string.IsNullOrWhiteSpace(args.QueryText))
        {
            await ViewModel.SearchComparisonCitiesAsync(args.QueryText);
            var first = ViewModel.ComparisonSearchResults.FirstOrDefault();
            if (first != null)
            {
                await ViewModel.SelectSearchCompareCityAsync(first);
            }
        }
    }

    private void ComparisonSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is GeocodingItem item)
        {
            sender.Text = item.DisplayName;
        }
    }

    private void FxPreviewAuto_Click(object sender, RoutedEventArgs e)
    {
        _weatherEffectRenderer?.SetPreviewEffect(null);
        _weatherEffectRenderer?.Resume();
    }

    private void FxPreviewSunny_Click(object sender, RoutedEventArgs e)
    {
        _weatherEffectRenderer?.SetPreviewEffect(WeatherEffectType.ClearSunny);
        _weatherEffectRenderer?.Resume();
    }

    private void FxPreviewRain_Click(object sender, RoutedEventArgs e)
    {
        _weatherEffectRenderer?.SetPreviewEffect(WeatherEffectType.HeavyRain);
        _weatherEffectRenderer?.Resume();
    }

    private void FxPreviewThunder_Click(object sender, RoutedEventArgs e)
    {
        _weatherEffectRenderer?.SetPreviewEffect(WeatherEffectType.Thunderstorm);
        _weatherEffectRenderer?.Resume();
    }

    private void FxPreviewSnow_Click(object sender, RoutedEventArgs e)
    {
        _weatherEffectRenderer?.SetPreviewEffect(WeatherEffectType.Snow);
        _weatherEffectRenderer?.Resume();
    }

    private void FxPreviewFog_Click(object sender, RoutedEventArgs e)
    {
        _weatherEffectRenderer?.SetPreviewEffect(WeatherEffectType.Fog);
        _weatherEffectRenderer?.Resume();
    }
    #endregion
}
