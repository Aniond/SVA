using System.Text.Json;
using SolaceWeather.Core;

namespace SolaceWeather.Core.Tests;

public class DeliveryRequestTests
{
    [Fact]
    public void OldSavesHaveNoQuestAndUnsupportedRequestsDoNothing()
    {
        var memory = JsonSerializer.Deserialize<AbigailMemory>("{}")!;
        Assert.True(memory.IsValid());
        Assert.Equal("none", memory.Delivery.Status);
        Assert.False(memory.Delivery.Offer("gold", 3, true));
        Assert.False(memory.Delivery.TestFishPending);
    }

    [Fact]
    public void RepeatedRepliesAndReloadDoNotDuplicateQuestOrTestFish()
    {
        var state = new DeliveryRequest();
        Assert.True(state.Offer("fish", 3, true));
        Assert.False(state.Offer("fish", 3, true));
        Assert.True(state.TestFishPending);
        state.MarkTestFishGranted();
        var loaded = JsonSerializer.Deserialize<DeliveryRequest>(JsonSerializer.Serialize(state))!;
        Assert.True(loaded.IsValid());
        Assert.False(loaded.TestFishPending);
        Assert.False(loaded.Offer("fish", 4, true));
    }

    [Fact]
    public void CompletionIsRecordedOnceAndStopsPendingTestItem()
    {
        var state = new DeliveryRequest();
        Assert.False(state.Complete("(O)131", "Sardine", 4));
        state.Offer("fish", 3, true);
        Assert.True(state.Complete("(O)131", "Sardine", 4));
        Assert.False(state.Complete("(O)131", "Sardine", 4));
        Assert.False(state.TestFishPending);
        Assert.False(state.Offer("fish", 5, true));
        var loaded = JsonSerializer.Deserialize<DeliveryRequest>(JsonSerializer.Serialize(state))!;
        Assert.True(loaded.IsValid());
        Assert.Equal("Sardine", loaded.DeliveredItemName);
        Assert.Equal(4, loaded.CompletedDay);
    }

    [Fact]
    public void NormalQuestNeverGrantsFreeFishAndMalformedStateIsRejected()
    {
        var state = new DeliveryRequest();
        state.Offer("fish", 3, false);
        Assert.False(state.TestFishPending);
        state.CompletedDay = 2;
        Assert.False(state.IsValid());
        Assert.False(new DeliveryRequest { Status = "completed" }.IsValid());
    }
}
