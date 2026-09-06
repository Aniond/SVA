namespace SolaceWeather.Core;

public enum Region { Farm, Town, Forest, Mountain, Beach, Desert, Island }
public enum WeatherKind { Clear, Cloudy, Rain, Storm, Snow, Wind }
public record WeatherSample(WeatherKind Kind, double TemperatureC, double RainIntensity, double CloudCover, double Wind);
public record DayForecast(int Day, Region Region, WeatherKind MainKind, double MinTemperatureC, double MaxTemperatureC);
public class ClimateProfile
{
    public double[] SeasonalTemperaturesC { get; set; } = new[] { 13d, 26d, 14d, -5d };
    public double PrecipitationChance { get; set; } = .45;
    public double[]? SeasonalPrecipitationChances { get; set; }
    public double RainIntensityScale { get; set; } = 1;
    public double DailyTemperatureSwingC { get; set; } = 5;
}
public class ClimateSettings
{
    public ClimateProfile Valley { get; set; } = new() { SeasonalPrecipitationChances = new[] { .50, .22, .48, .30 } };
    public ClimateProfile Desert { get; set; } = new() { SeasonalTemperaturesC = new[] { 25d, 38d, 27d, 18d }, PrecipitationChance = .04, SeasonalPrecipitationChances = new[] { .06, .02, .05, .04 }, DailyTemperatureSwingC = 9 };
    public ClimateProfile Island { get; set; } = new() { SeasonalTemperaturesC = new[] { 26d, 29d, 27d, 24d }, PrecipitationChance = .55, SeasonalPrecipitationChances = new[] { .50, .60, .55, .40 }, DailyTemperatureSwingC = 3 };
    public double CropWateringThreshold { get; set; } = 60;
}
public class WeatherSaveState
{
    public int Version { get; set; } = 1;
    public long Seed { get; set; }
    public bool Enabled { get; set; }
    public bool? PendingEnabled { get; set; }
    public int LastWateredDay { get; set; } = -1;
    public void ApplyPendingEnable()
    {
        if (PendingEnabled.HasValue) Enabled = PendingEnabled.Value;
        PendingEnabled = null;
    }
}
public class WeatherEngine
{
    private readonly long seed;
    private readonly ClimateSettings settings;

    public WeatherEngine(long seed, ClimateSettings? settings = null)
    {
        this.seed = seed;
        settings ??= new ClimateSettings();
        if (!double.IsFinite(settings.CropWateringThreshold) || settings.CropWateringThreshold <= 0)
            throw new ArgumentException("Crop watering threshold must be a finite positive number.", nameof(settings));
        this.settings = new ClimateSettings
        {
            Valley = CopyProfile(settings.Valley, "Valley"),
            Desert = CopyProfile(settings.Desert, "Desert"),
            Island = CopyProfile(settings.Island, "Island"),
            CropWateringThreshold = settings.CropWateringThreshold
        };
    }

    private static ClimateProfile CopyProfile(ClimateProfile? profile, string name)
    {
        if (profile?.SeasonalTemperaturesC is not { Length: 4 } temperatures || temperatures.Any(t => !double.IsFinite(t))
            || !double.IsFinite(profile.PrecipitationChance) || profile.PrecipitationChance < 0 || profile.PrecipitationChance > 1
            || (profile.SeasonalPrecipitationChances is { } chances && (chances.Length != 4 || chances.Any(c => !double.IsFinite(c) || c < 0 || c > 1)))
            || !double.IsFinite(profile.RainIntensityScale) || profile.RainIntensityScale < 0
            || !double.IsFinite(profile.DailyTemperatureSwingC) || profile.DailyTemperatureSwingC < 0)
            throw new ArgumentException($"Invalid {name} climate: provide four finite seasonal temperatures, precipitation chance 0–1, and nonnegative intensity and daily swing.", nameof(profile));
        return new ClimateProfile
        {
            SeasonalTemperaturesC = (double[])temperatures.Clone(),
            PrecipitationChance = profile.PrecipitationChance,
            SeasonalPrecipitationChances = profile.SeasonalPrecipitationChances is { } seasonalChances ? (double[])seasonalChances.Clone() : null,
            RainIntensityScale = profile.RainIntensityScale,
            DailyTemperatureSwingC = profile.DailyTemperatureSwingC
        };
    }

