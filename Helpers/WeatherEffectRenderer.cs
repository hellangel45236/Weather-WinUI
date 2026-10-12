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
/// Bộ dựng hiệu ứng thời tiết hạt động thời gian thực (Live Particle FX Engine)
/// Hỗ trợ hạt mưa xiên theo gió, bọt nước va chạm (splash), tuyết rơi chao đảo,
/// bụi nắng vàng óng ánh (golden bokeh), mây sương trôi và chớp giật sấm sét đa tầng.
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
    private readonly List<Snowflake> _snowflakes = new();
    private readonly List<SunSparkle> _sunSparkles = new();
    private readonly List<DriftingCloud> _clouds = new();
    private readonly List<NightStar> _nightStars = new();
    private readonly List<FrameworkElement> _ambientElements = new();

    private double _animationTime = 0;

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

    private class Snowflake
    {
        public Ellipse Element { get; set; } = null!;
        public double X { get; set; }
        public double Y { get; set; }
        public double Speed { get; set; }
        public double DriftOffset { get; set; }
        public double BaseSize { get; set; }
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

    private class DriftingCloud
    {
        public Border Element { get; set; } = null!;
        public double X { get; set; }
        public double Y { get; set; }
        public double SpeedX { get; set; }
        public double Width { get; set; }
    }

    private class NightStar
    {
        public Ellipse Element { get; set; } = null!;
        public double BaseOpacity { get; set; }
        public double TwinklePhase { get; set; }
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
        if (!AnimationHelper.AreAnimationsEnabled)
        {
            Pause();
            return;
        }

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
                    CreateRaindrops(42, isHeavy: true);
                    CreateSplashPool(10);
                    StartLightning();
                    break;

                case WeatherEffectType.HeavyRain:
                    CreateRaindrops(36, isHeavy: true);
                    CreateSplashPool(10);
                    StopLightning();
                    break;

                case WeatherEffectType.ModerateRain:
                    CreateRaindrops(26, isHeavy: false);
                    CreateSplashPool(6);
                    StopLightning();
                    break;

                case WeatherEffectType.LightRain:
                    CreateRaindrops(16, isHeavy: false, isLight: true);
                    CreateSplashPool(4);
                    StopLightning();
                    break;

                case WeatherEffectType.HighUvSunny:
                    CreateSunGlow(isHighUv: true);
                    CreateSunSparkles(18);
                    StopLightning();
                    break;

                case WeatherEffectType.ClearSunny:
                    CreateSunGlow(isHighUv: false);
                    CreateSunSparkles(14);
                    StopLightning();
                    break;

                case WeatherEffectType.ClearNight:
                    CreateNightStars(24);
                    StopLightning();
                    break;

                case WeatherEffectType.Snow:
                    CreateSnowflakes(32);
                    StopLightning();
                    break;

                case WeatherEffectType.Cloudy:
                case WeatherEffectType.PartlyCloudy:
                    CreateDriftingClouds(4, isHeavyMist: false);
                    StopLightning();
                    break;

                case WeatherEffectType.Fog:
                    CreateDriftingClouds(5, isHeavyMist: true);
                    StopLightning();
                    break;

                default:
                    StopLightning();
                    break;
            }

            if (!_renderTimer.IsEnabled && AnimationHelper.AreAnimationsEnabled)
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
        _snowflakes.Clear();
        _sunSparkles.Clear();
        _clouds.Clear();
        _nightStars.Clear();
        _ambientElements.Clear();
        _lightningOverlay.Opacity = 0;
    }

    #region Element Creation Methods

    private void CreateRaindrops(int count, bool isHeavy = false, bool isLight = false)
    {
        double width = _canvas.ActualWidth > 100 ? _canvas.ActualWidth : 750;
        double height = _canvas.ActualHeight > 100 ? _canvas.ActualHeight : 240;

        for (int i = 0; i < count; i++)
        {
            double dropLength = isHeavy ? _random.Next(18, 28) : (isLight ? _random.Next(9, 14) : _random.Next(13, 20));
            double dropWidth = isHeavy ? 2.0 : (isLight ? 1.2 : 1.6);
            double opacity = isHeavy ? _random.NextDouble() * 0.35 + 0.40 : _random.NextDouble() * 0.25 + 0.25;

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
                        new GradientStop { Color = Windows.UI.Color.FromArgb(245, 240, 250, 255), Offset = 1 }
                    }
                },
                RenderTransform = new RotateTransform { Angle = _windAngleDeg }
            };

            double startX = _random.NextDouble() * (width + 120) - 60;
            double startY = _random.NextDouble() * height;
            double speed = isHeavy ? _random.Next(16, 26) : (isLight ? _random.Next(8, 13) : _random.Next(12, 19));

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
            var ripple = new Ellipse
            {
                Width = 2,
                Height = 1,
                Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(160, 230, 245, 255)),
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
                MaxRadiusX = _random.Next(7, 14),
                MaxRadiusY = _random.Next(3, 6),
                Opacity = 0,
                GrowthSpeed = _random.NextDouble() * 0.8 + 0.6,
                IsActive = false
            });
        }
    }

    private void SpawnSplash(double x, double y)
    {
        foreach (var splash in _splashes)
        {
            if (!splash.IsActive)
            {
                splash.X = x;
                splash.Y = y;
                splash.CurrentRadiusX = 2;
                splash.CurrentRadiusY = 1;
                splash.Opacity = 0.65;
                splash.IsActive = true;
                splash.Element.Width = 2;
                splash.Element.Height = 1;
                splash.Element.Opacity = 0.65;
                Canvas.SetLeft(splash.Element, x - 1);
                Canvas.SetTop(splash.Element, y);
                break;
            }
        }
    }

    private void CreateSnowflakes(int count)
    {
        double width = _canvas.ActualWidth > 100 ? _canvas.ActualWidth : 750;
        double height = _canvas.ActualHeight > 100 ? _canvas.ActualHeight : 240;

        for (int i = 0; i < count; i++)
        {
            double size = _random.Next(3, 7);
            var circle = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(230, 255, 255, 255)),
                Opacity = _random.NextDouble() * 0.5 + 0.35
            };

            double startX = _random.NextDouble() * width;
            double startY = _random.NextDouble() * height;
            double speed = _random.NextDouble() * 2.2 + 1.2;

            Canvas.SetLeft(circle, startX);
            Canvas.SetTop(circle, startY);

            _canvas.Children.Add(circle);
            _snowflakes.Add(new Snowflake
            {
                Element = circle,
                X = startX,
                Y = startY,
                Speed = speed,
                DriftOffset = _random.NextDouble() * 10,
                BaseSize = size
            });
        }
    }

    private void CreateSunGlow(bool isHighUv)
    {
        double width = _canvas.ActualWidth > 100 ? _canvas.ActualWidth : 750;

        var primaryGlowColor = isHighUv
            ? Windows.UI.Color.FromArgb(65, 255, 90, 20)  // Đỏ cam rực rỡ UV cao
            : Windows.UI.Color.FromArgb(50, 255, 195, 30); // Vàng ấm áp

        var secondaryColor = isHighUv
            ? Windows.UI.Color.FromArgb(0, 255, 120, 30)
            : Windows.UI.Color.FromArgb(0, 255, 210, 60);

        var sunAura = new Ellipse
        {
            Width = 280,
            Height = 280,
            Fill = new RadialGradientBrush
            {
                Center = new Windows.Foundation.Point(0.5, 0.5),
                RadiusX = 0.5,
                RadiusY = 0.5,
                GradientStops =
                {
                    new GradientStop { Color = primaryGlowColor, Offset = 0 },
                    new GradientStop { Color = secondaryColor, Offset = 1 }
                }
            },
            Opacity = 0.85
        };

        Canvas.SetLeft(sunAura, width - 220);
        Canvas.SetTop(sunAura, -50);

        _canvas.Children.Add(sunAura);
        _ambientElements.Add(sunAura);
    }

    private void CreateSunSparkles(int count)
    {
        double width = _canvas.ActualWidth > 100 ? _canvas.ActualWidth : 750;
        double height = _canvas.ActualHeight > 100 ? _canvas.ActualHeight : 240;

        for (int i = 0; i < count; i++)
        {
            double size = _random.Next(3, 7);
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
                        new GradientStop { Color = Windows.UI.Color.FromArgb(240, 255, 235, 150), Offset = 0 },
                        new GradientStop { Color = Windows.UI.Color.FromArgb(0, 255, 180, 50), Offset = 1 }
                    }
                },
                Opacity = _random.NextDouble() * 0.5 + 0.25
            };

            double startX = _random.NextDouble() * width;
            double startY = _random.NextDouble() * height;
            double speedY = _random.NextDouble() * 0.8 + 0.3;

            Canvas.SetLeft(sparkle, startX);
            Canvas.SetTop(sparkle, startY);

            _canvas.Children.Add(sparkle);
            _sunSparkles.Add(new SunSparkle
            {
                Element = sparkle,
                X = startX,
                Y = startY,
                SpeedY = speedY,
                DriftOffset = _random.NextDouble() * 12,
                BaseOpacity = sparkle.Opacity,
                Phase = _random.NextDouble() * Math.PI * 2
            });
        }
    }

    private void CreateDriftingClouds(int count, bool isHeavyMist)
    {
        double width = _canvas.ActualWidth > 100 ? _canvas.ActualWidth : 750;
        double height = _canvas.ActualHeight > 100 ? _canvas.ActualHeight : 240;

        for (int i = 0; i < count; i++)
        {
            double cloudWidth = _random.Next(160, 280);
            double cloudHeight = _random.Next(35, 65);
            byte alpha = isHeavyMist ? (byte)_random.Next(25, 45) : (byte)_random.Next(15, 30);

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
                Opacity = 0.7
            };

            double startX = _random.NextDouble() * (width + cloudWidth) - cloudWidth;
            double startY = _random.NextDouble() * (height - cloudHeight);
            double speedX = _random.NextDouble() * 0.4 + 0.2;

            Canvas.SetLeft(cloudBorder, startX);
            Canvas.SetTop(cloudBorder, startY);

            _canvas.Children.Add(cloudBorder);
            _clouds.Add(new DriftingCloud
            {
                Element = cloudBorder,
                X = startX,
                Y = startY,
                SpeedX = speedX,
                Width = cloudWidth
            });
        }
    }

    private void CreateNightStars(int count)
    {
        double width = _canvas.ActualWidth > 100 ? _canvas.ActualWidth : 750;
        double height = _canvas.ActualHeight > 100 ? _canvas.ActualHeight : 240;

        for (int i = 0; i < count; i++)
        {
            double size = _random.Next(2, 4);
            var star = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(250, 255, 255, 225)),
                Opacity = _random.NextDouble() * 0.6 + 0.25
            };

            Canvas.SetLeft(star, _random.NextDouble() * width);
            Canvas.SetTop(star, _random.NextDouble() * height);

            _canvas.Children.Add(star);
            _nightStars.Add(new NightStar
            {
                Element = star,
                BaseOpacity = star.Opacity,
                TwinklePhase = _random.NextDouble() * Math.PI * 2
            });
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
            _lightningOverlay.Opacity = 0.38;
            await Task.Delay(45);
            if ((_previewOverrideEffect ?? _currentEffect) != WeatherEffectType.Thunderstorm) { _lightningOverlay.Opacity = 0; return; }

            // Giảm thoáng qua
            _lightningOverlay.Opacity = 0.08;
            await Task.Delay(35);
            if ((_previewOverrideEffect ?? _currentEffect) != WeatherEffectType.Thunderstorm) { _lightningOverlay.Opacity = 0; return; }

            // Pha 2: Chớp cực mạnh
            _lightningOverlay.Opacity = 0.88;
            await Task.Delay(90);
            if ((_previewOverrideEffect ?? _currentEffect) != WeatherEffectType.Thunderstorm) { _lightningOverlay.Opacity = 0; return; }

            // Pha 3: Tàn dần êm ái
            for (int i = 7; i >= 0; i--)
            {
                if ((_previewOverrideEffect ?? _currentEffect) != WeatherEffectType.Thunderstorm) { _lightningOverlay.Opacity = 0; return; }
                _lightningOverlay.Opacity = i * 0.11;
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

    #region Main Animation Loop

    private void RenderTimer_Tick(object? sender, object e)
    {
        if (!AnimationHelper.AreAnimationsEnabled || _canvas.ActualWidth <= 0 || _canvas.ActualHeight <= 0)
        {
            return;
        }

        _animationTime += 0.016;
        double height = _canvas.ActualHeight > 100 ? _canvas.ActualHeight : 240;
        double width = _canvas.ActualWidth > 100 ? _canvas.ActualWidth : 750;

        // 1. Cập nhật vị trí hạt mưa & kích hoạt bọt nước (splash)
        if (_raindrops.Count > 0)
        {
            foreach (var drop in _raindrops)
            {
                drop.Y += drop.Speed;
                drop.X += drop.Speed * _windDriftRatio;

                if (drop.Y >= height - 12)
                {
                    // Chạm sàn: tạo bọt nước va chạm
                    if (_random.NextDouble() < 0.65)
                    {
                        SpawnSplash(drop.X, height - 6);
                    }

                    // Tái sinh hạt mưa trên đỉnh
                    drop.Y = -drop.Length - 5;
                    drop.X = _random.NextDouble() * (width + 120) - 60;
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

        // 3. Cập nhật hoa tuyết rơi lượn sóng
        if (_snowflakes.Count > 0)
        {
            foreach (var flake in _snowflakes)
            {
                flake.Y += flake.Speed;
                double sway = Math.Sin(_animationTime * 2.2 + flake.DriftOffset) * 0.85 + (_windDriftRatio * 1.5);
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

        // 4. Cập nhật bụi nắng vàng óng ánh (Sun sparkles)
        if (_sunSparkles.Count > 0)
        {
            foreach (var sp in _sunSparkles)
            {
                sp.Y -= sp.SpeedY;
                sp.X += Math.Sin(_animationTime * 1.4 + sp.DriftOffset) * 0.45;

                // Chu kỳ nhấp nháy phát sáng
                double twinkle = 0.25 + 0.55 * Math.Abs(Math.Sin(_animationTime * 2.0 + sp.Phase));
                sp.Element.Opacity = Math.Clamp(twinkle, 0.1, 0.9);

                if (sp.Y < -10)
                {
                    sp.Y = height + 10;
                    sp.X = _random.NextDouble() * width;
                }

                Canvas.SetLeft(sp.Element, sp.X);
                Canvas.SetTop(sp.Element, sp.Y);
            }
        }

        // 5. Cập nhật mây/sương mù trôi lững lờ (Drifting Clouds)
        if (_clouds.Count > 0)
        {
            foreach (var cloud in _clouds)
            {
                cloud.X += cloud.SpeedX;

                if (cloud.X > width + 50)
                {
                    cloud.X = -cloud.Width - 30;
                }

                Canvas.SetLeft(cloud.Element, cloud.X);
            }
        }

        // 6. Cập nhật sao đêm nhấp nháy
        if (_nightStars.Count > 0)
        {
            foreach (var star in _nightStars)
            {
                double twinkle = star.BaseOpacity * (0.4 + 0.6 * Math.Abs(Math.Sin(_animationTime * 1.8 + star.TwinklePhase)));
                star.Element.Opacity = Math.Clamp(twinkle, 0.1, 0.95);
            }
        }

        // 7. Nhịp thở nhẹ của vầng hào quang nắng (Sun aura breathing)
        if (_ambientElements.Count > 0)
        {
            var activeEffect = _previewOverrideEffect ?? _currentEffect;
            if (activeEffect == WeatherEffectType.ClearSunny || activeEffect == WeatherEffectType.HighUvSunny)
            {
                double breath = 0.78 + 0.14 * Math.Sin(_animationTime * 1.6);
                foreach (var el in _ambientElements)
                {
                    el.Opacity = breath;
                }
            }
        }
    }

    #endregion
}
