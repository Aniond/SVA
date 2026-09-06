using System.Reflection;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SolaceWeather.Core;
using StardewValley;
using StardewValley.Menus;

namespace SolaceWeather.GameTests;

public sealed partial class ModEntry
{
    private void TreeUiChecks(string command)
    {
        RequireWorld();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        object mod = AiMod(Helper);
        object observer = mod.GetType().GetField("abigail", flags)!.GetValue(mod)!;
        object service = observer.GetType().GetProperty("TreeService", flags)!.GetValue(observer)!;
        var memory = (AbigailMemory)observer.GetType().GetField("memory", flags)!.GetValue(observer)!;
        string before = JsonSerializer.Serialize(memory);
        var friendship = Game1.player.friendshipData.TryGetValue("Abigail", out var value) ? value : null;
        int points = friendship?.Points ?? 0, gifts = friendship?.GiftsToday ?? 0;
        bool talked = friendship?.TalkedToToday ?? false;
        var results = new List<object>();
        void Check(string name, bool passed) => results.Add(new { Name = name, Passed = passed });
        if (command == "treecompact")
        {
            if (!originalUiScale.HasValue) { originalUiScale = Game1.options.baseUIScale; originalDesiredUiScale = Game1.options.desiredUIScale; }
            Game1.options.baseUIScale = 1.5f; Game1.options.desiredUIScale = 1.5f;
            Game1.game1.refreshWindowSettings();
        }
        Game1.PushUIMode();
        try
        {
            if (command is "treesocial" or "treesocialcapture")
            {
                var gameMenu = new GameMenu(GameMenu.socialTab, -1, playOpeningSound: false);
                Game1.activeClickableMenu = gameMenu;
                var social = (SocialPage)gameMenu.GetCurrentPage();
                int index = social.SocialEntries.FindIndex(entry => !entry.IsPlayer && entry.InternalName == "Abigail");
                if (index < 0) throw new InvalidOperationException("Abigail is absent from Social.");
                social.slotPosition = Math.Clamp(index, 0, Math.Max(0, social.SocialEntries.Count - 5));
                social.updateSlots();
                Check("Abigail social row is visible", index >= social.slotPosition && index < social.slotPosition + 5);
                if (command == "treesocial")
                {
                    Rectangle row = social.characterSlots[index].bounds;
                    social.receiveLeftClick(row.Center.X, row.Center.Y);
                    Check("Native Social row click opens relationship tree", Game1.activeClickableMenu?.GetType().Name == "RelationshipTreeMenu");
                }
            }
            else if (command == "treeperks")
            {
                object conversation = mod.GetType().GetField("abigailConversation", flags)!.GetValue(mod)!;
                var npc = Game1.getCharacterFromName("Abigail");
                Call(conversation, "Reset");
                Game1.activeClickableMenu = new DialogueBox(new Dialogue(npc, null, "Hello."));
                Call(conversation, "Tick");
                Check("Actual conversation input opens", Game1.activeClickableMenu is NamingMenu);
                Call(conversation, "ActOnQuest", "ui:services");
                Check("Actual conversation Perks route opens selector", Game1.activeClickableMenu?.GetType().Name == "ServiceChoicesMenu");
            }
            else
            {
                Call(service, "OpenTree");
                Check("Actual relationship tree opens", Game1.activeClickableMenu?.GetType().Name == "RelationshipTreeMenu");
                if (command == "treescroll")
                    for (int i = 0; i < 30; i++) Game1.activeClickableMenu?.receiveKeyPress(Keys.Down);
                if (Game1.activeClickableMenu?.GetType().Name == "RelationshipTreeMenu")
                {
                    var menu = Game1.activeClickableMenu;
                    var tree = (Rectangle)menu.GetType().GetField("treeArea", flags)!.GetValue(menu)!;
                    var detail = (Rectangle)menu.GetType().GetField("detailArea", flags)!.GetValue(menu)!;
                    Check("Tree and detail panels fit UI viewport", tree.Left >= 0 && detail.Left >= 0 && tree.Right <= Game1.uiViewport.Width
                        && detail.Right <= Game1.uiViewport.Width && tree.Top >= 0 && detail.Bottom <= Game1.uiViewport.Height);
                    Check("Both panels have readable height", tree.Height >= 80 && detail.Height >= 80);
                }
            }
            Check("UI leaves friendship and gifts unchanged", (friendship?.Points ?? 0) == points && (friendship?.GiftsToday ?? 0) == gifts
                && (friendship?.TalkedToToday ?? false) == talked);
            Check("UI leaves stored relationship memory unchanged", JsonSerializer.Serialize(memory) == before);
            Helper.Data.WriteJsonFile(command + "-results.json", results);
            captureRequested = true; captureDelay = 4;
        }
        finally { Game1.PopUIMode(); }
    }

