using System.Text.Json;
using SolaceWeather.Core;

namespace SolaceWeather.Core.Tests;

public class PhoneContactsTests
{
    [Theory]
    [InlineData("breakup")]
    [InlineData("divorce")]
    public void CompletedTransitionStaysBlockedAcrossReloadUntilValidatedReconciliation(string transition)
    {
        var romance = new RomanceSaveState();
        romance.Characters["Abigail"] = new() { IsDating = true, InConflict = true, PendingTransition = transition };
        Assert.True(RomanceRules.CompleteTransition(romance, "Abigail", 10));
        romance = JsonSerializer.Deserialize<RomanceSaveState>(JsonSerializer.Serialize(romance))!;
        Assert.True(romance.IsValid());
        var c = romance.Characters["Abigail"];
        Assert.NotEqual("", PhoneState.RelationshipBlock(c, false));
        var phone = new PhoneState { FarmerId = 7 };
        phone.TryOfferNumber("Abigail", 1); phone.AnswerNumberOffer("Abigail", true, 1);
        var pending = phone.Send("Abigail", "Hello", 1, 900)!;
        phone.SetBlock("Abigail", PhoneState.RelationshipBlock(c, false));
        phone = JsonSerializer.Deserialize<PhoneState>(JsonSerializer.Serialize(phone))!;
        Assert.True(phone.IsValid(7));
        Assert.False(phone.CanText("Abigail"));
        Assert.Null(phone.Send("Abigail", "Hello again", 11, 900));
        Assert.Null(phone.Retry("Abigail"));
        Assert.False(phone.TryInitiative("Abigail", 11));
        Assert.False(phone.Complete(pending.Id, "Late response"));
        Assert.False(phone.Incoming("Abigail", "Hello", 11, 900));
        Assert.Single(phone.Thread("Abigail").Messages);
        Assert.False(RomanceRules.StartDating(romance, "Abigail", 11));
        Assert.True(RomanceRules.ExpressInterest(romance, "Abigail", 11));
        Assert.NotEqual("", PhoneState.RelationshipBlock(c, false));
        c.TalkDays = 12; c.CompletedActivities = 4; c.ActivityTypes = new() { "walk", "coffee" };
        Assert.True(RomanceRules.StartDating(romance, "Abigail", 38));
        Assert.Equal("", PhoneState.RelationshipBlock(c, false));
        phone.SetBlock("Abigail", PhoneState.RelationshipBlock(c, false));
        Assert.True(phone.CanText("Abigail"));
        Assert.False(phone.Complete(pending.Id, "Still stale after reconciliation"));
        Assert.Equal(pending.Id, phone.Retry("Abigail")!.Id);
        Assert.Equal("divorced", PhoneState.RelationshipBlock(c, true));
    }

    [Fact]
    public void SeparationRequiresCompletedRepairBeforeBlockClears()
    {
        var romance = new RomanceSaveState();
        var c = romance.Characters["Abigail"] = new() { IsDating = true, InConflict = true,
            SeparationUntilDay = 14, LastViolationDay = 0, PhoneBlockReason = "separated" };
        Assert.True(RomanceRules.AcknowledgeConflict(romance, "Abigail", 1));
        Assert.True(RomanceRules.RecordCompletedActivity(romance, "Abigail", 2, "walk", repair: true));
        Assert.True(RomanceRules.RecordCompletedActivity(romance, "Abigail", 3, "coffee", repair: true));
        Assert.True(RomanceRules.AdvanceDay(romance, 7));
        Assert.Equal("separated", PhoneState.RelationshipBlock(c, false));
        Assert.True(RomanceRules.AdvanceDay(romance, 14));
        Assert.Equal("", PhoneState.RelationshipBlock(c, false));
        Assert.Equal("", c.PhoneBlockReason);
        Assert.True(romance.IsValid());
    }

    [Fact]
    public void OlderRelationshipDataLoadsWithoutInventingBlockAndInvalidMarkerIsRejected()
    {
        var old = JsonSerializer.Deserialize<RomanceCharacterState>("{\"InConflict\":true}")!;
        Assert.True(old.IsValid());
        Assert.Equal("", PhoneState.RelationshipBlock(old, false));
        old.PhoneBlockReason = "low-hearts";
        Assert.False(old.IsValid());
        old.PhoneBlockReason = null!;
        Assert.False(old.IsValid());
    }

