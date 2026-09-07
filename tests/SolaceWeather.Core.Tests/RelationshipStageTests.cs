using SolaceWeather.Core;

namespace SolaceWeather.Core.Tests;

public class RelationshipStageTests
{
    [Fact]
    public void TreeProgressNeverStartsRomance()
    {
        var nodes = new[] { "root", "photo", "style", "personal", "angle", "rest" };
        Assert.Equal("Best friend", RelationshipStageLabels.For(new(), true, nodes));
        Assert.Equal("Girlfriend", RelationshipStageLabels.For(new() { IsDating = true }, true, nodes));
        Assert.Equal("Boyfriend", RelationshipStageLabels.For(new() { IsDating = true }, false, nodes));
        Assert.Equal("Spouse", RelationshipStageLabels.For(new() { IsMarried = true }, true, nodes));
        Assert.Equal("Taking space", RelationshipStageLabels.For(new() { SeparationUntilDay = 4 }, true, nodes));
    }
    [Fact]
    public void NamesReflectMilestonesOrHonestFallbacks()
    {
        Assert.Equal("Acquaintance", RelationshipStageLabels.For(new(), true, Array.Empty<string>()));
        Assert.Equal("Friend", RelationshipStageLabels.For(new(), true, new[] { "root" }));
        Assert.Equal("Close friend", RelationshipStageLabels.For(new(), true, new[] { "root", "photo", "style" }));
        Assert.Equal("Best friend", RelationshipStageLabels.For(new() { TalkDays = 12, CompletedActivities = 4 }, false, null));
        Assert.Equal("Acquaintance", RelationshipStageLabels.NativeFriendship(0));
        Assert.Equal("Friend", RelationshipStageLabels.NativeFriendship(500));
        Assert.Equal("Close friend", RelationshipStageLabels.NativeFriendship(1250));
        Assert.Equal("Best friend", RelationshipStageLabels.NativeFriendship(2000));
    }
}
