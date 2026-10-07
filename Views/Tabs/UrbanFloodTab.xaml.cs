using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
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
        this.Loaded += (s, e) => RenderTidalSineWave();
    }

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

    private void TidalSineWaveCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        RenderTidalSineWave();
    }

    public void RenderTidalSineWave()
    {
        try
        {
            if (TidalSineWaveCanvas == null || ViewModel?.UrbanFloodWarning == null) return;
            TidalSineWaveCanvas.Children.Clear();

            double width = TidalSineWaveCanvas.ActualWidth;
            double height = TidalSineWaveCanvas.ActualHeight;
            if (width <= 20 || height <= 20) return;

            var points = ViewModel.UrbanFloodWarning.TideCurve24h;
            if (points == null || points.Count == 0) return;

            double minLevel = 0.70;
            double maxLevel = 1.80;
            double range = maxLevel - minLevel;

            double GetY(double level) => Math.Max(5, Math.Min(height - 20, height - 20 - ((level - minLevel) / range * (height - 30))));
            double GetX(int hour) => (hour / 23.0) * (width - 40) + 20;

            // 1. Alarm lines
            // BD 3: 1.60m
            double yBd3 = GetY(1.60);
            var lineBd3 = new Microsoft.UI.Xaml.Shapes.Line { X1 = 20, Y1 = yBd3, X2 = width - 20, Y2 = yBd3, Stroke = new SolidColorBrush(Color.FromArgb(160, 239, 68, 68)), StrokeDashArray = new DoubleCollection { 4, 3 }, StrokeThickness = 1 };
            TidalSineWaveCanvas.Children.Add(lineBd3);
            var labelBd3 = new TextBlock { Text = "Báo động III (1.60m)", FontSize = 10, Foreground = new SolidColorBrush(Color.FromArgb(220, 239, 68, 68)) };
            Canvas.SetLeft(labelBd3, 24);
            Canvas.SetTop(labelBd3, yBd3 - 14);
            TidalSineWaveCanvas.Children.Add(labelBd3);

            // BD 2: 1.55m
            double yBd2 = GetY(1.55);
            var lineBd2 = new Microsoft.UI.Xaml.Shapes.Line { X1 = 20, Y1 = yBd2, X2 = width - 20, Y2 = yBd2, Stroke = new SolidColorBrush(Color.FromArgb(140, 234, 88, 12)), StrokeDashArray = new DoubleCollection { 4, 3 }, StrokeThickness = 1 };
            TidalSineWaveCanvas.Children.Add(lineBd2);

            // BD 1: 1.40m
            double yBd1 = GetY(1.40);
            var lineBd1 = new Microsoft.UI.Xaml.Shapes.Line { X1 = 20, Y1 = yBd1, X2 = width - 20, Y2 = yBd1, Stroke = new SolidColorBrush(Color.FromArgb(140, 245, 158, 11)), StrokeDashArray = new DoubleCollection { 4, 3 }, StrokeThickness = 1 };
            TidalSineWaveCanvas.Children.Add(lineBd1);
            var labelBd1 = new TextBlock { Text = "Báo động I (1.40m)", FontSize = 10, Foreground = new SolidColorBrush(Color.FromArgb(180, 245, 158, 11)) };
            Canvas.SetLeft(labelBd1, Math.Max(20, width - 140));
            Canvas.SetTop(labelBd1, yBd1 - 14);
            TidalSineWaveCanvas.Children.Add(labelBd1);

            // 2. Fill Polygon
            var polygon = new Microsoft.UI.Xaml.Shapes.Polygon();
            var fillBrush = new LinearGradientBrush { StartPoint = new Windows.Foundation.Point(0, 0), EndPoint = new Windows.Foundation.Point(0, 1) };
            fillBrush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(90, 2, 132, 199), Offset = 0 });
            fillBrush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(15, 2, 132, 199), Offset = 1 });
            polygon.Fill = fillBrush;

            polygon.Points.Add(new Windows.Foundation.Point(GetX(0), height - 10));
            foreach (var p in points)
            {
                polygon.Points.Add(new Windows.Foundation.Point(GetX(p.Hour), GetY(p.LevelMeters)));
            }
            polygon.Points.Add(new Windows.Foundation.Point(GetX(23), height - 10));
            TidalSineWaveCanvas.Children.Add(polygon);

            // 3. Wave Line
            var polyline = new Microsoft.UI.Xaml.Shapes.Polyline
            {
                Stroke = new SolidColorBrush(Color.FromArgb(255, 56, 189, 248)),
                StrokeThickness = 2.5
            };
            foreach (var p in points)
            {
                polyline.Points.Add(new Windows.Foundation.Point(GetX(p.Hour), GetY(p.LevelMeters)));
            }
            TidalSineWaveCanvas.Children.Add(polyline);

            // 4. Markers
            int currentHour = DateTime.Now.Hour;
            foreach (var p in points)
            {
                double px = GetX(p.Hour);
                double py = GetY(p.LevelMeters);

                if (p.IsPeak)
                {
                    var peakDot = new Microsoft.UI.Xaml.Shapes.Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)), Stroke = new SolidColorBrush(Microsoft.UI.Colors.White), StrokeThickness = 1.5 };
                    Canvas.SetLeft(peakDot, px - 4);
                    Canvas.SetTop(peakDot, py - 4);
                    TidalSineWaveCanvas.Children.Add(peakDot);

                    var peakLabel = new TextBlock { Text = $"{p.LevelMeters:F2}m", FontSize = 9.5, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromArgb(230, 239, 68, 68)) };
                    Canvas.SetLeft(peakLabel, px - 12);
                    Canvas.SetTop(peakLabel, py - 18);
                    TidalSineWaveCanvas.Children.Add(peakLabel);
                }

                if (p.Hour == currentHour)
                {
                    var nowRing = new Microsoft.UI.Xaml.Shapes.Ellipse { Width = 18, Height = 18, Stroke = new SolidColorBrush(Color.FromArgb(160, 56, 189, 248)), StrokeThickness = 2, Fill = new SolidColorBrush(Color.FromArgb(40, 56, 189, 248)) };
                    Canvas.SetLeft(nowRing, px - 9);
                    Canvas.SetTop(nowRing, py - 9);
                    TidalSineWaveCanvas.Children.Add(nowRing);

                    var nowDot = new Microsoft.UI.Xaml.Shapes.Ellipse { Width = 10, Height = 10, Fill = new SolidColorBrush(Microsoft.UI.Colors.White), Stroke = new SolidColorBrush(Color.FromArgb(255, 2, 132, 199)), StrokeThickness = 2 };
                    Canvas.SetLeft(nowDot, px - 5);
                    Canvas.SetTop(nowDot, py - 5);
                    TidalSineWaveCanvas.Children.Add(nowDot);

                    var nowLabel = new TextBlock { Text = $"BÂY GIỜ: {p.LevelMeters:F2}m", FontSize = 10.5, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromArgb(255, 56, 189, 248)) };
                    Canvas.SetLeft(nowLabel, Math.Min(width - 110, Math.Max(10, px - 30)));
                    Canvas.SetTop(nowLabel, Math.Max(2, py - 20));
                    TidalSineWaveCanvas.Children.Add(nowLabel);
                }

                if (p.Hour % 4 == 0 || p.Hour == 23)
                {
                    var timeTxt = new TextBlock { Text = $"{p.Hour:D2}:00", FontSize = 9.5, Opacity = 0.65 };
                    Canvas.SetLeft(timeTxt, px - 12);
                    Canvas.SetTop(timeTxt, height - 16);
                    TidalSineWaveCanvas.Children.Add(timeTxt);
                }
            }
        }
        catch { }
    }
}
