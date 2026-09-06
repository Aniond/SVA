using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace SolaceWeather.Controls;

/// <summary>Guard the actual crop impact, including keyboard and queued tool uses.</summary>
public static class CropProtection
{
    private static IModHelper helper = null!;
    private static ModConfig config = null!;
    private static long lastNotice;

    public static bool Protects(HoeDirt soil, Tool? tool, bool overrideHeld) => !overrideHeld
        && tool is Axe or Pickaxe && soil.crop != null && !soil.crop.dead.Value && !soil.crop.forageCrop.Value;

    public static void Install(string id, IModHelper modHelper, ModConfig settings, IMonitor monitor)
    {
        if (Game1.version != "1.6.15" || Constants.ApiVersion.ToString() != "4.5.2")
        { monitor.Log("Crop protection requires Stardew 1.6.15 / SMAPI 4.5.2 and was not installed.", LogLevel.Warn); return; }
        helper = modHelper; config = settings;
        try
        {
            var target = AccessTools.Method(typeof(HoeDirt), nameof(HoeDirt.performToolAction),
                new[] { typeof(Tool), typeof(int), typeof(Microsoft.Xna.Framework.Vector2) });
            new Harmony(id + ".CropProtection").Patch(target,
                prefix: new HarmonyMethod(typeof(CropProtection), nameof(BeforeImpact)));
            monitor.Log("Crop protection enabled for axe and pickaxe impacts. Hold Shift to override.", LogLevel.Info);
        }
        catch (Exception ex) { monitor.Log($"Crop protection could not load: {ex.Message}", LogLevel.Error); }
    }

    private static bool BeforeImpact(HoeDirt __instance, Tool t, ref bool __result)
    {
        if (!config.EnableCropProtection || !Context.IsWorldReady || Context.IsMultiplayer || Game1.eventUp
            || (t?.getLastFarmerToUse() is Farmer farmer && farmer != Game1.player)
            || !Protects(__instance, t, helper.Input.IsDown(SButton.LeftShift) || helper.Input.IsDown(SButton.RightShift))) return true;
        __result = false;
        if (Environment.TickCount64 - lastNotice >= 1500)
        {
            lastNotice = Environment.TickCount64;
            Game1.showGlobalMessage("Crop protected. Hold Shift through the tool swing to remove it.");
        }
        return false;
    }
}
