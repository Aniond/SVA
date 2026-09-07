using SolaceWeather.Core;

namespace SolaceWeather.Core.Tests;

public class TailoringTests
{
    [Fact]
    public void LaterRecipesNeedCraftsOnTwoDaysAndCompletedMovement()
    {
        var tree = new EmilyTreeState(); tree.RecordCraft(1); Assert.Empty(tree.CraftDays);
        tree.RecordDesign(1, "quiet", "natural"); tree.RecordDesign(4, "playful", "geometric"); tree.Refresh(true, 3);
        Assert.Contains("tailor", tree.Unlocked); tree.RecordCraft(4); tree.RecordCraft(4); tree.Refresh(true, 3);
        Assert.Single(tree.CraftDays); Assert.DoesNotContain("care", tree.Unlocked);
        tree.RecordCraft(7); tree.Refresh(true, 3); Assert.DoesNotContain("care", tree.Unlocked);
        Assert.True(tree.RecordMovement(7)); tree.Refresh(true, 3); Assert.Contains("care", tree.Unlocked); Assert.True(tree.IsValid());
    }
    [Fact]
    public void SavedOrderPayloadIsPortableAndDoesNotDuplicatePendingImage()
    {
        var state = new TailoringSaveState { FarmerId = -7 }; var order = state.Create("shirt", "plain", "Green", 1)!;
        order.Png = "placeholder";
        string json = System.Text.Json.JsonSerializer.Serialize(state);
        Assert.DoesNotContain("Pending", json); Assert.Equal(1, json.Split("placeholder").Length - 1);
        var restored = System.Text.Json.JsonSerializer.Deserialize<TailoringSaveState>(json)!;
        Assert.True(restored.IsValid(-7)); Assert.Equal(order.QualifiedId, restored.Pending!.QualifiedId);
        Assert.True(restored.Cancel(order.Id)); Assert.Empty(restored.Orders[0].Png);
    }
    [Fact]
    public async Task ImageFailureIsOneSanitizedRequestAndNeverTriggersAutomaticRetry()
    {
        var handler = new ImageHandler();
        var error = await Assert.ThrowsAsync<PortraitFailureException>(() => new GeminiTailoringImage(new HttpClient(handler)).Generate("test-secret", new byte[] { 1 }, "Green stars", CancellationToken.None));
        Assert.Equal("no_image_returned", error.Code); Assert.Equal(1, handler.Calls); Assert.DoesNotContain("test-secret", handler.Body);
        Assert.Contains("Green stars", handler.Body); Assert.Contains("inlineData", handler.Body);
    }
    private sealed class ImageHandler : HttpMessageHandler
    {
        public int Calls; public string Body = "";
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        { Calls++; Body = await request.Content!.ReadAsStringAsync(token); return new(System.Net.HttpStatusCode.OK) { Content = new StringContent("{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"no image\"}]}}]}") }; }
    }
    [Fact]
    public void GenerationFailureNeverFulfillsAndOnlyValidatedReadyOrdersCanCommit()
    {
        var state = new TailoringSaveState { FarmerId = 7 };
        var order = state.Create("shirt", "plain", "Green with small gold stars", 1)!;
        Assert.True(state.IsValid(7)); Assert.False(state.IsValid(8));
        Assert.Null(state.Create("pants", "plain", "Blue", 1));
        Assert.False(state.Fulfill(order.Id, 1));
        Assert.True(state.Begin(order.Id)); Assert.True(state.Fail(order.Id, "no_image_returned"));
        Assert.False(state.Fulfill(order.Id, 1)); Assert.Equal(-100, state.LastCraftDay);
        Assert.True(state.Begin(order.Id));
        Assert.False(state.Ready(order.Id, "bad", "00", "20 30 40"));
        Assert.False(state.Fulfill(order.Id, 1));
    }

    [Fact]
    public void RecoveryNeedsActiveEquippedTimeAndHonorsCapsAndNoCatchup()
    {
        var meter = new TailoringRecovery();
        for (int i = 0; i < 60; i++) Assert.Equal(0, meter.Tick(1, false, true, false, false, 1).Health);
        for (int i = 0; i < 59; i++) Assert.Equal(0, meter.Tick(1, true, true, true, false, 1).Health);
        var earned = meter.Tick(1, true, true, true, false, 1);
        Assert.Equal(1, earned.Health); Assert.Equal(2, earned.Energy);
        Assert.Equal(0, meter.Tick(1, true, true, true, false, 5000).Health);
        for (int i = 0; i < 30; i++) meter.Tick(1, true, true, true, false, 1);
        meter.Tick(1, true, false, false, false, 1);
        for (int i = 0; i < 59; i++) Assert.Equal(0, meter.Tick(1, true, true, true, false, 1).Health);
        Assert.Equal(1, meter.Tick(1, true, true, true, false, 1).Health);
        for (int i = 0; i < 3000; i++) meter.Tick(1, true, true, true, false, 1);
        Assert.Equal(20, meter.HealthToday); Assert.Equal(40, meter.EnergyToday);
        Assert.Equal(0, meter.Tick(2, true, true, true, true, 1).Health);
        Assert.Equal(0, meter.HealthToday); Assert.True(meter.IsValid());
    }
}
