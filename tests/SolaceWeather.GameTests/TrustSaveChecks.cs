using System.Reflection;
using System.Text.Json;
using SolaceWeather.Core;
using StardewValley;

namespace SolaceWeather.GameTests;

public sealed partial class ModEntry
{
    private AbigailMemory LiveMemory()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        object mod = AiMod(Helper);
        object observer = mod.GetType().GetField("abigail", flags)!.GetValue(mod)!;
        Call(observer, "Observe");
        return (AbigailMemory)observer.GetType().GetField("memory", flags)!.GetValue(observer)!;
    }

    private void ClickTrustChoice(string action)
    {
        RequireWorld();
        var menu = Game1.activeClickableMenu ?? throw new InvalidOperationException("Open Abigail's conversation first.");
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        var choices = (Array)menu.GetType().GetField("visibleChoices", flags)!.GetValue(menu)!;
        int index = Array.FindIndex(choices.Cast<object>().ToArray(), row => ((string)Get(row, "Key")!).EndsWith(":" + action));
        if (index < 0) throw new InvalidOperationException("Requested quest choice is not currently visible.");
        var bounds = (Microsoft.Xna.Framework.Rectangle)Call(menu, "QuestBounds", index)!;
        menu.receiveLeftClick(bounds.Center.X, bounds.Center.Y);
    }

    private void TrustBed()
    {
        RequireWorld(); StageBackgroundProgress(); Game1.exitActiveMenu();
        var house = (StardewValley.Locations.FarmHouse)Game1.getLocationFromName("FarmHouse");
        var bed = house.GetPlayerBedSpot();
        Game1.warpFarmer("FarmHouse", bed.X, bed.Y, false);
    }

    private void TrustSleep()
    {
        RequireWorld();
        if (Game1.player.farmName.Value != "Solace") throw new InvalidOperationException("Overnight test is restricted to Solace.");
        var memory = LiveMemory();
        var ledger = memory.Promises;
        var expected = JsonSerializer.Deserialize<PromiseLedger>(JsonSerializer.Serialize(ledger))!;
        expected.AdvanceDay(Game1.Date.TotalDays + 1);
        var tree = JsonSerializer.Deserialize<RelationshipTreeState>(JsonSerializer.Serialize(memory.Tree))!;
        tree.Observe(expected, Game1.Date.TotalDays + 1, memory.Personal.Activities.Where(a => a.FirstMineVisitTime != null).Select(a => a.Day));
        var experiences = JsonSerializer.Deserialize<SharedExperienceStore>(JsonSerializer.Serialize(memory.Experiences))!;
        experiences.Observe(expected, tree);
        Helper.Data.WriteJsonFile("trust-overnight-expected.json", new OvernightExpectation { Day = Game1.Date.TotalDays + 1, Promises = expected, Tree = tree, Experiences = experiences });
        StageBackgroundProgress();
        Game1.exitActiveMenu();
        if (Game1.currentLocation is not StardewValley.Locations.FarmHouse) throw new InvalidOperationException("Move to the farmhouse bed with trustbed first.");
        Game1.player.isInBed.Value = true;
        Game1.currentLocation.answerDialogueAction("Sleep_Yes", null);
    }

    private void TrustSaveCheck()
    {
        RequireWorld();
        var expected = Helper.Data.ReadJsonFile<OvernightExpectation>("trust-overnight-expected.json")!;
        var memory = LiveMemory();
        var actual = memory.Promises;
        bool treePassed = memory.Tree.IsValid() && JsonSerializer.Serialize(memory.Tree) == JsonSerializer.Serialize(expected.Tree);
        bool experiencesPassed = JsonSerializer.Serialize(memory.Experiences) == JsonSerializer.Serialize(expected.Experiences);
        Helper.Data.WriteJsonFile("trust-overnight-result.json", new {
            Passed = Game1.Date.TotalDays == expected.Day && JsonSerializer.Serialize(actual) == JsonSerializer.Serialize(expected.Promises) && treePassed && experiencesPassed,
            ExpectedDay = expected.Day, ActualDay = Game1.Date.TotalDays, Records = actual.Records.Count,
            Trust = actual.TrustDescription, Valid = actual.IsValid(), TreePassed = treePassed, ExperiencesPassed = experiencesPassed, ExperienceCount = memory.Experiences.Entries.Count,
            TreeMilestones = memory.Tree.Unlocked.Count, TreeApproach = memory.Tree.Approach,
            TreeStudies = memory.Tree.Studies.Count, TreePendingSwitch = memory.Tree.OutstandingSwitch
        });
        RestoreFocusPause();
    }

    public sealed class OvernightExpectation
    {
        public int Day { get; set; }
        public PromiseLedger Promises { get; set; } = new();
        public RelationshipTreeState Tree { get; set; } = new();
        public SharedExperienceStore Experiences { get; set; } = new();
    }
}


