using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SolaceWeather.Core;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Quests;

namespace SolaceWeather.Relationships;

internal sealed partial class HaleyLifeService
{
    private const string PhotoQuestId = "David.SolaceWeather/HaleyPhotoWalk";
    private Point? photoSpot;
    private NPC? photoActor;
    private GameLocation? photoOrigin;
    private Vector2 photoPosition;
    private int photoFacing, photoSpeed;
    private bool photoFollow, photoIgnore, playerCouldMove, ownsPhotoMovement, advancingPhoto, shutterObserved;
    private Dialogue[] photoDialogue = Array.Empty<Dialogue>();
    private RomanceDateMenu? photoMenu;
    private double shutterStarted;
    private bool PhotoBoundaries() => Ready && photoSpot != null && romance.State.Booking == null && !otherOutingReserved()
        && romance.State.Characters.GetValueOrDefault("Haley")?.SeparationUntilDay == null
        && romance.State.Characters.GetValueOrDefault("Haley")?.PendingTransition == null
        && !(Game1.player.friendshipData.TryGetValue("Haley", out var friendship) && friendship.IsDivorced());
    private bool CanArrangePhoto() => Ready && (state!.Outing.CanOffer(Today)
        || state.Outing.Status == "completed" && state.Tree.Unlocked.Contains("photo") && Today >= state.Outing.MeetingDay + 3);
    private void FindPhotoSpot()
    {
        photoSpot = null; var town = Game1.getLocationFromName("Town");
        foreach (var tile in new[] { new Point(35,61), new Point(34,61), new Point(35,62), new Point(34,62) })
            if (town.isTilePassable(tile.ToVector2()) && town.CanItemBePlacedHere(tile.ToVector2())
                && town.isTilePassable((tile + new Point(0,1)).ToVector2()) && town.CanItemBePlacedHere((tile + new Point(0,1)).ToVector2()))
            { photoSpot = tile; return; }
    }
    private int? NextPhotoDay()
    {
        if (!PhotoBoundaries()) return null;
        var npc = Game1.getCharacterFromName("Haley");
        for (int day = Today; day <= Today + 7; day++)
            if ((day > Today || Minute < 990) && romance.Dates.Available(npc, day, 1020, "town-walk")) return day;
        return null;
    }
    private string PhotoStamp => $"haley:photo:{state!.Outing.Status}:{state.Outing.OfferDay}:{state.Outing.MeetingDay}:";
    private QuestChoice[] PhotoChoices() => state!.Outing.Status switch
    {
        "offered" => new[] { new QuestChoice(PhotoStamp + "accept", $"Yes, photo walk {AbigailDeliveryQuest.DateLabel(state.Outing.MeetingDay)} at 5 pm."), new QuestChoice(PhotoStamp + "decline", "Not this time.") },
        "accepted" => new[] { new QuestChoice(PhotoStamp + "cancel", "Cancel the photo walk.") },
        "missed" => new[] { new QuestChoice(PhotoStamp + "reschedule", "Find another afternoon for photos.") },
        _ => Array.Empty<QuestChoice>()
    };
    private QuestActionResult? ApplyPhoto(string key)
    {
        if (!key.StartsWith(PhotoStamp, StringComparison.Ordinal)) return null;
        string action = key[PhotoStamp.Length..]; var outing = state!.Outing;
        if (action == "accept" && PhotoBoundaries() && romance.Dates.Available(Game1.getCharacterFromName("Haley"), outing.MeetingDay, 1020, "town-walk") && outing.Answer(Today, true))
            return new("Yes, let's take that photo walk.", $"The farmer explicitly accepted a photo walk with Haley in Town on {AbigailDeliveryQuest.DateLabel(outing.MeetingDay)} at 5 pm. Meet by the square; no following or completed shoot yet.", "Okay! Meet me in Town at five. We can see what catches our eye.");
        if (action == "decline" && outing.Answer(Today, false)) return new("Not this time.", "The photo walk invitation was declined. No meeting or penalty.", "That's fine. Maybe another afternoon.");
        if (action == "cancel" && outing.Status == "accepted")
        { outing.Interrupt(); CleanupPhoto(); return new("Let's cancel the photo walk.", "The accepted photo walk was cancelled without completion or punishment.", "Okay. Thanks for letting me know."); }
        if (action == "reschedule" && NextPhotoDay() is int day && outing.Offer(Today, day))
            return new("When could we try again?", "A replacement afternoon is offered and still requires explicit acceptance.", $"How about {AbigailDeliveryQuest.DateLabel(day)} at five?");
        return null;
    }
    private bool PreparePhoto()
    {
        if (photoActor != null) return true;
        if (!PhotoBoundaries() || Game1.eventUp || Game1.isRaining || Game1.isGreenRain) return false;
        var npc = Game1.getCharacterFromName("Haley");
        if (!romance.Dates.Available(npc, Today, 1020, "town-walk") || npc.currentLocation == null || npc.isMoving()
            || npc.controller != null || npc.temporaryController != null || npc.doingEndOfRouteAnimation.Value || npc.isSleeping.Value) return false;
        photoActor = npc; photoOrigin = npc.currentLocation; photoPosition = npc.Position; photoFacing = npc.FacingDirection;
        photoSpeed = npc.speed; photoFollow = npc.followSchedule; photoIgnore = npc.ignoreScheduleToday; photoDialogue = npc.CurrentDialogue.ToArray();
        npc.Halt(); npc.followSchedule = false; npc.ignoreScheduleToday = true;
        Game1.warpCharacter(npc, "Town", photoSpot!.Value); npc.faceDirection(2); return true;
    }
    internal bool TryBegin(NPC npc)
    {
        if (!Ready || npc.Name != "Haley" || state!.Outing.Status != "accepted" || state.Outing.MeetingDay != Today
            || Minute is < 1020 or > 1050 || !PhotoBoundaries() || Game1.isRaining || Game1.isGreenRain || Game1.eventUp
            || Game1.currentLocation.Name != "Town" || Vector2.Distance(Game1.player.Tile, photoSpot!.Value.ToVector2()) > 3) return false;
        if (!PreparePhoto() || !state.Outing.Begin(Today, Minute)) return false;
        if (Game1.activeClickableMenu is DialogueBox native && native.characterDialogue?.speaker == npc) { native.closeDialogue(); Game1.player.CanMove = true; }
        playerCouldMove = Game1.player.CanMove; ownsPhotoMovement = true; Game1.player.Halt(); Game1.player.CanMove = false;
        NextPhotoStep(); return true;
    }
    private bool PhotoSceneValid() => Ready && state!.Outing.Status == "active" && state.Outing.MeetingDay == Today && Minute <= 1080
        && photoActor != null && photoActor.currentLocation == Game1.currentLocation && Game1.currentLocation.Name == "Town"
        && Vector2.Distance(Game1.player.Tile, photoActor.Tile) <= 4 && !Game1.eventUp && !Game1.isRaining && !Game1.isGreenRain
        && ReferenceEquals(Game1.activeClickableMenu, photoMenu);
    private void TickPhoto()
    {
        if (!Ready) { CleanupPhoto(); return; }
        try
        {
            var outing = state!.Outing; string previous = outing.Status; outing.CheckTime(Today, Minute);
            if (previous != outing.Status) { CleanupPhoto(); EnsureQuests(); }
            if (outing.Status == "active") { if (!PhotoSceneValid()) CancelPhoto(); return; }
            if (outing.Status != "accepted") return;
            if (photoActor != null && (!PhotoBoundaries() || Game1.eventUp || Game1.isRaining || Game1.isGreenRain)) { CancelPhoto(); return; }
            if (photoActor == null && outing.MeetingDay == Today && Minute is >= 1020 and <= 1050) PreparePhoto();
            if (photoActor != null && Game1.activeClickableMenu is DialogueBox box && box.characterDialogue?.speaker == photoActor) TryBegin(photoActor);
        }
        catch { state!.Outing.Interrupt(); CleanupPhoto(); monitor.Log("Haley's photo walk stopped safely; no completion recorded.", StardewModdingAPI.LogLevel.Warn); }
    }
    private void NextPhotoStep()
    {
        int step = state!.Outing.Step;
        string text = step switch {
            0 => "Okay, look around for a second. A good shot isn't just pointing at something pretty. Do you want the whole scene, or one little detail?",
            1 => "That could work. Now, do you want this to feel candid, or a little more posed? Either can look good if you actually commit to it.",
            _ => "There. I think we got something worth looking at. Honestly, it's nice having someone slow down and notice things with me." };
        var options = new List<(string, Action)>();
        if (step == 0) { options.Add(("Frame a wide view (10 minutes)", () => AdvancePhoto("wide"))); options.Add(("Focus on a small detail (10 minutes)", () => AdvancePhoto("detail"))); }
        if (step == 1) { options.Add(("Keep the shot candid (10 minutes)", () => AdvancePhoto("candid"))); options.Add(("Compose a posed shot (10 minutes)", () => AdvancePhoto("posed"))); }
        if (step == 2) options.Add(("Finish the photo session (10 minutes)", () => AdvancePhoto("finish")));
        options.Add(("Leave the photo walk early", CancelPhoto));
        photoMenu = new RomanceDateMenu(photoActor, $"Photo walk — {step + 1}/3\n{text}", options.ToArray(), CancelPhoto);
        Game1.activeClickableMenu = photoMenu;
        if (step == 2) { shutterStarted = Game1.currentGameTime.TotalGameTime.TotalMilliseconds; shutterObserved = false; }
        EnsureQuests();
    }
    private void AdvancePhoto(string choice)
    {
        if (advancingPhoto || !PhotoSceneValid() || state!.Outing.Step == 2 && !shutterObserved) return;
        var outing = state!.Outing;
        if (outing.Step == 0 && choice is not ("wide" or "detail") || outing.Step == 1 && choice is not ("candid" or "posed") || outing.Step == 2 && choice != "finish") return;
        advancingPhoto = true;
        try
        {
            Game1.performTenMinuteClockUpdate();
            if (!PhotoSceneValid() || !outing.Advance(Today, Minute, choice, shutterObserved)) return;
            if (outing.Status == "completed")
            {
                RememberOutcomes(); CleanupPhoto(); EnsureQuests();
                Game1.activeClickableMenu = new RomanceDateMenu(Game1.getCharacterFromName("Haley"), "That was actually really nice. We'll have to try another angle sometime.", Array.Empty<(string, Action)>(), Game1.exitActiveMenu);
            }
            else NextPhotoStep();
        }
        finally { advancingPhoto = false; }
    }
    private void DrawShutter(SpriteBatch b)
    {
        if (!PhotoSceneValid() || state!.Outing.Step != 2 || photoActor == null) return;
        double age = Game1.currentGameTime.TotalGameTime.TotalMilliseconds - shutterStarted;
        if (!shutterObserved || age < 350)
        {
            Vector2 point = Game1.GlobalToLocal(photoActor.Position + new Vector2(28,-24));
            b.Draw(Game1.staminaRect, new Rectangle((int)point.X-12,(int)point.Y-12,24,24), Color.White * (float)Math.Max(.2,1-age/350));
            shutterObserved = true;
        }
    }
    private void CancelPhoto() { state?.Outing.Interrupt(); CleanupPhoto(); EnsureQuests(); }
    private void CleanupPhoto()
    {
        if (photoMenu != null && ReferenceEquals(Game1.activeClickableMenu, photoMenu)) Game1.exitActiveMenu();
        photoMenu = null; shutterObserved = false;
        if (photoActor != null)
        {
            var npc = photoActor; photoActor = null; npc.Halt(); npc.controller = null; npc.temporaryController = null;
            if (photoOrigin != null) { Game1.warpCharacter(npc, photoOrigin, photoPosition / 64); npc.Position = photoPosition; npc.faceDirection(photoFacing); }
            npc.speed = photoSpeed; npc.followSchedule = photoFollow; npc.ignoreScheduleToday = photoIgnore;
            npc.CurrentDialogue.Clear(); for (int i = photoDialogue.Length-1; i >= 0; i--) npc.CurrentDialogue.Push(photoDialogue[i]);
            if (photoFollow && !Game1.eventUp) npc.checkSchedule(Game1.timeOfDay);
        }
        photoOrigin = null; photoDialogue = Array.Empty<Dialogue>();
        if (ownsPhotoMovement && !Game1.eventUp && Game1.currentMinigame == null && !Game1.player.isInBed.Value) Game1.player.CanMove = playerCouldMove;
        ownsPhotoMovement = false;
    }
    private void EnsurePhotoQuest()
    {
        if (!Ready) return;
        var outing = state!.Outing; bool needed = outing.Status is "accepted" or "active" or "missed";
        var existing = Game1.player.questLog.Where(q => q.id.Value == PhotoQuestId).ToArray();
        foreach (var extra in existing.Skip(needed ? 1 : 0)) Game1.player.questLog.Remove(extra);
        if (!needed) return;
        var quest = existing.FirstOrDefault();
        if (quest == null) { quest = new Quest(); quest.id.Value = PhotoQuestId; quest.questType.Value = Quest.type_basic; quest.accepted.Value = true; quest.showNew.Value = true; quest.canBeCancelled.Value = false; Game1.player.questLog.Add(quest); }
        quest.questTitle = "A photo walk with Haley";
        quest.questDescription = $"Meet Haley in the Town square on {AbigailDeliveryQuest.DateLabel(outing.MeetingDay)} at 5 pm, by 5:30. Talk to her to begin three photography choices. Talk to her to cancel or choose another day.";
        quest.currentObjective = outing.Status == "missed" ? "Talk to Haley to arrange another afternoon. No session completed."
            : outing.Status == "active" ? $"Photo walk: {outing.Step}/3 steps." : "Meet Haley in Town and talk at 5 pm.";
    }
}
