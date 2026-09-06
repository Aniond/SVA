using System.Text.Json;
using SolaceWeather.Core;

namespace SolaceWeather.Core.Tests;

public class WeatherTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void InvalidWateringThresholdCannotBypassRainRequirement(double threshold)
    {
        var settings = new ClimateSettings { CropWateringThreshold = threshold };
        Assert.Throws<ArgumentException>(() => new WeatherEngine(1, settings));
    }

    [Fact]
    public void MalformedClimateFailsClearlyBeforeWeatherIsRequested()
    {
        var settings = new ClimateSettings { Valley = new ClimateProfile { SeasonalTemperaturesC = new[] { 12d } } };
        Assert.Throws<ArgumentException>(() => new WeatherEngine(1, settings));
        settings.Valley.SeasonalTemperaturesC = new[] { 1d, 2d, 3d, double.NaN };
        Assert.Throws<ArgumentException>(() => new WeatherEngine(1, settings));
    }

    [Fact]
    public void JsonClimateProfilesControlWeatherAndAreStableAfterConstruction()
    {
        var settings = JsonSerializer.Deserialize<ClimateSettings>("{\"Valley\":{\"SeasonalTemperaturesC\":[30,30,30,30],\"PrecipitationChance\":0}}")!;
        var engine = new WeatherEngine(88, settings);
        var sample = engine.GetWeather(98, 900, Region.Farm);
        Assert.True(sample.TemperatureC > 20);
        Assert.Equal(0, engine.Rainfall(98, 1560, Region.Farm));
        settings.Valley.SeasonalTemperaturesC[3] = -100;
        Assert.Equal(sample, engine.GetWeather(98, 900, Region.Farm));
    }

    [Fact]
    public void ReloadAndQueryOrderCannotChangeWeather()
    {
        var engine = new WeatherEngine(19283);
        var expected = engine.GetWeather(52, 937, Region.Farm);
        engine.GetWeather(230, 1200, Region.Island);
        Assert.Equal(expected, new WeatherEngine(19283).GetWeather(52, 937, Region.Farm));
        Assert.NotEqual(expected, new WeatherEngine(19284).GetWeather(52, 937, Region.Farm));
        Assert.NotEqual(expected, engine.GetWeather(53, 937, Region.Farm));
    }

    [Fact]
    public void RegionalClimateSharesValleyFrontButChangesLocalConditions()
    {
        var engine = new WeatherEngine(1);
        var farm = engine.GetWeather(40, 900, Region.Farm);
        var mountain = engine.GetWeather(40, 900, Region.Mountain);
        Assert.True(mountain.TemperatureC < farm.TemperatureC);
        Assert.True(engine.GetWeather(40, 900, Region.Desert).TemperatureC > farm.TemperatureC);
        Assert.NotEqual(farm.CloudCover, engine.GetWeather(40, 900, Region.Island).CloudCover);
        Assert.InRange(Math.Abs(farm.CloudCover - mountain.CloudCover), 0, .11);
    }

    [Fact]
    public void SeasonsAndHourlyKeyframesDoNotIntroduceTemperatureJumps()
    {
        var engine = new WeatherEngine(72);
        foreach (int day in new[] { 28, 56, 84, 112 })
            Assert.InRange(Math.Abs(engine.GetWeather(day, 720, Region.Farm).TemperatureC - engine.GetWeather(day - 1, 720, Region.Farm).TemperatureC), 0, 5);
        for (int t = 361; t <= 1560; t++)
        {
            var a = engine.GetWeather(12, t - 1, Region.Farm);
            var b = engine.GetWeather(12, t, Region.Farm);
            Assert.InRange(Math.Abs(a.TemperatureC - b.TemperatureC), 0, .2);
            Assert.InRange(Math.Abs(a.RainIntensity - b.RainIntensity), 0, .05);
        }
    }

    [Fact]
    public void ForecastUsesActualMinuteTimelineIncludingTomorrow()
    {
        var engine = new WeatherEngine(818);
        foreach (var region in Enum.GetValues<Region>())
        {
            var forecast = engine.GetForecast(43, region);
            var actual = Enumerable.Range(360, 1201).Select(t => engine.GetWeather(43, t, region)).ToArray();
            Assert.Equal(actual.Min(x => x.TemperatureC), forecast.MinTemperatureC, 8);
            Assert.Equal(actual.Max(x => x.TemperatureC), forecast.MaxTemperatureC, 8);
            Assert.Equal(actual.GroupBy(x => x.Kind).OrderByDescending(x => x.Count()).ThenBy(x => x.Key).First().Key, forecast.MainKind);
        }
    }

    [Fact]
    public void RainAccumulationMatchesActualRainAndCompletesAfterEarlySleep()
    {
        var engine = new WeatherEngine(35);
        int day = Enumerable.Range(0, 84).First(d => engine.Rainfall(d, 1560, Region.Farm) >= 60);
        double beforeBed = engine.Rainfall(day, 900, Region.Farm);
        double remainder = Enumerable.Range(900, 660).Sum(t => (engine.GetWeather(day, t, Region.Farm).RainIntensity + engine.GetWeather(day, t + 1, Region.Farm).RainIntensity) / 2);
        Assert.Equal(engine.Rainfall(day, 1560, Region.Farm), beforeBed + remainder, 7);
        Assert.Equal(0, engine.Rainfall(day, 100, Region.Farm));
        Assert.Equal(engine.Rainfall(day, 1560, Region.Farm), engine.Rainfall(day, 2000, Region.Farm));
    }

    [Fact]
    public void FreezingPrecipitationIsSnowAndDoesNotWaterCrops()
    {
        var settings = new ClimateSettings { Valley = new ClimateProfile { SeasonalTemperaturesC = new[] { -20d, -20d, -20d, -20d }, PrecipitationChance = 1 } };
        var engine = new WeatherEngine(1, settings);
        Assert.Contains(Enumerable.Range(360, 1201).Select(t => engine.GetWeather(10, t, Region.Farm).Kind), x => x == WeatherKind.Snow);
        Assert.Equal(0, engine.Rainfall(10, 1560, Region.Farm));
    }

    [Fact]
    public void SavedEnableRequestAppliesOnlyNextMorningAndClears()
    {
        var state = new WeatherSaveState { Seed = 765, Enabled = true, PendingEnabled = false };
        var restored = JsonSerializer.Deserialize<WeatherSaveState>(JsonSerializer.Serialize(state))!;
        Assert.True(restored.Enabled);
        restored.ApplyPendingEnable();
        Assert.False(restored.Enabled);
        Assert.Null(restored.PendingEnabled);
        Assert.Equal(765, restored.Seed);
        restored.ApplyPendingEnable();
        Assert.False(restored.Enabled);
    }
}
