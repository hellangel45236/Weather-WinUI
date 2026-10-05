using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WeatherApp.Helpers;

public class UniformGrid : Panel
{
    public static readonly DependencyProperty ColumnsProperty =
        DependencyProperty.Register(nameof(Columns), typeof(int), typeof(UniformGrid),
            new PropertyMetadata(1, OnLayoutPropertyChanged));

    public static readonly DependencyProperty RowsProperty =
        DependencyProperty.Register(nameof(Rows), typeof(int), typeof(UniformGrid),
            new PropertyMetadata(0, OnLayoutPropertyChanged));

    public static readonly DependencyProperty FirstColumnProperty =
        DependencyProperty.Register(nameof(FirstColumn), typeof(int), typeof(UniformGrid),
            new PropertyMetadata(0, OnLayoutPropertyChanged));

    public int Columns
    {
        get => (int)GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    public int Rows
    {
        get => (int)GetValue(RowsProperty);
        set => SetValue(RowsProperty, value);
    }

    public int FirstColumn
    {
        get => (int)GetValue(FirstColumnProperty);
        set => SetValue(FirstColumnProperty, value);
    }

    private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UniformGrid grid)
        {
            grid.InvalidateMeasure();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        int visibleCount = 0;
        foreach (UIElement child in Children)
        {
            if (child.Visibility != Visibility.Collapsed)
            {
                visibleCount++;
            }
        }

        if (visibleCount == 0)
        {
            return new Size(0, 0);
        }

        int cols = Columns;
        int rows = Rows;

        if (cols <= 0 && rows <= 0)
        {
            cols = (int)Math.Ceiling(Math.Sqrt(visibleCount));
            rows = (int)Math.Ceiling((double)visibleCount / cols);
        }
        else if (cols <= 0)
        {
            cols = (int)Math.Ceiling((double)(visibleCount + FirstColumn) / rows);
        }
        else if (rows <= 0)
        {
            rows = (int)Math.Ceiling((double)(visibleCount + FirstColumn) / cols);
        }

        double childAvailableWidth = double.IsInfinity(availableSize.Width) ? 60.0 : Math.Max(0, availableSize.Width / cols);
        double childAvailableHeight = double.IsInfinity(availableSize.Height) ? double.PositiveInfinity : availableSize.Height;

        Size childMeasureSize = new Size(childAvailableWidth, childAvailableHeight);
        double maxChildWidth = 0;
        double maxChildHeight = 0;

        foreach (UIElement child in Children)
        {
            if (child.Visibility == Visibility.Collapsed) continue;
            child.Measure(childMeasureSize);
            maxChildWidth = Math.Max(maxChildWidth, child.DesiredSize.Width);
            maxChildHeight = Math.Max(maxChildHeight, child.DesiredSize.Height);
        }

        double totalWidth = double.IsInfinity(availableSize.Width) ? (maxChildWidth * cols) : availableSize.Width;
        double totalHeight = maxChildHeight * rows;

        return new Size(totalWidth, totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        int visibleCount = 0;
        foreach (UIElement child in Children)
        {
            if (child.Visibility != Visibility.Collapsed) visibleCount++;
        }

        if (visibleCount == 0) return finalSize;

        int cols = Columns;
        int rows = Rows;

        if (cols <= 0 && rows <= 0)
        {
            cols = (int)Math.Ceiling(Math.Sqrt(visibleCount));
            rows = (int)Math.Ceiling((double)visibleCount / cols);
        }
        else if (cols <= 0)
        {
            cols = (int)Math.Ceiling((double)(visibleCount + FirstColumn) / rows);
        }
        else if (rows <= 0)
        {
            rows = (int)Math.Ceiling((double)(visibleCount + FirstColumn) / cols);
        }

        double cellWidth = finalSize.Width / cols;
        double maxDesiredHeight = 0;
        foreach (UIElement child in Children)
        {
            if (child.Visibility != Visibility.Collapsed)
            {
                maxDesiredHeight = Math.Max(maxDesiredHeight, child.DesiredSize.Height);
            }
        }
        double cellHeight = Math.Max(maxDesiredHeight, finalSize.Height / Math.Max(1, rows));

        int cellIndex = FirstColumn;
        foreach (UIElement child in Children)
        {
            if (child.Visibility == Visibility.Collapsed) continue;

            int col = cellIndex % cols;
            int row = cellIndex / cols;

            Rect rect = new Rect(col * cellWidth, row * cellHeight, cellWidth, cellHeight);
            child.Arrange(rect);

            cellIndex++;
        }

        return new Size(finalSize.Width, cellHeight * rows);
    }
}
