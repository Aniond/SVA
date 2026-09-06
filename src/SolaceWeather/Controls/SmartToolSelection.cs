using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace SolaceWeather.Controls;

public enum ToolSelectionResult { Unchanged, Selected, MissingTool, AlreadyWatered }

/// <summary>Select a carried tool before the existing right-click tool pipeline runs.</summary>
public sealed class SmartToolSelection
{
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly ModConfig config;
    private readonly Func<int, int, bool> overWeatherButton;
    private readonly MovementControls? movement;

    public SmartToolSelection(IModHelper helper, IMonitor monitor, ModConfig config, Func<int, int, bool> overWeatherButton, MovementControls? movement = null)
    {
        this.helper = helper; this.monitor = monitor; this.config = config; this.overWeatherButton = overWeatherButton;
        this.movement = movement;
        helper.Events.Input.ButtonPressed += OnButton;
    }

    private void OnButton(object? sender, ButtonPressedEventArgs e)
    {
        if (e.Button != SButton.MouseRight || e.IsSuppressed() || !Context.IsWorldReady) return;
        int x = Game1.getMouseX(true), y = Game1.getMouseY(true);
        if (overWeatherButton(x, y) || Game1.onScreenMenus.Any(m => m.isWithinBounds(x, y))) return;
        try
        {
            Point tile = new((int)e.Cursor.Tile.X, (int)e.Cursor.Tile.Y);
            bool manual = helper.Input.IsDown(SButton.LeftShift) || helper.Input.IsDown(SButton.RightShift);
            if (TryWalkAndUse(tile, manual))
            {
                helper.Input.Suppress(SButton.MouseRight);
                return;
            }
            var result = SelectForTile(new Point((int)e.Cursor.Tile.X, (int)e.Cursor.Tile.Y),
                helper.Input.IsDown(SButton.LeftShift) || helper.Input.IsDown(SButton.RightShift));
            if (result is ToolSelectionResult.MissingTool or ToolSelectionResult.AlreadyWatered)
            {
                // Suppression persists through the hold, preventing a fallback swing.
                helper.Input.Suppress(SButton.MouseRight);
                if (result == ToolSelectionResult.MissingTool)
                    Game1.showRedMessage("The matching tool isn't in your backpack. Hold Shift to use your selected tool.");
            }
        }
        catch (Exception ex)
        {
            helper.Input.Suppress(SButton.MouseRight);
            monitor.Log($"Smart tool selection cancelled this click: {ex.Message}", LogLevel.Warn);
        }
    }

