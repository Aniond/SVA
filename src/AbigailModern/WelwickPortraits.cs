using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace AbigailModern;

internal static class WelwickPortraits
{
    private static IModHelper Helper = null!;
    private static IMonitor Monitor = null!;

    public static void Initialize(IModHelper helper, IMonitor monitor, string uniqueId)
    {
        Helper = helper; Monitor = monitor;
        var harmony = new Harmony(uniqueId);
        harmony.Patch(AccessTools.Method(typeof(TV), nameof(TV.selectChannel)), prefix: new HarmonyMethod(typeof(WelwickPortraits), nameof(Before)), postfix: new HarmonyMethod(typeof(WelwickPortraits), nameof(AfterOpening)));
        harmony.Patch(AccessTools.Method(typeof(TV), nameof(TV.proceedToNextScene)), prefix: new HarmonyMethod(typeof(WelwickPortraits), nameof(Before)), postfix: new HarmonyMethod(typeof(WelwickPortraits), nameof(AfterForecast)));
    }

    private static void Before(out IClickableMenu? __state) => __state = Game1.activeClickableMenu;
    private static void AfterOpening(TV __instance, IClickableMenu? __state) => Attach(__instance, __state, true);
    private static void AfterForecast(TV __instance, IClickableMenu? __state) => Attach(__instance, __state, false);
    private static string Normalize(string text) => string.Concat(text.Where(c => !char.IsWhiteSpace(c)));

    public static int ForecastExpression(string text)
    {
        foreach (var (key, emotion) in new[] { ("13191",5),("13192",5),("13193",2),("13195",2),("13197",4),("13198",4),("13199",1),("13200",0),("13201",0) })
            if (Normalize(text) == Normalize(Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs." + key))) return emotion;
        return 0;
    }

    private static void Attach(TV tv, IClickableMenu? previous, bool opening)
    {
        if ((int)AccessTools.Field(typeof(TV), "currentChannel").GetValue(tv)! != 3
            || ReferenceEquals(previous, Game1.activeClickableMenu) || Game1.activeClickableMenu is not DialogueBox box || box.characterDialogue != null) return;
        try { PortraitPanel.Attach(box, Helper.GameContent.Load<Texture2D>("Portraits/Welwick"), "Welwick", opening ? 3 : ForecastExpression(box.getCurrentString())); }
        catch (Exception ex) { Monitor.Log($"Could not display Welwick's portrait: {ex.Message}", LogLevel.Warn); }
    }
}
