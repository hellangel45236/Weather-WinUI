using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
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
            tab.RenderRadarSimulation();
        }
    }

    private DispatcherTimer? _radarSweepTimer;
    private double _radarSweepAngle = 0;

    public RadarTab()
    {
        this.InitializeComponent();
        this.Loaded += (s, e) => RenderRadarSimulation();
    }

    public void StartRadarSweep()
    {
        if (_radarSweepTimer == null)
        {
            _radarSweepTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
            _radarSweepTimer.Tick += (s, e) =>
            {
                _radarSweepAngle = (_radarSweepAngle + 3) % 360;
                RenderRadarSimulation();
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
            double maxRadius = Math.Min(cx, cy) - 22;
            if (maxRadius <= 10) return;

            // 1. Radar Circular Scope Dark Background
            var scopeBg = new Microsoft.UI.Xaml.Shapes.Ellipse
            {
                Width = maxRadius * 2,
                Height = maxRadius * 2,
                Fill = new SolidColorBrush(Color.FromArgb(220, 10, 16, 28)),
                Stroke = new SolidColorBrush(Color.FromArgb(90, 56, 189, 248)),
                StrokeThickness = 1.5
            };
            Canvas.SetLeft(scopeBg, cx - maxRadius);
            Canvas.SetTop(scopeBg, cy - maxRadius);
            RadarSimulationCanvas.Children.Add(scopeBg);

            // 2. Concentric Range Rings (50, 100, 150, 200 km)
            for (int i = 1; i <= 4; i++)
            {
                double r = maxRadius * (i / 4.0);
                var circle = new Microsoft.UI.Xaml.Shapes.Ellipse
                {
                    Width = r * 2,
                    Height = r * 2,
                    Stroke = new SolidColorBrush(Color.FromArgb((byte)(i == 4 ? 80 : 45), 56, 189, 248)),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 3, 3 }
                };
                Canvas.SetLeft(circle, cx - r);
                Canvas.SetTop(circle, cy - r);
                RadarSimulationCanvas.Children.Add(circle);

                // Distance badge text
                var kmBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(160, 15, 23, 42)),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(4, 1, 4, 1)
                };
                kmBorder.Child = new TextBlock
                {
                    Text = $"{i * 50} km",
                    FontSize = 8.5,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromArgb(200, 56, 189, 248))
                };
                Canvas.SetLeft(kmBorder, cx + 4);
                Canvas.SetTop(kmBorder, cy - r - 8);
                RadarSimulationCanvas.Children.Add(kmBorder);
            }

            // 3. Radial Compass Spokes & Cardinal Directions
            string[] cardinalLabels = { "BẮC (0°)", "ĐB (45°)", "ĐÔNG (90°)", "ĐN (135°)", "NAM (180°)", "TN (225°)", "TÂY (270°)", "TB (315°)" };
            for (int i = 0; i < 8; i++)
            {
                double deg = i * 45;
                double rad = (deg - 90) * Math.PI / 180.0;
                double xEnd = cx + maxRadius * Math.Cos(rad);
                double yEnd = cy + maxRadius * Math.Sin(rad);

                var spoke = new Microsoft.UI.Xaml.Shapes.Line
                {
                    X1 = cx,
                    Y1 = cy,
                    X2 = xEnd,
                    Y2 = yEnd,
                    Stroke = new SolidColorBrush(Color.FromArgb((byte)(i % 2 == 0 ? 50 : 25), 255, 255, 255)),
                    StrokeThickness = 1,
                    StrokeDashArray = i % 2 == 0 ? null : new DoubleCollection { 2, 4 }
                };
                RadarSimulationCanvas.Children.Add(spoke);

                if (i % 2 == 0)
                {
                    var cTb = new TextBlock
                    {
                        Text = cardinalLabels[i],
                        FontSize = 9,
                        FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromArgb(180, 148, 163, 184))
                    };
                    double lx = cx + (maxRadius + 6) * Math.Cos(rad) - 20;
                    double ly = cy + (maxRadius + 6) * Math.Sin(rad) - 6;
                    Canvas.SetLeft(cTb, Math.Clamp(lx, 2, w - 50));
                    Canvas.SetTop(cTb, Math.Clamp(ly, 2, h - 16));
                    RadarSimulationCanvas.Children.Add(cTb);
                }
            }

            // 4. Multi-Layer Simulated Doppler Precipitation Echoes (dBZ Storm Fronts)
            double swX = cx - maxRadius * 0.42;
            double swY = cy + maxRadius * 0.35;
            var echoA1 = new Microsoft.UI.Xaml.Shapes.Ellipse { Width = 110, Height = 75, Fill = new SolidColorBrush(Color.FromArgb(70, 16, 185, 129)) };
            Canvas.SetLeft(echoA1, swX - 55); Canvas.SetTop(echoA1, swY - 37);
            RadarSimulationCanvas.Children.Add(echoA1);
            var echoA2 = new Microsoft.UI.Xaml.Shapes.Ellipse { Width = 65, Height = 45, Fill = new SolidColorBrush(Color.FromArgb(90, 245, 158, 11)) };
            Canvas.SetLeft(echoA2, swX - 30); Canvas.SetTop(echoA2, swY - 20);
            RadarSimulationCanvas.Children.Add(echoA2);
            var echoA3 = new Microsoft.UI.Xaml.Shapes.Ellipse { Width = 30, Height = 22, Fill = new SolidColorBrush(Color.FromArgb(120, 239, 68, 68)) };
            Canvas.SetLeft(echoA3, swX - 12); Canvas.SetTop(echoA3, swY - 10);
            RadarSimulationCanvas.Children.Add(echoA3);

            double neX = cx + maxRadius * 0.45;
            double neY = cy - maxRadius * 0.40;
            var echoB1 = new Microsoft.UI.Xaml.Shapes.Ellipse { Width = 85, Height = 60, Fill = new SolidColorBrush(Color.FromArgb(65, 56, 189, 248)) };
            Canvas.SetLeft(echoB1, neX - 42); Canvas.SetTop(echoB1, neY - 30);
            RadarSimulationCanvas.Children.Add(echoB1);
            var echoB2 = new Microsoft.UI.Xaml.Shapes.Ellipse { Width = 48, Height = 36, Fill = new SolidColorBrush(Color.FromArgb(80, 16, 185, 129)) };
            Canvas.SetLeft(echoB2, neX - 22); Canvas.SetTop(echoB2, neY - 18);
            RadarSimulationCanvas.Children.Add(echoB2);

            double wX = cx - maxRadius * 0.75;
            double wY = cy - maxRadius * 0.15;
            var echoC1 = new Microsoft.UI.Xaml.Shapes.Ellipse { Width = 70, Height = 50, Fill = new SolidColorBrush(Color.FromArgb(60, 245, 158, 11)) };
            Canvas.SetLeft(echoC1, wX - 35); Canvas.SetTop(echoC1, wY - 25);
            RadarSimulationCanvas.Children.Add(echoC1);
            var echoC2 = new Microsoft.UI.Xaml.Shapes.Ellipse { Width = 32, Height = 24, Fill = new SolidColorBrush(Color.FromArgb(110, 239, 68, 68)) };
            Canvas.SetLeft(echoC2, wX - 16); Canvas.SetTop(echoC2, wY - 12);
            RadarSimulationCanvas.Children.Add(echoC2);

            // 5. Regional City Landmarks & Beacons
            string locName = ViewModel?.CurrentWeather?.LocationName ?? "";
            bool isSouth = locName.Contains("Hồ Chí Minh", StringComparison.OrdinalIgnoreCase) ||
                           locName.Contains("Sài Gòn", StringComparison.OrdinalIgnoreCase) ||
                           locName.Contains("Cần Thơ", StringComparison.OrdinalIgnoreCase) ||
                           locName.Contains("Vũng Tàu", StringComparison.OrdinalIgnoreCase) ||
                           locName.Contains("Bình Dương", StringComparison.OrdinalIgnoreCase);

            var landmarks = isSouth ? new[]
            {
                ("Bình Dương", 0.28, 20.0),
                ("Biên Hòa", 0.32, 65.0),
                ("Vũng Tàu", 0.72, 130.0),
                ("Cần Giờ", 0.42, 175.0),
                ("Long An", 0.40, 240.0),
                ("Tây Ninh", 0.70, 310.0)
            } : new[]
            {
                ("Bắc Ninh", 0.28, 35.0),
                ("Hải Phòng", 0.75, 105.0),
                ("Nam Định", 0.65, 155.0),
                ("Hòa Bình", 0.55, 235.0),
                ("Vĩnh Phúc", 0.45, 315.0),
                ("Thái Nguyên", 0.68, 355.0)
            };

            foreach (var (cityName, distRatio, bearingDeg) in landmarks)
            {
                double bRad = (bearingDeg - 90) * Math.PI / 180.0;
                double lmX = cx + (maxRadius * distRatio) * Math.Cos(bRad);
                double lmY = cy + (maxRadius * distRatio) * Math.Sin(bRad);

                var lmDot = new Microsoft.UI.Xaml.Shapes.Ellipse
                {
                    Width = 5,
                    Height = 5,
                    Fill = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255))
                };
                Canvas.SetLeft(lmDot, lmX - 2.5);
                Canvas.SetTop(lmDot, lmY - 2.5);
                RadarSimulationCanvas.Children.Add(lmDot);

                var lmText = new TextBlock
                {
                    Text = cityName,
                    FontSize = 8.5,
                    Foreground = new SolidColorBrush(Color.FromArgb(150, 203, 213, 225))
                };
                Canvas.SetLeft(lmText, lmX + 4);
                Canvas.SetTop(lmText, lmY - 5);
                RadarSimulationCanvas.Children.Add(lmText);
            }

            // 6. Animated Rotating Radar Sweep Cone & Beam (Phosphor Trail)
            double sweepRad = (_radarSweepAngle - 90) * Math.PI / 180.0;
            const int TRAIL_SLICES = 12;
            const double SLICE_ANGLE_DEG = 3.0;

            for (int s = TRAIL_SLICES; s >= 1; s--)
            {
                double a1 = (sweepRad - (s * SLICE_ANGLE_DEG * Math.PI / 180.0));
                double a2 = (sweepRad - ((s - 1) * SLICE_ANGLE_DEG * Math.PI / 180.0));

                var wedge = new Microsoft.UI.Xaml.Shapes.Polygon();
                byte alpha = (byte)Math.Max(3, (int)(75 * (1.0 - (double)s / TRAIL_SLICES)));
                wedge.Fill = new SolidColorBrush(Color.FromArgb(alpha, 16, 185, 129));

                wedge.Points.Add(new Windows.Foundation.Point(cx, cy));
                wedge.Points.Add(new Windows.Foundation.Point(cx + maxRadius * Math.Cos(a1), cy + maxRadius * Math.Sin(a1)));
                wedge.Points.Add(new Windows.Foundation.Point(cx + maxRadius * Math.Cos(a2), cy + maxRadius * Math.Sin(a2)));
                RadarSimulationCanvas.Children.Add(wedge);
            }

            // Leading Radar Sweep Beam Line
            double beamX = cx + maxRadius * Math.Cos(sweepRad);
            double beamY = cy + maxRadius * Math.Sin(sweepRad);
            var sweepBeam = new Microsoft.UI.Xaml.Shapes.Line
            {
                X1 = cx,
                Y1 = cy,
                X2 = beamX,
                Y2 = beamY,
                Stroke = new SolidColorBrush(Color.FromArgb(230, 52, 211, 153)),
                StrokeThickness = 2.0
            };
            RadarSimulationCanvas.Children.Add(sweepBeam);

            // 7. Center Radar Station Beacon
            var centerPulse = new Microsoft.UI.Xaml.Shapes.Ellipse
            {
                Width = 16,
                Height = 16,
                Stroke = new SolidColorBrush(Color.FromArgb(160, 56, 189, 248)),
                StrokeThickness = 1.5,
                Fill = new SolidColorBrush(Color.FromArgb(30, 56, 189, 248))
            };
            Canvas.SetLeft(centerPulse, cx - 8);
            Canvas.SetTop(centerPulse, cy - 8);
            RadarSimulationCanvas.Children.Add(centerPulse);

            var centerDot = new Microsoft.UI.Xaml.Shapes.Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129)),
                Stroke = new SolidColorBrush(Microsoft.UI.Colors.White),
                StrokeThickness = 1.5
            };
            Canvas.SetLeft(centerDot, cx - 4);
            Canvas.SetTop(centerDot, cy - 4);
            RadarSimulationCanvas.Children.Add(centerDot);

            string centerName = string.IsNullOrWhiteSpace(locName) ? "TRẠM CHÍNH" : locName.ToUpper();
            var centerLabel = new TextBlock
            {
                Text = $"● {centerName}",
                FontSize = 9.5,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromArgb(220, 56, 189, 248))
            };
            Canvas.SetLeft(centerLabel, cx + 12);
            Canvas.SetTop(centerLabel, cy - 8);
            RadarSimulationCanvas.Children.Add(centerLabel);
        }
        catch { }
    }
}
