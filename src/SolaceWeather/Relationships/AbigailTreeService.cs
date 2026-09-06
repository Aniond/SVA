using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using SolaceWeather.Controls;
using SolaceWeather.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Quests;
using StardewValley.Tools;

namespace SolaceWeather.Relationships;

internal sealed record PreparationReport(bool Weapon, bool Food, bool Torch, bool CherryBomb, int EnergyPercent, int Time)
{
    internal bool Meets(string approach) => Weapon && Torch && (approach == "bold" ? CherryBomb : Food);
    public override string ToString() => $"Weapon: {Weapon}; food: {Food}; torch: {Torch}; Cherry Bomb: {CherryBomb}; energy: {EnergyPercent}%; time: {Time}. Inventory and visits do not prove fighting or success.";
}

internal sealed class AbigailTreeService
{
    private const string SwitchQuestId = "David.SolaceWeather/ApproachSwitch";
    private readonly AbigailRelationship relationship;
    private readonly IMonitor monitor;
    private Action? stopFlute;
    private DateTime musicEnds;
    private AbigailMemory Memory => relationship.Memory!;
    internal RelationshipTreeState State => Memory.Tree;
    internal bool Enabled => relationship.Ready && Game1.version == "1.6.15";
    private int Today => Game1.Date.TotalDays;

    internal AbigailTreeService(IModHelper helper, IMonitor monitor, AbigailRelationship relationship)
    {
        this.relationship = relationship; this.monitor = monitor; relationship.TreeService = this;
        helper.Events.GameLoop.SaveLoaded += (_, _) => { StopMusic(); Observe(); };
        helper.Events.GameLoop.DayStarted += (_, _) => Observe();
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => StopMusic();
        helper.Events.GameLoop.UpdateTicked += (_, _) => { if (stopFlute != null && DateTime.UtcNow >= musicEnds) StopMusic(); };
        helper.Events.Player.Warped += (_, e) => {
            if (!e.IsLocalPlayer || !Enabled) return;
            if (e.NewLocation is MineShaft { mineLevel: > 0 }) State.RecordMineVisit(Today, Game1.timeOfDay);
            Observe();
        };
    }

