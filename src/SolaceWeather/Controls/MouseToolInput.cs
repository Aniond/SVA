using HarmonyLib;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;

namespace SolaceWeather.Controls;

/// <summary>Route physical right-click to the native tool press/hold/release pipeline.</summary>
internal static class MouseToolInput
{
    private static ModConfig config = null!;
    private static bool activating;

    public static void Activate(Microsoft.Xna.Framework.Point tile)
    {
        bool previous = activating;
        try
        {
            activating = true;
            Game1.tryToCheckAt(new Microsoft.Xna.Framework.Vector2(tile.X, tile.Y), Game1.player);
        }
        finally { activating = previous; }
    }

    private static void ActionClick(ref bool __result)
    {
        if (activating) __result = true;
    }

    public static void Install(string id, ModConfig settings, IMonitor monitor)
    {
        config = settings;
        // SInputState exposes the post-suppression mouse snapshot to the game. Its
        // physical-button tracking remains untouched, so SMAPI still reports right clicks.
        if (Game1.version != "1.6.15" || Constants.ApiVersion.ToString() != "4.5.2")
        {
            monitor.Log("Right-click tool mapping needs Stardew 1.6.15 / SMAPI 4.5.2; native mouse controls retained.", LogLevel.Warn);
            return;
        }
        try
        {
            var method = AccessTools.Method(Game1.input.GetType(), nameof(InputState.GetMouseState), Type.EmptyTypes)
                ?? throw new MissingMethodException("Mouse input snapshot method not found.");
            new Harmony(id + ".MouseTools").Patch(method, postfix: new HarmonyMethod(typeof(MouseToolInput), nameof(MapMouse)));
            new Harmony(id + ".ClickActions").Patch(AccessTools.Method(typeof(Game1), nameof(Game1.didPlayerJustRightClick)),
                postfix: new HarmonyMethod(typeof(MouseToolInput), nameof(ActionClick)));
            monitor.Log("Right-click tools enabled; menus retain native mouse controls.", LogLevel.Info);
        }
        catch (Exception ex) { monitor.Log($"Right-click tool mapping could not load: {ex.Message}", LogLevel.Error); }
    }

    private static void MapMouse(ref MouseState __result)
    {
        if (!config.RightClickUsesTool || !Context.IsWorldReady || Context.IsMultiplayer || Game1.activeClickableMenu != null
            || Game1.eventUp || Game1.currentMinigame != null || Game1.dialogueUp || (Game1.chatBox?.chatBox.Selected ?? false)) return;
        // Preserve positions, scroll and auxiliary buttons. Keeping the tool held in
        // the native snapshot also preserves charging and tool-release behavior.
        int x = (int)(__result.X / Game1.options.uiScale), y = (int)(__result.Y / Game1.options.uiScale);
        bool overHud = Game1.onScreenMenus.Any(menu => menu.isWithinBounds(x, y));
        if (__result.RightButton == ButtonState.Pressed || (config.EnableClickToMove && !overHud))
            __result = new MouseState(__result.X, __result.Y, __result.ScrollWheelValue,
                __result.RightButton, __result.MiddleButton, ButtonState.Released, __result.XButton1, __result.XButton2);
    }
}
