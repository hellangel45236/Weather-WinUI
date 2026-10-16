using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using WeatherApp.Models;

namespace WeatherApp.Helpers;

/// <summary>
/// Bộ dựng hiệu ứng thời tiết hạt động thời gian thực cao cấp (Live Particle FX Engine)
/// Hỗ trợ:
/// - Mưa rơi xiên theo gió với vệt mưa sắc nét, bọt nước va chạm sàn (splash ripples) và hạt nước bắn tóe (micro-droplets)
/// - Nắng vàng rực rỡ với quầng mặt trời đa tầng (pulsing corona), tia nắng quét óng ánh (sunbeams) và bụi nắng vàng bay lơ lửng (golden bokeh)
/// - Tuyết rơi lượn sóng đa tần chao đảo theo gió
/// - Mây bồng bềnh trôi đa tầng (volumetric drifting clouds) với độ sâu không gian và sóng dập dềnh
/// - Bầu trời đêm lung linh với quầng trăng, sao đêm nhấp nháy đa tầng và sao băng (meteor / shooting star) lướt qua bầu trời
/// - Dông bão với sấm sét đa tầng giật sáng chói lóa
/// Vận hành mượt mà 60 FPS, tối ưu tài nguyên và tương thích tuyệt đối mọi phiên bản Windows.
/// </summary>
public class WeatherEffectRenderer
{
    private readonly Canvas _canvas;
    private readonly Border _lightningOverlay;
    private readonly DispatcherTimer _renderTimer = new();
    private readonly DispatcherTimer _lightningTimer = new();
    private readonly Random _random = new();

    private WeatherEffectType _currentEffect = WeatherEffectType.ClearSunny;
    private WeatherEffectType? _previewOverrideEffect = null;

    private double _windSpeedKmh = 12.0;
    private double _windAngleDeg = 10.0;
    private double _windDriftRatio = 0.18;

    private readonly List<Raindrop> _raindrops = new();
    private readonly List<SplashRipple> _splashes = new();
    private readonly List<SplashDroplet> _splashDroplets = new();
    private readonly List<Snowflake> _snowflakes = new();
    private readonly List<SunSparkle> _sunSparkles = new();
    private readonly List<SunRay> _sunRays = new();
    private readonly List<DriftingCloud> _clouds = new();
    private readonly List<NightStar> _nightStars = new();
    private readonly List<Meteor> _meteors = new();
    private readonly List<FrameworkElement> _ambientElements = new();
    private readonly List<SeasonalParticle> _seasonalParticles = new();
    private SeasonalParticleType _currentSeasonalParticleType = SeasonalParticleType.None;
    private bool _isSeasonalParticlesEnabled = true;

    private double _animationTime = 0;
    private double _nextMeteorTime = 4.0;

    #region Particle Data Structures

    private class Raindrop
    {
        public Rectangle Element { get; set; } = null!;
        public double X { get; set; }
        public double Y { get; set; }
        public double Speed { get; set; }
        public double Length { get; set; }
    }

    private class SplashRipple
    {
        public Ellipse Element { get; set; } = null!;
        public double X { get; set; }
        public double Y { get; set; }
        public double CurrentRadiusX { get; set; }
        public double CurrentRadiusY { get; set; }
        public double MaxRadiusX { get; set; }
        public double MaxRadiusY { get; set; }
        public double Opacity { get; set; }
        public double GrowthSpeed { get; set; }
        public bool IsActive { get; set; }
    }

    private class SplashDroplet
    {
        public Ellipse Element { get; set; } = null!;
        public double X { get; set; }
        public double Y { get; set; }
        public double Vx { get; set; }
        public double Vy { get; set; }
        public double Gravity { get; set; }
        public double Opacity { get; set; }
        public bool IsActive { get; set; }
    }

    private class Snowflake
    {
        public Ellipse Element { get; set; } = null!;
        public double X { get; set; }
        public double Y { get; set; }
        public double Speed { get; set; }
        public double DriftOffset { get; set; }
        public double BaseSize { get; set; }
        public double SwayFreq { get; set; }
    }

    private class SunSparkle
    {
        public Ellipse Element { get; set; } = null!;
        public double X { get; set; }
        public double Y { get; set; }
        public double SpeedY { get; set; }
        public double DriftOffset { get; set; }
        public double BaseOpacity { get; set; }
        public double Phase { get; set; }
    }

    private class SunRay
    {
        public Rectangle Element { get; set; } = null!;
        public double BaseAngle { get; set; }
        public double BaseOpacity { get; set; }
        public double Phase { get; set; }
        public double Speed { get; set; }
        public RotateTransform Transform { get; set; } = null!;
    }

    private class DriftingCloud
    {
        public Border Element { get; set; } = null!;
        public double X { get; set; }
        public double BaseY { get; set; }
        public double SpeedX { get; set; }
        public double Width { get; set; }
        public double WavePhase { get; set; }
    }

    private class NightStar
    {
        public Ellipse Element { get; set; } = null!;
        public double BaseOpacity { get; set; }
        public double TwinklePhase { get; set; }
        public double TwinkleSpeed { get; set; }
    }

    private class Meteor
    {
        public Line Element { get; set; } = null!;
        public double X { get; set; }
        public double Y { get; set; }
        public double SpeedX { get; set; }
        public double SpeedY { get; set; }
        public double Length { get; set; }
        public double Opacity { get; set; }
        public bool IsActive { get; set; }
    }

    private class SeasonalParticle
    {
        public FrameworkElement Element { get; set; } = null!;
        public double X { get; set; }
        public double Y { get; set; }
        public double SpeedY { get; set; }
        public double DriftOffset { get; set; }
        public double SwayFreq { get; set; }
        public double BaseOpacity { get; set; }
        public double Size { get; set; }
        public bool FloatsUp { get; set; }
        public RotateTransform? RotationTransform { get; set; }
        public double CurrentAngle { get; set; }
        public double RotSpeed { get; set; }
    }

    #endregion

    public WeatherEffectRenderer(Canvas canvas, Border lightningOverlay)
    {
        _canvas = canvas;
        _lightningOverlay = lightningOverlay;

        _canvas.SizeChanged += (s, e) =>
        {
            if (e.NewSize.Width > 0 && e.NewSize.Height > 0)
            {
                _canvas.Clip = new RectangleGeometry
                {
                    Rect = new Windows.Foundation.Rect(0, 0, e.NewSize.Width, e.NewSize.Height)
                };
            }
        };

        // 60 FPS mượt mà cho trải nghiệm thị giác cao cấp
        _renderTimer.Interval = TimeSpan.FromMilliseconds(16);
        _renderTimer.Tick += RenderTimer_Tick;

        _lightningTimer.Interval = TimeSpan.FromSeconds(4);
        _lightningTimer.Tick += LightningTimer_Tick;
    }

