using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using WeatherApp.ViewModels;

namespace WeatherApp.Views.Tabs;

public sealed partial class LifestyleTab : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(MainViewModel), typeof(LifestyleTab), new PropertyMetadata(null, OnViewModelChanged));

    public MainViewModel? ViewModel
    {
        get => (MainViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LifestyleTab tab)
        {
            tab.UpdateOccasionButtonsVisual();
        }
    }

    public LifestyleTab()
    {
        this.InitializeComponent();
        this.Loaded += (s, e) => UpdateOccasionButtonsVisual();
    }

    private void OccasionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag && ViewModel != null)
        {
            ViewModel.SelectOutfitOccasion(tag);
            UpdateOccasionButtonsVisual();
        }
    }

    public void UpdateOccasionButtonsVisual()
    {
        if (ViewModel == null) return;
        string current = ViewModel.SelectedOutfitOccasion;
        SetOccasionButtonState(OccasionWorkButton, current == "Work");
        SetOccasionButtonState(OccasionSchoolButton, current == "School");
        SetOccasionButtonState(OccasionCasualButton, current == "Casual");
    }

    private void SetOccasionButtonState(Button? btn, bool isSelected)
    {
        if (btn == null) return;
        btn.BorderThickness = new Thickness(1);
        btn.BorderBrush = isSelected
            ? (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"]
            : (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"];
        btn.Background = isSelected
            ? (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"]
            : (Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"];

        var textColor = isSelected
            ? new SolidColorBrush(Color.FromArgb(255, 255, 255, 255))
            : (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];
        btn.Foreground = textColor;

        if (btn.Content is StackPanel sp)
        {
            foreach (var child in sp.Children)
            {
                if (child is TextBlock tb) tb.Foreground = textColor;
                if (child is FontIcon fi && isSelected) fi.Foreground = textColor;
            }
        }
    }
}
