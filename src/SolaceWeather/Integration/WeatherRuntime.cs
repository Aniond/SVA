using Microsoft.Xna.Framework;
using SolaceWeather.Core;
using SolaceWeather.Presentation;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Network;
using StardewValley.TerrainFeatures;

namespace SolaceWeather.Integration;

/// <summary>Owns game-facing weather. The simulation itself has no game dependency.</summary>
internal sealed class WeatherRuntime : IWeatherPresentation
{
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly ModConfig config;
    private readonly ClimateSettings climate;
    private readonly Dictionary<(int, Region), DayForecast> forecasts = new();
    private readonly Dictionary<(int, Region), string?> protections = new();
    private readonly Dictionary<Region, LocationWeather> localWeather = new();
    private readonly HashSet<(int, string)> watered = new();
    private readonly Dictionary<Region, WeatherKind> forced = new();
    private readonly Dictionary<string, LocationWeather> originals = new();
    private int endingDay = -1;
    private int begunDay = -1;
    private WeatherKind? lastVisual;
    private bool faulted;
    public WeatherSaveState? State { get; private set; }
    public WeatherEngine? Engine { get; private set; }
    public bool Compatible { get; set; }
    public int ScheduleDepth { get; set; }
    public bool Ending => endingDay >= 0;
    public bool Enabled => State?.Enabled == true && Compatible && !faulted && !Context.IsMultiplayer;
    public bool? PendingEnabled => State?.PendingEnabled;
    public string? UnavailableReason => !Compatible ? "unavailable.incompatible" : faulted ? "unavailable.session" : Context.IsMultiplayer ? "unavailable.multiplayer" : State == null ? "unavailable.not-loaded" : null;
    public string? GetSpecialWeather(Region region, int daysAhead) => Protection(Day + daysAhead, region);
    public bool UseFahrenheit { get => config.UseFahrenheit; set { config.UseFahrenheit = value; helper.WriteConfig(config); } }
    public Region ActiveRegion => RegionFor(Game1.currentLocation);
    public int Day => Game1.Date.TotalDays;
    public int Minute => Math.Min(1560, ToMinutes(Game1.timeOfDay) + Math.Clamp(Game1.gameTimeInterval * 10 / Math.Max(1, Game1.realMilliSecondsPerGameTenMinutes), 0, 9));

    public WeatherRuntime(IModHelper helper, IMonitor monitor, ModConfig config, ClimateSettings climate)
    { this.helper = helper; this.monitor = monitor; this.config = config; this.climate = climate; }

    public void Load()
    {
        Reset();
        try
        {
            State = helper.Data.ReadSaveData<WeatherSaveState>("weather-state")
                ?? new WeatherSaveState { Seed = unchecked((long)Game1.uniqueIDForThisGame) };
            if (State.Version != 1)
                throw new InvalidOperationException($"Unsupported weather save version {State.Version}; the stored data was not changed.");
            Engine = new WeatherEngine(State.Seed, climate);
            CaptureOriginals();
            var native = helper.Data.ReadSaveData<Dictionary<string, NativeWeatherState>>("native-weather");
            if (native != null)
                foreach (var (context, snapshot) in native) originals[context] = snapshot.ToWeather();
            monitor.Log($"Loaded farm '{Game1.player.farmName.Value}', weather {(Enabled ? "enabled" : "disabled")}, seed {State.Seed}, day {Day}.", LogLevel.Info);
        }
        catch (Exception ex) { DisableForSession(ex); }
    }

    public void Save()
    {
        if (State != null && !faulted)
        {
            helper.Data.WriteSaveData("weather-state", State);
            helper.Data.WriteSaveData("native-weather", originals.ToDictionary(p => p.Key, p => NativeWeatherState.From(p.Value)));
        }
    }

    public void Reset()
    {
        State = null; Engine = null; endingDay = -1; begunDay = -1; ScheduleDepth = 0;
        faulted = false; lastVisual = null;
        forecasts.Clear(); protections.Clear(); localWeather.Clear(); watered.Clear(); forced.Clear(); originals.Clear();
    }

