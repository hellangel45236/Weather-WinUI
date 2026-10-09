using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using WeatherApp.Models;

namespace WeatherApp.Helpers;

public class WeatherEffectRenderer
{
    private readonly Canvas _canvas;
    private readonly Border _lightningOverlay;
    private readonly DispatcherTimer _renderTimer = new();
    private readonly DispatcherTimer _lightningTimer = new();
    private readonly Random _random = new();

    private WeatherEffectType _currentEffect = WeatherEffectType.ClearSunny;
    private readonly List<Raindrop> _raindrops = new();
    private readonly List<Snowflake> _snowflakes = new();
    private readonly List<FrameworkElement> _ambientElements = new();
    private double _animationTime = 0;

    private class Raindrop
    {
        public Rectangle Element { get; set; } = null!;
        public double X { get; set; }
        public double Y { get; set; }
        public double Speed { get; set; }
    }

    private class Snowflake
    {
        public Ellipse Element { get; set; } = null!;
        public double X { get; set; }
        public double Y { get; set; }
        public double Speed { get; set; }
        public double DriftOffset { get; set; }
    }

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

        _renderTimer.Interval = TimeSpan.FromMilliseconds(33); // ~30 FPS
        _renderTimer.Tick += RenderTimer_Tick;

        _lightningTimer.Interval = TimeSpan.FromSeconds(4);
        _lightningTimer.Tick += LightningTimer_Tick;
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
        if (_currentEffect == WeatherEffectType.Thunderstorm && !_lightningTimer.IsEnabled)
        {
            _lightningTimer.Start();
        }
    }

    public void SetWeatherEffect(WeatherEffectType effect)
    {
        try
        {
            _currentEffect = effect;
            ClearElements();

            switch (effect)
            {
                case WeatherEffectType.Thunderstorm:
                    CreateRaindrops(45, isHeavy: true);
                    StartLightning();
                    break;

                case WeatherEffectType.HeavyRain:
                    CreateRaindrops(35, isHeavy: true);
                    StopLightning();
                    break;

                case WeatherEffectType.ModerateRain:
                    CreateRaindrops(25, isHeavy: false);
                    StopLightning();
                    break;

                case WeatherEffectType.LightRain:
                    CreateRaindrops(15, isHeavy: false, isLight: true);
                    StopLightning();
                    break;

                case WeatherEffectType.HighUvSunny:
                    CreateSunGlow(isHighUv: true);
                    StopLightning();
                    break;

                case WeatherEffectType.ClearSunny:
                    CreateSunGlow(isHighUv: false);
                    StopLightning();
                    break;

                case WeatherEffectType.ClearNight:
                    CreateNightStars();
                    StopLightning();
                    break;

                case WeatherEffectType.Snow:
                    CreateSnowflakes(30);
                    StopLightning();
                    break;

                default:
                    StopLightning();
                    break;
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
        _snowflakes.Clear();
        _ambientElements.Clear();
        _lightningOverlay.Opacity = 0;
    }

    private void CreateRaindrops(int count, bool isHeavy = false, bool isLight = false)
    {
        double width = _canvas.ActualWidth > 100 ? _canvas.ActualWidth : 700;
        double height = _canvas.ActualHeight > 100 ? _canvas.ActualHeight : 220;

        for (int i = 0; i < count; i++)
        {
            double dropLength = isHeavy ? _random.Next(16, 26) : (isLight ? _random.Next(8, 14) : _random.Next(12, 18));
            double dropWidth = isHeavy ? 2.0 : 1.5;
            double opacity = isHeavy ? _random.NextDouble() * 0.4 + 0.35 : _random.NextDouble() * 0.3 + 0.25;

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
                        new GradientStop { Color = Windows.UI.Color.FromArgb(0, 200, 235, 255), Offset = 0 },
                        new GradientStop { Color = Windows.UI.Color.FromArgb(240, 255, 255, 255), Offset = 1 }
                    }
                },
                RenderTransform = new RotateTransform { Angle = isHeavy ? 14 : 8 }
            };

            double startX = _random.NextDouble() * (width + 100) - 50;
            double startY = _random.NextDouble() * height;
            double speed = isHeavy ? _random.Next(15, 24) : (isLight ? _random.Next(7, 12) : _random.Next(11, 17));

            Canvas.SetLeft(rect, startX);
            Canvas.SetTop(rect, startY);

            _canvas.Children.Add(rect);
            _raindrops.Add(new Raindrop
            {
                Element = rect,
                X = startX,
                Y = startY,
                Speed = speed
            });
        }
    }

    private void CreateSnowflakes(int count)
    {
        double width = _canvas.ActualWidth > 100 ? _canvas.ActualWidth : 700;
        double height = _canvas.ActualHeight > 100 ? _canvas.ActualHeight : 220;

        for (int i = 0; i < count; i++)
        {
            double size = _random.Next(3, 7);
            var circle = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new SolidColorBrush(Colors.White),
                Opacity = _random.NextDouble() * 0.5 + 0.3
            };

            double startX = _random.NextDouble() * width;
            double startY = _random.NextDouble() * height;
            double speed = _random.NextDouble() * 2 + 1.5;

            Canvas.SetLeft(circle, startX);
            Canvas.SetTop(circle, startY);

            _canvas.Children.Add(circle);
            _snowflakes.Add(new Snowflake
            {
                Element = circle,
                X = startX,
                Y = startY,
                Speed = speed,
                DriftOffset = _random.NextDouble() * 10
            });
        }
    }

    private void CreateSunGlow(bool isHighUv)
    {
        double width = _canvas.ActualWidth > 100 ? _canvas.ActualWidth : 700;
        double height = _canvas.ActualHeight > 100 ? _canvas.ActualHeight : 220;

        // Vòng phát sáng mặt trời vàng ấm hoặc đỏ cam nắng gắt
        var glowColor = isHighUv 
            ? Windows.UI.Color.FromArgb(50, 245, 100, 30) // Cam đỏ UV cao
            : Windows.UI.Color.FromArgb(40, 255, 200, 50); // Vàng ấm

        var sunAura = new Ellipse
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
                    new GradientStop { Color = glowColor, Offset = 0 },
                    new GradientStop { Color = Windows.UI.Color.FromArgb(0, 255, 200, 50), Offset = 1 }
                }
            },
            Opacity = 0.8
        };

        Canvas.SetLeft(sunAura, width - 210);
        Canvas.SetTop(sunAura, -40);

        _canvas.Children.Add(sunAura);
        _ambientElements.Add(sunAura);
    }

    private void CreateNightStars()
    {
        double width = _canvas.ActualWidth > 100 ? _canvas.ActualWidth : 700;
        double height = _canvas.ActualHeight > 100 ? _canvas.ActualHeight : 220;

        for (int i = 0; i < 20; i++)
        {
            double size = _random.Next(2, 4);
            var star = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(240, 255, 255, 220)),
                Opacity = _random.NextDouble() * 0.6 + 0.2
            };

            Canvas.SetLeft(star, _random.NextDouble() * width);
            Canvas.SetTop(star, _random.NextDouble() * height);

            _canvas.Children.Add(star);
            _ambientElements.Add(star);
        }
    }

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

        if (_currentEffect != WeatherEffectType.Thunderstorm)
        {
            _lightningOverlay.Opacity = 0;
            return;
        }

        try
        {
            // Pha 1: Tia chớp mồi nhẹ
            _lightningOverlay.Opacity = 0.35;
            await Task.Delay(50);
            if (_currentEffect != WeatherEffectType.Thunderstorm) { _lightningOverlay.Opacity = 0; return; }

            // Giảm tối chớp nhoáng
            _lightningOverlay.Opacity = 0.05;
            await Task.Delay(40);
            if (_currentEffect != WeatherEffectType.Thunderstorm) { _lightningOverlay.Opacity = 0; return; }

            // Pha 2: Chớp cực mạnh rực rỡ
            _lightningOverlay.Opacity = 0.85;
            await Task.Delay(100);
            if (_currentEffect != WeatherEffectType.Thunderstorm) { _lightningOverlay.Opacity = 0; return; }

            // Tàn dần mềm mại
            for (int i = 8; i >= 0; i--)
            {
                if (_currentEffect != WeatherEffectType.Thunderstorm) { _lightningOverlay.Opacity = 0; return; }
                _lightningOverlay.Opacity = i * 0.1;
                await Task.Delay(25);
            }
            _lightningOverlay.Opacity = 0;
        }
        catch
        {
            _lightningOverlay.Opacity = 0;
        }
    }


    private void RenderTimer_Tick(object? sender, object e)
    {
        if (!AnimationHelper.AreAnimationsEnabled || _canvas.ActualWidth <= 0 || _canvas.ActualHeight <= 0)
        {
            return;
        }

        _animationTime += 0.033;
        double height = _canvas.ActualHeight > 100 ? _canvas.ActualHeight : 220;
        double width = _canvas.ActualWidth > 100 ? _canvas.ActualWidth : 700;

        // Cập nhật vị trí hạt mưa
        if (_raindrops.Count > 0)
        {
            foreach (var drop in _raindrops)
            {
                drop.Y += drop.Speed;
                drop.X += drop.Speed * 0.15; // Hơi nghiêng nhẹ theo gió

                if (drop.Y > height)
                {
                    drop.Y = -25;
                    drop.X = _random.NextDouble() * (width + 60) - 30;
                }

                Canvas.SetLeft(drop.Element, drop.X);
                Canvas.SetTop(drop.Element, drop.Y);
            }
        }

        // Cập nhật hạt tuyết
        if (_snowflakes.Count > 0)
        {
            foreach (var flake in _snowflakes)
            {
                flake.Y += flake.Speed;
                double sway = Math.Sin(_animationTime * 2 + flake.DriftOffset) * 0.8;
                flake.X += sway;

                if (flake.Y > height)
                {
                    flake.Y = -10;
                    flake.X = _random.NextDouble() * width;
                }

                Canvas.SetLeft(flake.Element, flake.X);
                Canvas.SetTop(flake.Element, flake.Y);
            }
        }

        // Hiệu ứng thở nhẹ của vầng nắng hoặc sao đêm
        if (_ambientElements.Count > 0)
        {
            if (_currentEffect == WeatherEffectType.ClearSunny || _currentEffect == WeatherEffectType.HighUvSunny)
            {
                double breath = 0.75 + 0.15 * Math.Sin(_animationTime * 1.5);
                foreach (var el in _ambientElements)
                {
                    el.Opacity = breath;
                }
            }
        }
    }
}
