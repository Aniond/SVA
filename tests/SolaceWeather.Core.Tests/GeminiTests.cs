using System.Net;
using SolaceWeather.Core;

namespace SolaceWeather.Core.Tests;

public class GeminiTests
{
    [Theory]
    [InlineData("Abigail", false, "cemetery", "cemetery")]
    [InlineData("Sam", false, "cemetery", "")]
    [InlineData("Abigail", true, "cemetery", "")]
    [InlineData("Abigail", false, "spawn-monster", "")]
    [InlineData("Haley", false, "haley-photo", "haley-photo")]
    [InlineData("Haley", false, "haley-sunflower", "haley-sunflower")]
    [InlineData("Haley", true, "haley-photo", "")]
    [InlineData("Haley", false, "cemetery", "")]
    [InlineData("Abigail", false, "haley-photo", "")]
    [InlineData("Emily", false, "emily-cloth", "emily-cloth")]
    [InlineData("Emily", false, "emily-design", "emily-design")]
    [InlineData("Emily", true, "emily-design", "")]
    [InlineData("Emily", false, "haley-photo", "")]
    [InlineData("Haley", false, "emily-cloth", "")]
    public async Task OutingProposalsAreRestrictedToSupportedInPersonTemplate(string name, bool phone, string proposal, string expected)
    {
        string structured = System.Text.Json.JsonSerializer.Serialize(new { reply = "Let's investigate.", questRequest = proposal });
        string envelope = System.Text.Json.JsonSerializer.Serialize(new { candidates = new[] {
            new { finishReason = "STOP", content = new { parts = new[] { new { text = structured } } } }
        } });
        using var http = new HttpClient(new Handler(HttpStatusCode.OK, envelope));
        var client = new GeminiConversation(http);
        var reply = phone
            ? await client.TextForCharacter("test", "gemini-3.8-flash", "{}", "Hi", name, false, CancellationToken.None)
            : await client.ReplyForCharacter("test", "gemini-3.8-flash", "{}", "Hi", name, CancellationToken.None);
        Assert.Equal(expected, reply.QuestRequest);
    }
    [Fact]
    public async Task StructuredReplySeparatesSpeechFromMemoryProposals()
    {
        string structured = System.Text.Json.JsonSerializer.Serialize(new { reply = "That sounds fun!", memories = new[] {
            new { topic = "fishing", kind = "preference", quote = "I enjoy fishing.", timing = "unspecified" }
        }, askedTopic = "", questRequest = "fish" });
        string envelope = System.Text.Json.JsonSerializer.Serialize(new { candidates = new[] {
            new { finishReason = "STOP", content = new { parts = new[] { new { text = structured } } } }
        } });
        using var http = new HttpClient(new Handler(HttpStatusCode.OK, envelope));
        var reply = await new GeminiConversation(http).ReplyWithMemory("test", "gemini-3.8-flash", "{}", "I enjoy fishing.", CancellationToken.None);
        Assert.Equal("That sounds fun!", reply.Reply);
        Assert.Equal("fish", reply.QuestRequest);
        Assert.Equal("I enjoy fishing.", Assert.Single(reply.Memories).Quote);
    }
    private sealed class Handler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            using var payload = System.Text.Json.JsonDocument.Parse(request.Content!.ReadAsStringAsync(token).GetAwaiter().GetResult());
            if (payload.RootElement.GetProperty("generationConfig").TryGetProperty("responseSchema", out var schema))
                Assert.All(schema.GetProperty("properties").GetProperty("questRequest").GetProperty("enum").EnumerateArray(),
                    value => Assert.False(string.IsNullOrEmpty(value.GetString())));
            Assert.Equal("generativelanguage.googleapis.com", request.RequestUri!.Host);
            Assert.Empty(request.RequestUri.Query);
            Assert.True(request.Headers.Contains("x-goog-api-key"));
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        }
    }

    [Fact]
    public async Task CompleteTextIsReturnedWithoutThoughtsOrGameCommands()
    {
        using var http = new HttpClient(new Handler(HttpStatusCode.OK,
            "{\"candidates\":[{\"finishReason\":\"STOP\",\"content\":{\"parts\":[{\"thought\":true,\"text\":\"secret thought\"},{\"text\":\"Hello#$1^@friend\"}]}}]}"));
        var client = new GeminiConversation(http);
        Assert.Equal("Hello  1  friend", await client.Reply("test", "gemini-2.5-flash-lite", "{}", "Hi", CancellationToken.None));
    }

    [Theory]
    [InlineData(429, "sensitive upstream body")]
    [InlineData(200, "{\"candidates\":[]}")]
    [InlineData(200, "{\"candidates\":[{\"finishReason\":\"MAX_TOKENS\"}]}")]
    public async Task FailuresDoNotExposeProviderBody(int code, string body)
    {
        using var http = new HttpClient(new Handler((HttpStatusCode)code, body));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new GeminiConversation(http).Reply("test", "gemini-2.5-flash-lite", "{}", "Hi", CancellationToken.None));
        Assert.DoesNotContain("sensitive", error.Message);
    }
}
