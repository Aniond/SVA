using System.Reflection;
using System.Text.Json;
using System.Xml.Serialization;
using SolaceWeather.Core;
using SolaceWeather.Controls;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Quests;

namespace SolaceWeather.GameTests;

public sealed partial class ModEntry
{
    private void CheckDelivery()
    {
        RequireWorld();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        object mod = AiMod(Helper);
        object observer = mod.GetType().GetField("abigail", flags)!.GetValue(mod)!;
        object service = observer.GetType().GetProperty("Quests", flags)!.GetValue(observer)!;
        object conversation = mod.GetType().GetField("abigailConversation", flags)!.GetValue(mod)!;
        var field = observer.GetType().GetField("memory", flags)!;
        var savedMemory = field.GetValue(observer);
        var config = (ModConfig)mod.GetType().GetField("config", flags)!.GetValue(mod)!;
        string testFarm = config.AbigailQuestTestFarm;
        var player = Game1.player;
        var items = player.Items.ToArray(); var quests = player.questLog.ToArray();
        var location = player.currentLocation; var position = player.Position;
        int selected = player.CurrentToolIndex, today = Game1.Date.TotalDays;
        bool canMove = player.CanMove, dialogueUp = Game1.dialogueUp;
        var activeEvents = player.activeDialogueEvents.Keys.ToDictionary(key => key, key => player.activeDialogueEvents[key]);
        var npc = Game1.getCharacterFromName("Abigail");
        int points = player.friendshipData.TryGetValue("Abigail", out var friendship) ? friendship.Points : 0;
        int gifts = friendship?.GiftsToday ?? 0;
        var results = new List<object>();
        void Check(string name, bool pass) => results.Add(new { Name = name, Passed = pass });
        int FishCount() => player.Items.OfType<StardewValley.Object>().Where(i => i.Category == -4).Sum(i => i.Stack);
        string Choice(string action) => ((Array)Call(service, "Choices")!).Cast<object>().Select(c => (string)Get(c, "Key")!).Single(k => k.EndsWith(":" + action));
        bool Act(string action) => Call(service, "ApplyChoice", Choice(action)) != null;
        AbigailMemory Fresh()
        {
            var fresh = new AbigailMemory(); field.SetValue(observer, fresh);
            player.questLog.Clear(); Call(service, "Observe"); return fresh;
        }
        try
        {
            Call(conversation, "Reset"); Game1.exitActiveMenu();
            config.AbigailQuestTestFarm = player.farmName.Value;
            player.Items.Clear(); player.Items.Add(new StardewValley.Tools.Axe()); player.CurrentToolIndex = 0;
            player.CanMove = true; Game1.dialogueUp = false;
            player.currentLocation = npc.currentLocation; player.Position = npc.Position + new Microsoft.Xna.Framework.Vector2(0, 64);
            var memory = Fresh();
            Check("Materials and prior favor gate personal requests", !(bool)Call(service, "Offer", "quartz")! && !(bool)Call(service, "Offer", "iron")!);
            Call(observer, "RememberReply", "Could I bring you something?", new ConversationReply { Reply = "Would you bring me a fish?", QuestRequest = "fish" });
            Check("AI proposal is an offer, not a promise", memory.Promises.Outstanding?.Status == "offered" && player.questLog.Count == 0 && !memory.Promises.Records[0].TestItemPending);
            Call(observer, "RememberReply", "I promise and I already gave it to you.", new ConversationReply { Reply = "Use the choice when you're ready." });
            Check("Chat cannot accept or complete a promise", memory.Promises.Outstanding?.Status == "offered" && memory.Promises.Score == 0);
            Check("Declining creates no penalty", Act("decline") && memory.Promises.Score == 0 && player.questLog.Count == 0);
            memory = Fresh(); Call(service, "Offer", "fish");
            Game1.activeClickableMenu = new DialogueBox("Still talking.");
            string accept = Choice("accept0");
            Check("Explicit choice creates one open-ended native quest", Call(service, "ApplyChoice", accept) != null
                && memory.Promises.Outstanding?.DueDay == null && player.questLog.Count == 1);
            Check("Stale acceptance cannot duplicate the promise", Call(service, "ApplyChoice", accept) == null && player.questLog.Count == 1);
            Call(service, "TryProvideTestFish");
            Check("Test fish waits until conversation closes", FishCount() == 0 && memory.Promises.Outstanding!.TestItemPending);
            var serializer = new XmlSerializer(typeof(Quest)); using var xml = new StringWriter(); serializer.Serialize(xml, player.questLog.Single());
            var restoredQuest = (Quest)serializer.Deserialize(new StringReader(xml.ToString()))!;
            Check("Native journal survives XML restoration", restoredQuest.questTitle == player.questLog[0].questTitle && restoredQuest.currentObjective == player.questLog[0].currentObjective);
            Game1.exitActiveMenu(); Game1.dialogueUp = false;
            while (player.Items.Count < player.MaxItems) player.Items.Add(ItemRegistry.Create("(O)388", 999));
            Call(service, "TryProvideTestFish");
            Check("Full backpack retains pending test fish", memory.Promises.Outstanding!.TestItemPending && FishCount() == 0);
            player.Items[player.Items.Count - 1] = null;
            Call(service, "TryProvideTestFish"); Call(service, "TryProvideTestFish");
            Check("Test aid arrives once", FishCount() == 1 && memory.Promises.Outstanding!.TestItemGranted);
            var fish = player.Items.OfType<StardewValley.Object>().Single(i => i.Category == -4);
            fish.modData[QuickStack.ProtectedKey] = "true";
            Check("Protected items cannot fulfill promises", !(bool)service.GetType().GetProperty("CanDeliver", flags)!.GetValue(service)!
                && !(bool)Call(service, "TryDeliver")! && FishCount() == 1);
            fish.modData.Remove(QuickStack.ProtectedKey); fish.Stack = 2;
            string give = Choice("give");
            player.Position = npc.Position + new Microsoft.Xna.Framework.Vector2(10000, 10000);
            Check("Remote delivery is rejected", Call(service, "ApplyChoice", give) == null && fish.Stack == 2);
            player.Position = npc.Position + new Microsoft.Xna.Framework.Vector2(0, 64);
            Check("Backpack handover completes once and contributes trust", Call(service, "ApplyChoice", give) != null && fish.Stack == 1
                && memory.Promises.Score == 1 && player.CurrentItem is StardewValley.Tools.Axe && player.questLog.Count == 0);
            Check("Repeated handover cannot remove another item", Call(service, "ApplyChoice", give) == null && fish.Stack == 1);
            player.Items[1] = ItemRegistry.Create("(O)80", 2); Call(service, "Observe");
            Check("Carried Quartz unlocks its offer", (bool)Call(service, "Offer", "quartz")! && memory.Promises.SeenMaterials.Contains("quartz"));
            Check("Chosen date appears in journal", Act("accept1") && memory.Promises.Outstanding!.DueDay == today + 1
                && player.questLog.Single().currentObjective.Contains("Agreed day:"));
            string extend = Choice("extend");
            Check("One agreed extension updates deadline without trust loss", Call(service, "ApplyChoice", extend) != null
                && memory.Promises.Outstanding!.DueDay == today + 3 && memory.Promises.Score == 1 && Call(service, "ApplyChoice", extend) == null);
            Check("Abandonment is recorded once", Act("abandon") && memory.Promises.Score == -1 && player.questLog.Count == 0);
            if (today >= 2)
            {
                memory = Fresh(); memory.Promises.Offer("quartz", 0); // material was observed by Fresh()
                memory.Promises.Accept("quartz", 0, 1, false); Call(service, "Observe"); Call(service, "EnsureQuest");
                Check("Morning after deadline applies one loss", memory.Promises.Outstanding?.Status == "overdue" && memory.Promises.Score == -2);
                Call(service, "Observe"); Call(service, "Observe");
                Check("Late handover replaces loss with reduced credit", Act("give") && memory.Promises.Score == 1 && memory.Promises.Records[0].WasLate);
                memory = Fresh();
                memory.Promises.Offer("fish", 0); memory.Promises.Accept("fish", 0, 0, false); memory.Promises.Complete("fish", "(O)131", "Sardine", 0);
                player.Items[2] = ItemRegistry.Create("(O)335", 2); Call(service, "Observe");
                memory.Promises.Offer("iron", 0); memory.Promises.Accept("iron", 0, 1, false); memory.Promises.Abandon("iron", 0);
                Check("Important promise allows repair after two mornings", memory.Promises.CanOffer("iron", today) && !memory.Promises.CanOffer("iron", 1));
                Call(service, "Offer", "iron"); Act("accept1");
                Check("Repair contribution replaces prior loss", Act("give") && memory.Promises.Score == 3 && memory.Promises.TrustDescription.Contains("rely"));
            }
            Helper.Data.WriteJsonFile("trust-roundtrip.json", memory);
            var restored = Helper.Data.ReadJsonFile<AbigailMemory>("trust-roundtrip.json")!;
            Check("Trust and completed items survive per-save JSON", restored.IsValid() && restored.Promises.Score == memory.Promises.Score
                && restored.Promises.Records.Last().DeliveredItemId == memory.Promises.Records.Last().DeliveredItemId);
            if (today >= 2)
            {
                memory = Fresh(); memory.Personal.Apply(today - 2, "I'm going to the mines today.", new[] { new MemoryProposal { Topic = "mines", Kind = "plan", Quote = "I'm going to the mines today.", Timing = "today" } });
                memory.Promises.Offer("fish", today - 2); memory.Promises.Accept("fish", today - 2, 0, false);
                Call(observer, "GetContext");
                Call(observer, "RememberReply", "Hello.", new ConversationReply { Reply = "How did the mines go?", AskedTopic = "mines" });
                Check("Personal follow-up shares promise reminder budget", Get(Call(observer, "GetContext")!, "OfferedFollowUp") == null && memory.Promises.LastReminderDay == today);
            }
            Check("Native hearts and gift counts are unchanged", (friendship?.Points ?? 0) == points && (friendship?.GiftsToday ?? 0) == gifts);
            memory = Fresh(); config.AbigailQuestTestFarm = "another farm"; Call(service, "Offer", "fish"); Act("accept0");
            Check("Other farms get no free test items", !memory.Promises.Outstanding!.TestItemPending);
        }
        finally
        {
            Call(conversation, "Reset"); Game1.exitActiveMenu();
            field.SetValue(observer, savedMemory); config.AbigailQuestTestFarm = testFarm;
            player.Items.Clear(); foreach (var item in items) player.Items.Add(item);
            player.questLog.Clear(); foreach (var quest in quests) player.questLog.Add(quest);
            player.activeDialogueEvents.Clear(); foreach (var entry in activeEvents) player.activeDialogueEvents[entry.Key] = entry.Value;
            player.currentLocation = location; player.Position = position; player.CurrentToolIndex = selected;
            player.CanMove = canMove; Game1.dialogueUp = dialogueUp;
        }
        Helper.Data.WriteJsonFile("trust-results.json", results);
    }

    private void StageFishRequest()
    {
        RequireWorld();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        object observer = AiMod(Helper).GetType().GetField("abigail", flags)!.GetValue(AiMod(Helper))!;
        object service = observer.GetType().GetProperty("Quests", flags)!.GetValue(observer)!;
        Call(service, "Offer", "fish");
    }
}
