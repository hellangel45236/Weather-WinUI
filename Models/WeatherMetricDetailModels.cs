using System.Collections.Generic;

namespace WeatherApp.Models;

public enum WeatherMetricType
{
    UvIndex,
    AirQuality,
    Wind,
    Humidity,
    Rain,
    Pressure,
    SunMoon,
    Pollutants
}

public class MetricDailyTrendItem
{
    public string DayName { get; set; } = string.Empty;
    public string DateDisplay { get; set; } = string.Empty;
    public string ValueText { get; set; } = string.Empty;
    public double NumericValue { get; set; }
    public double BarPercent { get; set; } = 0.5;
    public string BarColor { get; set; } = "#38BDF8";
    public string StatusBadge { get; set; } = string.Empty;
    public string IconGlyph { get; set; } = "\uf185";
    public string ConditionText { get; set; } = string.Empty;
    public string ExtraInfo { get; set; } = string.Empty;
}

public class MetricAdviceItem
{
    public string IconGlyph { get; set; } = "\uf0eb";
    public string IconColor { get; set; } = "#F59E0B";
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BadgeText { get; set; } = string.Empty;
    public string BadgeBackground { get; set; } = "#10B981";
}

public class WeatherMetricDetailPopupData
{
    public WeatherMetricType MetricType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string IconGlyph { get; set; } = "\uf185";
    public string IconColor { get; set; } = "#F59E0B";
    public string PrimaryValueText { get; set; } = string.Empty;
    public string UnitText { get; set; } = string.Empty;
    public string StatusBadgeText { get; set; } = string.Empty;
    public string StatusBadgeColor { get; set; } = "#10B981";
    public string SecondaryInfoText { get; set; } = string.Empty;
    public string ScientificExplanation { get; set; } = string.Empty;
    public string ScaleStandardName { get; set; } = string.Empty;
    public double GaugePercent { get; set; } = 0.5;
    public string GaugeGradientStart { get; set; } = "#10B981";
    public string GaugeGradientEnd { get; set; } = "#EF4444";
    public string GaugeMinLabel { get; set; } = "0";
    public string GaugeMidLabel { get; set; } = "5";
    public string GaugeMaxLabel { get; set; } = "11+";

    public List<MetricAdviceItem> Advices { get; set; } = new();
    public List<MetricDailyTrendItem> DailyTrends { get; set; } = new();
}
