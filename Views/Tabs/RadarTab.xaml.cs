using System;
using System.Collections.Generic;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;
using WeatherApp.Models;
using WeatherApp.ViewModels;

namespace WeatherApp.Views.Tabs;

public sealed partial class RadarTab : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(MainViewModel), typeof(RadarTab), new PropertyMetadata(null, OnViewModelChanged));

    public MainViewModel? ViewModel
    {
        get => (MainViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RadarTab tab)
        {
            tab.UpdateTelemetryIndicators();
            tab.RenderRadarSimulation();
        }
    }

    private DispatcherTimer? _radarSweepTimer;
    private double _radarSweepAngle = 0;
    private bool _isSweepingPaused = false;
    private double _sweepSpeedMultiplier = 1.0;

    private int _selectedRangeKm = 200;
    private string _selectedLayerMode = "Precipitation"; // "Precipitation", "Wind", "Lightning", "Thermal"

    public RadarTab()
    {
        this.InitializeComponent();
        this.Loaded += (s, e) =>
        {
            UpdateTelemetryIndicators();
            StartRadarSweep();
        };
        this.Unloaded += (s, e) => StopRadarSweep();
    }

    public void StartRadarSweep()
    {
        if (_radarSweepTimer == null)
        {
            _radarSweepTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
            _radarSweepTimer.Tick += (s, e) =>
            {
                if (!_isSweepingPaused)
                {
                    _radarSweepAngle = (_radarSweepAngle + (3.0 * _sweepSpeedMultiplier)) % 360;
                    RenderRadarSimulation();
                }
            };
        }

        if (!_radarSweepTimer.IsEnabled)
        {
            _radarSweepTimer.Start();
        }
        RenderRadarSimulation();
    }

    public void StopRadarSweep()
    {
        _radarSweepTimer?.Stop();
    }

    private void RadarSimulationCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        RenderRadarSimulation();
    }

    #region INTERACTIVE CONTROLS (RANGE, LAYER, PAUSE, SPEED)

    private void TogglePause_Click(object sender, RoutedEventArgs e)
    {
        _isSweepingPaused = !_isSweepingPaused;

        if (_isSweepingPaused)
        {
            TogglePauseIcon.Glyph = "\uf04b"; // Play icon
            TogglePauseText.Text = "Tiếp tục";
            StatusBadgeBorder.Background = new SolidColorBrush(Color.FromArgb(40, 245, 158, 11));
            StatusBadgeBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(90, 245, 158, 11));
            StatusDot.Fill = new SolidColorBrush(Color.FromArgb(255, 245, 158, 11));
            StatusBadgeText.Text = "Đã Tạm Dừng";
            StatusBadgeText.Foreground = new SolidColorBrush(Color.FromArgb(255, 245, 158, 11));
        }
        else
        {
            TogglePauseIcon.Glyph = "\uf04c"; // Pause icon
            TogglePauseText.Text = "Tạm dừng";
            StatusBadgeBorder.Background = new SolidColorBrush(Color.FromArgb(32, 16, 185, 129));
            StatusBadgeBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(70, 16, 185, 129));
            StatusDot.Fill = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129));
            StatusBadgeText.Text = "Đang Quét 360°";
            StatusBadgeText.Foreground = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129));
        }
    }

    private void ToggleSpeed_Click(object sender, RoutedEventArgs e)
    {
        if (_sweepSpeedMultiplier == 1.0)
            _sweepSpeedMultiplier = 2.0;
        else if (_sweepSpeedMultiplier == 2.0)
            _sweepSpeedMultiplier = 0.5;
        else
            _sweepSpeedMultiplier = 1.0;

        SpeedText.Text = $"Tốc độ: {_sweepSpeedMultiplier}x";
    }

    private void RangeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && int.TryParse(btn.Tag?.ToString(), out int range))
        {
            _selectedRangeKm = range;

            Button[] rangeButtons = { BtnRange100, BtnRange200, BtnRange300, BtnRange500 };
            foreach (var b in rangeButtons)
            {
                if (b == null) continue;
                bool isTarget = (b == btn);
                b.FontWeight = isTarget ? Microsoft.UI.Text.FontWeights.Bold : Microsoft.UI.Text.FontWeights.SemiBold;
                b.Background = isTarget
                    ? new SolidColorBrush(Color.FromArgb(55, 56, 189, 248))
                    : (Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"];
                b.BorderBrush = isTarget ? new SolidColorBrush(Color.FromArgb(255, 56, 189, 248)) : null;
                b.BorderThickness = new Thickness(isTarget ? 1 : 0);
            }

            RenderRadarSimulation();
        }
    }

    private void LayerButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string layer)
        {
            _selectedLayerMode = layer;

            Button[] layerButtons = { BtnLayerPrecip, BtnLayerWind, BtnLayerLightning, BtnLayerThermal };
            foreach (var b in layerButtons)
            {
                if (b == null) continue;
                bool isTarget = (b == btn);
                b.FontWeight = isTarget ? Microsoft.UI.Text.FontWeights.Bold : Microsoft.UI.Text.FontWeights.SemiBold;

                Color activeColor = layer switch
                {
                    "Wind" => Color.FromArgb(255, 56, 189, 248),
                    "Lightning" => Color.FromArgb(255, 245, 158, 11),
                    "Thermal" => Color.FromArgb(255, 239, 68, 68),
                    _ => Color.FromArgb(255, 16, 185, 129)
                };

                b.Background = isTarget
                    ? new SolidColorBrush(Color.FromArgb(50, activeColor.R, activeColor.G, activeColor.B))
                    : (Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"];
                b.BorderBrush = isTarget ? new SolidColorBrush(activeColor) : null;
                b.BorderThickness = new Thickness(isTarget ? 1 : 0);
            }

            RenderRadarSimulation();
        }
    }

    #endregion

    #region TELEMETRY DATA BINDING

    private void UpdateTelemetryIndicators()
    {
        if (ViewModel?.CurrentWeather == null) return;

        var effect = ViewModel.CurrentWeather.WeatherEffect;
        int maxDbz = effect switch
        {
            WeatherEffectType.Thunderstorm => 62,
            WeatherEffectType.HeavyRain => 54,
            WeatherEffectType.ModerateRain => 42,
            WeatherEffectType.LightRain => 30,
            WeatherEffectType.Cloudy => 22,
            _ => 15
        };

        if (MaxDbzValueText != null)
        {
            MaxDbzValueText.Text = $"{maxDbz} dBZ";
            MaxDbzValueText.Foreground = maxDbz >= 50
                ? new SolidColorBrush(Color.FromArgb(255, 239, 68, 68))
                : (maxDbz >= 35
                    ? new SolidColorBrush(Color.FromArgb(255, 245, 158, 11))
                    : new SolidColorBrush(Color.FromArgb(255, 56, 189, 248)));
        }

        if (MaxDbzStatusText != null)
        {
            MaxDbzStatusText.Text = maxDbz switch
            {
                >= 55 => "Mây dông đối lưu cực mạnh",
                >= 45 => "Mưa to diện rộng",
                >= 35 => "Mưa rào đối lưu cục bộ",
                >= 25 => "Mưa phùn rải rác",
                _ => "Phản hồi mây mỏng bình thường"
            };
        }

        if (RiskLevelText != null)
        {
            RiskLevelText.Text = effect switch
            {
                WeatherEffectType.Thunderstorm => "Cảnh Báo Dông Sét",
                WeatherEffectType.HeavyRain => "Nguy Cơ Ngập Úng",
                WeatherEffectType.HighUvSunny => "Nắng Gắt - UV Cao",
                _ => "Bình Thường"
            };

            RiskLevelText.Foreground = effect switch
            {
                WeatherEffectType.Thunderstorm => new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)),
                WeatherEffectType.HeavyRain or WeatherEffectType.HighUvSunny => new SolidColorBrush(Color.FromArgb(255, 245, 158, 11)),
                _ => new SolidColorBrush(Color.FromArgb(255, 16, 185, 129))
            };
        }

        if (RiskDetailText != null)
        {
            RiskDetailText.Text = effect switch
            {
                WeatherEffectType.Thunderstorm => "Có thể xuất hiện sét đánh & gió giật mạnh",
                WeatherEffectType.HeavyRain => "Lượng mưa lớn cục bộ trong 1-2h tới",
                _ => "Không có áp thấp nhiệt đới hay lốc xoáy"
            };
        }
    }

    #endregion

    #region RADAR CANVAS RENDERING

    public void RenderRadarSimulation()
    {
        try
        {
            if (RadarSimulationCanvas == null) return;
            RadarSimulationCanvas.Children.Clear();

            double w = RadarSimulationCanvas.ActualWidth;
            double h = RadarSimulationCanvas.ActualHeight;
            if (w <= 20 || h <= 20) return;

            double cx = w / 2;
            double cy = h / 2;
            double maxRadius = Math.Min(cx, cy) - 24;
            if (maxRadius <= 15) return;

            // 1. Phông nền Scope Radar tròn tối màu
            var scopeBg = new Ellipse
            {
                Width = maxRadius * 2,
                Height = maxRadius * 2,
                Fill = new SolidColorBrush(Color.FromArgb(240, 8, 14, 26)),
                Stroke = new SolidColorBrush(Color.FromArgb(120, 56, 189, 248)),
                StrokeThickness = 2.0
            };
            Canvas.SetLeft(scopeBg, cx - maxRadius);
            Canvas.SetTop(scopeBg, cy - maxRadius);
            RadarSimulationCanvas.Children.Add(scopeBg);

            // 2. Vòng tròn cự ly đồng tâm (Concentric Range Rings thích ứng theo _selectedRangeKm)
            int stepKm = _selectedRangeKm / 4;
            for (int i = 1; i <= 4; i++)
            {
                double r = maxRadius * (i / 4.0);
                var circle = new Ellipse
                {
                    Width = r * 2,
                    Height = r * 2,
                    Stroke = new SolidColorBrush(Color.FromArgb((byte)(i == 4 ? 90 : 50), 56, 189, 248)),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 3, 3 }
                };
                Canvas.SetLeft(circle, cx - r);
                Canvas.SetTop(circle, cy - r);
                RadarSimulationCanvas.Children.Add(circle);

                // Thẻ ghi số km cự ly
                var kmBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(180, 15, 23, 42)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(80, 56, 189, 248)),
                    BorderThickness = new Thickness(0.5),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(5, 1, 5, 1)
                };
                kmBorder.Child = new TextBlock
                {
                    Text = $"{i * stepKm} km",
                    FontSize = 9,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromArgb(220, 56, 189, 248))
                };
                Canvas.SetLeft(kmBorder, cx + 5);
                Canvas.SetTop(kmBorder, cy - r - 9);
                RadarSimulationCanvas.Children.Add(kmBorder);
            }

            // 3. Các nan hoa góc tọa độ la bàn (Cardinal Spoke Lines 8 hướng)
            string[] cardinalLabels = { "BẮC (0°)", "ĐB (45°)", "ĐÔNG (90°)", "ĐN (135°)", "NAM (180°)", "TN (225°)", "TÂY (270°)", "TB (315°)" };
            for (int i = 0; i < 8; i++)
            {
                double deg = i * 45;
                double rad = (deg - 90) * Math.PI / 180.0;
                double xEnd = cx + maxRadius * Math.Cos(rad);
                double yEnd = cy + maxRadius * Math.Sin(rad);

                var spoke = new Line
                {
                    X1 = cx,
                    Y1 = cy,
                    X2 = xEnd,
                    Y2 = yEnd,
                    Stroke = new SolidColorBrush(Color.FromArgb((byte)(i % 2 == 0 ? 55 : 25), 255, 255, 255)),
                    StrokeThickness = 1,
                    StrokeDashArray = i % 2 == 0 ? null : new DoubleCollection { 2, 4 }
                };
                RadarSimulationCanvas.Children.Add(spoke);

                if (i % 2 == 0)
                {
                    var cTb = new TextBlock
                    {
                        Text = cardinalLabels[i],
                        FontSize = 9.5,
                        FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromArgb(200, 148, 163, 184))
                    };
                    double lx = cx + (maxRadius + 8) * Math.Cos(rad) - 22;
                    double ly = cy + (maxRadius + 8) * Math.Sin(rad) - 7;
                    Canvas.SetLeft(cTb, Math.Clamp(lx, 2, w - 55));
                    Canvas.SetTop(cTb, Math.Clamp(ly, 2, h - 18));
                    RadarSimulationCanvas.Children.Add(cTb);
                }
            }

            // 4. Vẽ tầng dữ liệu khí tượng được chọn (_selectedLayerMode)
            switch (_selectedLayerMode)
            {
                case "Wind":
                    RenderWindVectorsLayer(cx, cy, maxRadius);
                    break;
                case "Lightning":
                    RenderLightningLayer(cx, cy, maxRadius);
                    break;
                case "Thermal":
                    RenderThermalLayer(cx, cy, maxRadius);
                    break;
                default:
                    RenderPrecipitationEchoesLayer(cx, cy, maxRadius);
                    break;
            }

            // 5. Địa danh vệ tinh các tỉnh lân cận theo vùng miền (Bắc, Trung, Tây Nguyên, Nam)
            RenderRegionalLandmarks(cx, cy, maxRadius);

            // 6. Chùm tia quét Doppler xoay 360° phát sáng (Phosphor Sweep Beam & Tail)
            RenderSweepBeam(cx, cy, maxRadius);

            // 7. Trạm Radar trung tâm (Center Radar Beacon)
            RenderCenterBeacon(cx, cy);
        }
        catch { }
    }

    private void RenderPrecipitationEchoesLayer(double cx, double cy, double maxRadius)
    {
        var effect = ViewModel?.CurrentWeather?.WeatherEffect ?? WeatherEffectType.ClearSunny;
        double scale = 200.0 / _selectedRangeKm; // Co giãn theo cự ly

        // Tùy theo thời tiết: mưa to/dông thì nhiều đám mây đỏ vàng, nắng thì mây mỏng xanh
        if (effect == WeatherEffectType.Thunderstorm || effect == WeatherEffectType.HeavyRain)
        {
            // Cụm mây dông tây nam
            DrawStormCell(cx - maxRadius * 0.40 * scale, cy + maxRadius * 0.32 * scale, 130 * scale, 90 * scale, true);
            // Cụm mây đông bắc
            DrawStormCell(cx + maxRadius * 0.48 * scale, cy - maxRadius * 0.42 * scale, 100 * scale, 75 * scale, true);
            // Cụm mây cận trung tâm
            DrawStormCell(cx - maxRadius * 0.15 * scale, cy - maxRadius * 0.18 * scale, 75 * scale, 55 * scale, true);
        }
        else if (effect == WeatherEffectType.ModerateRain || effect == WeatherEffectType.LightRain)
        {
            // Mưa vừa / mưa rào rải rác
            DrawRainCell(cx - maxRadius * 0.38 * scale, cy + maxRadius * 0.30 * scale, 95 * scale, 65 * scale);
            DrawRainCell(cx + maxRadius * 0.42 * scale, cy - maxRadius * 0.35 * scale, 75 * scale, 50 * scale);
            DrawRainCell(cx - maxRadius * 0.60 * scale, cy - maxRadius * 0.15 * scale, 65 * scale, 45 * scale);
        }
        else
        {
            // Quang mây / nắng: mây đối lưu mỏng, cường độ nhẹ
            DrawLightCloudCell(cx - maxRadius * 0.45 * scale, cy + maxRadius * 0.40 * scale, 70 * scale, 50 * scale);
            DrawLightCloudCell(cx + maxRadius * 0.50 * scale, cy - maxRadius * 0.35 * scale, 60 * scale, 40 * scale);
        }
    }

    private void DrawStormCell(double x, double y, double w, double h, bool severe)
    {
        // Vòng ngoài: Xanh lá (20-30 dBZ)
        var out1 = new Ellipse { Width = w, Height = h, Fill = new SolidColorBrush(Color.FromArgb(70, 16, 185, 129)) };
        Canvas.SetLeft(out1, x - w / 2); Canvas.SetTop(out1, y - h / 2);
        RadarSimulationCanvas.Children.Add(out1);

        // Vòng giữa: Vàng cam (35-45 dBZ)
        double w2 = w * 0.65; double h2 = h * 0.65;
        var mid = new Ellipse { Width = w2, Height = h2, Fill = new SolidColorBrush(Color.FromArgb(100, 245, 158, 11)) };
        Canvas.SetLeft(mid, x - w2 / 2); Canvas.SetTop(mid, y - h2 / 2);
        RadarSimulationCanvas.Children.Add(mid);

        // Lõi dông: Đỏ thẫm (50-65 dBZ)
        if (severe)
        {
            double w3 = w * 0.35; double h3 = h * 0.35;
            var core = new Ellipse { Width = w3, Height = h3, Fill = new SolidColorBrush(Color.FromArgb(140, 239, 68, 68)) };
            Canvas.SetLeft(core, x - w3 / 2); Canvas.SetTop(core, y - h3 / 2);
            RadarSimulationCanvas.Children.Add(core);

            // Tâm dông tím
            double w4 = w * 0.15; double h4 = h * 0.15;
            var eye = new Ellipse { Width = w4, Height = h4, Fill = new SolidColorBrush(Color.FromArgb(180, 168, 85, 247)) };
            Canvas.SetLeft(eye, x - w4 / 2); Canvas.SetTop(eye, y - h4 / 2);
            RadarSimulationCanvas.Children.Add(eye);
        }
    }

    private void DrawRainCell(double x, double y, double w, double h)
    {
        var out1 = new Ellipse { Width = w, Height = h, Fill = new SolidColorBrush(Color.FromArgb(60, 56, 189, 248)) };
        Canvas.SetLeft(out1, x - w / 2); Canvas.SetTop(out1, y - h / 2);
        RadarSimulationCanvas.Children.Add(out1);

        double w2 = w * 0.6; double h2 = h * 0.6;
        var mid = new Ellipse { Width = w2, Height = h2, Fill = new SolidColorBrush(Color.FromArgb(85, 16, 185, 129)) };
        Canvas.SetLeft(mid, x - w2 / 2); Canvas.SetTop(mid, y - h2 / 2);
        RadarSimulationCanvas.Children.Add(mid);
    }

    private void DrawLightCloudCell(double x, double y, double w, double h)
    {
        var out1 = new Ellipse { Width = w, Height = h, Fill = new SolidColorBrush(Color.FromArgb(45, 56, 189, 248)) };
        Canvas.SetLeft(out1, x - w / 2); Canvas.SetTop(out1, y - h / 2);
        RadarSimulationCanvas.Children.Add(out1);
    }

    private void RenderWindVectorsLayer(double cx, double cy, double maxRadius)
    {
        // Vẽ lưới các mũi tên véc-tơ luồng khí xoáy đối lưu
        int rings = 3;
        for (int r = 1; r <= rings; r++)
        {
            double radius = maxRadius * (r / (double)(rings + 1));
            int arrowCount = r * 6;
            for (int i = 0; i < arrowCount; i++)
            {
                double angleDeg = (i * (360.0 / arrowCount)) + (_radarSweepAngle * 0.25);
                double rad = angleDeg * Math.PI / 180.0;
                double ax = cx + radius * Math.Cos(rad);
                double ay = cy + radius * Math.Sin(rad);

                // Hướng mũi tên tiếp tuyến theo vòng xoáy
                double tangentDeg = angleDeg + 80;
                double tRad = tangentDeg * Math.PI / 180.0;
                double arrowLen = 14;

                var arrowLine = new Line
                {
                    X1 = ax,
                    Y1 = ay,
                    X2 = ax + arrowLen * Math.Cos(tRad),
                    Y2 = ay + arrowLen * Math.Sin(tRad),
                    Stroke = new SolidColorBrush(Color.FromArgb(160, 56, 189, 248)),
                    StrokeThickness = 1.5
                };
                RadarSimulationCanvas.Children.Add(arrowLine);

                // Mũi nhọn
                var arrowDot = new Ellipse
                {
                    Width = 3.5,
                    Height = 3.5,
                    Fill = new SolidColorBrush(Color.FromArgb(220, 56, 189, 248))
                };
                Canvas.SetLeft(arrowDot, ax + arrowLen * Math.Cos(tRad) - 1.75);
                Canvas.SetTop(arrowDot, ay + arrowLen * Math.Sin(tRad) - 1.75);
                RadarSimulationCanvas.Children.Add(arrowDot);
            }
        }
    }

    private void RenderLightningLayer(double cx, double cy, double maxRadius)
    {
        double scale = 200.0 / _selectedRangeKm;
        // Điểm dông bão phát sét 1
        DrawLightningStrikeBeacon(cx - maxRadius * 0.40 * scale, cy + maxRadius * 0.32 * scale, "Tâm Dông #1 (42km)");
        // Điểm dông bão phát sét 2
        DrawLightningStrikeBeacon(cx + maxRadius * 0.48 * scale, cy - maxRadius * 0.42 * scale, "Tâm Dông #2 (68km)");
    }

    private void DrawLightningStrikeBeacon(double x, double y, string label)
    {
        // Vòng sóng phát thanh
        var pulse = new Ellipse
        {
            Width = 32,
            Height = 32,
            Stroke = new SolidColorBrush(Color.FromArgb(180, 245, 158, 11)),
            StrokeThickness = 1.5,
            Fill = new SolidColorBrush(Color.FromArgb(40, 245, 158, 11))
        };
        Canvas.SetLeft(pulse, x - 16); Canvas.SetTop(pulse, y - 16);
        RadarSimulationCanvas.Children.Add(pulse);

        // Biểu tượng tia chớp
        var boltIcon = new TextBlock
        {
            Text = "⚡",
            FontSize = 13,
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
            VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center
        };
        Canvas.SetLeft(boltIcon, x - 7); Canvas.SetTop(boltIcon, y - 10);
        RadarSimulationCanvas.Children.Add(boltIcon);

        // Nhãn cảnh báo
        var tagBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(200, 15, 23, 42)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(120, 245, 158, 11)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(4, 1, 4, 1)
        };
        tagBorder.Child = new TextBlock
        {
            Text = label,
            FontSize = 8.5,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 245, 158, 11))
        };
        Canvas.SetLeft(tagBorder, x + 12); Canvas.SetTop(tagBorder, y - 9);
        RadarSimulationCanvas.Children.Add(tagBorder);
    }

    private void RenderThermalLayer(double cx, double cy, double maxRadius)
    {
        double temp = ViewModel?.CurrentWeather?.TemperatureValue ?? 30.0;
        // Các vòng gradient nhiệt độ
        Color heatColor = temp >= 32
            ? Color.FromArgb(70, 239, 68, 68)
            : (temp >= 26
                ? Color.FromArgb(70, 245, 158, 11)
                : Color.FromArgb(70, 16, 185, 129));

        for (int i = 3; i >= 1; i--)
        {
            double r = maxRadius * (i / 3.5);
            byte alpha = (byte)(30 + (4 - i) * 15);
            var heatCircle = new Ellipse
            {
                Width = r * 2,
                Height = r * 2,
                Fill = new SolidColorBrush(Color.FromArgb(alpha, heatColor.R, heatColor.G, heatColor.B)),
                Stroke = new SolidColorBrush(Color.FromArgb((byte)(alpha + 30), heatColor.R, heatColor.G, heatColor.B)),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 4 }
            };
            Canvas.SetLeft(heatCircle, cx - r); Canvas.SetTop(heatCircle, cy - r);
            RadarSimulationCanvas.Children.Add(heatCircle);

            var tempLabel = new TextBlock
            {
                Text = $"{Math.Round(temp - (3 - i) * 1.5)}°C",
                FontSize = 9,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = new SolidColorBrush(heatColor)
            };
            Canvas.SetLeft(tempLabel, cx + r - 15); Canvas.SetTop(tempLabel, cy - 8);
            RadarSimulationCanvas.Children.Add(tempLabel);
        }
    }

    private void RenderRegionalLandmarks(double cx, double cy, double maxRadius)
    {
        string locName = ViewModel?.CurrentWeather?.LocationName ?? "";

        // Tự động phân loại vùng miền Việt Nam
        bool isCentral = locName.Contains("Đà Nẵng", StringComparison.OrdinalIgnoreCase) ||
                         locName.Contains("Huế", StringComparison.OrdinalIgnoreCase) ||
                         locName.Contains("Hội An", StringComparison.OrdinalIgnoreCase) ||
                         locName.Contains("Quy Nhơn", StringComparison.OrdinalIgnoreCase) ||
                         locName.Contains("Quảng Nam", StringComparison.OrdinalIgnoreCase) ||
                         locName.Contains("Quảng Ngãi", StringComparison.OrdinalIgnoreCase) ||
                         locName.Contains("Nha Trang", StringComparison.OrdinalIgnoreCase);

        bool isHighlands = locName.Contains("Đà Lạt", StringComparison.OrdinalIgnoreCase) ||
                           locName.Contains("Pleiku", StringComparison.OrdinalIgnoreCase) ||
                           locName.Contains("Buôn Ma Thuột", StringComparison.OrdinalIgnoreCase) ||
                           locName.Contains("Lâm Đồng", StringComparison.OrdinalIgnoreCase) ||
                           locName.Contains("Kon Tum", StringComparison.OrdinalIgnoreCase);

        bool isSouth = locName.Contains("Hồ Chí Minh", StringComparison.OrdinalIgnoreCase) ||
                       locName.Contains("Sài Gòn", StringComparison.OrdinalIgnoreCase) ||
                       locName.Contains("Cần Thơ", StringComparison.OrdinalIgnoreCase) ||
                       locName.Contains("Vũng Tàu", StringComparison.OrdinalIgnoreCase) ||
                       locName.Contains("Bình Dương", StringComparison.OrdinalIgnoreCase) ||
                       locName.Contains("Long An", StringComparison.OrdinalIgnoreCase) ||
                       locName.Contains("Tiền Giang", StringComparison.OrdinalIgnoreCase) ||
                       locName.Contains("Phú Quốc", StringComparison.OrdinalIgnoreCase);

        (string Name, double DistKm, double BearingDeg)[] landmarks;

        if (isCentral)
        {
            landmarks = new[]
            {
                ("Hội An", 28.0, 150.0),
                ("Bà Nà Hills", 30.0, 260.0),
                ("Lăng Cô", 35.0, 340.0),
                ("Cù Lao Chàm", 38.0, 110.0),
                ("Tam Kỳ", 65.0, 160.0),
                ("Huế", 88.0, 325.0),
                ("Quảng Ngãi", 125.0, 155.0),
                ("Quy Nhơn", 240.0, 160.0)
            };
        }
        else if (isHighlands)
        {
            landmarks = new[]
            {
                ("Lạc Dương", 20.0, 15.0),
                ("Đức Trọng", 35.0, 195.0),
                ("Đơn Dương", 40.0, 140.0),
                ("Di Linh", 75.0, 215.0),
                ("Bảo Lộc", 110.0, 220.0),
                ("Nha Trang", 135.0, 80.0),
                ("Buôn Ma Thuột", 160.0, 345.0)
            };
        }
        else if (isSouth)
        {
            landmarks = new[]
            {
                ("Bình Dương", 30.0, 20.0),
                ("Biên Hòa", 32.0, 65.0),
                ("Long An", 45.0, 240.0),
                ("Cần Giờ", 48.0, 175.0),
                ("Tiền Giang", 65.0, 215.0),
                ("Tây Ninh", 85.0, 310.0),
                ("Vũng Tàu", 95.0, 130.0),
                ("Cần Thơ", 130.0, 220.0)
            };
        }
        else // Miền Bắc
        {
            landmarks = new[]
            {
                ("Bắc Ninh", 32.0, 40.0),
                ("Vĩnh Phúc", 55.0, 315.0),
                ("Hưng Yên", 55.0, 135.0),
                ("Hải Dương", 58.0, 95.0),
                ("Thái Nguyên", 75.0, 355.0),
                ("Hòa Bình", 75.0, 245.0),
                ("Hải Phòng", 102.0, 98.0),
                ("Nam Định", 115.0, 155.0)
            };
        }

        foreach (var (cityName, distKm, bearingDeg) in landmarks)
        {
            // Chỉ vẽ những địa danh nằm trong bán kính quan sát được chọn
            if (distKm > _selectedRangeKm) continue;

            double distRatio = distKm / _selectedRangeKm;
            double bRad = (bearingDeg - 90) * Math.PI / 180.0;
            double lmX = cx + (maxRadius * distRatio) * Math.Cos(bRad);
            double lmY = cy + (maxRadius * distRatio) * Math.Sin(bRad);

            var lmDot = new Ellipse
            {
                Width = 5.5,
                Height = 5.5,
                Fill = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)),
                Stroke = new SolidColorBrush(Color.FromArgb(160, 56, 189, 248)),
                StrokeThickness = 1
            };
            Canvas.SetLeft(lmDot, lmX - 2.75); Canvas.SetTop(lmDot, lmY - 2.75);
            RadarSimulationCanvas.Children.Add(lmDot);

            var lmText = new TextBlock
            {
                Text = cityName,
                FontSize = 9,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromArgb(180, 203, 213, 225))
            };
            Canvas.SetLeft(lmText, lmX + 5); Canvas.SetTop(lmText, lmY - 6);
            RadarSimulationCanvas.Children.Add(lmText);
        }
    }

    private void RenderSweepBeam(double cx, double cy, double maxRadius)
    {
        double sweepRad = (_radarSweepAngle - 90) * Math.PI / 180.0;
        const int TRAIL_SLICES = 14;
        const double SLICE_ANGLE_DEG = 2.8;

        for (int s = TRAIL_SLICES; s >= 1; s--)
        {
            double a1 = (sweepRad - (s * SLICE_ANGLE_DEG * Math.PI / 180.0));
            double a2 = (sweepRad - ((s - 1) * SLICE_ANGLE_DEG * Math.PI / 180.0));

            var wedge = new Polygon();
            byte alpha = (byte)Math.Max(3, (int)(85 * (1.0 - (double)s / TRAIL_SLICES)));
            wedge.Fill = new SolidColorBrush(Color.FromArgb(alpha, 16, 185, 129));

            wedge.Points.Add(new Point(cx, cy));
            wedge.Points.Add(new Point(cx + maxRadius * Math.Cos(a1), cy + maxRadius * Math.Sin(a1)));
            wedge.Points.Add(new Point(cx + maxRadius * Math.Cos(a2), cy + maxRadius * Math.Sin(a2)));
            RadarSimulationCanvas.Children.Add(wedge);
        }

        // Chùm tia quét dẫn đầu (Leading Line)
        double beamX = cx + maxRadius * Math.Cos(sweepRad);
        double beamY = cy + maxRadius * Math.Sin(sweepRad);
        var sweepBeam = new Line
        {
            X1 = cx,
            Y1 = cy,
            X2 = beamX,
            Y2 = beamY,
            Stroke = new SolidColorBrush(Color.FromArgb(240, 52, 211, 153)),
            StrokeThickness = 2.0
        };
        RadarSimulationCanvas.Children.Add(sweepBeam);
    }

    private void RenderCenterBeacon(double cx, double cy)
    {
        // Vòng xung phát xạ trạm trung tâm
        var centerPulse = new Ellipse
        {
            Width = 18,
            Height = 18,
            Stroke = new SolidColorBrush(Color.FromArgb(180, 56, 189, 248)),
            StrokeThickness = 1.5,
            Fill = new SolidColorBrush(Color.FromArgb(40, 56, 189, 248))
        };
        Canvas.SetLeft(centerPulse, cx - 9); Canvas.SetTop(centerPulse, cy - 9);
        RadarSimulationCanvas.Children.Add(centerPulse);

        // Chấm tròn tâm trạm
        var centerDot = new Ellipse
        {
            Width = 8,
            Height = 8,
            Fill = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129)),
            Stroke = new SolidColorBrush(Colors.White),
            StrokeThickness = 1.5
        };
        Canvas.SetLeft(centerDot, cx - 4); Canvas.SetTop(centerDot, cy - 4);
        RadarSimulationCanvas.Children.Add(centerDot);

        // Tên đài trung tâm
        string locName = ViewModel?.CurrentWeather?.LocationName ?? "";
        string centerName = string.IsNullOrWhiteSpace(locName) ? "TRẠM CHÍNH" : locName.ToUpper();
        var centerLabel = new TextBlock
        {
            Text = $"● {centerName}",
            FontSize = 10,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromArgb(240, 56, 189, 248))
        };
        Canvas.SetLeft(centerLabel, cx + 13); Canvas.SetTop(centerLabel, cy - 8);
        RadarSimulationCanvas.Children.Add(centerLabel);
    }

    #endregion
}
