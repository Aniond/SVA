using System.Net;
using System.Text.Json;
using SolaceWeather.Core;

namespace SolaceWeather.Core.Tests;

public class PhoneProviderTests
{
    [Fact]
    public async Task PhoneRequestUsesTrustedChannelAndRepeatedTextCallsProviderAgain()
    {
        var handler = new Capture();
        using var http = new HttpClient(handler);
        var provider = new GeminiConversation(http);
        await provider.TextForCharacter("test", "gemini-3.8-flash", "{\"Farmer\":\"Alex\"}", "Hello", "Abigail", false, CancellationToken.None);
        await provider.TextForCharacter("test", "gemini-3.8-flash", "{}", "Hello", "Abigail", false, CancellationToken.None);
        Assert.Equal(2, handler.Payloads.Count);
        var root = JsonDocument.Parse(handler.Payloads[0]).RootElement;
        Assert.Contains("PHONE TEXT", root.GetProperty("systemInstruction").ToString());
        Assert.Contains("Abigail", root.GetProperty("systemInstruction").ToString());
        Assert.Contains("Alex", root.GetProperty("contents").ToString());
        await provider.TextForCharacter("test", "gemini-3.8-flash", "{}", "", "Sam", true, CancellationToken.None);
        root = JsonDocument.Parse(handler.Payloads.Last()).RootElement;
        Assert.Contains("No farmer message", root.GetProperty("contents").ToString());
        Assert.DoesNotContain("FARMER SAYS", root.GetProperty("contents").ToString());
    }
    private sealed class Capture : HttpMessageHandler
    {
        public List<string> Payloads { get; } = new();
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Payloads.Add(await request.Content!.ReadAsStringAsync(token));
            return new(HttpStatusCode.OK) { Content = new StringContent("{\"candidates\":[{\"finishReason\":\"STOP\",\"content\":{\"parts\":[{\"text\":\"{\\\"reply\\\":\\\"Hey!\\\",\\\"memories\\\":[]}\"}]}}]}") };
        }
    }
}
