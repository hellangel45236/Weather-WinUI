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

        // 1. Gallery Grid (Chuyá»ƒn giá»¯a 2x2 trÃªn mÃ n rá»™ng vÃ  1x4 trÃªn mÃ n háº¹p)
        if (GalleryGrid != null)
        {
            if (isNarrow)
            {
                GalleryGrid.ColumnDefinitions.Clear();
                GalleryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                GalleryGrid.RowDefinitions.Clear();
                for (int r = 0; r < 7; r++) GalleryGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                if (CardWidgetBryanC != null) { Grid.SetColumn(CardWidgetBryanC, 0); Grid.SetRow(CardWidgetBryanC, 0); }
                if (CardWidgetGlassCard != null) { Grid.SetColumn(CardWidgetGlassCard, 0); Grid.SetRow(CardWidgetGlassCard, 1); }
                if (CardWidgetCompact != null) { Grid.SetColumn(CardWidgetCompact, 0); Grid.SetRow(CardWidgetCompact, 2); }
                if (CardWidgetMiniIsland != null) { Grid.SetColumn(CardWidgetMiniIsland, 0); Grid.SetRow(CardWidgetMiniIsland, 3); }
                if (CardWidgetBentoVivid != null) { Grid.SetColumn(CardWidgetBentoVivid, 0); Grid.SetRow(CardWidgetBentoVivid, 4); }
                if (CardWidgetBentoTactical != null) { Grid.SetColumn(CardWidgetBentoTactical, 0); Grid.SetRow(CardWidgetBentoTactical, 5); }
                if (CardWidgetRetroLcd != null) { Grid.SetColumn(CardWidgetRetroLcd, 0); Grid.SetRow(CardWidgetRetroLcd, 6); Grid.SetColumnSpan(CardWidgetRetroLcd, 1); }
            }

            else
            {
                GalleryGrid.ColumnDefinitions.Clear();
                GalleryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                GalleryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                GalleryGrid.RowDefinitions.Clear();
                for (int r = 0; r < 4; r++) GalleryGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                if (CardWidgetBryanC != null) { Grid.SetColumn(CardWidgetBryanC, 0); Grid.SetRow(CardWidgetBryanC, 0); }
                if (CardWidgetGlassCard != null) { Grid.SetColumn(CardWidgetGlassCard, 1); Grid.SetRow(CardWidgetGlassCard, 0); }
                if (CardWidgetCompact != null) { Grid.SetColumn(CardWidgetCompact, 0); Grid.SetRow(CardWidgetCompact, 1); }
                if (CardWidgetMiniIsland != null) { Grid.SetColumn(CardWidgetMiniIsland, 1); Grid.SetRow(CardWidgetMiniIsland, 1); }
                if (CardWidgetBentoVivid != null) { Grid.SetColumn(CardWidgetBentoVivid, 0); Grid.SetRow(CardWidgetBentoVivid, 2); }
                if (CardWidgetBentoTactical != null) { Grid.SetColumn(CardWidgetBentoTactical, 1); Grid.SetRow(CardWidgetBentoTactical, 2); }
                if (CardWidgetRetroLcd != null) { Grid.SetColumn(CardWidgetRetroLcd, 0); Grid.SetRow(CardWidgetRetroLcd, 3); Grid.SetColumnSpan(CardWidgetRetroLcd, 2); }
            }
        }

        // 1b. Seasonal Gallery Grid (Chuyá»ƒn giá»¯a 2x4 trÃªn mÃ n rá»™ng vÃ  1x8 trÃªn mÃ n háº¹p)
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

        // 1. Äá»“ng bá»™ skin theo mÃ¹a trÃªn toÃ n app
        ViewModel.ApplySeasonalSkin(skinTag, save: true);

        // 2. Cáº­p nháº­t phong cÃ¡ch widget sang SeasonalCard & lÆ°u theme mÃ¹a
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

        // 3. Khá»Ÿi táº¡o hoáº·c cáº­p nháº­t WidgetWindow trÃªn Desktop
        var targetSkin = SeasonalSkinService.Instance.GetActiveSkin(skinTag);
        double chosenOpacity = ViewModel.Settings.WidgetOpacity;
        if (chosenOpacity <= 0.05) chosenOpacity = 1.0;
        bool alwaysOnTop = ViewModel.Settings.WidgetAlwaysOnTop;

        // Náº¿u Ä‘Ã£ cÃ³ Widget Ä‘ang má»Ÿ, Ã¡p dá»¥ng skin mÃ¹a ngay láº­p tá»©c
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
            // Náº¿u chÆ°a má»Ÿ Widget nÃ o, ghim má»™t Widget má»›i ra Desktop
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