    private void TreePrepareAi()
    {
        TreeUiChecks("treeperks");
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        object mod = AiMod(Helper);
        object observer = mod.GetType().GetField("abigail", flags)!.GetValue(mod)!;
        object service = observer.GetType().GetProperty("TreeService", flags)!.GetValue(observer)!;
        string key = ((Array)Call(service, "Choices")!).Cast<object>().Select(c => (string)Get(c, "Key")!).First(k => k.EndsWith(":prepare"));
        aiCount = LiveMemory().Exchanges.Count;
        aiPoints = Game1.player.friendshipData.TryGetValue("Abigail", out var friendship) ? friendship.Points : 0;
        aiWatching = true;
        Call(mod.GetType().GetField("abigailConversation", flags)!.GetValue(mod)!, "ActOnQuest", key);
    }

    private void StageTreeSave()
    {
        RequireWorld();
        if (Game1.player.farmName.Value != "Solace") throw new InvalidOperationException("Tree staging is restricted to the Solace test farm.");
        int today = Game1.Date.TotalDays;
        if (today < 5) throw new InvalidOperationException("Tree staging needs a test date after day five.");
        var memory = LiveMemory();
        string personalBefore = JsonSerializer.Serialize(memory.Personal);
        var ledger = new PromiseLedger();
        ledger.ObserveMaterials(new[] { "quartz", "iron" });
        foreach (string id in new[] { "fish", "quartz", "iron" })
        {
            ledger.Offer(id, 0); ledger.Accept(id, 0, id == "fish" ? 0 : 1, false);
            ledger.Complete(id, id == "fish" ? "(O)131" : PromiseLedger.Definition(id)!.ItemId, id, 0);
        }
        var tree = new RelationshipTreeState();
        tree.Observe(ledger, today, new[] { 0, 1, 2, 3, 4 });
        tree.Study("(O)80", 0); tree.Study("(O)66", 1); tree.Study("(O)86", 2);
        tree.ChooseApproach("safe", today); tree.LastKitDay = today;
        tree.BeginSwitch("bold", today, false);
        tree.RecordPreparation(today, 900, true); tree.RecordMineVisit(today, 1000);
        if (!ledger.IsValid() || !tree.IsValid()) throw new InvalidOperationException("Staged tree was not valid.");
        memory.Promises = ledger; memory.Tree = tree;
        Helper.Data.WriteJsonFile("tree-stage-results.json", new {
            Valid = memory.IsValid(), CompletedPromises = ledger.Records.Count(r => r.Status == "completed"),
            MineDays = tree.MineDays.Count, Studies = tree.Studies.Count, tree.Approach, tree.LastKitDay,
            PendingTarget = tree.PendingSwitch?.Target, VerifiedSwitchDays = tree.PendingSwitch?.VerifiedDays.Count,
            PersonalMemoryUnchanged = personalBefore == JsonSerializer.Serialize(memory.Personal), Tree = tree
        });
    }
}
