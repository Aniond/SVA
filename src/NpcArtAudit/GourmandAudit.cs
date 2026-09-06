using System.Reflection;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;

namespace NpcArtAudit;

internal static class GourmandAudit
{
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        var menuField = typeof(Game1).GetField("_activeClickableMenu", BindingFlags.Static | BindingFlags.NonPublic)!;
        var originalMenu = Game1.activeClickableMenu;
        var originalLocation = Game1.currentLocation;
        var originalDialogue = Game1.dialogueUp;
        var originalMove = Game1.player.CanMove;
        var originalAfter = Game1.afterDialogues;
        var hadMail = Game1.player.mailReceived.Contains("talkedToGourmand");
        try
        {
            var panelType = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "AbigailModern").GetType("AbigailModern.PortraitPanel")!;
            bool Has(DialogueBox box) => (bool)panelType.GetMethod("HasPanel")!.Invoke(null, new object[] { box })!;
            var cave = new IslandFarmCave();
            cave.gourmand = new NPC(new AnimatedSprite("Characters/Gourmand", 0, 32, 32), Vector2.Zero, 2, "Gourmand");
            Game1.currentLocation = cave;
            var count = 0;
            foreach (var key in helper.GameContent.Load<Dictionary<string, string>>("Strings/Locations").Keys.Where(k => k.StartsWith("Gourmand_")))
            foreach (var line in Game1.content.LoadString("Strings\\Locations:" + key).Split('|'))
            {
                var box = new DialogueBox(Game1.parseText(line));
                if (!Has(box)) throw new Exception("Missing portrait for " + key);
                count++;
            }
            if (Has(new DialogueBox("Unrelated cave message."))) throw new Exception("Decorated unrelated message.");
            menuField.SetValue(null, null); Game1.dialogueUp = false;
            if (!hadMail) Game1.player.mailReceived.Add("talkedToGourmand");
            cave.TalkToGourmand();
            var request = Game1.activeClickableMenu as DialogueBox ?? throw new Exception("Native request missing.");
            if (!Has(request) || Game1.afterDialogues == null) throw new Exception("Request portrait or callback missing.");
            menuField.SetValue(null, null); Game1.dialogueUp = false;
            Game1.afterDialogues();
            var question = Game1.activeClickableMenu as DialogueBox ?? throw new Exception("Native question missing.");
            if (!Has(question) || !question.responses.Select(r => r.responseKey).SequenceEqual(new[] { "Yes", "No" }) || Game1.afterDialogues != null)
                throw new Exception("Question portrait, response keys or callback behavior wrong.");
            helper.Data.WriteJsonFile("gourmand-interaction-checks.json", new { Passed = true, LocalizedLinesChecked = count, NativeTalk = true, NativeQuestion = true, YesNoPreserved = true, CropChecksTested = false, RewardsTested = false, FarmLoaded = false });
            monitor.Log($"Gourmand checks passed: {count} localized lines plus native request and Yes/No callback.", LogLevel.Info);
        }
        catch (Exception ex)
        {
            helper.Data.WriteJsonFile("gourmand-interaction-checks.json", new { Passed = false, Error = ex.ToString() });
            monitor.Log($"Gourmand audit failed: {ex}", LogLevel.Error);
        }
        finally
        {
            menuField.SetValue(null, originalMenu); Game1.currentLocation = originalLocation;
            Game1.dialogueUp = originalDialogue; Game1.player.CanMove = originalMove; Game1.afterDialogues = originalAfter;
            if (!hadMail) Game1.player.mailReceived.Remove("talkedToGourmand");
        }
    }
}
