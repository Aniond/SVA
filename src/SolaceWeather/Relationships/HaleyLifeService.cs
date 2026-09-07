using SolaceWeather.Core;
using StardewModdingAPI;
using StardewValley;

namespace SolaceWeather.Relationships;

internal sealed partial class HaleyLifeService
{
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly RomanceService romance;
    private readonly Func<AbigailMemory?> getMemory;
    private readonly Func<bool> otherOutingReserved;
    private HaleyLifeState? state;
    private string? offeredReminder;
    private static int Today => Game1.Date.TotalDays;
    private static int Minute => Game1.timeOfDay / 100 * 60 + Game1.timeOfDay % 100;
    internal bool Ready => romance.Ready && state != null && getMemory() != null;
    internal HaleyLifeState? State => state;
    internal bool HasReservation => Ready && state!.Outing.Status is "accepted" or "active";
    internal HaleyLifeService(IModHelper helper, IMonitor monitor, RomanceService romance, Func<AbigailMemory?> getMemory, Func<bool> otherOutingReserved)
    {
        this.helper = helper; this.monitor = monitor; this.romance = romance; this.getMemory = getMemory; this.otherOutingReserved = otherOutingReserved;
        helper.Events.GameLoop.SaveLoaded += (_, _) => Load();
        helper.Events.GameLoop.Saving += (_, _) =>
        {
            if (state?.Outing.Status == "active") state.Outing.Interrupt(); CleanupPhoto();
            if (Ready && state!.IsValid(Game1.player.UniqueMultiplayerID)) helper.Data.WriteSaveData("haley-life", state);
        };
        helper.Events.GameLoop.DayStarted += (_, _) => { if (Ready) { ObservePromise(); state!.Outing.CheckTime(Today, Minute); EnsureQuests(); } };
        helper.Events.GameLoop.DayEnding += (_, _) => { if (Ready) state!.Outing.CheckTime(Today, 1560); CleanupPhoto(); };
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => { CleanupPhoto(); state = null; offeredReminder = null; };
        helper.Events.Player.InventoryChanged += (_, e) => { if (e.IsLocalPlayer) ObservePromise(); };
        helper.Events.Player.Warped += (_, e) => { if (e.IsLocalPlayer && state?.Outing.Status == "active") { state.Outing.Interrupt(); CleanupPhoto(); EnsureQuests(); } };
        helper.Events.GameLoop.UpdateTicked += (_, e) => { if (e.IsMultipleOf(15)) TickPhoto(); };
        helper.Events.Display.RenderedWorld += (_, e) => DrawShutter(e.SpriteBatch);
    }
    private void Load()
    {
        CleanupPhoto(); state = null;
        if (!romance.Ready) return;
        try
        {
            var loaded = helper.Data.ReadSaveData<HaleyLifeState>("haley-life");
            if (loaded != null && !loaded.IsValid(Game1.player.UniqueMultiplayerID)) throw new InvalidDataException();
            state = loaded ?? new() { FarmerId = Game1.player.UniqueMultiplayerID };
            if (state.Outing.Status == "active") state.Outing.Interrupt();
            state.Outing.CheckTime(Today, Minute); ObservePromise(); FindPhotoSpot(); RememberOutcomes(); EnsureQuests();
        }
        catch { state = null; monitor.Log("Haley's outing and promise data are unavailable; existing save data preserved.", LogLevel.Warn); }
    }
    internal object ContextFor(string name, bool phone)
    {
        if (!Ready || name != "Haley") return new { Available = false };
        var memory = getMemory()!; offeredReminder = null;
        bool reminderFree = state!.LastReminderDay != Today && memory.Personal.LastFollowUpDay != Today;
        if (reminderFree && memory.Personal.FollowUp(Today) == null && state.Promise.Status is "active" or "overdue") offeredReminder = "haley:sunflower";
        RefreshTree();
        int? night = !phone && CanArrangePhoto() ? NextPhotoDay() : null;
        return new
        {
            Available = true,
            AvailableRequests = !phone && state.Promise.CanOffer(Today) ? new[] { "haley-sunflower" } : Array.Empty<string>(),
            Promise = state.Promise,
            TrustMeaning = state.Promise.Reliability < 0 ? "A specific sunflower promise is unfinished. This is not evidence of cheating or a reason for a breakup."
                : state.Promise.Status == "completed" ? "The sunflower promise was actually fulfilled. Respond to that specific follow-through, without automatic affection." : "No verified reason to judge reliability negatively.",
            OfferedReminder = offeredReminder == null ? null : new { Topic = offeredReminder, state.Promise.DueDay, state.Promise.Status },
            PhotoWalk = new { CanOffer = night.HasValue, Template = "haley-photo", Date = night.HasValue ? AbigailDeliveryQuest.DateLabel(night.Value) : null,
                CurrentDate = AbigailDeliveryQuest.DateLabel(Today), Timing = night == Today ? "today" : night == Today + 1 ? "tomorrow" : "on the stated date",
                Meeting = "Town at 5 pm, arrive by 5:30 pm; a short photo session finishing by 6 pm.", state.Outing.Status },
            ActivityEvidence = new { CurrentOuting = state.Outing, MilestoneSessions = state.Tree.Photos,
                Rule = "MilestoneSessions retains the first three verified photo sessions; it is not an exhaustive history. A completed CurrentOuting and verified shared experiences also support later sessions. A current invitation does not erase previous sessions or prove a new shoot. No inventory photograph or generated image was created." },
            Tree = new { state.Tree.Unlocked, state.Tree.Approach, PersonalReflectionUnlocked = state.Tree.Unlocked.Contains("personal"),
                Rule = "These are verified shared milestones. They do not imply dating, love, or knowledge of unseen heart events. Never call them levels or perks in dialogue." },
            Rule = "Only explicit game choices accept promises or outings. Farmer claims cannot deliver items or complete a photo walk. No gift, heart or romance reward is implied. Use only the offered reminder, at most once; never nag or assume an overdue plan succeeded."
        };
    }
    internal void Reply(string name, ConversationReply reply)
    {
        if (!Ready || name != "Haley") return;
        if (reply.QuestRequest == "haley-sunflower") state!.Promise.Offer(Today);
        if (reply.QuestRequest == "haley-photo" && CanArrangePhoto() && NextPhotoDay() is int day)
        {
            if (state!.Outing.Status == "completed") state.Outing = new();
            state!.Outing.Offer(Today, day);
        }
        EnsureQuests();
    }
    internal void RememberReminder(string name, ConversationReply reply, string snapshot)
    {
        if (!Ready || name != "Haley" || reply.AskedTopic != "haley:sunflower") return;
        try
        {
            var root = System.Text.Json.Nodes.JsonNode.Parse(snapshot);
            var context = root?["Relationship"] ?? root;
            if (context?["Day"]?.GetValue<int>() != Today || context?["HaleyLife"]?["OfferedReminder"]?["Topic"]?.GetValue<string>() != reply.AskedTopic) return;
            state!.LastReminderDay = Today; getMemory()!.Personal.LastFollowUpDay = Today;
        }
        catch (System.Text.Json.JsonException) { }
        catch (InvalidOperationException) { }
    }
    internal QuestChoice[] Choices(string name) => Ready && name == "Haley" ? PromiseChoices().Concat(PhotoChoices()).ToArray() : Array.Empty<QuestChoice>();
    internal QuestActionResult? Apply(string name, string key)
    {
        if (!Ready || name != "Haley" || !BesideHaley()) return null;
        var result = key.StartsWith("haley:promise:", StringComparison.Ordinal) ? ApplyPromise(key)
            : key.StartsWith("haley:tree:", StringComparison.Ordinal) ? ApplyTree(key) : ApplyPhoto(key);
        RememberOutcomes(); EnsureQuests(); return result;
    }
    private static bool BesideHaley()
    {
        var npc = Game1.getCharacterFromName("Haley");
        return npc?.currentLocation == Game1.player.currentLocation && Microsoft.Xna.Framework.Vector2.Distance(npc.Tile, Game1.player.Tile) <= 3
            && !Game1.eventUp && Game1.currentMinigame == null;
    }
    private void RememberOutcomes()
    {
        if (!Ready) return;
        var memory = getMemory()!;
        var promise = state!.Promise;
        if (promise.Status == "completed") memory.Experiences.Record("haley:promise:sunflower", "promise", promise.CompletedDay!.Value,
            "The farmer explicitly gave Haley one " + promise.DeliveredItemName + " for her photography still life. " + (promise.WasLate ? "The delivery was late, but they followed through." : "They kept the accepted promise.")
            + " This does not prove a photo was taken or a romantic commitment.", "sunflower promise photography follow-through");
        if (state.Outing.Status == "completed")
        {
            state.Tree.RecordPhoto(state.Outing.MeetingDay, state.Outing.Framing, state.Outing.Style);
            memory.Experiences.Record("haley:outing:photo:" + state.Outing.MeetingDay, "outing", state.Outing.MeetingDay,
                $"The farmer and Haley met in Town for their accepted afternoon photo walk, chose {state.Outing.Framing} framing and a {state.Outing.Style} shot, and finished after the visible camera shutter. They actually shared the session. No inventory photo or generated image was created, and no romantic commitment was made.", "photography photo walk town shared afternoon");
        }
        RefreshTree();
    }
    private void RefreshTree()
    {
        if (Ready) state!.Tree.Refresh(state.Promise.Status == "completed", romance.State.Characters.GetValueOrDefault("Haley")?.TalkDays ?? 0);
    }
    internal void ObserveOutfit(string name, string fingerprint)
    {
        if (!Ready || name != "Haley" || !BesideHaley()) return;
        state!.Tree.ObserveOutfit(Today, fingerprint); RefreshTree();
    }
    private void EnsureQuests() { EnsurePromiseQuest(); EnsurePhotoQuest(); }
}
