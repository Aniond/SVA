using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SolaceWeather.Core;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Quests;

namespace SolaceWeather.Relationships;

internal sealed partial class EmilyLifeService
{
    private const string DesignQuestId = "David.SolaceWeather/EmilyDesignSession";
    private Point? designSpot;
    private NPC? designActor;
    private GameLocation? designOrigin;
    private Vector2 designPosition;
    private int designFacing, designSpeed;
    private bool designFollow, designIgnore, playerCouldMove, ownsDesignMovement, advancingDesign, designObserved;
    private Dialogue[] designDialogue = Array.Empty<Dialogue>();
    private RomanceDateMenu? designMenu;
    private bool DesignBoundaries() => Ready && designSpot != null && romance.State.Booking == null && !otherOutingReserved()
        && romance.State.Characters.GetValueOrDefault("Emily")?.SeparationUntilDay == null
        && romance.State.Characters.GetValueOrDefault("Emily")?.PendingTransition == null
        && !(Game1.player.friendshipData.TryGetValue("Emily", out var friendship) && friendship.IsDivorced());
    private bool CanArrangeDesign() => Ready && (state!.Session.CanOffer(Today)
        || state.Session.Status == "completed" && state.Tree.Unlocked.Contains("design") && Today >= state.Session.MeetingDay + 3);
    private void FindDesignSpot()
    {
        designSpot = null; var town = Game1.getLocationFromName("Town");
        foreach (var tile in new[] { new Point(35,61), new Point(34,61), new Point(35,62), new Point(34,62) })
            if (town.isTilePassable(tile.ToVector2()) && town.CanItemBePlacedHere(tile.ToVector2())
                && town.isTilePassable((tile + new Point(0,1)).ToVector2()) && town.CanItemBePlacedHere((tile + new Point(0,1)).ToVector2()))
            { designSpot = tile; return; }
    }
    private int? NextDesignDay()
    {
        if (!DesignBoundaries()) return null;
        var npc = Game1.getCharacterFromName("Emily");
        for (int day = Today; day <= Today + 7; day++)
            if ((day > Today || Minute < 630) && romance.Dates.Available(npc, day, 660, "town-walk")) return day;
        return null;
    }
    private string DesignStamp => $"emily:design:{state!.Session.Status}:{state.Session.OfferDay}:{state.Session.MeetingDay}:";
    private QuestChoice[] DesignChoices() => state!.Session.Status switch
    {
        "offered" => new[] { new QuestChoice(DesignStamp + "accept", $"Yes, outfit-design session {AbigailDeliveryQuest.DateLabel(state.Session.MeetingDay)} at 11 am."), new QuestChoice(DesignStamp + "decline", "Not this time.") },
        "accepted" => new[] { new QuestChoice(DesignStamp + "cancel", "Cancel the outfit-design session.") },
        "missed" => new[] { new QuestChoice(DesignStamp + "reschedule", "Find another morning for designs.") },
        _ => Array.Empty<QuestChoice>()
    };
    private QuestActionResult? ApplyDesign(string key)
    {
        if (!key.StartsWith(DesignStamp, StringComparison.Ordinal)) return null;
        string action = key[DesignStamp.Length..]; var outing = state!.Session;
        if (action == "accept" && DesignBoundaries() && romance.Dates.Available(Game1.getCharacterFromName("Emily"), outing.MeetingDay, 660, "town-walk") && outing.Answer(Today, true))
            return new("Yes, let's take that outfit-design session.", $"The farmer explicitly accepted a outfit-design session with Emily in Town on {AbigailDeliveryQuest.DateLabel(outing.MeetingDay)} at 11 am. Meet by the square; no design session or garment completed yet.", "Okay! Meet me in Town at eleven. We can see what catches our eye.");
        if (action == "decline" && outing.Answer(Today, false)) return new("Not this time.", "The outfit-design session invitation was declined. No meeting or penalty.", "That's fine. Maybe another morning.");
        if (action == "cancel" && outing.Status == "accepted")
        { outing.Interrupt(); CleanupDesign(); return new("Let's cancel the outfit-design session.", "The accepted outfit-design session was cancelled without completion or punishment.", "Okay. Thanks for letting me know."); }
        if (action == "reschedule" && NextDesignDay() is int day && outing.Offer(Today, day))
            return new("When could we try again?", "A replacement morning is offered and still requires explicit acceptance.", $"How about {AbigailDeliveryQuest.DateLabel(day)} at eleven?");
        return null;
    }
    private bool PrepareDesign()
    {
        if (designActor != null) return true;
        if (!DesignBoundaries() || Game1.eventUp || Game1.isRaining || Game1.isGreenRain) return false;
        var npc = Game1.getCharacterFromName("Emily");
        if (!romance.Dates.Available(npc, Today, 660, "town-walk") || npc.currentLocation == null || npc.isMoving()
            || npc.controller != null || npc.temporaryController != null || npc.doingEndOfRouteAnimation.Value || npc.isSleeping.Value) return false;
        designActor = npc; designOrigin = npc.currentLocation; designPosition = npc.Position; designFacing = npc.FacingDirection;
        designSpeed = npc.speed; designFollow = npc.followSchedule; designIgnore = npc.ignoreScheduleToday; designDialogue = npc.CurrentDialogue.ToArray();
        npc.Halt(); npc.followSchedule = false; npc.ignoreScheduleToday = true;
        Game1.warpCharacter(npc, "Town", designSpot!.Value); npc.faceDirection(2); return true;
    }
    internal bool TryBegin(NPC npc)
    {
        if (!Ready || npc.Name != "Emily" || state!.Session.Status != "accepted" || state.Session.MeetingDay != Today
            || Minute is < 660 or > 690 || !DesignBoundaries() || Game1.isRaining || Game1.isGreenRain || Game1.eventUp
            || Game1.currentLocation.Name != "Town" || Vector2.Distance(Game1.player.Tile, designSpot!.Value.ToVector2()) > 3) return false;
        if (!PrepareDesign() || !state.Session.Begin(Today, Minute)) return false;
        if (Game1.activeClickableMenu is DialogueBox native && native.characterDialogue?.speaker == npc) { native.closeDialogue(); Game1.player.CanMove = true; }
        playerCouldMove = Game1.player.CanMove; ownsDesignMovement = true; Game1.player.Halt(); Game1.player.CanMove = false;
        NextDesignStep(); return true;
    }
    private bool DesignSceneValid() => Ready && state!.Session.Status == "active" && state.Session.MeetingDay == Today && Minute <= 720
        && designActor != null && designActor.currentLocation == Game1.currentLocation && Game1.currentLocation.Name == "Town"
        && Vector2.Distance(Game1.player.Tile, designActor.Tile) <= 4 && !Game1.eventUp && !Game1.isRaining && !Game1.isGreenRain
        && ReferenceEquals(Game1.activeClickableMenu, designMenu);
    private void TickDesign()
    {
        if (!Ready) { CleanupDesign(); return; }
        try
        {
            var outing = state!.Session; string previous = outing.Status; outing.CheckTime(Today, Minute);
            if (previous != outing.Status) { CleanupDesign(); EnsureQuests(); }
            if (outing.Status == "active") { if (!DesignSceneValid()) CancelDesign(); return; }
            if (outing.Status != "accepted") return;
            if (designActor != null && (!DesignBoundaries() || Game1.eventUp || Game1.isRaining || Game1.isGreenRain)) { CancelDesign(); return; }
            if (designActor == null && outing.MeetingDay == Today && Minute is >= 660 and <= 690) PrepareDesign();
            if (designActor != null && Game1.activeClickableMenu is DialogueBox box && box.characterDialogue?.speaker == designActor) TryBegin(designActor);
        }
        catch { state!.Session.Interrupt(); CleanupDesign(); monitor.Log("Emily's outfit-design session stopped safely; no completion recorded.", StardewModdingAPI.LogLevel.Warn); }
    }
    private void NextDesignStep()
    {
        int step = state!.Session.Step;
        string text = step switch {
            0 => "An outfit can say something without saying a word. Let's begin with a feeling. Would you like to explore something quiet, or something playful?",
            1 => "I like having a feeling to work from. Now imagine the pattern: shapes borrowed from nature, or a rhythm of clear geometric lines?",
            _ => $"A {state.Session.Mood} mood with a {state.Session.Pattern} pattern. That's a lovely starting point. We haven't sewn anything yet, but now the idea has a shape of its own." };
        var options = new List<(string, Action)>();
        if (step == 0) { options.Add(("Explore a quiet mood (10 minutes)", () => AdvanceDesign("quiet"))); options.Add(("Explore a playful mood (10 minutes)", () => AdvanceDesign("playful"))); }
        if (step == 1) { options.Add(("Explore natural patterns (10 minutes)", () => AdvanceDesign("natural"))); options.Add(("Explore geometric patterns (10 minutes)", () => AdvanceDesign("geometric"))); }
        if (step == 2) options.Add(("Finish the design idea (10 minutes)", () => AdvanceDesign("finish")));
        options.Add(("Leave the outfit-design session early", CancelDesign));
        designMenu = new RomanceDateMenu(designActor, $"Outfit design — {step + 1}/3\n{text}", options.ToArray(), CancelDesign);
        Game1.activeClickableMenu = designMenu;
        if (step == 2) designObserved = false;
        EnsureQuests();
    }
    private void AdvanceDesign(string choice)
    {
        if (advancingDesign || !DesignSceneValid() || state!.Session.Step == 2 && !designObserved) return;
        var outing = state!.Session;
        if (outing.Step == 0 && choice is not ("quiet" or "playful") || outing.Step == 1 && choice is not ("natural" or "geometric") || outing.Step == 2 && choice != "finish") return;
        advancingDesign = true;
        try
        {
            Game1.performTenMinuteClockUpdate();
            if (!DesignSceneValid() || !outing.Advance(Today, Minute, choice, designObserved)) return;
            if (outing.Status == "completed")
            {
                RememberOutcomes(); CleanupDesign(); EnsureQuests();
                Game1.activeClickableMenu = new RomanceDateMenu(Game1.getCharacterFromName("Emily"), "Thank you for exploring that with me. I enjoy seeing how someone else's imagination works.", Array.Empty<(string, Action)>(), Game1.exitActiveMenu);
            }
            else NextDesignStep();
        }
        finally { advancingDesign = false; }
    }
    private void DrawDesignPreview(SpriteBatch b)
    {
        if (!DesignSceneValid() || state!.Session.Step != 2 || designActor == null) return;
        // A visible abstract palette marks the shared idea, not a finished inventory garment.
        Vector2 point = Game1.GlobalToLocal(designActor.Position + new Vector2(16,-28));
        Color first = state.Session.Mood == "quiet" ? new(109, 151, 166) : new(213, 93, 151);
        Color second = state.Session.Mood == "quiet" ? new(224, 213, 176) : new(238, 188, 72);
        b.Draw(Game1.staminaRect, new Rectangle((int)point.X, (int)point.Y, 16, 20), first);
        b.Draw(Game1.staminaRect, new Rectangle((int)point.X + 17, (int)point.Y, 16, 20), second);
        designObserved = true;
    }
    private void CancelDesign() { state?.Session.Interrupt(); CleanupDesign(); EnsureQuests(); }
    private void CleanupDesign()
    {
        if (designMenu != null && ReferenceEquals(Game1.activeClickableMenu, designMenu)) Game1.exitActiveMenu();
        designMenu = null; designObserved = false;
        if (designActor != null)
        {
            var npc = designActor; designActor = null; npc.Halt(); npc.controller = null; npc.temporaryController = null;
            if (designOrigin != null) { Game1.warpCharacter(npc, designOrigin, designPosition / 64); npc.Position = designPosition; npc.faceDirection(designFacing); }
            npc.speed = designSpeed; npc.followSchedule = designFollow; npc.ignoreScheduleToday = designIgnore;
            npc.CurrentDialogue.Clear(); for (int i = designDialogue.Length-1; i >= 0; i--) npc.CurrentDialogue.Push(designDialogue[i]);
            if (designFollow && !Game1.eventUp) npc.checkSchedule(Game1.timeOfDay);
        }
        designOrigin = null; designDialogue = Array.Empty<Dialogue>();
        if (ownsDesignMovement && !Game1.eventUp && Game1.currentMinigame == null && !Game1.player.isInBed.Value) Game1.player.CanMove = playerCouldMove;
        ownsDesignMovement = false;
    }
    private void EnsureDesignQuest()
    {
        if (!Ready) return;
        var outing = state!.Session; bool needed = outing.Status is "accepted" or "active" or "missed";
        var existing = Game1.player.questLog.Where(q => q.id.Value == DesignQuestId).ToArray();
        foreach (var extra in existing.Skip(needed ? 1 : 0)) Game1.player.questLog.Remove(extra);
        if (!needed) return;
        var quest = existing.FirstOrDefault();
        if (quest == null) { quest = new Quest(); quest.id.Value = DesignQuestId; quest.questType.Value = Quest.type_basic; quest.accepted.Value = true; quest.showNew.Value = true; quest.canBeCancelled.Value = false; Game1.player.questLog.Add(quest); }
        quest.questTitle = "An outfit-design session with Emily";
        quest.questDescription = $"Meet Emily in the Town square on {AbigailDeliveryQuest.DateLabel(outing.MeetingDay)} at 11 am, by 11:30. Talk to her to begin three design choices. Talk to her to cancel or choose another day.";
        quest.currentObjective = outing.Status == "missed" ? "Talk to Emily to arrange another morning. No session completed."
            : outing.Status == "active" ? $"Outfit design: {outing.Step}/3 steps." : "Meet Emily in Town and talk at 11 am.";
    }
}
