using System.Net;
using System.Text.Json;
using SolaceWeather.Core;

namespace SolaceWeather.Core.Tests;

public class PortraitReactionTests
{
    [Theory]
    [InlineData("happy", 1)]
    [InlineData("sad", 2)]
    [InlineData("angry", 3)]
    [InlineData("thoughtful", 4)]
    [InlineData("serious", 6)]
    [InlineData("surprised", 7)]
    [InlineData("warm", 9)]
    [InlineData("neutral", 0)]
    [InlineData("invented", 0)]
    [InlineData(null, 0)]
    public void AuthoredReactionsUseTheMatchingPortraitCell(string? expression, int expected)
    {
        Assert.Equal(expected, AbigailExpression.PortraitIndex(expression));
    }

    [Theory]
    [InlineData("sad", "sad")]
    [InlineData("invented", "neutral")]
    [InlineData(null, "neutral")]
    public async Task StructuredReactionIsReadAndRestricted(string? supplied, string expected)
    {
        using var http = new HttpClient(new ReactionHandler(supplied));
        var reply = await new GeminiConversation(http).ReplyWithMemory("test", "gemini-3.8-flash", "{}", "Sorry I missed the promised date.", CancellationToken.None);
        Assert.Equal(expected, reply.Expression);
    }
    private sealed class ReactionHandler(string? expression) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            using var payload = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            Assert.True(payload.RootElement.GetProperty("generationConfig").GetProperty("responseSchema").GetProperty("properties").TryGetProperty("expression", out _));
            var response = JsonSerializer.Serialize(new { reply = "I was counting on it. Thanks for telling me.", expression, memories = Array.Empty<object>(), askedTopic = "", questRequest = "none" });
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new { candidates = new[] { new { finishReason = "STOP", content = new { parts = new[] { new { text = response } } } } } })) };
        }
    }
}
