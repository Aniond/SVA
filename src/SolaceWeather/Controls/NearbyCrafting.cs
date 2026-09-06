using System.Runtime.CompilerServices;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Menus;
using StardewValley.Objects;

namespace SolaceWeather.Controls;

/// <summary>Attach nearby ordinary storage to the native crafting menu.</summary>
public static class NearbyCrafting
{
    private sealed record Sources(GameLocation Location, Point Tile, Chest[] Chests, List<IInventory>? Original);
    private static readonly ConditionalWeakTable<CraftingPage, Sources> cache = new();
    private static ModConfig config = null!;
    private static bool checking;
    private static bool Enabled => config?.EnableNearbyCrafting == true && Context.IsWorldReady && !Context.IsMultiplayer;
    private static CraftingPage? ActivePage => Game1.activeClickableMenu is CraftingPage page ? page
        : (Game1.activeClickableMenu as GameMenu)?.GetCurrentPage() as CraftingPage;
    private static bool ProtectItems => Enabled && (checking || ActivePage is { cooking: false });

    public static void Install(string id, ModConfig settings, IMonitor monitor)
    {
        config = settings;
        if (Game1.version != "1.6.15" || Constants.ApiVersion.ToString() != "4.5.2")
        { monitor.Log("Nearby crafting requires Stardew 1.6.15 / SMAPI 4.5.2.", LogLevel.Warn); return; }
        var harmony = new Harmony(id + ".NearbyCrafting");
        try
        {
            harmony.Patch(AccessTools.Method(typeof(CraftingPage), "getContainerContents"), prefix: new HarmonyMethod(typeof(NearbyCrafting), nameof(BeforeContents)));
            harmony.Patch(AccessTools.Method(typeof(CraftingPage), "clickCraftingRecipe"), prefix: new HarmonyMethod(typeof(NearbyCrafting), nameof(BeforeCraft)));
            harmony.Patch(AccessTools.Method(typeof(CraftingRecipe), nameof(CraftingRecipe.ItemMatchesForCrafting)), postfix: new HarmonyMethod(typeof(NearbyCrafting), nameof(FilterProtected)));
            harmony.Patch(AccessTools.Method(typeof(CraftingRecipe), nameof(CraftingRecipe.getCraftableCount), new[] { typeof(IList<Item>) }), prefix: new HarmonyMethod(typeof(NearbyCrafting), nameof(CraftableCount)));
            harmony.Patch(AccessTools.Method(typeof(CraftingRecipe), nameof(CraftingRecipe.drawRecipeDescription)),
                prefix: new HarmonyMethod(typeof(NearbyCrafting), nameof(ShowCounts)), finalizer: new HarmonyMethod(typeof(NearbyCrafting), nameof(RestoreCounts)));
            monitor.Log("Nearby crafting enabled for ordinary chests within six tiles.", LogLevel.Info);
        }
        catch (Exception ex) { harmony.UnpatchAll(harmony.Id); monitor.Log($"Nearby crafting could not load: {ex.Message}", LogLevel.Error); }
    }

    private static bool Eligible(Chest chest) => chest.playerChest.Value && !chest.giftbox.Value && !chest.fridge.Value
        && chest.SpecialChestType is Chest.SpecialChestTypes.None or Chest.SpecialChestTypes.BigChest
        && string.IsNullOrEmpty(chest.GlobalInventoryId) && !chest.GetMutex().IsLocked();

    public static void Refresh(CraftingPage page, bool rescan = false)
    {
        if (page.cooking) return;
        if (!Enabled)
        {
            if (cache.TryGetValue(page, out var old)) { page._materialContainers = old.Original; cache.Remove(page); }
            return;
        }
        var location = Game1.currentLocation; var tile = Game1.player.TilePoint;
        cache.TryGetValue(page, out var sources);
        if (rescan || sources == null || sources.Location != location || sources.Tile != tile)
        {
            var chests = location.objects.Values.OfType<Chest>().Where(c => Eligible(c)
                && Math.Abs(c.TileLocation.X - tile.X) <= 6 && Math.Abs(c.TileLocation.Y - tile.Y) <= 6)
                .OrderBy(c => Math.Abs(c.TileLocation.X - tile.X) + Math.Abs(c.TileLocation.Y - tile.Y))
                .ThenBy(c => c.TileLocation.Y).ThenBy(c => c.TileLocation.X).ToArray();
            var original = sources == null ? page._materialContainers : sources.Original;
            sources = new(location, tile, chests, original);
            cache.Remove(page); cache.Add(page, sources);
        }
        page._materialContainers = sources.Chests.Where(c => Eligible(c)
            && location.objects.TryGetValue(c.TileLocation, out var current) && ReferenceEquals(c, current)
            && Math.Abs(c.TileLocation.X - tile.X) <= 6 && Math.Abs(c.TileLocation.Y - tile.Y) <= 6)
            .Select(c => c.GetItemsForPlayer()).ToList();
    }

