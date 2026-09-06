using Microsoft.Xna.Framework;
using SolaceWeather.Controls;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace SolaceWeather.GameTests;

public sealed partial class ModEntry
{
    private void NearbyCraftingChecks()
    {
        var player = Game1.player;
        var inventory = player.Items.ToArray(); int selected = player.CurrentToolIndex;
        var location = Game1.currentLocation; var menu = Game1.activeClickableMenu;
        var recipes = player.craftingRecipes.Pairs.ToArray();
        var results = new List<object>();
        void Check(string name, Func<bool> test)
        {
            bool passed = false; string? error = null;
            try { passed = test(); } catch (Exception ex) { error = ex.GetBaseException().Message; }
            results.Add(new { Name = name, Passed = passed, Error = error });
            Monitor.Log($"{(passed ? "PASS" : "FAIL")}: {name}", passed ? LogLevel.Info : LogLevel.Error);
        }
        try
        {
            var testLocation = new GameLocation("Maps/FarmHouse", "NearbyCraftingTest");
            Game1.currentLocation = testLocation; player.currentLocation = testLocation;
            player.Items.Clear(); while (player.Items.Count < player.MaxItems) player.Items.Add(null);
            player.Items[0] = ItemRegistry.Create("(O)388", 20);
            player.Items[1] = ItemRegistry.Create("(O)388", 100); player.Items[1].modData[QuickStack.ProtectedKey] = "true";
            Chest Add(int dx, int count)
            {
                var chest = new Chest(true) { TileLocation = new Vector2(player.TilePoint.X + dx, player.TilePoint.Y) };
                chest.Items.Add(ItemRegistry.Create("(O)388", count)); testLocation.objects[chest.TileLocation] = chest; return chest;
            }
            var first = Add(2, 20); var second = Add(3, 20); var far = Add(8, 999);
            var protectedChest = Add(4, 100); protectedChest.Items[0].modData[QuickStack.ProtectedKey] = "true";
            var fridge = Add(5, 999); fridge.fridge.Value = true;
            var locked = Add(6, 999); locked.GetMutex().RequestLock(() => { });
            player.craftingRecipes["Chest"] = 0;
            var page = new CraftingPage(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height);
            Game1.activeClickableMenu = page;
            var pair = page.pagesOfCraftingRecipes.SelectMany(p => p).First(p => p.Value.name == "Chest");
            page.currentCraftingPage = page.pagesOfCraftingRecipes.FindIndex(p => p.ContainsKey(pair.Key));
            var recipe = pair.Value;
            IList<Item> Contents() => (IList<Item>)Call(page, "getContainerContents")!;
            void Craft() => Call(page, "clickCraftingRecipe", pair.Key, false);
            Check("Nearby chest inventories appear in the native crafting menu", () => Contents().Count == 3 && page._materialContainers.Count == 3);
#pragma warning disable CS0618 // Exercise the native methods used by the recipe tooltip.
            Check("Combined counts exclude protected, distant, fridge, and locked materials", () => recipe.getCraftableCount(Contents()) == 1
                && player.getItemCount("388") == 20 && player.getItemCountInList(Contents(), "388") == 40);
#pragma warning restore CS0618
            Check("Native craft uses backpack then nearby chests and produces output", () =>
            {
                Craft();
                return page.heldItem?.QualifiedItemId == "(BC)130" && player.Items[0] == null && first.Items.Count == 0
                    && second.Items[0].Stack == 10 && player.Items[1].Stack == 100 && protectedChest.Items[0].Stack == 100 && far.Items[0].Stack == 999;
            });
            page.heldItem = null;
            Check("Insufficient materials leave every source untouched", () => { Craft(); return page.heldItem == null && second.Items[0].Stack == 10 && player.Items[1].Stack == 100; });
            first.Items.Add(ItemRegistry.Create("(O)388", 50)); NearbyCrafting.Refresh(page, true);
            testLocation.objects.Remove(first.TileLocation);
            Check("A removed chest is rechecked before crafting", () => { Craft(); return page.heldItem == null && first.Items[0].Stack == 50 && second.Items[0].Stack == 10; });
            testLocation.objects[first.TileLocation] = first;
            for (int i = 0; i < player.Items.Count; i++) player.Items[i] = ItemRegistry.Create("(O)390", 999);
            Check("Full output space does not consume ingredients", () => { Craft(); return page.heldItem == null && first.Items[0].Stack == 50 && second.Items[0].Stack == 10; });
            player.Items[0] = null;
            page.heldItem = ItemRegistry.Create("(O)390", 999);
            Check("An incompatible cursor item does not consume ingredients", () => { Craft(); return page.heldItem.QualifiedItemId == "(O)390" && first.Items[0].Stack == 50; });
            page.heldItem = null;
            var cooking = new CraftingPage(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height, cooking: true);
            NearbyCrafting.Refresh(cooking, true);
            Check("Cooking does not gain nearby crafting storage", () => cooking._materialContainers == null);
            locked.GetMutex().ReleaseLock();
        }
        finally
        {
            Game1.activeClickableMenu = menu; Game1.currentLocation = location; player.currentLocation = location;
            player.Items.Clear(); foreach (var item in inventory) player.Items.Add(item); player.CurrentToolIndex = selected;
            player.craftingRecipes.Clear(); foreach (var pair in recipes) player.craftingRecipes[pair.Key] = pair.Value;
            Helper.Data.WriteJsonFile("nearby-crafting-results.json", results);
        }
    }
}
