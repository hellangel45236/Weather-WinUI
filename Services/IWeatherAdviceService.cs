using WeatherApp.Models;

namespace WeatherApp.Services;

public interface IWeatherAdviceService
{
    List<WeatherAdvice> GenerateAdvice(CurrentWeatherDto current, DailyWeatherDto? daily);
}
