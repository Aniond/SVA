using Microsoft.Xna.Framework;
using SolaceWeather.Core;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Pathfinding;
using StardewValley.Quests;

namespace SolaceWeather.Relationships;

/// <summary>Host-owned appointments. Only an on-screen, explicitly chosen third beat can complete one.</summary>
internal sealed class RomanceDates
{
    private readonly Func<RomanceSaveState> getState;
    private readonly Func<NPC, bool> canHelpNpc;
    private readonly Func<NPC, int, int, string, bool> profileAvailable;
    private readonly Action<string, string, string>? remember;
    private readonly Func<NPC, string, Task<string>>? narrate;
    private readonly Action<string, string>? signal;
    private string? promptedBooking;
    private int promptAgainMinute;
    internal const string QuestId = "David.SolaceWeather/SharedActivity";
    private Task<string>? narration;
    private RomanceDateMenu? narrationMenu;
    private string lastReply = "";
    private NPC? actor;
    private GameLocation? origin;
    private GameLocation? meetingLocation;
    private Vector2 originPosition;
    private int originFacing;
    private bool originFollowSchedule, originIgnoreSchedule, farmerCanMove;
    private PathFindController? originController;
    private int originSpeed, originBlocked;
    private bool originCharging;
    private Dialogue[] originDialogue = Array.Empty<Dialogue>();
    private RomanceDateMenu? scene;
    private bool advancing;
    private static int Today => Game1.Date.TotalDays;
    private static int Minute => Game1.timeOfDay / 100 * 60 + Game1.timeOfDay % 100;
    private static string Venue(string type) => type switch { "town-walk" => "Town", "mountain-lake" => "Mountain", _ => "Saloon" };
    private static string Label(string type) => type switch { "town-walk" => "Town walk", "mountain-lake" => "Quiet mountain lake", _ => "Saloon conversation" };

    internal RomanceDates(Func<RomanceSaveState> getState, Func<NPC, bool> canHelpNpc,
        Func<NPC, int, int, string, bool>? profileAvailable = null, Action<string, string, string>? remember = null,
        Func<NPC, string, Task<string>>? narrate = null, Action<string, string>? signal = null)
    {
        this.getState = getState; this.canHelpNpc = canHelpNpc;
        this.profileAvailable = profileAvailable ?? ((npc, _, minute, venue) =>
        {
            var profile = RomanceProfiles.Get(npc.Name);
            return profile != null && !IsWorkPlace(profile, venue)
                && minute >= profile.EarliestDateTime / 100 * 60 + profile.EarliestDateTime % 100
                && minute + 90 <= profile.LatestDateTime / 100 * 60 + profile.LatestDateTime % 100;
        });
        this.remember = remember;
        this.narrate = narrate;
        this.signal = signal;
    }

    internal bool TryOpen(NPC npc, bool romantic = false, bool repair = false)
    {
        if (!Game1.IsMasterGame || !canHelpNpc(npc) || Game1.eventUp) return false;
        if (getState().Booking is { } booking)
        {
            Show($"Meeting {booking.Npc}: {Label(booking.Type)}, {DateLabel(booking.Day)}, {Clock(booking.StartMinute)}. Meet in {Venue(booking.Type)}; arrival grace is 30 minutes.",
                ("Begin our meeting", () => Begin()),
                ("Cancel / choose another day", () => { if (RomanceRules.CancelBooking(getState(), Today, Minute)) { EnsureQuest(); TryOpen(npc, romantic, repair); } else Show("It is too late to cancel in advance."); }),
                ("Keep the plan", () => Game1.exitActiveMenu()));
            return true;
        }
        Show($"Plan an hour with {npc.displayName}. What would you enjoy?",
            ("Town walk", () => ChooseDay(npc, "town-walk", romantic, repair)),
            ("Quiet mountain lake", () => ChooseDay(npc, "mountain-lake", romantic, repair)),
            ("Saloon conversation", () => ChooseDay(npc, "saloon-conversation", romantic, repair)));
        return true;
    }

