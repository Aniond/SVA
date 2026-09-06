using System.Collections;
using System.Reflection;
using Microsoft.Xna.Framework;
using SolaceWeather.Controls;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace SolaceWeather.GameTests;

public sealed partial class ModEntry
{
    private void CropProtectionChecks()
    {
        var location = Game1.currentLocation;
        var tile = new Vector2(Game1.player.TilePoint.X + 2, Game1.player.TilePoint.Y);
        location.terrainFeatures.TryGetValue(tile, out var original);
        var states = (IDictionary)Game1.input.GetType().GetProperty("ButtonStates")!.GetValue(Game1.input)!;
        var enumType = states.GetType().GetGenericArguments()[1];
        object? oldLeft = states[SButton.LeftShift], oldRight = states[SButton.RightShift];
        var config = (SolaceWeather.ModConfig)typeof(CropProtection).GetField("config", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        bool enabled = config.EnableCropProtection;
        var results = new List<object>();
        void Check(string name, Func<bool> test)
        {
            bool passed = false; string? error = null;
            try { passed = test(); } catch (Exception ex) { error = ex.GetBaseException().Message; }
            results.Add(new { Name = name, Passed = passed, Error = error });
            Monitor.Log($"{(passed ? "PASS" : "FAIL")}: {name}", passed ? LogLevel.Info : LogLevel.Error);
        }
        HoeDirt Plant()
        {
            var soil = new HoeDirt(0, new Crop("472", (int)tile.X, (int)tile.Y, location));
            location.terrainFeatures[tile] = soil;
            return soil;
        }
        try
        {
            states.Remove(SButton.LeftShift); states.Remove(SButton.RightShift); config.EnableCropProtection = true;
            var soil = Plant(); var crop = soil.crop;
            Check("Native axe impact preserves living crop", () => !soil.performToolAction(new Axe(), 0, tile) && soil.crop == crop);
            Check("Native pickaxe impact preserves living crop and soil", () => !soil.performToolAction(new Pickaxe(), 0, tile) && soil.crop == crop);
            Check("Watering still changes soil state", () => { soil.performToolAction(new WateringCan(), 0, tile); return soil.state.Value == 1 && soil.crop == crop; });
            Check("Scythe remains eligible for normal harvest", () => !CropProtection.Protects(soil, ItemRegistry.Create<Tool>("(W)47"), false));
            Check("Hoe remains eligible for normal game actions", () => !CropProtection.Protects(soil, new Hoe(), false));
            Check("Shift permits deliberate native crop removal", () =>
            {
                states[SButton.LeftShift] = Enum.Parse(enumType, "Held");
                soil.performToolAction(new Pickaxe(), 0, tile);
                states.Remove(SButton.LeftShift);
                return soil.crop == null;
            });
            soil = Plant(); soil.crop.dead.Value = true;
            Check("Dead crop can still be cleared", () => { soil.performToolAction(new Axe(), 0, tile); return soil.crop == null; });
            soil = Plant(); soil.crop = null;
            Check("Empty tilled soil can still be removed", () => soil.performToolAction(new Pickaxe(), 0, tile));
            soil = Plant();
            Check("Environmental damage is unchanged", () => { soil.performToolAction(null!, 1, tile); return soil.crop == null; });
            soil = Plant(); config.EnableCropProtection = false;
            Check("Disabling protection restores native destruction", () => { soil.performToolAction(new Axe(), 0, tile); return soil.crop == null; });
        }
        finally
        {
            config.EnableCropProtection = enabled;
            if (oldLeft == null) states.Remove(SButton.LeftShift); else states[SButton.LeftShift] = oldLeft;
            if (oldRight == null) states.Remove(SButton.RightShift); else states[SButton.RightShift] = oldRight;
            if (original == null) location.terrainFeatures.Remove(tile); else location.terrainFeatures[tile] = original;
            Helper.Data.WriteJsonFile("crop-protection-results.json", results);
        }
    }
}
