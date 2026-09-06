using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace SolaceWeather.Controls;

public sealed record ClickHint(Rectangle Bounds, string Left, string Right, string Status = "");

/// <summary>Read-only hover information. Never probes an action or changes inventory.</summary>
public sealed class ClickFeedback
{
    private readonly IModHelper helper;
    private readonly ModConfig config;
    private readonly MovementControls movement;
    private readonly Func<int, int, bool> overWeatherButton;

    public ClickFeedback(IModHelper helper, ModConfig config, MovementControls movement, Func<int, int, bool> overWeatherButton)
    {
        this.helper = helper; this.config = config; this.movement = movement; this.overWeatherButton = overWeatherButton;
        helper.Events.Display.RenderedHud += (_, e) => Draw(e.SpriteBatch);
    }

    public ClickHint? Describe(Point tile, bool manual = false)
    {
        if (!Context.IsWorldReady) return null;
        var location = Game1.currentLocation;
        if (tile.X < 0 || tile.Y < 0 || tile.X >= location.Map.Layers[0].LayerWidth || tile.Y >= location.Map.Layers[0].LayerHeight) return null;
        Vector2 at = new(tile.X, tile.Y);
        location.objects.TryGetValue(at, out var obj);
        if (obj == null && location.objects.TryGetValue(at + new Vector2(0, 1), out var below) && (below is Chest || below.bigCraftable.Value))
        { obj = below; tile.Y++; at.Y++; }
        var bounds = new Rectangle(tile.X * 64, tile.Y * 64, 64, 64);
        var actor = location.characters.FirstOrDefault(c => !c.IsMonster && c.GetBoundingBox().Intersects(bounds));
        var furniture = location.furniture.FirstOrDefault(f => f.boundingBox.Value.Intersects(bounds))
            ?? location.furniture.LastOrDefault(f => new Rectangle(f.boundingBox.X,
                f.boundingBox.Y - Math.Max(0, f.sourceRect.Height * 4 - f.boundingBox.Height), f.boundingBox.Width,
                Math.Max(f.boundingBox.Height, f.sourceRect.Height * 4)).Contains(tile.X * 64 + 32, tile.Y * 64 + 32));
        string left = "", right = "";
        if (actor != null) left = $"Interact: {actor.displayName}";
        else if (obj is Chest) left = "Open chest";
        else if (obj?.GetMachineData() != null)
            left = obj.readyForHarvest.Value ? "Collect output" : Game1.player.ActiveObject != null ? "Insert held item" : "Use machine";
        else if (location.doesTileHaveProperty(tile.X, tile.Y, "Action", "Buildings") is string action)
            left = action.Contains("Door") || action.StartsWith("Warp") ? "Use door" : "Interact";
        else if (furniture != null)
        {
            bounds = furniture.boundingBox.Value;
            left = furniture.GetSeatCapacity() > 0 ? "Sit" : furniture is TV ? "Watch TV"
                : furniture.furniture_type.Value is 14 or 16 ? "Toggle fireplace" : "Use furniture";
        }

        if (config.RightClickUsesTool)
        {
            var item = Game1.player.CurrentItem;
            if (item == null || item is Tool)
            {
                Type? needed = null;
                string action = "";
                if (!manual && config.EnableSmartToolSelection)
                {
                    if (obj?.IsBreakableStone() == true) { needed = typeof(Pickaxe); action = "Break rock"; }
                    else if (obj?.IsTwig() == true) { needed = typeof(Axe); action = "Chop"; }
                    else if (obj == null && location.terrainFeatures.TryGetValue(at, out var terrain))
                    {
                        if (terrain is Tree or FruitTree) { needed = typeof(Axe); action = "Chop"; }
                        else if (terrain is HoeDirt soil)
                        {
                            if (soil.state.Value == 1) action = "Already watered";
                            else if (soil.state.Value == 0) { needed = typeof(WateringCan); action = "Water"; }
                        }
                    }
                }
                if (needed != null && !Game1.player.Items.Any(i => i != null && needed.IsInstanceOfType(i))) right = "Matching tool missing";
                else if (action != "") right = action;
            }
        }
        if (!config.EnableClickToMove || !config.EnableClickToInteract) left = "";
        if (manual) left = "";
        if (config.EnableCropProtection && right == "" && Game1.player.CurrentTool is Tool selectedTool
            && location.terrainFeatures.TryGetValue(at, out var protectedTerrain) && protectedTerrain is HoeDirt plantedSoil
            && CropProtection.Protects(plantedSoil, selectedTool, manual)) right = "Crop protected (hold Shift to remove)";
        if (Game1.player.IsSitting() && config.EnableClickToMove && !manual && left != "")
        {
            left = Game1.player.sittingFurniture?.GetSeatBounds().Intersects(new Rectangle(bounds.X / 64, bounds.Y / 64,
                Math.Max(1, bounds.Width / 64), Math.Max(1, bounds.Height / 64))) == true ? "Stand up" : "Stand up and approach";
            right = "";
        }
        string status = config.EnableMachineIndicators && obj != null ? MachineIndicators.Describe(obj)?.Text ?? "" : "";
        return left == "" && right == "" && status == "" ? null : new ClickHint(bounds, left, right, status);
    }