    public void RequestEnabled(bool enabled)
    {
        if (State == null || !Compatible || faulted || Context.IsMultiplayer) return;
        State.PendingEnabled = enabled == State.Enabled ? null : enabled;
    }

    public static int ToMinutes(int hhmm) => hhmm / 100 * 60 + hhmm % 100;
    public static int ToClock(int minutes) => minutes / 60 * 100 + minutes % 60;

    public static Region RegionFor(GameLocation? location)
    {
        if (location == null) return Region.Farm;
        string name = location.NameOrUniqueName ?? location.Name ?? "";
        if (location.InIslandContext() || name.StartsWith("Island", StringComparison.Ordinal)) return Region.Island;
        if (location.GetLocationContextId() == "Desert" || name is "Desert" or "SandyHouse" or "SandyShop" or "Club" or "SkullCave") return Region.Desert;
        if (name.Contains("Beach", StringComparison.Ordinal) || name is "FishShop" or "ElliottHouse") return Region.Beach;
        if (name is "Forest" or "Woods" or "WizardHouse" or "AnimalShop" or "LeahHouse") return Region.Forest;
        if (name is "Mountain" or "Backwoods" or "BusStop" or "Tent" or "ScienceHouse" or "Mine" or "Railroad" or "AdventureGuild") return Region.Mountain;
        if (name.StartsWith("Farm", StringComparison.Ordinal) || location.IsFarm || location.IsGreenhouse) return Region.Farm;
        return Region.Town;
    }

    public string? Protection(int day, Region region)
    {
        WorldDate date = new(Game1.Date) { TotalDays = day };
        bool valley = region is not (Region.Desert or Region.Island);
        if (valley)
        {
            if (day == Day && Game1.weddingToday) return "Wedding";
            // Known wedding dates are deterministic too. Match the native postponement
            // to the first eligible day, and leave roommate moves out of the forecast.
            Farmer player = Game1.player;
            if (player.spouse != null && player.isEngaged() && !player.hasCurrentOrPendingRoommate()
                && player.friendshipData.TryGetValue(player.spouse, out var friendship) && friendship.WeddingDate != null)
            {
                WorldDate wedding = new(friendship.WeddingDate) { TotalDays = Math.Max(Day, friendship.WeddingDate.TotalDays) };
                for (int tries = 0; tries < 112 && !Game1.canHaveWeddingOnDay(wedding.DayOfMonth, wedding.Season); tries++) wedding.TotalDays++;
                if (wedding.TotalDays == day) return "Wedding";
            }
        }
        if (protections.TryGetValue((day, region), out var cached)) return cached;
        string context = region == Region.Desert ? "Desert" : region == Region.Island ? "Island" : "Default";
        bool eggPilot = valley && date.Season == Season.Spring && date.DayOfMonth == 13;
        if (!eggPilot && Utility.isFestivalDay(date.DayOfMonth, date.Season, context))
            return protections[(day, region)] = "Festival";
        // Read date-based data directly: ActivePassiveFestivals is not populated yet
        // during early overnight setup, when weather is first queried.
        foreach (var passive in DataLoader.PassiveFestivals(Game1.content).Values)
        {
            if (date.Season != passive.Season || date.DayOfMonth < passive.StartDay || date.DayOfMonth > passive.EndDay
                || passive.MapReplacements == null || !GameStateQuery.CheckConditions(passive.Condition)) continue;
            if (passive.MapReplacements.Keys.Any(name => Game1.getLocationFromName(name)?.GetLocationContextId() == context))
                return protections[(day, region)] = "Sun";
        }
        if (!valley) return protections[(day, region)] = null;
        const string marker = "Solace_Unspecified";
        string weather = Game1.getWeatherModificationsForDate(date, marker);
        if (weather == "Festival" && date.Season == Season.Spring && date.DayOfMonth == 13) weather = marker;
        return protections[(day, region)] = weather == marker ? null : weather;
    }

    public WeatherSample Sample(int day, int minute, Region region)
    {
        var sample = Engine!.GetWeather(day, minute, region);
        string? protection = Protection(day, region);
        if (protection != null) return WithKind(sample, FromId(protection));
        if (day == (Ending ? endingDay : Day) && forced.TryGetValue(region, out var kind)) return WithKind(sample, kind);
        return sample;
    }

