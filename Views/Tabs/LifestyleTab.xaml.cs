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
            if (e.OldValue is MainViewModel oldVm)
            {
                oldVm.PropertyChanged -= tab.OnViewModelPropertyChanged;
            }
            if (e.NewValue is MainViewModel newVm)
            {
                newVm.PropertyChanged += tab.OnViewModelPropertyChanged;
            }
            tab.UpdateOccasionButtonsVisual();
            tab.UpdateGenderButtonsVisual();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SelectedOutfitOccasion))
        {
            UpdateOccasionButtonsVisual();
        }
        else if (e.PropertyName == nameof(MainViewModel.SelectedOutfitGender))
        {
            UpdateGenderButtonsVisual();
        }
    }

    public LifestyleTab()
    {
        this.InitializeComponent();
        this.Loaded += (s, e) =>
        {
            UpdateOccasionButtonsVisual();
            UpdateGenderButtonsVisual();
        };
    }

    private void OccasionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag && ViewModel != null)
        {
            ViewModel.SelectOutfitOccasion(tag);
            UpdateOccasionButtonsVisual();
        }
    }

    private void GenderButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag && ViewModel != null)
        {
            ViewModel.SelectOutfitGender(tag);
            UpdateGenderButtonsVisual();
        }
    }

    private void CopyOotdButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.CopyOotdAdviceCommand.Execute(null);
    }

    public void UpdateOccasionButtonsVisual()
    {
        if (ViewModel == null) return;
        string current = ViewModel.SelectedOutfitOccasion;
        SetButtonState(OccasionWorkButton, current == "Work");
        SetButtonState(OccasionSchoolButton, current == "School");
        SetButtonState(OccasionCasualButton, current == "Casual");
        SetButtonState(OccasionSportButton, current == "Sport");
        SetButtonState(OccasionTravelButton, current == "Travel");
    }

    public void UpdateGenderButtonsVisual()
    {
        if (ViewModel == null) return;
        string current = ViewModel.SelectedOutfitGender;
        SetButtonState(GenderAllButton, current == "All");
        SetButtonState(GenderMenButton, current == "Men");
        SetButtonState(GenderWomenButton, current == "Women");
    }

    private void SetButtonState(Button? btn, bool isSelected)
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
                if (child is FontIcon fi)
                {
                    fi.Foreground = isSelected
                        ? textColor
                        : (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
                }
            }
        }
    }
}
