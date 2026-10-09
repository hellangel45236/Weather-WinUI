using System.Text.Json.Serialization;

namespace WeatherApp.Models;

public class OpenMeteoResponse
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = string.Empty;

    [JsonPropertyName("current")]
    public CurrentWeatherDto? Current { get; set; }

    [JsonPropertyName("hourly")]
    public HourlyWeatherDto? Hourly { get; set; }

    [JsonPropertyName("daily")]
    public DailyWeatherDto? Daily { get; set; }
}

public class CurrentWeatherDto
{
    [JsonPropertyName("time")]
    public string Time { get; set; } = string.Empty;

    [JsonPropertyName("temperature_2m")]
    public double Temperature { get; set; }

    [JsonPropertyName("relative_humidity_2m")]
    public double RelativeHumidity { get; set; }

    [JsonPropertyName("apparent_temperature")]
    public double ApparentTemperature { get; set; }

    [JsonPropertyName("is_day")]
    public int IsDay { get; set; }

    [JsonPropertyName("precipitation")]
    public double Precipitation { get; set; }

    [JsonPropertyName("weather_code")]
    public int WeatherCode { get; set; }

    [JsonPropertyName("cloud_cover")]
    public double CloudCover { get; set; }

    [JsonPropertyName("surface_pressure")]
    public double SurfacePressure { get; set; }

    [JsonPropertyName("wind_speed_10m")]
    public double WindSpeed { get; set; }

    [JsonPropertyName("wind_direction_10m")]
    public double WindDirection { get; set; }

    [JsonPropertyName("wind_gusts_10m")]
    public double WindGusts { get; set; }
}

public class HourlyWeatherDto
{
    [JsonPropertyName("time")]
    public List<string>? Time { get; set; }

    [JsonPropertyName("temperature_2m")]
    public List<double>? Temperature { get; set; }

    [JsonPropertyName("relative_humidity_2m")]
    public List<double>? RelativeHumidity { get; set; }

    [JsonPropertyName("precipitation_probability")]
    public List<int>? PrecipitationProbability { get; set; }

    [JsonPropertyName("precipitation")]
    public List<double>? Precipitation { get; set; }

    [JsonPropertyName("weather_code")]
    public List<int>? WeatherCode { get; set; }

    [JsonPropertyName("wind_speed_10m")]
    public List<double>? WindSpeed { get; set; }

    [JsonPropertyName("surface_pressure")]
    public List<double>? SurfacePressure { get; set; }
}

public class DailyWeatherDto
{
    [JsonPropertyName("time")]
    public List<string>? Time { get; set; }

    [JsonPropertyName("weather_code")]
    public List<int>? WeatherCode { get; set; }

    [JsonPropertyName("temperature_2m_max")]
    public List<double>? TemperatureMax { get; set; }

    [JsonPropertyName("temperature_2m_min")]
    public List<double>? TemperatureMin { get; set; }

    [JsonPropertyName("apparent_temperature_max")]
    public List<double>? ApparentTemperatureMax { get; set; }

    [JsonPropertyName("apparent_temperature_min")]
    public List<double>? ApparentTemperatureMin { get; set; }

    [JsonPropertyName("sunrise")]
    public List<string>? Sunrise { get; set; }

    [JsonPropertyName("sunset")]
    public List<string>? Sunset { get; set; }

    [JsonPropertyName("uv_index_max")]
    public List<double>? UvIndexMax { get; set; }

    [JsonPropertyName("precipitation_sum")]
    public List<double>? PrecipitationSum { get; set; }

    [JsonPropertyName("precipitation_probability_max")]
    public List<int>? PrecipitationProbabilityMax { get; set; }

    [JsonPropertyName("wind_speed_10m_max")]
    public List<double>? WindSpeedMax { get; set; }

    [JsonPropertyName("wind_direction_10m_dominant")]
    public List<double>? WindDirectionDominant { get; set; }
}

public class AirQualityData
{
    public int Aqi { get; set; } = 0;
    public double Pm25 { get; set; } = 0;
    public double Pm10 { get; set; } = 0;
    public double Ozone { get; set; } = 0;
    public double No2 { get; set; } = 0;
    public double So2 { get; set; } = 0;
    public double Co { get; set; } = 0;
    public bool HasData => Aqi > 0;
}
