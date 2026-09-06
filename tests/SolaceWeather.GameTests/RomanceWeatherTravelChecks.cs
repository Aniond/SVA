using System.Reflection;
using SolaceWeather.Core;
using StardewModdingAPI;
using StardewValley;

namespace SolaceWeather.GameTests;

public sealed partial class ModEntry
{
    private void RomanceWeatherTravelChecks()
    {
        RequireWorld();
        var originalDrops = Game1.rainDrops;
        var results = new List<object>();
        void Check(string name, Action test)
        {
            bool passed = false;
            string? error = null;
            try { test(); passed = true; }
            catch (Exception ex) { error = ex.GetBaseException().Message; }
            results.Add(new { Name = name, Passed = passed, Error = error });
            Monitor.Log($"{(passed ? "PASS" : "FAIL")}: {name}", passed ? LogLevel.Info : LogLevel.Error);
        }
        try
        {
            Check("Refresh repairs a legacy twelve-drop buffer before native travel", () =>
            {
                Game1.rainDrops = new RainDrop[12];
                Call(Runtime, "Refresh");
                if (Game1.rainDrops.Length < 70) throw new InvalidOperationException("Native rain buffer is shorter than seventy drops.");
                Game1.randomizeRainPositions();
            });
            Check("Repeated refreshes retain native travel and sleep capacity", () =>
            {
                for (int i = 0; i < 30; i++)
                {
                    Call(Runtime, "Refresh");
                    if (Game1.rainDrops.Length < 70) throw new InvalidOperationException("Weather density shrank below the native loop length.");
                    Game1.randomizeRainPositions();
                }
            });
        }
        finally
        {
            // Never reintroduce a short buffer left by an older build into this session.
            if (originalDrops.Length >= 70) Game1.rainDrops = originalDrops;
            Helper.Data.WriteJsonFile("romance-weather-travel-results.json", results);
        }
    }
}
