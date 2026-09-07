using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SolaceWeather.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Quests;

namespace SolaceWeather.Relationships;

internal sealed class CemeteryOuting
{
    private const string SaveKey = "cemetery-investigation", QuestId = "David.SolaceWeather/CemeteryInvestigation";
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly RomanceService romance;
    private readonly AbigailRelationship abigail;
    private CemeteryOutingState? state;
    private Point? meetingTile;
    private NPC? actor;
    private GameLocation? origin;
    private Vector2 originPosition;
    private int originFacing, originSpeed;
    private bool originFollow, originIgnore, originalCanMove, ownsMovement;
    private Dialogue[] originDialogue = Array.Empty<Dialogue>();
    private RomanceDateMenu? scene;
    private bool advancing, ghostSeen;
    private double ghostSince;
    private static int Today => Game1.Date.TotalDays;
    private static int Minute => Game1.timeOfDay / 100 * 60 + Game1.timeOfDay % 100;
    internal bool Ready => romance.Ready && state != null;
    internal CemeteryOutingState? State => state;
    internal Func<bool>? OtherOutingReserved { get; set; }

    internal CemeteryOuting(IModHelper helper, IMonitor monitor, RomanceService romance, AbigailRelationship abigail)
    {
        this.helper = helper; this.monitor = monitor; this.romance = romance; this.abigail = abigail;
        helper.Events.GameLoop.SaveLoaded += (_, _) => Load();
        helper.Events.GameLoop.Saving += (_, _) =>
        {
            if (state?.Status == "active") state.Interrupt(); Cleanup();
            if (Ready && state!.IsValid(Game1.player.UniqueMultiplayerID)) helper.Data.WriteSaveData(SaveKey, state);
        };
        helper.Events.GameLoop.DayEnding += (_, _) => { if (Ready) state!.CheckTime(Today, 1560); Cleanup(); };
        helper.Events.GameLoop.DayStarted += (_, _) => { if (Ready) { state!.CheckTime(Today, Minute); EnsureQuest(); } };
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => { Cleanup(); state = null; meetingTile = null; };
        helper.Events.GameLoop.UpdateTicked += (_, e) => { if (e.IsMultipleOf(15)) Tick(); };
        helper.Events.Player.Warped += (_, e) => { if (e.IsLocalPlayer && state?.Status == "active") { state.Interrupt(); Cleanup(); EnsureQuest(); } };
        helper.Events.Display.RenderedWorld += (_, e) => DrawGhost(e.SpriteBatch);
    }
    private void Load()
    {
        Cleanup(); state = null; meetingTile = null;
        if (!romance.Ready) return;
        try
        {
            var loaded = helper.Data.ReadSaveData<CemeteryOutingState>(SaveKey);
            if (loaded != null && !loaded.IsValid(Game1.player.UniqueMultiplayerID)) throw new InvalidDataException();
            state = loaded ?? new() { FarmerId = Game1.player.UniqueMultiplayerID };
            if (state.Status == "active") state.Interrupt();
            state.CheckTime(Today, Minute); FindCemetery(); RememberCompletion(); EnsureQuest();
        }
        catch { monitor.Log("Cemetery outing save unavailable; existing data preserved.", LogLevel.Warn); state = null; }
    }
    private void FindCemetery()
    {
        var town = Game1.getLocationFromName("Town");
        var layer = town?.Map?.GetLayer("Buildings");
        if (layer == null) return;
        var graves = new List<Point>();
        for (int x = 0; x < layer.LayerWidth; x++)
            for (int y = 0; y < layer.LayerHeight; y++)
            {
                string? action = town!.doesTileHaveProperty(x, y, "Action", "Buildings");
                if (action?.Contains("grave", StringComparison.OrdinalIgnoreCase) == true || action?.Contains("cemetery", StringComparison.OrdinalIgnoreCase) == true)
                    graves.Add(new Point(x, y));
            }
        foreach (var grave in graves.OrderBy(p => p.X).ThenBy(p => p.Y))
            foreach (var offset in new[] { new Point(-2, 1), new Point(-2, 2), new Point(0, 2), new Point(1, 2), new Point(-1, 2), new Point(0, 1) })
            {
                Point tile = grave + offset;
                if (town!.isTilePassable(tile.ToVector2()) && town.CanItemBePlacedHere(tile.ToVector2())) { meetingTile = tile; return; }
            }
    }
    private bool BoundariesAllow() => Ready && meetingTile != null && romance.State.Booking == null && OtherOutingReserved?.Invoke() != true
        && romance.State.Characters.GetValueOrDefault("Abigail")?.SeparationUntilDay == null
        && !(Game1.player.friendshipData.TryGetValue("Abigail", out var f) && f.IsDivorced());
    private int? NextNight()
    {
        if (!BoundariesAllow()) return null;
        var npc = Game1.getCharacterFromName("Abigail");
        for (int day = Today; day <= Today + 7; day++)
            if ((day > Today || Minute < 1170) && romance.Dates.Available(npc, day, 1170, "town-walk")) return day;
        return null;
    }
    internal object ContextFor(string name)
    {
        int? next = name == "Abigail" && state?.CanOffer(Today) == true ? NextNight() : null;
        return new { CanOffer = next.HasValue, Template = "cemetery", Date = next.HasValue ? AbigailDeliveryQuest.DateLabel(next.Value) : null,
            Meeting = "Meet Abigail at the cemetery in Town at 8 pm; arrive by 8:30 pm. A friendly, noncombat ghost investigation, no item reward.",
            Status = name == "Abigail" ? state?.Status : "unavailable", Rule = "Only an explicit acceptance books this outing. No arbitrary generated quest, combat, reward or automatic completion." };
    }
    internal void Reply(string name, ConversationReply reply)
    {
        if (name == "Abigail" && reply.QuestRequest == "cemetery" && state?.CanOffer(Today) == true && NextNight() is int day)
            state.Offer(Today, day);
    }
    private string Stamp => $"cemetery:{state!.Status}:{state.OfferDay}:{state.MeetingDay}:";
    internal QuestChoice[] Choices(string name)
    {
        if (!Ready || name != "Abigail") return Array.Empty<QuestChoice>();
        return state!.Status switch
        {
            "offered" => new[] { new QuestChoice(Stamp + "accept", $"Yes — meet {AbigailDeliveryQuest.DateLabel(state.MeetingDay)}, 8 pm"), new QuestChoice(Stamp + "decline", "Not this time") },
            "accepted" => new[] { new QuestChoice(Stamp + "cancel", "Cancel / reschedule the cemetery meeting") },
            "missed" => new[] { new QuestChoice(Stamp + "reschedule", "Find another night for the cemetery") },
            _ => Array.Empty<QuestChoice>()
        };
    }
    internal QuestActionResult? Apply(string name, string key)
    {
        if (!Ready || name != "Abigail" || !key.StartsWith(Stamp, StringComparison.Ordinal)) return null;
        string action = key[Stamp.Length..];
        if (action == "accept")
        {
            if (!BoundariesAllow() || !romance.Dates.Available(Game1.getCharacterFromName("Abigail"), state!.MeetingDay, 1170, "town-walk") || !state.Answer(Today, true)) return null;
            EnsureQuest();
            return new("Yes, let's investigate the cemetery.", $"The farmer explicitly accepted a cemetery investigation with Abigail on {AbigailDeliveryQuest.DateLabel(state.MeetingDay)} at 8 pm. Meet there; no following from here.", "Meet me at the cemetery at eight. We can see what that place is like after dark.");
        }
        if (action == "decline" && state!.Answer(Today, false))
            return new("Not this time, thanks.", "The farmer declined the cemetery invitation. No meeting was booked.", "No problem. Maybe another night.");
        if (action == "cancel" && state!.Status == "accepted")
        {
            state.Interrupt(); Cleanup(); EnsureQuest();
            return new("Let's find another night.", "The cemetery meeting was cancelled without a completed outing or penalty.", "That's okay. We can choose another night when we're both free.");
        }
        if (action == "reschedule" && NextNight() is int day && state!.Offer(Today, day))
            return new("When could we try the cemetery again?", "A new cemetery date is offered and still requires acceptance.", $"How about {AbigailDeliveryQuest.DateLabel(day)} at eight? Only if that works for you.");
        return null;
    }
    private void Tick()
    {
        if (!Ready) { Cleanup(); return; }
        try
        {
            string before = state!.Status; state.CheckTime(Today, Minute);
            if (before != state.Status) { Cleanup(); EnsureQuest(); }
            if (state.Status == "active")
            {
                if (!SceneValid()) { state.Interrupt(); Cleanup(); EnsureQuest(); }
                return;
            }
            if (state.Status != "accepted") return;
            if (actor != null && (!BoundariesAllow() || Game1.eventUp || Game1.isRaining || Game1.isGreenRain))
            { state.Interrupt(); Cleanup(); EnsureQuest(); return; }
            if (actor == null && state.MeetingDay == Today && Minute is >= 1200 and <= 1230) Prepare();
            if (actor != null && Game1.activeClickableMenu is DialogueBox box && box.characterDialogue?.speaker == actor) TryBegin(actor);
        }
        catch { if (state?.Status == "active") state.Interrupt(); Cleanup(); monitor.Log("The cemetery meeting paused safely; no completion was recorded.", LogLevel.Warn); }
    }
    private bool Prepare()
    {
        if (actor != null) return true;
        if (!BoundariesAllow() || Game1.eventUp || Game1.isRaining || Game1.isGreenRain) return false;
        var npc = Game1.getCharacterFromName("Abigail");
        if (!romance.Dates.Available(npc, Today, 1170, "town-walk") || npc.currentLocation == null || npc.isMoving()
            || npc.controller != null || npc.temporaryController != null || npc.doingEndOfRouteAnimation.Value || npc.isSleeping.Value) return false;
        actor = npc; origin = npc.currentLocation; originPosition = npc.Position; originFacing = npc.FacingDirection;
        originSpeed = npc.speed; originFollow = npc.followSchedule; originIgnore = npc.ignoreScheduleToday; originDialogue = npc.CurrentDialogue.ToArray();
        npc.followSchedule = false; npc.ignoreScheduleToday = true; npc.Halt();
        Game1.warpCharacter(npc, "Town", meetingTile!.Value); npc.faceDirection(2);
        return true;
    }
    internal bool TryBegin(NPC npc)
    {
        if (!Ready || npc.Name != "Abigail" || state!.Status != "accepted" || state.MeetingDay != Today || Minute is < 1200 or > 1230
            || Game1.currentLocation.Name != "Town" || meetingTile == null || Vector2.Distance(Game1.player.Tile, meetingTile.Value.ToVector2()) > 3 || Game1.eventUp
            || Game1.isRaining || Game1.isGreenRain || !BoundariesAllow()) return false;
        if (!Prepare() || !state.Begin(Today, Minute)) return false;
        if (Game1.activeClickableMenu is DialogueBox native && native.characterDialogue?.speaker == npc)
        {
            native.closeDialogue();
            Game1.player.CanMove = true;
        }
        originalCanMove = Game1.player.CanMove; ownsMovement = true; Game1.player.Halt(); Game1.player.CanMove = false;
        ghostSeen = false; NextStep(); return true;
    }
    internal bool HasReservation => Ready && state!.Status is "accepted" or "active";
    private bool SceneValid() => Ready && actor != null && state!.Status == "active" && state.MeetingDay == Today && Minute <= 1260
        && !Game1.eventUp && !Game1.isRaining && !Game1.isGreenRain && Game1.currentLocation.Name == "Town" && actor.currentLocation == Game1.currentLocation
        && Vector2.Distance(Game1.player.Tile, actor.Tile) <= 4 && ReferenceEquals(Game1.activeClickableMenu, scene);
    private void NextStep()
    {
        int step = state!.Step;
        string[] text = {
            "You made it. Let's start with this old headstone. The marks are worn down... maybe the interesting part is what happens when we stop rushing past.",
            "There's a pale light beside the stone. Let's stay quiet for a minute and see whether it moves. We don't need to disturb anything.",
            "Okay... that is definitely a ghost. It doesn't seem interested in hurting us. Let's give it some space and leave it in peace."
        };
        string[] choice = { "Examine the worn headstone (10 minutes)", "Wait quietly beside the pale light (10 minutes)", "Say goodnight and leave peacefully (10 minutes)" };
        scene = new RomanceDateMenu(actor, $"Cemetery investigation — {step + 1}/3\n{text[step]}",
            new[] { (choice[step], (Action)(() => Advance(step))), ("Leave the investigation early", (Action)CancelScene) }, CancelScene);
        Game1.activeClickableMenu = scene;
        if (step == 2) ghostSince = Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
        EnsureQuest();
    }
    private void Advance(int expected)
    {
        if (advancing || !SceneValid() || state!.Step != expected || expected == 2 && !ghostSeen) return;
        advancing = true;
        try
        {
            Game1.performTenMinuteClockUpdate();
            if (!SceneValid() || !state.Advance(Today, Minute, expected, ghostSeen)) return;
            if (state.Status == "completed")
            {
                RememberCompletion(); Cleanup(); EnsureQuest();
                Game1.activeClickableMenu = new RomanceDateMenu(Game1.getCharacterFromName("Abigail"),
                    "That was strange... and kind of beautiful. I'm glad we went together. We didn't have to fight anything to find an adventure.",
                    Array.Empty<(string, Action)>(), Game1.exitActiveMenu);
            }
            else NextStep();
        }
        finally { advancing = false; }
    }
    private void DrawGhost(SpriteBatch b)
    {
        if (!SceneValid() || state!.Step == 0 || meetingTile == null) return;
        Vector2 point = Game1.GlobalToLocal((meetingTile.Value.ToVector2() + new Vector2(1, -1)) * 64);
        double time = Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
        if (state.Step == 1)
        {
            int size = 10 + (int)(Math.Sin(time / 400) * 2);
            b.Draw(Game1.staminaRect, new Rectangle((int)point.X + 24, (int)point.Y - 24, size, size), Color.LightCyan * .6f); return;
        }
        var texture = helper.GameContent.Load<Texture2D>("Characters/Monsters/Ghost");
        int frame = (int)(time / 180) % Math.Max(1, Math.Min(4, texture.Width / 16));
        b.Draw(texture, point + new Vector2(16, -48 + (float)Math.Sin(time / 600) * 8), new Rectangle(frame * 16, 0, 16, Math.Min(32, texture.Height)), Color.White * .65f, 0, Vector2.Zero, 3f, SpriteEffects.None, 1);
        if (time - ghostSince >= 1500) ghostSeen = true;
    }
    private void CancelScene() { state?.Interrupt(); Cleanup(); EnsureQuest(); }
    private void Cleanup()
    {
        if (ReferenceEquals(Game1.activeClickableMenu, scene) && scene != null) Game1.exitActiveMenu();
        scene = null; ghostSeen = false;
        if (actor != null)
        {
            var npc = actor; actor = null;
            npc.Halt(); npc.controller = null; npc.temporaryController = null;
            if (origin != null) { Game1.warpCharacter(npc, origin, originPosition / 64); npc.Position = originPosition; npc.faceDirection(originFacing); }
            npc.speed = originSpeed; npc.followSchedule = originFollow; npc.ignoreScheduleToday = originIgnore;
            npc.CurrentDialogue.Clear(); for (int i = originDialogue.Length - 1; i >= 0; i--) npc.CurrentDialogue.Push(originDialogue[i]);
            if (originFollow && !Game1.eventUp) npc.checkSchedule(Game1.timeOfDay);
        }
        origin = null; originDialogue = Array.Empty<Dialogue>();
        if (ownsMovement && !Game1.eventUp && Game1.currentMinigame == null && !Game1.player.isInBed.Value) Game1.player.CanMove = originalCanMove;
        ownsMovement = false;
    }
    private void RememberCompletion()
    {
        if (state?.Status == "completed") abigail.Memory?.Experiences.Record("outing:cemetery", "outing", state.MeetingDay,
            "The farmer and Abigail met in the Town cemetery at night, examined a worn headstone, watched a pale light become a harmless ghost, and left peacefully. Both witnessed the apparition. There was no combat, item drop or romantic commitment.",
            "cemetery ghost investigation adventure nighttime peaceful Abigail");
    }
    private void EnsureQuest()
    {
        if (!Ready) return;
        var existing = Game1.player.questLog.Where(q => q.id.Value == QuestId).ToArray();
        bool needed = state!.Status is "accepted" or "active" or "missed";
        foreach (var extra in existing.Skip(needed ? 1 : 0)) Game1.player.questLog.Remove(extra);
        if (!needed) return;
        Quest? quest = existing.FirstOrDefault();
        if (quest == null) { quest = new Quest(); quest.id.Value = QuestId; quest.questType.Value = Quest.type_basic; quest.accepted.Value = true; quest.showNew.Value = true; quest.canBeCancelled.Value = false; Game1.player.questLog.Add(quest); }
        quest.questTitle = "A quiet night in the cemetery";
        quest.questDescription = $"Meet Abigail at the Town cemetery on {AbigailDeliveryQuest.DateLabel(state.MeetingDay)} at 8 pm (by 8:30). Talk to her there to start a noncombat investigation. Complete all three steps. Talk to her to cancel or arrange another night.";
        quest.currentObjective = state.Status == "missed" ? "Talk to Abigail to choose another night. No investigation was completed."
            : state.Status == "active" ? $"Investigate together: {state.Step}/3 steps." : "Meet at the cemetery at 8 pm and talk to Abigail.";
    }
}
