using System.Collections.Generic;
using System.Linq;

namespace WeatherApp.Models;

public class FloodHotspotRoad
{
    public string District { get; set; } = string.Empty;
    public string StreetName { get; set; } = string.Empty;
    public string EstimatedDepth { get; set; } = string.Empty;
    public string EstimatedDepthDisplay
    {
        get
        {
            bool isVi = Services.LocalizationService.Instance.IsVietnamese;
            if (EstimatedDepth.Contains("Khô ráo") || EstimatedDepth.Contains("An toàn") || EstimatedDepth.Contains("Dry") || EstimatedDepth.Contains("Safe"))
            {
                return isVi ? "Khô ráo / An toàn" : "Dry / Safe";
            }
            return EstimatedDepth;
        }
    }
    public string Cause { get; set; } = "Do Mưa Lớn"; // "Do Triều Cường", "Do Mưa Lớn", "Mưa + Triều Cường"
    public string CauseDisplay
    {
        get
        {
            bool isVi = Services.LocalizationService.Instance.IsVietnamese;
            if (isVi) return Cause;
            if (Cause.Contains("Mưa") && Cause.Contains("Triều")) return "Rain + Tide";
            if (Cause.Contains("Triều")) return "Tidal Surge";
            if (Cause.Contains("Mưa")) return "Heavy Rain";
            return Cause;
        }
    }
    public int SeverityLevel { get; set; } = 1; // 1 = Nhẹ, 2 = Trung bình, 3 = Nặng
    public string SeverityText
    {
        get
        {
            bool isVi = Services.LocalizationService.Instance.IsVietnamese;
            return SeverityLevel switch
            {
                3 => isVi ? "Ngập sâu (Nguy hiểm)" : "Deep flood (Dangerous)",
                2 => isVi ? "Ngập vừa (Xe máy khó đi)" : "Moderate flood (Difficult for bikes)",
                _ => isVi ? "Đọng nước cục bộ" : "Localized water logging"
            };
        }
    }
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
    public string IconGlyph => (Cause.Contains("Triều") || Cause.Contains("Tidal")) ? "\uf773" : "\uf73d";
    public string CauseIconGlyph => IconGlyph;
    public string CauseBadgeBg => (Cause.Contains("Triều") || Cause.Contains("Tidal")) && (Cause.Contains("Mưa") || Cause.Contains("Rain")) ? "#25EA580C" : (Cause.Contains("Triều") || Cause.Contains("Tidal")) ? "#200284C7" : "#2038BDF8";
    public string CauseBadgeFg => (Cause.Contains("Triều") || Cause.Contains("Tidal")) && (Cause.Contains("Mưa") || Cause.Contains("Rain")) ? "#EA580C" : (Cause.Contains("Triều") || Cause.Contains("Tidal")) ? "#0284C7" : "#38BDF8";
}

public class UrbanFloodWarning
{
    public bool IsRelevantCity { get; set; } = true;
    public string CityType { get; set; } = "General"; // "Hcm", "Hanoi", "General"
    public string CityDisplayName { get; set; } = "Khu vực đô thị";
    
    // Mức độ rủi ro: 0 = An toàn (Xanh), 1 = Nhẹ (Vàng), 2 = Cảnh báo cao (Cam), 3 = Báo động đỏ (Đỏ)
    public int RiskLevel { get; set; } = 0;
    
    public string RiskTitle
    {
        get
        {
            bool isVi = Services.LocalizationService.Instance.IsVietnamese;
            return RiskLevel switch
            {
                3 => isVi ? "BÁO ĐỘNG ĐỎ NGẬP SÂU" : "RED ALERT: DEEP FLOODING",
                2 => isVi ? "CẢNH BÁO NGẬP ÚNG CAO" : "HIGH FLOOD WARNING",
                1 => isVi ? "NGUY CƠ NGẬP NHẸ" : "LOW FLOOD RISK",
                _ => isVi ? "AN TOÀN - THÔNG THOÁNG" : "SAFE - CLEAR ROADS"
            };
        }
    }

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
    public string TideAlertBadge
    {
        get
        {
            bool isVi = Services.LocalizationService.Instance.IsVietnamese;
            return CurrentTideLevel switch
            {
                >= 1.65 => isVi ? "Báo động 3+ (Ngập nặng)" : "Alert 3+ (Severe Flood)",
                >= 1.55 => isVi ? "Báo động 2 (Ngập mép đường)" : "Alert 2 (Road Edge Flood)",
                >= 1.40 => isVi ? "Báo động 1 (Triều cao)" : "Alert 1 (High Tide)",
                _ => isVi ? "Dưới Báo Động (An toàn)" : "Below Alert (Safe)"
            };
        }
    }
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

