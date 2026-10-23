using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using WeatherApp.ViewModels;
using WeatherApp.Helpers;

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
                oldVm.NothingEInkModeChanged -= tab.OnNothingEInkModeChanged;
            }
            if (e.NewValue is MainViewModel newVm)
            {
                newVm.PropertyChanged += tab.OnViewModelPropertyChanged;
                newVm.NothingEInkModeChanged += tab.OnNothingEInkModeChanged;
            }
            tab.RefreshAllVisuals();
        }
    }

    private void OnNothingEInkModeChanged()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            RefreshAllVisuals();
        });
    }

    private void OnThemeChanged(ElementTheme theme)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            RefreshAllVisuals();
        });
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
        else if (e.PropertyName == nameof(MainViewModel.IsNothingEInkMode) ||
                 e.PropertyName == nameof(MainViewModel.IsNothingDesign) ||
                 e.PropertyName == nameof(MainViewModel.SelectedDesignLanguage))
        {
            RefreshAllVisuals();
        }
    }

    public LifestyleTab()
    {
        this.InitializeComponent();

        this.Loaded += (s, e) =>
        {
            ThemeHelper.ThemeChanged += OnThemeChanged;
            RefreshAllVisuals();
            UpdateResponsiveLayout(this.ActualWidth);
        };

        this.Unloaded += (s, e) =>
        {
            ThemeHelper.ThemeChanged -= OnThemeChanged;
            if (ViewModel != null)
            {
                ViewModel.NothingEInkModeChanged -= OnNothingEInkModeChanged;
            }
        };

        this.ActualThemeChanged += (s, e) =>
        {
            RefreshAllVisuals();
        };

        this.SizeChanged += (s, e) =>
        {
            UpdateResponsiveLayout(e.NewSize.Width);
        };
    }

    public void RefreshAllVisuals()
    {
        UpdateOccasionButtonsVisual();
        UpdateGenderButtonsVisual();
        UpdateCopyButtonVisual();
        UpdateIconAndBadgeVisuals();
    }

    private void UpdateResponsiveLayout(double width)
    {
        if (width <= 0) return;

        bool isNarrow = width < 950;
        bool isUltraWide = width >= 1250;

        // 1. Palette & Fabric Grid (2 cột trên màn rộng, 1 cột xếp dọc trên màn hẹp)
        if (PaletteFabricGrid != null)
        {
            if (isNarrow)
            {
                PaletteFabricGrid.ColumnDefinitions.Clear();
                PaletteFabricGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                PaletteFabricGrid.RowDefinitions.Clear();
                PaletteFabricGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                PaletteFabricGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                PaletteFabricGrid.RowSpacing = 12;

                if (PaletteFabricGrid.Children.Count >= 2)
                {
                    Grid.SetColumn((FrameworkElement)PaletteFabricGrid.Children[0], 0);
                    Grid.SetRow((FrameworkElement)PaletteFabricGrid.Children[0], 0);
                    Grid.SetColumn((FrameworkElement)PaletteFabricGrid.Children[1], 0);
                    Grid.SetRow((FrameworkElement)PaletteFabricGrid.Children[1], 1);
                }
            }
            else
            {
                PaletteFabricGrid.RowDefinitions.Clear();
                PaletteFabricGrid.RowSpacing = 0;
                PaletteFabricGrid.ColumnDefinitions.Clear();
                PaletteFabricGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                PaletteFabricGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                if (PaletteFabricGrid.Children.Count >= 2)
                {
                    Grid.SetColumn((FrameworkElement)PaletteFabricGrid.Children[0], 0);
                    Grid.SetRow((FrameworkElement)PaletteFabricGrid.Children[0], 0);
                    Grid.SetColumn((FrameworkElement)PaletteFabricGrid.Children[1], 1);
                    Grid.SetRow((FrameworkElement)PaletteFabricGrid.Children[1], 0);
                }
            }
        }

        // 2. Khối 3: Khung giờ vận động & Chỉ số sinh hoạt
        if (BottomIndicesGrid != null)
        {
            if (isNarrow)
            {
                BottomIndicesGrid.ColumnDefinitions.Clear();
                BottomIndicesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                BottomIndicesGrid.RowDefinitions.Clear();
                BottomIndicesGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                BottomIndicesGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                BottomIndicesGrid.RowSpacing = 16;

                if (BottomIndicesGrid.Children.Count >= 2)
                {
                    Grid.SetColumn((FrameworkElement)BottomIndicesGrid.Children[0], 0);
                    Grid.SetRow((FrameworkElement)BottomIndicesGrid.Children[0], 0);
                    Grid.SetColumn((FrameworkElement)BottomIndicesGrid.Children[1], 0);
                    Grid.SetRow((FrameworkElement)BottomIndicesGrid.Children[1], 1);
                }
            }
            else
            {
                BottomIndicesGrid.RowDefinitions.Clear();
                BottomIndicesGrid.RowSpacing = 0;
                BottomIndicesGrid.ColumnDefinitions.Clear();
                BottomIndicesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                BottomIndicesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                if (BottomIndicesGrid.Children.Count >= 2)
                {
                    Grid.SetColumn((FrameworkElement)BottomIndicesGrid.Children[0], 0);
                    Grid.SetRow((FrameworkElement)BottomIndicesGrid.Children[0], 0);
                    Grid.SetColumn((FrameworkElement)BottomIndicesGrid.Children[1], 1);
                    Grid.SetRow((FrameworkElement)BottomIndicesGrid.Children[1], 0);
                }
            }
        }

        // 3. Capsule Bento Grid (4 món cốt lõi: 4 cột trên ultra wide, 2 cột trên chuẩn, 1 cột trên siêu hẹp)
        if (CapsuleBentoGrid != null && TopClothingCard != null && BottomClothingCard != null && OuterwearCard != null && FootwearCard != null)
        {
            if (isUltraWide)
            {
                CapsuleBentoGrid.RowDefinitions.Clear();
                CapsuleBentoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                CapsuleBentoGrid.RowSpacing = 0;
                CapsuleBentoGrid.ColumnDefinitions.Clear();
                for (int i = 0; i < 4; i++)
                    CapsuleBentoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                Grid.SetRow(TopClothingCard, 0); Grid.SetColumn(TopClothingCard, 0);
                Grid.SetRow(BottomClothingCard, 0); Grid.SetColumn(BottomClothingCard, 1);
                Grid.SetRow(OuterwearCard, 0); Grid.SetColumn(OuterwearCard, 2);
                Grid.SetRow(FootwearCard, 0); Grid.SetColumn(FootwearCard, 3);
            }
            else
            {
                CapsuleBentoGrid.RowDefinitions.Clear();
                CapsuleBentoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                CapsuleBentoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                CapsuleBentoGrid.RowSpacing = 12;
                CapsuleBentoGrid.ColumnDefinitions.Clear();
                CapsuleBentoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                CapsuleBentoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                Grid.SetRow(TopClothingCard, 0); Grid.SetColumn(TopClothingCard, 0);
                Grid.SetRow(BottomClothingCard, 0); Grid.SetColumn(BottomClothingCard, 1);
                Grid.SetRow(OuterwearCard, 1); Grid.SetColumn(OuterwearCard, 0);
                Grid.SetRow(FootwearCard, 1); Grid.SetColumn(FootwearCard, 1);
            }
        }
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

    private void UpdateCopyButtonVisual()
    {
        if (CopyOotdButton == null) return;

        bool isNothing = ViewModel?.IsNothingDesign == true;
        bool isEInk = isNothing && (ViewModel?.IsNothingEInkMode == true);
        bool isDark = !isEInk && ThemeHelper.IsDarkTheme(this);

        CopyOotdButton.BorderThickness = new Thickness(1);

        if (isEInk)
        {
            CopyOotdButton.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            CopyOotdButton.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 180, 180, 180));
            var black = new SolidColorBrush(Color.FromArgb(255, 17, 17, 17));
            CopyOotdButton.Foreground = black;
            ApplyContentColors(CopyOotdButton, black, black);
        }
        else if (isDark)
        {
            CopyOotdButton.Background = new SolidColorBrush(Color.FromArgb(32, 255, 255, 255));
            CopyOotdButton.BorderBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
            var white = new SolidColorBrush(Color.FromArgb(255, 248, 250, 252));
            CopyOotdButton.Foreground = white;
            var accentIcon = isNothing
                ? new SolidColorBrush(Color.FromArgb(255, 215, 25, 33))
                : new SolidColorBrush(Color.FromArgb(255, 236, 72, 153));
            ApplyContentColors(CopyOotdButton, white, accentIcon);
        }
        else
        {
            CopyOotdButton.Background = new SolidColorBrush(Color.FromArgb(255, 241, 245, 249));
            CopyOotdButton.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 203, 213, 225));
            var deepCharcoal = new SolidColorBrush(Color.FromArgb(255, 15, 23, 42));
            CopyOotdButton.Foreground = deepCharcoal;
            var accentIcon = isNothing
                ? new SolidColorBrush(Color.FromArgb(255, 215, 25, 33))
                : new SolidColorBrush(Color.FromArgb(255, 219, 39, 119));
            ApplyContentColors(CopyOotdButton, deepCharcoal, accentIcon);
        }
    }

    private void UpdateIconAndBadgeVisuals()
    {
        bool isNothing = ViewModel?.IsNothingDesign == true;
        bool isEInk = isNothing && (ViewModel?.IsNothingEInkMode == true);
        bool isDark = !isEInk && ThemeHelper.IsDarkTheme(this);

        if (isEInk)
        {
            // E-Ink mode: 100% black ink icons for high legibility
            var ink = new SolidColorBrush(Color.FromArgb(255, 17, 17, 17));
            if (OotdHeaderIconBorder != null) OotdHeaderIconBorder.Background = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0));
            if (OotdHeaderIcon != null) OotdHeaderIcon.Foreground = ink;
            if (FashionStyleTagText != null) FashionStyleTagText.Foreground = ink;
            if (RaincoatIcon != null) RaincoatIcon.Foreground = ink;
            if (TopClothingIcon != null) TopClothingIcon.Foreground = ink;
            if (BottomClothingIcon != null) BottomClothingIcon.Foreground = ink;
            if (OuterwearIcon != null) OuterwearIcon.Foreground = ink;
            if (FootwearIcon != null) FootwearIcon.Foreground = ink;
            if (PaletteIcon != null) PaletteIcon.Foreground = ink;
            if (FabricIcon != null) FabricIcon.Foreground = ink;
            if (AccessoriesIcon != null) AccessoriesIcon.Foreground = ink;
            if (WorkoutWindowsIcon != null) WorkoutWindowsIcon.Foreground = ink;
            if (LifestyleIndicesIcon != null) LifestyleIndicesIcon.Foreground = ink;
        }
        else if (isDark)
        {
            // Dark mode: luminous pastel tones
            var accent = isNothing
                ? new SolidColorBrush(Color.FromArgb(255, 215, 25, 33))
                : new SolidColorBrush(Color.FromArgb(255, 236, 72, 153));
            var accentBg = isNothing
                ? new SolidColorBrush(Color.FromArgb(35, 215, 25, 33))
                : new SolidColorBrush(Color.FromArgb(35, 236, 72, 153));

            if (OotdHeaderIconBorder != null) OotdHeaderIconBorder.Background = accentBg;
            if (OotdHeaderIcon != null) OotdHeaderIcon.Foreground = accent;
            if (FashionStyleTagText != null) FashionStyleTagText.Foreground = accent;
            if (RaincoatIcon != null) RaincoatIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 56, 189, 248)); // Sky-400
            if (TopClothingIcon != null) TopClothingIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 56, 189, 248));
            if (BottomClothingIcon != null) BottomClothingIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 129, 140, 248)); // Indigo-400
            if (OuterwearIcon != null) OuterwearIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 245, 158, 11)); // Amber-500
            if (FootwearIcon != null) FootwearIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129)); // Emerald-500
            if (PaletteIcon != null) PaletteIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 236, 72, 153)); // Rose-500
            if (FabricIcon != null) FabricIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 6, 182, 212)); // Cyan-500
            if (AccessoriesIcon != null) AccessoriesIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 96, 165, 250)); // Blue-400
            if (WorkoutWindowsIcon != null) WorkoutWindowsIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129));
            if (LifestyleIndicesIcon != null) LifestyleIndicesIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 139, 92, 246)); // Violet-500
        }
        else
        {
            // Light mode: deep jewel tones with high contrast on white/slate cards (> 4.5:1 ratio)
            var accent = isNothing
                ? new SolidColorBrush(Color.FromArgb(255, 215, 25, 33))
                : new SolidColorBrush(Color.FromArgb(255, 219, 39, 119));
            var accentBg = isNothing
                ? new SolidColorBrush(Color.FromArgb(30, 215, 25, 33))
                : new SolidColorBrush(Color.FromArgb(30, 219, 39, 119));

            if (OotdHeaderIconBorder != null) OotdHeaderIconBorder.Background = accentBg;
            if (OotdHeaderIcon != null) OotdHeaderIcon.Foreground = accent;
            if (FashionStyleTagText != null) FashionStyleTagText.Foreground = accent;
            if (RaincoatIcon != null) RaincoatIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 2, 132, 199)); // Sky-600
            if (TopClothingIcon != null) TopClothingIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 2, 132, 199));
            if (BottomClothingIcon != null) BottomClothingIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 79, 70, 229)); // Indigo-600
            if (OuterwearIcon != null) OuterwearIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 217, 119, 6)); // Amber-600
            if (FootwearIcon != null) FootwearIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 5, 150, 105)); // Emerald-600
            if (PaletteIcon != null) PaletteIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 190, 24, 93)); // Pink-700
            if (FabricIcon != null) FabricIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 8, 145, 178)); // Cyan-600
            if (AccessoriesIcon != null) AccessoriesIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 37, 99, 235)); // Blue-600
            if (WorkoutWindowsIcon != null) WorkoutWindowsIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 5, 150, 105));
            if (LifestyleIndicesIcon != null) LifestyleIndicesIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 124, 58, 237)); // Violet-600
        }
    }

    private void SetButtonState(Button? btn, bool isSelected)
    {
        if (btn == null) return;

        bool isNothing = ViewModel?.IsNothingDesign == true;
        bool isEInk = isNothing && (ViewModel?.IsNothingEInkMode == true);
        bool isDark = !isEInk && ThemeHelper.IsDarkTheme(this);

        btn.BorderThickness = new Thickness(1);

        if (isEInk)
        {
            // ==================== CHẾ ĐỘ E-INK (HIGH-CONTRAST MONOCHROME) ====================
            if (isSelected)
            {
                btn.Background = new SolidColorBrush(Color.FromArgb(255, 17, 17, 17));      // #111111 Solid Black
                btn.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));        // #000000 Solid Black
                var whiteText = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                btn.Foreground = whiteText;
                ApplyContentColors(btn, whiteText, whiteText);
            }
            else
            {
                btn.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));   // #FFFFFF Paper White
                btn.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 180, 180, 180));  // Clean border
                var blackText = new SolidColorBrush(Color.FromArgb(255, 17, 17, 17));       // #111111 Deep Carbon Ink
                btn.Foreground = blackText;
                ApplyContentColors(btn, blackText, new SolidColorBrush(Color.FromArgb(255, 70, 70, 70)));
            }
        }
        else if (isDark)
        {
            // ==================== CHẾ ĐỘ DARK MODE ====================
            if (isSelected)
            {
                var accentBg = isNothing
                    ? new SolidColorBrush(Color.FromArgb(255, 215, 25, 33))   // Nothing Red #D71921
                    : new SolidColorBrush(Color.FromArgb(255, 236, 72, 153)); // Fluent Rose #EC4899
                var accentBorder = isNothing
                    ? new SolidColorBrush(Color.FromArgb(255, 239, 68, 68))
                    : new SolidColorBrush(Color.FromArgb(255, 244, 114, 182));
                btn.Background = accentBg;
                btn.BorderBrush = accentBorder;
                var whiteText = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                btn.Foreground = whiteText;
                ApplyContentColors(btn, whiteText, whiteText);
            }
            else
            {
                btn.Background = new SolidColorBrush(Color.FromArgb(32, 255, 255, 255));
                btn.BorderBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
                var lightText = new SolidColorBrush(Color.FromArgb(255, 248, 250, 252));
                btn.Foreground = lightText;
                ApplyContentColors(btn, lightText, new SolidColorBrush(Color.FromArgb(200, 226, 232, 240)));
            }
        }
        else
        {
            // ==================== CHẾ ĐỘ LIGHT MODE ====================
            // KHẮC PHỤC TRIỆT ĐỂ LỖI CHỮ TRẮNG TÀNG HÌNH TRÊN NỀN SÁNG
            if (isSelected)
            {
                var accentBg = isNothing
                    ? new SolidColorBrush(Color.FromArgb(255, 215, 25, 33))   // Nothing Red #D71921
                    : new SolidColorBrush(Color.FromArgb(255, 219, 39, 119)); // Fluent Rose-600 #DB2777
                var accentBorder = isNothing
                    ? new SolidColorBrush(Color.FromArgb(255, 185, 28, 28))
                    : new SolidColorBrush(Color.FromArgb(255, 190, 24, 93));
                btn.Background = accentBg;
                btn.BorderBrush = accentBorder;
                var whiteText = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                btn.Foreground = whiteText;
                ApplyContentColors(btn, whiteText, whiteText);
            }
            else
            {
                btn.Background = new SolidColorBrush(Color.FromArgb(255, 241, 245, 249));  // #F1F5F9 Slate-100
                btn.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 203, 213, 225)); // #CBD5E1 Slate-300
                var deepCharcoalText = new SolidColorBrush(Color.FromArgb(255, 15, 23, 42)); // #0F172A Slate-900 (Ultra clear!)
                btn.Foreground = deepCharcoalText;
                ApplyContentColors(btn, deepCharcoalText, new SolidColorBrush(Color.FromArgb(255, 71, 85, 105))); // #475569
            }
        }
    }

    private static void ApplyContentColors(Button btn, Brush textColor, Brush iconColor)
    {
        if (btn.Content is StackPanel sp)
        {
            foreach (var child in sp.Children)
            {
                if (child is TextBlock tb) tb.Foreground = textColor;
                if (child is FontIcon fi) fi.Foreground = iconColor;
            }
        }
    }
}