    /// <summary>
    /// Cập nhật sức gió để điều chỉnh góc nghiêng hạt mưa/tuyết động
    /// </summary>
    public void SetWind(double speedKmh)
    {
        _windSpeedKmh = Math.Max(0, speedKmh);

        // Chuyển tốc độ gió (km/h) sang góc nghiêng
        // Gió lặng: ~5 độ, Gió vừa 15-25 km/h: ~12-18 độ, Gió mạnh > 40 km/h: ~25-35 độ
        _windAngleDeg = Math.Clamp(4.0 + (_windSpeedKmh / 50.0) * 26.0, 4.0, 34.0);
        _windDriftRatio = Math.Tan(_windAngleDeg * Math.PI / 180.0);
    }

    public void Pause()
    {
        _renderTimer.Stop();
        _lightningTimer.Stop();
    }

    public void Resume()
    {
        if (!_renderTimer.IsEnabled)
        {
            _renderTimer.Start();
        }

        var activeEffect = _previewOverrideEffect ?? _currentEffect;
        if (activeEffect == WeatherEffectType.Thunderstorm && !_lightningTimer.IsEnabled)
        {
            _lightningTimer.Start();
        }
    }

    public void SetWeatherEffect(WeatherEffectType effect)
    {
        _currentEffect = effect;
        if (_previewOverrideEffect == null)
        {
            ApplyEffect(effect);
        }
    }

    /// <summary>
    /// Cho phép người dùng xem thử bất kỳ hiệu ứng nào hoặc quay lại chế độ tự động
    /// </summary>
    public void SetPreviewEffect(WeatherEffectType? previewEffect)
    {
        _previewOverrideEffect = previewEffect;
        ApplyEffect(previewEffect ?? _currentEffect);
    }

    private void ApplyEffect(WeatherEffectType effect)
    {
        try
        {
            ClearElements();

            switch (effect)
            {
                case WeatherEffectType.Thunderstorm:
                    CreateRaindrops(120, isHeavy: true);
                    CreateSplashPool(25);
                    StartLightning();
                    break;

                case WeatherEffectType.HeavyRain:
                    CreateRaindrops(95, isHeavy: true);
                    CreateSplashPool(20);
                    StopLightning();
                    break;

                case WeatherEffectType.ModerateRain:
                    CreateRaindrops(65, isHeavy: false);
                    CreateSplashPool(15);
                    StopLightning();
                    break;

                case WeatherEffectType.LightRain:
                    CreateRaindrops(40, isHeavy: false, isLight: true);
                    CreateSplashPool(10);
                    StopLightning();
                    break;

                case WeatherEffectType.HighUvSunny:
                    CreateSunGlow(isHighUv: true);
                    CreateSunRays(isHighUv: true);
                    CreateSunSparkles(42, isHighUv: true);
                    StopLightning();
                    break;

                case WeatherEffectType.ClearSunny:
                    CreateSunGlow(isHighUv: false);
                    CreateSunRays(isHighUv: false);
                    CreateSunSparkles(36, isHighUv: false);
                    StopLightning();
                    break;

                case WeatherEffectType.ClearNight:
                    CreateNightAura();
                    CreateNightStars(48);
                    CreateMeteorPool(2);
                    StopLightning();
                    break;

                case WeatherEffectType.Snow:
                    CreateSnowflakes(60);
                    StopLightning();
                    break;

                case WeatherEffectType.PartlyCloudy:
                    CreateSunGlow(isHighUv: false, isSubtle: true);
                    CreateSunSparkles(12, isHighUv: false);
                    CreateDriftingClouds(6, isHeavyMist: false);
                    StopLightning();
                    break;

                case WeatherEffectType.Cloudy:
                    CreateDriftingClouds(8, isHeavyMist: false);
                    StopLightning();
                    break;

                case WeatherEffectType.Fog:
                    CreateDriftingClouds(9, isHeavyMist: true);
                    StopLightning();
                    break;

                default:
                    CreateSunGlow(isHighUv: false, isSubtle: true);
                    CreateDriftingClouds(5, isHeavyMist: false);
                    StopLightning();
                    break;
            }

            if (_isSeasonalParticlesEnabled && _currentSeasonalParticleType != SeasonalParticleType.None)
            {
                CreateSeasonalParticles(_currentSeasonalParticleType);
            }

            if (!_renderTimer.IsEnabled)
            {
                _renderTimer.Start();
            }
        }
        catch { }
    }

    private void ClearElements()
    {
        _canvas.Children.Clear();
        _raindrops.Clear();
        _splashes.Clear();
        _splashDroplets.Clear();
        _snowflakes.Clear();
        _sunSparkles.Clear();
        _sunRays.Clear();
        _clouds.Clear();
        _nightStars.Clear();
        _meteors.Clear();
        _seasonalParticles.Clear();
        _ambientElements.Clear();
        _lightningOverlay.Opacity = 0;
    }

    private (double Width, double Height) GetCanvasDimensions()
    {
        var parent = _canvas.Parent as FrameworkElement;
        double width = _canvas.ActualWidth > 50 ? _canvas.ActualWidth
            : (parent != null && parent.ActualWidth > 50 ? parent.ActualWidth : 800);
        double height = _canvas.ActualHeight > 50 ? _canvas.ActualHeight
            : (parent != null && parent.ActualHeight > 50 ? parent.ActualHeight : 260);
        return (width, height);
    }

    #region Element Creation Methods

