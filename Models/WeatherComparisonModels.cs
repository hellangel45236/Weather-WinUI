using System.Collections.Generic;
using WeatherApp.Models;

namespace WeatherApp.Models;

/// <summary>
/// Đại diện cho một thành phố gợi ý nhanh trong bảng so sánh thời tiết đối đầu
/// </summary>
public class QuickCompareCityItem
{
    public string Name { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string IconGlyph { get; set; } = "\uf3c5";
}

/// <summary>
/// Đại diện cho một hàng chỉ số so sánh đối đầu giữa 2 thành phố
/// </summary>
public class MetricComparisonRow
{
    public string MetricName { get; set; } = string.Empty;
    public string IconGlyph { get; set; } = "\uf76c";
    public string IconColor { get; set; } = "#38BDF8";

    public string ValueA { get; set; } = string.Empty;
    public string SubValueA { get; set; } = string.Empty;

    public string ValueB { get; set; } = string.Empty;
    public string SubValueB { get; set; } = string.Empty;

    public string DifferenceBadge { get; set; } = string.Empty;
    public string DifferenceBadgeColor { get; set; } = "#38BDF8"; // Màu badge chênh lệch
    public string AdvantageCity { get; set; } = string.Empty; // "A", "B", hoặc "Equal"
}

/// <summary>
/// Dữ liệu tổng hợp so sánh thời tiết đối đầu giữa 2 thành phố (Weather Duel)
/// </summary>
public class WeatherComparisonData
{
    public CurrentWeatherDisplay CityA { get; set; } = new();
    public CurrentWeatherDisplay CityB { get; set; } = new();

    // Huy hiệu so sánh nhanh nhiệt độ
    public string TempComparisonBadge { get; set; } = string.Empty;
    public string TempDiffText { get; set; } = string.Empty;

    // Phán quyết tổng thể mức độ dễ chịu (Comfort Verdict)
    public string VerdictTitle { get; set; } = string.Empty;
    public string VerdictDescription { get; set; } = string.Empty;
    public string VerdictBadgeText { get; set; } = "CÂN BẰNG";
    public string VerdictBadgeColor { get; set; } = "#10B981"; // Xanh ngọc hoặc Hổ phách

    // Danh sách các chỉ số so sánh chi tiết
    public List<MetricComparisonRow> MetricRows { get; set; } = new();

    // Lời khuyên di chuyển & sắp xếp hành lý
    public string TravelAdviceTitle { get; set; } = string.Empty;
    public string TravelAdviceDescription { get; set; } = string.Empty;
    public string PackingTips { get; set; } = string.Empty;
}
