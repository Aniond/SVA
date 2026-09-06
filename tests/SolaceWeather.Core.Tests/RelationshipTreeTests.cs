using System.Text.Json;
using SolaceWeather.Core;

namespace SolaceWeather.Core.Tests;

public class RelationshipTreeTests
{
    [Fact]
    public void MilestonesUseVerifiedHistoryAndRemainUnlocked()
    {
        var tree = new RelationshipTreeState();
        var ledger = Completed();
        tree.Observe(ledger, 10, new[] { 1, 1, 2, -1, 99 });
        Assert.Contains("planning", tree.Unlocked);
        Assert.Contains("flute", tree.Unlocked);
        Assert.DoesNotContain("fork", tree.Unlocked);
        tree.Observe(ledger, 10, new[] { 3, 4, 5 });
        Assert.Contains("fork", tree.Unlocked);
        Assert.Equal("none", tree.Approach);
        tree.Observe(new PromiseLedger(), 11, Array.Empty<int>());
        Assert.Contains("fork", tree.Unlocked);
        Assert.True(Reload(tree).IsValid());
    }

    [Fact]
    public void StudyNeedsUniqueMineralsAndDistinctDays()
    {
        var tree = Ready();
        Assert.False(tree.Study("(O)335", 10));
        Assert.True(tree.Study("(O)80", 10));
        Assert.False(tree.Study("(O)66", 10));
        Assert.False(tree.Study("(O)86", 10));
        Assert.False(tree.Study("(O)80", 11));
        Assert.DoesNotContain("exchange", tree.Unlocked);
        Assert.True(tree.Study("(O)84", 11));
        Assert.DoesNotContain("exchange", tree.Unlocked);
        Assert.True(tree.Study("(O)82", 12));
        Assert.Contains("exchange", tree.Unlocked);
        Assert.True(Reload(tree).IsValid());
    }

    [Fact]
    public void RememberedFailureSurvivesTruncatedHistoryWithoutRestartingCooling()
    {
        var ledger = new PromiseLedger();
        ledger.ObserveMaterials(new[] { "quartz" });
        ledger.Offer("quartz", 1); ledger.Accept("quartz", 1, 1, false); ledger.AdvanceDay(3);
        var tree = new RelationshipTreeState();
        tree.Observe(ledger, 3, Array.Empty<int>());
        ledger.Abandon("quartz", 7);
        ledger.Records[0].History = ledger.Records[0].History.TakeLast(1).ToList();
        Assert.True(ledger.IsValid());
        tree = Reload(tree);
        tree.Observe(ledger, 7, Array.Empty<int>());
        Assert.Equal(4, tree.CoolingUntilDay);
        Assert.True(tree.HelpStatus(ledger, 100).Paused);
    }

    [Fact]
    public void MigrationNeverInventsStudiesAndFishAbandonDoesNotPauseHelp()
    {
        var ledger = new PromiseLedger();
        ledger.Offer("fish", 1); ledger.Accept("fish", 1, 0, false); ledger.Abandon("fish", 2);
        var tree = new RelationshipTreeState();
        tree.Observe(ledger, 10, Enumerable.Range(0, 11));
        Assert.Empty(tree.Studies);
        Assert.Empty(tree.Unlocked);
        Assert.Equal(5, tree.MineDays.Count);
        Assert.Null(tree.CoolingUntilDay);
        Assert.False(tree.HelpStatus(ledger, 10).Paused);
    }

    [Fact]
    public void CanceledSwitchProofCannotCarryIntoANewAttempt()
    {
        var tree = Ready();
        tree.ChooseApproach("safe", 10);
        tree.BeginSwitch("bold", 10, false);
        tree.RecordPreparation(10, 900, true); tree.RecordMineVisit(10, 1000);
        Assert.False(tree.BeginSwitch("bold", 11, false));
        Assert.True(tree.CancelSwitch());
        Assert.True(tree.BeginSwitch("bold", 11, false));
        Assert.Empty(tree.PendingSwitch!.VerifiedDays);
        Assert.Null(tree.PendingSwitch.PreparationDay);
        tree.RecordPreparation(11, 900, true); tree.RecordMineVisit(11, 1000);
        Assert.False(tree.FinishSwitch(11));
        Assert.Equal("safe", tree.Approach);
        Assert.True(Reload(tree).IsValid());
    }