    private static void BeforeContents(CraftingPage __instance) => Refresh(__instance);

    private static void ShowCounts(CraftingRecipe __instance, out bool? __state)
    {
        __state = null;
        if (!ProtectItems || __instance.isCookingRecipe) return;
        __state = Game1.options.showAdvancedCraftingInformation;
        Game1.options.showAdvancedCraftingInformation = true;
    }

    private static void RestoreCounts(bool? __state)
    {
        if (__state.HasValue) Game1.options.showAdvancedCraftingInformation = __state.Value;
    }

    private static void FilterProtected(Item item, ref bool __result)
    {
        if (__result && ProtectItems && (item.modData.ContainsKey(QuickStack.ProtectedKey)
            || item is StardewValley.Object { questItem.Value: true })) __result = false;
    }

    private static bool CraftableCount(CraftingRecipe __instance, IList<Item> additional_materials, ref int __result)
    {
        if (!ProtectItems || __instance.isCookingRecipe) return true;
        var items = Game1.player.Items.Concat(additional_materials ?? Array.Empty<Item>()).Where(i => i != null).ToArray();
        __result = __instance.recipeList.Count == 0 ? 0 : __instance.recipeList.Min(pair =>
            items.Where(i => CraftingRecipe.ItemMatchesForCrafting(i, pair.Key)).Sum(i => i.Stack) / Math.Max(1, pair.Value));
        return false;
    }

    private static bool BeforeCraft(CraftingPage __instance, ClickableTextureComponent c)
    {
        if (!Enabled || __instance.cooking) return true;
        Refresh(__instance, rescan: true);
        var recipe = __instance.pagesOfCraftingRecipes[__instance.currentCraftingPage][c];
        bool previous = checking; checking = true;
        try
        {
            var materials = __instance._materialContainers.SelectMany(i => i).ToList();
            if (!recipe.doesFarmerHaveIngredientsInInventory(materials)) return false;
            // Allocate each unit once, including recipes with overlapping category rules.
            var available = Game1.player.Items.Reverse().Concat(__instance._materialContainers.SelectMany(i => i.Reverse()))
                .Where(i => i != null).ToArray();
            var remaining = available.Select(i => i.Stack).ToArray();
            foreach (var ingredient in recipe.recipeList)
            {
                int needed = ingredient.Value;
                for (int i = 0; i < available.Length && needed > 0; i++)
                {
                    if (!CraftingRecipe.ItemMatchesForCrafting(available[i], ingredient.Key)) continue;
                    int take = Math.Min(needed, remaining[i]); needed -= take; remaining[i] -= take;
                }
                if (needed > 0) return false;
            }
            // Native crafting holds its output on the cursor. Require a destination
            // before consuming anything, even if materials would free a backpack slot.
            foreach (string id in recipe.itemToProduce)
            {
                var output = ItemRegistry.Create(recipe.bigCraftable ? ItemRegistry.ManuallyQualifyItemId(id, "(BC)") : id, recipe.numberProducedPerCraft);
                if (__instance.heldItem == null)
                {
                    if (!Game1.player.couldInventoryAcceptThisItem(output))
                    { Game1.showRedMessage("Make room in your backpack before crafting."); return false; }
                }
                else if (!__instance.heldItem.canStackWith(output) || __instance.heldItem.Stack + output.Stack > __instance.heldItem.maximumStackSize()) return false;
            }
            return true;
        }
        finally { checking = previous; }
    }
}
