using System.Reflection;
using HarmonyLib;
using SolaceWeather.Festivals;
using SolaceWeather.Presentation;
using SolaceWeather.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Network;
using StardewValley.Objects;

namespace SolaceWeather.Integration;

/// <summary>All game-method patches are installed atomically and restricted to the tested game version.</summary>
internal static class GamePatches
{
    private static WeatherRuntime runtime = null!;
    private static WeatherUi ui = null!;
    private static EggFestivalController festival = null!;

    public static bool Install(string id, WeatherRuntime service, WeatherUi presentation, EggFestivalController events, IMonitor monitor)
    {
        runtime = service; ui = presentation; festival = events;
        if (Game1.version != "1.6.15")
        {
            monitor.Log($"This build targets Stardew 1.6.15; found {Game1.version}. Weather remains disabled.", LogLevel.Error);
            return false;
        }
        var harmony = new Harmony(id);
        try
        {
            Patch(typeof(GameLocation), nameof(GameLocation.GetWeather), Type.EmptyTypes, nameof(GetWeatherPrefix));
            Patch(typeof(GameLocation), nameof(GameLocation.makeHoeDirt), new[] { typeof(Microsoft.Xna.Framework.Vector2), typeof(bool) }, postfix: nameof(TilledPostfix));
            Patch(typeof(Game1), nameof(Game1.UpdateWeatherForNewDay), Type.EmptyTypes, nameof(BeforeNewDay), nameof(AfterNewDay));
            Patch(typeof(NPC), nameof(NPC.TryLoadSchedule), Type.EmptyTypes, nameof(SchedulePrefix), finalizer: nameof(ScheduleFinalizer));
            Patch(typeof(Utility), nameof(Utility.performLightningUpdate), new[] { typeof(int) }, nameof(LightningPrefix));
            Patch(typeof(TV), "getWeatherForecast", Type.EmptyTypes, postfix: nameof(TvPostfix));
            Patch(typeof(TV), "getIslandWeatherForecast", Type.EmptyTypes, nameof(IslandTvPrefix));
            Patch(typeof(TV), "setWeatherOverlay", new[] { typeof(string) }, nameof(TvOverlayPrefix));
            Patch(typeof(Event), "TryGetFestivalDialogueForYear", new[] { typeof(NPC), typeof(string), typeof(Dialogue).MakeByRefType() }, postfix: nameof(FestivalDialoguePostfix));
            monitor.Log("Installed 9 version-checked weather integration patches.", LogLevel.Info);
            return true;
        }
        catch (Exception ex)
        {
            harmony.UnpatchAll(id);
            monitor.Log($"Weather integration could not be installed; no patches left active. {ex}", LogLevel.Error);
            return false;
        }

        void Patch(Type type, string name, Type[] arguments, string? prefix = null, string? postfix = null, string? finalizer = null)
        {
            MethodInfo method = AccessTools.Method(type, name, arguments) ?? throw new MissingMethodException(type.FullName, name);
            HarmonyMethod? H(string? patch) => patch == null ? null : new HarmonyMethod(typeof(GamePatches), patch);
            harmony.Patch(method, H(prefix), H(postfix), finalizer: H(finalizer));
        }
    }

    private static bool GetWeatherPrefix(GameLocation __instance, ref LocationWeather __result)
    {
        try
        {
            if (runtime.TryLocal(__instance, out var local)) { __result = local; return false; }
        }
        catch (Exception ex) { runtime.DisableForSession(ex); }
        return true;
    }
    private static void BeforeNewDay() => Guard(runtime.BeforeNewDayWeather);
    private static void TilledPostfix(GameLocation __instance, Microsoft.Xna.Framework.Vector2 tileLocation, bool __result)
    {
        if (__result) Guard(() => runtime.AdjustNewSoil(__instance, tileLocation));
    }
    private static void AfterNewDay() => Guard(runtime.AfterNewDayWeather);
    private static void SchedulePrefix(NPC __instance, out bool? __state)
    {
        __state = null;
        if (!runtime.Enabled) return;
        try
        {
            __state = Game1.isRaining;
            runtime.ScheduleDepth++;
            if (runtime.Protection(runtime.Day, WeatherRuntime.RegionFor(__instance.currentLocation)) == null)
                Game1.isRaining = WeatherRuntime.IsRain(runtime.GetForecast(WeatherRuntime.RegionFor(__instance.currentLocation), 0).MainKind);
        }
        catch (Exception ex) { runtime.DisableForSession(ex); }
    }
    private static Exception? ScheduleFinalizer(Exception? __exception, bool? __state)
    {
        if (__state.HasValue) { Game1.isRaining = __state.Value; runtime.ScheduleDepth = Math.Max(0, runtime.ScheduleDepth - 1); }
        return __exception;
    }
    private static bool LightningPrefix(int time_of_day)
    {
        try { return runtime.AllowLightning(time_of_day); }
        catch (Exception ex) { runtime.DisableForSession(ex); return true; }
    }
    private static void TvPostfix(ref string __result)
    {
        if (!runtime.Enabled) return;
        try
        {
            string regional = ui.FormatTomorrowReport();
            __result = runtime.Protection(runtime.Day + 1, Region.Farm) != null ? __result + "^^" + regional : regional;
        }
        catch (Exception ex) { runtime.DisableForSession(ex); }
    }
    private static bool IslandTvPrefix(ref string __result)
    {
        if (!runtime.Enabled) return true;
        try { __result = ui.FormatTomorrowReport(Region.Island); return false; }
        catch (Exception ex) { runtime.DisableForSession(ex); return true; }
    }
    private static void TvOverlayPrefix(ref string weatherId)
    {
        try
        {
            if (runtime.Enabled && weatherId == "Festival" && runtime.Protection(runtime.Day + 1, Region.Farm) == null)
                weatherId = WeatherRuntime.WeatherId(runtime.GetForecast(Region.Farm, 1).MainKind);
        }
        catch (Exception ex) { runtime.DisableForSession(ex); }
    }
    private static void FestivalDialoguePostfix(Event __instance, NPC npc, string key, Dialogue dialogue, bool __result)
    {
        if (__result) Guard(() => festival.AppendDialogue(__instance, npc, key, dialogue));
    }
    private static void Guard(Action action)
    {
        try { action(); } catch (Exception ex) { runtime.DisableForSession(ex); }
    }
}