    private void CreateRaindrops(int count, bool isHeavy = false, bool isLight = false)
    {
        var (width, height) = GetCanvasDimensions();

        for (int i = 0; i < count; i++)
        {
            double dropLength = isHeavy ? _random.Next(22, 34) : (isLight ? _random.Next(11, 16) : _random.Next(16, 24));
            double dropWidth = isHeavy ? 2.2 : (isLight ? 1.3 : 1.7);
            double opacity = isHeavy ? _random.NextDouble() * 0.35 + 0.50 : _random.NextDouble() * 0.25 + 0.35;

            var rect = new Rectangle
            {
                Width = dropWidth,
                Height = dropLength,
                RadiusX = 1,
                RadiusY = 1,
                Opacity = opacity,
                Fill = new LinearGradientBrush
                {
                    StartPoint = new Windows.Foundation.Point(0, 0),
                    EndPoint = new Windows.Foundation.Point(0, 1),
                    GradientStops =
                    {
                        new GradientStop { Color = Windows.UI.Color.FromArgb(0, 180, 225, 255), Offset = 0 },
                        new GradientStop { Color = Windows.UI.Color.FromArgb(245, 245, 255, 255), Offset = 1 }
                    }
                },
                RenderTransform = new RotateTransform { Angle = _windAngleDeg }
            };

            double startX = _random.NextDouble() * (width + 140) - 70;
            double startY = _random.NextDouble() * height;
            double speed = isHeavy ? _random.Next(24, 35) : (isLight ? _random.Next(11, 17) : _random.Next(17, 26));

            Canvas.SetLeft(rect, startX);
            Canvas.SetTop(rect, startY);

            _canvas.Children.Add(rect);
            _raindrops.Add(new Raindrop
            {
                Element = rect,
                X = startX,
                Y = startY,
                Speed = speed,
                Length = dropLength
            });
        }
    }

    private void CreateSplashPool(int count)
    {
        for (int i = 0; i < count; i++)
        {
            // Bọt nước mở rộng hình oval (Splash Ripple)
            var ripple = new Ellipse
            {
                Width = 2,
                Height = 1,
                Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(170, 230, 245, 255)),
                StrokeThickness = 1.0,
                Opacity = 0,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(ripple, -100);
            Canvas.SetTop(ripple, -100);
            _canvas.Children.Add(ripple);

            _splashes.Add(new SplashRipple
            {
                Element = ripple,
                X = -100,
                Y = -100,
                CurrentRadiusX = 1,
                CurrentRadiusY = 1,
                MaxRadiusX = _random.Next(8, 16),
                MaxRadiusY = _random.Next(3, 7),
                Opacity = 0,
                GrowthSpeed = _random.NextDouble() * 0.9 + 0.7,
                IsActive = false
            });

            // Hạt nước bắn tóe lên trên (Micro Droplets)
            var droplet = new Ellipse
            {
                Width = 2,
                Height = 2,
                Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(220, 240, 250, 255)),
                Opacity = 0,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(droplet, -100);
            Canvas.SetTop(droplet, -100);
            _canvas.Children.Add(droplet);

            _splashDroplets.Add(new SplashDroplet
            {
                Element = droplet,
                X = -100,
                Y = -100,
                Vx = 0,
                Vy = 0,
                Gravity = 0.28,
                Opacity = 0,
                IsActive = false
            });
        }
    }

    private void TriggerSplash(double x, double y)
    {
        // Kích hoạt ripple
        foreach (var splash in _splashes)
        {
            if (!splash.IsActive)
            {
                splash.X = x;
                splash.Y = y;
                splash.CurrentRadiusX = 2;
                splash.CurrentRadiusY = 1;
                splash.Opacity = 0.70;
                splash.IsActive = true;
                splash.Element.Width = 2;
                splash.Element.Height = 1;
                splash.Element.Opacity = 0.70;
                Canvas.SetLeft(splash.Element, x - 1);
                Canvas.SetTop(splash.Element, y);
                break;
            }
        }

        // Kích hoạt 1 hạt nước bắn tóe lên
        foreach (var drop in _splashDroplets)
        {
            if (!drop.IsActive)
            {
                drop.X = x;
                drop.Y = y;
                drop.Vx = (_random.NextDouble() * 3.2 - 1.6) + (_windDriftRatio * 1.8);
                drop.Vy = -(_random.NextDouble() * 2.8 + 1.8);
                drop.Opacity = 0.85;
                drop.IsActive = true;
                drop.Element.Opacity = 0.85;
                Canvas.SetLeft(drop.Element, x);
                Canvas.SetTop(drop.Element, y);
                break;
            }
        }
    }

