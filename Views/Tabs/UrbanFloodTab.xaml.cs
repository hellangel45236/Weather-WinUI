using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;
using WeatherApp.Models;
using WeatherApp.ViewModels;

namespace WeatherApp.Views.Tabs;

public sealed partial class UrbanFloodTab : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(MainViewModel), typeof(UrbanFloodTab), new PropertyMetadata(null, OnViewModelChanged));

    public MainViewModel? ViewModel
    {
        get => (MainViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UrbanFloodTab tab)
        {
            tab.RenderTidalSineWave();
        }
    }

    public UrbanFloodTab()
    {
        this.InitializeComponent();
        this.Loaded += (s, e) =>
        {
            if (TidalSineWaveCanvas != null)
            {
                TidalSineWaveCanvas.PointerMoved += TidalSineWaveCanvas_PointerMoved;
                TidalSineWaveCanvas.PointerExited += TidalSineWaveCanvas_PointerExited;
            }
            RenderTidalSineWave();
        };
    }

    #region Search & Quick Filter Handlers

    private void FloodSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            if (sender is TextBox tb && ViewModel != null)
            {
                ViewModel.FloodStreetSearchQuery = tb.Text ?? string.Empty;
            }
        }
        catch { }
    }

    private void FloodDistrictComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (sender is ComboBox cb && cb.SelectedItem is string district && ViewModel != null)
            {
                ViewModel.SelectedFloodDistrict = district;
            }
        }
        catch { }
    }

    private void FilterBtnAll_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        if (FloodSearchTextBox != null) FloodSearchTextBox.Text = string.Empty;
        ViewModel.FloodStreetSearchQuery = string.Empty;
        ViewModel.SelectedFloodDistrict = "Tất cả";
    }

    private void FilterBtnTide_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        bool isVi = Services.LocalizationService.Instance.IsVietnamese;
        string term = isVi ? "Triều Cường" : "Tidal";
        if (FloodSearchTextBox != null) FloodSearchTextBox.Text = term;
        ViewModel.FloodStreetSearchQuery = term;
    }

    private void FilterBtnRain_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        bool isVi = Services.LocalizationService.Instance.IsVietnamese;
        string term = isVi ? "Mưa Lớn" : "Rain";
        if (FloodSearchTextBox != null) FloodSearchTextBox.Text = term;
        ViewModel.FloodStreetSearchQuery = term;
    }

    private void FilterBtnCritical_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        bool isVi = Services.LocalizationService.Instance.IsVietnamese;
        string term = isVi ? "Ngập sâu" : "Deep";
        if (FloodSearchTextBox != null) FloodSearchTextBox.Text = term;
        ViewModel.FloodStreetSearchQuery = term;
    }

    #endregion

    #region Tidal Wave Chart Canvas Rendering

    private void TidalSineWaveCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        RenderTidalSineWave();
    }

    private Line? _hoverCrosshairLine;
    private Ellipse? _hoverDot;
    private Border? _hoverTooltip;
    private TextBlock? _hoverTooltipText;

    private void TidalSineWaveCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        try
        {
            if (TidalSineWaveCanvas == null || ViewModel?.UrbanFloodWarning == null) return;
            var points = ViewModel.UrbanFloodWarning.TideCurve24h;
            if (points == null || points.Count == 0) return;

            double width = TidalSineWaveCanvas.ActualWidth;
            double height = TidalSineWaveCanvas.ActualHeight;
            if (width <= 40 || height <= 30) return;

            Point pos = e.GetCurrentPoint(TidalSineWaveCanvas).Position;
            double leftMargin = 32;
            double rightMargin = 20;
            double chartW = width - leftMargin - rightMargin;

            if (pos.X < leftMargin || pos.X > width - rightMargin)
            {
                TidalSineWaveCanvas_PointerExited(sender, e);
                return;
            }

            double hourApprox = Math.Max(0, Math.Min(23, (pos.X - leftMargin) / chartW * 23.0));
            int baseHour = (int)Math.Floor(hourApprox);
            int nextHour = Math.Min(23, baseHour + 1);
            double frac = hourApprox - baseHour;

            var p0 = points.FirstOrDefault(p => p.Hour == baseHour);
            var p1 = points.FirstOrDefault(p => p.Hour == nextHour);
            if (p0 == null) return;

            double level = (p1 != null) ? p0.LevelMeters + (p1.LevelMeters - p0.LevelMeters) * frac : p0.LevelMeters;

            double minLevel = 0.70;
            double maxLevel = 1.80;
            double range = maxLevel - minLevel;
            double y = Math.Max(12, Math.Min(height - 24, height - 24 - ((level - minLevel) / range * (height - 36))));

            if (_hoverCrosshairLine == null)
            {
                _hoverCrosshairLine = new Line
                {
                    Stroke = new SolidColorBrush(Color.FromArgb(180, 56, 189, 248)),
                    StrokeThickness = 1.5,
                    StrokeDashArray = new DoubleCollection { 3, 3 }
                };
                TidalSineWaveCanvas.Children.Add(_hoverCrosshairLine);
            }
            _hoverCrosshairLine.X1 = pos.X;
            _hoverCrosshairLine.Y1 = 12;
            _hoverCrosshairLine.X2 = pos.X;
            _hoverCrosshairLine.Y2 = height - 24;
            _hoverCrosshairLine.Visibility = Visibility.Visible;

            if (_hoverDot == null)
            {
                _hoverDot = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = new SolidColorBrush(Colors.White),
                    Stroke = new SolidColorBrush(Color.FromArgb(255, 2, 132, 199)),
                    StrokeThickness = 2
                };
                TidalSineWaveCanvas.Children.Add(_hoverDot);
            }
            Canvas.SetLeft(_hoverDot, pos.X - 5);
            Canvas.SetTop(_hoverDot, y - 5);
            _hoverDot.Visibility = Visibility.Visible;

            if (_hoverTooltip == null)
            {
                _hoverTooltipText = new TextBlock
                {
                    FontSize = 10.5,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 248, 250, 252))
                };
                _hoverTooltip = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(235, 15, 23, 42)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(255, 56, 189, 248)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(7),
                    Padding = new Thickness(8, 4, 8, 4),
                    Child = _hoverTooltipText
                };
                TidalSineWaveCanvas.Children.Add(_hoverTooltip);
            }

            bool isVi = Services.LocalizationService.Instance.IsVietnamese;
            string alertText = isVi
                ? (level >= 1.60 ? "BĐ III (Ngập)" : level >= 1.55 ? "BĐ II" : level >= 1.40 ? "BĐ I" : "An toàn")
                : (level >= 1.60 ? "Alert 3 (Flooded)" : level >= 1.55 ? "Alert 2" : level >= 1.40 ? "Alert 1" : "Safe");
            if (_hoverTooltipText != null)
            {
                _hoverTooltipText.Text = $"⏱️ {Math.Round(hourApprox):D2}:00 • {level:F2}m ({alertText})";
            }

            double tooltipX = Math.Max(leftMargin, Math.Min(width - rightMargin - 160, pos.X - 80));
            double tooltipY = Math.Max(6, y - 32);
            Canvas.SetLeft(_hoverTooltip, tooltipX);
            Canvas.SetTop(_hoverTooltip, tooltipY);
            _hoverTooltip.Visibility = Visibility.Visible;
        }
        catch { }
    }

    private void TidalSineWaveCanvas_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        try
        {
            if (_hoverCrosshairLine != null) _hoverCrosshairLine.Visibility = Visibility.Collapsed;
            if (_hoverDot != null) _hoverDot.Visibility = Visibility.Collapsed;
            if (_hoverTooltip != null) _hoverTooltip.Visibility = Visibility.Collapsed;
        }
        catch { }
    }

    public void RenderTidalSineWave()
    {
        try
        {
            if (TidalSineWaveCanvas == null || ViewModel?.UrbanFloodWarning == null) return;
            TidalSineWaveCanvas.Children.Clear();
            _hoverCrosshairLine = null;
            _hoverDot = null;
            _hoverTooltip = null;
            _hoverTooltipText = null;

            double width = TidalSineWaveCanvas.ActualWidth;
            double height = TidalSineWaveCanvas.ActualHeight;
            if (width <= 40 || height <= 40) return;

            bool isVi = Services.LocalizationService.Instance.IsVietnamese;
            var points = ViewModel.UrbanFloodWarning.TideCurve24h;
            if (points == null || points.Count == 0) return;

            double leftMargin = 32;
            double rightMargin = 20;
            double topMargin = 14;
            double bottomMargin = 24;

            double plotW = width - leftMargin - rightMargin;
            double plotH = height - topMargin - bottomMargin;

            double minLevel = 0.70;
            double maxLevel = 1.80;
            double range = maxLevel - minLevel;

            double GetY(double level) => Math.Max(topMargin, Math.Min(height - bottomMargin, height - bottomMargin - ((level - minLevel) / range * plotH)));
            double GetX(double hour) => leftMargin + (hour / 23.0) * plotW;

            // 1. Vùng cảnh báo ngập bờ trên 1.60m (Danger Zone Ribbon)
            double yBd3 = GetY(1.60);
            var dangerZone = new Rectangle
            {
                Width = plotW,
                Height = Math.Max(0, yBd3 - topMargin),
                Fill = new SolidColorBrush(Color.FromArgb(20, 239, 68, 68))
            };
            Canvas.SetLeft(dangerZone, leftMargin);
            Canvas.SetTop(dangerZone, topMargin);
            TidalSineWaveCanvas.Children.Add(dangerZone);

            // 2. Các đường lưới ngang và nhãn trục Y
            void DrawHorizontalAlarmLine(double levelM, string label, Color color, bool isDashed = true)
            {
                double ly = GetY(levelM);
                var line = new Line
                {
                    X1 = leftMargin,
                    Y1 = ly,
                    X2 = width - rightMargin,
                    Y2 = ly,
                    Stroke = new SolidColorBrush(Color.FromArgb(130, color.R, color.G, color.B)),
                    StrokeThickness = 1.2
                };
                if (isDashed) line.StrokeDashArray = new DoubleCollection { 4, 3 };
                TidalSineWaveCanvas.Children.Add(line);

                var txt = new TextBlock
                {
                    Text = label,
                    FontSize = 9.5,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromArgb(220, color.R, color.G, color.B))
                };
                Canvas.SetLeft(txt, 4);
                Canvas.SetTop(txt, ly - 7);
                TidalSineWaveCanvas.Children.Add(txt);
            }

            DrawHorizontalAlarmLine(1.60, isVi ? "1.6m BĐ3" : "1.6m Alert 3", Color.FromArgb(255, 239, 68, 68));
            DrawHorizontalAlarmLine(1.55, "1.55m", Color.FromArgb(255, 234, 88, 12));
            DrawHorizontalAlarmLine(1.40, isVi ? "1.4m BĐ1" : "1.4m Alert 1", Color.FromArgb(255, 245, 158, 11));
            DrawHorizontalAlarmLine(1.00, "1.0m", Color.FromArgb(255, 16, 185, 129), isDashed: true);

            // 3. Nội suy Catmull-Rom cho sóng biển mượt mà qua 138 mẫu
            int sampleCount = 138;
            var smoothPoints = new List<Point>();

            for (int s = 0; s <= sampleCount; s++)
            {
                double h = (s / (double)sampleCount) * 23.0;
                int i0 = (int)Math.Floor(h);
                int i1 = Math.Min(23, i0 + 1);
                double t = h - i0;

                int im1 = Math.Max(0, i0 - 1);
                int i2 = Math.Min(23, i1 + 1);

                double y_im1 = points[im1].LevelMeters;
                double y_i0 = points[i0].LevelMeters;
                double y_i1 = points[i1].LevelMeters;
                double y_i2 = points[i2].LevelMeters;

                double interpY = 0.5 * ((2.0 * y_i0) +
                                        (-y_im1 + y_i1) * t +
                                        (2.0 * y_im1 - 5.0 * y_i0 + 4.0 * y_i1 - y_i2) * t * t +
                                        (-y_im1 + 3.0 * y_i0 - 3.0 * y_i1 + y_i2) * t * t * t);

                smoothPoints.Add(new Point(GetX(h), GetY(interpY)));
            }

            // 4. Đa giác tô màu gradient nước biển (Gradient Wave Fill)
            var fillPolygon = new Polygon();
            var fillBrush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1)
            };
            fillBrush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(100, 2, 132, 199), Offset = 0.0 });
            fillBrush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(40, 56, 189, 248), Offset = 0.5 });
            fillBrush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(8, 2, 132, 199), Offset = 1.0 });
            fillPolygon.Fill = fillBrush;

            fillPolygon.Points.Add(new Point(GetX(0), height - bottomMargin));
            foreach (var pt in smoothPoints)
            {
                fillPolygon.Points.Add(pt);
            }
            fillPolygon.Points.Add(new Point(GetX(23), height - bottomMargin));
            TidalSineWaveCanvas.Children.Add(fillPolygon);

            // 5. Đường sóng chính (Glow Stroke + Crisp Line)
            var glowLine = new Polyline
            {
                Stroke = new SolidColorBrush(Color.FromArgb(70, 56, 189, 248)),
                StrokeThickness = 6.0
            };
            var mainLine = new Polyline
            {
                Stroke = new SolidColorBrush(Color.FromArgb(255, 56, 189, 248)),
                StrokeThickness = 2.6
            };
            foreach (var pt in smoothPoints)
            {
                glowLine.Points.Add(pt);
                mainLine.Points.Add(pt);
            }
            TidalSineWaveCanvas.Children.Add(glowLine);
            TidalSineWaveCanvas.Children.Add(mainLine);

            // 6. Đánh dấu Đỉnh Triều (Peaks)
            foreach (var p in points.Where(pt => pt.IsPeak))
            {
                double px = GetX(p.Hour);
                double py = GetY(p.LevelMeters);

                var peakDot = new Ellipse
                {
                    Width = 9,
                    Height = 9,
                    Fill = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)),
                    Stroke = new SolidColorBrush(Colors.White),
                    StrokeThickness = 1.8
                };
                Canvas.SetLeft(peakDot, px - 4.5);
                Canvas.SetTop(peakDot, py - 4.5);
                TidalSineWaveCanvas.Children.Add(peakDot);

                var peakPill = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(220, 239, 68, 68)),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(4, 1, 4, 1)
                };
                peakPill.Child = new TextBlock
                {
                    Text = $"▲ {p.LevelMeters:F2}m",
                    FontSize = 9.5,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    Foreground = new SolidColorBrush(Colors.White)
                };
                Canvas.SetLeft(peakPill, px - 18);
                Canvas.SetTop(peakPill, py - 21);
                TidalSineWaveCanvas.Children.Add(peakPill);
            }

            // 7. Đánh dấu Giờ Hiện Tại (Current Hour Marker "BÂY GIỜ")
            int currentHour = DateTime.Now.Hour;
            var curPoint = points.FirstOrDefault(p => p.Hour == currentHour);
            if (curPoint != null)
            {
                double nowX = GetX(curPoint.Hour);
                double nowY = GetY(curPoint.LevelMeters);

                var nowGuideline = new Line
                {
                    X1 = nowX,
                    Y1 = topMargin,
                    X2 = nowX,
                    Y2 = height - bottomMargin,
                    Stroke = new SolidColorBrush(Color.FromArgb(120, 56, 189, 248)),
                    StrokeThickness = 1.2,
                    StrokeDashArray = new DoubleCollection { 3, 2 }
                };
                TidalSineWaveCanvas.Children.Add(nowGuideline);

                var outerGlow = new Ellipse
                {
                    Width = 22,
                    Height = 22,
                    Fill = new SolidColorBrush(Color.FromArgb(40, 56, 189, 248)),
                    Stroke = new SolidColorBrush(Color.FromArgb(160, 56, 189, 248)),
                    StrokeThickness = 1.5
                };
                Canvas.SetLeft(outerGlow, nowX - 11);
                Canvas.SetTop(outerGlow, nowY - 11);
                TidalSineWaveCanvas.Children.Add(outerGlow);

                var centerDot = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = new SolidColorBrush(Colors.White),
                    Stroke = new SolidColorBrush(Color.FromArgb(255, 2, 132, 199)),
                    StrokeThickness = 2
                };
                Canvas.SetLeft(centerDot, nowX - 5);
                Canvas.SetTop(centerDot, nowY - 5);
                TidalSineWaveCanvas.Children.Add(centerDot);

                var nowBadge = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(235, 15, 23, 42)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(255, 56, 189, 248)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(6, 2, 6, 2)
                };
                nowBadge.Child = new TextBlock
                {
                    Text = (isVi ? "BÂY GIỜ: " : "NOW: ") + $"{curPoint.LevelMeters:F2}m",
                    FontSize = 10,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 56, 189, 248))
                };
                double badgeX = Math.Max(leftMargin, Math.Min(width - rightMargin - 95, nowX - 45));
                double badgeY = Math.Max(topMargin, nowY - 26);
                Canvas.SetLeft(nowBadge, badgeX);
                Canvas.SetTop(nowBadge, badgeY);
                TidalSineWaveCanvas.Children.Add(nowBadge);
            }

            // 8. Trục hoành thời gian X
            for (int h = 0; h < 24; h += 4)
            {
                double tx = GetX(h);
                var timeTick = new Line
                {
                    X1 = tx,
                    Y1 = height - bottomMargin,
                    X2 = tx,
                    Y2 = height - bottomMargin + 4,
                    Stroke = new SolidColorBrush(Color.FromArgb(140, 255, 255, 255)),
                    StrokeThickness = 1
                };
                TidalSineWaveCanvas.Children.Add(timeTick);

                var timeTxt = new TextBlock
                {
                    Text = $"{h:D2}:00",
                    FontSize = 10,
                    FontWeight = (h == currentHour) ? Microsoft.UI.Text.FontWeights.Bold : Microsoft.UI.Text.FontWeights.Normal,
                    Foreground = (h == currentHour)
                        ? new SolidColorBrush(Color.FromArgb(255, 56, 189, 248))
                        : new SolidColorBrush(Color.FromArgb(170, 255, 255, 255))
                };
                Canvas.SetLeft(timeTxt, tx - 14);
                Canvas.SetTop(timeTxt, height - bottomMargin + 6);
                TidalSineWaveCanvas.Children.Add(timeTxt);
            }

            double txEnd = GetX(23);
            var endTxt = new TextBlock
            {
                Text = "23:00",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromArgb(170, 255, 255, 255))
            };
            Canvas.SetLeft(endTxt, txEnd - 14);
            Canvas.SetTop(endTxt, height - bottomMargin + 6);
            TidalSineWaveCanvas.Children.Add(endTxt);

            // 9. Cập nhật Trụ Đo Mực Nước Thủy Triều Kỹ Thuật Số bên cạnh (WaterLevelBar)
            if (WaterLevelBar != null)
            {
                double curLevel = ViewModel.UrbanFloodWarning.CurrentTideLevel;
                double ratio = Math.Max(0.08, Math.Min(1.0, (curLevel - minLevel) / range));
                WaterLevelBar.Height = Math.Max(14, ratio * 130);

                var barBrush = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, 1)
                };

                if (curLevel >= 1.60)
                {
                    barBrush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 239, 68, 68), Offset = 0.0 });
                    barBrush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 185, 28, 28), Offset = 1.0 });
                }
                else if (curLevel >= 1.55)
                {
                    barBrush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 234, 88, 12), Offset = 0.0 });
                    barBrush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 194, 65, 12), Offset = 1.0 });
                }
                else if (curLevel >= 1.40)
                {
                    barBrush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 245, 158, 11), Offset = 0.0 });
                    barBrush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 217, 119, 6), Offset = 1.0 });
                }
                else
                {
                    barBrush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 16, 185, 129), Offset = 0.0 });
                    barBrush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 5, 150, 105), Offset = 1.0 });
                }
                WaterLevelBar.Background = barBrush;
            }
        }
        catch { }
    }

    #endregion
}
