using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;

namespace AbigailModern;

internal static class WinterMysteryPortrait
{
    private static IModHelper Helper = null!;
    private static IMonitor Monitor = null!;

    public static void Initialize(IModHelper helper, IMonitor monitor, string uniqueId)
    {
        Helper = helper; Monitor = monitor;
        new Harmony(uniqueId).Patch(AccessTools.Method(typeof(Town), "mgThief_speech"),
            prefix: new HarmonyMethod(typeof(WinterMysteryPortrait), nameof(Before)),
            postfix: new HarmonyMethod(typeof(WinterMysteryPortrait), nameof(After)));
    }

    private static void Before(out IClickableMenu? __state) => __state = Game1.activeClickableMenu;

    private static void After(IClickableMenu? __state)
    {
        if (ReferenceEquals(__state, Game1.activeClickableMenu) || Game1.activeClickableMenu is not DialogueBox box || box.characterDialogue != null) return;
        try { PortraitPanel.Attach(box, Helper.GameContent.Load<Texture2D>("Portraits/Krobus"), "???", 2); }
        catch (Exception ex) { Monitor.Log($"Could not display the winter mystery portrait: {ex.Message}", LogLevel.Warn); }
    }
}
