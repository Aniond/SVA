using System.Text.Json;
using SolaceWeather.Core;

namespace SolaceWeather.Core.Tests;

public class HaleyPromiseTests
{
    [Fact]
    public void ObservedMaterialAndExplicitAcceptanceAreRequired()
    {
        var promise = new HaleyPromiseState();
        Assert.False(promise.Offer(1)); promise.Observe("(O)421");
        Assert.True(promise.Offer(1)); Assert.False(promise.Complete("(O)421", "Sunflower", 1));
        Assert.False(promise.Accept(1, 0)); Assert.True(promise.Accept(1, 3));
        Assert.Equal(4, promise.DueDay); Assert.False(promise.Accept(1, 1));
        Assert.False(promise.Complete("(O)24", "Parsnip", 2));
        Assert.True(promise.Complete("(O)421", "Sunflower", 2));
        Assert.False(promise.Complete("(O)421", "Sunflower", 2));
        Assert.Equal("completed", promise.Status); Assert.Equal(1, promise.Reliability);
    }
    [Fact]
    public void DeclineAndExtensionHaveClearLimits()
    {
        var promise = Offered(); Assert.True(promise.Decline(1)); Assert.Equal(0, promise.Reliability);
        Assert.False(promise.Offer(2)); Assert.True(promise.Offer(4)); Assert.True(promise.Accept(4, 1));
        Assert.True(promise.Extend(5)); Assert.Equal(7, promise.DueDay); Assert.False(promise.Extend(5));
        promise.AdvanceDay(7); Assert.Equal("active", promise.Status);
        promise.AdvanceDay(8); Assert.Equal("overdue", promise.Status); Assert.Equal(-1, promise.Reliability);
        Assert.True(promise.Complete("(O)421", "Sunflower", 8)); Assert.True(promise.WasLate);
        Assert.Equal(0, promise.Reliability);
    }
    [Fact]
    public void RenewedPromiseDoesNotMislabelTimelyDeliveryAsLate()
    {
        var promise = Offered(); promise.Accept(1, 1); promise.AdvanceDay(3); promise.Abandon(3);
        promise.Offer(5); promise.Accept(5, 3); Assert.True(promise.Complete("(O)421", "Sunflower", 6));
        Assert.False(promise.WasLate); Assert.Equal(0, promise.Reliability);
    }
    [Fact]
    public void ReloadAndAbandonmentNeverInventDelivery()
    {
        var promise = Offered(); promise.Accept(1, 1); Assert.True(promise.Abandon(1));
        Assert.Null(promise.CompletedDay); Assert.False(promise.Offer(2)); Assert.True(promise.Offer(3));
        var loaded = JsonSerializer.Deserialize<HaleyPromiseState>(JsonSerializer.Serialize(promise))!;
        Assert.True(loaded.IsValid()); Assert.Equal("offered", loaded.Status);
        Assert.False(loaded.Accept(2, 1)); Assert.Null(loaded.CompletedDay);
        loaded.Status = "invented"; Assert.False(loaded.IsValid());
    }
    private static HaleyPromiseState Offered()
    {
        var promise = new HaleyPromiseState(); promise.Observe("(O)421"); Assert.True(promise.Offer(1)); return promise;
    }
}