    private void CreateSnowflakes(int count)
    {
        var (width, height) = GetCanvasDimensions();

        for (int i = 0; i < count; i++)
        {
            double size = _random.Next(3, 9);
            var circle = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new RadialGradientBrush
                {
                    Center = new Windows.Foundation.Point(0.5, 0.5),
                    RadiusX = 0.5,
                    RadiusY = 0.5,
                    GradientStops =
                    {
                        new GradientStop { Color = Windows.UI.Color.FromArgb(245, 255, 255, 255), Offset = 0 },
                        new GradientStop { Color = Windows.UI.Color.FromArgb(80, 240, 245, 255), Offset = 1 }
                    }
                },
                Opacity = _random.NextDouble() * 0.5 + 0.40
            };

            double startX = _random.NextDouble() * width;
            double startY = _random.NextDouble() * height;
            double speed = _random.NextDouble() * 1.8 + 1.1;

            Canvas.SetLeft(circle, startX);
            Canvas.SetTop(circle, startY);

            _canvas.Children.Add(circle);
            _snowflakes.Add(new Snowflake
            {
                Element = circle,
                X = startX,
                Y = startY,
                Speed = speed,
                DriftOffset = _random.NextDouble() * 12,
                BaseSize = size,
                SwayFreq = _random.NextDouble() * 1.2 + 1.5
            });
        }
    }

    private void CreateSunGlow(bool isHighUv, bool isSubtle = false)
    {
        var (width, _) = GetCanvasDimensions();

        var primaryGlowColor = isHighUv
            ? Windows.UI.Color.FromArgb(isSubtle ? (byte)30 : (byte)75, 255, 95, 20)
            : Windows.UI.Color.FromArgb(isSubtle ? (byte)25 : (byte)60, 255, 195, 30);

        var outerGlowColor = isHighUv
            ? Windows.UI.Color.FromArgb(0, 255, 120, 30)
            : Windows.UI.Color.FromArgb(0, 255, 210, 60);

        // Vầng hào quang ngoài (Outer Pulsing Corona)
        var outerAura = new Ellipse
        {
            Width = 360,
            Height = 360,
            Fill = new RadialGradientBrush
            {
                Center = new Windows.Foundation.Point(0.5, 0.5),
                RadiusX = 0.5,
                RadiusY = 0.5,
                GradientStops =
                {
                    new GradientStop { Color = primaryGlowColor, Offset = 0 },
                    new GradientStop { Color = outerGlowColor, Offset = 1 }
                }
            },
            Opacity = isSubtle ? 0.45 : 0.85
        };

        Canvas.SetLeft(outerAura, width - 260);
        Canvas.SetTop(outerAura, -80);
        _canvas.Children.Add(outerAura);
        _ambientElements.Add(outerAura);

        if (!isSubtle)
        {
            // Quầng sáng trong sắc nét hơn (Inner Radiant Sun Core)
            var innerAura = new Ellipse
            {
                Width = 200,
                Height = 200,
                Fill = new RadialGradientBrush
                {
                    Center = new Windows.Foundation.Point(0.5, 0.5),
                    RadiusX = 0.5,
                    RadiusY = 0.5,
                    GradientStops =
                    {
                        new GradientStop { Color = Windows.UI.Color.FromArgb(100, 255, 240, 160), Offset = 0 },
                        new GradientStop { Color = Windows.UI.Color.FromArgb(0, 255, 200, 60), Offset = 1 }
                    }
                },
                Opacity = 0.90
            };

            Canvas.SetLeft(innerAura, width - 180);
            Canvas.SetTop(innerAura, -30);
            _canvas.Children.Add(innerAura);
            _ambientElements.Add(innerAura);
        }
    }

    private void CreateSunRays(bool isHighUv)
    {
        var (width, _) = GetCanvasDimensions();

        // 4 vệt tia nắng vàng óng ánh chiếu nghiêng từ quầng mặt trời xuống (Shimmering God Rays)
        double[] angles = { 32.0, 41.0, 49.0, 58.0 };
        double[] widths = { 55.0, 75.0, 65.0, 85.0 };
        double[] opacities = { 0.18, 0.24, 0.20, 0.16 };

        for (int i = 0; i < angles.Length; i++)
        {
            var rotate = new RotateTransform
            {
                Angle = angles[i],
                CenterX = 0,
                CenterY = 0
            };

            var rayRect = new Rectangle
            {
                Width = widths[i],
                Height = 450,
                Opacity = opacities[i],
                RenderTransform = rotate,
                IsHitTestVisible = false,
                Fill = new LinearGradientBrush
                {
                    StartPoint = new Windows.Foundation.Point(0, 0),
                    EndPoint = new Windows.Foundation.Point(0, 1),
                    GradientStops =
                    {
                        new GradientStop { Color = Windows.UI.Color.FromArgb(isHighUv ? (byte)95 : (byte)75, 255, 235, 140), Offset = 0 },
                        new GradientStop { Color = Windows.UI.Color.FromArgb(isHighUv ? (byte)45 : (byte)30, 255, 215, 90), Offset = 0.4 },
                        new GradientStop { Color = Windows.UI.Color.FromArgb(0, 255, 200, 50), Offset = 1 }
                    }
                }
            };

            Canvas.SetLeft(rayRect, width - 130);
            Canvas.SetTop(rayRect, -40);

            _canvas.Children.Add(rayRect);
            _sunRays.Add(new SunRay
            {
                Element = rayRect,
                BaseAngle = angles[i],
                BaseOpacity = opacities[i],
                Phase = _random.NextDouble() * Math.PI * 2,
                Speed = _random.NextDouble() * 0.8 + 0.8,
                Transform = rotate
            });
        }
    }

    private void CreateSunSparkles(int count, bool isHighUv)
    {
        var (width, height) = GetCanvasDimensions();

        for (int i = 0; i < count; i++)
        {
            double size = _random.Next(4, 11);
            var sparkle = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new RadialGradientBrush
                {
                    Center = new Windows.Foundation.Point(0.5, 0.5),
                    RadiusX = 0.5,
                    RadiusY = 0.5,
                    GradientStops =
                    {
                        new GradientStop { Color = Windows.UI.Color.FromArgb(255, 255, 245, 180), Offset = 0 },
                        new GradientStop { Color = isHighUv ? Windows.UI.Color.FromArgb(120, 255, 160, 40) : Windows.UI.Color.FromArgb(100, 255, 200, 60), Offset = 0.6 },
                        new GradientStop { Color = Windows.UI.Color.FromArgb(0, 255, 180, 50), Offset = 1 }
                    }
                },
                Opacity = _random.NextDouble() * 0.5 + 0.35
            };

            double startX = _random.NextDouble() * width;
            double startY = _random.NextDouble() * height;
            double speedY = _random.NextDouble() * 1.1 + 0.7;

            Canvas.SetLeft(sparkle, startX);
            Canvas.SetTop(sparkle, startY);

            _canvas.Children.Add(sparkle);
            _sunSparkles.Add(new SunSparkle
            {
                Element = sparkle,
                X = startX,
                Y = startY,
                SpeedY = speedY,
                DriftOffset = _random.NextDouble() * 14,
                BaseOpacity = sparkle.Opacity,
                Phase = _random.NextDouble() * Math.PI * 2
            });
        }
    }

    private void CreateDriftingClouds(int count, bool isHeavyMist)
    {
        var (width, height) = GetCanvasDimensions();

        for (int i = 0; i < count; i++)
        {
            double cloudWidth = _random.Next(210, 360);
            double cloudHeight = _random.Next(45, 85);
            byte alpha = isHeavyMist ? (byte)_random.Next(50, 85) : (byte)_random.Next(35, 65);

            var cloudBorder = new Border
            {
                Width = cloudWidth,
                Height = cloudHeight,
                CornerRadius = new CornerRadius(cloudHeight / 2),
                Background = new LinearGradientBrush
                {
                    StartPoint = new Windows.Foundation.Point(0, 0),
                    EndPoint = new Windows.Foundation.Point(1, 0),
                    GradientStops =
                    {
                        new GradientStop { Color = Windows.UI.Color.FromArgb(0, 255, 255, 255), Offset = 0 },
                        new GradientStop { Color = Windows.UI.Color.FromArgb(alpha, 255, 255, 255), Offset = 0.5 },
                        new GradientStop { Color = Windows.UI.Color.FromArgb(0, 255, 255, 255), Offset = 1 }
                    }
                },
                Opacity = isHeavyMist ? 0.75 : 0.60
            };

            double startX = _random.NextDouble() * (width + cloudWidth) - cloudWidth;
            double baseY = _random.NextDouble() * (height - cloudHeight);
            double speedX = isHeavyMist ? (_random.NextDouble() * 0.5 + 0.4) : (_random.NextDouble() * 0.9 + 0.7);

            Canvas.SetLeft(cloudBorder, startX);
            Canvas.SetTop(cloudBorder, baseY);

            _canvas.Children.Add(cloudBorder);
            _clouds.Add(new DriftingCloud
            {
                Element = cloudBorder,
                X = startX,
                BaseY = baseY,
                SpeedX = speedX,
                Width = cloudWidth,
                WavePhase = _random.NextDouble() * Math.PI * 2
            });
        }
    }

    private void CreateNightAura()
    {
        var (width, _) = GetCanvasDimensions();

        // Vầng sáng trăng đêm mát dịu
        var moonAura = new Ellipse
        {
            Width = 260,
            Height = 260,
            Fill = new RadialGradientBrush
            {
                Center = new Windows.Foundation.Point(0.5, 0.5),
                RadiusX = 0.5,
                RadiusY = 0.5,
                GradientStops =
                {
                    new GradientStop { Color = Windows.UI.Color.FromArgb(55, 210, 230, 255), Offset = 0 },
                    new GradientStop { Color = Windows.UI.Color.FromArgb(0, 160, 190, 250), Offset = 1 }
                }
            },
            Opacity = 0.85
        };

        Canvas.SetLeft(moonAura, width - 210);
        Canvas.SetTop(moonAura, -60);
        _canvas.Children.Add(moonAura);
        _ambientElements.Add(moonAura);
    }

    private void CreateNightStars(int count)
    {
        var (width, height) = GetCanvasDimensions();

        for (int i = 0; i < count; i++)
        {
            double size = _random.Next(2, 6);
            var star = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(250, 255, 255, 235)),
                Opacity = _random.NextDouble() * 0.6 + 0.35
            };

            Canvas.SetLeft(star, _random.NextDouble() * width);
            Canvas.SetTop(star, _random.NextDouble() * height);

            _canvas.Children.Add(star);
            _nightStars.Add(new NightStar
            {
                Element = star,
                BaseOpacity = star.Opacity,
                TwinklePhase = _random.NextDouble() * Math.PI * 2,
                TwinkleSpeed = _random.NextDouble() * 1.6 + 1.2
            });
        }
    }

    private void CreateMeteorPool(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var meteorLine = new Line
            {
                X1 = 0,
                Y1 = 0,
                X2 = 35,
                Y2 = 25,
                StrokeThickness = 1.8,
                Stroke = new LinearGradientBrush
                {
                    StartPoint = new Windows.Foundation.Point(0, 0),
                    EndPoint = new Windows.Foundation.Point(1, 1),
                    GradientStops =
                    {
                        new GradientStop { Color = Windows.UI.Color.FromArgb(0, 255, 255, 255), Offset = 0 },
                        new GradientStop { Color = Windows.UI.Color.FromArgb(245, 255, 255, 255), Offset = 1 }
                    }
                },
                Opacity = 0,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(meteorLine, -200);
            Canvas.SetTop(meteorLine, -200);
            _canvas.Children.Add(meteorLine);

            _meteors.Add(new Meteor
            {
                Element = meteorLine,
                X = -200,
                Y = -200,
                SpeedX = -18,
                SpeedY = 13,
                Length = 40,
                Opacity = 0,
                IsActive = false
            });
        }
    }

    private void TriggerMeteor(double width)
    {
        foreach (var meteor in _meteors)
        {
            if (!meteor.IsActive)
            {
                meteor.X = _random.NextDouble() * (width * 0.6) + (width * 0.4);
                meteor.Y = _random.NextDouble() * 50 - 20;
                meteor.SpeedX = -(_random.NextDouble() * 10 + 16);
                meteor.SpeedY = _random.NextDouble() * 8 + 12;
                meteor.Opacity = 0.95;
                meteor.IsActive = true;
                meteor.Element.Opacity = 0.95;
                Canvas.SetLeft(meteor.Element, meteor.X);
                Canvas.SetTop(meteor.Element, meteor.Y);
                break;
            }
        }
    }

    #endregion

    #region Lightning Logic

    private void StartLightning()
    {
        _lightningTimer.Interval = TimeSpan.FromSeconds(_random.Next(3, 6));
        if (!_lightningTimer.IsEnabled)
        {
            _lightningTimer.Start();
        }
    }

    private void StopLightning()
    {
        _lightningTimer.Stop();
        _lightningOverlay.Opacity = 0;
    }

    private async void LightningTimer_Tick(object? sender, object e)
    {
        _lightningTimer.Interval = TimeSpan.FromSeconds(_random.Next(4, 8));

        var activeEffect = _previewOverrideEffect ?? _currentEffect;
        if (activeEffect != WeatherEffectType.Thunderstorm)
        {
            _lightningOverlay.Opacity = 0;
            return;
        }

        try
        {
            // Pha 1: Tia chớp mồi
            _lightningOverlay.Opacity = 0.40;
            await Task.Delay(45);
            if ((_previewOverrideEffect ?? _currentEffect) != WeatherEffectType.Thunderstorm) { _lightningOverlay.Opacity = 0; return; }

            // Giảm thoáng qua
            _lightningOverlay.Opacity = 0.08;
            await Task.Delay(35);
            if ((_previewOverrideEffect ?? _currentEffect) != WeatherEffectType.Thunderstorm) { _lightningOverlay.Opacity = 0; return; }

            // Pha 2: Chớp cực mạnh chói lòa
            _lightningOverlay.Opacity = 0.92;
            await Task.Delay(90);
            if ((_previewOverrideEffect ?? _currentEffect) != WeatherEffectType.Thunderstorm) { _lightningOverlay.Opacity = 0; return; }

            // Pha 3: Tàn dần êm ái
            for (int i = 7; i >= 0; i--)
            {
                if ((_previewOverrideEffect ?? _currentEffect) != WeatherEffectType.Thunderstorm) { _lightningOverlay.Opacity = 0; return; }
                _lightningOverlay.Opacity = i * 0.12;
                await Task.Delay(25);
            }
            _lightningOverlay.Opacity = 0;
        }
        catch
        {
            _lightningOverlay.Opacity = 0;
        }
    }

    #endregion

    #region Seasonal & Festival Particle Creation

    public void SetSeasonalParticleType(SeasonalParticleType type, bool enabled = true)
    {
        _currentSeasonalParticleType = type;
        _isSeasonalParticlesEnabled = enabled;
        ClearSeasonalParticles();

        if (enabled && type != SeasonalParticleType.None)
        {
            CreateSeasonalParticles(type);
        }
    }

    private void ClearSeasonalParticles()
    {
        foreach (var p in _seasonalParticles)
        {
            _canvas.Children.Remove(p.Element);
        }
        _seasonalParticles.Clear();
    }

    private void CreateSeasonalParticles(SeasonalParticleType type, int count = 35)
    {
        var (width, height) = GetCanvasDimensions();

        for (int i = 0; i < count; i++)
        {
            FrameworkElement el;
            double size = 8;
            bool floatsUp = false;
            RotateTransform? rot = null;
            double rotSpeed = 0;
            double speedY = 1.0;
            double baseOpacity = 0.7;

            switch (type)
            {
                case SeasonalParticleType.TetBlossom:
                    size = _random.Next(9, 16);
                    bool isMai = _random.NextDouble() > 0.45; // 55% mai vàng, 45% đào hồng
                    var tetBrush = new RadialGradientBrush
                    {
                        Center = new Windows.Foundation.Point(0.5, 0.5),
                        RadiusX = 0.5,
                        RadiusY = 0.5
                    };
                    if (isMai)
                    {
                        tetBrush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(255, 253, 224, 71), Offset = 0 });
                        tetBrush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(220, 245, 158, 11), Offset = 1 });
                    }
                    else
                    {
                        tetBrush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(255, 244, 114, 182), Offset = 0 });
                        tetBrush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(220, 236, 72, 153), Offset = 1 });
                    }
                    var petal = new Ellipse
                    {
                        Width = size,
                        Height = size * 0.65,
                        Fill = tetBrush,
                        Opacity = _random.NextDouble() * 0.4 + 0.55
                    };
                    rot = new RotateTransform { CenterX = size / 2, CenterY = (size * 0.65) / 2 };
                    petal.RenderTransform = rot;
                    rotSpeed = _random.NextDouble() * 2.4 - 1.2;
                    speedY = _random.NextDouble() * 1.5 + 0.9;
                    baseOpacity = petal.Opacity;
                    el = petal;
                    break;

                case SeasonalParticleType.MidAutumnMoonlit:
                    size = _random.Next(5, 11);
                    floatsUp = true;
                    var moonlit = new Ellipse
                    {
                        Width = size,
                        Height = size,
                        Fill = new RadialGradientBrush
                        {
                            Center = new Windows.Foundation.Point(0.5, 0.5),
                            RadiusX = 0.5,
                            RadiusY = 0.5,
                            GradientStops =
                            {
                                new GradientStop { Color = Windows.UI.Color.FromArgb(255, 254, 240, 138), Offset = 0 },
                                new GradientStop { Color = Windows.UI.Color.FromArgb(60, 250, 204, 21), Offset = 1 }
                            }
                        },
                        Opacity = _random.NextDouble() * 0.45 + 0.45
                    };
                    speedY = -(_random.NextDouble() * 0.75 + 0.35);
                    baseOpacity = moonlit.Opacity;
                    el = moonlit;
                    break;

                case SeasonalParticleType.ChristmasSnow:
                    size = _random.Next(4, 9);
                    var snow = new Ellipse
                    {
                        Width = size,
                        Height = size,
                        Fill = new RadialGradientBrush
                        {
                            Center = new Windows.Foundation.Point(0.5, 0.5),
                            RadiusX = 0.5,
                            RadiusY = 0.5,
                            GradientStops =
                            {
                                new GradientStop { Color = Windows.UI.Color.FromArgb(255, 255, 255, 255), Offset = 0 },
                                new GradientStop { Color = Windows.UI.Color.FromArgb(100, 186, 230, 253), Offset = 1 }
                            }
                        },
                        Opacity = _random.NextDouble() * 0.4 + 0.5
                    };
                    speedY = _random.NextDouble() * 1.6 + 1.1;
                    baseOpacity = snow.Opacity;
                    el = snow;
                    break;

                case SeasonalParticleType.HalloweenSparks:
                    size = _random.Next(4, 9);
                    floatsUp = true;
                    bool isPumpkin = _random.NextDouble() > 0.4;
                    var sparkBrush = new RadialGradientBrush
                    {
                        Center = new Windows.Foundation.Point(0.5, 0.5),
                        RadiusX = 0.5,
                        RadiusY = 0.5
                    };
                    if (isPumpkin)
                    {
                        sparkBrush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(255, 251, 146, 60), Offset = 0 });
                        sparkBrush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(80, 234, 88, 12), Offset = 1 });
                    }
                    else
                    {
                        sparkBrush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(255, 192, 132, 252), Offset = 0 });
                        sparkBrush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(80, 168, 85, 247), Offset = 1 });
                    }
                    var spark = new Ellipse
                    {
                        Width = size,
                        Height = size,
                        Fill = sparkBrush,
                        Opacity = _random.NextDouble() * 0.4 + 0.5
                    };
                    speedY = -(_random.NextDouble() * 0.8 + 0.4);
                    baseOpacity = spark.Opacity;
                    el = spark;
                    break;

                case SeasonalParticleType.SpringPetals:
                    size = _random.Next(8, 14);
                    var springPetal = new Ellipse
                    {
                        Width = size,
                        Height = size * 0.65,
                        Fill = new RadialGradientBrush
                        {
                            Center = new Windows.Foundation.Point(0.5, 0.5),
                            RadiusX = 0.5,
                            RadiusY = 0.5,
                            GradientStops =
                            {
                                new GradientStop { Color = Windows.UI.Color.FromArgb(255, 251, 207, 232), Offset = 0 },
                                new GradientStop { Color = Windows.UI.Color.FromArgb(200, 244, 114, 182), Offset = 1 }
                            }
                        },
                        Opacity = _random.NextDouble() * 0.4 + 0.5
                    };
                    rot = new RotateTransform { CenterX = size / 2, CenterY = (size * 0.65) / 2 };
                    springPetal.RenderTransform = rot;
                    rotSpeed = _random.NextDouble() * 2.0 - 1.0;
                    speedY = _random.NextDouble() * 1.4 + 0.8;
                    baseOpacity = springPetal.Opacity;
                    el = springPetal;
                    break;

                case SeasonalParticleType.SummerSparkles:
                    size = _random.Next(4, 9);
                    floatsUp = true;
                    var sparkle = new Ellipse
                    {
                        Width = size,
                        Height = size,
                        Fill = new RadialGradientBrush
                        {
                            Center = new Windows.Foundation.Point(0.5, 0.5),
                            RadiusX = 0.5,
                            RadiusY = 0.5,
                            GradientStops =
                            {
                                new GradientStop { Color = Windows.UI.Color.FromArgb(255, 254, 243, 199), Offset = 0 },
                                new GradientStop { Color = Windows.UI.Color.FromArgb(60, 251, 191, 36), Offset = 1 }
                            }
                        },
                        Opacity = _random.NextDouble() * 0.4 + 0.45
                    };
                    speedY = -(_random.NextDouble() * 0.6 + 0.3);
                    baseOpacity = sparkle.Opacity;
                    el = sparkle;
                    break;

                case SeasonalParticleType.AutumnLeaves:
                    size = _random.Next(10, 16);
                    bool isAmber = _random.NextDouble() > 0.5;
                    var leafBrush = new RadialGradientBrush
                    {
                        Center = new Windows.Foundation.Point(0.5, 0.5),
                        RadiusX = 0.5,
                        RadiusY = 0.5
                    };
                    if (isAmber)
                    {
                        leafBrush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(255, 251, 191, 36), Offset = 0 });
                        leafBrush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(220, 217, 119, 6), Offset = 1 });
                    }
                    else
                    {
                        leafBrush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(255, 239, 68, 68), Offset = 0 });
                        leafBrush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(220, 185, 28, 28), Offset = 1 });
                    }
                    var leaf = new Ellipse
                    {
                        Width = size,
                        Height = size * 0.6,
                        Fill = leafBrush,
                        Opacity = _random.NextDouble() * 0.35 + 0.55
                    };
                    rot = new RotateTransform { CenterX = size / 2, CenterY = (size * 0.6) / 2 };
                    leaf.RenderTransform = rot;
                    rotSpeed = _random.NextDouble() * 2.6 - 1.3;
                    speedY = _random.NextDouble() * 1.5 + 0.9;
                    baseOpacity = leaf.Opacity;
                    el = leaf;
                    break;

                case SeasonalParticleType.WinterFrost:
                default:
                    size = _random.Next(4, 8);
                    var frost = new Ellipse
                    {
                        Width = size,
                        Height = size,
                        Fill = new RadialGradientBrush
                        {
                            Center = new Windows.Foundation.Point(0.5, 0.5),
                            RadiusX = 0.5,
                            RadiusY = 0.5,
                            GradientStops =
                            {
                                new GradientStop { Color = Windows.UI.Color.FromArgb(255, 224, 242, 254), Offset = 0 },
                                new GradientStop { Color = Windows.UI.Color.FromArgb(80, 56, 189, 248), Offset = 1 }
                            }
                        },
                        Opacity = _random.NextDouble() * 0.35 + 0.45
                    };
                    speedY = _random.NextDouble() * 1.4 + 1.0;
                    baseOpacity = frost.Opacity;
                    el = frost;
                    break;
            }

            double startX = _random.NextDouble() * width;
            double startY = _random.NextDouble() * height;

            Canvas.SetLeft(el, startX);
            Canvas.SetTop(el, startY);

            _canvas.Children.Add(el);
            _seasonalParticles.Add(new SeasonalParticle
            {
                Element = el,
                X = startX,
                Y = startY,
                SpeedY = speedY,
                DriftOffset = _random.NextDouble() * 12,
                SwayFreq = _random.NextDouble() * 1.2 + 1.2,
                BaseOpacity = baseOpacity,
                Size = size,
                FloatsUp = floatsUp,
                RotationTransform = rot,
                RotSpeed = rotSpeed
            });
        }
    }

    #endregion

    #region Main Animation Loop (60 FPS)

    private void RenderTimer_Tick(object? sender, object e)
    {
        _animationTime += 0.016;
        var (width, height) = GetCanvasDimensions();

        // 1. Cập nhật vị trí hạt mưa & kích hoạt bọt nước (splash)
        if (_raindrops.Count > 0)
        {
            foreach (var drop in _raindrops)
            {
                drop.Y += drop.Speed;
                drop.X += drop.Speed * _windDriftRatio;

                if (drop.Y >= height - 8)
                {
                    // Chạm sàn: tạo bọt nước va chạm và giọt nước tóe lên
                    if (_random.NextDouble() < 0.70)
                    {
                        TriggerSplash(drop.X, height - 6);
                    }

                    // Tái sinh hạt mưa trên đỉnh
                    drop.Y = -drop.Length - 5;
                    drop.X = _random.NextDouble() * (width + 140) - 70;
                }

                Canvas.SetLeft(drop.Element, drop.X);
                Canvas.SetTop(drop.Element, drop.Y);
            }
        }

        // 2. Cập nhật bọt nước lan tỏa & mờ dần (splash ripple)
        if (_splashes.Count > 0)
        {
            foreach (var splash in _splashes)
            {
                if (splash.IsActive)
                {
                    splash.CurrentRadiusX += splash.GrowthSpeed;
                    splash.CurrentRadiusY += splash.GrowthSpeed * 0.4;
                    splash.Opacity -= 0.045;

                    if (splash.Opacity <= 0 || splash.CurrentRadiusX >= splash.MaxRadiusX)
                    {
                        splash.IsActive = false;
                        splash.Element.Opacity = 0;
                    }
                    else
                    {
                        splash.Element.Width = splash.CurrentRadiusX * 2;
                        splash.Element.Height = splash.CurrentRadiusY * 2;
                        splash.Element.Opacity = splash.Opacity;
                        Canvas.SetLeft(splash.Element, splash.X - splash.CurrentRadiusX);
                        Canvas.SetTop(splash.Element, splash.Y - splash.CurrentRadiusY);
                    }
                }
            }
        }

        // 2b. Cập nhật giọt nước bắn tóe (splash micro droplets)
        if (_splashDroplets.Count > 0)
        {
            foreach (var droplet in _splashDroplets)
            {
                if (droplet.IsActive)
                {
                    droplet.X += droplet.Vx;
                    droplet.Y += droplet.Vy;
                    droplet.Vy += droplet.Gravity;
                    droplet.Opacity -= 0.045;

                    if (droplet.Opacity <= 0 || droplet.Y >= height)
                    {
                        droplet.IsActive = false;
                        droplet.Element.Opacity = 0;
                    }
                    else
                    {
                        droplet.Element.Opacity = droplet.Opacity;
                        Canvas.SetLeft(droplet.Element, droplet.X);
                        Canvas.SetTop(droplet.Element, droplet.Y);
                    }
                }
            }
        }

        // 3. Cập nhật hoa tuyết rơi lượn sóng đa tần
        if (_snowflakes.Count > 0)
        {
            foreach (var flake in _snowflakes)
            {
                flake.Y += flake.Speed;
                double sway = Math.Sin(_animationTime * flake.SwayFreq + flake.DriftOffset) * 1.5
                    + Math.Cos(_animationTime * 0.9) * 0.7
                    + (_windDriftRatio * 1.8);
                flake.X += sway;

                if (flake.Y > height)
                {
                    flake.Y = -flake.BaseSize - 5;
                    flake.X = _random.NextDouble() * width;
                }

                Canvas.SetLeft(flake.Element, flake.X);
                Canvas.SetTop(flake.Element, flake.Y);
            }
        }

        // 4. Cập nhật tia nắng vàng óng ánh quét qua bầu trời (Sun Rays)
        if (_sunRays.Count > 0)
        {
            foreach (var ray in _sunRays)
            {
                double breath = ray.BaseOpacity * (0.65 + 0.35 * Math.Sin(_animationTime * ray.Speed + ray.Phase));
                ray.Element.Opacity = Math.Clamp(breath, 0.05, 0.60);
                ray.Transform.Angle = ray.BaseAngle + Math.Sin(_animationTime * 0.6 + ray.Phase) * 1.8;
            }
        }

        // 5. Cập nhật bụi nắng vàng óng ánh (Sun sparkles bokeh)
        if (_sunSparkles.Count > 0)
        {
            foreach (var sp in _sunSparkles)
            {
                sp.Y -= sp.SpeedY;
                sp.X += Math.Sin(_animationTime * 1.5 + sp.DriftOffset) * 0.65;

                // Chu kỳ nhấp nháy phát sáng lung linh
                double twinkle = sp.BaseOpacity * (0.35 + 0.65 * Math.Abs(Math.Sin(_animationTime * 2.2 + sp.Phase)));
                sp.Element.Opacity = Math.Clamp(twinkle, 0.15, 0.95);

                if (sp.Y < -15)
                {
                    sp.Y = height + 10;
                    sp.X = _random.NextDouble() * width;
                }

                Canvas.SetLeft(sp.Element, sp.X);
                Canvas.SetTop(sp.Element, sp.Y);
            }
        }

        // 6. Cập nhật mây/sương mù trôi lững lờ đa tầng với sóng dập dềnh (Drifting Clouds)
        if (_clouds.Count > 0)
        {
            foreach (var cloud in _clouds)
            {
                cloud.X += cloud.SpeedX;

                if (cloud.X > width + 60)
                {
                    cloud.X = -cloud.Width - 40;
                }

                // Sóng dập dềnh lên xuống nhẹ nhàng tự nhiên
                double floatY = cloud.BaseY + Math.Sin(_animationTime * 0.8 + cloud.WavePhase) * 5.0;

                Canvas.SetLeft(cloud.Element, cloud.X);
                Canvas.SetTop(cloud.Element, floatY);
            }
        }

        // 7. Cập nhật sao đêm lung linh nhấp nháy đa tầng
        if (_nightStars.Count > 0)
        {
            foreach (var star in _nightStars)
            {
                double twinkle = star.BaseOpacity * (0.35 + 0.65 * Math.Abs(Math.Sin(_animationTime * star.TwinkleSpeed + star.TwinklePhase)));
                star.Element.Opacity = Math.Clamp(twinkle, 0.12, 0.98);
            }
        }

        // 7b. Cập nhật sao băng (Meteors)
        if (_meteors.Count > 0)
        {
            if (_animationTime >= _nextMeteorTime)
            {
                TriggerMeteor(width);
                _nextMeteorTime = _animationTime + _random.NextDouble() * 5.0 + 6.0;
            }

            foreach (var meteor in _meteors)
            {
                if (meteor.IsActive)
                {
                    meteor.X += meteor.SpeedX;
                    meteor.Y += meteor.SpeedY;
                    meteor.Opacity -= 0.045;

                    if (meteor.Opacity <= 0 || meteor.Y > height || meteor.X < -50)
                    {
                        meteor.IsActive = false;
                        meteor.Element.Opacity = 0;
                    }
                    else
                    {
                        meteor.Element.Opacity = meteor.Opacity;
                        Canvas.SetLeft(meteor.Element, meteor.X);
                        Canvas.SetTop(meteor.Element, meteor.Y);
                    }
                }
            }
        }

        // 8. Nhịp thở hữu cơ của quầng mặt trời / quầng trăng (Aura organic pulse)
        if (_ambientElements.Count > 0)
        {
            double breath = 0.80 + 0.15 * Math.Sin(_animationTime * 1.5);
            foreach (var el in _ambientElements)
            {
                el.Opacity = Math.Clamp(breath, 0.40, 0.95);
            }
        }

        // 9. Cập nhật chuyển động hạt thời tiết theo Mùa & Lễ Hội (Seasonal Particles Physics)
        if (_seasonalParticles.Count > 0 && _isSeasonalParticlesEnabled)
        {
            foreach (var p in _seasonalParticles)
            {
                if (p.FloatsUp)
                {
                    p.Y += p.SpeedY;
                    p.X += Math.Sin(_animationTime * p.SwayFreq + p.DriftOffset) * 0.75;

                    // Nhịp phát sáng nhẹ nhàng
                    double pulse = p.BaseOpacity * (0.50 + 0.50 * Math.Abs(Math.Sin(_animationTime * 2.0 + p.DriftOffset)));
                    p.Element.Opacity = Math.Clamp(pulse, 0.15, 0.95);

                    if (p.Y < -20)
                    {
                        p.Y = height + 10;
                        p.X = _random.NextDouble() * width;
                    }
                }
                else
                {
                    p.Y += p.SpeedY;
                    double sway = Math.Sin(_animationTime * p.SwayFreq + p.DriftOffset) * 1.6 + (_windDriftRatio * 1.4);
                    p.X += sway;

                    if (p.RotationTransform != null)
                    {
                        p.CurrentAngle += p.RotSpeed;
                        p.RotationTransform.Angle = p.CurrentAngle;
                    }

                    if (p.Y > height + 25)
                    {
                        p.Y = -p.Size - 10;
                        p.X = _random.NextDouble() * width;
                    }
                }

                Canvas.SetLeft(p.Element, p.X);
                Canvas.SetTop(p.Element, p.Y);
            }
        }
    }

    #endregion
}
