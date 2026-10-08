using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WeatherApp.Models;
using WeatherApp.Services;
using WeatherApp.ViewModels;

namespace WeatherApp.Views.Tabs;

public sealed partial class WidgetStudioTab : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(MainViewModel), typeof(WidgetStudioTab), new PropertyMetadata(null, OnViewModelChanged));

    public MainViewModel? ViewModel
    {
        get => (MainViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WidgetStudioTab tab)
        {
            tab.SyncVisuals();
        }
    }

    private bool _isSyncing = false;

    public WidgetStudioTab()
    {
        this.InitializeComponent();
        this.Loaded += (s, e) => SyncVisuals();
    }

    private void SyncVisuals()
    {
        if (ViewModel == null) return;
        _isSyncing = true;
        try
        {
            string style = ViewModel.Settings.WidgetStyle ?? "BryanCDynamic";
            UpdateWidgetCardsVisuals(style);

            if (StudioWidgetStyleComboBox != null)
            {
                for (int i = 0; i < StudioWidgetStyleComboBox.Items.Count; i++)
                {
                    if (StudioWidgetStyleComboBox.Items[i] is ComboBoxItem item && item.Tag?.ToString() == style)
                    {
                        StudioWidgetStyleComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }

            if (StudioOpacitySlider != null)
            {
                double op = ViewModel.Settings.WidgetOpacity;
                StudioOpacitySlider.Value = op > 1.0 ? op : op * 100.0;
            }
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private void CardWidgetBryanC_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        SelectWidgetStyle("BryanCDynamic");
    }

    private void CardWidgetGlassCard_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        SelectWidgetStyle("GlassCard");
    }

    private void CardWidgetCompact_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        SelectWidgetStyle("Compact");
    }

    private void CardWidgetMiniIsland_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        SelectWidgetStyle("MiniIsland");
    }

    private void SelectWidgetStyle(string styleTag)
    {
        if (ViewModel == null) return;
        ViewModel.Settings.WidgetStyle = styleTag;
        ViewModel.ApplySettings();

        _isSyncing = true;
        if (StudioWidgetStyleComboBox != null)
        {
            for (int i = 0; i < StudioWidgetStyleComboBox.Items.Count; i++)
            {
                if (StudioWidgetStyleComboBox.Items[i] is ComboBoxItem item && item.Tag?.ToString() == styleTag)
                {
                    StudioWidgetStyleComboBox.SelectedIndex = i;
                    break;
                }
            }
        }
        _isSyncing = false;

        UpdateWidgetCardsVisuals(styleTag);
        foreach (var w in WidgetWindow.ActiveWidgets)
        {
            try { w.ApplyWidgetStyle(styleTag); } catch { }
        }
    }

    private void UpdateWidgetCardsVisuals(string selectedTag)
    {
        var accentBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
        var subtleBorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"];

        if (CardWidgetBryanC != null)
        {
            CardWidgetBryanC.BorderBrush = selectedTag == "BryanCDynamic" ? accentBrush : subtleBorderBrush;
            CardWidgetBryanC.BorderThickness = selectedTag == "BryanCDynamic" ? new Thickness(2) : new Thickness(1);
            if (BadgeWidgetBryanC != null) BadgeWidgetBryanC.Visibility = selectedTag == "BryanCDynamic" ? Visibility.Visible : Visibility.Collapsed;
        }
        if (CardWidgetGlassCard != null)
        {
            CardWidgetGlassCard.BorderBrush = selectedTag == "GlassCard" ? accentBrush : subtleBorderBrush;
            CardWidgetGlassCard.BorderThickness = selectedTag == "GlassCard" ? new Thickness(2) : new Thickness(1);
            if (BadgeWidgetGlassCard != null) BadgeWidgetGlassCard.Visibility = selectedTag == "GlassCard" ? Visibility.Visible : Visibility.Collapsed;
        }
        if (CardWidgetCompact != null)
        {
            CardWidgetCompact.BorderBrush = selectedTag == "Compact" ? accentBrush : subtleBorderBrush;
            CardWidgetCompact.BorderThickness = selectedTag == "Compact" ? new Thickness(2) : new Thickness(1);
            if (BadgeWidgetCompact != null) BadgeWidgetCompact.Visibility = selectedTag == "Compact" ? Visibility.Visible : Visibility.Collapsed;
        }
        if (CardWidgetMiniIsland != null)
        {
            CardWidgetMiniIsland.BorderBrush = selectedTag == "MiniIsland" ? accentBrush : subtleBorderBrush;
            CardWidgetMiniIsland.BorderThickness = selectedTag == "MiniIsland" ? new Thickness(2) : new Thickness(1);
            if (BadgeWidgetMiniIsland != null) BadgeWidgetMiniIsland.Visibility = selectedTag == "MiniIsland" ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void WidgetStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncing || ViewModel == null) return;
        if (sender is ComboBox cb && cb.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            SelectWidgetStyle(tag);
        }
    }

    private void WidgetOpacitySlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_isSyncing || ViewModel == null) return;
        double val = e.NewValue;
        double normalizedOpacity = val > 1.0 ? val / 100.0 : val;
        normalizedOpacity = Math.Clamp(normalizedOpacity, 0.2, 1.0);
        ViewModel.Settings.WidgetOpacity = normalizedOpacity;
        ViewModel.ApplySettings();

        foreach (var w in WidgetWindow.ActiveWidgets)
        {
            try { w.SetOpacity(normalizedOpacity); } catch { }
        }
    }

    private void OpenCurrentCityWidget_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        string chosenStyle = ViewModel.Settings.WidgetStyle ?? "BryanCDynamic";
        double chosenOpacity = ViewModel.Settings.WidgetOpacity;
        if (chosenOpacity <= 0.05) chosenOpacity = 1.0;

        var widgetWindow = new WidgetWindow(ViewModel);
        widgetWindow.ApplyWidgetStyle(chosenStyle);
        widgetWindow.SetOpacity(chosenOpacity);
        widgetWindow.Activate();
    }

    private async void OpenCustomCityWidget_Click(object sender, RoutedEventArgs e)
    {
        var inputTextBox = new TextBox
        {
            PlaceholderText = "Nhập tên thành phố (VD: Đà Nẵng, Tokyo, Paris, New York...)",
            Height = 38
        };
        var dialog = new ContentDialog
        {
            Title = "➕ Tạo Widget Mới Cho Thành Phố Khác",
            Content = inputTextBox,
            PrimaryButtonText = "Tạo Widget",
            CloseButtonText = "Hủy",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(inputTextBox.Text))
        {
            string city = inputTextBox.Text.Trim();
            string chosenStyle = ViewModel?.Settings.WidgetStyle ?? "BryanCDynamic";
            double chosenOpacity = ViewModel?.Settings.WidgetOpacity ?? 1.0;
            if (chosenOpacity <= 0.05) chosenOpacity = 1.0;

            try
            {
                var locService = new LocationService();
                var locs = await locService.SearchLocationsAsync(city);
                if (locs != null && locs.Count > 0)
                {
                    var first = locs[0];
                    var widget = new WidgetWindow(ViewModel, $"{first.Name}, {first.Country}", first.Latitude, first.Longitude);
                    widget.ApplyWidgetStyle(chosenStyle);
                    widget.SetOpacity(chosenOpacity);
                    widget.Activate();
                }
                else
                {
                    var widget = new WidgetWindow(ViewModel, city, 21.0285, 105.8542);
                    widget.ApplyWidgetStyle(chosenStyle);
                    widget.SetOpacity(chosenOpacity);
                    widget.Activate();
                }
            }
            catch
            {
                var widget = new WidgetWindow(ViewModel, city, 21.0285, 105.8542);
                widget.ApplyWidgetStyle(chosenStyle);
                widget.SetOpacity(chosenOpacity);
                widget.Activate();
            }
        }
    }

    private void CloseAllWidgets_Click(object sender, RoutedEventArgs e)
    {
        WidgetWindow.CloseAllWidgets();
    }
}