    internal bool OpenAppointment() => getState().Booking is { } booking && TryOpen(Game1.getCharacterFromName(booking.Npc));

    private void ChooseDay(NPC npc, string type, bool romantic, bool repair)
    {
        var options = new List<(string, Action)>();
        for (int day = Today; day <= Today + 7; day++)
        {
            int chosenDay = day;
            for (int minute = 600; minute <= 1200; minute += 30)
            {
                if (day == Today && minute <= Minute || !Available(npc, day, minute, type)) continue;
                int chosenMinute = minute;
                options.Add(($"{DateLabel(day)}, {Clock(minute)}", () =>
                {
                    if (Available(npc, chosenDay, chosenMinute, type) && RomanceRules.Book(getState(), npc.Name, chosenDay, chosenMinute, Today, Minute, type, romantic, repair))
                    {
                        EnsureQuest();
                        if (romantic) signal?.Invoke(npc.Name, "date");
                        Show($"Agreed: {Label(type)} in {Venue(type)}, {DateLabel(chosenDay)} at {Clock(chosenMinute)}. Come to that location; a meeting prompt will appear. Weather or changed schedules cancel it without blame. Your journal has the details.");
                    }
                    else Show("That time is no longer available.");
                }));
                break;
            }
        }
        Show(options.Count == 0 ? "There is no confirmed free hour for this activity in the next seven days. Try another activity." : "Choose a confirmed free hour. Plans can be cancelled before the meeting.", options.ToArray());
    }

    internal bool Available(NPC npc, int day, int minute, string type)
    {
        var date = new WorldDate(Game1.Date) { TotalDays = day };
        string season = date.Season.ToString().ToLowerInvariant();
        if (Utility.isFestivalDay(date.DayOfMonth, date.Season) || !profileAvailable(npc, day, minute, Venue(type))) return false;
        if (day == Today + 1 && Venue(type) != "Saloon" && Game1.weatherForTomorrow is Game1.weather_rain or Game1.weather_lightning or Game1.weather_green_rain) return false;
        if (day == Today)
        {
            if (Game1.eventUp || Game1.isGreenRain || Game1.netWorldState.Value.ActivePassiveFestivals.Any()) return false;
            if (Venue(type) != "Saloon" && Game1.isRaining) return false;
            if (npc.ignoreScheduleToday || !string.IsNullOrEmpty(npc.islandScheduleName.Value)) return false;
            if (npc.Schedule == null || npc.Schedule.Count == 0)
                return npc.isMarried() && npc.currentLocation != null && (npc.currentLocation.Name.StartsWith("FarmHouse", StringComparison.Ordinal) || npc.currentLocation.Name == "Farm")
                    && npc.controller == null && npc.temporaryController == null && !npc.isMoving();
            var stops = npc.Schedule.OrderBy(p => p.Key).Select(p => (Time: p.Key / 100 * 60 + p.Key % 100, Place: p.Value.targetLocationName)).ToArray();
            return HasLeisureWindow(npc, stops, minute);
        }
        // Future schedules are read, never loaded onto the live NPC. Reject conditional routes whose native conditions cannot yet be known.
        var raw = npc.getMasterScheduleRawData();
        if (raw == null) return false;
        string weekday = Game1.shortDayNameFromDayOfSeason(date.DayOfMonth);
        int hearts = Math.Max(0, Utility.GetAllPlayerFriendshipLevel(npc)) / 250;
        var keys = new List<string> { $"{season}_{date.DayOfMonth}" };
        if (npc.isMarried())
        {
            keys.Clear();
            keys.Add($"marriage_{season}_{date.DayOfMonth}");
            bool workday = npc.Name == "Penny" && weekday is "Tue" or "Wed" or "Fri"
                || npc.Name is "Maru" or "Harvey" && weekday is "Tue" or "Thu";
            if (workday) keys.Add("marriageJob");
            keys.Add("marriage_" + weekday);
            string? spouseKey = keys.FirstOrDefault(raw.ContainsKey);
            // Native TryLoadSchedule clears the schedule on these spouse home days. Authored profile hours still apply, and actual presence/chores are checked on arrival.
            return spouseKey == null || ResolveSimpleSchedule(raw, spouseKey, season) is { } spouseRaw && ParseLeisureWindow(npc, spouseRaw, minute);
        }
        for (int h = hearts; h > 0; h--) keys.Add($"{date.DayOfMonth}_{h}");
        keys.Add(date.DayOfMonth.ToString());
        for (int h = hearts; h > 0; h--) keys.Add($"{season}_{weekday}_{h}");
        keys.Add($"{season}_{weekday}");
        for (int h = hearts; h > 0; h--) keys.Add($"{weekday}_{h}");
        keys.AddRange(new[] { weekday, season, $"spring_{weekday}", "spring" });
        string? selected = keys.FirstOrDefault(raw.ContainsKey);
        if (selected == null) return false;
        return ResolveSimpleSchedule(raw, selected, season) is { } resolved && ParseLeisureWindow(npc, resolved, minute);
    }

