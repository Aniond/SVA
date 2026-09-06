using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace AbigailModern;

/// <summary>Decorates Kel's existing plain event messages without changing dialogue or choices.</summary>
internal static class KelPortraits
{
    private static IModHelper Helper = null!;
    private static IMonitor Monitor = null!;

    public static void Initialize(IModHelper helper, IMonitor monitor, string uniqueId)
    {
        Helper = helper;
        Monitor = monitor;
        new Harmony(uniqueId).Patch(AccessTools.Method(typeof(Event.DefaultCommands), nameof(Event.DefaultCommands.Message)),
            prefix: new HarmonyMethod(typeof(KelPortraits), nameof(BeforeMessage)),
            postfix: new HarmonyMethod(typeof(KelPortraits), nameof(AfterMessage)));
    }

    private static void BeforeMessage(out IClickableMenu? __state) => __state = Game1.activeClickableMenu;

    private static void AfterMessage(Event @event, string[] args, IClickableMenu? __state)
    {
        if (ReferenceEquals(__state, Game1.activeClickableMenu) || args.Length < 2
            || Game1.activeClickableMenu is not DialogueBox box || box.characterDialogue != null || box.isQuestion
            || !args[1].StartsWith("KEL:", StringComparison.OrdinalIgnoreCase)) return;
        var actor = @event.getActorByName("LeahEx");
        if (actor == null) return;
        try
        {
            var spriteName = actor.Sprite.textureName.Value.Replace('\\', '/');
            var variant = spriteName.EndsWith("/LeahExFemale", StringComparison.OrdinalIgnoreCase) ? "Female"
                : spriteName.EndsWith("/LeahExMale", StringComparison.OrdinalIgnoreCase) ? "Male" : null;
            if (variant == null) return;
            var text = args[1];
            var emotion = text.Contains("miss you", StringComparison.OrdinalIgnoreCase) ? 2
                : text.Contains("Hey!", StringComparison.OrdinalIgnoreCase) || text.Contains("nothing to say", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("bumpkin", StringComparison.OrdinalIgnoreCase) ? 5
                : text.Contains("Heh.", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            PortraitPanel.Attach(box, Helper.GameContent.Load<Texture2D>($"Portraits/LeahEx{variant}"), "Kel", emotion);
        }
        catch (Exception ex) { Monitor.Log($"Could not display Kel's portrait: {ex.Message}", LogLevel.Warn); }
    }
}
