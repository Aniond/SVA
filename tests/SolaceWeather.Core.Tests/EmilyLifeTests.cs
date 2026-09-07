using SolaceWeather.Core;
using System.Text.Json;

namespace SolaceWeather.Core.Tests;

public class EmilyLifeTests
{
    [Theory]
    [InlineData("delighted", 1)]
    [InlineData("concerned", 2)]
    [InlineData("thoughtful", 0)]
    [InlineData("stern", 0)]
    [InlineData("invented-healing-face", 0)]
    public void ReactionsUseOnlyExistingPortraitCells(string reaction, int cell)
    {
        Assert.Equal(cell, RomanceProfiles.Get("Emily")!.PortraitIndex(reaction));
        Assert.Contains(EmilyExpression.Normalize(reaction), EmilyExpression.Names);
    }
    [Fact]
    public void MilestonesRequireDistinctObservedDaysAndFirstMovementIsNotARecoveryReward()
    {
        var tree = new EmilyTreeState(); tree.Refresh(false, 20); Assert.Empty(tree.Unlocked);
        tree.RecordDesign(1, "quiet", "natural"); tree.RecordDesign(1, "playful", "geometric"); tree.Refresh(true, 3);
        Assert.Single(tree.Designs); Assert.Contains("personal", tree.Unlocked); Assert.DoesNotContain("rhythm", tree.Unlocked);
        tree.ObserveOutfit(1, "a"); tree.ObserveOutfit(1, "b"); tree.Refresh(true, 3); Assert.DoesNotContain("style", tree.Unlocked);
        tree.ObserveOutfit(2, "b"); tree.RecordDesign(4, "playful", "geometric"); tree.Refresh(true, 3);
        Assert.Contains("rhythm", tree.Unlocked); Assert.Contains("style", tree.Unlocked);
        Assert.True(tree.RecordMovement(4)); tree.Refresh(true, 3); Assert.Contains("light", tree.Unlocked);
        Assert.False(tree.ClaimRecovery(4)); Assert.False(tree.RecordMovement(4));
        Assert.True(tree.RecordMovement(5)); Assert.True(tree.ClaimRecovery(5)); Assert.False(tree.ClaimRecovery(5));
        Assert.True(tree.RecordMovement(11)); Assert.False(tree.ClaimRecovery(11));
        Assert.True(tree.RecordMovement(12)); Assert.True(tree.ClaimRecovery(12)); Assert.True(tree.IsValid());
        var invalid = new EmilyTreeState { Unlocked = new() { "root", "personal", "rhythm", "light" } };
        Assert.False(invalid.IsValid()); Assert.False(invalid.RecordMovement(1));
    }

    [Fact]
    public void ClothRequiresObservedMaterialAndExplicitAcceptedDelivery()
    {
        var promise = new EmilyClothPromise();
        Assert.False(promise.Offer(1)); promise.Observe("(O)421"); Assert.False(promise.Offer(1));
        promise.Observe("(O)428"); Assert.True(promise.Offer(1));
        Assert.False(promise.Complete("(O)428", "Cloth", 1));
        Assert.True(promise.Accept(1, 3)); Assert.False(promise.Complete("(O)421", "Sunflower", 2));
        Assert.True(promise.Extend(2)); Assert.False(promise.Extend(2));
        Assert.True(promise.Complete("(O)428", "Cloth", 7)); Assert.True(promise.WasLate);
        Assert.False(promise.Complete("(O)428", "Cloth", 7));
    }

    [Fact]
    public void SessionNeedsConsentOrderedChoicesAndObservedCompletion()
    {
        var session = new EmilyDesignSession();
        Assert.False(session.Begin(2, 660)); Assert.True(session.Offer(1, 2));
        Assert.False(session.Answer(2, true)); Assert.True(session.Answer(1, true));
        Assert.False(session.Begin(2, 659)); Assert.True(session.Begin(2, 690));
        Assert.False(session.Advance(2, 700, "geometric", false));
        Assert.True(session.Advance(2, 700, "playful", false));
        Assert.True(session.Advance(2, 710, "geometric", false));
        Assert.False(session.Advance(2, 720, "finish", false));
        Assert.True(session.Advance(2, 720, "finish", true));
        Assert.Equal("completed", session.Status); Assert.Equal("playful", session.Mood);
        Assert.Equal("geometric", session.Pattern); Assert.False(session.Advance(2, 720, "finish", true));
    }

    [Fact]
    public void MissingOrInterruptedSessionCannotCreateDesignHistory()
    {
        var state = new EmilyLifeState { FarmerId = 7 };
        Assert.True(state.IsValid(7)); Assert.False(state.IsValid(8));
        state.Session.Offer(1, 2); state.Session.Answer(1, true); state.Session.CheckTime(2, 691);
        Assert.Equal("missed", state.Session.Status);
        state.Session.Offer(3, 3); state.Session.Answer(3, true); state.Session.Begin(3, 660);
        state.Session.Advance(3, 670, "quiet", false);
        var restored = JsonSerializer.Deserialize<EmilyLifeState>(JsonSerializer.Serialize(state))!;
        restored.Session.Interrupt(); Assert.True(restored.IsValid(7));
        Assert.Equal("missed", restored.Session.Status); Assert.Empty(restored.Session.Mood);
    }
}
