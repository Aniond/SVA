using System.Text.Json;
using SolaceWeather.Core;

namespace SolaceWeather.Core.Tests;

public class CemeteryOutingTests
{
    [Fact]
    public void RequiresAcceptedInvitationAndNightWindow()
    {
        var state = new CemeteryOutingState { FarmerId = 7 };
        Assert.False(state.Begin(3, 1200));
        Assert.True(state.Offer(1, 3));
        Assert.False(state.Begin(3, 1200));
        Assert.False(state.Answer(2, true));
        Assert.True(state.Answer(1, true));
        Assert.False(state.Begin(2, 1200)); Assert.False(state.Begin(3, 1199));
        Assert.True(state.Begin(3, 1230)); Assert.False(state.Begin(3, 1230));
    }
    [Fact]
    public void LatestArrivalCanCompleteAtNine()
    {
        var state = Accepted(); Assert.True(state.Begin(3, 1230));
        Assert.True(state.Advance(3, 1240, 0, false));
        Assert.True(state.Advance(3, 1250, 1, false));
        Assert.True(state.Advance(3, 1260, 2, true));
        Assert.Equal("completed", state.Status);
    }
    [Fact]
    public void OrderedObservedStepsCompleteOnlyOnce()
    {
        var state = Accepted(); Assert.True(state.Begin(3, 1200));
        Assert.False(state.Advance(3, 1210, 1, true));
        Assert.True(state.Advance(3, 1210, 0, false));
        Assert.True(state.Advance(3, 1220, 1, false));
        Assert.False(state.Advance(3, 1230, 2, false));
        Assert.True(state.Advance(3, 1230, 2, true));
        Assert.Equal("completed", state.Status);
        Assert.False(state.Advance(3, 1240, 2, true)); Assert.False(state.Offer(4, 5));
    }
    [Fact]
    public void DeclineMissAndReloadPreserveConsentWithoutFalseCompletion()
    {
        var state = new CemeteryOutingState { FarmerId = 7 };
        Assert.True(state.Offer(1, 3)); Assert.True(state.Answer(1, false));
        Assert.False(state.Offer(2, 3)); Assert.True(state.Offer(4, 6)); Assert.True(state.Answer(4, true));
        state.CheckTime(6, 1231); Assert.Equal("missed", state.Status); Assert.Equal(0, state.Step);
        Assert.True(state.Offer(7, 8)); Assert.True(state.Answer(7, true)); Assert.True(state.Begin(8, 1200));
        Assert.True(state.Advance(8, 1210, 0, false));
        var loaded = JsonSerializer.Deserialize<CemeteryOutingState>(JsonSerializer.Serialize(state))!;
        Assert.True(loaded.IsValid(7)); Assert.False(loaded.IsValid(8));
        loaded.Interrupt(); Assert.Equal("missed", loaded.Status); Assert.Equal(0, loaded.Step);
        Assert.True(loaded.Offer(9, 10));
    }
    private static CemeteryOutingState Accepted()
    {
        var state = new CemeteryOutingState { FarmerId = 7 };
        state.Offer(1, 3); state.Answer(1, true); return state;
    }
}
