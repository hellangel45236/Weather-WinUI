using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Markup;
using Path = Microsoft.UI.Xaml.Shapes.Path;
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
        public double SpeedX { get; set; }
        public double DriftOffset { get; set; }
        public double SwayFreq { get; set; }
        public double SwayAmplitude { get; set; } = 1.6;
        public double BaseOpacity { get; set; }
        public double Size { get; set; }
        public bool FloatsUp { get; set; }
        public CompositeTransform? Transform { get; set; }
        public double CurrentAngle { get; set; }
        public double RotSpeed { get; set; }
        public double FlutterFreq { get; set; } = 2.0;
        public bool IsFluttering { get; set; }
        public bool IsFlapping { get; set; }
        public bool IsPulsing { get; set; }
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

    private const string BlossomFlowerPath = "M 25,12 C 29,3 37,3 41,10 C 45,17 41,24 34,26 C 42,22 50,26 51,35 C 52,44 45,49 37,46 C 42,53 38,61 30,61 C 22,61 21,52 23,45 C 22,52 21,61 13,61 C 5,61 1,53 6,46 C -2,49 -5,44 -4,35 C -3,26 5,22 13,26 C 6,24 2,17 6,10 C 10,3 18,3 22,12 Z";
    private const string BlossomPetalPath = "M 20,40 C 10,35 2,24 4,12 C 6,2 14,0 20,4 C 26,0 34,2 36,12 C 38,24 30,35 20,40 Z";
    private const string MapleLeafPath = "M 25,2 L 28,12 L 34,9 L 32,17 L 43,14 L 37,24 L 48,27 L 40,33 L 46,39 L 36,39 L 33,47 L 27,41 L 27,55 L 23,55 L 23,41 L 17,47 L 14,39 L 4,39 L 10,33 L 2,27 L 13,24 L 7,14 L 18,17 L 16,9 L 22,12 Z";
    private const string HalloweenBatPath = "M 25,12 C 26,10 27,8 28,5 C 28.5,8 29.5,10 30.5,12 C 37,9 45,9 52,14 C 47,19 44,24 46,30 C 40,28 35,30 31,34 C 30,28 27,24 25,27 C 23,24 20,28 19,34 C 15,30 10,28 4,30 C 6,24 3,19 -2,14 C 5,9 13,9 19.5,12 C 20.5,10 21.5,8 22,5 C 23,8 24,10 25,12 Z";
    private const string HalloweenGhostPath = "M 20,5 C 28,5 34,12 34,22 C 34,34 32,42 34,48 C 30,45 26,48 23,44 C 20,47 17,44 14,48 C 11,44 7,46 4,48 C 6,42 4,34 4,22 C 4,12 10,5 20,5 Z";
    private const string SnowflakeCrystalPath = "M 25,2 L 25,48 M 5,13.5 L 45,36.5 M 5,36.5 L 45,13.5 M 25,10 L 21,6 M 25,10 L 29,6 M 25,40 L 21,44 M 25,40 L 29,44 M 15,19 L 10,16 M 15,19 L 17,13 M 35,31 L 40,34 M 35,31 L 33,37 M 15,31 L 10,34 M 15,31 L 17,37 M 35,19 L 40,16 M 35,19 L 33,13";
    private const string LanternPath = "M 14,4 L 26,4 L 28,9 L 30,24 L 26,36 L 14,36 L 10,24 L 12,9 Z";
    private const string StarFlarePath = "M 25,0 Q 25,25 50,25 Q 25,25 25,50 Q 25,25 0,25 Q 25,25 25,0 Z";

    private static Path CreateVectorPath(string pathData, double size, Brush fill, Brush? stroke = null, double strokeThickness = 0, double opacity = 1.0, double heightScale = 1.0)
    {
        try
        {
            string xaml = $"<Path xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" Data=\"{pathData}\" Stretch=\"Uniform\" />";
            var p = (Path)XamlReader.Load(xaml);
            p.Width = size;
            p.Height = size * heightScale;
            p.Fill = fill;
            if (stroke != null) p.Stroke = stroke;
            if (strokeThickness > 0) p.StrokeThickness = strokeThickness;
            p.Opacity = opacity;
            return p;
        }
        catch
        {
            return new Path
            {
                Data = new EllipseGeometry { RadiusX = size / 2, RadiusY = (size * heightScale) / 2 },
                Fill = fill,
                Width = size,
                Height = size * heightScale,
                Opacity = opacity,
                Stretch = Stretch.Uniform
            };
        }
    }

    private void CreateSeasonalParticles(SeasonalParticleType type, int count = 65)
    {
        var (width, height) = GetCanvasDimensions();
        if (width <= 0) width = 800;
        if (height <= 0) height = 400;

        // Tự động phân bổ mật độ hạt tối ưu theo độ rộng màn hình (50 - 85 hạt)
        int optimalCount = Math.Clamp((int)(width / 18.0), 50, 85);

        for (int i = 0; i < optimalCount; i++)
        {
            FrameworkElement el;
            double size = 20;
            bool floatsUp = false;
            double rotSpeed = 0;
            double speedY = 1.2;
            double speedX = 0;
            double baseOpacity = 0.85;
            double swayAmplitude = 1.8;
            double flutterFreq = _random.NextDouble() * 1.5 + 1.8;
            bool isFluttering = false;
            bool isFlapping = false;
            bool isPulsing = false;

            switch (type)
            {
                case SeasonalParticleType.TetBlossom:
                    bool isFullBlossom = _random.NextDouble() > 0.65;
                    bool isMai = _random.NextDouble() > 0.45; // 55% Mai Vàng, 45% Đào Hồng
                    size = isFullBlossom ? _random.Next(20, 30) : _random.Next(15, 24);

                    Brush tetBrush;
                    if (isMai)
                    {
                        var grad = new LinearGradientBrush { StartPoint = new Windows.Foundation.Point(0, 0), EndPoint = new Windows.Foundation.Point(1, 1) };
                        grad.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(255, 254, 240, 138), Offset = 0 });
                        grad.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(255, 234, 179, 8), Offset = 0.7 });
                        grad.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(255, 202, 138, 4), Offset = 1 });
                        tetBrush = grad;
                    }
                    else
                    {
                        var grad = new LinearGradientBrush { StartPoint = new Windows.Foundation.Point(0, 0), EndPoint = new Windows.Foundation.Point(1, 1) };
                        grad.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(255, 252, 231, 243), Offset = 0 });
                        grad.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(255, 244, 114, 182), Offset = 0.6 });
                        grad.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(255, 225, 29, 72), Offset = 1 });
                        tetBrush = grad;
                    }

                    var petalPath = CreateVectorPath(
                        isFullBlossom ? BlossomFlowerPath : BlossomPetalPath,
                        size,
                        tetBrush,
                        opacity: _random.NextDouble() * 0.35 + 0.65);

                    rotSpeed = _random.NextDouble() * 2.8 - 1.4;
                    speedY = _random.NextDouble() * 1.4 + 1.0;
                    swayAmplitude = _random.NextDouble() * 2.0 + 1.4;
                    isFluttering = true;
                    baseOpacity = petalPath.Opacity;
                    el = petalPath;
                    break;

                case SeasonalParticleType.AutumnLeaves:
                    size = _random.Next(22, 36);
                    double colorRoll = _random.NextDouble();
                    Brush leafBrush;
                    if (colorRoll < 0.35)
                    {
                        // Đỏ phong rực lửa (Flame Crimson)
                        var grad = new LinearGradientBrush { StartPoint = new Windows.Foundation.Point(0, 0), EndPoint = new Windows.Foundation.Point(1, 1) };
                        grad.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(255, 248, 113, 113), Offset = 0 });
                        grad.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(255, 220, 38, 38), Offset = 0.7 });
                        grad.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(255, 153, 27, 27), Offset = 1 });
                        leafBrush = grad;
                    }
                    else if (colorRoll < 0.70)
                    {
                        // Cam hổ phách (Sunset Amber)
                        var grad = new LinearGradientBrush { StartPoint = new Windows.Foundation.Point(0, 0), EndPoint = new Windows.Foundation.Point(1, 1) };
                        grad.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(255, 253, 186, 116), Offset = 0 });
                        grad.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(255, 234, 88, 12), Offset = 0.8 });
                        leafBrush = grad;
                    }
                    else
                    {
                        // Vàng mật ong (Honey Gold)
                        var grad = new LinearGradientBrush { StartPoint = new Windows.Foundation.Point(0, 0), EndPoint = new Windows.Foundation.Point(1, 1) };
                        grad.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(255, 254, 240, 138), Offset = 0 });
                        grad.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(255, 245, 158, 11), Offset = 0.7 });
                        grad.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(255, 180, 83, 9), Offset = 1 });
                        leafBrush = grad;
                    }

                    var leafPath = CreateVectorPath(
                        MapleLeafPath,
                        size,
                        leafBrush,
                        opacity: _random.NextDouble() * 0.35 + 0.65);

                    rotSpeed = _random.NextDouble() * 3.0 - 1.5;
                    speedY = _random.NextDouble() * 1.5 + 1.1;
                    swayAmplitude = _random.NextDouble() * 2.4 + 1.6;
                    isFluttering = true;
                    baseOpacity = leafPath.Opacity;
                    el = leafPath;
                    break;

                case SeasonalParticleType.HalloweenSparks:
                    bool isBat = _random.NextDouble() > 0.45;
                    if (isBat)
                    {
                        size = _random.Next(24, 38);
                        var batBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(235, 30, 27, 75));
                        var batPath = CreateVectorPath(
                            HalloweenBatPath,
                            size,
                            batBrush,
                            opacity: _random.NextDouble() * 0.3 + 0.7,
                            heightScale: 0.6);
                        isFlapping = true;
                        flutterFreq = _random.NextDouble() * 3.0 + 4.5; // Tốc độ đập cánh nhanh
                        rotSpeed = _random.NextDouble() * 1.0 - 0.5;
                        speedY = _random.NextDouble() * 0.9 + 0.6;
                        speedX = (_random.NextDouble() * 2.0 - 1.0) * 1.5;
                        baseOpacity = batPath.Opacity;
                        el = batPath;
                    }
                    else
                    {
                        // Linh hồn ma trơi / Tia sáng bí ngô
                        size = _random.Next(16, 26);
                        floatsUp = true;
                        bool isGhost = _random.NextDouble() > 0.5;
                        Brush spiritBrush;
                        if (isGhost)
                        {
                            var grad = new RadialGradientBrush { Center = new Windows.Foundation.Point(0.5, 0.4), RadiusX = 0.5, RadiusY = 0.5 };
                            grad.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(230, 233, 213, 255), Offset = 0 });
                            grad.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(140, 168, 85, 247), Offset = 0.8 });
                            grad.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(0, 126, 34, 206), Offset = 1 });
                            spiritBrush = grad;
                        }
                        else
                        {
                            var grad = new RadialGradientBrush { Center = new Windows.Foundation.Point(0.5, 0.5), RadiusX = 0.5, RadiusY = 0.5 };
                            grad.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(255, 254, 215, 170), Offset = 0 });
                            grad.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(200, 249, 115, 22), Offset = 0.7 });
                            grad.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(0, 194, 65, 12), Offset = 1 });
                            spiritBrush = grad;
                        }

                        var ghostPath = CreateVectorPath(
                            HalloweenGhostPath,
                            size,
                            spiritBrush,
                            opacity: _random.NextDouble() * 0.35 + 0.65);
                        speedY = -(_random.NextDouble() * 1.1 + 0.6);
                        isPulsing = true;
                        isFluttering = true;
                        baseOpacity = ghostPath.Opacity;
                        el = ghostPath;
                    }
                    break;

                case SeasonalParticleType.ChristmasSnow:
                    bool isCrystal = _random.NextDouble() > 0.45;
                    size = isCrystal ? _random.Next(14, 26) : _random.Next(8, 16);

                    if (isCrystal)
                    {
                        var crystalPath = CreateVectorPath(
                            SnowflakeCrystalPath,
                            size,
                            new SolidColorBrush(Windows.UI.Color.FromArgb(20, 255, 255, 255)),
                            stroke: new SolidColorBrush(Windows.UI.Color.FromArgb(240, 255, 255, 255)),
                            strokeThickness: 1.4,
                            opacity: _random.NextDouble() * 0.4 + 0.6);
                        rotSpeed = _random.NextDouble() * 1.8 - 0.9;
                        speedY = _random.NextDouble() * 1.3 + 0.9;
                        swayAmplitude = _random.NextDouble() * 1.8 + 1.0;
                        baseOpacity = crystalPath.Opacity;
                        el = crystalPath;
                    }
                    else
                    {
                        var snowCircle = new Ellipse
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
                                    new GradientStop { Color = Windows.UI.Color.FromArgb(140, 186, 230, 253), Offset = 0.8 },
                                    new GradientStop { Color = Windows.UI.Color.FromArgb(0, 186, 230, 253), Offset = 1 }
                                }
                            },
                            Opacity = _random.NextDouble() * 0.4 + 0.55
                        };
                        speedY = _random.NextDouble() * 1.6 + 1.1;
                        swayAmplitude = _random.NextDouble() * 2.0 + 1.2;
                        baseOpacity = snowCircle.Opacity;
                        el = snowCircle;
                    }
                    break;

                case SeasonalParticleType.MidAutumnMoonlit:
                    bool isLantern = _random.NextDouble() > 0.45;
                    if (isLantern)
                    {
                        size = _random.Next(20, 32);
                        floatsUp = true;
                        var lanternBrush = new LinearGradientBrush { StartPoint = new Windows.Foundation.Point(0, 0), EndPoint = new Windows.Foundation.Point(0, 1) };
                        lanternBrush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(255, 254, 240, 138), Offset = 0 });
                        lanternBrush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(240, 245, 158, 11), Offset = 0.5 });
                        lanternBrush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(255, 220, 38, 38), Offset = 1 });

                        var lanternPath = CreateVectorPath(
                            LanternPath,
                            size * 0.75,
                            lanternBrush,
                            stroke: new SolidColorBrush(Windows.UI.Color.FromArgb(200, 251, 191, 36)),
                            strokeThickness: 1.0,
                            opacity: _random.NextDouble() * 0.35 + 0.65,
                            heightScale: 1.33);
                        speedY = -(_random.NextDouble() * 0.9 + 0.5);
                        swayAmplitude = _random.NextDouble() * 1.5 + 1.0;
                        isPulsing = true;
                        baseOpacity = lanternPath.Opacity;
                        el = lanternPath;
                    }
                    else
                    {
                        // Đom đóm & bụi sao trăng rằm
                        size = _random.Next(8, 16);
                        floatsUp = true;
                        var firefly = new Ellipse
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
                                    new GradientStop { Color = Windows.UI.Color.FromArgb(160, 250, 204, 21), Offset = 0.5 },
                                    new GradientStop { Color = Windows.UI.Color.FromArgb(0, 234, 179, 8), Offset = 1 }
                                }
                            },
                            Opacity = _random.NextDouble() * 0.45 + 0.55
                        };
                        speedY = -(_random.NextDouble() * 0.7 + 0.3);
                        swayAmplitude = _random.NextDouble() * 2.2 + 1.2;
                        isPulsing = true;
                        baseOpacity = firefly.Opacity;
                        el = firefly;
                    }
                    break;

                case SeasonalParticleType.SpringPetals:
                    size = _random.Next(16, 26);
                    var springBrush = new LinearGradientBrush { StartPoint = new Windows.Foundation.Point(0, 0), EndPoint = new Windows.Foundation.Point(1, 1) };
                    springBrush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(255, 253, 242, 248), Offset = 0 });
                    springBrush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(240, 244, 114, 182), Offset = 0.7 });
                    springBrush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(255, 236, 72, 153), Offset = 1 });

                    var springPetal = CreateVectorPath(
                        BlossomPetalPath,
                        size,
                        springBrush,
                        opacity: _random.NextDouble() * 0.35 + 0.65,
                        heightScale: 0.8);
                    rotSpeed = _random.NextDouble() * 2.6 - 1.3;
                    speedY = _random.NextDouble() * 1.3 + 0.9;
                    swayAmplitude = _random.NextDouble() * 2.2 + 1.4;
                    isFluttering = true;
                    baseOpacity = springPetal.Opacity;
                    el = springPetal;
                    break;

                case SeasonalParticleType.SummerSparkles:
                    size = _random.Next(14, 24);
                    floatsUp = true;
                    var starFlare = CreateVectorPath(
                        StarFlarePath,
                        size,
                        new LinearGradientBrush
                        {
                            StartPoint = new Windows.Foundation.Point(0, 0),
                            EndPoint = new Windows.Foundation.Point(1, 1),
                            GradientStops =
                            {
                                new GradientStop { Color = Windows.UI.Color.FromArgb(255, 254, 240, 138), Offset = 0 },
                                new GradientStop { Color = Windows.UI.Color.FromArgb(240, 245, 158, 11), Offset = 0.7 },
                                new GradientStop { Color = Windows.UI.Color.FromArgb(180, 14, 165, 233), Offset = 1 }
                            }
                        },
                        opacity: _random.NextDouble() * 0.4 + 0.6);
                    rotSpeed = _random.NextDouble() * 2.0 - 1.0;
                    speedY = -(_random.NextDouble() * 0.8 + 0.4);
                    isPulsing = true;
                    baseOpacity = starFlare.Opacity;
                    el = starFlare;
                    break;

                case SeasonalParticleType.WinterFrost:
                default:
                    size = _random.Next(12, 22);
                    var frostStar = CreateVectorPath(
                        StarFlarePath,
                        size,
                        new RadialGradientBrush
                        {
                            Center = new Windows.Foundation.Point(0.5, 0.5),
                            RadiusX = 0.5,
                            RadiusY = 0.5,
                            GradientStops =
                            {
                                new GradientStop { Color = Windows.UI.Color.FromArgb(255, 255, 255, 255), Offset = 0 },
                                new GradientStop { Color = Windows.UI.Color.FromArgb(180, 186, 230, 253), Offset = 0.6 },
                                new GradientStop { Color = Windows.UI.Color.FromArgb(0, 56, 189, 248), Offset = 1 }
                            }
                        },
                        opacity: _random.NextDouble() * 0.4 + 0.55);
                    rotSpeed = _random.NextDouble() * 1.5 - 0.75;
                    speedY = _random.NextDouble() * 1.3 + 0.8;
                    baseOpacity = frostStar.Opacity;
                    el = frostStar;
                    break;
            }

            double startX = _random.NextDouble() * width;
            double startY = _random.NextDouble() * height;

            var transform = new CompositeTransform
            {
                CenterX = size / 2.0,
                CenterY = size / 2.0
            };
            el.RenderTransform = transform;

            Canvas.SetLeft(el, startX);
            Canvas.SetTop(el, startY);

            _canvas.Children.Add(el);
            _seasonalParticles.Add(new SeasonalParticle
            {
                Element = el,
                X = startX,
                Y = startY,
                SpeedY = speedY,
                SpeedX = speedX,
                DriftOffset = _random.NextDouble() * 14,
                SwayFreq = _random.NextDouble() * 1.2 + 1.2,
                SwayAmplitude = swayAmplitude,
                BaseOpacity = baseOpacity,
                Size = size,
                FloatsUp = floatsUp,
                Transform = transform,
                RotSpeed = rotSpeed,
                FlutterFreq = flutterFreq,
                IsFluttering = isFluttering,
                IsFlapping = isFlapping,
                IsPulsing = isPulsing
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
                    p.X += Math.Sin(_animationTime * p.SwayFreq + p.DriftOffset) * p.SwayAmplitude + (_windDriftRatio * 0.8) + p.SpeedX;

                    if (p.IsPulsing)
                    {
                        // Nhịp thở phát sáng hữu cơ (Organic breathing luminescence)
                        double pulse = p.BaseOpacity * (0.55 + 0.45 * Math.Sin(_animationTime * 2.6 + p.DriftOffset));
                        p.Element.Opacity = Math.Clamp(pulse, 0.15, 1.0);
                    }

                    if (p.Transform != null)
                    {
                        if (p.IsFluttering)
                        {
                            p.CurrentAngle += p.RotSpeed;
                            p.Transform.Rotation = p.CurrentAngle;
                            p.Transform.ScaleX = 0.5 + 0.5 * Math.Cos(_animationTime * p.FlutterFreq + p.DriftOffset);
                        }
                        else
                        {
                            // Đèn lồng hoa đăng chao nghiêng nhẹ tự nhiên
                            p.Transform.Rotation = Math.Sin(_animationTime * p.SwayFreq + p.DriftOffset) * 10.0;
                        }
                    }

                    if (p.Y < -40)
                    {
                        p.Y = height + 15;
                        p.X = _random.NextDouble() * width;
                    }
                }
                else
                {
                    p.Y += p.SpeedY;
                    double sway = Math.Sin(_animationTime * p.SwayFreq + p.DriftOffset) * p.SwayAmplitude + (_windDriftRatio * 1.6);
                    p.X += sway + p.SpeedX;

                    if (p.Transform != null)
                    {
                        if (p.IsFlapping)
                        {
                            // Đập cánh dơi Halloween (Bat wing flapping)
                            p.Transform.ScaleY = 0.35 + 0.65 * Math.Abs(Math.Sin(_animationTime * p.FlutterFreq + p.DriftOffset));
                            p.Transform.Rotation = Math.Sin(_animationTime * 2.2 + p.DriftOffset) * 16.0;
                        }
                        else if (p.IsFluttering)
                        {
                            // Lật 3D chân thực cánh hoa / lá phong chao đảo (3D tumble flip)
                            p.CurrentAngle += p.RotSpeed;
                            p.Transform.Rotation = p.CurrentAngle;
                            p.Transform.ScaleX = Math.Cos(_animationTime * p.FlutterFreq + p.DriftOffset);
                        }
                        else
                        {
                            p.CurrentAngle += p.RotSpeed;
                            p.Transform.Rotation = p.CurrentAngle;
                        }
                    }

                    if (p.Y > height + 35)
                    {
                        p.Y = -p.Size - 15;
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