    internal static string? ResolveSimpleSchedule(Dictionary<string, string> schedules, string key, string season)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        while (visited.Count < 8 && visited.Add(key) && schedules.TryGetValue(key, out string? raw))
        {
            string[] first = raw.Split('/')[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (first.Length == 0 || first[0] != "GOTO") return raw;
            if (first.Length != 2 || first[1].Equals("no_schedule", StringComparison.OrdinalIgnoreCase)) return null;
            key = first[1].Equals("season", StringComparison.OrdinalIgnoreCase) ? (schedules.ContainsKey(season) ? season : "spring") : first[1];
        }
        return null;
    }

    private static bool ParseLeisureWindow(NPC npc, string raw, int minute)
    {
        var parsed = new List<(int Time, string Place)>();
        foreach (string entry in raw.Split('/'))
        {
            string[] fields = entry.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length < 4 || !int.TryParse(fields[0].TrimStart('a'), out int time)) return false;
            parsed.Add((time / 100 * 60 + time % 100, fields[1]));
        }
        return HasLeisureWindow(npc, parsed.ToArray(), minute);
    }

    private static bool HasLeisureWindow(NPC npc, (int Time, string Place)[] stops, int minute)
    {
        var profile = RomanceProfiles.Get(npc.Name);
        if (profile == null) return false;
        return stops.Any(stop => !IsWorkPlace(profile, stop.Place)
            && !stop.Place.Contains("Hospital", StringComparison.OrdinalIgnoreCase)
            && !stop.Place.Contains("Island", StringComparison.OrdinalIgnoreCase)
            && FreeWindow(stops, minute, stop.Place));
    }
    private static bool IsWorkPlace(RomanceProfile profile, string place) => profile.WorkLocations.Contains(place, StringComparer.OrdinalIgnoreCase)
        || place == "ArchaeologyHouse" && profile.WorkLocations.Contains("Museum", StringComparer.OrdinalIgnoreCase);

    internal static bool FreeWindow((int Time, string Place)[] stops, int start, string venue)
    {
        // Leave thirty minutes after the native departure time for travel, and reserve the full arrival-grace + activity window.
        var ordered = stops.OrderBy(s => s.Time).ToArray();
        for (int i = 0; i < ordered.Length; i++)
            if (ordered[i].Place == venue && ordered[i].Time + 30 <= start && (i + 1 == ordered.Length ? 1440 : ordered[i + 1].Time) >= start + 90) return true;
        return false;
    }

    private void Begin()
    {
        var booking = getState().Booking;
        if (booking == null) return;
        NPC npc = Game1.getCharacterFromName(booking.Npc);
        if (booking.Day != Today || Minute < booking.StartMinute || Minute > booking.StartMinute + 30) { Show("Come back at the agreed time (within thirty minutes)."); return; }
        if (!BookingEligible(booking) || !Available(npc, Today, booking.StartMinute, booking.Type) || !canHelpNpc(npc)) { GameCancel(); Show("Today's conditions changed. The meeting is cancelled without blame."); return; }
        if (Game1.currentLocation.Name != Venue(booking.Type)) { Show($"Meet in {Venue(booking.Type)} first."); return; }
        var profile = RomanceProfiles.Get(npc.Name);
        if (npc.currentLocation == null || profile == null || IsWorkPlace(profile, npc.currentLocation.Name)
            || npc.currentLocation.Name.Contains("Hospital", StringComparison.OrdinalIgnoreCase)
            || npc.isMoving() || npc.controller != null || npc.temporaryController != null || npc.doingEndOfRouteAnimation.Value)
        { Show("They have not reached their free-time stop yet. Give them a moment to arrive."); return; }
        Vector2 meetingTile = Game1.player.Tile + new Vector2(1, 0);
        if (!Game1.currentLocation.isTilePassable(meetingTile) || !Game1.currentLocation.CanItemBePlacedHere(meetingTile))
        { Show("Choose an open spot with room beside you, then begin the meeting."); return; }
        if (!RomanceRules.Arrive(getState(), Today, Minute)) return;
        actor = npc; origin = npc.currentLocation; originPosition = npc.Position; originFacing = npc.FacingDirection;
        meetingLocation = Game1.currentLocation;
        originSpeed = npc.speed; originBlocked = npc.blockedInterval; originCharging = npc.isCharging; originDialogue = npc.CurrentDialogue.ToArray();
        originController = npc.controller; originFollowSchedule = npc.followSchedule; originIgnoreSchedule = npc.ignoreScheduleToday; farmerCanMove = Game1.player.CanMove;
        npc.followSchedule = false; npc.ignoreScheduleToday = true; npc.controller = null; npc.Halt(); Game1.player.CanMove = false;
        try
        {
            Game1.warpCharacter(npc, meetingLocation, meetingTile);
            RestoreWarpFields(npc);
            npc.faceDirection(3);
            lastReply = "";
            NextBeat();
        }
        catch { CancelForTransition(); throw; }
    }

    private void NextBeat()
    {
        var booking = getState().Booking;
        if (booking == null || actor == null) return;
        string[] beats = booking.Type switch
        {
            "town-walk" => new[] { "Pause together and take in the town.", "Talk about a small moment from today.", "Decide what you want to remember from this walk." },
            "mountain-lake" => new[] { "Settle beside the lake and listen.", "Share what has been on your mind.", "Let the quiet settle before saying goodbye." },
            _ => new[] { "Settle in together at the saloon.", "Share a story and listen to theirs.", "Talk about what you enjoyed in this hour." }
        };
        int beat = booking.SegmentsCompleted;
        string prefix = booking.Repair ? "Make room for honesty without demanding forgiveness. " : booking.Romantic ? "Enjoy time together without rushing either person. " : "A friendly hour together. ";
        scene = new RomanceDateMenu(actor, $"{Label(booking.Type)} — {beat + 1}/3\n{prefix}{beats[Math.Clamp(beat, 0, 2)]}\n{lastReply}",
            new[] { ("Share something honest (20 minutes)", (Action)(() => Segment("We shared an honest conversation."))), ("Listen and ask about their day (20 minutes)", (Action)(() => Segment("We listened and talked about the day."))), ("Leave early", (Action)(() => { CancelForTransition(); Show("You leave the meeting early. This was not a completed activity."); })) },
            () => CancelForTransition());
        Game1.activeClickableMenu = scene;
        if (narrate != null)
        {
            try { narrationMenu = scene; narration = narrate(actor, $"We are present together for {Label(booking.Type)}, beat {beat + 1} of 3. {beats[Math.Clamp(beat, 0, 2)]} Give one short in-character conversational line. Do not claim this activity is completed or change the relationship."); }
            catch { narration = null; }
        }
    }

    private void Segment(string reflection)
    {
        if (advancing || actor == null || getState().Booking == null) return;
        advancing = true;
        try
        {
            for (int i = 0; i < 2; i++)
            {
                if (!SceneValid()) { CancelForTransition(); return; }
                Game1.performTenMinuteClockUpdate();
                if (!SceneValid()) { CancelForTransition(); return; }
            }
            string npcName = actor.Name;
            var booking = getState().Booking!;
            string type = booking.Type;
            bool final = booking.SegmentsCompleted == 2;
            lastReply = AuthoredReply(actor.Name, booking.Repair);
            if (!RomanceRules.RecordActivitySegment(getState(), Today, Minute)) { CancelForTransition(); return; }
            EnsureQuest();
            if (final)
            {
                Restore();
                remember?.Invoke(npcName, $"Completed a sixty-minute {Label(type).ToLowerInvariant()} together in {Venue(type)}.", reflection + " " + lastReply);
                Show("You spent the full hour together. This shared activity is now part of your history.");
            }
            else
            {
                remember?.Invoke(npcName, $"During our {Label(type).ToLowerInvariant()} in {Venue(type)}, we spent twenty minutes together. {reflection}", lastReply);
                NextBeat();
            }
        }
        catch
        {
            CancelForTransition();
            throw;
        }
        finally { advancing = false; }
    }

    private bool SceneValid() => actor != null && meetingLocation != null && getState().Booking is { Arrived: true } booking && BookingEligible(booking) && Game1.currentLocation == meetingLocation && actor.currentLocation == meetingLocation && !Game1.eventUp && !Game1.player.isInBed.Value && Game1.activeClickableMenu == scene;
    internal void OnUpdateTicked()
    {
        if (actor != null && !advancing && !SceneValid()) CancelForTransition();
        if (narration?.IsCompleted == true)
        {
            if (narration.IsCompletedSuccessfully && scene != null && scene == narrationMenu)
                scene.SetNarration(narration.Result);
            narration = null; narrationMenu = null;
        }
        if (actor == null && getState().Booking is { } booking && booking.Day == Today && Minute >= booking.StartMinute && Minute <= booking.StartMinute + 30
            && Game1.currentLocation?.Name == Venue(booking.Type) && Game1.activeClickableMenu == null && !Game1.eventUp && Game1.currentMinigame == null && Game1.player.CanMove)
        {
            string id = $"{booking.Npc}:{booking.Day}:{booking.StartMinute}";
            if (promptedBooking != id || Minute >= promptAgainMinute) { promptedBooking = id; promptAgainMinute = Minute + 10; OpenAppointment(); }
        }
    }
    internal void OnTimeChanged()
    {
        if (actor != null) { if (!advancing && !SceneValid()) CancelForTransition(); return; }
        if (getState().Booking is { } existing && !BookingEligible(existing)) { GameCancel(); return; }
        if (getState().Booking is { } b && b.Day == Today && Minute >= b.StartMinute)
        {
            NPC npc = Game1.getCharacterFromName(b.Npc);
            if (!Available(npc, Today, b.StartMinute, b.Type)) { GameCancel(); return; }
        }
        RomanceRules.AdvanceBooking(getState(), Today, Minute);
        EnsureQuest();
    }
    private bool BookingEligible(ActivityBooking booking)
    {
        var c = getState().Characters.GetValueOrDefault(booking.Npc);
        return c != null && c.PendingTransition == null
            && (!booking.Romantic || c.IsDating && !c.InConflict && !c.NeedsReaffirmation && c.SeparationUntilDay == null)
            && (!booking.Repair || c.InConflict && c.RepairAcknowledged);
    }
    internal void OnDayStarted() { promptedBooking = null; if (getState().Booking?.Arrived == true) GameCancel(); OnTimeChanged(); EnsureQuest(); }
    internal void CancelForTransition() { if (actor != null) GameCancel(); Restore(); }
    private void GameCancel() { RomanceRules.GameCancelBooking(getState(), Today); EnsureQuest(); }
    private void Restore()
    {
        bool hadActor = actor != null;
        if (actor != null)
        {
            try
            {
                actor.followSchedule = originFollowSchedule;
                actor.ignoreScheduleToday = originIgnoreSchedule;
                actor.controller = originController;
                if (origin != null && actor.currentLocation == meetingLocation)
                {
                    Game1.warpCharacter(actor, origin, originPosition / 64);
                    RestoreWarpFields(actor);
                    actor.Position = originPosition; actor.faceDirection(originFacing);
                }
                if (originFollowSchedule && !Game1.eventUp) actor.checkSchedule(Game1.timeOfDay);
            }
            finally
            {
                // An event or sleep transition owns its own movement lock; do not release that lock.
                if (!Game1.eventUp && Game1.currentMinigame == null && !Game1.player.isInBed.Value) Game1.player.CanMove = farmerCanMove;
                actor = null;
            }
        }
        actor = null; origin = null; meetingLocation = null; scene = null; originController = null; originDialogue = Array.Empty<Dialogue>();
        narration = null; narrationMenu = null;
        if (hadActor) EnsureQuest();
    }
    private void RestoreWarpFields(NPC npc)
    {
        npc.speed = originSpeed; npc.blockedInterval = originBlocked; npc.isCharging = originCharging;
        npc.CurrentDialogue.Clear();
        for (int i = originDialogue.Length - 1; i >= 0; i--) npc.CurrentDialogue.Push(originDialogue[i]);
    }
    private static string AuthoredReply(string name, bool repair) => repair ? "Thank you for listening. I still need time, and I want us to be honest." : name switch
    {
        "Abigail" => "It's good to get out for a while. I like having room to just be myself.",
        "Alex" => "I don't always know what to say. But I'm glad we made time for this.",
        "Elliott" => "An unhurried conversation can change the shape of a day.",
        "Emily" => "I like hearing what people notice. Everyone sees something different.",
        "Haley" => "Sometimes a little detail is the best part of the whole day.",
        "Harvey" => "It's nice to slow down and listen for a change.",
        "Leah" => "I like leaving a little space in the day for something unexpected.",
        "Maru" => "Tell me more. I like finding out how other people see things.",
        "Penny" => "You don't have to have a perfect story. I'm happy to listen.",
        "Sam" => "We should make time for little breaks like this more often.",
        "Sebastian" => "It's easier to talk when there's no pressure to fill every silence.",
        "Shane" => "I'm not great at this. But I heard you, and I appreciate the company.",
        _ => "I'm glad we made time to talk."
    };
    private static string Clock(int minute) => $"{minute / 60:00}:{minute % 60:00}";
    private static string DateLabel(int day) => AbigailDeliveryQuest.DateLabel(day);
    internal void EnsureQuest()
    {
        var booking = getState().Booking;
        var managed = Game1.player.questLog.Where(q => q.id.Value == QuestId).ToArray();
        foreach (Quest extra in managed.Skip(booking == null ? 0 : 1)) Game1.player.questLog.Remove(extra);
        if (booking == null) return;
        Quest? quest = managed.FirstOrDefault();
        if (quest == null)
        {
            quest = new Quest(); quest.id.Value = QuestId; quest.questType.Value = Quest.type_basic;
            quest.accepted.Value = true; quest.showNew.Value = true; quest.canBeCancelled.Value = false;
            Game1.player.questLog.Add(quest);
        }
        quest.questTitle = $"An hour with {booking.Npc}";
        quest.questDescription = $"{Label(booking.Type)} in {Venue(booking.Type)} on {DateLabel(booking.Day)} at {Clock(booking.StartMinute)}. Arrive within thirty minutes. Enter the location with no menu open and choose Begin our meeting in the arrival prompt. Complete all three twenty-minute choices. Cancel or reschedule through their shared-activity services before the meeting. Changed weather or schedules cancel without blame.";
        quest.currentObjective = booking.Arrived ? $"Spend time together: {booking.SegmentsCompleted}/3 parts." : $"Meet in {Venue(booking.Type)} at {Clock(booking.StartMinute)} on {DateLabel(booking.Day)}.";
    }
    private static void Show(string text, params (string Label, Action Action)[] options) => Game1.activeClickableMenu = new RomanceDateMenu(null, text, options, () => Game1.exitActiveMenu());
}
