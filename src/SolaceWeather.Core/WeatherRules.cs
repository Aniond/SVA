namespace SolaceWeather.Core;

public static class WeatherRules
{
    public static bool IsRain(WeatherKind kind) => kind is WeatherKind.Rain or WeatherKind.Storm;
    public static string ToGameWeatherId(WeatherKind kind) => kind switch
    {
        WeatherKind.Rain => "Rain",
        WeatherKind.Storm => "Storm",
        WeatherKind.Snow => "Snow",
        WeatherKind.Wind => "Wind",
        _ => "Sun"
    };
}