    // v3.0 Beta: Dữ liệu đồ thị sóng triều bán nhật triều 24h & Đếm ngược đỉnh triều
    public List<TideHourlyPoint> TideCurve24h { get; set; } = new();
    public string NextPeakCountdown { get; set; } = string.Empty;
    public double PeakTideMeters { get; set; } = 1.60;

    // Helper UI Properties cho giao diện cao cấp
    public string DrainageIconGlyph => RainIntensity switch
    {
        >= 45.0 => "\uf071",
        >= 25.0 => "\uf73d",
        >= 10.0 => "\uf73d",
        _ => "\uf058"
    };

    public string TideTrendIconGlyph => IsCurrentlyPeakTide ? "\uf13d" : (TideStatusText.Contains("dâng") ? "\uf062" : "\uf063");
    public string TideTrendText
    {
        get
        {
            bool isVi = Services.LocalizationService.Instance.IsVietnamese;
            if (isVi)
            {
                return IsCurrentlyPeakTide ? "Đang đỉnh triều" : (TideStatusText.Contains("dâng") ? "Triều đang dâng" : "Triều đang rút");
            }
            else
            {
                return IsCurrentlyPeakTide ? "At peak tide" : (TideStatusText.Contains("dâng") ? "Tide rising" : "Tide receding");
            }
        }
    }
    public string TideTrendColor => IsCurrentlyPeakTide ? "#EF4444" : (TideStatusText.Contains("dâng") ? "#EA580C" : "#10B981");

    public int HotspotsCriticalCount => HotspotRoads?.Count(r => r.SeverityLevel == 3) ?? 0;
    public int HotspotsMediumCount => HotspotRoads?.Count(r => r.SeverityLevel == 2) ?? 0;
    public int HotspotsMildCount => HotspotRoads?.Count(r => r.SeverityLevel <= 1) ?? 0;

    public string CurrentTideMetersText => $"{CurrentTideLevel:F2}";
    public string HotspotRoadsCountText => HotspotRoadsCount.ToString();
    public string HotspotsCriticalCountText => HotspotsCriticalCount.ToString();
    public string HotspotsMediumCountText => HotspotsMediumCount.ToString();

    public string MotorbikeRiskStatus
    {
        get
        {
            bool isVi = Services.LocalizationService.Instance.IsVietnamese;
            return RiskLevel switch
            {
                3 => isVi ? "Nguy cơ chết máy cao" : "High engine stall risk",
                2 => isVi ? "Cần hết sức cẩn trọng" : "Exercise extreme caution",
                1 => isVi ? "Chú ý vũng trũng" : "Watch out for puddles",
                _ => isVi ? "Lưu thông an toàn" : "Safe to commute"
            };
        }
    }
    public string MotorbikeRiskColor => RiskLevel switch
    {
        3 => "#EF4444",
        2 => "#EA580C",
        1 => "#F59E0B",
        _ => "#10B981"
    };

    public string CarRiskStatus
    {
        get
        {
            bool isVi = Services.LocalizationService.Instance.IsVietnamese;
            return RiskLevel switch
            {
                3 => isVi ? "Nguy cơ thủy kích cao" : "High hydrolock risk",
                2 => isVi ? "Tránh các trục ngập sâu" : "Avoid deep flooded routes",
                1 => isVi ? "Giảm tốc tránh tạt nước" : "Slow down near water",
                _ => isVi ? "Lưu thông an toàn" : "Safe to commute"
            };
        }
    }
    public string CarRiskColor => RiskLevel switch
    {
        3 => "#EF4444",
        2 => "#EA580C",
        1 => "#F59E0B",
        _ => "#10B981"
    };

    public string HeroGradientStart => RiskLevel switch
    {
        3 => "#330F13",
        2 => "#301B08",
        1 => "#2B1E07",
        _ => "#092419"
    };
    public string HeroGradientEnd => RiskLevel switch
    {
        3 => "#1A0608",
        2 => "#180D04",
        1 => "#161005",
        _ => "#05130D"
    };
    public string HeroBorderColor => RiskLevel switch
    {
        3 => "#66EF4444",
        2 => "#66EA580C",
        1 => "#66F59E0B",
        _ => "#4410B981"
    };
}

public class TideHourlyPoint
{
    public int Hour { get; set; }
    public string TimeLabel => $"{Hour:D2}:00";
    public double LevelMeters { get; set; }
    public string LevelDisplay => $"{LevelMeters:F2} m";
    public bool IsCurrentHour { get; set; }
    public bool IsPeak { get; set; }
}

