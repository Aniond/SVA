using SolaceWeather.Controls;
using SolaceWeather.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Quests;

namespace SolaceWeather.Relationships;

internal readonly record struct QuestChoice(string Key, string Label);
internal readonly record struct QuestActionResult(string FarmerLine, string Fact, string Fallback);

/// <summary>Game-owned promise choices, inventory transfers, and native journal entries.</summary>
internal sealed class AbigailDeliveryQuest
{
    internal const string QuestId = "David.SolaceWeather/AbigailFish"; // legacy save entry
    private const string Prefix = "David.SolaceWeather/Promise/";
    private readonly AbigailRelationship relationship;
    private readonly ModConfig config;
    private bool fullInventoryNotice;
    private PromiseLedger Ledger => relationship.Promises!;

    internal AbigailDeliveryQuest(IModHelper helper, ModConfig config, AbigailRelationship relationship)
    {
        this.config = config;
        this.relationship = relationship;
        relationship.Quests = this;
        helper.Events.GameLoop.SaveLoaded += (_, _) => { fullInventoryNotice = false; Observe(); EnsureQuest(); };
        helper.Events.GameLoop.DayStarted += (_, _) => { Observe(); EnsureQuest(); };
        helper.Events.Player.InventoryChanged += (_, e) => { if (e.IsLocalPlayer) Observe(); };
        helper.Events.GameLoop.UpdateTicked += (_, _) => TryProvideTestFish();
    }

    internal bool Enabled => relationship.Ready && config.EnableAbigailAi && Game1.version == "1.6.15";
    internal bool CanRequestFish => Enabled && relationship.Memory?.Tree.OutstandingSwitch != true && Ledger.CanOffer("fish", Game1.Date.TotalDays);
    internal bool CanDeliver => Enabled && Ledger.Outstanding is { Status: "active" or "overdue" } request && FindItem(request.Id) != null;
    internal string? DeliveryLabel => CanDeliver ? "Give Abigail: " + FindItem(Ledger.Outstanding!.Id)!.DisplayName : null;
    internal string[] ItemTerms => !Enabled ? Array.Empty<string>() : Ledger.Records
        .SelectMany(r => new[] { PromiseLedger.Definition(r.Id)!.Name, r.DeliveredItemName, FindItem(r.Id)?.DisplayName ?? "" })
        .Concat(Ledger.Records.Any(r => r.Id == "fish") ? new[] { "fish", "fishes" } : Array.Empty<string>())
        .Where(s => s.Length > 0).Distinct().ToArray();

    internal void Observe()
    {
        if (!Enabled) return;
        Ledger.MigrateLegacy(relationship.Delivery!);
        Ledger.ObserveMaterials(Game1.player.Items.Where(i => i != null).Select(i => i.QualifiedItemId switch {
            "(O)80" => "quartz", "(O)335" => "iron", _ => "" }));
        Ledger.AdvanceDay(Game1.Date.TotalDays);
    }

    private static StardewValley.Object? FindItem(string id) => Game1.player.Items.OfType<StardewValley.Object>()
        .Where(item => item.Stack > 0 && !item.questItem.Value && !item.modData.ContainsKey(QuickStack.ProtectedKey)
            && (id == "fish" ? item.Category == StardewValley.Object.FishCategory : item.QualifiedItemId == PromiseLedger.Definition(id)?.ItemId))
        .OrderBy(item => item.Quality).ThenBy(item => item.Price).FirstOrDefault();

    internal bool Offer(string request)
    {
        if (!Enabled) return false;
        Observe();
        if (relationship.Memory?.Tree.OutstandingSwitch == true) return false;
        if (!Ledger.Offer(request, Game1.Date.TotalDays)) return false;
        Game1.addHUDMessage(new HUDMessage("Abigail has a request. Choose whether to make a promise."));
        return true;
    }

