using System.Text.Json;
using SolaceWeather.Core;

namespace SolaceWeather.Core.Tests;

public class PromiseLedgerTests
{
    [Fact]
    public void EmptyOldJsonStartsValidWithNoTrustOrPromise()
    {
        var ledger = JsonSerializer.Deserialize<PromiseLedger>("{}")!;
        Assert.True(ledger.IsValid());
        Assert.Equal(0, ledger.Score);
        Assert.Null(ledger.Outstanding);
        Assert.True(ledger.CanOffer("fish", 0));
        Assert.False(ledger.CanOffer("gold", 0));
        Assert.False(ledger.CanOffer("fish", -1));
    }

    [Fact]
    public void OffersRequireAnExplicitAcceptanceAndOnlyOneCanBeOutstanding()
    {
        var ledger = Ready();
        Assert.True(ledger.Offer("fish", 2));
        Assert.Equal("offered", ledger.Outstanding!.Status);
        Assert.Null(ledger.Outstanding.AcceptedDay);
        Assert.False(ledger.Outstanding.TestItemPending);
        Assert.False(ledger.Complete("fish", "(O)131", "Sardine", 2));
        Assert.False(ledger.Offer("quartz", 2));
        Assert.False(ledger.Offer("fish", 2));
        Assert.True(ledger.Accept("fish", 2, 0, true));
        Assert.False(ledger.Accept("fish", 2, 0, true));
        Assert.Equal(0, ledger.Score);
        Assert.True(ledger.IsValid());
    }

    [Fact]
    public void FishStaysCasualForeverAndCannotBeGivenADeadline()
    {
        var ledger = Ready();
        ledger.Offer("fish", 1);
        Assert.False(ledger.Accept("fish", 1, 1, false));
        Assert.False(ledger.Accept("fish", 1, 3, false));
        Assert.True(ledger.Accept("fish", 1, 0, false));
        ledger.AdvanceDay(999);
        Assert.Equal("active", ledger.Outstanding!.Status);
        Assert.Null(ledger.Outstanding.DueDay);
        Assert.False(ledger.Extend("fish", 999));
        Assert.Equal(0, ledger.Score);
        Assert.True(ledger.Abandon("fish", 999));
        Assert.Equal(0, ledger.Score);
    }

    [Fact]
    public void MaterialAndExperienceGatesPersistAcrossSaveReload()
    {
        var ledger = new PromiseLedger();
        Assert.False(ledger.CanOffer("quartz", 1));
        Assert.False(ledger.CanOffer("iron", 1));
        ledger.ObserveMaterials(new[] { "quartz", "iron", "gold", "fish", "quartz" });
        Assert.Equal(2, ledger.SeenMaterials.Count);
        Assert.True(ledger.CanOffer("quartz", 1));
        Assert.False(ledger.CanOffer("iron", 1));
        Finish(ledger, "fish", 1, 0, 1);
        var loaded = Reload(ledger);
        Assert.True(loaded.CanOffer("iron", 1));
        Assert.False(loaded.CanOffer("fish", 999));
        Assert.False(new PromiseLedger().CanOffer("iron", 1));
        Assert.True(loaded.IsValid());
    }

    [Theory]
    [InlineData(1, 11)]
    [InlineData(3, 13)]
    public void DueDayItselfIsOnTime(int days, int dueDay)
    {
        var ledger = Ready();
        ledger.Offer("quartz", 10);
        Assert.False(ledger.Accept("quartz", 10, 0, false));
        Assert.False(ledger.Accept("quartz", 10, 2, false));
        Assert.True(ledger.Accept("quartz", 10, days, true));
        Assert.False(ledger.Outstanding!.TestItemPending);
        Assert.Equal(dueDay, ledger.Outstanding.DueDay);
        ledger.AdvanceDay(dueDay);
        Assert.Equal("active", ledger.Outstanding.Status);
        Assert.True(ledger.Complete("quartz", "(O)80", "Quartz", dueDay));
        Assert.Equal(2, ledger.Score);
        Assert.False(ledger.Records.Single().WasLate);
        Assert.True(ledger.IsValid());
    }

