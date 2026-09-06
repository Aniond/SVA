using System.Reflection;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace SolaceWeather.GameTests;

public sealed partial class ModEntry
{
    private void SmartToolChecks()
    {
        object info = Helper.ModRegistry.Get("David.SolaceWeather")!;
        object mod = info.GetType().GetProperty("Mod", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(info)!;
        object? smart = typeof(SolaceWeather.ModEntry).GetField("smartTools", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(mod);
        var results = new List<object>();
        void Check(string name, Func<bool> test)
        {
            bool passed = false; string? error = null;
            try { passed = test(); } catch (Exception ex) { error = ex.GetBaseException().Message; }
            results.Add(new { Name = name, Passed = passed, Error = error });
            Monitor.Log($"{(passed ? "PASS" : "FAIL")}: {name}", passed ? LogLevel.Info : LogLevel.Error);
        }
        Check("Smart tool selection is installed", () => smart != null);
        if (smart == null) { Helper.Data.WriteJsonFile("smart-tool-results.json", new { Checks = results, Restored = true }); return; }
        Farmer player = Game1.player;
        GameLocation location = player.currentLocation;
        Item[] inventory = player.Items.ToArray();
        int selected = player.CurrentToolIndex;
        bool canMove = player.CanMove, usingTool = player.UsingTool, fade = Game1.fadeToBlack;
        var menu = Game1.activeClickableMenu;
        Point tile = new(player.TilePoint.X - 1, player.TilePoint.Y);
        Vector2 at = new(tile.X, tile.Y);
        location.objects.TryGetValue(at, out var originalObject);
        location.terrainFeatures.TryGetValue(at, out var originalTerrain);
        string Select(bool manual = false) => Call(smart, "SelectForTile", tile, manual)!.ToString()!;
        try
        {
            player.CurrentToolIndex = 0; player.Items.Clear();
            player.Items.Add(new Axe()); player.Items.Add(new Pickaxe()); player.Items.Add(new WateringCan());
            player.CanMove = true; player.UsingTool = false; Game1.fadeToBlack = false; Game1.activeClickableMenu = null;
            location.objects.Remove(at); location.terrainFeatures.Remove(at);
            location.objects[at] = ItemRegistry.Create<StardewValley.Object>("(O)343");
            Check("Rock chooses carried pickaxe", () => Select() == "Selected" && player.CurrentTool is Pickaxe);
            Check("Already correct tool stays selected", () => Select() == "Selected" && player.CurrentToolIndex == 1);
            player.CurrentToolIndex = 0;
            Check("Shift preserves the manually selected tool", () => Select(true) == "Unchanged" && player.CurrentTool is Axe);
            Check("Distant targets do not switch tools", () => Call(smart, "SelectForTile", new Point(tile.X + 10, tile.Y), false)!.ToString() == "Unchanged" && player.CurrentTool is Axe);
            location.objects.Remove(at); location.terrainFeatures[at] = new Tree("1", 5);
            player.CurrentToolIndex = 1;
            Check("Tree chooses carried axe", () => Select() == "Selected" && player.CurrentTool is Axe);
            location.terrainFeatures[at] = new HoeDirt(0, location);
            Check("Dry soil chooses carried watering can", () => Select() == "Selected" && player.CurrentTool is WateringCan);
            location.terrainFeatures[at] = new HoeDirt(1, location);
            player.CurrentToolIndex = 1;
            Check("Watered soil blocks an accidental tool swing", () => Select() == "AlreadyWatered" && player.CurrentTool is Pickaxe);
            location.terrainFeatures[at] = new HoeDirt(0, location);
            player.Items[2] = null;
            Check("Missing watering can does not substitute a damaging tool", () => Select() == "MissingTool" && player.CurrentTool is Pickaxe);
            player.Items.Add(ItemRegistry.Create("(O)472")); player.CurrentToolIndex = 3;
            Check("Selected seeds keep normal planting behavior", () => Select() == "Unchanged" && player.CurrentItem?.QualifiedItemId == "(O)472");
            player.CurrentToolIndex = 1; Game1.activeClickableMenu = new GameMenu();
            Check("Menus do not switch tools", () => Select() == "Unchanged" && player.CurrentTool is Pickaxe);
            Game1.activeClickableMenu = null; player.UsingTool = true;
            Check("An active tool animation is not interrupted", () => Select() == "Unchanged" && player.CurrentTool is Pickaxe);
            player.UsingTool = false; location.terrainFeatures.Remove(at);
            Check("Unrecognized ground keeps current tool", () => Select() == "Unchanged" && player.CurrentTool is Pickaxe);
        }
        finally
        {
            player.UsingTool = false; player.Items.Clear(); foreach (Item item in inventory) player.Items.Add(item);
            player.CurrentToolIndex = selected; player.CanMove = canMove; player.UsingTool = usingTool;
            Game1.activeClickableMenu = menu; Game1.fadeToBlack = fade;
            if (originalObject == null) location.objects.Remove(at); else location.objects[at] = originalObject;
            if (originalTerrain == null) location.terrainFeatures.Remove(at); else location.terrainFeatures[at] = originalTerrain;
            Helper.Data.WriteJsonFile("smart-tool-results.json", new { Checks = results, Restored = true });
        }
    }
}
