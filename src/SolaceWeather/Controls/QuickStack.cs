using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;

namespace SolaceWeather.Controls;

public sealed record QuickStackResult(int ItemsMoved, int ChestsUsed);

/// <summary>Single-player transfers using native stacking and capacity rules.</summary>
public sealed class QuickStack
{
    public const string ProtectedKey = "David.SolaceWeather/QuickStackProtected";
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly ModConfig config;

    public QuickStack(IModHelper helper, IMonitor monitor, ModConfig config)
    {
        this.helper = helper; this.monitor = monitor; this.config = config;
        helper.Events.Input.ButtonPressed += OnButton;
    }

    private static bool CanRun() => Context.IsPlayerFree && !Context.IsMultiplayer && !Game1.eventUp
        && Game1.activeClickableMenu == null && Game1.currentMinigame == null && !Game1.dialogueUp
        && !Game1.player.UsingTool && Game1.player.CanMove && !Game1.fadeToBlack && !Game1.isWarping
        && !Game1.player.IsSitting() && !(Game1.chatBox?.chatBox.Selected ?? false);

    private void OnButton(object? sender, ButtonPressedEventArgs e)
    {
        if (!config.EnableQuickStack || e.IsSuppressed() || !CanRun()) return;
        if (e.Button == config.ProtectItemKey)
        {
            helper.Input.Suppress(e.Button);
            var item = Game1.player.CurrentItem;
            if (item == null) { Game1.showGlobalMessage("Select an item to protect from quick-stack."); return; }
            bool protect = !item.modData.ContainsKey(ProtectedKey);
            if (protect) item.modData[ProtectedKey] = "true"; else item.modData.Remove(ProtectedKey);
            Game1.showGlobalMessage($"{item.DisplayName}: {(protect ? "protected from quick-stack" : "quick-stack protection removed")}.");
        }
        else if (e.Button == config.QuickStackKey)
        {
            helper.Input.Suppress(e.Button);
            try
            {
                var result = Run();
                Game1.showGlobalMessage(result.ItemsMoved > 0
                    ? $"Stored {result.ItemsMoved} items in {result.ChestsUsed} nearby chest(s)."
                    : "Nothing moved: no matching space in nearby chests, or items are protected.");
                if (result.ItemsMoved > 0) Game1.playSound("Ship");
            }
            catch (Exception ex) { monitor.Log($"Quick-stack stopped: {ex.Message}", LogLevel.Error); Game1.showRedMessage("Quick-stack stopped. Check your inventory."); }
        }
    }

    public QuickStackResult Run()
    {
        if (!config.EnableQuickStack || !CanRun()) return new(0, 0);
        Farmer player = Game1.player;
        int radius = Math.Clamp(config.QuickStackRadius, 1, 12);
        var chests = player.currentLocation.objects.Values.OfType<Chest>()
            .Where(chest => IsOrdinaryChest(chest) && Math.Abs(chest.TileLocation.X - player.TilePoint.X) <= radius
                && Math.Abs(chest.TileLocation.Y - player.TilePoint.Y) <= radius)
            .OrderBy(chest => Math.Abs(chest.TileLocation.X - player.TilePoint.X) + Math.Abs(chest.TileLocation.Y - player.TilePoint.Y))
            .ThenBy(chest => chest.TileLocation.Y).ThenBy(chest => chest.TileLocation.X).ToArray();
        int moved = 0, used = 0;
        foreach (var chest in chests)
        {
            int count = TransferMatching(player, chest);
            moved += count;
            if (count > 0) used++;
        }
        return new(moved, used);
    }

    private static bool IsOrdinaryChest(Chest chest) => chest.playerChest.Value && !chest.giftbox.Value && !chest.fridge.Value
        && chest.SpecialChestType is Chest.SpecialChestTypes.None or Chest.SpecialChestTypes.BigChest
        && string.IsNullOrEmpty(chest.GlobalInventoryId) && !chest.GetMutex().IsLocked();

    public static int TransferMatching(Farmer player, Chest chest)
    {
        if (!IsOrdinaryChest(chest)) return 0;
        int moved = 0;
        for (int slot = 0; slot < player.Items.Count; slot++)
        {
            var item = player.Items[slot];
            if (item == null || slot == player.CurrentToolIndex || item is Tool || item.maximumStackSize() <= 1
                || item.modData.ContainsKey(ProtectedKey) || item is StardewValley.Object { questItem.Value: true }) continue;
            if (!chest.GetItemsForPlayer().Any(existing => existing != null && existing.canStackWith(item))) continue;
            int before = item.Stack;
            // The native method keeps overflow in the returned item, including when a chest is full.
            var remainder = chest.addItem(item);
            player.Items[slot] = remainder;
            moved += before - (remainder?.Stack ?? 0);
        }
        return moved;
    }
}