    internal QuestChoice[] Choices()
    {
        if (!Enabled || Ledger.Outstanding is not { } request) return Array.Empty<QuestChoice>();
        int today = Game1.Date.TotalDays;
        var rows = new List<QuestChoice>();
        string stamp = $"{request.Id}:{request.OfferedDay}:{request.AcceptedDay}:{request.DueDay}:{request.Status}";
        void Add(string action, string text) => rows.Add(new QuestChoice(stamp
            + (action == "give" ? ":" + FindItem(request.Id)!.QualifiedItemId + ":" + FindItem(request.Id)!.Quality : "") + ":" + action, text));
        string name = PromiseLedger.Definition(request.Id)!.Name;
        if (request.Status == "offered")
        {
            if (request.Id == "fish") Add("accept0", "I'll bring one fish when I can.");
            else
            {
                Add("accept1", $"I'll bring {name} tomorrow ({DateLabel(today + 1)}).");
                Add("accept3", $"Give me three days for {name} ({DateLabel(today + 3)}).");
            }
            Add("decline", "Not right now.");
        }
        else
        {
            if (DeliveryLabel is string delivery) Add("give", delivery);
            if (request.Status == "active" && request.DueDay >= today && !request.Extended)
                Add("extend", $"I need more time (until {DateLabel(request.DueDay!.Value + 2)}).");
            Add("abandon", "I can't follow through on this promise.");
        }
        return rows.ToArray();
    }

    internal QuestActionResult? ApplyChoice(string key)
    {
        if (!Enabled || Game1.eventUp || Game1.currentMinigame != null) return null;
        NPC? npc = Game1.getCharacterFromName("Abigail");
        if (npc?.currentLocation != Game1.player.currentLocation || Microsoft.Xna.Framework.Vector2.Distance(npc.Tile, Game1.player.Tile) > 3) return null;
        Observe();
        var choice = Choices().FirstOrDefault(c => c.Key == key);
        if (choice.Key == null || Ledger.Outstanding is not { } request) return null;
        string action = key.Split(':').Last();
        int day = Game1.Date.TotalDays;
        string name = PromiseLedger.Definition(request.Id)!.Name;
        bool ok;
        string fact, fallback;
        if (action.StartsWith("accept", StringComparison.Ordinal))
        {
            int delay = int.Parse(action[6..], System.Globalization.CultureInfo.InvariantCulture);
            ok = Ledger.Accept(request.Id, day, delay, !string.IsNullOrEmpty(config.AbigailQuestTestFarm) && config.AbigailQuestTestFarm == Game1.player.farmName.Value);
            fact = $"The player explicitly accepted the {name} promise NOW. Due: {(request.DueDay.HasValue ? DateLabel(request.DueDay.Value) : "open-ended; no deadline")}. This is not delivery.";
            fallback = "Thanks. I'll leave it to you, then.";
        }
        else if (action == "decline")
        {
            ok = Ledger.Decline(request.Id, day); fact = $"The player declined the {name} request NOW, without making a promise. No penalty.";
            fallback = "That's okay. I'd rather you tell me than feel pushed into it.";
        }
        else if (action == "extend")
        {
            ok = Ledger.Extend(request.Id, day); fact = $"The game agreed to a two-day extension NOW. The {name} promise is due {DateLabel(request.DueDay ?? day)}. No penalty.";
            fallback = "Thanks for telling me before I was counting on it. A little more time is fine.";
        }
        else if (action == "abandon")
        {
            ok = Ledger.Abandon(request.Id, day); fact = $"The player explicitly backed out of the {name} promise NOW. Use the recorded importance and trust response; do not add punishment.";
            fallback = request.Id == "fish" ? "All right, don't worry about the fish." : "I was counting on that. Thanks for telling me, but I need some time before I rely on another promise.";
        }
        else if (action == "give")
        {
            ok = TryDeliver(); fact = $"The player JUST handed over one {request.DeliveredItemName}. The game removed it from the backpack and completed the {name} promise NOW. Late/repair: {request.WasLate || request.AbandonedDay != null}. Do not treat it as an earlier handover or claim they caught/mined it.";
            fallback = request.WasLate || request.AbandonedDay != null ? "You came through after all. I appreciate you making it right." : "You remembered! Thanks for following through.";
        }
        else return null;
        if (!ok) return null;
        EnsureQuest();
        return new QuestActionResult(choice.Label, fact, fallback);
    }

