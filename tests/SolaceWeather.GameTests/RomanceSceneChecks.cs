using System.Reflection;
using Microsoft.Xna.Framework;
using SolaceWeather.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Pathfinding;

namespace SolaceWeather.GameTests;

public sealed partial class ModEntry
{
    /// <summary>Run on a disposable loaded session, then reload without saving: native clock updates also advance world machines.</summary>
    private void RomanceSceneChecks()
    {
        RequireWorld();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var results = new List<object>();
        void Check(string name, bool passed) { results.Add(new { Name = name, Passed = passed }); Monitor.Log($"{(passed ? "PASS" : "FAIL")}: {name}", passed ? LogLevel.Info : LogLevel.Error); }
        int day = Game1.Date.TotalDays;
        object mod = AiMod(Helper);
        Type dateType = mod.GetType().Assembly.GetType("SolaceWeather.Relationships.RomanceDates", true)!;
        const string questId = "David.SolaceWeather/SharedActivity";
        object MakeDates(Func<RomanceSaveState> state, Action<string, string, string>? memory = null) => Activator.CreateInstance(dateType, flags, null,
            new object?[] { state, (Func<NPC, bool>)(_ => true), null, memory, null, null }, null)!;
        var scan = MakeDates(() => new RomanceSaveState());
        foreach (string name in RomanceRules.Candidates)
        {
            NPC candidate = Game1.getCharacterFromName(name);
            int slots = 0; string? first = null;
            for (int d = day; d <= day + 7; d++)
                for (int minute = 600; minute <= 1200; minute += 30)
                    foreach (string activity in new[] { "town-walk", "mountain-lake", "saloon-conversation" })
                    {
                        if (d == day && minute <= Game1.timeOfDay / 100 * 60 + Game1.timeOfDay % 100) continue;
                        if (!(bool)dateType.GetMethod("Available", flags)!.Invoke(scan, new object[] { candidate, d, minute, activity })!) continue;
                        slots++; first ??= $"day {d}, {minute / 60:00}:{minute % 60:00}, {activity}";
                    }
            results.Add(new { Name = name + " has a native-compatible slot within seven days", Passed = slots > 0, Slots = slots, First = first });
        }
        object service = mod.GetType().GetFields(flags).Single(f => f.FieldType.FullName == "SolaceWeather.Relationships.RomanceService").GetValue(mod)!;
        FieldInfo farmField = service.GetType().GetField("farm", flags)!;
        object? originalFarm = farmField.GetValue(service);
        FieldInfo datesField = service.GetType().GetField("<Dates>k__BackingField", flags)!;
        object? originalDates = datesField.GetValue(service);
        object observer = mod.GetType().GetField("abigail", flags)!.GetValue(mod)!;
        FieldInfo memoryField = observer.GetType().GetField("memory", flags)!;
        object? originalMemory = memoryField.GetValue(observer);
        var originalLocation = Game1.currentLocation; var originalFarmerLocation = Game1.player.currentLocation;
        Vector2 originalFarmerPosition = Game1.player.Position;
        int originalTime = Game1.timeOfDay, originalInterval = Game1.gameTimeInterval;
        bool originalCanMove = Game1.player.CanMove, originalEvent = Game1.eventUp;
        bool originalInBed = Game1.player.isInBed.Value, originalTemporaryBed = Game1.player.sleptInTemporaryBed.Value;
        var originalMenu = Game1.activeClickableMenu;
        var originalQuests = Game1.player.questLog.Where(q => q.id.Value == questId).ToArray();
        var friendships = Game1.player.friendshipData.Pairs.ToDictionary(p => p.Key, p => (p.Value.Points, p.Value.Status));
        var npcSnapshots = Utility.getAllCharacters().Where(n => n.currentLocation != null).Select(n => new
        {
            Npc = n, Location = n.currentLocation, n.Position, Facing = n.FacingDirection, n.controller, n.temporaryController,
            n.followSchedule, n.ignoreScheduleToday, n.lastAttemptedSchedule, Queue = n.queuedSchedulePaths.ToArray(),
            n.speed, n.blockedInterval, n.isCharging, Dialogue = n.CurrentDialogue.ToArray(), Sleeping = n.isSleeping.Value,
            Animation = n.doingEndOfRouteAnimation.Value, n.currentScheduleDelay
        }).ToArray();
        NPC npc = Game1.getCharacterFromName("Leah");
        var originalSchedule = npc.Schedule;
        var originalIsland = npc.islandScheduleName.Value;
        RomanceSaveState state = new();
        var memories = new List<string>();
        var dates = MakeDates(() => state, (_, fact, _) => memories.Add(fact));
        object fixture = Activator.CreateInstance(farmField.FieldType, true)!;
        farmField.FieldType.GetProperty("FarmerId")!.SetValue(fixture, Game1.player.UniqueMultiplayerID);
        farmField.FieldType.GetProperty("State")!.SetValue(fixture, state);
        void Invoke(string name, params object[] args) => dateType.GetMethod(name, flags)!.Invoke(dates, args);
        void Diagnose(string stage) => results.Add(new
        {
            Name = "Diagnostic: " + stage, Passed = true, Time = Game1.timeOfDay,
            FarmerLocation = Game1.player.currentLocation?.Name, ActiveLocation = Game1.currentLocation?.Name,
            FarmerCanMove = Game1.player.CanMove, InBed = Game1.player.isInBed.Value, OriginalInBed = originalInBed,
            EventUp = Game1.eventUp, NpcLocation = npc.currentLocation?.Name, NpcX = npc.Position.X, NpcY = npc.Position.Y,
            npc.followSchedule, npc.ignoreScheduleToday, Booking = state.Booking == null ? "none" : $"arrived={state.Booking.Arrived}; segments={state.Booking.SegmentsCompleted}",
            Completions = state.Characters.GetValueOrDefault(npc.Name)?.CompletedActivities ?? 0,
            Memories = memories.Count, Menu = Game1.activeClickableMenu?.GetType().Name,
            SceneValid = (bool)dateType.GetMethod("SceneValid", flags)!.Invoke(dates, null)!
        });
        var scheduleProperty = typeof(NPC).GetProperty("Schedule")!;
        try
        {
            farmField.SetValue(service, fixture);
            datesField.SetValue(service, dates);
            memoryField.SetValue(observer, new AbigailMemory());
            if (Game1.isGreenRain || Utility.isFestivalDay(Game1.dayOfMonth, Game1.season) || Game1.netWorldState.Value.ActivePassiveFestivals.Any())
                throw new InvalidOperationException("Scene fixture requires a normal non-festival, non-green-rain day.");
            GameLocation venue = Game1.getLocationFromName("Saloon"), source = Game1.getLocationFromName("Town");
            Vector2? spot = null;
            for (int x = 5; x < 24 && spot == null; x++)
                for (int y = 8; y < 18 && spot == null; y++)
                {
                    var tile = new Vector2(x, y); var beside = tile + new Vector2(1, 0);
                    if (venue.isTilePassable(tile) && venue.CanItemBePlacedHere(tile) && venue.isTilePassable(beside) && venue.CanItemBePlacedHere(beside)) spot = tile;
                }
            if (spot == null) throw new InvalidOperationException("No open fixture spot in Saloon.");
            Game1.currentLocation = venue; Game1.player.currentLocation = venue; Game1.player.Position = spot.Value * 64;
            // Native Farmer.update normally recalculates these after a warp. This synchronous fixture advances no regular update frame.
            Game1.player.isInBed.Value = false; Game1.player.sleptInTemporaryBed.Value = false;
            Game1.eventUp = false; Game1.player.CanMove = true; Game1.activeClickableMenu = null;
            Game1.warpCharacter(npc, source, new Vector2(35, 70));
            npc.controller = null; npc.temporaryController = null; npc.Halt(); npc.followSchedule = true; npc.ignoreScheduleToday = false;
            npc.isSleeping.Value = false; npc.doingEndOfRouteAnimation.Value = false; npc.islandScheduleName.Value = null;
            var schedule = new Dictionary<int, SchedulePathDescription>
            {
                [1700] = new(new Stack<Point>(), 2, null!, null!, "Town", new Point(35, 70)),
                [2200] = new(new Stack<Point>(), 2, null!, null!, "Town", new Point(35, 70))
            };
            scheduleProperty.SetValue(npc, schedule);
            Vector2 sourcePosition = npc.Position;
            bool BeginFixture()
            {
                Game1.timeOfDay = 1750;
                state.Booking = null;
                bool booked = RomanceRules.Book(state, npc.Name, day, 1080, day, 1070, "saloon-conversation");
                Game1.timeOfDay = 1800;
                Invoke("Begin");
                return booked && state.Booking?.Arrived == true;
            }
            Check("Real scene begins at the appointment", BeginFixture());
            Diagnose("begin");
            Check("Real NPC joins farmer once at the venue", npc.currentLocation == venue && venue.characters.Count(n => ReferenceEquals(n, npc)) == 1 && !source.characters.Contains(npc));
            Check("Scene temporarily locks controls and schedule", !Game1.player.CanMove && npc.ignoreScheduleToday && !npc.followSchedule);
            Invoke("Segment", "We listened carefully."); Diagnose("after beat 1");
            Invoke("Segment", "We shared a story."); Diagnose("after beat 2");
            Invoke("Segment", "We reflected on the hour."); Diagnose("after beat 3");
            Check("Three explicit beats advance the real clock sixty minutes", Game1.timeOfDay == 1900);
            Check("Only one completed activity is credited", state.Booking == null && state.Characters[npc.Name].CompletedActivities == 1);
            Check("Completed scene restores original NPC position and controls", npc.currentLocation == source && npc.Position == sourcePosition && Game1.player.CanMove && npc.followSchedule && !npc.ignoreScheduleToday);
            Check("Only actual segments and completion reach memory callback", memories.Count == 3 && memories.Count(m => m.StartsWith("Completed")) == 1);
            Check("Completion removes native booking journal entry", !Game1.player.questLog.Any(q => q.id.Value == questId));
            Game1.activeClickableMenu = null;
            Check("Early departure scene begins", BeginFixture());
            Invoke("CancelForTransition");
            Diagnose("early departure");
            Check("Early departure restores NPC without another completion", state.Booking == null && npc.currentLocation == source && npc.Position == sourcePosition && Game1.player.CanMove && state.Characters[npc.Name].CompletedActivities == 1);
            Game1.activeClickableMenu = null;
            Check("Interrupted scene begins", BeginFixture());
            Game1.activeClickableMenu = null;
            Invoke("OnUpdateTicked");
            Diagnose("interruption");
            Check("Menu interruption restores NPC and farmer", state.Booking == null && npc.currentLocation == source && Game1.player.CanMove && !npc.ignoreScheduleToday);
            Check("Native friendship points and statuses are unchanged", friendships.All(p => Game1.player.friendshipData.TryGetValue(p.Key, out var now) && now.Points == p.Value.Points && now.Status == p.Value.Status));
        }
        catch (Exception ex) { results.Add(new { Name = "Scene fixture completed", Passed = false, Error = ex.ToString() }); }
        finally
        {
            Game1.eventUp = false;
            Invoke("CancelForTransition");
            scheduleProperty.SetValue(npc, originalSchedule); npc.islandScheduleName.Value = originalIsland;
            foreach (var snap in npcSnapshots)
            {
                NPC n = snap.Npc;
                Game1.warpCharacter(n, snap.Location, snap.Position / 64);
                n.Position = snap.Position; n.faceDirection(snap.Facing); n.controller = snap.controller; n.temporaryController = snap.temporaryController;
                n.followSchedule = snap.followSchedule; n.ignoreScheduleToday = snap.ignoreScheduleToday; n.lastAttemptedSchedule = snap.lastAttemptedSchedule;
                n.queuedSchedulePaths.Clear(); n.queuedSchedulePaths.AddRange(snap.Queue);
                n.speed = snap.speed; n.blockedInterval = snap.blockedInterval; n.isCharging = snap.isCharging;
                n.CurrentDialogue.Clear(); for (int i = snap.Dialogue.Length - 1; i >= 0; i--) n.CurrentDialogue.Push(snap.Dialogue[i]);
                n.isSleeping.Value = snap.Sleeping; n.doingEndOfRouteAnimation.Value = snap.Animation; n.currentScheduleDelay = snap.currentScheduleDelay;
            }
            Game1.currentLocation = originalLocation; Game1.player.currentLocation = originalFarmerLocation; Game1.player.Position = originalFarmerPosition;
            Game1.timeOfDay = originalTime; Game1.gameTimeInterval = originalInterval; Game1.eventUp = originalEvent;
            Game1.player.CanMove = originalCanMove; Game1.activeClickableMenu = originalMenu;
            Game1.player.isInBed.Value = originalInBed; Game1.player.sleptInTemporaryBed.Value = originalTemporaryBed;
            farmField.SetValue(service, originalFarm); datesField.SetValue(service, originalDates); memoryField.SetValue(observer, originalMemory);
            foreach (var quest in Game1.player.questLog.Where(q => q.id.Value == questId).ToArray()) Game1.player.questLog.Remove(quest);
            foreach (var quest in originalQuests) Game1.player.questLog.Add(quest);
            Helper.Data.WriteJsonFile("romance-scene-results.json", results);
            Monitor.Log("Scene checks finished. Reload this isolated session without saving; native clock updates advanced world timers.", LogLevel.Info);
        }
    }
}