    private static WeatherSample WithKind(WeatherSample sample, WeatherKind kind) => sample with
    {
        Kind = kind,
        RainIntensity = kind == WeatherKind.Storm ? 2 : kind == WeatherKind.Rain ? 1 : 0,
        CloudCover = kind is WeatherKind.Rain or WeatherKind.Storm or WeatherKind.Snow ? .9 : kind == WeatherKind.Cloudy ? .7 : .1,
        Wind = kind is WeatherKind.Storm or WeatherKind.Wind ? .9 : .2
    };

    public WeatherSample GetCurrent(Region region) => Sample(Day, Minute, region);
    public DayForecast GetForecast(Region region, int daysAhead)
    {
        int day = Day + daysAhead;
        if (!forecasts.TryGetValue((day, region), out var forecast))
            forecasts[(day, region)] = forecast = Engine!.GetForecast(day, region);
        string? protection = Protection(day, region);
        if (protection != null) forecast = forecast with { MainKind = FromId(protection) };
        else if (daysAhead == 0 && forced.TryGetValue(region, out var forcedKind)) forecast = forecast with { MainKind = forcedKind };
        return forecast;
    }
    public IReadOnlyList<TimelineEntry> GetTodayTimeline(Region region) => new[]
    {
        new TimelineEntry("morning", Sample(Day, 360, region)),
        new TimelineEntry("noon", Sample(Day, 720, region)),
        new TimelineEntry("evening", Sample(Day, 1080, region)),
        new TimelineEntry("night", Sample(Day, 1440, region))
    };

    public double Rainfall(int day, int through, Region region)
    {
        string? protection = Protection(day, region);
        if (protection != null) return protection is "Rain" or "GreenRain" or "Storm" ? Math.Max(0, through - 360) * (protection == "Storm" ? 2 : 1) : 0;
        // Developer overrides intentionally replace this test day's weather.
        if (day == Day && forced.TryGetValue(region, out var kind))
            return Math.Max(0, through - 360) * (kind == WeatherKind.Storm ? 2 : kind == WeatherKind.Rain ? 1 : 0);
        return Engine!.Rainfall(day, through, region);
    }

    public bool TryLocal(GameLocation location, out LocationWeather result)
    {
        result = null!;
        if (!Enabled || Engine == null) return false;
        Region region = RegionFor(location);
        // Leave untouched special events in the native location weather system.
        if (Protection(Day, region) != null) return false;
        if (!localWeather.TryGetValue(region, out result!)) localWeather[region] = result = new LocationWeather();
        WeatherSample sample = Sample(Day, Ending ? 360 : Minute, region);
        SetWeather(result, sample.Kind);
        if (ScheduleDepth > 0)
            result.IsRaining = IsRain(GetForecast(region, 0).MainKind);
        else if (Ending)
            result.IsRaining = false; // Prevent native whole-day automatic soil watering during overnight setup.
        result.WeatherForTomorrow = ForecastId(region);
        return true;
    }

    public static bool IsRain(WeatherKind kind) => kind is WeatherKind.Rain or WeatherKind.Storm;
    public static string WeatherId(WeatherKind kind) => kind is WeatherKind.Clear or WeatherKind.Cloudy ? "Sun" : kind.ToString();
    public static WeatherKind FromId(string id) => Enum.TryParse<WeatherKind>(id, out var kind) ? kind : id == "GreenRain" ? WeatherKind.Rain : WeatherKind.Clear;
    private static void SetWeather(LocationWeather weather, WeatherKind kind)
    {
        weather.Weather = WeatherId(kind);
        weather.IsGreenRain = false;
        weather.IsRaining = IsRain(kind);
        weather.IsLightning = kind == WeatherKind.Storm;
        weather.IsSnowing = kind == WeatherKind.Snow;
        weather.IsDebrisWeather = kind == WeatherKind.Wind;
    }

