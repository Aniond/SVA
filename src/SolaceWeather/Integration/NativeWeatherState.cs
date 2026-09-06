using StardewValley.Network;

namespace SolaceWeather.Integration;

/// <summary>Preserves native weather separately so opting out doesn't consume a simulated forecast.</summary>
public sealed class NativeWeatherState
{
    public string Weather { get; set; } = "Sun";
    public string Tomorrow { get; set; } = "Sun";
    public bool Rain { get; set; }
    public bool Snow { get; set; }
    public bool Lightning { get; set; }
    public bool Wind { get; set; }
    public bool GreenRain { get; set; }
    public static NativeWeatherState From(LocationWeather weather) => new()
    { Weather = weather.Weather, Tomorrow = weather.WeatherForTomorrow, Rain = weather.IsRaining, Snow = weather.IsSnowing, Lightning = weather.IsLightning, Wind = weather.IsDebrisWeather, GreenRain = weather.IsGreenRain };
    public LocationWeather ToWeather() => new()
    { Weather = Weather, WeatherForTomorrow = Tomorrow, IsRaining = Rain, IsSnowing = Snow, IsLightning = Lightning, IsDebrisWeather = Wind, IsGreenRain = GreenRain };
}
