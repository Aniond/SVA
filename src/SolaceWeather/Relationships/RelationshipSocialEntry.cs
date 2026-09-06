using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using SolaceWeather.Core;

namespace SolaceWeather.Relationships;

/// <summary>Changes candidate presentation only. Native friendship values are never edited.</summary>
internal static class RelationshipSocialEntry
{
    private static Func<bool>? ready;
    private static Action<string>? open;
    private static Func<string, string>? summary;
    private static IMonitor? monitor;
    private static bool failed;

    public static void Install(string id, IMonitor monitor, Func<bool> ready, Action<string> openJournal, Func<string, string> bondSummary)
    {
        if (Game1.version != "1.6.15" || Constants.ApiVersion.ToString() != "4.5.2")
        {
            monitor.Log("The relationship Social-page links were skipped: this game/SMAPI version has not been checked. Use F6 for the journal.", LogLevel.Warn);
            return;
        }
        var harmony = new Harmony(id + ".RelationshipSocialEntry");
        try
        {
            var heart = AccessTools.Method(typeof(SocialPage), nameof(SocialPage.drawNPCSlotHeart),
                new[] { typeof(SpriteBatch), typeof(int), typeof(SocialPage.SocialEntry), typeof(int), typeof(bool), typeof(bool) })
                ?? throw new MissingMethodException("SocialPage.drawNPCSlotHeart");
            var click = AccessTools.Method(typeof(SocialPage), nameof(SocialPage.receiveLeftClick), new[] { typeof(int), typeof(int), typeof(bool) })
                ?? throw new MissingMethodException("SocialPage.receiveLeftClick");
            RelationshipSocialEntry.monitor = monitor;
            RelationshipSocialEntry.ready = ready;
            open = openJournal;
            summary = bondSummary;
            failed = false;
            harmony.Patch(heart, prefix: new HarmonyMethod(typeof(RelationshipSocialEntry), nameof(BeforeHeart)));
            harmony.Patch(click, prefix: new HarmonyMethod(typeof(RelationshipSocialEntry), nameof(BeforeClick)));
        }
        catch (Exception ex)
        {
            harmony.UnpatchAll(harmony.Id);
            failed = true;
            monitor.Log($"The relationship Social-page links were skipped safely: {ex.Message}. Use F6 for the journal.", LogLevel.Warn);
        }
    }

    private static void Disable(Exception ex)
    {
        failed = true;
        monitor?.Log($"The relationship Social-page links were disabled: {ex.Message}. Use F6 for the journal.", LogLevel.Warn);
    }

    private static bool BeforeHeart(SocialPage __instance, SpriteBatch b, int npcIndex, SocialPage.SocialEntry entry, int hearts)
    {
        if (RomanceProfiles.Get(entry.InternalName) == null || entry.IsPlayer || failed) return true;
        try
        {
            if (ready?.Invoke() != true || npcIndex < 0 || npcIndex >= __instance.sprites.Count) return true;
            if (hearts == 0)
            {
                int x = __instance.xPositionOnScreen + 316, y = __instance.sprites[npcIndex].bounds.Y + 30;
                Color purple = new(104, 63, 145);
                b.Draw(Game1.staminaRect, new Rectangle(x + 8, y + 8, 3, 25), purple);
                b.Draw(Game1.staminaRect, new Rectangle(x + 8, y + 20, 20, 3), purple);
                b.Draw(Game1.staminaRect, new Rectangle(x, y, 18, 18), purple);
                b.Draw(Game1.staminaRect, new Rectangle(x + 22, y + 15, 12, 12), purple);
                string label = summary?.Invoke(entry.InternalName) ?? "Friendship";
                label = label.Split('\n')[0];
                while (label.Length > 1 && Game1.smallFont.MeasureString(label).X * .75f > 220) label = label[..^1];
                b.DrawString(Game1.smallFont, label,
                    new Vector2(x + 42, y + 3), purple, 0, Vector2.Zero, .75f, SpriteEffects.None, .89f);
            }
            return false;
        }
        catch (Exception ex) { Disable(ex); return true; }
    }

    private static bool BeforeClick(SocialPage __instance, int x, int y)
    {
        if (failed) return true;
        try
        {
            if (ready?.Invoke() != true || __instance.scrolling) return true;
            for (int i = Math.Max(0, __instance.slotPosition); i < Math.Min(__instance.slotPosition + 5, __instance.characterSlots.Count); i++)
            {
                if (i >= __instance.SocialEntries.Count || !__instance.characterSlots[i].bounds.Contains(x, y)) continue;
                var entry = __instance.SocialEntries[i];
                if (entry.IsPlayer || RomanceProfiles.Get(entry.InternalName) == null) return true;
                var previous = Game1.activeClickableMenu;
                open?.Invoke(entry.InternalName);
                if (ReferenceEquals(previous, Game1.activeClickableMenu)) return true;
                // Match the native profile's return behavior, including its scroll position.
                int slot = __instance.slotPosition;
                Game1.activeClickableMenu.exitFunction = () =>
                {
                    var socialMenu = new GameMenu(GameMenu.socialTab, -1, playOpeningSound: false);
                    Game1.activeClickableMenu = socialMenu;
                    if (socialMenu.GetCurrentPage() is SocialPage social)
                    {
                        social.slotPosition = Math.Clamp(slot, 0, Math.Max(0, social.SocialEntries.Count - 5));
                        social.updateSlots();
                    }
                };
                return false;
            }
        }
        catch (Exception ex) { Disable(ex); }
        return true;
    }
}