    public void EndDay()
    {
        if (!Enabled) { if (State != null) endingDay = Day; return; }
        WaterThrough(1560);
        endingDay = Day;
        // Vanilla's overnight strike loop runs only if this flag is true; individual
        // calls are filtered to actual farm storm hours by the guarded patch.
        Game1.isLightning = Enumerable.Range(1, Math.Max(0, (2300 - Game1.timeOfDay) / 100))
            .Any(hour => Sample(Day, ToMinutes(Game1.timeOfDay + hour * 100), Region.Farm).Kind == WeatherKind.Storm);
    }

    public void BeforeNewDayWeather()
    {
        if (State == null || begunDay == Day || !Compatible || faulted || Context.IsMultiplayer) return;
        begunDay = Day;
        // Native forecasting must consume its own prior forecast, not the simulated
        // value exposed to TV. This also restores correct weather on disabling.
        if (State.Enabled)
        {
            foreach (var (context, snapshot) in originals)
                Game1.netWorldState.Value.GetWeatherForLocation(context).WeatherForTomorrow = snapshot.WeatherForTomorrow;
            if (originals.TryGetValue("Default", out var previous)) Game1.weatherForTomorrow = previous.WeatherForTomorrow;
        }
        State.ApplyPendingEnable();
        forced.Clear(); forecasts.Clear(); protections.Clear(); watered.Clear();
    }

    public void AfterNewDayWeather()
    {
        CaptureOriginals();
        if (!Enabled) return;
        foreach (var (context, region) in Contexts)
        {
            if (Protection(Day, region) != null) continue;
            var weather = Game1.netWorldState.Value.GetWeatherForLocation(context);
            SetWeather(weather, GetForecast(region, 0).MainKind);
            weather.WeatherForTomorrow = ForecastId(region);
        }
    }

    private string ForecastId(Region region) => Protection(Day + 1, region) ?? WeatherId(GetForecast(region, 1).MainKind);
    private static readonly (string Context, Region Region)[] Contexts = { ("Default", Region.Farm), ("Desert", Region.Desert), ("Island", Region.Island) };
    private void CaptureOriginals()
    {
        originals.Clear();
        foreach (var (context, _) in Contexts)
        {
            var copy = new LocationWeather(); copy.CopyFrom(Game1.netWorldState.Value.GetWeatherForLocation(context)); originals[context] = copy;
        }
    }

    public void StartDay()
    {
        endingDay = -1; ScheduleDepth = 0; lastVisual = null;
        if (Enabled) Refresh();
    }

    public void Refresh()
    {
        // Native travel and overnight setup index exactly seventy entries, even indoors.
        // Repair buffers made by older builds before any weather-specific early return.
        if (Context.IsWorldReady && Game1.rainDrops.Length < 70)
        {
            Array.Resize(ref Game1.rainDrops, 70);
            Game1.randomizeRainPositions();
        }
        if (!Enabled || Ending || !Context.IsWorldReady) return;
        WeatherSample farm = Sample(Day, Minute, Region.Farm);
        if (Protection(Day, Region.Farm) == null)
        {
            Game1.isRaining = IsRain(farm.Kind); Game1.isSnowing = farm.Kind == WeatherKind.Snow;
            Game1.isLightning = farm.Kind == WeatherKind.Storm; Game1.isDebrisWeather = farm.Kind == WeatherKind.Wind;
            Game1.isGreenRain = false;
        }
        foreach (var (context, region) in Contexts)
        {
            if (Protection(Day, region) != null) continue;
            var weather = Game1.netWorldState.Value.GetWeatherForLocation(context);
            SetWeather(weather, Sample(Day, Minute, region).Kind);
            weather.WeatherForTomorrow = ForecastId(region);
        }
        Game1.weatherForTomorrow = ForecastId(Region.Farm);
        var local = GetCurrent(ActiveRegion);
        if (lastVisual != local.Kind)
        {
            // Native ambient updates pause during events. Keep only the Egg Festival
            // pilot in sync when its weather changes; preserve other event lighting.
            if (Game1.currentLocation.IsOutdoors && Game1.CurrentEvent?.isSpecificFestival("spring13") == true)
                Game1.ambientLight = IsRain(local.Kind) ? new Color(255, 200, 80) : Color.White;
            if (local.Kind == WeatherKind.Wind && Game1.debrisWeather.Count == 0) Game1.populateDebrisWeatherArray();
            if (Game1.currentLocation.IsOutdoors && !Game1.eventUp)
            {
                if (IsRain(local.Kind)) Game1.changeMusicTrack("rain");
                else if (Game1.currentSong?.Name == "rain") Game1.changeMusicTrack("none");
            }
            lastVisual = local.Kind;
            Game1.updateWeatherIcon();
        }
        // Keep native minimum capacity; heavier rain can still increase density gradually.
        if (Game1.currentLocation.IsOutdoors && Protection(Day, ActiveRegion) == null && IsRain(local.Kind))
        {
            int target = Math.Clamp((int)(70 * local.RainIntensity), 70, 140);
            int current = Game1.rainDrops.Length;
            int count = current + Math.Clamp(target - current, -8, 8);
            if (current != count)
            {
                Array.Resize(ref Game1.rainDrops, count);
                for (int i = current; i < count; i++) Game1.rainDrops[i] = new RainDrop(Game1.random.Next(Math.Max(1, Game1.viewport.Width)), Game1.random.Next(Math.Max(1, Game1.viewport.Height)), Game1.random.Next(4), Game1.random.Next(70));
            }
        }
    }

