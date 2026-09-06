using System.Collections.Generic;
using SolaceWeather.Core;

namespace SolaceWeather.Presentation;

public interface IWeatherPresentation
{
    Region ActiveRegion { get; }
    bool Enabled { get; }
    bool? PendingEnabled { get; }
    bool UseFahrenheit { get; set; }
    string? UnavailableReason { get; }
    string? GetSpecialWeather(Region region, int daysAhead);
    WeatherSample GetCurrent(Region region);
    DayForecast GetForecast(Region region, int daysAhead);
    IReadOnlyList<TimelineEntry> GetTodayTimeline(Region region);
    void RequestEnabled(bool enabled);
}

public record TimelineEntry(string PeriodKey, WeatherSample Weather);