    public WeatherSample GetWeather(int absoluteDay, int minutes, Region region)
    {
        minutes = Math.Clamp(minutes, 360, 1560);
        int hour = minutes / 60;
        double blend = Smooth((minutes % 60) / 60d);
        var a = Keyframe(absoluteDay, hour, region);
        var b = Keyframe(absoluteDay, hour + 1, region);
        double temperature = Lerp(a.Temperature, b.Temperature, blend);
        double precipitation = Lerp(a.Precipitation, b.Precipitation, blend);
        double cloud = Lerp(a.Cloud, b.Cloud, blend);
        double wind = Lerp(a.Wind, b.Wind, blend);
        // Snow contributes no crop watering; the thaw band eases liquid rain in.
        double rain = precipitation * Smooth(Math.Clamp((temperature - 1) / 2, 0, 1));
        var kind = precipitation > .02
            ? temperature <= 1 ? WeatherKind.Snow : rain > .8 && wind > .6 ? WeatherKind.Storm : WeatherKind.Rain
            : wind > .72 ? WeatherKind.Wind : cloud > .5 ? WeatherKind.Cloudy : WeatherKind.Clear;
        return new(kind, temperature, rain, cloud, wind);
    }

    public DayForecast GetForecast(int absoluteDay, Region region)
    {
        // Sampling every game minute includes short weather windows and the exact
        // extrema of the hourly interpolation. Forecasts never roll new weather.
        var samples = Enumerable.Range(360, 1201).Select(t => GetWeather(absoluteDay, t, region)).ToArray();
        var dominant = samples.GroupBy(s => s.Kind).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).First().Key;
        return new(absoluteDay, region, dominant, samples.Min(s => s.TemperatureC), samples.Max(s => s.TemperatureC));
    }

    public double Rainfall(int absoluteDay, int throughMinutes, Region region)
    {
        int end = Math.Clamp(throughMinutes, 360, 1560);
        double total = 0;
        double previous = GetWeather(absoluteDay, 360, region).RainIntensity;
        for (int minute = 361; minute <= end; minute++)
        {
            double next = GetWeather(absoluteDay, minute, region).RainIntensity;
            total += (previous + next) / 2;
            previous = next;
        }
        return total;
    }

    private (double Temperature, double Precipitation, double Cloud, double Wind) Keyframe(int day, int hour, Region region)
    {
        int climate = region == Region.Desert ? 1 : region == Region.Island ? 2 : 0;
        var profile = climate == 1 ? settings.Desert : climate == 2 ? settings.Island : settings.Valley;
        double time = day + hour / 24d;
        double seasonalPosition = ((time - 14) % 112 + 112) % 112 / 28;
        int season = (int)seasonalPosition;
        double seasonalTemperature = Lerp(profile.SeasonalTemperaturesC[season], profile.SeasonalTemperaturesC[(season + 1) % 4], Smooth(seasonalPosition - season));
        double front = Noise(time / 2, climate, 1);
        double fluctuation = (Noise(time * 3, climate, 2) - .5) * .35;
        double localCloud = region switch { Region.Forest => .04, Region.Mountain => .08, Region.Beach => -.03, _ => 0 };
        double cloud = Math.Clamp(front + fluctuation + localCloud, 0, 1);
        double chance = profile.SeasonalPrecipitationChances is { } chances
            ? Lerp(chances[season], chances[(season + 1) % 4], Smooth(seasonalPosition - season))
            : profile.PrecipitationChance;
        double precipitation = chance == 0 ? 0 : Smooth(Math.Clamp((cloud - (1 - chance)) * 3, 0, 1)) * Math.Max(0, profile.RainIntensityScale) * 1.35;
        double offset = region switch { Region.Town => 1, Region.Forest => -1.5, Region.Mountain => -4, Region.Beach => -.5, _ => 0 };
        double temperature = seasonalTemperature + offset + (Noise(time / 3, climate, 3) - .5) * 6
            + Math.Cos((hour - 14) * Math.PI / 12) * profile.DailyTemperatureSwingC - cloud * 2;
        double wind = Math.Clamp(Noise(time * 2, climate, 4) * .75 + cloud * .25 + (region == Region.Mountain ? .1 : 0), 0, 1);
        return (temperature, precipitation, cloud, wind);
    }

    private double Noise(double time, int climate, int channel)
    {
        long index = (long)Math.Floor(time);
        return Lerp(Random(index, climate, channel), Random(index + 1, climate, channel), Smooth(time - index));
    }

    private double Random(long index, int climate, int channel)
    {
        // Fixed integer mixing avoids process-dependent string hashes and Random state.
        unchecked
        {
            ulong value = (ulong)seed ^ ((ulong)index * 0x9E3779B97F4A7C15UL)
                ^ ((ulong)(climate + 1) * 0xBF58476D1CE4E5B9UL) ^ ((ulong)channel * 0x94D049BB133111EBUL);
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            value ^= value >> 31;
            return (value >> 11) * (1d / 9007199254740992d);
        }
    }

    private static double Smooth(double value) => value * value * (3 - 2 * value);
    private static double Lerp(double a, double b, double blend) => a + (b - a) * blend;
}
