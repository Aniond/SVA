using System.Text.Json;
using SolaceWeather.Core;

namespace SolaceWeather.Core.Tests;

public class SeasonalRainTests
{
    [Fact]
    public void ValleySpringAndFallAreWetterThanSummerAcrossManySeeds()
    {
        double[] totals = new double[3];
        for (int seed = 0; seed < 64; seed++)
        {
            var engine = new WeatherEngine(seed);
            for (int season = 0; season < 3; season++)
                for (int day = season * 28; day < (season + 1) * 28; day++)
                    totals[season] += engine.GetWeather(day, 900, Region.Farm).RainIntensity;
        }
        Assert.True(totals[0] > totals[1] * 1.25, $"Spring {totals[0]} should be wetter than summer {totals[1]}.");
        Assert.True(totals[2] > totals[1] * 1.25, $"Fall {totals[2]} should be wetter than summer {totals[1]}.");
    }

    [Fact]
    public void SeasonalChanceOverridesLegacyScalarAndBlendsAcrossBoundary()
    {
        var settings = JsonSerializer.Deserialize<ClimateSettings>("{\"Valley\":{\"SeasonalTemperaturesC\":[30,30,30,30],\"PrecipitationChance\":0,\"SeasonalPrecipitationChances\":[1,0,1,0]}}")!;
        double springRain = 0;
        double[] boundaryDifferences = new double[4];
        for (int seed = 0; seed < 128; seed++)
        {
            var engine = new WeatherEngine(seed, settings);
            springRain += engine.Rainfall(14, 1560, Region.Farm);
            // Near a zero-chance seasonal midpoint, continuous interpolation may
            // allow a trace amount as the calendar advances toward the next season.
            Assert.InRange(engine.Rainfall(42, 1560, Region.Farm), 0, 1);
            for (int boundary = 0; boundary < 4; boundary++)
            {
                int day = (boundary + 1) * 28;
                // Average over independent fronts: a hard seasonal switch would
                // cause a large population-wide rainfall jump on the first day.
                boundaryDifferences[boundary] += engine.GetWeather(day, 900, Region.Farm).RainIntensity
                    - engine.GetWeather(day - 1, 900, Region.Farm).RainIntensity;
            }
        }
        Assert.True(springRain > 1000);
        Assert.All(boundaryDifferences, difference => Assert.InRange(Math.Abs(difference / 128), 0, .15));
    }

    [Theory]
    [InlineData("[0.1]")]
    [InlineData("[0.1,0.2,0.3,1.1]")]
    [InlineData("[0.1,-0.2,0.3,0.4]")]
    public void InvalidSeasonalChanceConfigurationIsRejected(string chances)
    {
        var settings = JsonSerializer.Deserialize<ClimateSettings>("{\"Valley\":{\"SeasonalPrecipitationChances\":" + chances + "}}")!;
        Assert.Throws<ArgumentException>(() => new WeatherEngine(1, settings));
    }
}
