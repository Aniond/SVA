using System.Net;
using System.Text.Json;
using SolaceWeather.Core;

namespace SolaceWeather.Core.Tests;

public class TownChatterTests
{
    [Fact]
    public void AttemptsAreBoundedAcrossPairsDaysAndReloads()
    {
        var state = new TownChatterState { FarmerId = 10 };
        Assert.True(state.TryAttempt("Abigail", "Sam", 0, 600));
        Assert.False(state.TryAttempt("Sam", "Abigail", 0, 800));
        Assert.False(state.TryAttempt("Abigail", "Sebastian", 0, 719));
        Assert.True(state.TryAttempt("Abigail", "Sebastian", 0, 720));
        Assert.True(state.TryAttempt("Leah", "Elliott", 0, 840));
        Assert.False(state.TryAttempt("Emily", "Haley", 0, 960));
        var restored = JsonSerializer.Deserialize<TownChatterState>(JsonSerializer.Serialize(state))!;
        Assert.True(restored.IsValid(10)); Assert.False(restored.IsValid(11));
        Assert.False(restored.TryAttempt("Emily", "Haley", 0, 1080));
        Assert.True(restored.TryAttempt("Abigail", "Sam", 1, 360));
        Assert.False(restored.TryAttempt("Abigail", "Abigail", 1, 600));
    }

    [Fact]
    public void WitnessedSpeechIsBoundedAndAttributedWithoutFarmerClaims()
    {
        var state = new TownChatterState { FarmerId = 10 };
        for (int day = 0; day < 30; day++)
            Assert.True(state.Record(day, "Town", "weather", new[] { new ChatterLine("Abigail", "I like this rain."), new ChatterLine("Sam", "My shoes disagree.") }));
        Assert.Equal(24, state.Heard.Count);
        Assert.True(state.IsValid(10));
        Assert.Equal(4, state.ForCharacter("Abigail").Length);
        Assert.Empty(state.ForCharacter("Harvey"));
        Assert.False(state.Record(31, "Town", "weather", new[] { new ChatterLine("Farmer", "Private claim.") }));
    }

    [Fact]
    public async Task ProviderUsesOnlyTypedPublicFactsAndRejectsWrongSpeakersOrUnknownFacts()
    {
        var context = new ChatterContext("Abigail", "Sam", 0, "spring", 1, "Town", new[] { new ChatterFact("weather", "It is raining here now.") });
        var handler = new Capture(); using var http = new HttpClient(handler);
        var provider = new GeminiChatter(http);
        var reply = await provider.Generate("test", "gemini-3.8-flash", context, CancellationToken.None);
        Assert.Equal("Abigail", reply.Lines[0].Speaker);
        Assert.Contains("independent", handler.Payload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("public", handler.Payload);
        Assert.DoesNotContain("PersistentDetails", handler.Payload);
        Assert.DoesNotContain("FARMER SAYS", handler.Payload);
        handler.Reply = new ChatterReply("secret", new[] { new ChatterLine("Abigail", "Hi."), new ChatterLine("Sam", "Hi.") });
        await Assert.ThrowsAsync<InvalidDataException>(() => provider.Generate("test", "gemini-3.8-flash", context, CancellationToken.None));
        handler.Reply = new ChatterReply("weather", new[] { new ChatterLine("Farmer", "Hi."), new ChatterLine("Sam", "Hi.") });
        await Assert.ThrowsAsync<InvalidDataException>(() => provider.Generate("test", "gemini-3.8-flash", context, CancellationToken.None));
    }
    private sealed class Capture : HttpMessageHandler
    {
        public string Payload = "";
        public ChatterReply Reply = new("weather", new[] { new ChatterLine("Abigail", "I like this rain."), new ChatterLine("Sam", "My shoes disagree.") });
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Payload = await request.Content!.ReadAsStringAsync(token);
            return new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new { candidates = new[] { new { finishReason = "STOP", content = new { parts = new[] { new { text = JsonSerializer.Serialize(Reply) } } } } } })) };
        }
    }
}