    [Fact]
    public void NextMorningMarksLateOnceAndLateDeliveryReplacesThePenalty()
    {
        var ledger = Ready();
        ledger.Offer("quartz", 10);
        ledger.Accept("quartz", 10, 1, false);
        ledger.AdvanceDay(12);
        ledger.AdvanceDay(12);
        ledger.AdvanceDay(20);
        Assert.Equal("overdue", ledger.Outstanding!.Status);
        Assert.Equal(-2, ledger.Score);
        Assert.True(ledger.Outstanding.WasLate);
        Assert.Single(ledger.Outstanding.History.Where(h => h.Kind == "late"));
        Assert.False(ledger.Extend("quartz", 20));
        Assert.True(ledger.Complete("quartz", "(O)80", "Quartz", 20));
        Assert.Equal(1, ledger.Score);
        Assert.False(ledger.Complete("quartz", "(O)80", "Quartz", 20));
        Assert.Equal(1, ledger.Score);
        Assert.True(ledger.IsValid());
    }

    [Fact]
    public void CompletionChecksTheDeadlineEvenIfMorningUpdateWasMissed()
    {
        var ledger = Ready();
        Finish(ledger, "fish", 1, 0, 1);
        ledger.Offer("iron", 2);
        ledger.Accept("iron", 2, 3, false);
        Assert.True(ledger.Complete("iron", "(O)335", "Iron Bar", 6));
        Assert.Equal(3, ledger.Score);
        Assert.Equal(2, ledger.Records.Single(r => r.Id == "iron").Contribution);
        Assert.True(ledger.IsValid());
    }

    [Fact]
    public void OneExtensionAddsTwoDaysAndCannotBeUsedAfterTheDeadline()
    {
        var ledger = Ready();
        ledger.Offer("quartz", 1);
        ledger.Accept("quartz", 1, 1, false);
        Assert.True(ledger.Extend("quartz", 2));
        Assert.Equal(4, ledger.Outstanding!.DueDay);
        Assert.False(ledger.Extend("quartz", 3));
        Assert.True(ledger.Complete("quartz", "(O)80", "Quartz", 4));
        Assert.Equal(2, ledger.Score);

        ledger = Ready();
        ledger.Offer("quartz", 1);
        ledger.Accept("quartz", 1, 1, false);
        Assert.False(ledger.Extend("quartz", 3));
        Assert.Equal(2, ledger.Outstanding!.DueDay);
    }

    [Fact]
    public void AbandonedRepairsKeepTheirPenaltyAndOnlyRestoreReducedCredit()
    {
        var ledger = Ready();
        ledger.Offer("quartz", 1);
        ledger.Accept("quartz", 1, 3, false);
        Assert.True(ledger.Abandon("quartz", 2));
        Assert.False(ledger.Abandon("quartz", 2));
        Assert.Equal(-2, ledger.Score);
        Assert.False(ledger.CanOffer("quartz", 2));
        Assert.False(ledger.CanOffer("quartz", 3));
        Assert.True(ledger.Offer("quartz", 4));
        Assert.Equal(-2, ledger.Score);
        Assert.False(ledger.CanOffer("fish", 4));
        Assert.True(ledger.Decline("quartz", 4));
        Assert.Equal(-2, ledger.Score);
        Assert.False(ledger.CanOffer("quartz", 4));
        Assert.True(ledger.Offer("quartz", 5));
        Assert.True(ledger.Accept("quartz", 5, 1, false));
        Assert.True(ledger.Complete("quartz", "(O)80", "Quartz", 6));
        Assert.Equal(1, ledger.Score);
        Assert.Single(ledger.Records);
        Assert.Contains(ledger.Records.Single().History, h => h.Kind == "abandoned");
        Assert.True(ledger.IsValid());
    }

    [Fact]
    public void DecliningAnOfferHasNoTrustCostAndCanBeRetriedTomorrow()
    {
        var ledger = Ready();
        ledger.Offer("quartz", 1);
        Assert.False(ledger.Abandon("quartz", 1));
        Assert.True(ledger.Decline("quartz", 1));
        Assert.False(ledger.Decline("quartz", 1));
        Assert.Equal(0, ledger.Score);
        Assert.Null(ledger.Outstanding);
        Assert.False(ledger.Offer("quartz", 1));
        Assert.True(ledger.Offer("quartz", 2));
        Assert.True(ledger.IsValid());
    }

