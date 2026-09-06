using Microsoft.Xna.Framework;
using SolaceWeather.Controls;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace SolaceWeather.GameTests;

public sealed partial class ModEntry
{
    private void QuickStackChecks()
    {
        var player = Game1.player;
        var inventory = player.Items.ToArray(); int selected = player.CurrentToolIndex;
        var location = Game1.currentLocation;
        bool canMove = player.CanMove, usingTool = player.UsingTool, fade = Game1.fadeToBlack;
        var results = new List<object>();
        void Check(string name, Func<bool> test)
        {
            bool passed = false; string? error = null;
            try { passed = test(); } catch (Exception ex) { error = ex.GetBaseException().Message; }
            results.Add(new { Name = name, Passed = passed, Error = error });
            Monitor.Log($"{(passed ? "PASS" : "FAIL")}: {name}", passed ? LogLevel.Info : LogLevel.Error);
        }
        void Pack(params Item[] items) { player.Items.Clear(); player.Items.Add(new StardewValley.Tools.Axe()); foreach (var item in items) player.Items.Add(item); player.CurrentToolIndex = 0; }
        Chest ChestWith(string id, int amount) { var c = new Chest(true); c.Items.Add(ItemRegistry.Create(id, amount)); return c; }
        try
        {
            Pack(ItemRegistry.Create("(O)388", 20), ItemRegistry.Create("(O)390", 10));
            var chest = ChestWith("(O)388", 10);
            Check("Moves only existing matching items and conserves quantity", () => QuickStack.TransferMatching(player, chest) == 20
                && chest.Items[0].Stack == 30 && player.Items[1] == null && player.Items[2].Stack == 10 && player.Items[0] is Tool);
            Pack(ItemRegistry.Create("(O)388", 20)); player.CurrentToolIndex = 1;
            Check("Selected stack stays in backpack", () => QuickStack.TransferMatching(player, chest) == 0 && player.Items[1].Stack == 20);
            Pack(ItemRegistry.Create("(O)388", 20)); player.Items[1].modData[QuickStack.ProtectedKey] = "true";
            Check("Protected stack stays in backpack", () => QuickStack.TransferMatching(player, chest) == 0 && player.Items[1].Stack == 20);
            Pack(ItemRegistry.Create("(O)388", 20)); ((StardewValley.Object)player.Items[1]).questItem.Value = true;
            Check("Quest items stay in backpack", () => QuickStack.TransferMatching(player, chest) == 0);
            Pack(ItemRegistry.Create("(O)24", 4, quality: 2)); chest = ChestWith("(O)24", 3);
            Check("Different quality does not match", () => QuickStack.TransferMatching(player, chest) == 0);
            Pack(ItemRegistry.Create("(O)388", 5)); chest = ChestWith("(O)388", 998);
            while (chest.Items.Count < chest.GetActualCapacity()) chest.Items.Add(ItemRegistry.Create("(O)390", 999));
            Check("Full chest keeps overflow without losing or duplicating items", () => QuickStack.TransferMatching(player, chest) == 1
                && chest.Items[0].Stack == 999 && player.Items[1].Stack == 4);
            Check("Completely full chest leaves backpack untouched", () => QuickStack.TransferMatching(player, chest) == 0 && player.Items[1].Stack == 4);
            chest = ChestWith("(O)388", 5); chest.SpecialChestType = Chest.SpecialChestTypes.MiniShippingBin;
            Check("Shipping bins are excluded", () => QuickStack.TransferMatching(player, chest) == 0);
            chest.SpecialChestType = Chest.SpecialChestTypes.None; chest.fridge.Value = true;
            Check("Fridges are excluded", () => QuickStack.TransferMatching(player, chest) == 0);
            chest.fridge.Value = false; chest.GetMutex().RequestLock(() => { });
            Check("Locked chests are skipped", () => QuickStack.TransferMatching(player, chest) == 0);
            chest.GetMutex().ReleaseLock();
            Pack(ItemRegistry.Create("(O)388", 20));
            var testLocation = new GameLocation("Maps/FarmHouse", "QuickStackTest");
            Game1.currentLocation = testLocation; player.currentLocation = testLocation;
            player.CanMove = true; player.UsingTool = false; Game1.fadeToBlack = false;
            var near = ChestWith("(O)388", 10); near.TileLocation = new Vector2(player.TilePoint.X + 2, player.TilePoint.Y);
            var far = ChestWith("(O)388", 10); far.TileLocation = new Vector2(player.TilePoint.X + 8, player.TilePoint.Y);
            testLocation.objects[near.TileLocation] = near; testLocation.objects[far.TileLocation] = far;
            var config = new SolaceWeather.ModConfig();
            var service = new QuickStack(Helper, Monitor, config);
            Check("Quick-stack finds nearby chests and excludes distant ones", () =>
            {
                var result = service.Run();
                return result.ItemsMoved == 20 && result.ChestsUsed == 1 && near.Items[0].Stack == 30 && far.Items[0].Stack == 10;
            });
            config.EnableQuickStack = false;
            Check("Disabled quick-stack does nothing", () => service.Run().ItemsMoved == 0);
        }
        finally
        {
            Game1.currentLocation = location; player.currentLocation = location;
            player.Items.Clear(); foreach (var item in inventory) player.Items.Add(item);
            player.CurrentToolIndex = selected; player.CanMove = canMove; player.UsingTool = usingTool; Game1.fadeToBlack = fade;
            Helper.Data.WriteJsonFile("quick-stack-results.json", results);
        }
    }
}