    internal bool TryDeliver()
    {
        if (!CanDeliver || Game1.eventUp || Game1.currentMinigame != null) return false;
        NPC? npc = Game1.getCharacterFromName("Abigail");
        if (npc?.currentLocation != Game1.player.currentLocation || Microsoft.Xna.Framework.Vector2.Distance(npc.Tile, Game1.player.Tile) > 3) return false;
        var request = Ledger.Outstanding!;
        var item = FindItem(request.Id)!;
        if (!Ledger.Complete(request.Id, item.QualifiedItemId, item.DisplayName, Game1.Date.TotalDays)) return false;
        Game1.player.Items.Reduce(item, 1);
        foreach (var quest in Game1.player.questLog.Where(q => q.id.Value == Prefix + request.Id).ToArray()) quest.questComplete();
        Game1.addHUDMessage(new HUDMessage("Promise fulfilled: " + request.DeliveredItemName + "."));
        return true;
    }

    internal void EnsureQuest()
    {
        if (!Enabled) return;
        var request = Ledger.Outstanding;
        bool accepted = request?.Status is "active" or "overdue";
        foreach (var quest in Game1.player.questLog.Where(q => q.id.Value == QuestId || (q.id.Value?.StartsWith(Prefix) ?? false)).ToArray())
            if (!accepted || quest.id.Value != Prefix + request!.Id) Game1.player.questLog.Remove(quest);
        if (!accepted) return;
        var entry = Game1.player.questLog.FirstOrDefault(q => q.id.Value == Prefix + request!.Id);
        if (entry == null)
        {
            entry = new Quest();
            entry.id.Value = Prefix + request!.Id;
            entry.questType.Value = Quest.type_basic;
            entry.accepted.Value = true;
            entry.showNew.Value = true;
            entry.canBeCancelled.Value = false;
            Game1.player.questLog.Add(entry);
        }
        var definition = PromiseLedger.Definition(request!.Id)!;
        string deadline = request.DueDay.HasValue ? "Agreed day: " + DateLabel(request.DueDay.Value) + "." : "No deadline.";
        entry.questTitle = "A promise to Abigail: " + definition.Name;
        entry.questDescription = definition.Meaning + " Bring one " + definition.Name + ". " + deadline
            + (request.Status == "overdue" ? " She is still waiting; you can make it right." : "")
            + " Talk to Abigail and use the quest choices below chat. F9-protected items are kept.";
        entry.currentObjective = "Deliver one " + definition.Name + ". " + deadline;
    }

    internal void TryProvideTestFish()
    {
        if (!Enabled || Ledger.Outstanding is not { TestItemPending: true, Status: "active" or "overdue" } request
            || Game1.activeClickableMenu != null || Game1.dialogueUp || Game1.eventUp || Game1.currentMinigame != null || !Game1.player.CanMove) return;
        var fish = ItemRegistry.Create("(O)131");
        if (!Game1.player.couldInventoryAcceptThisItem(fish))
        {
            if (!fullInventoryNotice) Game1.addHUDMessage(new HUDMessage("Make room in your backpack for Abigail's test fish.", HUDMessage.error_type));
            fullInventoryNotice = true; return;
        }
        if (!Game1.player.addItemToInventoryBool(fish)) return;
        Ledger.MarkTestItemGranted(request.Id);
        fullInventoryNotice = false;
        Game1.addHUDMessage(new HUDMessage("Test fish added. Return to Abigail to turn it in."));
    }

    internal static string DateLabel(int day) => $"{new[] { "Spring", "Summer", "Fall", "Winter" }[(day % 112) / 28]} {day % 28 + 1}, Y{day / 112 + 1}";
}