    [Fact]
    public void CooldownsCannotOverflowNearTheEndOfTheDayRange()
    {
        var tree = Ready();
        Assert.True(tree.Claim("flute", int.MaxValue - 6));
        Assert.False(tree.CanClaim("flute", int.MaxValue));
        Assert.False(tree.CanClaim("flute", 0));
        Assert.False(tree.CanClaim("flute", -1));
        Assert.True(Reload(tree).IsValid());
    }

    [Fact]
    public void ServicesHaveSeparateSevenDayCooldownsAndKitIsShared()
    {
        var tree = Ready();
        tree.Study("(O)80", 10); tree.Study("(O)66", 11); tree.Study("(O)86", 12);
        Assert.False(tree.Claim("kit", 12));
        Assert.True(tree.ChooseApproach("safe", 12));
        Assert.True(tree.Claim("kit", 12));
        Assert.True(tree.Claim("flute", 12));
        Assert.True(tree.Claim("exchange", 12));
        Assert.False(tree.CanClaim("kit", 18));
        Assert.True(tree.CanClaim("kit", 19));
        Assert.False(tree.CanClaim("unknown", 19));
        Assert.False(tree.CanClaim("flute", 11));
    }

    [Fact]
    public void SwitchRequiresExplicitPreparationBeforeTwoSeparateMineDaysAndFinalReturn()
    {
        var tree = Ready();
        tree.ChooseApproach("safe", 10);
        tree.Claim("kit", 10);
        Assert.False(tree.BeginSwitch("bold", 10, true));
        Assert.False(tree.BeginSwitch("safe", 10, false));
        Assert.True(tree.BeginSwitch("bold", 10, false));
        Assert.True(tree.OutstandingSwitch);
        tree.RecordMineVisit(10, 900);
        Assert.False(tree.RecordPreparation(10, 1000, false));
        Assert.True(tree.RecordPreparation(10, 1000, true));
        tree.RecordMineVisit(10, 1000);
        Assert.Empty(tree.PendingSwitch!.VerifiedDays);
        tree.RecordMineVisit(10, 1100);
        tree.RecordMineVisit(10, 1200);
        Assert.False(tree.FinishSwitch(10));
        tree.RecordMineVisit(11, 1100);
        Assert.Single(tree.PendingSwitch.VerifiedDays);
        tree.RecordPreparation(11, 1200, true);
        tree.RecordMineVisit(11, 1300);
        Assert.Equal("safe", tree.Approach);
        tree = Reload(tree);
        Assert.True(tree.FinishSwitch(11));
        Assert.Equal("bold", tree.Approach);
        Assert.False(tree.OutstandingSwitch);
        Assert.False(tree.CanClaim("kit", 16));
        Assert.True(tree.CanClaim("kit", 17));
        Assert.True(tree.BeginSwitch("safe", 20, false));
        Assert.True(tree.CancelSwitch());
        Assert.Equal("bold", tree.Approach);
    }