    [Fact]
    public void TestFishIsGrantedAtMostOnceAcrossAbandonmentAndReload()
    {
        var ledger = Ready();
        ledger.Offer("fish", 1);
        Assert.False(ledger.MarkTestItemGranted("fish"));
        ledger.Accept("fish", 1, 0, true);
        Assert.True(ledger.MarkTestItemGranted("fish"));
        Assert.False(ledger.MarkTestItemGranted("fish"));
        ledger.Abandon("fish", 1);
        ledger = Reload(ledger);
        ledger.Offer("fish", 3);
        ledger.Accept("fish", 3, 0, true);
        Assert.False(ledger.Outstanding!.TestItemPending);
        Assert.True(ledger.Outstanding.TestItemGranted);
        Assert.False(ledger.MarkTestItemGranted("fish"));
        Assert.True(ledger.IsValid());
    }

    [Fact]
    public void ActualDeliveryIsSavedAndCompletionCancelsPendingTestFish()
    {
        var ledger = Ready();
        ledger.Offer("fish", 1);
        ledger.Accept("fish", 1, 0, true);
        Assert.True(ledger.Complete("fish", "(O)145", "Sunfish", 8));
        var loaded = Reload(ledger);
        var fish = loaded.Records.Single();
        Assert.Equal("(O)145", fish.DeliveredItemId);
        Assert.Equal("Sunfish", fish.DeliveredItemName);
        Assert.Equal(8, fish.CompletedDay);
        Assert.False(fish.TestItemPending);
        Assert.False(loaded.MarkTestItemGranted("fish"));
        Assert.True(loaded.IsValid());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LegacyActiveMigrationPreservesTestFishAndNeverDuplicates(bool granted)
    {
        var legacy = new DeliveryRequest();
        legacy.Offer("fish", 3, true);
        if (granted) legacy.MarkTestFishGranted();
        var ledger = new PromiseLedger();
        ledger.MigrateLegacy(legacy);
        ledger.MigrateLegacy(legacy);
        var fish = Assert.Single(ledger.Records);
        Assert.Equal("active", fish.Status);
        Assert.Equal(3, fish.AcceptedDay);
        Assert.Null(fish.DueDay);
        Assert.Equal(!granted, fish.TestItemPending);
        Assert.Equal(granted, fish.TestItemGranted);
        Assert.True(ledger.LegacyMigrated);
        Assert.True(ledger.IsValid());
    }

    [Fact]
    public void LegacyCompletedMigrationAwardsExactlyOneAndKeepsActualDelivery()
    {
        var legacy = new DeliveryRequest();
        legacy.Offer("fish", 3, false);
        legacy.Complete("(O)131", "Sardine", 5);
        var ledger = new PromiseLedger();
        ledger.MigrateLegacy(legacy);
        ledger = Reload(ledger);
        ledger.MigrateLegacy(legacy);
        Assert.Equal(1, ledger.Score);
        var fish = Assert.Single(ledger.Records);
        Assert.Equal("Sardine", fish.DeliveredItemName);
        Assert.Equal(5, fish.CompletedDay);
        Assert.False(ledger.CanOffer("fish", 6));
        Assert.True(ledger.IsValid());
    }

    [Fact]
    public void EmptyMigrationIsIdempotentAndDoesNotReplaceNewPromises()
    {
        var ledger = new PromiseLedger();
        ledger.MigrateLegacy(new DeliveryRequest());
        Assert.True(ledger.LegacyMigrated);
        Assert.Empty(ledger.Records);
        ledger.Offer("fish", 1);
        var legacy = new DeliveryRequest();
        legacy.Offer("fish", 0, true);
        ledger.MigrateLegacy(legacy);
        Assert.Equal("offered", ledger.Outstanding!.Status);
        Assert.False(ledger.Outstanding.TestItemPending);
    }

    [Fact]
    public void TrustIsBoundedAndDescriptionsFollowEarnedContributions()
    {
        var ledger = Ready();
        Assert.Equal("Still learning about you", ledger.TrustDescription);
        Finish(ledger, "fish", 1, 0, 1);
        Assert.Equal("Still learning about you", ledger.TrustDescription);
        Finish(ledger, "quartz", 2, 1, 3);
        Assert.Equal(3, ledger.Score);
        Assert.Equal("Starting to rely on you", ledger.TrustDescription);
        Finish(ledger, "iron", 3, 1, 4);
        Assert.Equal(6, ledger.Score);
        Assert.Equal("Consistently dependable", ledger.TrustDescription);
        foreach (var id in new[] { "fish", "quartz", "iron" }) Assert.False(ledger.Offer(id, 999));

        ledger = Ready();
        ledger.Offer("quartz", 1);
        ledger.Accept("quartz", 1, 1, false);
        ledger.AdvanceDay(3);
        Assert.Equal("Cautious", ledger.TrustDescription);
    }

    [Fact]
    public void HistoryIsBoundedButRepeatedFailuresCannotEraseThePenalty()
    {
        var ledger = Ready();
        for (var day = 1; day < 61; day += 3)
        {
            Assert.True(ledger.Offer("quartz", day));
            Assert.True(ledger.Accept("quartz", day, 1, false));
            Assert.True(ledger.Abandon("quartz", day));
            Assert.Equal(-2, ledger.Score);
            Assert.True(ledger.IsValid());
        }
        Assert.Equal(24, ledger.Records.Single().History.Count);
        ledger.Offer("quartz", 61);
        ledger.Accept("quartz", 61, 1, false);
        ledger.Complete("quartz", "(O)80", "Quartz", 62);
        Assert.Equal(1, ledger.Score);
        Assert.Equal(24, ledger.Records.Single().History.Count);
    }

    [Theory]
    [InlineData("{\"Version\":2}")]
    [InlineData("{\"Records\":null}")]
    [InlineData("{\"SeenMaterials\":null}")]
    [InlineData("{\"SeenMaterials\":[\"gold\"]}")]
    [InlineData("{\"LastReminderDay\":-1}")]
    [InlineData("{\"Records\":[null]}")]
    [InlineData("{\"Records\":[{\"Id\":\"gold\",\"Status\":\"offered\"}]}")]
    [InlineData("{\"Records\":[{\"Id\":\"fish\",\"Status\":\"completed\"}]}")]
    [InlineData("{\"Records\":[{\"Id\":\"fish\",\"Status\":\"offered\",\"History\":null}]}")]
    [InlineData("{\"Records\":[{\"Id\":\"fish\",\"Status\":\"offered\",\"Contribution\":99}]}")]
    [InlineData("{\"Records\":[{\"Id\":\"fish\",\"Status\":\"offered\"},{\"Id\":\"fish\",\"Status\":\"offered\"}]}")]
    [InlineData("{\"Records\":[{\"Id\":\"fish\",\"Status\":\"offered\"},{\"Id\":\"quartz\",\"Status\":\"offered\"}]}")]
    public void MalformedSavesAreRejectedAndCannotBeMutated(string json)
    {
        var ledger = JsonSerializer.Deserialize<PromiseLedger>(json)!;
        Assert.False(ledger.IsValid());
        Assert.False(ledger.Offer("fish", 5));
        Assert.False(ledger.Accept("fish", 5, 0, true));
        Assert.False(ledger.Complete("fish", "(O)131", "Sardine", 5));
        Assert.False(ledger.MarkTestItemGranted("fish"));
        ledger.AdvanceDay(5);
    }

    [Fact]
    public void InvalidInputsCannotRewindOrInventAPromiseOutcome()
    {
        var ledger = Ready();
        ledger.Offer("quartz", 10);
        Assert.False(ledger.Accept("quartz", 9, 1, false));
        Assert.False(ledger.Accept("quartz", int.MaxValue, 3, false));
        Assert.True(ledger.Accept("quartz", 10, 1, false));
        Assert.False(ledger.Complete("quartz", "", "Quartz", 10));
        Assert.False(ledger.Complete("quartz", "(O)80", " ", 10));
        Assert.False(ledger.Complete("quartz", "(O)80", "Quartz", 9));
        Assert.False(ledger.Abandon("quartz", 9));
        Assert.False(ledger.Extend("quartz", 9));
        Assert.Equal(0, ledger.Score);
        Assert.True(ledger.IsValid());
    }

    [Fact]
    public void MalformedLegacyDataIsNotMarkedAsMigrated()
    {
        var ledger = new PromiseLedger();
        ledger.MigrateLegacy(new DeliveryRequest { Status = "active", CreatedDay = -1 });
        Assert.False(ledger.LegacyMigrated);
        Assert.Empty(ledger.Records);
        ledger.MigrateLegacy(null!);
        Assert.False(ledger.LegacyMigrated);
        ledger.MigrateLegacy(new DeliveryRequest {
            Status = "completed", CreatedDay = 1, CompletedDay = 2,
            DeliveredItemId = " ", DeliveredItemName = " "
        });
        Assert.False(ledger.LegacyMigrated);
        Assert.Empty(ledger.Records);
    }

    [Fact]
    public void MutationOfAReloadedSaveDoesNotChangeAnotherFarmerOrTheOriginal()
    {
        var original = Ready();
        original.Offer("quartz", 1);
        original.Accept("quartz", 1, 1, false);
        var loaded = Reload(original);
        var anotherFarmer = new PromiseLedger();
        loaded.AdvanceDay(3);
        Assert.Equal(-2, loaded.Score);
        Assert.Equal(0, original.Score);
        Assert.Equal("active", original.Outstanding!.Status);
        Assert.Empty(anotherFarmer.Records);
        Assert.Empty(anotherFarmer.SeenMaterials);
    }

    [Fact]
    public void ForgedTrustAndImpossibleHistoryAreRejected()
    {
        var ledger = Ready();
        ledger.Offer("quartz", 1);
        ledger.Accept("quartz", 1, 1, false);
        var active = ledger.Records.Single();
        active.Contribution = -2;
        Assert.False(ledger.IsValid());
        active.Contribution = 0;
        active.History.Clear();
        Assert.False(ledger.IsValid());

        ledger = Ready();
        ledger.Offer("quartz", 1);
        ledger.Accept("quartz", 1, 1, false);
        ledger.Complete("quartz", "(O)80", "Quartz", 2);
        ledger.Records.Single().CompletedDay = 99;
        Assert.False(ledger.IsValid());

        ledger = Ready();
        ledger.Offer("quartz", 1);
        ledger.Accept("quartz", 1, 1, false);
        ledger.Abandon("quartz", 1);
        ledger.Offer("quartz", 3);
        ledger.Records.Single().AbandonedDay = 99;
        Assert.False(ledger.IsValid());
    }

    [Fact]
    public void OfferingAgainNeverErasesAnUnrepairedLatePromise()
    {
        var ledger = Ready();
        ledger.Offer("quartz", 1);
        ledger.Accept("quartz", 1, 1, false);
        ledger.AdvanceDay(3);
        ledger.Abandon("quartz", 3);
        ledger.Offer("quartz", 5);
        ledger.Accept("quartz", 5, 3, false);
        Assert.True(ledger.Extend("quartz", 6));
        Assert.True(ledger.Complete("quartz", "(O)80", "Quartz", 7));
        Assert.Equal(1, ledger.Score);
        Assert.True(ledger.Records.Single().WasLate);
        Assert.True(ledger.IsValid());
    }

    [Theory]
    [InlineData("quartz", "(O)80", "Quartz")]
    [InlineData("iron", "(O)335", "Iron Bar")]
    public void FixedMaterialRequestsRejectWrongItemsWithoutChangingThePromise(string id, string itemId, string itemName)
    {
        var ledger = Ready();
        Finish(ledger, "fish", 1, 0, 1);
        Assert.True(ledger.Offer(id, 2));
        Assert.True(ledger.Accept(id, 2, 1, false));
        var before = JsonSerializer.Serialize(ledger);
        Assert.False(ledger.Complete(id, "(O)390", "Stone", 2));
        Assert.Equal(before, JsonSerializer.Serialize(ledger));
        Assert.True(ledger.Complete(id, itemId, itemName, 2));
        Assert.True(ledger.IsValid());
    }

    [Theory]
    [InlineData("quartz")]
    [InlineData("iron")]
    public void CompletedMaterialSavesRejectARecordedWrongItem(string id)
    {
        var ledger = Ready();
        Finish(ledger, "fish", 1, 0, 1);
        Finish(ledger, id, 2, 1, 2);
        var completed = ledger.Records.Single(r => r.Id == id);
        completed.DeliveredItemId = "(O)390";
        completed.DeliveredItemName = "Stone";
        Assert.False(ledger.IsValid());
        Assert.False(Reload(ledger).IsValid());
    }

    private static PromiseLedger Ready()
    {
        var ledger = new PromiseLedger();
        ledger.ObserveMaterials(new[] { "quartz", "iron" });
        return ledger;
    }

    private static void Finish(PromiseLedger ledger, string id, int accepted, int days, int completed)
    {
        Assert.True(ledger.Offer(id, accepted));
        Assert.True(ledger.Accept(id, accepted, days, false));
        var definition = PromiseLedger.Definition(id)!;
        Assert.True(ledger.Complete(id, id == "fish" ? "(O)131" : definition.ItemId,
            id == "fish" ? "Sardine" : definition.Name, completed));
    }

    private static PromiseLedger Reload(PromiseLedger ledger) =>
        JsonSerializer.Deserialize<PromiseLedger>(JsonSerializer.Serialize(ledger))!;
}
