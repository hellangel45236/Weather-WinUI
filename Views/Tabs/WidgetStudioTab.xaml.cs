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
        this.Loaded += (s, e) =>
        {
            SyncVisuals();
            UpdateResponsiveLayout(this.ActualWidth);
        };
        this.SizeChanged += (s, e) =>
        {
            UpdateResponsiveLayout(e.NewSize.Width);
        };
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
                double percent = op > 1.0 ? op : op * 100.0;
                StudioOpacitySlider.Value = percent;
                if (OpacityValueText != null)
                {
                    OpacityValueText.Text = $"{(int)percent}%";
                }
            }

            if (AlwaysOnTopSwitch != null)
            {
                AlwaysOnTopSwitch.IsOn = ViewModel.Settings.WidgetAlwaysOnTop;
                if (GhostModeSwitch != null) GhostModeSwitch.IsOn = ViewModel.Settings.WidgetGhostClickThrough;
            }

            if (style == "SeasonalCard")
            {
                string seasonalMode = ViewModel.Settings.SeasonalWidgetTheme;
                if (string.IsNullOrEmpty(seasonalMode) || string.Equals(seasonalMode, "Auto", StringComparison.OrdinalIgnoreCase))
                {
                    seasonalMode = ViewModel.CurrentSeasonalSkin?.Id.ToString() ?? "Tet";
                }
                UpdateSeasonalCardsVisuals(seasonalMode);
            }
            else
            {
                UpdateSeasonalCardsVisuals(string.Empty);
            }
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private void UpdateResponsiveLayout(double width)
    {
        if (width <= 0) return;

        bool isNarrow = width < 950;

        // 1. Gallery Grid (Chuyển giữa 2x2 trên màn rộng và 1x4 trên màn hẹp)
        if (GalleryGrid != null)
        {
            if (isNarrow)
            {
                GalleryGrid.ColumnDefinitions.Clear();
                GalleryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                GalleryGrid.RowDefinitions.Clear();
                for (int r = 0; r < 14; r++) GalleryGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                if (CardWidgetBryanC != null) { Grid.SetColumn(CardWidgetBryanC, 0); Grid.SetRow(CardWidgetBryanC, 0); }
                if (CardWidgetGlassCard != null) { Grid.SetColumn(CardWidgetGlassCard, 0); Grid.SetRow(CardWidgetGlassCard, 1); }
                if (CardWidgetCompact != null) { Grid.SetColumn(CardWidgetCompact, 0); Grid.SetRow(CardWidgetCompact, 2); }
                if (CardWidgetMiniIsland != null) { Grid.SetColumn(CardWidgetMiniIsland, 0); Grid.SetRow(CardWidgetMiniIsland, 3); }
                if (CardWidgetBentoVivid != null) { Grid.SetColumn(CardWidgetBentoVivid, 0); Grid.SetRow(CardWidgetBentoVivid, 4); }
                if (CardWidgetBentoTactical != null) { Grid.SetColumn(CardWidgetBentoTactical, 0); Grid.SetRow(CardWidgetBentoTactical, 5); }
                if (CardWidgetRetroLcd != null) { Grid.SetColumn(CardWidgetRetroLcd, 0); Grid.SetRow(CardWidgetRetroLcd, 6); Grid.SetColumnSpan(CardWidgetRetroLcd, 1); }
                if (CardWidgetNothingBento != null) { Grid.SetColumn(CardWidgetNothingBento, 0); Grid.SetRow(CardWidgetNothingBento, 7); }
                if (CardWidgetNothingSquare != null) { Grid.SetColumn(CardWidgetNothingSquare, 0); Grid.SetRow(CardWidgetNothingSquare, 8); }
                if (CardWidgetNothingCircular != null) { Grid.SetColumn(CardWidgetNothingCircular, 0); Grid.SetRow(CardWidgetNothingCircular, 9); }
                if (CardWidgetNothingPill != null) { Grid.SetColumn(CardWidgetNothingPill, 0); Grid.SetRow(CardWidgetNothingPill, 10); }
                if (CardWidgetDynamicIsland != null) { Grid.SetColumn(CardWidgetDynamicIsland, 0); Grid.SetRow(CardWidgetDynamicIsland, 11); }
                if (CardWidgetTeenageDeck != null) { Grid.SetColumn(CardWidgetTeenageDeck, 0); Grid.SetRow(CardWidgetTeenageDeck, 12); }
                if (CardWidgetPureTypo != null) { Grid.SetColumn(CardWidgetPureTypo, 0); Grid.SetRow(CardWidgetPureTypo, 13); Grid.SetColumnSpan(CardWidgetPureTypo, 1); }
            }

            else
            {
                GalleryGrid.ColumnDefinitions.Clear();
                GalleryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                GalleryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                GalleryGrid.RowDefinitions.Clear();
                for (int r = 0; r < 8; r++) GalleryGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                if (CardWidgetBryanC != null) { Grid.SetColumn(CardWidgetBryanC, 0); Grid.SetRow(CardWidgetBryanC, 0); }
                if (CardWidgetGlassCard != null) { Grid.SetColumn(CardWidgetGlassCard, 1); Grid.SetRow(CardWidgetGlassCard, 0); }
                if (CardWidgetCompact != null) { Grid.SetColumn(CardWidgetCompact, 0); Grid.SetRow(CardWidgetCompact, 1); }
                if (CardWidgetMiniIsland != null) { Grid.SetColumn(CardWidgetMiniIsland, 1); Grid.SetRow(CardWidgetMiniIsland, 1); }
                if (CardWidgetBentoVivid != null) { Grid.SetColumn(CardWidgetBentoVivid, 0); Grid.SetRow(CardWidgetBentoVivid, 2); }
                if (CardWidgetBentoTactical != null) { Grid.SetColumn(CardWidgetBentoTactical, 1); Grid.SetRow(CardWidgetBentoTactical, 2); }
                if (CardWidgetRetroLcd != null) { Grid.SetColumn(CardWidgetRetroLcd, 0); Grid.SetRow(CardWidgetRetroLcd, 3); Grid.SetColumnSpan(CardWidgetRetroLcd, 2); }
                if (CardWidgetNothingBento != null) { Grid.SetColumn(CardWidgetNothingBento, 0); Grid.SetRow(CardWidgetNothingBento, 4); }
                if (CardWidgetNothingSquare != null) { Grid.SetColumn(CardWidgetNothingSquare, 1); Grid.SetRow(CardWidgetNothingSquare, 4); }
                if (CardWidgetNothingCircular != null) { Grid.SetColumn(CardWidgetNothingCircular, 0); Grid.SetRow(CardWidgetNothingCircular, 5); }
                if (CardWidgetNothingPill != null) { Grid.SetColumn(CardWidgetNothingPill, 1); Grid.SetRow(CardWidgetNothingPill, 5); }
                if (CardWidgetDynamicIsland != null) { Grid.SetColumn(CardWidgetDynamicIsland, 0); Grid.SetRow(CardWidgetDynamicIsland, 6); }
                if (CardWidgetTeenageDeck != null) { Grid.SetColumn(CardWidgetTeenageDeck, 1); Grid.SetRow(CardWidgetTeenageDeck, 6); }
                if (CardWidgetPureTypo != null) { Grid.SetColumn(CardWidgetPureTypo, 0); Grid.SetRow(CardWidgetPureTypo, 7); Grid.SetColumnSpan(CardWidgetPureTypo, 2); }
            }
        }

        // 1b. Seasonal Gallery Grid (Chuyển giữa 2x4 trên màn rộng và 1x8 trên màn hẹp)
        if (SeasonalGalleryGrid != null)
        {
            var sCards = new[] { CardSeasonalTet, CardSeasonalMidAutumn, CardSeasonalChristmas, CardSeasonalHalloween, CardSeasonalSpring, CardSeasonalSummer, CardSeasonalAutumn, CardSeasonalWinter };
            if (isNarrow)
            {
                SeasonalGalleryGrid.ColumnDefinitions.Clear();
                SeasonalGalleryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                SeasonalGalleryGrid.RowDefinitions.Clear();
                for (int i = 0; i < 8; i++) SeasonalGalleryGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                for (int i = 0; i < sCards.Length; i++)
                {
                    if (sCards[i] != null) { Grid.SetColumn(sCards[i], 0); Grid.SetRow(sCards[i], i); }
                }
            }
            else
            {
                SeasonalGalleryGrid.ColumnDefinitions.Clear();
                SeasonalGalleryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                SeasonalGalleryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                SeasonalGalleryGrid.RowDefinitions.Clear();
                for (int i = 0; i < 4; i++) SeasonalGalleryGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                for (int i = 0; i < sCards.Length; i++)
                {
                    if (sCards[i] != null)
                    {
                        Grid.SetColumn(sCards[i], i % 2);
                        Grid.SetRow(sCards[i], i / 2);
                    }
                }
            }
        }

        // 2. Controls Grid
        if (ControlsGrid != null)
        {
            if (isNarrow)
            {
                ControlsGrid.ColumnDefinitions.Clear();
                ControlsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                ControlsGrid.RowDefinitions.Clear();
                ControlsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                ControlsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                if (ControlsGrid.Children.Count >= 2)
                {
                    Grid.SetColumn((FrameworkElement)ControlsGrid.Children[0], 0);
                    Grid.SetRow((FrameworkElement)ControlsGrid.Children[0], 0);
                    Grid.SetColumn((FrameworkElement)ControlsGrid.Children[1], 0);
                    Grid.SetRow((FrameworkElement)ControlsGrid.Children[1], 1);
                }
            }
            else
            {
                ControlsGrid.RowDefinitions.Clear();
                ControlsGrid.ColumnDefinitions.Clear();
                ControlsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                ControlsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                if (ControlsGrid.Children.Count >= 2)
                {
                    Grid.SetColumn((FrameworkElement)ControlsGrid.Children[0], 0);
                    Grid.SetRow((FrameworkElement)ControlsGrid.Children[0], 0);
                    Grid.SetColumn((FrameworkElement)ControlsGrid.Children[1], 1);
                    Grid.SetRow((FrameworkElement)ControlsGrid.Children[1], 0);
                }
            }
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

    private void Card_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            border.Opacity = 0.95;
        }
    }

    private void Card_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            border.Opacity = 1.0;
        }
    }

    private void CardWidgetBentoVivid_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        SelectWidgetStyle("BentoVivid");
    }

    private void CardWidgetBentoTactical_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        SelectWidgetStyle("BentoTactical");
    }

    private void CardWidgetRetroLcd_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        SelectWidgetStyle("RetroLcd");
    }

    private void CardWidgetNothingBento_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        SelectWidgetStyle("NothingBento");
    }

    private void CardWidgetNothingSquare_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        SelectWidgetStyle("NothingSquare");
    }

    private void CardWidgetNothingCircular_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        SelectWidgetStyle("NothingCircular");
    }

    private void CardWidgetNothingPill_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) => SelectWidgetStyle("NothingPill");
    private void CardWidgetDynamicIsland_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) => SelectWidgetStyle("DynamicIsland");
    private void CardWidgetTeenageDeck_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) => SelectWidgetStyle("TeenageDeck");
    private void CardWidgetPureTypo_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) => SelectWidgetStyle("PureTypoHud");
    private void CardWidgetNothingPill_Unused(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        SelectWidgetStyle("NothingPill");
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
        if (styleTag == "SeasonalCard")
        {
            string seasonalMode = ViewModel.Settings.SeasonalWidgetTheme;
            if (string.IsNullOrEmpty(seasonalMode) || string.Equals(seasonalMode, "Auto", StringComparison.OrdinalIgnoreCase))
            {
                seasonalMode = ViewModel.CurrentSeasonalSkin?.Id.ToString() ?? "Tet";
            }
            UpdateSeasonalCardsVisuals(seasonalMode);
        }
        else
        {
            UpdateSeasonalCardsVisuals(string.Empty);
        }

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
        if (CardWidgetBentoVivid != null)
        {
            CardWidgetBentoVivid.BorderBrush = selectedTag == "BentoVivid" ? accentBrush : subtleBorderBrush;
            CardWidgetBentoVivid.BorderThickness = selectedTag == "BentoVivid" ? new Thickness(2) : new Thickness(1);
            if (BadgeWidgetBentoVivid != null) BadgeWidgetBentoVivid.Visibility = selectedTag == "BentoVivid" ? Visibility.Visible : Visibility.Collapsed;
        }
        if (CardWidgetBentoTactical != null)
        {
            CardWidgetBentoTactical.BorderBrush = selectedTag == "BentoTactical" ? accentBrush : subtleBorderBrush;
            CardWidgetBentoTactical.BorderThickness = selectedTag == "BentoTactical" ? new Thickness(2) : new Thickness(1);
            if (BadgeWidgetBentoTactical != null) BadgeWidgetBentoTactical.Visibility = selectedTag == "BentoTactical" ? Visibility.Visible : Visibility.Collapsed;
        }
        if (CardWidgetRetroLcd != null)
        {
            CardWidgetRetroLcd.BorderBrush = selectedTag == "RetroLcd" ? accentBrush : subtleBorderBrush;
            CardWidgetRetroLcd.BorderThickness = selectedTag == "RetroLcd" ? new Thickness(2) : new Thickness(1);
            if (BadgeWidgetRetroLcd != null) BadgeWidgetRetroLcd.Visibility = selectedTag == "RetroLcd" ? Visibility.Visible : Visibility.Collapsed;
        }
        if (CardWidgetNothingBento != null)
        {
            CardWidgetNothingBento.BorderBrush = selectedTag == "NothingBento" ? accentBrush : subtleBorderBrush;
            CardWidgetNothingBento.BorderThickness = selectedTag == "NothingBento" ? new Thickness(2) : new Thickness(1);
            if (BadgeWidgetNothingBento != null) BadgeWidgetNothingBento.Visibility = selectedTag == "NothingBento" ? Visibility.Visible : Visibility.Collapsed;
        }
        if (CardWidgetNothingSquare != null)
        {
            CardWidgetNothingSquare.BorderBrush = selectedTag == "NothingSquare" ? accentBrush : subtleBorderBrush;
            CardWidgetNothingSquare.BorderThickness = selectedTag == "NothingSquare" ? new Thickness(2) : new Thickness(1);
            if (BadgeWidgetNothingSquare != null) BadgeWidgetNothingSquare.Visibility = selectedTag == "NothingSquare" ? Visibility.Visible : Visibility.Collapsed;
        }
        if (CardWidgetNothingCircular != null)
        {
            CardWidgetNothingCircular.BorderBrush = selectedTag == "NothingCircular" ? accentBrush : subtleBorderBrush;
            CardWidgetNothingCircular.BorderThickness = selectedTag == "NothingCircular" ? new Thickness(2) : new Thickness(1);
            if (BadgeWidgetNothingCircular != null) BadgeWidgetNothingCircular.Visibility = selectedTag == "NothingCircular" ? Visibility.Visible : Visibility.Collapsed;
        }
        if (CardWidgetNothingPill != null)
        {
            CardWidgetNothingPill.BorderBrush = selectedTag == "NothingPill" ? accentBrush : subtleBorderBrush;
            CardWidgetNothingPill.BorderThickness = selectedTag == "NothingPill" ? new Thickness(2) : new Thickness(1);
            if (BadgeWidgetNothingPill != null) BadgeWidgetNothingPill.Visibility = selectedTag == "NothingPill" ? Visibility.Visible : Visibility.Collapsed;
            if (CardWidgetDynamicIsland != null)
            {
                CardWidgetDynamicIsland.BorderBrush = selectedTag == "DynamicIsland" ? accentBrush : subtleBorderBrush;
                CardWidgetDynamicIsland.BorderThickness = selectedTag == "DynamicIsland" ? new Thickness(2) : new Thickness(1);
                if (BadgeWidgetDynamicIsland != null) BadgeWidgetDynamicIsland.Visibility = selectedTag == "DynamicIsland" ? Visibility.Visible : Visibility.Collapsed;
            }
            if (CardWidgetTeenageDeck != null)
            {
                CardWidgetTeenageDeck.BorderBrush = selectedTag == "TeenageDeck" ? accentBrush : subtleBorderBrush;
                CardWidgetTeenageDeck.BorderThickness = selectedTag == "TeenageDeck" ? new Thickness(2) : new Thickness(1);
                if (BadgeWidgetTeenageDeck != null) BadgeWidgetTeenageDeck.Visibility = selectedTag == "TeenageDeck" ? Visibility.Visible : Visibility.Collapsed;
            }
            if (CardWidgetPureTypo != null)
            {
                CardWidgetPureTypo.BorderBrush = selectedTag == "PureTypoHud" ? accentBrush : subtleBorderBrush;
                CardWidgetPureTypo.BorderThickness = selectedTag == "PureTypoHud" ? new Thickness(2) : new Thickness(1);
                if (BadgeWidgetPureTypo != null) BadgeWidgetPureTypo.Visibility = selectedTag == "PureTypoHud" ? Visibility.Visible : Visibility.Collapsed;
            }
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
        if (OpacityValueText != null)
        {
            OpacityValueText.Text = $"{(int)e.NewValue}%";
        }

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

    private void GhostModeSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isSyncing || ViewModel == null) return;
        if (sender is ToggleSwitch ts)
        {
            ViewModel.Settings.WidgetGhostClickThrough = ts.IsOn;
            ViewModel.ApplySettings();
            foreach (var w in WidgetWindow.ActiveWidgets)
            {
                try { w.SetGhostClickThrough(ts.IsOn); } catch { }
            }
        }
    }

    private void AlwaysOnTopSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isSyncing || ViewModel == null) return;
        if (sender is ToggleSwitch ts)
        {
            ViewModel.Settings.WidgetAlwaysOnTop = ts.IsOn;
            ViewModel.ApplySettings();

            foreach (var w in WidgetWindow.ActiveWidgets)
            {
                try { w.SetAlwaysOnTop(ts.IsOn); } catch { }
            }
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
        widgetWindow.SetAlwaysOnTop(ViewModel.Settings.WidgetAlwaysOnTop);
        widgetWindow.Activate();
    }

    private async void OpenCustomCityWidget_Click(object sender, RoutedEventArgs e)
    {
        var loc = LocalizationService.Instance;
        var inputTextBox = new TextBox
        {
            PlaceholderText = loc.WidgetStudioCityDialogPlaceholder,
            Height = 38
        };
        var dialog = new ContentDialog
        {
            Title = loc.WidgetStudioCityDialogTitle,
            Content = inputTextBox,
            PrimaryButtonText = loc.WidgetStudioCityDialogCreate,
            CloseButtonText = loc.WidgetStudioCityDialogCancel,
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
            bool alwaysOnTop = ViewModel?.Settings.WidgetAlwaysOnTop ?? true;

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
                    widget.SetAlwaysOnTop(alwaysOnTop);
                    widget.Activate();
                }
                else
                {
                    var widget = new WidgetWindow(ViewModel, city, 21.0285, 105.8542);
                    widget.ApplyWidgetStyle(chosenStyle);
                    widget.SetOpacity(chosenOpacity);
                    widget.SetAlwaysOnTop(alwaysOnTop);
                    widget.Activate();
                }
            }
            catch
            {
                var widget = new WidgetWindow(ViewModel, city, 21.0285, 105.8542);
                widget.ApplyWidgetStyle(chosenStyle);
                widget.SetOpacity(chosenOpacity);
                widget.SetAlwaysOnTop(alwaysOnTop);
                widget.Activate();
            }
        }
    }

    private void CloseAllWidgets_Click(object sender, RoutedEventArgs e)
    {
        WidgetWindow.CloseAllWidgets();
    }

    private void SeasonalWidgetCard_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement elem && elem.Tag is string tag)
        {
            PinSeasonalWidgetToDesktop(tag);
        }
    }

    private void PinSeasonalWidget_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement elem && elem.Tag is string tag)
        {
            PinSeasonalWidgetToDesktop(tag);
        }
    }

    private void PinCurrentSeasonalWidget_Click(object sender, RoutedEventArgs e)
    {
        string currentTag = ViewModel?.CurrentSeasonalSkin?.Id.ToString() ?? "Tet";
        PinSeasonalWidgetToDesktop(currentTag);
    }

    private void PinSeasonalWidgetToDesktop(string skinTag)
    {
        if (ViewModel == null) return;

        // 1. Đồng bộ skin theo mùa trên toàn app
        ViewModel.ApplySeasonalSkin(skinTag, save: true);

        // 2. Cập nhật phong cách widget sang SeasonalCard & lưu theme mùa
        ViewModel.Settings.WidgetStyle = "SeasonalCard";
        ViewModel.Settings.SeasonalWidgetTheme = skinTag;
        ViewModel.ApplySettings();

        _isSyncing = true;
        if (StudioWidgetStyleComboBox != null)
        {
            for (int i = 0; i < StudioWidgetStyleComboBox.Items.Count; i++)
            {
                if (StudioWidgetStyleComboBox.Items[i] is ComboBoxItem item && item.Tag?.ToString() == "SeasonalCard")
                {
                    StudioWidgetStyleComboBox.SelectedIndex = i;
                    break;
                }
            }
        }
        _isSyncing = false;

        UpdateWidgetCardsVisuals("SeasonalCard");
        UpdateSeasonalCardsVisuals(skinTag);

        // 3. Khởi tạo hoặc cập nhật WidgetWindow trên Desktop
        var targetSkin = SeasonalSkinService.Instance.GetActiveSkin(skinTag);
        double chosenOpacity = ViewModel.Settings.WidgetOpacity;
        if (chosenOpacity <= 0.05) chosenOpacity = 1.0;
        bool alwaysOnTop = ViewModel.Settings.WidgetAlwaysOnTop;

        // Nếu đã có Widget đang mở, áp dụng skin mùa ngay lập tức
        if (WidgetWindow.ActiveWidgets.Count > 0)
        {
            foreach (var w in WidgetWindow.ActiveWidgets)
            {
                try
                {
                    w.ApplySeasonalTheme(targetSkin);
                    w.Activate();
                }
                catch { }
            }
        }
        else
        {
        // Nếu đã có Widget đang mở, áp dụng skin mùa ngay lập tức
            var widgetWindow = new WidgetWindow(ViewModel);
            widgetWindow.ApplySeasonalTheme(targetSkin);
            widgetWindow.SetOpacity(chosenOpacity);
            widgetWindow.SetAlwaysOnTop(alwaysOnTop);
            widgetWindow.Activate();
        }
    }

    private void UpdateSeasonalCardsVisuals(string currentSkinTag)
    {
        var cards = new (Border? card, Border? badge, string tag)[]
        {
            (CardSeasonalTet, BadgeSeasonalTet, "Tet"),
            (CardSeasonalMidAutumn, BadgeSeasonalMidAutumn, "MidAutumn"),
            (CardSeasonalChristmas, BadgeSeasonalChristmas, "Christmas"),
            (CardSeasonalHalloween, BadgeSeasonalHalloween, "Halloween"),
            (CardSeasonalSpring, BadgeSeasonalSpring, "Spring"),
            (CardSeasonalSummer, BadgeSeasonalSummer, "Summer"),
            (CardSeasonalAutumn, BadgeSeasonalAutumn, "Autumn"),
            (CardSeasonalWinter, BadgeSeasonalWinter, "Winter")
        };

        foreach (var (card, badge, tag) in cards)
        {
            if (card == null) continue;
            bool isSelected = string.Equals(tag, currentSkinTag, StringComparison.OrdinalIgnoreCase);

            if (isSelected)
            {
                card.BorderThickness = new Thickness(2.5);
                if (badge != null) badge.Visibility = Visibility.Visible;
            }
            else
            {
                card.BorderThickness = new Thickness(1.5);
                if (badge != null) badge.Visibility = Visibility.Collapsed;
            }
        }
    }

        public void ScrollToBentoSection()
    {
        try
        {
            WidgetScrollViewer?.ChangeView(null, 1100, null, false);
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                WidgetScrollViewer?.ChangeView(null, 1100, null, false);
            });
        }
        catch { }
    }
    public void ScrollToSeasonalSection()
    {
        try
        {
            double targetOffset = WidgetScrollViewer?.ScrollableHeight > 100 ? WidgetScrollViewer.ScrollableHeight : 1600;
            WidgetScrollViewer?.ChangeView(null, targetOffset, null, false);
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                double finalOffset = WidgetScrollViewer?.ScrollableHeight > 100 ? WidgetScrollViewer.ScrollableHeight : 1600;
                WidgetScrollViewer?.ChangeView(null, finalOffset, null, false);
            });
        }
        catch { }
    }
}