    internal void Observe()
    {
        if (!Enabled) return;
        State.Observe(Memory.Promises, Today, Memory.Personal.Activities.Where(a => a.FirstMineVisitTime != null).Select(a => a.Day));
        EnsureSwitchQuest();
    }
    internal PreparationReport Preparation() => new(
        Game1.player.Items.OfType<MeleeWeapon>().Any(w => !w.isScythe()),
        Game1.player.Items.OfType<StardewValley.Object>().Any(o => o.Stack > 0 && o.Edibility > 0),
        Has("(O)93"), Has("(O)286"), (int)(100 * Game1.player.Stamina / Math.Max(1, Game1.player.MaxStamina)), Game1.timeOfDay);
    private bool Has(string id) => Game1.player.Items.Any(i => i?.QualifiedItemId == id && i.Stack > 0);
    private StardewValley.Object? Item(string id, int count, bool consume) => Game1.player.Items.OfType<StardewValley.Object>()
        .FirstOrDefault(i => i.QualifiedItemId == id && i.Stack >= count && (!consume || !i.questItem.Value && !i.modData.ContainsKey(QuickStack.ProtectedKey)));
    private Dictionary<Item, int>? ExchangeItems(string id)
    {
        int needed = 2;
        var selected = new Dictionary<Item, int>();
        foreach (var item in Game1.player.Items.OfType<StardewValley.Object>().Where(i => i.QualifiedItemId == id && i.Stack > 0 && !i.questItem.Value && !i.modData.ContainsKey(QuickStack.ProtectedKey)))
        {
            int take = Math.Min(needed, item.Stack); selected.Add(item, take); needed -= take;
            if (needed == 0) return selected;
        }
        return null;
    }
    private string Token() => Today + ":" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(State) + JsonSerializer.Serialize(Memory.Promises))))[..12];
    private bool Nearby() => Enabled && !Game1.eventUp && Game1.currentMinigame == null
        && Game1.getCharacterFromName("Abigail") is { } npc && npc.currentLocation == Game1.player.currentLocation
        && Vector2.Distance(npc.Tile, Game1.player.Tile) <= 3;

    internal QuestChoice[] Choices()
    {
        if (!Enabled) return Array.Empty<QuestChoice>();
        Observe();
        var rows = new List<QuestChoice>();
        string prefix = "tree:" + Token() + ":";
        void Add(string action, string label) => rows.Add(new(prefix + action, label));
        bool Help = !State.HelpStatus(Memory.Promises, Today).Paused;
        if (State.Unlocked.Contains("study") && State.Studies.All(s => s.Day != Today))
            foreach (string id in RelationshipTreeState.SupportedMinerals.Where(id => State.Studies.All(s => s.ItemId != id)))
                if (Item(id, 1, false) is { } item) Add("study/" + id, "Show Me What You Found: " + item.DisplayName + " (keep it)");
        if (State.Unlocked.Contains("personal"))
            foreach (var theme in new[] { "independence", "uncertainty", "music and games" }) Add("personal/" + theme, "No Need to Pretend: " + theme);
        if (State.OutstandingSwitch)
        {
            Add("cancel", "Cancel A Different Kind of Adventure");
            if (State.PendingSwitch!.VerifiedDays.Count == 2) Add("finish", "I've tried both days. Let's change our approach.");
        }
        if (!Help) return rows.ToArray();
        if (State.Unlocked.Contains("planning")) Add("prepare", State.OutstandingSwitch ? "Prepare for A Different Kind of Adventure" : "No More Daydreaming: plan a mine trip");
        if (State.CanClaim("exchange", Today))
            foreach (var study in State.Studies)
                if (ExchangeItems(study.ItemId) != null) Add("exchange/" + study.ItemId, "Trade 2 " + ItemRegistry.Create(study.ItemId).DisplayName + " for 1 Geode");
        if (State.CanClaim("flute", Today) && Game1.timeOfDay <= 2340) Add("flute", "Quiet Notes: 20 minutes, restore 30 energy");
        if (State.Unlocked.Contains("fork") && State.Approach == "none")
        {
            Add("choose/safe", "Choose Better Safe Than Sorry"); Add("choose/bold", "Choose Let's Make Some Noise");
        }
        if (State.CanClaim("kit", Today)) Add("kit", State.Approach == "safe" ? "Ask for 2 Field Snacks and 5 Torches" : "Ask for 2 Cherry Bombs and 5 Torches");
        if (State.Approach != "none" && !State.OutstandingSwitch && Memory.Promises.Outstanding == null)
            Add("switch", "A Different Kind of Adventure: earn the other approach");
        return rows.ToArray();
    }

    internal QuestActionResult? ApplyChoice(string key)
    {
        if (!Nearby()) return null;
        var choice = Choices().FirstOrDefault(c => c.Key == key);
        if (choice.Key == null) return null;
        string action = key[(key.LastIndexOf(':') + 1)..];
        // Qualified item IDs contain no colon. Never accept an arbitrary action not in the current choices.
        string fact, fallback;
        bool ok;
        if (action.StartsWith("study/"))
        {
            string id = action[6..]; var item = Item(id, 1, false); if (item == null) return null;
            ok = State.Study(id, Today); fact = $"The farmer just showed Abigail {item.DisplayName}. She examined it NOW; the farmer kept the item. This is a mineral study, not a gift or delivery.";
            fallback = $"Let me see that {item.DisplayName}. There's something fascinating about what comes out of the ground.";
        }
        else if (action.StartsWith("exchange/"))
        {
            var items = ExchangeItems(action[9..]); if (items == null) return null;
            var reward = ItemRegistry.Create("(O)535");
            if (!CanReceive(new[] { reward }, items)) return Full();
            ok = State.Claim("exchange", Today); if (!ok) return null;
            foreach (var pair in items) Game1.player.Items.Reduce(pair.Key, pair.Value); Give(new[] { reward });
            fact = "The game just exchanged two pieces of " + ItemRegistry.Create(action[9..]).DisplayName + " for one Geode with Abigail. The inventory transfer happened; this is not a promise or ordinary gift.";
            fallback = "Two familiar little treasures for something mysterious. Sounds like a fair trade to me.";
        }
        else if (action == "prepare")
        {
            var report = Preparation();
            bool recorded = State.OutstandingSwitch && State.RecordPreparation(Today, Game1.timeOfDay, report.Meets(State.PendingSwitch!.Target));
            ok = true; fact = "Abigail and the farmer just reviewed a mine-trip preparation checklist: " + report + $" Switch preparation recorded: {recorded}. No mine visit has happened as part of this action. A later entry on this game day is required; preparations from yesterday do not count.";
            var missing = new List<string>();
            if (!report.Weapon) missing.Add("a weapon");
            if (!report.Torch) missing.Add("a torch");
            if (State.PendingSwitch?.Target == "bold") { if (!report.CherryBomb) missing.Add("a Cherry Bomb"); }
            else if (!report.Food) missing.Add("something to eat");
            fallback = missing.Count > 0 ? "Before you go, you're still missing " + string.Join(", ", missing) + "."
                : recorded ? "That checks out for our new approach. Head into the mines later today, then come back and tell me about it."
                : "You've got the supplies we checked. Keep an eye on your energy and the time, too.";
            if (report.EnergyPercent < 40) fallback += " You look tired. Take a rest or eat something first.";
            if (report.Time >= 1800) fallback += " It's getting late for a mine trip.";
        }
        else if (action.StartsWith("personal/"))
        {
            ok = true; fact = "The farmer explicitly opened the earned personal conversation theme: " + action[9..] + ". Be candid within Abigail's personality and verified shared history. This does not establish new story events, romance, a new promise, or progress merely for speaking.";
            fallback = "It's nice being able to say what I think without someone deciding who I'm supposed to be.";
        }
        else if (action == "flute")
        {
            ok = State.Claim("flute", Today); if (!ok) return null;
            Game1.player.Stamina = Math.Min(Game1.player.MaxStamina, Game1.player.Stamina + 30);
            Game1.performTenMinuteClockUpdate(); Game1.performTenMinuteClockUpdate();
            PlayFlute();
            fact = "Abigail JUST played a short flute break for the farmer. The game advanced twenty minutes normally and restored up to thirty energy. This service is complete once; no additional reward or time change is permitted.";
            fallback = "There. A little music and a moment to breathe. You don't have to rush every second of the day.";
        }
        else if (action == "kit")
        {
            Item[] reward = { ItemRegistry.Create(State.Approach == "safe" ? "(O)403" : "(O)286", 2), ItemRegistry.Create("(O)93", 5) };
            if (!CanReceive(reward)) return Full();
            ok = State.Claim("kit", Today); if (!ok) return null; Give(reward);
            fact = $"Abigail just handed over the earned {ApproachName(State.Approach)} kit: " + string.Join(", ", reward.Select(i => i.Stack + " " + i.DisplayName)) + ". The game added these items once. The seven-day kit cooldown has started.";
            fallback = "Here, I've got a few things for your next trip. Try to bring yourself back in one piece, okay?";
        }
        else if (action.StartsWith("choose/"))
        {
            ok = State.ChooseApproach(action[7..], Today); fact = "The farmer explicitly chose " + ApproachName(State.Approach) + ". No kit was claimed by choosing.";
            fallback = "All right, that sounds like your kind of adventure. We'll prepare that way.";
        }
        else if (action == "switch")
        {
            ok = State.BeginSwitch(State.Approach == "safe" ? "bold" : "safe", Today, Memory.Promises.Outstanding != null);
            fact = "The farmer accepted A Different Kind of Adventure. Before changing approach, record qualified preparation then a later mine entry on each of two different days, then return. The current approach stays active; no deadline or trust penalty.";
            fallback = "Let's try preparing the other way a couple of times first. Come see me before you head into the mines.";
        }
        else if (action == "finish")
        {
            ok = State.FinishSwitch(Today); fact = "The game verified two prepared mine-visit days and the farmer just returned to finish the approach change: " + ApproachName(State.Approach) + ". Existing kit cooldown is unchanged.";
            fallback = "You've given it a real try. Let's make that our approach from here on.";
        }
        else if (action == "cancel")
        {
            ok = State.CancelSwitch(); fact = "The farmer cancelled the open-ended approach-change milestone. Original approach retained; no trust loss.";
            fallback = "That's fine. We can stick with what works for you.";
        }
        else return null;
        if (!ok) return null;
        if (!action.StartsWith("personal/"))
        {
            string id = action.StartsWith("study/") ? "study:" + action[6..] : action.StartsWith("exchange/") ? "exchange" : action;
            if (action == "prepare") id += State.PendingSwitch == null ? ":review" : Preparation().Meets(State.PendingSwitch.Target) ? ":qualified" : ":missing";
            string kind = action is "flute" ? "music" : action.StartsWith("study/") || action.StartsWith("exchange/") ? "curiosity" : "adventure";
            string topics = kind == "music" ? "music flute quiet tired rest comfort" : kind == "curiosity" ? "minerals discovery curiosity geode" : "mines adventure preparation supplies independence";
            string remembered = action.StartsWith("study/") ? "Abigail examined " + ItemRegistry.Create(action[6..]).DisplayName + " with the farmer. The farmer kept it; this was a study, not a gift."
                : action.StartsWith("exchange/") ? "The farmer traded two pieces of " + ItemRegistry.Create(action[9..]).DisplayName + " to Abigail for one Geode."
                : action == "flute" ? "Abigail played a short flute break. The farmer rested for twenty minutes and recovered up to thirty energy."
                : action == "kit" ? "Abigail gave the farmer " + (State.Approach == "safe" ? "two Field Snacks" : "two Cherry Bombs") + " and five Torches for future preparations."
                : action == "prepare" ? "Abigail and the farmer reviewed mine preparations: " + Preparation()
                : action.StartsWith("choose/") ? "The farmer chose " + ApproachName(State.Approach) + " as their approach to preparing for adventures."
                : action == "switch" ? "The farmer began A Different Kind of Adventure, to try preparing differently before changing approach."
                : action == "finish" ? "After two verified prepared mine-visit days, the farmer returned and changed approach to " + ApproachName(State.Approach) + ". Abigail did not accompany these mine visits."
                : "The farmer cancelled A Different Kind of Adventure and kept the existing approach. There was no trust penalty.";
            Memory.Experiences.Record(id, kind, Today, remembered, topics);
        }
        Observe();
        return new QuestActionResult(choice.Label, fact, fallback);
    }

    private QuestActionResult? Full() { Game1.addHUDMessage(new HUDMessage("Make room for the whole reward first. Nothing has been exchanged or claimed.", HUDMessage.error_type)); return null; }
    private static void Give(IEnumerable<Item> items) { foreach (var item in items) Game1.player.addItemToInventoryBool(item); }
    private static bool CanReceive(IEnumerable<Item> rewards, Dictionary<Item, int>? remove = null)
    {
        var inventory = Game1.player.Items.Select(i => { if (i == null) return null; var copy = i.getOne(); copy.Stack = i.Stack - (remove != null && remove.TryGetValue(i, out int count) ? count : 0); return copy.Stack > 0 ? copy : null; }).ToList();
        while (inventory.Count < Game1.player.MaxItems) inventory.Add(null);
        foreach (var reward in rewards)
        {
            int remaining = reward.Stack;
            foreach (var stack in inventory.Where(i => i != null && i.canStackWith(reward))) { int add = Math.Min(remaining, Math.Max(0, stack!.maximumStackSize() - stack.Stack)); stack.Stack += add; remaining -= add; }
            while (remaining > 0)
            {
                int empty = inventory.FindIndex(i => i == null); if (empty < 0) return false;
                var copy = reward.getOne(); copy.Stack = Math.Min(remaining, Math.Max(1, reward.maximumStackSize())); inventory[empty] = copy; remaining -= copy.Stack;
            }
        }
        return true;
    }
    private void PlayFlute()
    {
        StopMusic();
        var song = Game1.currentSong; bool resume = song?.IsPlaying == true;
        try {
            if (resume) song!.Pause();
            var cue = Game1.soundBank.GetCue("AbigailFlute"); cue.Play();
            stopFlute = () => { cue.Stop(AudioStopOptions.Immediate); if (resume && ReferenceEquals(Game1.currentSong, song) && song!.IsPaused) song.Resume(); };
            musicEnds = DateTime.UtcNow.AddSeconds(12);
        } catch {
            if (resume && ReferenceEquals(Game1.currentSong, song) && song!.IsPaused) song.Resume();
            monitor.Log("Quiet Notes completed, but flute audio was unavailable.", LogLevel.Trace);
        }
    }
    private void StopMusic() { var stop = stopFlute; stopFlute = null; if (stop != null) { try { stop(); } catch { } } }
    private void EnsureSwitchQuest()
    {
        var quest = Game1.player.questLog.FirstOrDefault(q => q.id.Value == SwitchQuestId);
        if (State.PendingSwitch is not { } pending) { if (quest != null) Game1.player.questLog.Remove(quest); return; }
        if (quest == null) { quest = new Quest(); quest.id.Value = SwitchQuestId; quest.questType.Value = Quest.type_basic; quest.accepted.Value = true; quest.canBeCancelled.Value = false; Game1.player.questLog.Add(quest); }
        quest.questTitle = "A Different Kind of Adventure";
        quest.questDescription = "Prepare with Abigail, then enter a mine later that day. Do this on two different days and return. " + (pending.Target == "safe" ? "Carry a weapon, food and a torch." : "Carry a weapon, Cherry Bomb and a torch.") + " No deadline. Cancel through conversation without a penalty.";
        quest.currentObjective = $"Prepared mine-visit days: {pending.VerifiedDays.Count}/2. Return to Abigail when ready.";
    }
    internal static string ApproachName(string value) => value == "safe" ? "Better Safe Than Sorry" : value == "bold" ? "Let's Make Some Noise" : "Not chosen";
    internal object GetContext()
    {
        Observe(); var help = State.HelpStatus(Memory.Promises, Today);
        return new { Milestones = RelationshipTreeState.Definitions.Where(d => State.Unlocked.Contains(d.Id)).Select(d => new { d.Id, d.Title, d.Perk }).ToArray(),
            PreparationFindings = State.Unlocked.Contains("planning") ? Preparation() : null,
            Approach = ApproachName(State.Approach), CoolingOff = help.Paused, CoolingReason = help.Description,
            AvailableAgain = help.UntilDay.HasValue ? AbigailDeliveryQuest.DateLabel(help.UntilDay.Value) : null,
            AvailableServices = Choices().Select(c => c.Label).ToArray(), Studies = State.Studies.Select(s => new { Mineral = ItemRegistry.Create(s.ItemId).DisplayName, s.Day }).ToArray(),
            Switch = State.PendingSwitch == null ? null : new { Target = ApproachName(State.PendingSwitch.Target), Progress = State.PendingSwitch.VerifiedDays.Count },
            Rule = "Only game-confirmed actions unlock or grant perks. Time alone does not erase trust loss. Cooling off can continue after repair. No shared mine adventures are proven." };
    }
    internal RelationshipTreeView View()
    {
        Observe(); var help = State.HelpStatus(Memory.Promises, Today);
        string status = help.Paused ? "Needs some time. " + help.Description + (help.UntilDay is int until ? " Earliest: " + AbigailDeliveryQuest.DateLabel(until) + ". Trust must also recover." : "") : "Ask about perks when talking beside Abigail. New mineral study: one per day.";
        string Availability(string id) {
            if (!State.Unlocked.Contains(id)) return "Locked";
            if ((id is "safe" or "bold") && State.Approach != id) return "Remembered / inactive";
            if (help.Paused && id is "planning" or "exchange" or "flute" or "safe" or "bold") return "Paused";
            int? last = id == "exchange" ? State.LastExchangeDay : id == "flute" ? State.LastFluteDay : id is "safe" or "bold" ? State.LastKitDay : null;
            return last is int claim && Today < claim + 7 ? "Available " + AbigailDeliveryQuest.DateLabel(claim + 7) : "Unlocked";
        }
        string history = string.Join("\n\n", Memory.Promises.Records.SelectMany(p => p.History).OrderBy(h => h.Day).TakeLast(12).Select(h => AbigailDeliveryQuest.DateLabel(h.Day) + ": " + h.Text));
        history += "\n\n" + string.Join("\n", State.Studies.Select(s => "Studied " + ItemRegistry.Create(s.ItemId).DisplayName + " on " + AbigailDeliveryQuest.DateLabel(s.Day)));
        history += "\n\n" + string.Join("\n", Memory.Personal.Details.TakeLast(5).Select(d => "You told her: " + d.Quote));
        history += "\n\nShared experiences\n" + string.Join("\n\n", Memory.Experiences.Entries.OrderByDescending(e => e.LastDay).Select(e =>
            AbigailDeliveryQuest.DateLabel(e.LastDay) + ": " + e.Fact + "\n" + string.Join("\n", e.Conversations.TakeLast(1).Select(c => "You said: " + c.Farmer + "\nAbigail said: " + c.Reply))));
        string promises = string.Join("\n\n", Memory.Promises.Records.Select(p => PromiseLedger.Definition(p.Id)!.Name + ": " + p.Status + ". " + (p.DueDay is int due ? "Agreed day: " + AbigailDeliveryQuest.DateLabel(due) : "Open-ended") + (p.CompletedDay is int completed ? ". Delivered " + p.DeliveredItemName + " on " + AbigailDeliveryQuest.DateLabel(completed) : "")));
        if (State.PendingSwitch is { } pending) promises += $"\n\nA Different Kind of Adventure: {pending.VerifiedDays.Count}/2 prepared days. Target: {ApproachName(pending.Target)}. No deadline.";
        return new(RelationshipTreeState.Definitions.Select(d => new TreeCardView(d.Id, d.Title, d.Branch, d.Requirement, d.Perk, Availability(d.Id), d.ParentIds)).ToArray(), Memory.Promises.TrustDescription, status, history, promises, ApproachName(State.Approach));
    }
    internal void OpenTree() { if (Enabled) Game1.activeClickableMenu = new RelationshipTreeMenu(View); }
}
