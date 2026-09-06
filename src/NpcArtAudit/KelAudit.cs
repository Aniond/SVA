using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace NpcArtAudit;

internal static class KelAudit
{
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        var menuField = typeof(Game1).GetField("_activeClickableMenu", BindingFlags.Static | BindingFlags.NonPublic)!;
        var originalMenu = Game1.activeClickableMenu;
        var originalDialogue = Game1.dialogueUp;
        var originalMove = Game1.player.CanMove;
        try
        {
            var panelType = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "AbigailModern").GetType("AbigailModern.PortraitPanel")!;
            var panels = panelType.GetField("Panels", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            var checks = new List<object>();
            foreach (var variant in new[] { "Male", "Female" })
            foreach (var (text, emotion) in new[] { ("KEL: And to get you to come back with me.", 0), ("KEL: Heh. Well, you were half right...", 1), ("KEL: I miss you, babe.", 2), ("KEL: Hey! Come Here!", 5), ("An unrelated event message.", -1) })
            {
                menuField.SetValue(null, null);
                Game1.dialogueUp = false;
                var scene = new Event();
                scene.actors.Add(new NPC(new AnimatedSprite($"Characters/LeahEx{variant}", 0, 16, 32), Vector2.Zero, 2, "LeahEx"));
                var args = new[] { "message", text };
                Event.DefaultCommands.Message(scene, args, new EventContext(scene, Game1.currentLocation, Game1.currentGameTime, args));
                var box = Game1.activeClickableMenu as DialogueBox ?? throw new Exception("Native message did not open.");
                if (box.characterDialogue != null || box.isQuestion || !box.dialogues.SequenceEqual(new[] { Game1.parseText(text) })) throw new Exception("Original message changed.");
                var query = new object?[] { box, null };
                var found = (bool)panels.GetType().GetMethod("TryGetValue")!.Invoke(panels, query)!;
                if (found != (emotion >= 0)) throw new Exception("Portrait routing mismatch.");
                if (found)
                {
                    var panel = query[1]!;
                    if ((int)panel.GetType().GetProperty("Emotion")!.GetValue(panel)! != emotion) throw new Exception("Wrong expression.");
                    var actual = (Texture2D)panel.GetType().GetProperty("Texture")!.GetValue(panel)!;
                    var expected = helper.GameContent.Load<Texture2D>($"Portraits/LeahEx{variant}");
                    var actualPixels = new Color[actual.Width * actual.Height];
                    var expectedPixels = new Color[expected.Width * expected.Height];
                    actual.GetData(actualPixels); expected.GetData(expectedPixels);
                    if (!actualPixels.SequenceEqual(expectedPixels)) throw new Exception("Wrong Kel portrait variant.");
                }
                checks.Add(new { Variant = variant, Emotion = emotion, NativeMessagePreserved = true, PortraitMatched = found });
            }
            helper.Data.WriteJsonFile("kel-interaction-checks.json", new { Passed = true, Checks = checks, FarmLoaded = false, FullEventVerified = false, OtherLanguagesVerified = false });
            monitor.Log("Kel native event message checks passed for both variants, four expressions and unrelated message exclusion.", LogLevel.Info);
        }
        catch (Exception ex)
        {
            helper.Data.WriteJsonFile("kel-interaction-checks.json", new { Passed = false, Error = ex.ToString() });
            monitor.Log($"Kel audit failed: {ex}", LogLevel.Error);
        }
        finally
        {
            menuField.SetValue(null, originalMenu);
            Game1.dialogueUp = originalDialogue;
            Game1.player.CanMove = originalMove;
        }
    }
}
