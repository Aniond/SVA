using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;

namespace AbigailModern;

internal static class GourmandPortraits
{
    private static IModHelper Helper = null!;
    private static IMonitor Monitor = null!;
    private static Dictionary<string, int>? Lines;
    private static LocalizedContentManager.LanguageCode Language;

    public static void Initialize(IModHelper helper, IMonitor monitor, string uniqueId)
    {
        Helper = helper; Monitor = monitor;
        var harmony = new Harmony(uniqueId);
        foreach (var args in new[] { new[] { typeof(string) }, new[] { typeof(List<string>) }, new[] { typeof(string), typeof(Response[]), typeof(int) } })
            harmony.Patch(AccessTools.Constructor(typeof(DialogueBox), args), postfix: new HarmonyMethod(typeof(GourmandPortraits), nameof(AfterConstruct)));
    }

    private static void AfterConstruct(DialogueBox __instance) => Refresh(__instance);
    private static string Normalize(string text) => string.Concat(text.Where(c => !char.IsWhiteSpace(c)));

    public static void Refresh(DialogueBox box)
    {
        if (box.characterDialogue != null || Game1.currentLocation is not IslandFarmCave
            && Game1.currentLocation?.currentEvent?.getActorByName("Gourmand") == null) return;
        try
        {
            if (Lines == null || Language != LocalizedContentManager.CurrentLanguageCode)
            {
                Language = LocalizedContentManager.CurrentLanguageCode;
                Lines = new Dictionary<string, int>();
                foreach (var key in Helper.GameContent.Load<Dictionary<string, string>>("Strings/Locations").Keys.Where(k => k.StartsWith("Gourmand_", StringComparison.Ordinal)))
                {
                    var emotion = key.Contains("Failed") || key.Contains("InProgress") ? 2
                        : key.Contains("Success") ? 1 : key.Contains("Reward") || key.Contains("Finished") ? 5
                        : key.Contains("Request") ? 3 : 0;
                    foreach (var line in Game1.content.LoadString("Strings\\Locations:" + key).Split('|'))
                        Lines[Normalize(Game1.parseText(line))] = emotion;
                }
            }
            if (Lines.TryGetValue(Normalize(box.getCurrentString()), out var expression))
                PortraitPanel.Attach(box, Helper.GameContent.Load<Texture2D>("Portraits/Gourmand"), "Gourmand Frog", expression);
        }
        catch (Exception ex) { Monitor.Log($"Could not display Gourmand's portrait: {ex.Message}", LogLevel.Warn); }
    }
}
