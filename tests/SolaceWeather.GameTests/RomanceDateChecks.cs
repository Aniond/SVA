using System.Reflection;
using SolaceWeather.Core;
using StardewValley;

namespace SolaceWeather.GameTests;

public sealed partial class ModEntry
{
    private void RomanceDateChecks()
    {
        RequireWorld();
        var results = new List<object>();
        void Check(string name, bool passed) => results.Add(new { Name = name, Passed = passed });
        Type type = AiMod(Helper).GetType().Assembly.GetType("SolaceWeather.Relationships.RomanceDates", true)!;
        MethodInfo window = type.GetMethod("FreeWindow", BindingFlags.Static | BindingFlags.NonPublic)!;
        bool Free((int, string)[] stops, int start, string venue) => (bool)window.Invoke(null, new object[] { stops, start, venue })!;
        Check("Cannot count travel as free time", !Free(new[] { (1020, "Town"), (1200, "SeedShop") }, 1020, "Town"));
        Check("Confirmed ninety-minute venue window accepted", Free(new[] { (1020, "Town"), (1200, "SeedShop") }, 1050, "Town"));
        Check("Work departure inside grace plus activity rejected", !Free(new[] { (1020, "Town"), (1120, "SeedShop") }, 1050, "Town"));
        Check("Wrong meeting location rejected", !Free(new[] { (1020, "Hospital"), (1200, "Town") }, 1050, "Town"));
        Check("Missing schedule is not permission", !Free(Array.Empty<(int, string)>(), 1050, "Town"));
        MethodInfo resolve = type.GetMethod("ResolveSimpleSchedule", BindingFlags.Static | BindingFlags.NonPublic)!;
        var schedules = new Dictionary<string, string> { ["summer"] = "GOTO spring", ["spring"] = "900 Town 10 10 2/1800 SeedShop 1 1 2", ["cycle"] = "GOTO cycle" };
        Check("Native seasonal schedule redirects are resolved without mutating NPCs", (string?)resolve.Invoke(null, new object[] { schedules, "summer", "summer" }) == schedules["spring"]);
        Check("Cyclic schedule redirects are safely unavailable", resolve.Invoke(null, new object[] { schedules, "cycle", "spring" }) == null);
        var state = new RomanceSaveState();
        int day = Game1.Date.TotalDays;
        Check("Appointment booked", RomanceRules.Book(state, "Abigail", day, 1080, day, 1000, "town-walk"));
        Check("Arrival recorded", RomanceRules.Arrive(state, day, 1080));
        RomanceRules.AdvanceBooking(state, day, 1200);
        Check("Elapsed time cannot complete an offscreen activity", state.Booking?.SegmentsCompleted == 0 && state.Characters["Abigail"].CompletedActivities == 0);
        Check("First explicit twenty-minute segment", RomanceRules.RecordActivitySegment(state, day, 1100));
        Check("Second segment cannot reuse the first time", !RomanceRules.RecordActivitySegment(state, day, 1100));
        Check("Second explicit segment", RomanceRules.RecordActivitySegment(state, day, 1120));
        Check("Only final explicit segment completes", RomanceRules.RecordActivitySegment(state, day, 1140) && state.Booking == null && state.Characters["Abigail"].CompletedActivities == 1);
        Check("Private interest cannot authorize a romantic booking", !RomanceRules.Book(state, "Abigail", day + 1, 1080, day, 1140, "town-walk", romantic: true));
        const string questId = "David.SolaceWeather/SharedActivity";
        var originals = Game1.player.questLog.Where(q => q.id.Value == questId).ToArray();
        int unrelated = Game1.player.questLog.Count(q => q.id.Value != questId);
        try
        {
            foreach (var quest in originals) Game1.player.questLog.Remove(quest);
            object dates = Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.NonPublic, null,
                new object?[] { (Func<RomanceSaveState>)(() => state), (Func<NPC, bool>)(_ => true), null, null, null, null }, null)!;
            var eligibility = type.GetMethod("BookingEligible", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var romantic = new ActivityBooking { Npc = "Abigail", Romantic = true };
            bool Eligible(ActivityBooking candidate) => (bool)eligibility.Invoke(dates, new object[] { candidate })!;
            state.Characters["Abigail"].IsDating = true;
            Check("Existing dating supports romantic appointment eligibility", Eligible(romantic));
            state.Characters["Abigail"].InConflict = true;
            Check("New conflict invalidates an existing romantic appointment", !Eligible(romantic));
            var repair = new ActivityBooking { Npc = "Abigail", Repair = true };
            Check("Repair appointment requires acknowledgement", !Eligible(repair));
            state.Characters["Abigail"].RepairAcknowledged = true;
            Check("Acknowledged conflict allows designated repair appointment", Eligible(repair));
            state.Characters["Abigail"].InConflict = false;
            state.Characters["Abigail"].RepairAcknowledged = false;
            state.Characters["Abigail"].IsDating = false;
            MethodInfo sync = type.GetMethod("EnsureQuest", BindingFlags.Instance | BindingFlags.NonPublic)!;
            RomanceRules.Book(state, "Abigail", day + 1, 1080, day, 1140, "town-walk");
            sync.Invoke(dates, null); sync.Invoke(dates, null);
            var managed = Game1.player.questLog.Where(q => q.id.Value == questId).ToArray();
            Check("Native journal has exactly one managed appointment", managed.Length == 1 && managed[0].questTitle.Contains("Abigail") && managed[0].currentObjective.Contains("Town"));
            RomanceRules.CancelBooking(state, day, 1140); sync.Invoke(dates, null);
            Check("Cancellation removes managed journal entry", !Game1.player.questLog.Any(q => q.id.Value == questId));
            Check("Journal sync preserves unrelated native quests", Game1.player.questLog.Count(q => q.id.Value != questId) == unrelated);
        }
        finally
        {
            foreach (var quest in Game1.player.questLog.Where(q => q.id.Value == questId).ToArray()) Game1.player.questLog.Remove(quest);
            foreach (var quest in originals) Game1.player.questLog.Add(quest);
        }
        Helper.Data.WriteJsonFile("romance-date-results.json", results);
    }
}
