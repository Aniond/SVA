using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Machines;
using StardewValley.Objects;

namespace SolaceWeather.Controls;

public enum MachineState { Idle, Working, Ready }
public sealed record MachineHint(MachineState State, string Text);

/// <summary>Display native machine state without probing recipes or advancing production.</summary>
public sealed class MachineIndicators
{
    public MachineIndicators(IModHelper helper, ModConfig config)
    {
        helper.Events.Display.RenderedHud += (_, e) =>
        {
            if (!config.EnableMachineIndicators || !Context.IsWorldReady || Context.IsMultiplayer || !Game1.displayHUD
                || Game1.activeClickableMenu != null || Game1.dialogueUp || Game1.eventUp || Game1.currentMinigame != null
                || Game1.fadeToBlack || Game1.isWarping || (Game1.chatBox?.chatBox.Selected ?? false)) return;
            float scale = Game1.options.zoomLevel / Game1.options.uiScale;
            foreach (var obj in Game1.currentLocation.objects.Values)
            {
                int x = (int)((obj.TileLocation.X * 64 + 48 - Game1.viewport.X) * scale);
                int y = (int)((obj.TileLocation.Y * 64 - 48 - Game1.viewport.Y) * scale);
                if (x < 0 || y < 0 || x > Game1.uiViewport.Width - 20 || y > Game1.uiViewport.Height - 20) continue;
                var hint = Describe(obj);
                if (hint == null) continue;
                Color color = hint.State == MachineState.Ready ? Color.LightGreen : hint.State == MachineState.Working ? Color.Gold : Color.LightGray;
                string symbol = hint.State == MachineState.Ready ? "!" : hint.State == MachineState.Working ? "~" : "-";
                e.SpriteBatch.Draw(Game1.staminaRect, new Rectangle(x - 1, y - 1, 20, 20), Color.Black * .85f);
                e.SpriteBatch.DrawString(Game1.smallFont, symbol, new Vector2(x + 4, y), color, 0, Vector2.Zero, .75f, SpriteEffects.None, 1);
            }
        };
    }

    public static MachineHint? Describe(StardewValley.Object obj)
    {
        if (obj is Chest || obj.isTemporarilyInvisible) return null;
        var data = obj.GetMachineData();
        if (data == null) return null;
        if (obj.readyForHarvest.Value && obj.heldObject.Value != null)
            return new(MachineState.Ready, "Ready: " + obj.heldObject.Value.DisplayName);
        if (obj.MinutesUntilReady > 0)
        {
            int minutes = obj.MinutesUntilReady;
            string duration = minutes < 60 ? $"{minutes}m" : minutes % 60 == 0 ? $"{minutes / 60}h" : $"{minutes / 60}h {minutes % 60}m";
            return new(MachineState.Working, $"Working: about {duration} of game time left");
        }
        if (obj.heldObject.Value != null) return new(MachineState.Working, "Working: finishing up");
        bool takesInput = data.OutputRules?.Any(rule => rule.Triggers?.Any(trigger => trigger.Trigger == MachineOutputTrigger.ItemPlacedInMachine) == true) == true;
        return new(MachineState.Idle, takesInput ? "Idle: waiting for ingredients" : "Idle: waiting to start");
    }
}