    /// <summary>Consume a distant farming-tool click and execute it once after approaching.</summary>
    public bool TryWalkAndUse(Point tile, bool manual)
    {
        if (movement == null || !config.EnableClickToMove || !config.RightClickUsesTool || !Context.IsPlayerFree
            || Context.IsMultiplayer || Game1.eventUp || Game1.player.UsingTool || Game1.fadeToBlack || Game1.isWarping
            || Game1.player.isRidingHorse() || Game1.player.IsSitting() || Game1.player.swimming.Value
            || (Game1.chatBox?.chatBox.Selected ?? false)
            || Game1.player.CurrentTool is not (Axe or Pickaxe or Hoe or WateringCan)
            || Utility.tileWithinRadiusOfPlayer(tile.X, tile.Y, 1, Game1.player)) return false;
        var player = Game1.player;
        var selected = player.CurrentItem;
        var location = player.currentLocation;
        Vector2 at = new(tile.X, tile.Y);
        location.objects.TryGetValue(at, out var originalObject);
        location.terrainFeatures.TryGetValue(at, out var originalTerrain);
        if (!movement.TryApproachTool(tile, () =>
        {
            location.objects.TryGetValue(at, out var currentObject);
            location.terrainFeatures.TryGetValue(at, out var currentTerrain);
            if (!ReferenceEquals(selected, player.CurrentItem) || !ReferenceEquals(originalObject, currentObject)
                || !ReferenceEquals(originalTerrain, currentTerrain)) return;
            var result = SelectForTile(tile, manual);
            if (result is ToolSelectionResult.MissingTool or ToolSelectionResult.AlreadyWatered)
            {
                if (result == ToolSelectionResult.MissingTool) Game1.showRedMessage("The matching tool isn't in your backpack.");
                return;
            }
            // Use the native press pipeline at the stored target, without moving the actual pointer.
            var previousMouse = Game1.oldMouseState;
            bool visible = Game1.wasMouseVisibleThisFrame, ui = Game1.uiMode;
            try
            {
                Game1.uiMode = false;
                Game1.wasMouseVisibleThisFrame = true;
                Game1.oldMouseState = new Microsoft.Xna.Framework.Input.MouseState(
                    (int)((tile.X * 64 + 32 - Game1.viewport.X) * Game1.options.zoomLevel),
                    (int)((tile.Y * 64 + 32 - Game1.viewport.Y) * Game1.options.zoomLevel), 0,
                    Microsoft.Xna.Framework.Input.ButtonState.Released, Microsoft.Xna.Framework.Input.ButtonState.Released,
                    Microsoft.Xna.Framework.Input.ButtonState.Released, Microsoft.Xna.Framework.Input.ButtonState.Released,
                    Microsoft.Xna.Framework.Input.ButtonState.Released);
                Game1.pressUseToolButton();
            }
            finally { Game1.oldMouseState = previousMouse; Game1.wasMouseVisibleThisFrame = visible; Game1.uiMode = ui; }
        })) Game1.showRedMessage("Can't reach that spot to use a tool.");
        return true;
    }

    public ToolSelectionResult SelectForTile(Point tile, bool preserveManualTool)
    {
        if (preserveManualTool || !config.EnableSmartToolSelection || !config.RightClickUsesTool
            || !Context.IsPlayerFree || Context.IsMultiplayer || Game1.eventUp || Game1.currentMinigame != null
            || Game1.player.UsingTool || !Game1.player.CanMove || Game1.fadeToBlack || Game1.isWarping
            || Game1.player.isRidingHorse() || Game1.player.IsSitting() || Game1.player.swimming.Value
            || (Game1.chatBox?.chatBox.Selected ?? false)) return ToolSelectionResult.Unchanged;
        Farmer player = Game1.player;
        // An explicitly selected seed, fertilizer, food, or placeable item keeps its action.
        if (player.CurrentItem != null && player.CurrentItem is not Tool) return ToolSelectionResult.Unchanged;
        if (!Utility.tileWithinRadiusOfPlayer(tile.X, tile.Y, 1, player)) return ToolSelectionResult.Unchanged;
        GameLocation location = player.currentLocation;
        Vector2 at = new(tile.X, tile.Y);
        Type? needed = null;
        if (location.objects.TryGetValue(at, out var placed))
        {
            // A tapper/chest on a tree tile takes precedence over the tree beneath it.
            if (placed.IsBreakableStone()) needed = typeof(Pickaxe);
            else if (placed.IsTwig()) needed = typeof(Axe);
        }
        else if (location.terrainFeatures.TryGetValue(at, out var terrain))
        {
            if (terrain is Tree or FruitTree) needed = typeof(Axe);
            else if (terrain is HoeDirt soil)
            {
                if (soil.state.Value == 1) return ToolSelectionResult.AlreadyWatered;
                if (soil.state.Value == 0) needed = typeof(WateringCan);
            }
        }
        if (needed == null) return ToolSelectionResult.Unchanged;
        if (player.CurrentTool != null && needed.IsInstanceOfType(player.CurrentTool)) return ToolSelectionResult.Selected;
        for (int slot = 0; slot < player.Items.Count; slot++)
        {
            if (player.Items[slot] is not Tool candidate || !needed.IsInstanceOfType(candidate)) continue;
            player.CurrentToolIndex = slot;
            return ToolSelectionResult.Selected;
        }
        return ToolSelectionResult.MissingTool;
    }
}
