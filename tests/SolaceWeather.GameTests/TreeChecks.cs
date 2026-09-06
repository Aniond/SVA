using System.Reflection;
using System.Text.Json;
using Microsoft.Xna.Framework;
using SolaceWeather.Controls;
using SolaceWeather.Core;
using StardewValley;
using StardewValley.Tools;

namespace SolaceWeather.GameTests;

public sealed partial class ModEntry
{
    private void CheckTree()
    {
        RequireWorld();
        int today = Game1.Date.TotalDays;
        if (today < 5 || Game1.eventUp || Game1.currentMinigame != null)
            throw new InvalidOperationException("Run treechecks on an ordinary day after the first five days.");
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        object mod = AiMod(Helper);
        object observer = mod.GetType().GetField("abigail", flags)!.GetValue(mod)!;
        object service = observer.GetType().GetProperty("TreeService", flags)!.GetValue(observer)!;
        var memoryField = observer.GetType().GetField("memory", flags)!;
        object? savedMemory = memoryField.GetValue(observer);
        var player = Game1.player;
        var items = player.Items.ToArray(); var quests = player.questLog.ToArray();
        var location = player.currentLocation; var position = player.Position;
        int selected = player.CurrentToolIndex, time = Game1.timeOfDay;
        float stamina = player.Stamina;
        bool canMove = player.CanMove, dialogueUp = Game1.dialogueUp;
        var menu = Game1.activeClickableMenu;
        var hud = Game1.hudMessages.ToArray();
        string? song = Game1.currentSong?.Name;
        var activeEvents = player.activeDialogueEvents.Keys.ToDictionary(k => k, k => player.activeDialogueEvents[k]);
        var npc = Game1.getCharacterFromName("Abigail");
        var friendship = player.friendshipData.TryGetValue("Abigail", out var originalFriendship) ? originalFriendship : null;
        int points = friendship?.Points ?? 0, gifts = friendship?.GiftsToday ?? 0;
        bool talked = friendship?.TalkedToToday ?? false;
        var results = new List<object>();
        void Check(string name, bool pass) => results.Add(new { Name = name, Passed = pass });
        string? Choice(string action) => ((Array)Call(service, "Choices")!).Cast<object>()
            .Select(c => (string)Get(c, "Key")!).SingleOrDefault(k => k.EndsWith(":" + action));
        bool Act(string action) => Choice(action) is string key && Call(service, "ApplyChoice", key) != null;
        int Count(string id) => player.Items.Where(i => i?.QualifiedItemId == id).Sum(i => i.Stack);
        void Inventory(params Item[] inventory)
        {
            player.Items.Clear(); foreach (var item in inventory) player.Items.Add(item);
            while (player.Items.Count < player.MaxItems) player.Items.Add(null);
            player.CurrentToolIndex = 0;
        }
        AbigailMemory Fresh()
        {
            var memory = new AbigailMemory();
            memory.Promises.ObserveMaterials(new[] { "quartz", "iron" });
            foreach (string id in new[] { "fish", "quartz", "iron" })
            {
                memory.Promises.Offer(id, 0); memory.Promises.Accept(id, 0, id == "fish" ? 0 : 1, false);
                memory.Promises.Complete(id, id == "fish" ? "(O)131" : PromiseLedger.Definition(id)!.ItemId, id, 0);
            }
            memory.Tree.Observe(memory.Promises, today, new[] { 0, 1, 2, 3, 4 });
            memory.Tree.Study("(O)80", 0); memory.Tree.Study("(O)66", 1); memory.Tree.Study("(O)86", 2);
            memoryField.SetValue(observer, memory); player.questLog.Clear();
            Call(service, "Observe"); return memory;
        }
        try
        {
            Game1.activeClickableMenu = null; Game1.dialogueUp = false; player.CanMove = true;
            player.currentLocation = npc.currentLocation; player.Position = npc.Position + new Vector2(0, 64);
            Game1.timeOfDay = 1000;
            var memory = Fresh();
            Inventory(ItemRegistry.Create("(O)80", 4));
            string exchange = Choice("exchange/(O)80")!;
            ((StardewValley.Object)player.Items[0]).modData[QuickStack.ProtectedKey] = "true";
            Check("Protected mineral rejects even an already-open exchange choice", Call(service, "ApplyChoice", exchange) == null && Count("(O)80") == 4 && memory.Tree.LastExchangeDay == null);
            player.Items[0].modData.Remove(QuickStack.ProtectedKey);
            ((StardewValley.Object)player.Items[0]).questItem.Value = true;
            Check("Quest minerals cannot be exchanged", Choice("exchange/(O)80") == null && Count("(O)80") == 4);
            ((StardewValley.Object)player.Items[0]).questItem.Value = false;
            for (int i = 1; i < player.Items.Count; i++) player.Items[i] = ItemRegistry.Create("(O)388", 999);
            Check("Full backpack rejects exchange without spending minerals or cooldown", !Act("exchange/(O)80") && Count("(O)80") == 4 && memory.Tree.LastExchangeDay == null);
            player.Items[0].Stack = 2;
            exchange = Choice("exchange/(O)80")!;
            Check("Exchange can use the slot freed by two consumed minerals", Call(service, "ApplyChoice", exchange) != null && Count("(O)80") == 0 && Count("(O)535") == 1 && memory.Tree.LastExchangeDay == today);
            Check("Duplicate exchange cannot give another Geode", Call(service, "ApplyChoice", exchange) == null && Count("(O)535") == 1);

            memory = Fresh(); Inventory(ItemRegistry.Create("(O)80"), ItemRegistry.Create("(O)80"));
            Check("Exchange accepts two eligible minerals held in separate stacks", Act("exchange/(O)80") && Count("(O)80") == 0 && Count("(O)535") == 1);
            memory = Fresh(); Inventory(ItemRegistry.Create("(O)80"), ItemRegistry.Create("(O)80"));
            player.Items[1].modData[QuickStack.ProtectedKey] = "true";
            Check("Split stacks cannot borrow a protected mineral", Choice("exchange/(O)80") == null && Count("(O)80") == 2 && memory.Tree.LastExchangeDay == null);

            memory = Fresh(); Inventory(ItemRegistry.Create("(O)84"));
            string study = Choice("study/(O)84")!;
            Check("Study keeps the item and native gifts unchanged", Call(service, "ApplyChoice", study) != null && Count("(O)84") == 1 && memory.Tree.Studies.Count == 4 && (friendship?.GiftsToday ?? 0) == gifts);
            Check("Repeated study cannot advance twice", Call(service, "ApplyChoice", study) == null && memory.Tree.Studies.Count == 4);
            Check("Approach selection is explicit and does not give a kit", memory.Tree.Approach == "none" && Act("choose/safe") && memory.Tree.Approach == "safe" && Count("(O)403") == 0);
            Inventory(Enumerable.Range(0, player.MaxItems).Select(_ => ItemRegistry.Create("(O)388", 999)).ToArray());
            Check("Full kit reward requires room before starting cooldown", !Act("kit") && memory.Tree.LastKitDay == null && Count("(O)403") == 0);
            player.Items[0] = null;
            Check("One free slot is insufficient for both kit stacks", !Act("kit") && memory.Tree.LastKitDay == null && Count("(O)403") == 0);
            player.Items[1] = null;
            string kit = Choice("kit")!;
            Check("Safe kit grants its complete contents once", Call(service, "ApplyChoice", kit) != null && Count("(O)403") == 2 && Count("(O)93") == 5 && memory.Tree.LastKitDay == today);
            Check("Duplicate kit action cannot add rewards", Call(service, "ApplyChoice", kit) == null && Count("(O)403") == 2 && Count("(O)93") == 5);
            int trust = memory.Promises.Score;
            Check("Switch action opens one native journal milestone", Act("switch") && memory.Tree.OutstandingSwitch && player.questLog.Count == 1);
            Check("Cancel retains approach, trust and kit cooldown", Act("cancel") && !memory.Tree.OutstandingSwitch && memory.Tree.Approach == "safe" && memory.Promises.Score == trust && memory.Tree.LastKitDay == today && player.questLog.Count == 0);

            memory.Tree.BeginSwitch("bold", today - 1, false);
            memory.Tree.RecordPreparation(today - 1, 900, true); memory.Tree.RecordMineVisit(today - 1, 1000);
            Inventory(new MeleeWeapon("0"), ItemRegistry.Create("(O)93", 5));
            Check("Incomplete target preparation does not advance switch", Act("prepare") && memory.Tree.PendingSwitch!.PreparationDay == today - 1);
            player.Items[2] = ItemRegistry.Create("(O)286", 2);
            Check("Explicit qualified preparation records current day and time", Act("prepare") && memory.Tree.PendingSwitch!.PreparationDay == today && memory.Tree.PendingSwitch.PreparationTime == 1000);
            memory.Tree.RecordMineVisit(today, 1000);
            Check("Mine entry at preparation time is not later proof", memory.Tree.PendingSwitch!.VerifiedDays.Count == 1 && Choice("finish") == null);
            memory.Tree.RecordMineVisit(today, 1010);
            Check("Verified visits keep old approach until explicit return", memory.Tree.Approach == "safe" && Act("finish") && memory.Tree.Approach == "bold" && !memory.Tree.OutstandingSwitch && player.questLog.Count == 0 && memory.Tree.LastKitDay == today);

            memory = Fresh(); Inventory();
            Check("Bold kit grants two Cherry Bombs and five Torches", Act("choose/bold") && Act("kit") && Count("(O)286") == 2 && Count("(O)93") == 5 && Count("(O)403") == 0 && memory.Tree.LastKitDay == today);
            foreach (string promiseId in new[] { "quartz", "iron" })
            {
                memory = Fresh(); Inventory(ItemRegistry.Create("(O)80", 2));
                memory.Tree.ChooseApproach("safe", today);
                string staleKit = Choice("kit")!;
                var ledger = new PromiseLedger(); ledger.ObserveMaterials(new[] { "quartz", "iron" });
                ledger.Offer("fish", today - 3); ledger.Accept("fish", today - 3, 0, false);
                ledger.Complete("fish", "(O)131", "Sardine", today - 3);
                ledger.Offer(promiseId, today - 2); ledger.Accept(promiseId, today - 2, 1, false);
                ledger.AdvanceDay(today); memory.Promises = ledger; Call(service, "Observe");
                int until = today + (promiseId == "iron" ? 3 : 1);
                Check(promiseId + " failure pauses native practical service choices", memory.Tree.CoolingUntilDay == until
                    && Choice("prepare") == null && Choice("exchange/(O)80") == null && Choice("flute") == null && Choice("kit") == null
                    && Call(service, "ApplyChoice", staleKit) == null && Count("(O)403") == 0);
                var abandonedLedger = JsonSerializer.Deserialize<PromiseLedger>(JsonSerializer.Serialize(ledger))!;
                var abandonedTree = JsonSerializer.Deserialize<RelationshipTreeState>(JsonSerializer.Serialize(memory.Tree))!;
                abandonedLedger.Abandon(promiseId, today + 1);
                abandonedTree.Observe(abandonedLedger, today + 1, Array.Empty<int>());
                Check(promiseId + " abandonment after overdue does not restart cooling", abandonedTree.CoolingUntilDay == until);
                Check(promiseId + " negative trust remains paused after its cooling date", abandonedTree.HelpStatus(abandonedLedger, until + 10).Paused);
                ledger.Complete(promiseId, PromiseLedger.Definition(promiseId)!.ItemId, promiseId, today);
                Call(service, "Observe");
                Check(promiseId + " same-day repair retains the pause and earned milestones", ledger.Score >= 0 && memory.Tree.CoolingUntilDay == until
                    && Choice("prepare") == null && Choice("exchange/(O)80") == null && Choice("flute") == null && Choice("kit") == null
                    && memory.Tree.Unlocked.Contains("fork") && memory.Tree.Unlocked.Contains("flute"));
                memory.Tree.Observe(ledger, until, Array.Empty<int>());
                Check(promiseId + " repaired trust resumes help after the required mornings", memory.Tree.HelpStatus(ledger, until - 1).Paused && !memory.Tree.HelpStatus(ledger, until).Paused);
            }

            memory = Fresh(); Inventory(); player.Stamina = 50; Game1.timeOfDay = 1000;
            string flute = Choice("flute")!;
            Check("Flute grants thirty energy and advances twenty native minutes", Call(service, "ApplyChoice", flute) != null && Math.Abs(player.Stamina - 80) < 0.1f && Game1.timeOfDay == 1020 && memory.Tree.LastFluteDay == today);
            Check("Duplicate flute action grants no energy or time", Call(service, "ApplyChoice", flute) == null && Math.Abs(player.Stamina - 80) < 0.1f && Game1.timeOfDay == 1020);
            Call(service, "StopMusic");
            Check("Tree survives JSON save serialization", JsonSerializer.Deserialize<AbigailMemory>(JsonSerializer.Serialize(memory)) is { } restored && restored.IsValid() && restored.Tree.LastFluteDay == today && restored.Tree.Unlocked.SetEquals(memory.Tree.Unlocked));
            Check("Native friendship points, gifts and talked status stay unchanged", (friendship?.Points ?? 0) == points && (friendship?.GiftsToday ?? 0) == gifts && (friendship?.TalkedToToday ?? false) == talked);
        }
        catch (Exception ex)
        {
            results.Add(new { Name = "Tree harness completed", Passed = false, Error = ex.GetBaseException().Message });
            throw;
        }
        finally
        {
            Call(service, "StopMusic"); memoryField.SetValue(observer, savedMemory);
            player.Items.Clear(); foreach (var item in items) player.Items.Add(item);
            player.questLog.Clear(); foreach (var quest in quests) player.questLog.Add(quest);
            player.activeDialogueEvents.Clear(); foreach (var entry in activeEvents) player.activeDialogueEvents[entry.Key] = entry.Value;
            player.currentLocation = location; player.Position = position; player.CurrentToolIndex = selected;
            player.CanMove = canMove; player.Stamina = stamina; Game1.timeOfDay = time;
            Game1.activeClickableMenu = menu; Game1.dialogueUp = dialogueUp;
            Game1.hudMessages.Clear(); Game1.hudMessages.AddRange(hud);
            if (friendship != null) { friendship.Points = points; friendship.GiftsToday = gifts; friendship.TalkedToToday = talked; }
            else player.friendshipData.Remove("Abigail");
            if (Game1.currentSong?.Name != song) Game1.changeMusicTrack(song ?? "none");
            Helper.Data.WriteJsonFile("tree-results.json", new { Checks = results, FixtureRestored = true, ClockNote = "Flute invokes real native clock callbacks; ordinary world clock effects may occur." });
        }
    }
}
