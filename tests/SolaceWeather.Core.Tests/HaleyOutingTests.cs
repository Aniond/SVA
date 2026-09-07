using System.Text.Json;
using SolaceWeather.Core;

namespace SolaceWeather.Core.Tests;

public class HaleyOutingTests
{
    [Fact]
    public void PhotoWalkRequiresConsentAndKeepsActualChoices()
    {
        var outing = new HaleyPhotoOuting();
        Assert.False(outing.Begin(3, 1020)); Assert.True(outing.Offer(1, 3));
        Assert.False(outing.Answer(2, true)); Assert.True(outing.Answer(1, true));
        Assert.False(outing.Begin(3, 1019)); Assert.True(outing.Begin(3, 1050));
        Assert.False(outing.Advance(3, 1060, "posed", false));
        Assert.True(outing.Advance(3, 1060, "wide", false));
        Assert.True(outing.Advance(3, 1070, "candid", false));
        Assert.False(outing.Advance(3, 1080, "finish", false));
        Assert.True(outing.Advance(3, 1080, "finish", true));
        Assert.Equal("completed", outing.Status); Assert.Equal("wide", outing.Framing); Assert.Equal("candid", outing.Style);
        Assert.False(outing.Advance(3, 1080, "finish", true));
    }
    [Fact]
    public void DeclineMissAndInterruptedReloadCannotBecomeCompleted()
    {
        var life = new HaleyLifeState { FarmerId = 7 };
        Assert.True(life.IsValid(7)); Assert.False(life.IsValid(8));
        var outing = life.Outing;
        outing.Offer(1, 3); outing.Answer(1, false); Assert.False(outing.Offer(2, 3));
        Assert.True(outing.Offer(4, 5)); outing.Answer(4, true); outing.CheckTime(5, 1051);
        Assert.Equal("missed", outing.Status); Assert.Equal(0, outing.Step);
        outing.Offer(6, 6); outing.Answer(6, true); outing.Begin(6, 1020); outing.Advance(6, 1030, "detail", false);
        var loaded = JsonSerializer.Deserialize<HaleyLifeState>(JsonSerializer.Serialize(life))!;
        loaded.Outing.Interrupt(); Assert.Equal("missed", loaded.Outing.Status);
        Assert.Equal("", loaded.Outing.Framing); Assert.True(loaded.IsValid(7));
    }
}
