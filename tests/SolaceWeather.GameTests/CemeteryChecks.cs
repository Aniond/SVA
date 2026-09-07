using System.Text.Json;
using Microsoft.Xna.Framework;
using SolaceWeather.Core;
using StardewValley;
using StardewValley.Menus;

namespace SolaceWeather.GameTests;

public sealed partial class ModEntry
{
    private object Cemetery() { Phone(); return PhoneGet<object>(AiMod(Helper), "cemetery"); }
    private void CemeteryState()
    {
        var outing = Cemetery(); var npc = Game1.getCharacterFromName("Abigail");
        Helper.Data.WriteJsonFile("cemetery-state.json", new {
            Ready = PhoneGet<bool>(outing, "Ready"), State = PhoneGet<CemeteryOutingState>(outing, "State"),
            Context = Call(outing, "ContextFor", "Abigail"), Tile = PhoneGet<Point?>(outing, "meetingTile"),
            Day = Game1.Date.TotalDays, Time = Game1.timeOfDay, Game1.player.CanMove,
            Menu = Game1.activeClickableMenu?.GetType().Name, NpcLocation = npc.currentLocation?.Name, NpcTile = npc.Tile,
            npc.ignoreScheduleToday, Schedule = npc.Schedule?.Keys.ToArray(),
            Memory = JsonSerializer.Serialize(Call(PhoneGet<object>(Phone(), "romance"), "GetPhoneContext", "Abigail", "cemetery")) });
    }
    private void CemeteryStage()
    {
        var outing = Cemetery(); StageBackgroundProgress(); Call(Chatter(), "Stop"); PhoneSet(Chatter(), "nextAttempt", double.MaxValue);
        Game1.exitActiveMenu(); Game1.player.isInBed.Value = false; Game1.player.CanMove = true;
        Game1.timeOfDay = 1000;
        Game1.isRaining = false; Game1.isGreenRain = false;
        var npc = Game1.getCharacterFromName("Abigail"); npc.ignoreScheduleToday = false; npc.followSchedule = true;
        npc.TryLoadSchedule();
        Call(outing, "FindCemetery"); CemeteryState();
    }
    private void CemeteryOffer()
    {
        var outing = Cemetery();
        Call(outing, "Reply", "Abigail", new ConversationReply { Reply = "Shall we investigate the cemetery?", QuestRequest = "cemetery" });
        CemeteryState();
    }
    private void CemeteryAccept()
    {
        var outing = Cemetery(); var state = PhoneGet<CemeteryOutingState>(outing, "State");
        if (Call(outing, "Apply", "Abigail", $"cemetery:{state.Status}:{state.OfferDay}:{state.MeetingDay}:accept") == null)
            throw new InvalidOperationException("Cemetery acceptance was unavailable.");
        CemeteryState();
    }
    private void CemeteryMeet()
    {
        var outing = Cemetery(); var state = PhoneGet<CemeteryOutingState>(outing, "State");
        if (state.Status != "accepted") throw new InvalidOperationException("Accept an invitation first.");
        var date = new WorldDate(Game1.Date) { TotalDays = state.MeetingDay };
        Game1.year = date.Year; Game1.season = date.Season; Game1.dayOfMonth = date.DayOfMonth; Game1.timeOfDay = 2000;
        var npc = Game1.getCharacterFromName("Abigail"); npc.ignoreScheduleToday = false; npc.followSchedule = true;
        npc.TryLoadSchedule(); npc.Halt(); npc.controller = null; npc.temporaryController = null;
        npc.isSleeping.Value = false; npc.doingEndOfRouteAnimation.Value = false;
        var tile = PhoneGet<Point?>(outing, "meetingTile") ?? throw new InvalidOperationException("No cemetery tile.");
        Game1.warpFarmer("Town", tile.X, tile.Y + 1, false);
        Call(outing, "Tick"); CemeteryState(); captureRequested = true; captureDelay = 15;
    }
    private void CemeteryTalk()
    {
        var outing = Cemetery(); var npc = Game1.getCharacterFromName("Abigail");
        Game1.activeClickableMenu = new DialogueBox(new Dialogue(npc, null, "You made it. Ready to look around?"));
        if (Game1.activeClickableMenu is not DialogueBox) throw new InvalidOperationException("Native talk did not open.");
        Game1.player.CanMove = false;
        Call(outing, "TryBegin", npc); CemeteryState(); captureRequested = true; captureDelay = 15;
    }
    private void CemeteryAdvance()
    {
        var outing = Cemetery(); var state = PhoneGet<CemeteryOutingState>(outing, "State");
        Call(outing, "Advance", state.Step); CemeteryState(); captureRequested = true; captureDelay = 15;
    }
    private void CemeteryCancel() { Call(Cemetery(), "CancelScene"); CemeteryState(); }
    private void CemeteryLive()
    {
        var outing = Cemetery();
        if (PhoneGet<CemeteryOutingState>(outing, "State").Status != "none") throw new InvalidOperationException("Live invitation check needs no existing invitation.");
        var conversation = PhoneGet<object>(AiMod(Helper), "abigailConversation");
        Call(conversation, "Start", Game1.getCharacterFromName("Abigail"));
        if (Game1.activeClickableMenu is not NamingMenu input) throw new InvalidOperationException("In-person input did not open.");
        input.textBox.Text = "I'd love a spooky but peaceful adventure with you. Could we investigate the cemetery together one night? When are you free?";
        input.textBoxEnter(input.textBox);
    }
    private void CemeteryChecks()
    {
        var outing = Cemetery(); var original = PhoneGet<CemeteryOutingState>(outing, "State");
        var npc = Game1.getCharacterFromName("Abigail"); var checks = new Dictionary<string, bool>();
        var romance = PhoneGet<object>(Phone(), "romance"); var dates = PhoneGet<object>(romance, "Dates");
        int time = Game1.timeOfDay; var position = Game1.player.Position; var originalDate = new WorldDate(Game1.Date);
        try
        {
            // This completion test needs a genuinely free native evening, not whichever
            // workday a preceding scenario happened to leave in the disposable farm.
            bool free = false;
            for (int offset = 0; offset <= 7; offset++)
            {
                var date = new WorldDate(originalDate) { TotalDays = originalDate.TotalDays + offset };
                Game1.year = date.Year; Game1.season = date.Season; Game1.dayOfMonth = date.DayOfMonth;
                npc.ignoreScheduleToday = false; npc.TryLoadSchedule();
                if (Call(dates, "Available", npc, Game1.Date.TotalDays, 1170, "town-walk") is true) { free = true; break; }
            }
            if (!free) throw new InvalidOperationException("No native free evening for the guarded cemetery completion check.");
            Call(outing, "Cleanup");
            var state = new CemeteryOutingState { FarmerId = Game1.player.UniqueMultiplayerID };
            PhoneSet(outing, "state", state);
            checks["ordinary talk without invitation cannot start"] = Call(outing, "TryBegin", npc) is false;
            state.Offer(Game1.Date.TotalDays, Game1.Date.TotalDays); state.Answer(Game1.Date.TotalDays, false);
            checks["declined invitation cannot start"] = Call(outing, "TryBegin", npc) is false;
            state.Status = "accepted"; state.MeetingDay = Game1.Date.TotalDays;
            checks["accepted cemetery reserves activity slot"] = PhoneGet<bool>(outing, "HasReservation");
            Call(dates, "TryOpen", npc, false, false);
            checks["regular activity planning shows reservation message"] = Game1.activeClickableMenu != null
                && PhoneGet<string>(Game1.activeClickableMenu, "text").Contains("outing");
            Game1.exitActiveMenu(); Game1.timeOfDay = 1950;
            checks["early talk cannot begin"] = Call(outing, "TryBegin", npc) is false;
            Game1.timeOfDay = 2030; npc.ignoreScheduleToday = false; npc.followSchedule = true; npc.TryLoadSchedule();
            npc.Halt(); npc.controller = null; npc.temporaryController = null; npc.isSleeping.Value = false;
            npc.doingEndOfRouteAnimation.Value = false;
            var tile = PhoneGet<Point?>(outing, "meetingTile")!.Value;
            Game1.player.Position = new Vector2(tile.X, tile.Y + 1) * 64;
            Game1.activeClickableMenu = new DialogueBox(new Dialogue(npc, null, "Ready?")); Game1.player.CanMove = false;
            // Capture the actual gates before TryBegin can acquire an actor or change scene state.
            var relationship = PhoneGet<RomanceSaveState>(romance, "State");
            var profileAvailable = PhoneGet<Func<NPC, int, int, string, bool>>(dates, "profileAvailable");
            var stops = npc.Schedule?.OrderBy(p => p.Key).Select(p => new {
                NativeTime = p.Key, Minute = p.Key / 100 * 60 + p.Key % 100,
                Place = p.Value.targetLocationName }).ToArray();
            Helper.Data.WriteJsonFile("cemetery-begin-diagnostics.json", new {
                Ready = PhoneGet<bool>(outing, "Ready"), state.Status, state.MeetingDay,
                Today = Game1.Date.TotalDays, Game1.timeOfDay,
                Minute = Game1.timeOfDay / 100 * 60 + Game1.timeOfDay % 100,
                Location = Game1.currentLocation?.Name, PlayerTile = Game1.player.Tile,
                MeetingTile = tile, Distance = Vector2.Distance(Game1.player.Tile, tile.ToVector2()),
                Game1.eventUp, Game1.isRaining, Game1.isGreenRain,
                BoundariesAllow = Call(outing, "BoundariesAllow"), Booking = relationship.Booking,
                Separation = relationship.Characters.GetValueOrDefault("Abigail")?.SeparationUntilDay,
                NativeDivorced = Game1.player.friendshipData.TryGetValue("Abigail", out var friend) && friend.IsDivorced(),
                DateAvailable = Call(dates, "Available", npc, Game1.Date.TotalDays, 1170, "town-walk"),
                ProfileAvailable = profileAvailable(npc, Game1.Date.TotalDays, 1170, "Town"),
                Festival = Utility.isFestivalDay(Game1.dayOfMonth, Game1.season),
                PassiveFestival = Game1.netWorldState.Value.ActivePassiveFestivals.Any(),
                NpcLocation = npc.currentLocation?.Name, NpcTile = npc.Tile,
                Moving = npc.isMoving(), Controller = npc.controller != null,
                TemporaryController = npc.temporaryController != null,
                EndAnimation = npc.doingEndOfRouteAnimation.Value, Sleeping = npc.isSleeping.Value,
                npc.ignoreScheduleToday, npc.followSchedule, IslandSchedule = npc.islandScheduleName.Value,
                Married = npc.isMarried(), Schedule = stops,
                AvailabilityWindow = "Prepare requires a free schedule window at minute 1170 (19:30), with 30 minutes after departure and 90 minutes before next stop."
            });
            checks["native talk at latest grace begins"] = Call(outing, "TryBegin", npc) is true;
            Call(outing, "CancelScene");
            checks["native talk cancellation restores walking"] = Game1.player.CanMove && state.Status == "missed";
            checks["cancel restores Abigail schedule ownership"] = npc.followSchedule && !npc.ignoreScheduleToday;
        }
        finally { Call(outing, "Cleanup"); PhoneSet(outing, "state", original); Game1.year = originalDate.Year; Game1.season = originalDate.Season; Game1.dayOfMonth = originalDate.DayOfMonth; npc.TryLoadSchedule(); Game1.timeOfDay = time; Game1.player.Position = position; Game1.exitActiveMenu(); Game1.player.CanMove = true; Call(outing, "EnsureQuest"); }
        Helper.Data.WriteJsonFile("cemetery-checks.json", new { Passed = checks.Values.All(v => v), Checks = checks });
    }
}