    public void WaterThrough(int through)
    {
        if (!Enabled) return;
        var totals = new Dictionary<Region, double>();
        Utility.ForEachLocation(location =>
        {
            if (!location.IsOutdoors || location.IsGreenhouse || Protection(Day, RegionFor(location)) != null) return true;
            var region = RegionFor(location);
            if (!totals.TryGetValue(region, out double total)) totals[region] = total = Rainfall(Day, through, region);
            if (total < climate.CropWateringThreshold) return true;
            // Revisit tiles after threshold: crops planted later during a wet day need water too.
            foreach (var feature in location.terrainFeatures.Values)
                if (feature is HoeDirt soil && soil.state.Value == 0) { soil.state.Value = 1; soil.updateNeighbors(); }
            if (watered.Add((Day, location.NameOrUniqueName)))
                monitor.Log($"Rain watering: {location.NameOrUniqueName}, day {Day}, through {ToClock(through)}.", LogLevel.Trace);
            return true;
        });
    }

    public void AdjustNewSoil(GameLocation location, Vector2 tile)
    {
        if (!Enabled || !location.IsOutdoors || location.IsGreenhouse || Protection(Day, RegionFor(location)) != null) return;
        if (location.terrainFeatures.TryGetValue(tile, out var feature) && feature is HoeDirt soil)
        {
            soil.state.Value = Rainfall(Day, Minute, RegionFor(location)) >= climate.CropWateringThreshold ? 1 : 0;
            soil.updateNeighbors();
        }
    }

    public bool AllowLightning(int time)
    {
        if (!Enabled || Protection(Ending ? endingDay : Day, Region.Farm) != null) return true;
        if (Game1.eventUp) return false;
        return Sample(Ending ? endingDay : Day, ToMinutes(time), Region.Farm).Kind == WeatherKind.Storm;
    }

    public void Force(Region region, WeatherKind? kind)
    {
        if (kind.HasValue) forced[region] = kind.Value; else forced.Remove(region);
        forecasts.Clear(); lastVisual = null; Refresh();
    }

    public void DisableForSession(Exception exception)
    {
        faulted = true;
        foreach (var (context, original) in originals)
            Game1.netWorldState.Value.GetWeatherForLocation(context).CopyFrom(original);
        if (originals.TryGetValue("Default", out var weather))
        {
            Game1.isRaining = weather.IsRaining; Game1.isLightning = weather.IsLightning;
            Game1.isSnowing = weather.IsSnowing; Game1.isDebrisWeather = weather.IsDebrisWeather;
            Game1.isGreenRain = weather.IsGreenRain; Game1.weatherForTomorrow = weather.WeatherForTomorrow;
        }
        monitor.Log($"Weather disabled for this session; original weather restored. {exception}", LogLevel.Error);
    }
}
