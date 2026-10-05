using System.Collections.Generic;
using System.Linq;

namespace WeatherApp.Models;

public class FloodHotspotRoad
{
    public string District { get; set; } = string.Empty;
    public string StreetName { get; set; } = string.Empty;
    public string EstimatedDepth { get; set; } = string.Empty;
    public string Cause { get; set; } = "Do Mưa Lớn"; // "Do Triều Cường", "Do Mưa Lớn", "Mưa + Triều Cường"
    public int SeverityLevel { get; set; } = 1; // 1 = Nhẹ, 2 = Trung bình, 3 = Nặng
    public string SeverityText => SeverityLevel switch
    {
        3 => "Ngập sâu (Nguy hiểm)",
        2 => "Ngập vừa (Xe máy khó đi)",
        _ => "Đọng nước cục bộ"
    };
    public string SeverityColor => SeverityLevel switch
    {
        3 => "#EF4444",
        2 => "#EA580C",
        _ => "#F59E0B"
    };
    public string SeverityBg => SeverityLevel switch
    {
        3 => "#20EF4444",
        2 => "#20EA580C",
        _ => "#20F59E0B"
    };
    public string Note { get; set; } = string.Empty;
    public string IconGlyph => Cause.Contains("Triều") ? "\uf773" : "\uf73d";
}

public class UrbanFloodWarning
{
    public bool IsRelevantCity { get; set; } = true;
    public string CityType { get; set; } = "General"; // "Hcm", "Hanoi", "General"
    public string CityDisplayName { get; set; } = "Khu vực đô thị";
    
    // Mức độ rủi ro: 0 = An toàn (Xanh), 1 = Nhẹ (Vàng), 2 = Cảnh báo cao (Cam), 3 = Báo động đỏ (Đỏ)
    public int RiskLevel { get; set; } = 0;
    
    public string RiskTitle => RiskLevel switch
    {
        3 => "BÁO ĐỘNG ĐỎ NGẬP SÂU",
        2 => "CẢNH BÁO NGẬP ÚNG CAO",
        1 => "NGUY CƠ NGẬP NHẸ",
        _ => "AN TOÀN - THÔNG THOÁNG"
    };

    public string RiskBadgeColor => RiskLevel switch
    {
        3 => "#EF4444",
        2 => "#EA580C",
        1 => "#F59E0B",
        _ => "#10B981"
    };

    public string RiskBadgeBg => RiskLevel switch
    {
        3 => "#25EF4444",
        2 => "#25EA580C",
        1 => "#25F59E0B",
        _ => "#2010B981"
    };

    public string RiskIconGlyph => RiskLevel switch
    {
        3 => "\uf071", // Exclamation triangle
        2 => "\uf773", // Water wave
        1 => "\uf73d", // Cloud showers
        _ => "\uf058"  // Check circle
    };

    public string Headline { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;

    // Dữ liệu Triều cường (Đặc quyền TP.HCM và vùng hạ lưu)
    public bool HasTideData { get; set; } = false;
    public string TideStatusText { get; set; } = "Triều bình thường";
    public string TidePhaseText { get; set; } = "Kỳ Triều Kém";
    public string TidePeakMorning { get; set; } = "--:--";
    public string TidePeakEvening { get; set; } = "--:--";
    public double CurrentTideLevel { get; set; } = 1.25;
    public string CurrentTideLevelDisplay { get; set; } = "1.25 m (An toàn)";
    public bool IsCurrentlyPeakTide { get; set; } = false;
    public string TideAlertBadge => CurrentTideLevel switch
    {
        >= 1.65 => "Báo động 3+ (Ngập nặng)",
        >= 1.55 => "Báo động 2 (Ngập mép đường)",
        >= 1.40 => "Báo động 1 (Triều cao)",
        _ => "Dưới Báo Động (An toàn)"
    };
    public string TideAlertColor => CurrentTideLevel switch
    {
        >= 1.65 => "#EF4444",
        >= 1.55 => "#EA580C",
        >= 1.40 => "#F59E0B",
        _ => "#10B981"
    };

    // Dữ liệu Mưa & Thoát nước (Hà Nội, TP.HCM & các đô thị)
    public double RainIntensity { get; set; } = 0.0;
    public string RainIntensityDisplay => $"{RainIntensity:F1} mm/h";
    public string RainDrainageStatus { get; set; } = "Hệ thống thoát nước tốt";
    public string RainDrainageColor { get; set; } = "#10B981";

    // Khuyến nghị lưu thông xe máy & ô tô
    public string MotorbikeAdvice { get; set; } = string.Empty;
    public string CarAdvice { get; set; } = string.Empty;

    // Danh sách điểm ngập trọng điểm
    public List<FloodHotspotRoad> HotspotRoads { get; set; } = new();
    public int HotspotRoadsCount => HotspotRoads?.Count ?? 0;
    public bool HasHotspotRoads => HotspotRoadsCount > 0;
    public bool HasCriticalRisk => RiskLevel >= 2;
}
