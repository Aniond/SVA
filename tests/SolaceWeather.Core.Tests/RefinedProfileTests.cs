using System.Net;
using System.Text.Json;
using SolaceWeather.Core;

namespace SolaceWeather.Core.Tests;

public sealed class RefinedProfileTests
{
    [Theory]
    [InlineData("Abigail")]
    [InlineData("Haley")]
    [InlineData("Penny")]
    [InlineData("Alex")]
    [InlineData("Maru")]
    public async Task RefinedVoiceAndFiveReactionsReachProviderWithoutAddingActions(string name)
    {
        var handler = new Capture(); var provider = new GeminiConversation(new HttpClient(handler));
        var reply = await provider.ReplyForCharacter("test", "gemini-3.8-flash", "{}", "What interests you?", name, CancellationToken.None);
        Assert.Equal("delighted", reply.Expression);
        using var request = JsonDocument.Parse(handler.Body);
        var schema = request.RootElement.GetProperty("generationConfig").GetProperty("responseSchema").GetProperty("properties");
        Assert.Equal(new[] { "delighted", "thoughtful", "concerned", "stern", "neutral" }, schema.GetProperty("expression").GetProperty("enum").EnumerateArray().Select(e => e.GetString()));
        string prompt = request.RootElement.GetProperty("systemInstruction").GetProperty("parts")[0].GetProperty("text").GetString()!;
        Assert.Contains(RomanceProfiles.Get(name)!.Voice, prompt);
        Assert.Contains("missing", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("[delighted]", reply.Reply);
        if (name is "Penny" or "Alex" or "Maru") Assert.Single(schema.GetProperty("questRequest").GetProperty("enum").EnumerateArray());
        Assert.Equal(1, RomanceProfiles.Get(name)!.PortraitIndex("delighted"));
        Assert.Equal(2, RomanceProfiles.Get(name)!.PortraitIndex("concerned"));
        Assert.Equal(name == "Abigail" ? 6 : 0, RomanceProfiles.Get(name)!.PortraitIndex("stern"));
        foreach (string reaction in new[] { "delighted", "thoughtful", "concerned", "stern", "neutral" })
            Assert.Equal(reaction, CharacterReactions.Normalize(name, reaction));
        await provider.TextForCharacter("test", "gemini-3.8-flash", "{}", "Hello", name, false, CancellationToken.None);
        Assert.Contains("1 to 3 short natural sentences", handler.Body);
    }
    private sealed class Capture : HttpMessageHandler
    {
        public string Body = "";
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Body = await request.Content!.ReadAsStringAsync(token);
            var reply = JsonSerializer.Serialize(new { reply = "That sounds interesting.", expression = "delighted", memories = Array.Empty<object>(), askedTopic = "", questRequest = "none", recalledExperienceId = "", spontaneousRecall = false, commentedOutfit = false });
            return new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new { candidates = new[] { new { finishReason = "STOP", content = new { parts = new[] { new { text = reply } } } } } })) };
        }
    }
}
