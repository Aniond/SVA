using System.Net;
using System.Text.Json;
using SolaceWeather.Core;

namespace SolaceWeather.Core.Tests;

public class RomanceProfileTests
{
    [Fact]
    public void EveryNativeCandidateHasIndependentGroundingAndSafePortraits()
    {
        Assert.Equal(new[] { "Abigail", "Alex", "Elliott", "Emily", "Haley", "Harvey", "Leah", "Maru", "Penny", "Sam", "Sebastian", "Shane" }, RomanceProfiles.All.Select(p => p.Name));
        Assert.Equal(12, RomanceProfiles.All.Select(p => p.Voice).Distinct().Count());
        foreach (var profile in RomanceProfiles.All)
        {
            Assert.NotEmpty(profile.Anchors);
            Assert.NotEmpty(profile.Boundaries);
            Assert.NotEmpty(profile.Interests);
            Assert.Equal(0, profile.PortraitIndex("untrusted-expression"));
            Assert.Same(profile, RomanceProfiles.Get(profile.Name.ToLowerInvariant()));
            Assert.True(profile.EarliestDateTime < profile.LatestDateTime);
        }
        Assert.Null(RomanceProfiles.Get("Lewis"));
        Assert.Equal(9, RomanceProfiles.Get("Abigail")!.PortraitIndex("warm"));
        Assert.Equal(6, RomanceProfiles.Get("Abigail")!.PortraitIndex("serious"));
    }

    [Theory]
    [InlineData("Alex")]
    [InlineData("Elliott")]
    [InlineData("Emily")]
    [InlineData("Haley")]
    [InlineData("Harvey")]
    [InlineData("Leah")]
    [InlineData("Maru")]
    [InlineData("Penny")]
    [InlineData("Sam")]
    [InlineData("Sebastian")]
    [InlineData("Shane")]
    public void NativeEmotionSlotsUseVerifiedDialogueMapping(string name)
    {
        var profile = RomanceProfiles.Get(name)!;
        Assert.Equal(1, profile.PortraitIndex("happy"));
        Assert.Equal(1, profile.PortraitIndex("warm"));
        Assert.Equal(2, profile.PortraitIndex("sad"));
        Assert.Equal(5, profile.PortraitIndex("angry"));
        Assert.Equal(0, profile.PortraitIndex("thoughtful"));
        Assert.Equal(0, profile.PortraitIndex("serious"));
        Assert.Equal(0, profile.PortraitIndex("surprised"));
    }

    [Theory]
    [InlineData("Leah")]
    [InlineData("Shane")]
    public async Task GenericReplyUsesChosenCharacterAndRejectsAbigailQuestProposals(string name)
    {
        var handler = new CaptureHandler();
        using var http = new HttpClient(handler);
        var reply = await new GeminiConversation(http).ReplyForCharacter("test", "gemini-3.8-flash", "{\"Stage\":\"friendship\"}", "Hi", name, CancellationToken.None);
        Assert.Equal("Hello.", reply.Reply);
        Assert.Equal("", reply.QuestRequest);
        Assert.Contains("Roleplay " + name, handler.SystemPrompt);
        Assert.Contains(RomanceProfiles.Get(name)!.Voice, handler.SystemPrompt);
        Assert.DoesNotContain("Roleplay Abigail", handler.SystemPrompt);
        Assert.DoesNotContain("Quartz/Iron", handler.SystemPrompt);
        Assert.Contains("friendship-only", handler.SystemPrompt);
        Assert.Contains("Gossip", handler.SystemPrompt);
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public string SystemPrompt { get; private set; } = "";
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            using var payload = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            SystemPrompt = payload.RootElement.GetProperty("systemInstruction").GetProperty("parts")[0].GetProperty("text").GetString()!;
            string text = JsonSerializer.Serialize(new { reply = "Hello.", expression = "warm", questRequest = "fish", memories = Array.Empty<object>() });
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new { candidates = new[] { new { finishReason = "STOP", content = new { parts = new[] { new { text } } } } } })) };
        }
    }
}
