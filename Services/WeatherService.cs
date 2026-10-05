using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using WeatherApp.Helpers;
using WeatherApp.Models;

namespace WeatherApp.Services;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public WeatherService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("WeatherAppWinUI/1.0");
        }
    }

    public async Task<OpenMeteoResponse?> GetWeatherDataAsync(double latitude, double longitude)
    {
        try
        {
            string latStr = latitude.ToString("F4", CultureInfo.InvariantCulture);
            string lonStr = longitude.ToString("F4", CultureInfo.InvariantCulture);

            string url = $"https://api.open-meteo.com/v1/forecast?latitude={latStr}&longitude={lonStr}" +
                         "&current=temperature_2m,relative_humidity_2m,apparent_temperature,is_day,precipitation,rain,showers,snowfall,weather_code,cloud_cover,surface_pressure,wind_speed_10m,wind_direction_10m,wind_gusts_10m" +
                         "&hourly=temperature_2m,relative_humidity_2m,precipitation_probability,precipitation,weather_code,wind_speed_10m" +
                         "&daily=weather_code,temperature_2m_max,temperature_2m_min,apparent_temperature_max,apparent_temperature_min,sunrise,sunset,uv_index_max,precipitation_sum,precipitation_probability_max" +
                         "&timezone=auto";

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var response = await _httpClient.GetFromJsonAsync<OpenMeteoResponse>(url, JsonOptions, cts.Token);
            return response;
        }
        catch
        {
            return null;
        }
    }

    public async Task<AirQualityData> GetAirQualityAsync(double latitude, double longitude)
    {
        var result = new AirQualityData();
        try
        {
            string latStr = latitude.ToString("F4", CultureInfo.InvariantCulture);
            string lonStr = longitude.ToString("F4", CultureInfo.InvariantCulture);
            string url = $"https://air-quality-api.open-meteo.com/v1/air-quality?latitude={latStr}&longitude={lonStr}&current=us_aqi,pm2_5,pm10,carbon_monoxide,nitrogen_dioxide,sulphur_dioxide,ozone";

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var resp = await _httpClient.GetStringAsync(url, cts.Token);
            using var doc = JsonDocument.Parse(resp);
            if (doc.RootElement.TryGetProperty("current", out var cur))
            {
                result.Aqi = cur.TryGetProperty("us_aqi", out var a) ? a.GetInt32() : 0;
                result.Pm25 = cur.TryGetProperty("pm2_5", out var p) ? p.GetDouble() : 0;
                result.Pm10 = cur.TryGetProperty("pm10", out var p10) ? p10.GetDouble() : 0;
                result.Ozone = cur.TryGetProperty("ozone", out var oz) ? oz.GetDouble() : 0;
                result.No2 = cur.TryGetProperty("nitrogen_dioxide", out var n2) ? n2.GetDouble() : 0;
                result.So2 = cur.TryGetProperty("sulphur_dioxide", out var s2) ? s2.GetDouble() : 0;
                result.Co = cur.TryGetProperty("carbon_monoxide", out var co) ? co.GetDouble() : 0;
            }
        }
        catch { }
        return result;
    }

    public CurrentWeatherDisplay CreateCurrentWeatherDisplay(OpenMeteoResponse data, string locationName, AppSettings? settings = null, AirQualityData? airQuality = null)
    {
        settings ??= new AppSettings();
        bool isFahrenheit = settings.TemperatureUnit == "F";

        var display = new CurrentWeatherDisplay
        {
            LocationName = string.IsNullOrWhiteSpace(locationName) ? "Vị trí hiện tại" : locationName,
            UpdatedTimeText = $"Cập nhật lúc {DateTime.Now:HH:mm}"
        };

        if (airQuality != null && airQuality.Aqi > 0)
        {
            display.AqiValue = airQuality.Aqi;
            var (aqiDesc, aqiColor, aqiAdvice) = WeatherCodeHelper.GetAqiInterpretation(airQuality.Aqi);
            display.AqiDescription = aqiDesc;
            display.AqiColor = aqiColor;
            display.AqiAdvice = aqiAdvice;
            display.AqiProgressPercent = Math.Clamp((double)airQuality.Aqi / 300.0, 0.05, 1.0);
            display.Pm25Text = $"{airQuality.Pm25:F1} µg/m³";
            display.Pm25Status = WeatherCodeHelper.GetPm25Status(airQuality.Pm25);
            display.Pm10Text = $"{airQuality.Pm10:F1} µg/m³";
            display.Pm10Status = WeatherCodeHelper.GetPm10Status(airQuality.Pm10);
            display.OzoneText = $"{airQuality.Ozone:F0} µg/m³";
            display.OzoneStatus = WeatherCodeHelper.GetOzoneStatus(airQuality.Ozone);
            display.No2Text = $"{airQuality.No2:F1} µg/m³";
            display.No2Status = WeatherCodeHelper.GetNo2Status(airQuality.No2);
            display.So2Text = $"{airQuality.So2:F1} µg/m³";
            display.CoText = $"{airQuality.Co:F0} µg/m³";
        }

        if (data.Current == null) return display;

        var cur = data.Current;
        bool isDay = cur.IsDay == 1;
        display.IsDay = isDay;
        display.TemperatureValue = cur.Temperature;

        double temp = isFahrenheit ? ToFahrenheit(cur.Temperature) : cur.Temperature;
        double feelsLike = isFahrenheit ? ToFahrenheit(cur.ApparentTemperature) : cur.ApparentTemperature;
        string unit = isFahrenheit ? "°F" : "°C";

        display.TemperatureText = $"{Math.Round(temp)}{unit}";
        display.FeelsLikeText = $"Cảm giác như {Math.Round(feelsLike)}{unit}";

        var (desc, glyph) = WeatherCodeHelper.GetConditionInfo(cur.WeatherCode, isDay);
        display.ConditionText = desc;
        display.IconGlyph = glyph;

        double uvIndex = 0;
        if (data.Daily?.UvIndexMax?.Count > 0)
        {
            uvIndex = data.Daily.UvIndexMax[0];
            var (level, descUv) = WeatherCodeHelper.GetUvInterpretation(uvIndex);
            display.UvIndexText = $"{uvIndex:F1}";
            display.UvIndexDescription = level;
        }

        // Xác định hiệu ứng thời tiết động chính xác 100%
        var effect = WeatherCodeHelper.GetWeatherEffect(cur.WeatherCode, isDay, uvIndex);
        display.WeatherEffect = effect;

        var (startColor, endColor) = WeatherCodeHelper.GetHeroGradientsByEffect(effect);
        display.HeroGradientStart = startColor;
        display.HeroGradientEnd = endColor;

        var (alertText, alertColor) = WeatherCodeHelper.GetWeatherAlert(effect, cur.Temperature, uvIndex);
        display.WeatherAlertBadgeText = alertText;
        display.WeatherAlertBadgeColor = alertColor;

        // Lấy icon tương ứng theo bộ biểu tượng người dùng đã chọn (Meteocons / Fluent3D / FontAwesome)
        string iconPack = settings.SelectedIconPack ?? "Meteocons";
        display.SvgIconPath = WeatherCodeHelper.GetWeatherIconPath(cur.WeatherCode, isDay, iconPack);
        string fullPath = WeatherCodeHelper.GetWeatherIconFullPath(cur.WeatherCode, isDay, iconPack);
        if (!string.IsNullOrEmpty(fullPath))
        {
            display.SvgIconFullPath = fullPath;
            display.SvgIconUri = new Uri(fullPath);
            try
            {
                display.SvgContent = File.ReadAllText(fullPath);
            }
            catch { }
        }

        display.HumidityText = $"{Math.Round(cur.RelativeHumidity)}%";

        // Tốc độ gió theo đơn vị đã cài đặt
        display.WindText = settings.WindSpeedUnit switch
        {
            "ms" => $"{Math.Round(cur.WindSpeed / 3.6, 1)} m/s",
            "mph" => $"{Math.Round(cur.WindSpeed * 0.621371, 1)} mph",
            _ => $"{Math.Round(cur.WindSpeed)} km/h"
        };
        display.WindDirectionText = WeatherCodeHelper.GetWindDirection(cur.WindDirection);

        // Áp suất theo đơn vị đã cài đặt
        display.PressureText = settings.PressureUnit switch
        {
            "mmHg" => $"{Math.Round(cur.SurfacePressure * 0.750062)} mmHg",
            _ => $"{Math.Round(cur.SurfacePressure)} hPa"
        };

        // Lượng mưa theo đơn vị đã cài đặt
        display.PrecipitationText = settings.PrecipitationUnit switch
        {
            "inch" => $"{cur.Precipitation * 0.0393701:F2} in",
            _ => $"{cur.Precipitation:F1} mm"
        };

        if (data.Daily != null)
        {
            if (data.Daily.TemperatureMax?.Count > 0 && data.Daily.TemperatureMin?.Count > 0)
            {
                double max = isFahrenheit ? ToFahrenheit(data.Daily.TemperatureMax[0]) : data.Daily.TemperatureMax[0];
                double min = isFahrenheit ? ToFahrenheit(data.Daily.TemperatureMin[0]) : data.Daily.TemperatureMin[0];
                display.MinMaxText = $"Thấp nhất: {Math.Round(min)}{unit}  •  Cao nhất: {Math.Round(max)}{unit}";
            }

            if (data.Daily.PrecipitationProbabilityMax?.Count > 0)
            {
                display.RainProbabilityText = $"{data.Daily.PrecipitationProbabilityMax[0]}%";
            }

            if (data.Daily.Sunrise?.Count > 0)
            {
                display.SunriseText = FormatTimeOnly(data.Daily.Sunrise[0]);
            }

            if (data.Daily.Sunset?.Count > 0)
            {
                display.SunsetText = FormatTimeOnly(data.Daily.Sunset[0]);
            }

            // Tính toán Vòng Cung Mặt Trời & Mặt Trăng (Sun & Moon Arc Tracker)
            try
            {
                if (data.Daily.Sunrise?.Count > 0 && data.Daily.Sunset?.Count > 0 &&
                    DateTime.TryParse(data.Daily.Sunrise[0], out DateTime rise) &&
                    DateTime.TryParse(data.Daily.Sunset[0], out DateTime set))
                {
                    DateTime now = DateTime.Now;
                    if (now >= rise && now <= set)
                    {
                        display.IsSunVisible = true;
                        double totalMins = Math.Max(1.0, (set - rise).TotalMinutes);
                        double elapsedMins = Math.Max(0.0, (now - rise).TotalMinutes);
                        display.SunProgressPercent = Math.Clamp(elapsedMins / totalMins, 0.0, 1.0);
                        var remaining = set - now;
                        display.SunStatusText = remaining.TotalHours >= 1
                            ? $"Còn {(int)remaining.TotalHours}h {remaining.Minutes}m nữa đến hoàng hôn"
                            : $"Còn {remaining.Minutes} phút nữa đến hoàng hôn";
                    }
                    else
                    {
                        display.IsSunVisible = false;
                        display.SunProgressPercent = now < rise ? 0.0 : 1.0;
                        display.SunStatusText = $"Đêm quang mây • Bình minh lúc {rise:HH:mm}";
                    }
                }
            }
            catch { }
        }

        // Tính toán Lịch Âm & 24 Tiết Khí truyền thống Việt Nam
        try
        {
            var lunar = VietnameseLunarHelper.ConvertSolarToLunar(DateTime.Now);
            display.LunarDateText = lunar.FullDisplay;
            display.SolarTermText = $"Tiết {lunar.SolarTerm}";
        }
        catch { }

        // Câu tóm tắt thông minh đầu ngày (AI Weather Glance Summary)
        try
        {
            string userName = string.IsNullOrWhiteSpace(settings.UserName) ? "bạn" : settings.UserName;
            string greeting = DateTime.Now.Hour switch
            {
                < 12 => $"Chào buổi sáng, {userName}!",
                < 18 => $"Chào buổi chiều, {userName}!",
                _ => $"Buổi tối an lành, {userName}!"
            };
            display.SmartSummaryText = $"{greeting} Hiện tại {display.TemperatureText.ToLowerInvariant()}, {display.ConditionText.ToLowerInvariant()}. {display.MinMaxText.ToLowerInvariant()}.";
        }
        catch { }

        return display;
    }

    public List<HourlyForecastItem> CreateHourlyForecast(OpenMeteoResponse data, AppSettings? settings = null)
    {
        settings ??= new AppSettings();
        bool isFahrenheit = settings.TemperatureUnit == "F";

        var list = new List<HourlyForecastItem>();
        if (data.Hourly?.Time == null || data.Hourly.Temperature == null) return list;

        var times = data.Hourly.Time;
        var temps = data.Hourly.Temperature;
        var codes = data.Hourly.WeatherCode;
        var rainProbs = data.Hourly.PrecipitationProbability;

        DateTime now = DateTime.Now;
        int startIndex = 0;
        for (int i = 0; i < times.Count; i++)
        {
            if (DateTime.TryParse(times[i], out DateTime dt) && dt >= now.AddMinutes(-30))
            {
                startIndex = i;
                break;
            }
        }

        string unit = isFahrenheit ? "°" : "°";
        int count = Math.Min(24, times.Count - startIndex);

        for (int i = 0; i < count; i++)
        {
            int idx = startIndex + i;
            string timeStr = times[idx];
            string timeDisplay = "Bây giờ";
            bool isDayHour = true;

            if (DateTime.TryParse(timeStr, out DateTime dt))
            {
                timeDisplay = i == 0 ? "Bây giờ" : dt.ToString(settings.Is24HourFormat ? "HH:mm" : "hh:mm tt");
                isDayHour = dt.Hour >= 6 && dt.Hour < 18;
            }

            double rawTemp = (idx < temps.Count) ? temps[idx] : 0;
            double t = isFahrenheit ? ToFahrenheit(rawTemp) : rawTemp;

            int code = (codes != null && idx < codes.Count) ? codes[idx] : 0;
            int rainProb = (rainProbs != null && idx < rainProbs.Count) ? rainProbs[idx] : 0;

            var (desc, glyph) = WeatherCodeHelper.GetConditionInfo(code, isDayHour);
            string iconPack = settings?.SelectedIconPack ?? "Meteocons";
            string svgPath = WeatherCodeHelper.GetWeatherIconPath(code, isDayHour, iconPack);

            list.Add(new HourlyForecastItem
            {
                TimeDisplay = timeDisplay,
                TempDisplay = $"{Math.Round(t)}{unit}",
                TempValue = Math.Round(t, 1),
                IconGlyph = glyph,
                SvgIconPath = svgPath,
                ConditionText = desc,
                RainProbabilityText = rainProb > 0 ? $"{rainProb}%" : "0%"
            });
        }

        return list;
    }

    public List<DailyForecastItem> CreateDailyForecast(OpenMeteoResponse data, AppSettings? settings = null)
    {
        settings ??= new AppSettings();
        bool isFahrenheit = settings.TemperatureUnit == "F";

        var list = new List<DailyForecastItem>();
        if (data.Daily?.Time == null || data.Daily.TemperatureMax == null || data.Daily.TemperatureMin == null)
            return list;

        var times = data.Daily.Time;
        var maxs = data.Daily.TemperatureMax;
        var mins = data.Daily.TemperatureMin;
        var codes = data.Daily.WeatherCode;
        var rainProbs = data.Daily.PrecipitationProbabilityMax;

        string unit = isFahrenheit ? "°" : "°";
        int count = Math.Min(7, times.Count);

        // Tính nhiệt độ thấp nhất và cao nhất của cả tuần để định vị thanh range bar
        double weekMin = double.MaxValue;
        double weekMax = double.MinValue;
        var dayMins = new double[count];
        var dayMaxs = new double[count];

        for (int i = 0; i < count; i++)
        {
            double maxRaw = i < maxs.Count ? maxs[i] : 0;
            double minRaw = i < mins.Count ? mins[i] : 0;
            dayMaxs[i] = isFahrenheit ? ToFahrenheit(maxRaw) : maxRaw;
            dayMins[i] = isFahrenheit ? ToFahrenheit(minRaw) : minRaw;

            if (dayMins[i] < weekMin) weekMin = dayMins[i];
            if (dayMaxs[i] > weekMax) weekMax = dayMaxs[i];
        }

        double tempRange = Math.Max(1.0, weekMax - weekMin);
        const double totalBarWidth = 96.0; // Chiều rộng chuẩn của thanh bar trực quan

        for (int i = 0; i < count; i++)
        {
            string dayName;
            string dateDisplay = "";

            if (DateTime.TryParse(times[i], out DateTime dt))
            {
                dateDisplay = dt.ToString("dd/MM");
                if (i == 0)
                {
                    dayName = "Hôm nay";
                }
                else if (i == 1)
                {
                    dayName = "Ngày mai";
                }
                else
                {
                    dayName = dt.DayOfWeek switch
                    {
                        DayOfWeek.Monday => "Thứ Hai",
                        DayOfWeek.Tuesday => "Thứ Ba",
                        DayOfWeek.Wednesday => "Thứ Tư",
                        DayOfWeek.Thursday => "Thứ Năm",
                        DayOfWeek.Friday => "Thứ Sáu",
                        DayOfWeek.Saturday => "Thứ Bảy",
                        DayOfWeek.Sunday => "Chủ Nhật",
                        _ => dt.ToString("dddd", new CultureInfo("vi-VN"))
                    };
                }
            }
            else
            {
                dayName = $"Ngày {i + 1}";
            }

            double max = dayMaxs[i];
            double min = dayMins[i];

            int code = (codes != null && i < codes.Count) ? codes[i] : 0;
            int rainProb = (rainProbs != null && i < rainProbs.Count) ? rainProbs[i] : 0;

            var (desc, glyph) = WeatherCodeHelper.GetConditionInfo(code, isDay: true);
            string iconPack = settings?.SelectedIconPack ?? "Meteocons";
            string svgPath = WeatherCodeHelper.GetWeatherIconPath(code, isDay: true, iconPack);

            // Tính toán khoảng hiển thị thanh Bar (Offset & Width)
            double left = Math.Clamp(((min - weekMin) / tempRange) * totalBarWidth, 0.0, totalBarWidth - 12.0);
            double width = Math.Clamp(((max - min) / tempRange) * totalBarWidth, 14.0, totalBarWidth - left);

            bool isToday = (i == 0);
            double currentDotOffset = 0;
            bool showCurrentDot = false;

            if (isToday && data.Current != null)
            {
                double curTemp = isFahrenheit ? ToFahrenheit(data.Current.Temperature) : data.Current.Temperature;
                double dayRange = Math.Max(0.5, max - min);
                currentDotOffset = Math.Clamp(((curTemp - min) / dayRange) * width, 0.0, width);
                showCurrentDot = true;
            }

            list.Add(new DailyForecastItem
            {
                DayName = dayName,
                DateDisplay = dateDisplay,
                IconGlyph = glyph,
                SvgIconPath = svgPath,
                ConditionText = desc,
                TempMaxDisplay = $"{Math.Round(max)}{unit}",
                TempMinDisplay = $"{Math.Round(min)}{unit}",
                RainProbabilityText = $"{rainProb}%",
                MinTemp = min,
                MaxTemp = max,
                BarLeftMargin = left,
                BarWidth = width,
                IsToday = isToday,
                CurrentTempIndicatorOffset = currentDotOffset,
                ShowCurrentIndicator = showCurrentDot
            });
        }

        return list;
    }

    private static double ToFahrenheit(double celsius) => (celsius * 9.0 / 5.0) + 32.0;

    private static string FormatTimeOnly(string isoDateTime)
    {
        if (DateTime.TryParse(isoDateTime, out DateTime dt))
        {
            return dt.ToString("HH:mm");
        }
        return isoDateTime;
    }
}