    private void Draw(SpriteBatch batch)
    {
        if (!config.EnableClickFeedback || !Context.IsWorldReady || Context.IsMultiplayer || !Game1.displayHUD
            || Game1.activeClickableMenu != null || Game1.eventUp || Game1.currentMinigame != null || Game1.dialogueUp
            || Game1.fadeToBlack || Game1.isWarping || Game1.player.isRidingHorse()
            || Game1.player.UsingTool || !Game1.player.CanMove || Game1.player.swimming.Value
            || (Game1.chatBox?.chatBox.Selected ?? false)) return;
        float scale = Game1.options.zoomLevel / Game1.options.uiScale;
        Rectangle Screen(Rectangle world) => new((int)((world.X - Game1.viewport.X) * scale), (int)((world.Y - Game1.viewport.Y) * scale),
            Math.Max(1, (int)(world.Width * scale)), Math.Max(1, (int)(world.Height * scale)));
        if (movement.WalkingDestination is Point destination)
        {
            Rectangle marker = Screen(new Rectangle(destination.X * 64 + 16, destination.Y * 64 + 16, 32, 32));
            Outline(batch, marker, Color.DeepSkyBlue, 3);
        }
        int x = Game1.getMouseX(true), y = Game1.getMouseY(true);
        if (overWeatherButton(x, y) || Game1.onScreenMenus.Any(m => m.isWithinBounds(x, y))) return;
        var cursor = helper.Input.GetCursorPosition().Tile;
        var hint = Describe(new Point((int)cursor.X, (int)cursor.Y), helper.Input.IsDown(SButton.LeftShift) || helper.Input.IsDown(SButton.RightShift));
        if (hint == null) return;
        Outline(batch, Screen(hint.Bounds), hint.Left != "" ? Color.LightGreen : Color.Gold, 2);
        string text = string.Join("\n", new[] { hint.Status, hint.Left == "" ? "" : "Left: " + hint.Left, hint.Right == "" ? "" : "Right: " + hint.Right }.Where(s => s != ""));
        Vector2 size = Game1.smallFont.MeasureString(text);
        float textScale = Math.Min(1f, Math.Max(0.4f, (Game1.uiViewport.Width - 24) / Math.Max(1, size.X)));
        int width = (int)(size.X * textScale) + 16, height = (int)(size.Y * textScale) + 12;
        x = Math.Clamp(x + 24, 4, Math.Max(4, Game1.uiViewport.Width - width - 4));
        y = Math.Clamp(y + 24, 4, Math.Max(4, Game1.uiViewport.Height - height - 4));
        batch.Draw(Game1.staminaRect, new Rectangle(x, y, width, height), Color.Black * .85f);
        batch.DrawString(Game1.smallFont, text, new Vector2(x + 8, y + 6), Color.White, 0, Vector2.Zero, textScale, SpriteEffects.None, 1);
    }

    private static void Outline(SpriteBatch batch, Rectangle r, Color color, int thickness)
    {
        batch.Draw(Game1.staminaRect, new Rectangle(r.X, r.Y, r.Width, thickness), color);
        batch.Draw(Game1.staminaRect, new Rectangle(r.X, r.Bottom - thickness, r.Width, thickness), color);
        batch.Draw(Game1.staminaRect, new Rectangle(r.X, r.Y, thickness, r.Height), color);
        batch.Draw(Game1.staminaRect, new Rectangle(r.Right - thickness, r.Y, thickness, r.Height), color);
    }
}
