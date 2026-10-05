using WeatherApp.Models;

namespace WeatherApp.Services;

public interface IWeatherService
{
    Task<OpenMeteoResponse?> GetWeatherDataAsync(double latitude, double longitude);
    Task<AirQualityData> GetAirQualityAsync(double latitude, double longitude);
    CurrentWeatherDisplay CreateCurrentWeatherDisplay(OpenMeteoResponse data, string locationName, AppSettings? settings = null, AirQualityData? airQuality = null);
    List<HourlyForecastItem> CreateHourlyForecast(OpenMeteoResponse data, AppSettings? settings = null);
    List<DailyForecastItem> CreateDailyForecast(OpenMeteoResponse data, AppSettings? settings = null);
}