    [Fact]
    public void MeetingRequiresAcceptedExchangeAndDeclineCanBeReofferedLater()
    {
        var state = new PhoneState { FarmerId = 7 };
        Assert.False(state.HasNumber("Abigail"));
        Assert.Null(state.Send("Abigail", "Hello", 1, 900));
        Assert.False(state.TryInitiative("Abigail", 1));
        Assert.True(state.TryOfferNumber("Abigail", 100)); // Existing saves are not tied to first-meeting day.
        Assert.True(state.AnswerNumberOffer("Abigail", false, 100));
        Assert.False(state.HasNumber("Abigail"));
        Assert.False(state.TryOfferNumber("Abigail", 100));
        Assert.False(state.TryOfferNumber("Abigail", 102));
        Assert.True(state.TryOfferNumber("Abigail", 103));
        Assert.True(state.AnswerNumberOffer("Abigail", true, 103));
        Assert.True(state.CanText("Abigail"));
        Assert.False(state.TryOfferNumber("Abigail", 200));
        Assert.NotNull(state.Send("Abigail", "Hello", 103, 900));
    }

    [Fact]
    public void AcceptanceNeedsCurrentOfferAndPersistsOnlyForOwningFarm()
    {
        var state = new PhoneState { FarmerId = 7 };
        Assert.False(state.AnswerNumberOffer("Sam", true, 1));
        state.TryOfferNumber("Sam", 1);
        Assert.False(state.AnswerNumberOffer("Sam", true, 2));
        Assert.True(state.AnswerNumberOffer("Sam", true, 1));
        var loaded = JsonSerializer.Deserialize<PhoneState>(JsonSerializer.Serialize(state))!;
        Assert.True(loaded.HasNumber("Sam"));
        Assert.True(loaded.IsValid(7));
        Assert.False(loaded.IsValid(8));
        Assert.False(new PhoneState { FarmerId = 8 }.HasNumber("Sam"));
    }

    [Fact]
    public void BlockStopsSendRetryInitiativeAndLateRepliesWhileKeepingHistory()
    {
        var state = new PhoneState { FarmerId = 7 };
        state.TryOfferNumber("Abigail", 1); state.AnswerNumberOffer("Abigail", true, 1);
        var message = state.Send("Abigail", "Hello", 1, 900)!;
        state.SetBlock("Abigail", "separated");
        Assert.True(state.HasNumber("Abigail"));
        Assert.False(state.CanText("Abigail"));
        Assert.Null(state.Send("Abigail", "Hello", 1, 900));
        Assert.Null(state.Retry("Abigail"));
        Assert.False(state.Complete(message.Id, "Too late"));
        Assert.False(state.Incoming("Abigail", "Hello", 1, 900));
        Assert.False(state.TryInitiative("Abigail", 1));
        Assert.Single(state.Thread("Abigail").Messages);
        Assert.Single(state.Thread("Abigail").Shortcuts);
        var loaded = JsonSerializer.Deserialize<PhoneState>(JsonSerializer.Serialize(state))!;
        Assert.False(loaded.CanText("Abigail"));
        Assert.True(loaded.IsValid(7));
        loaded.SetBlock("Abigail", ""); // Only called from the authoritative relationship observer.
        Assert.True(loaded.CanText("Abigail"));
        Assert.NotNull(loaded.Retry("Abigail"));
    }

    [Fact]
    public void RelationshipBlockUsesEstablishedFallingOutNotHeartsOrOrdinaryConflict()
    {
        Assert.Equal("", PhoneState.RelationshipBlock(new() { InConflict = true }, false));
        Assert.Equal("", PhoneState.RelationshipBlock(new() { FriendshipOnly = true }, false));
        Assert.Equal("separated", PhoneState.RelationshipBlock(new() { InConflict = true, SeparationUntilDay = 20 }, false));
        Assert.Equal("ending-relationship", PhoneState.RelationshipBlock(new() { PendingTransition = "breakup" }, false));
        Assert.Equal("divorced", PhoneState.RelationshipBlock(null, true));
        Assert.Equal("", PhoneState.RelationshipBlock(new(), false));
    }
}
