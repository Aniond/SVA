using SolaceWeather.Core;
using StardewModdingAPI;
using StardewValley;

namespace SolaceWeather.Relationships;

internal sealed partial class EmilyLifeService
{
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly RomanceService romance;
    private readonly Func<AbigailMemory?> getMemory;
    private readonly Func<bool> otherOutingReserved;
    private EmilyLifeState? state;
    private string? offeredReminder;
    private static int Today => Game1.Date.TotalDays;
    private static int Minute => Game1.timeOfDay / 100 * 60 + Game1.timeOfDay % 100;
    internal bool Ready => romance.Ready && state != null && getMemory() != null;
    internal EmilyLifeState? State => state;
    internal bool HasReservation => Ready && (movementNpc != null || state!.Session.Status is "accepted" or "active");
    internal EmilyLifeService(IModHelper helper, IMonitor monitor, RomanceService romance, Func<AbigailMemory?> getMemory, Func<bool> otherOutingReserved)
    {
        this.helper = helper; this.monitor = monitor; this.romance = romance; this.getMemory = getMemory; this.otherOutingReserved = otherOutingReserved;
        helper.Events.GameLoop.SaveLoaded += (_, _) => Load();
        helper.Events.GameLoop.Saving += (_, _) =>
        {
            if (state?.Session.Status == "active") state.Session.Interrupt(); CleanupDesign(); CleanupMovement();
            if (Ready && state!.IsValid(Game1.player.UniqueMultiplayerID)) helper.Data.WriteSaveData("emily-life", state);
        };
        helper.Events.GameLoop.DayStarted += (_, _) => { if (Ready) { ObservePromise(); state!.Session.CheckTime(Today, Minute); EnsureQuests(); } };
        helper.Events.GameLoop.DayEnding += (_, _) => { if (Ready) state!.Session.CheckTime(Today, 1560); CleanupDesign(); CleanupMovement(); };
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => { CleanupDesign(); CleanupMovement(); state = null; offeredReminder = null; };
        helper.Events.Player.InventoryChanged += (_, e) => { if (e.IsLocalPlayer) ObservePromise(); };
        helper.Events.Player.Warped += (_, e) => { if (e.IsLocalPlayer && state?.Session.Status == "active") { state.Session.Interrupt(); CleanupDesign(); EnsureQuests(); } };
        helper.Events.GameLoop.UpdateTicked += (_, e) => { if (e.IsMultipleOf(15)) TickDesign(); TickMovement(); };
        helper.Events.Display.RenderedWorld += (_, e) => { DrawDesignPreview(e.SpriteBatch); ObserveMovementFrame(); };
    }
    private void Load()
    {
        CleanupDesign(); CleanupMovement(); state = null;
        if (!romance.Ready) return;
        try
        {
            var loaded = helper.Data.ReadSaveData<EmilyLifeState>("emily-life");
            if (loaded != null && !loaded.IsValid(Game1.player.UniqueMultiplayerID)) throw new InvalidDataException();
            state = loaded ?? new() { FarmerId = Game1.player.UniqueMultiplayerID };
            if (state.Session.Status == "active") state.Session.Interrupt();
            state.Session.CheckTime(Today, Minute); ObservePromise(); FindDesignSpot(); RememberOutcomes(); EnsureQuests();
        }
        catch { state = null; monitor.Log("Emily's outing and promise data are unavailable; existing save data preserved.", LogLevel.Warn); }
    }
    internal object ContextFor(string name, bool phone)
    {
        if (!Ready || name != "Emily") return new { Available = false };
        var memory = getMemory()!; offeredReminder = null;
        bool reminderFree = state!.LastReminderDay != Today && memory.Personal.LastFollowUpDay != Today;
        if (reminderFree && memory.Personal.FollowUp(Today) == null && state.Promise.Status is "active" or "overdue") offeredReminder = "emily:cloth";
        RefreshTree();
        int? meetingDay = !phone && CanArrangeDesign() ? NextDesignDay() : null;
        return new
        {
            Available = true,
            AvailableRequests = !phone && state.Promise.CanOffer(Today) ? new[] { "emily-cloth" } : Array.Empty<string>(),
            Promise = state.Promise,
            TrustMeaning = state.Promise.Reliability < 0 ? "A specific cloth promise is unfinished. This is not evidence of cheating or a reason for a breakup."
                : state.Promise.Status == "completed" ? "The cloth promise was actually fulfilled. Respond to that specific follow-through, without automatic affection." : "No verified reason to judge reliability negatively.",
            OfferedReminder = offeredReminder == null ? null : new { Topic = offeredReminder, state.Promise.DueDay, state.Promise.Status },
            DesignSession = new { CanOffer = meetingDay.HasValue, Template = "emily-design", Date = meetingDay.HasValue ? AbigailDeliveryQuest.DateLabel(meetingDay.Value) : null,
                CurrentDate = AbigailDeliveryQuest.DateLabel(Today), Timing = meetingDay == Today ? "today" : meetingDay == Today + 1 ? "tomorrow" : "on the stated date",
                Meeting = "Town at 11 am, arrive by 11:30 am; a short design session finishing by noon.", state.Session.Status },
            ActivityEvidence = new { CurrentOuting = state.Session, MilestoneSessions = state.Tree.Designs,
                Rule = "MilestoneSessions retains the first two verified outfit-design sessions, not an exhaustive history. A completed CurrentOuting and verified shared experiences also support later sessions. Invitations do not erase prior sessions or prove a new session. A design discussion produces no garment, inventory item, or image." },
            Tree = new { state.Tree.Unlocked, PersonalReflectionUnlocked = state.Tree.Unlocked.Contains("personal"),
                Rule = "These are verified shared milestones. They do not imply dating, love, or knowledge of unseen heart events. Never call them levels or perks in dialogue." },
            Rule = "Only explicit game choices accept promises or sessions. Farmer claims cannot deliver cloth, finish a design session or make a garment. No gift, heart or romance reward is implied. Use only the offered reminder, at most once; never nag or assume an overdue plan succeeded. Spiritual meaning may be a personal belief, never a verified supernatural effect."
        };
    }
    internal void Reply(string name, ConversationReply reply)
    {
        if (!Ready || name != "Emily") return;
        if (reply.QuestRequest == "emily-cloth") state!.Promise.Offer(Today);
        if (reply.QuestRequest == "emily-design" && CanArrangeDesign() && NextDesignDay() is int day)
        {
            if (state!.Session.Status == "completed") state.Session = new();
            state!.Session.Offer(Today, day);
        }
        EnsureQuests();
    }
    internal void RememberReminder(string name, ConversationReply reply, string snapshot)
    {
        if (!Ready || name != "Emily" || reply.AskedTopic != "emily:cloth") return;
        try
        {
            var root = System.Text.Json.Nodes.JsonNode.Parse(snapshot);
            var context = root?["Relationship"] ?? root;
            if (context?["Day"]?.GetValue<int>() != Today || context?["EmilyLife"]?["OfferedReminder"]?["Topic"]?.GetValue<string>() != reply.AskedTopic) return;
            state!.LastReminderDay = Today; getMemory()!.Personal.LastFollowUpDay = Today;
        }
        catch (System.Text.Json.JsonException) { }
        catch (InvalidOperationException) { }
    }
    internal QuestChoice[] Choices(string name) => Ready && name == "Emily" ? PromiseChoices().Concat(DesignChoices()).ToArray() : Array.Empty<QuestChoice>();
    internal QuestActionResult? Apply(string name, string key)
    {
        if (!Ready || name != "Emily" || !BesideEmily()) return null;
        var result = key.StartsWith("emily:promise:", StringComparison.Ordinal) ? ApplyPromise(key)
            : key.StartsWith("emily:tree:", StringComparison.Ordinal) ? ApplyTree(key) : ApplyDesign(key);
        RememberOutcomes(); EnsureQuests(); return result;
    }
    private static bool BesideEmily()
    {
        var npc = Game1.getCharacterFromName("Emily");
        return npc?.currentLocation == Game1.player.currentLocation && Microsoft.Xna.Framework.Vector2.Distance(npc.Tile, Game1.player.Tile) <= 3
            && !Game1.eventUp && Game1.currentMinigame == null;
    }
    private void RememberOutcomes()
    {
        if (!Ready) return;
        var memory = getMemory()!;
        var promise = state!.Promise;
        if (promise.Status == "completed") memory.Experiences.Record("emily:promise:cloth", "promise", promise.CompletedDay!.Value,
            "The farmer explicitly gave Emily one " + promise.DeliveredItemName + " for her sewing and fabric work. " + (promise.WasLate ? "The delivery was late, but they followed through." : "They kept the accepted promise.")
            + " This does not prove a garment was made or a romantic commitment.", "cloth sewing fashion promise follow-through");
        if (state.Session.Status == "completed")
        {
            state.Tree.RecordDesign(state.Session.MeetingDay, state.Session.Mood, state.Session.Pattern);
            memory.Experiences.Record("emily:outing:design:" + state.Session.MeetingDay, "outing", state.Session.MeetingDay,
                $"The farmer and Emily completed their accepted morning outfit-design session in Town, exploring a {state.Session.Mood} mood and a {state.Session.Pattern} pattern. They shared and confirmed the design idea. No garment, inventory item, image or romantic commitment was created.", "fashion outfit design color pattern sewing creative shared morning");
        }
        RefreshTree();
    }
    private void RefreshTree()
    {
        if (Ready) state!.Tree.Refresh(state.Promise.Status == "completed", romance.State.Characters.GetValueOrDefault("Emily")?.TalkDays ?? 0);
    }
    internal void ObserveOutfit(string name, string fingerprint)
    {
        if (!Ready || name != "Emily" || !BesideEmily()) return;
        state!.Tree.ObserveOutfit(Today, fingerprint); RefreshTree();
    }
    private void EnsureQuests() { EnsurePromiseQuest(); EnsureDesignQuest(); }
}
