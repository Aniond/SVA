using SolaceWeather.Core;

namespace SolaceWeather.Core.Tests;

public class HaleyTreeTests
{
    [Fact]
    public void SavedUnlocksWithoutSupportingEvidenceAreRejected()
    {
        var tree = new HaleyTreeState { Unlocked = new() { "root", "photo", "personal", "rest" } };
        Assert.False(tree.IsValid());
        Assert.False(tree.ClaimRest(10));
        tree = new HaleyTreeState();
        tree.RecordPhoto(1, "wide", "candid"); tree.Refresh(true, 3);
        tree.Unlocked.Add("rest");
        Assert.False(tree.IsValid());
        Assert.False(tree.ClaimRest(10));
    }

    [Fact]
    public void OnlyVerifiedMilestonesUnlockBranchesAndRestIsBounded()
    {
        var tree = new HaleyTreeState(); tree.Refresh(false, 10);
        Assert.Empty(tree.Unlocked); Assert.False(tree.ClaimRest(1));
        tree.RecordPhoto(1, "wide", "candid"); tree.Refresh(false, 3);
        Assert.Contains("root", tree.Unlocked); Assert.Contains("photo", tree.Unlocked); Assert.DoesNotContain("personal", tree.Unlocked);
        tree.ObserveOutfit(1, "a"); tree.ObserveOutfit(1, "b"); tree.Refresh(true, 3);
        Assert.DoesNotContain("style", tree.Unlocked);
        tree.ObserveOutfit(2, "b"); tree.Refresh(true, 3); Assert.Contains("style", tree.Unlocked);
        tree.RecordPhoto(1, "wide", "candid"); Assert.Single(tree.Photos);
        tree.RecordPhoto(4, "detail", "posed"); tree.RecordPhoto(7, "wide", "posed"); tree.Refresh(true, 3);
        Assert.Equal(6, tree.Unlocked.Count); Assert.True(tree.ChooseApproach("relaxed")); Assert.False(tree.ChooseApproach("polished"));
        Assert.True(tree.ClaimRest(7)); Assert.False(tree.ClaimRest(7)); Assert.False(tree.ClaimRest(13)); Assert.True(tree.ClaimRest(14));
        Assert.True(tree.IsValid());
    }
}