    [Theory]
    [InlineData("quartz", 1)]
    [InlineData("iron", 3)]
    public void CoolingStartsAtFirstFailureAndRepairOrAbandonDoesNotResetIt(string id, int mornings)
    {
        var ledger = new PromiseLedger();
        ledger.ObserveMaterials(new[] { "quartz", "iron" });
        Finish(ledger, "fish", 1);
        ledger.Offer(id, 2); ledger.Accept(id, 2, 1, false);
        ledger.AdvanceDay(4);
        var tree = new RelationshipTreeState();
        tree.Observe(ledger, 4, Array.Empty<int>());
        Assert.Equal(4 + mornings, tree.CoolingUntilDay);
        ledger.Abandon(id, 5);
        tree.Observe(ledger, 5, Array.Empty<int>());
        Assert.Equal(4 + mornings, tree.CoolingUntilDay);
        ledger.Offer(id, 7); ledger.Accept(id, 7, 1, false);
        ledger.AdvanceDay(9);
        tree.Observe(ledger, 9, Array.Empty<int>());
        Assert.Equal(9 + mornings, tree.CoolingUntilDay);
        ledger.Complete(id, PromiseLedger.Definition(id)!.ItemId, id, 9);
        tree.Observe(ledger, 9, Array.Empty<int>());
        Assert.True(tree.HelpStatus(ledger, 9).Paused);
        Assert.False(tree.HelpStatus(ledger, 9 + mornings).Paused);
    }

    [Fact]
    public void UnresolvedIronPausesHelpEvenWhenOtherPromisesKeepScoreNonnegative()
    {
        var ledger = new PromiseLedger();
        ledger.ObserveMaterials(new[] { "quartz", "iron" });
        Finish(ledger, "fish", 1); Finish(ledger, "quartz", 2);
        ledger.Offer("iron", 3); ledger.Accept("iron", 3, 1, false); ledger.AdvanceDay(5);
        var tree = new RelationshipTreeState();
        tree.Observe(ledger, 20, Array.Empty<int>());
        Assert.Equal(0, ledger.Score);
        Assert.True(tree.HelpStatus(ledger, 20).Paused);
    }

    [Theory]
    [InlineData("{\"Version\":2}")]
    [InlineData("{\"Unlocked\":[\"bogus\"]}")]
    [InlineData("{\"Unlocked\":[\"fork\"]}")]
    [InlineData("{\"Studies\":null}")]
    [InlineData("{\"Approach\":\"bold\"}")]
    [InlineData("{\"LastKitDay\":-1}")]
    public void CorruptionFailsClosed(string json)
    {
        var tree = JsonSerializer.Deserialize<RelationshipTreeState>(json)!;
        Assert.False(tree.IsValid());
        Assert.False(tree.Study("(O)80", 1));
        Assert.False(tree.Claim("kit", 1));
        Assert.True(tree.HelpStatus(new PromiseLedger(), 1).Paused);
    }

    [Fact]
    public void AbandoningOpenEndedFishDoesNotStartCoolingOrReduceTrust()
    {
        var ledger = new PromiseLedger();
        ledger.Offer("fish", 1); ledger.Accept("fish", 1, 0, false);
        ledger.AdvanceDay(30); ledger.Abandon("fish", 30);
        var tree = new RelationshipTreeState(); tree.Observe(ledger, 30, Array.Empty<int>());
        Assert.Null(tree.CoolingUntilDay);
        Assert.False(tree.HelpStatus(ledger, 30).Paused);
        Assert.Equal(0, ledger.Score);
    }

    private static RelationshipTreeState Reload(RelationshipTreeState state) => JsonSerializer.Deserialize<RelationshipTreeState>(JsonSerializer.Serialize(state))!;
    private static RelationshipTreeState Ready()
    {
        var tree = new RelationshipTreeState();
        tree.Observe(Completed(), 10, new[] { 1, 2, 3, 4, 5 });
        return tree;
    }
    private static PromiseLedger Completed()
    {
        var ledger = new PromiseLedger();
        ledger.ObserveMaterials(new[] { "quartz", "iron" });
        Finish(ledger, "fish", 1); Finish(ledger, "quartz", 2); Finish(ledger, "iron", 3);
        return ledger;
    }
    private static void Finish(PromiseLedger ledger, string id, int day)
    {
        Assert.True(ledger.Offer(id, day));
        Assert.True(ledger.Accept(id, day, id == "fish" ? 0 : 1, false));
        Assert.True(ledger.Complete(id, id == "fish" ? "(O)131" : PromiseLedger.Definition(id)!.ItemId, id, day));
    }
}
