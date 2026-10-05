using System.Net.Http.Json;
using System.Text.Json;
using Windows.Devices.Geolocation;
using WeatherApp.Models;

namespace WeatherApp.Services;

public class LocationService : ILocationService
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public LocationService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("WeatherAppWinUI/1.0");
        }
    }

    public async Task<GeoLocationInfo> GetCurrentLocationAsync()
    {
        // 1. Thử lấy vị trí từ Windows Geolocation API
        try
        {
            var accessStatus = await Geolocator.RequestAccessAsync();
            if (accessStatus == GeolocationAccessStatus.Allowed)
            {
                var geolocator = new Geolocator { DesiredAccuracy = PositionAccuracy.High };
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                var pos = await geolocator.GetGeopositionAsync().AsTask(cts.Token);
                // Chỉ tin cậy Windows Geolocation nếu độ chính xác cao (GPS phần cứng hoặc Wi-Fi triangulation <= 5000m)
                // Tránh lỗi Windows IP định tuyến nhầm toàn bộ máy tính bàn Việt Nam về Hà Nội
                if (pos?.Coordinate?.Point?.Position != null && pos.Coordinate.Accuracy <= 5000)
                {
                    double lat = pos.Coordinate.Point.Position.Latitude;
                    double lon = pos.Coordinate.Point.Position.Longitude;
                    string cityName = await ResolveCityNameAsync(lat, lon, "");
                    return new GeoLocationInfo(lat, lon, cityName, "Việt Nam", isAutoDetected: true, detectionSource: "Windows GPS");
                }
            }
        }
        catch
        {
            // Windows Geolocation không khả dụng hoặc bị tắt
        }

        // 2. Kênh định vị IP cấp 1: get.geojs.io (HTTPS, máy chủ Cloudflare Anycast tại VN cực nhanh, nhận diện HCM siêu chuẩn)
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var geojs = await _httpClient.GetFromJsonAsync<GeoJsResponse>("https://get.geojs.io/v1/ip/geo.json", JsonOptions, cts.Token);
            if (geojs != null && 
                double.TryParse(geojs.Latitude, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
                double.TryParse(geojs.Longitude, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double lon))
            {
                string cityName = await ResolveCityNameAsync(lat, lon, geojs.City);
                return new GeoLocationInfo(lat, lon, cityName, "Việt Nam", isAutoDetected: true, detectionSource: "GeoJS IP");
            }
        }
        catch { }

        // 3. Kênh định vị IP cấp 2: ipwho.is (HTTPS, dữ liệu vị trí ISP chi tiết)
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
            var ipwho = await _httpClient.GetFromJsonAsync<IpWhoIsResponse>("https://ipwho.is/", JsonOptions, cts.Token);
            if (ipwho != null && ipwho.Success && ipwho.Latitude.HasValue && ipwho.Longitude.HasValue)
            {
                double lat = ipwho.Latitude.Value;
                double lon = ipwho.Longitude.Value;
                string cityName = await ResolveCityNameAsync(lat, lon, ipwho.City);
                return new GeoLocationInfo(lat, lon, cityName, "Việt Nam", isAutoDetected: true, detectionSource: "ipwho.is IP");
            }
        }
        catch { }

        // 4. Kênh định vị IP cấp 3: freeipapi.com (HTTPS)
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
            var freeip = await _httpClient.GetFromJsonAsync<FreeIpApiResponse>("https://freeipapi.com/api/json", JsonOptions, cts.Token);
            if (freeip != null && freeip.Latitude.HasValue && freeip.Longitude.HasValue)
            {
                double lat = freeip.Latitude.Value;
                double lon = freeip.Longitude.Value;
                string cityName = await ResolveCityNameAsync(lat, lon, freeip.CityName);
                return new GeoLocationInfo(lat, lon, cityName, "Việt Nam", isAutoDetected: true, detectionSource: "FreeIP IP");
            }
        }
        catch { }

        // 5. Dự phòng an toàn khi hoàn toàn mất mạng (đánh dấu IsAutoDetected = false để UI không báo thành công giả lập)
        return new GeoLocationInfo(10.823, 106.6296, "TP. Hồ Chí Minh", "Việt Nam", isAutoDetected: false, detectionSource: "Default Fallback");
    }

    private async Task<string> ResolveCityNameAsync(double lat, double lon, string? hintCity)
    {
        // 1. Nhận diện các vùng đô thị lớn tại Việt Nam dựa trên toạ độ địa lý chính xác
        if ((lat >= 10.2 && lat <= 11.2 && lon >= 106.2 && lon <= 107.1) ||
            (!string.IsNullOrEmpty(hintCity) && (hintCity.Contains("Ho Chi Minh", StringComparison.OrdinalIgnoreCase) || hintCity.Contains("Saigon", StringComparison.OrdinalIgnoreCase))))
        {
            return "TP. Hồ Chí Minh";
        }
        if ((lat >= 20.7 && lat <= 21.4 && lon >= 105.4 && lon <= 106.2) ||
            (!string.IsNullOrEmpty(hintCity) && hintCity.Contains("Hanoi", StringComparison.OrdinalIgnoreCase)))
        {
            return "Hà Nội";
        }
        if ((lat >= 15.8 && lat <= 16.3 && lon >= 108.0 && lon <= 108.5) ||
            (!string.IsNullOrEmpty(hintCity) && hintCity.Contains("Da Nang", StringComparison.OrdinalIgnoreCase)))
        {
            return "Đà Nẵng";
        }
        if (lat >= 11.8 && lat <= 12.1 && lon >= 108.3 && lon <= 108.6)
        {
            return "Đà Lạt";
        }
        if (lat >= 12.1 && lat <= 12.4 && lon >= 109.1 && lon <= 109.3)
        {
            return "Nha Trang";
        }
        if (lat >= 9.9 && lat <= 10.2 && lon >= 105.6 && lon <= 105.9)
        {
            return "Cần Thơ";
        }
        if (lat >= 20.7 && lat <= 21.0 && lon >= 106.5 && lon <= 106.9)
        {
            return "Hải Phòng";
        }

        // 2. Tra cứu Reverse Geocoding chi tiết
        string revName = await GetReverseGeocodedCityNameAsync(lat, lon);
        if (!string.IsNullOrWhiteSpace(revName))
        {
            return revName;
        }

        return !string.IsNullOrWhiteSpace(hintCity) ? hintCity : "Vị trí của bạn";
    }

    #region Vietnamese Built-in Geocoding Database
    private static readonly List<GeocodingItem> VietnamLocations = new()
    {
        // Các thành phố trực thuộc trung ương
        new GeocodingItem { Id = 1001, Name = "Hà Nội", Latitude = 21.0285, Longitude = 105.8542, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Thủ đô Hà Nội" },
        new GeocodingItem { Id = 1002, Name = "TP. Hồ Chí Minh", Latitude = 10.8231, Longitude = 106.6297, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Đông Nam Bộ" },
        new GeocodingItem { Id = 1003, Name = "Đà Nẵng", Latitude = 16.0544, Longitude = 108.2022, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Nam Trung Bộ" },
        new GeocodingItem { Id = 1004, Name = "Hải Phòng", Latitude = 20.8449, Longitude = 106.6881, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Đồng bằng sông Hồng" },
        new GeocodingItem { Id = 1005, Name = "Cần Thơ", Latitude = 10.0452, Longitude = 105.7469, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Đồng bằng sông Cửu Long" },

        // Tỉnh Kiên Giang & các thành phố/địa danh nổi tiếng
        new GeocodingItem { Id = 1006, Name = "Kiên Giang", Latitude = 10.0163, Longitude = 105.0809, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Đồng bằng sông Cửu Long" },
        new GeocodingItem { Id = 1007, Name = "Rạch Giá", Latitude = 10.0125, Longitude = 105.0809, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Kiên Giang" },
        new GeocodingItem { Id = 1008, Name = "Phú Quốc", Latitude = 10.2289, Longitude = 103.9572, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Kiên Giang" },
        new GeocodingItem { Id = 1009, Name = "Hà Tiên", Latitude = 10.3833, Longitude = 104.4833, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Kiên Giang" },

        // Các tỉnh & thành phố du lịch, công nghiệp trọng điểm
        new GeocodingItem { Id = 1010, Name = "Nha Trang", Latitude = 12.2388, Longitude = 109.1967, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Khánh Hòa" },
        new GeocodingItem { Id = 1011, Name = "Đà Lạt", Latitude = 11.9404, Longitude = 108.4583, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Lâm Đồng" },
        new GeocodingItem { Id = 1012, Name = "Vũng Tàu", Latitude = 10.3460, Longitude = 107.0843, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Bà Rịa - Vũng Tàu" },
        new GeocodingItem { Id = 1013, Name = "Bà Rịa", Latitude = 10.4967, Longitude = 107.1683, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Bà Rịa - Vũng Tàu" },
        new GeocodingItem { Id = 1014, Name = "Côn Đảo", Latitude = 8.6835, Longitude = 106.6074, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Bà Rịa - Vũng Tàu" },
        new GeocodingItem { Id = 1015, Name = "Hạ Long", Latitude = 20.9505, Longitude = 107.0734, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Quảng Ninh" },
        new GeocodingItem { Id = 1016, Name = "Cẩm Phả", Latitude = 21.0167, Longitude = 107.2833, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Quảng Ninh" },
        new GeocodingItem { Id = 1017, Name = "Móng Cái", Latitude = 21.5278, Longitude = 107.9694, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Quảng Ninh" },
        new GeocodingItem { Id = 1018, Name = "Huế", Latitude = 16.4637, Longitude = 107.5909, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Thừa Thiên Huế" },
        new GeocodingItem { Id = 1019, Name = "Hội An", Latitude = 15.8801, Longitude = 108.3380, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Quảng Nam" },
        new GeocodingItem { Id = 1020, Name = "Tam Kỳ", Latitude = 15.5739, Longitude = 108.4744, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Quảng Nam" },
        new GeocodingItem { Id = 1021, Name = "Sa Pa", Latitude = 22.3364, Longitude = 103.8438, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Lào Cai" },
        new GeocodingItem { Id = 1022, Name = "Lào Cai", Latitude = 22.4856, Longitude = 103.9708, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Lào Cai" },
        new GeocodingItem { Id = 1023, Name = "Quy Nhơn", Latitude = 13.7830, Longitude = 109.2197, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Bình Định" },
        new GeocodingItem { Id = 1024, Name = "Phan Thiết", Latitude = 10.9289, Longitude = 108.1021, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Bình Thuận" },
        new GeocodingItem { Id = 1025, Name = "Buôn Ma Thuột", Latitude = 12.6667, Longitude = 108.0500, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Đắk Lắk" },
        new GeocodingItem { Id = 1026, Name = "Pleiku", Latitude = 13.9833, Longitude = 108.0000, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Gia Lai" },
        new GeocodingItem { Id = 1027, Name = "Thủ Dầu Một", Latitude = 10.9804, Longitude = 106.6519, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Bình Dương" },
        new GeocodingItem { Id = 1028, Name = "Dĩ An", Latitude = 10.9067, Longitude = 106.7725, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Bình Dương" },
        new GeocodingItem { Id = 1029, Name = "Biên Hòa", Latitude = 10.9460, Longitude = 106.8239, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Đồng Nai" },
        new GeocodingItem { Id = 1030, Name = "Long Xuyên", Latitude = 10.3833, Longitude = 105.4167, Country = "Việt Nam", CountryCode = "VN", Admin1 = "An Giang" },
        new GeocodingItem { Id = 1031, Name = "Châu Đốc", Latitude = 10.7000, Longitude = 105.1167, Country = "Việt Nam", CountryCode = "VN", Admin1 = "An Giang" },
        new GeocodingItem { Id = 1032, Name = "Mỹ Tho", Latitude = 10.3600, Longitude = 106.3600, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Tiền Giang" },
        new GeocodingItem { Id = 1033, Name = "Cà Mau", Latitude = 9.1769, Longitude = 105.1524, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Cà Mau" },
        new GeocodingItem { Id = 1034, Name = "Bạc Liêu", Latitude = 9.2941, Longitude = 105.7278, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Bạc Liêu" },
        new GeocodingItem { Id = 1035, Name = "Sóc Trăng", Latitude = 9.6033, Longitude = 105.9800, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Sóc Trăng" },
        new GeocodingItem { Id = 1036, Name = "Trà Vinh", Latitude = 9.9347, Longitude = 106.3456, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Trà Vinh" },
        new GeocodingItem { Id = 1037, Name = "Bến Tre", Latitude = 10.2415, Longitude = 106.3759, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Bến Tre" },
        new GeocodingItem { Id = 1038, Name = "Vĩnh Long", Latitude = 10.2536, Longitude = 105.9722, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Vĩnh Long" },
        new GeocodingItem { Id = 1039, Name = "Cao Lãnh", Latitude = 10.4597, Longitude = 105.6325, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Đồng Tháp" },
        new GeocodingItem { Id = 1040, Name = "Sa Đéc", Latitude = 10.2944, Longitude = 105.7597, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Đồng Tháp" },
        new GeocodingItem { Id = 1041, Name = "Tân An", Latitude = 10.5361, Longitude = 106.4131, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Long An" },
        new GeocodingItem { Id = 1042, Name = "Tây Ninh", Latitude = 11.3100, Longitude = 106.0983, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Tây Ninh" },
        new GeocodingItem { Id = 1043, Name = "Đồng Xoài", Latitude = 11.5333, Longitude = 106.8833, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Bình Phước" },
        new GeocodingItem { Id = 1044, Name = "Gia Nghĩa", Latitude = 12.0000, Longitude = 107.6833, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Đắk Nông" },
        new GeocodingItem { Id = 1045, Name = "Kon Tum", Latitude = 14.3500, Longitude = 108.0000, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Kon Tum" },
        new GeocodingItem { Id = 1046, Name = "Tuy Hòa", Latitude = 13.0883, Longitude = 109.3083, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Phú Yên" },
        new GeocodingItem { Id = 1047, Name = "Phan Rang - Tháp Chàm", Latitude = 11.5667, Longitude = 108.9833, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Ninh Thuận" },
        new GeocodingItem { Id = 1048, Name = "Quảng Ngãi", Latitude = 15.1206, Longitude = 108.7922, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Quảng Ngãi" },
        new GeocodingItem { Id = 1049, Name = "Đông Hà", Latitude = 16.8167, Longitude = 107.1000, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Quảng Trị" },
        new GeocodingItem { Id = 1050, Name = "Đồng Hới", Latitude = 17.4761, Longitude = 106.6008, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Quảng Bình" },
        new GeocodingItem { Id = 1051, Name = "Hà Tĩnh", Latitude = 18.3428, Longitude = 105.9058, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Hà Tĩnh" },
        new GeocodingItem { Id = 1052, Name = "Vinh", Latitude = 18.6733, Longitude = 105.6819, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Nghệ An" },
        new GeocodingItem { Id = 1053, Name = "Thanh Hóa", Latitude = 19.8075, Longitude = 105.7764, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Thanh Hóa" },
        new GeocodingItem { Id = 1054, Name = "Sầm Sơn", Latitude = 19.7436, Longitude = 105.9050, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Thanh Hóa" },
        new GeocodingItem { Id = 1055, Name = "Ninh Bình", Latitude = 20.2506, Longitude = 105.9744, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Ninh Bình" },
        new GeocodingItem { Id = 1056, Name = "Nam Định", Latitude = 20.4344, Longitude = 106.1683, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Nam Định" },
        new GeocodingItem { Id = 1057, Name = "Thái Bình", Latitude = 20.4464, Longitude = 106.3364, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Thái Bình" },
        new GeocodingItem { Id = 1058, Name = "Hải Dương", Latitude = 20.9372, Longitude = 106.3150, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Hải Dương" },
        new GeocodingItem { Id = 1059, Name = "Hưng Yên", Latitude = 20.6464, Longitude = 106.0511, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Hưng Yên" },
        new GeocodingItem { Id = 1060, Name = "Phủ Lý", Latitude = 20.5411, Longitude = 105.9139, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Hà Nam" },
        new GeocodingItem { Id = 1061, Name = "Bắc Ninh", Latitude = 21.1861, Longitude = 106.0763, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Bắc Ninh" },
        new GeocodingItem { Id = 1062, Name = "Bắc Giang", Latitude = 21.2731, Longitude = 106.1946, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Bắc Giang" },
        new GeocodingItem { Id = 1063, Name = "Vĩnh Yên", Latitude = 21.3089, Longitude = 105.6044, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Vĩnh Phúc" },
        new GeocodingItem { Id = 1064, Name = "Tam Đảo", Latitude = 21.4589, Longitude = 105.6450, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Vĩnh Phúc" },
        new GeocodingItem { Id = 1065, Name = "Việt Trì", Latitude = 21.3228, Longitude = 105.4019, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Phú Thọ" },
        new GeocodingItem { Id = 1066, Name = "Hòa Bình", Latitude = 20.8172, Longitude = 105.3375, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Hòa Bình" },
        new GeocodingItem { Id = 1067, Name = "Sơn La", Latitude = 21.3283, Longitude = 103.9189, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Sơn La" },
        new GeocodingItem { Id = 1068, Name = "Mộc Châu", Latitude = 20.8433, Longitude = 104.6533, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Sơn La" },
        new GeocodingItem { Id = 1069, Name = "Điện Biên Phủ", Latitude = 21.3833, Longitude = 103.0167, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Điện Biên" },
        new GeocodingItem { Id = 1070, Name = "Lai Châu", Latitude = 22.3967, Longitude = 103.4719, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Lai Châu" },
        new GeocodingItem { Id = 1071, Name = "Yên Bái", Latitude = 21.7228, Longitude = 104.9114, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Yên Bái" },
        new GeocodingItem { Id = 1072, Name = "Tuyên Quang", Latitude = 21.8233, Longitude = 105.2147, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Tuyên Quang" },
        new GeocodingItem { Id = 1073, Name = "Hà Giang", Latitude = 22.8233, Longitude = 104.9839, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Hà Giang" },
        new GeocodingItem { Id = 1074, Name = "Cao Bằng", Latitude = 22.6667, Longitude = 106.2500, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Cao Bằng" },
        new GeocodingItem { Id = 1075, Name = "Bắc Kạn", Latitude = 22.1470, Longitude = 105.8348, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Bắc Kạn" },
        new GeocodingItem { Id = 1076, Name = "Thái Nguyên", Latitude = 21.5942, Longitude = 105.8481, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Thái Nguyên" },
        new GeocodingItem { Id = 1077, Name = "Lạng Sơn", Latitude = 21.8533, Longitude = 106.7619, Country = "Việt Nam", CountryCode = "VN", Admin1 = "Lạng Sơn" }
    };

    private static string NormalizeVietnamese(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        string normalized = text.Trim().ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (char c in normalized)
        {
            var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                if (c == 'đ') sb.Append('d');
                else if (c == 'Đ') sb.Append('D');
                else sb.Append(c);
            }
        }
        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC).Replace(" ", "");
    }
    #endregion

    public async Task<List<GeocodingItem>> SearchLocationsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            return new List<GeocodingItem>();
        }

        string cleanQuery = query.Trim();
        string normQuery = NormalizeVietnamese(cleanQuery);
        var combined = new List<GeocodingItem>();
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. Tìm kiếm trong danh mục địa phương Việt Nam trước (tốc độ tức thì, chính xác 100%)
        foreach (var loc in VietnamLocations)
        {
            bool match = false;
            if (loc.Name.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase) ||
                (loc.Admin1 != null && loc.Admin1.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase)))
            {
                match = true;
            }
            else
            {
                string normName = NormalizeVietnamese(loc.Name);
                string normAdmin = loc.Admin1 != null ? NormalizeVietnamese(loc.Admin1) : string.Empty;
                if (normName.Contains(normQuery) || normAdmin.Contains(normQuery))
                {
                    match = true;
                }
                // Hỗ trợ viết tắt phổ biến: hcm, tphcm, saigon, hn, kieng, kien giang...
                else if (normQuery == "hcm" || normQuery == "tphcm" || normQuery == "saigon")
                {
                    if (loc.Name.Contains("Hồ Chí Minh", StringComparison.OrdinalIgnoreCase)) match = true;
                }
                else if (normQuery == "hn" || normQuery == "hanoi")
                {
                    if (loc.Name.Contains("Hà Nội", StringComparison.OrdinalIgnoreCase)) match = true;
                }
            }

            if (match)
            {
                string key = $"{loc.Name}_{loc.Admin1}";
                if (seenNames.Add(key))
                {
                    combined.Add(loc);
                    if (combined.Count >= 6) break;
                }
            }
        }

        // 2. Gọi Open-Meteo Geocoding API đồng thời cho các địa điểm toàn cầu & chi tiết
        try
        {
            string url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(cleanQuery)}&count=6&language=vi&format=json";
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3.5));
            var response = await _httpClient.GetFromJsonAsync<OpenMeteoGeocodingResponse>(url, JsonOptions, cts.Token);
            if (response?.Results != null)
            {
                foreach (var item in response.Results)
                {
                    string key = $"{item.Name}_{item.Admin1}";
                    if (seenNames.Add(key))
                    {
                        combined.Add(item);
                        if (combined.Count >= 10) break;
                    }
                }
            }
        }
        catch
        {
            // Bỏ qua lỗi mạng từ API từ xa nếu offline hoặc mạng chậm, vẫn có kết quả địa phương
        }

        return combined;
    }

    public async Task<string> GetReverseGeocodedCityNameAsync(double latitude, double longitude)
    {
        try
        {
            string url = $"https://api.bigdatacloud.net/data/reverse-geocode-client?latitude={latitude}&longitude={longitude}&localityLanguage=vi";
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
            var result = await _httpClient.GetFromJsonAsync<ReverseGeocodeResponse>(url, JsonOptions, cts.Token);
            if (result != null)
            {
                string city = !string.IsNullOrWhiteSpace(result.City) 
                    ? result.City 
                    : (!string.IsNullOrWhiteSpace(result.PrincipalSubdivision) 
                        ? result.PrincipalSubdivision 
                        : result.Locality ?? string.Empty);

                if (!string.IsNullOrWhiteSpace(city))
                {
                    return !string.IsNullOrWhiteSpace(result.CountryName) 
                        ? $"{city}, {result.CountryName}" 
                        : city;
                }
            }
        }
        catch
        {
            // Bỏ qua lỗi reverse geocode
        }

        return string.Empty;
    }
}
