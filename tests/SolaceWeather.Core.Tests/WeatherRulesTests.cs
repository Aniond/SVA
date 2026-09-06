using SolaceWeather.Core;

namespace SolaceWeather.Core.Tests;

public class WeatherRulesTests
{
    [Theory]
    [InlineData(WeatherKind.Clear, "Sun", false)]
    [InlineData(WeatherKind.Cloudy, "Sun", false)]
    [InlineData(WeatherKind.Rain, "Rain", true)]
    [InlineData(WeatherKind.Storm, "Storm", true)]
    [InlineData(WeatherKind.Snow, "Snow", false)]
    [InlineData(WeatherKind.Wind, "Wind", false)]
    public void GameWeatherAndRainFlagsAgree(WeatherKind kind, string expectedId, bool expectedRain)
    {
        Assert.Equal(expectedId, WeatherRules.ToGameWeatherId(kind));
        Assert.Equal(expectedRain, WeatherRules.IsRain(kind));
    }
}
