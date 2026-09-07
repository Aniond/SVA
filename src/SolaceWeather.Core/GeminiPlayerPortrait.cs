using System.Text;
using System.Text.Json;

namespace SolaceWeather.Core;

/// <summary>A single image request; deliberately independent of conversation models and response schemas.</summary>
public sealed class GeminiPlayerPortrait
{
    public const string Model = "gemini-3.1-flash-image";
    private readonly HttpClient http;
    public GeminiPlayerPortrait(HttpClient http) => this.http = http;
    public async Task<byte[]> Generate(string key, byte[] reference, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new PortraitFailureException("credential_missing");
        const string prompt = "Create ONE square head-and-shoulders player dialogue portrait matching the game avatar in the attached reference. Use polished Stardew Valley pixel art: crisp deliberate pixel clusters, soft dimensional shading, expressive friendly neutral face, subtle material detail. Match the reference skin tone, hair style and hair color, eye color, clothing and visible accessories. The game avatar is the identity reference; do not redesign or substitute another character. Center the bust, all hair and shoulders inside frame, simple muted dark blue background. No text, no UI, no multiple views, no sprite sheet, no extra characters.";
        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://generativelanguage.googleapis.com/v1/models/{Model}:generateContent");
        request.Headers.Add("x-goog-api-key", key);
        request.Content = new StringContent(JsonSerializer.Serialize(new { contents = new[] { new { role = "user", parts = new object[] { new { text = prompt }, new { inlineData = new { mimeType = "image/png", data = Convert.ToBase64String(reference) } } } } }, generationConfig = new { responseModalities = new[] { "TEXT", "IMAGE" }, imageConfig = new { aspectRatio = "1:1" } } }), Encoding.UTF8, "application/json");
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) throw new PortraitFailureException($"http_{(int)response.StatusCode}");
        using var stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
        using var content = new MemoryStream(); var buffer = new byte[8192]; int count;
        while ((count = await stream.ReadAsync(buffer.AsMemory(), token).ConfigureAwait(false)) > 0)
        { if (content.Length + count > 16 * 1024 * 1024) throw new PortraitFailureException("response_byte_limit"); await content.WriteAsync(buffer.AsMemory(0, count), token).ConfigureAwait(false); }
        using var json = JsonDocument.Parse(content.ToArray());
        if (json.RootElement.TryGetProperty("candidates", out var candidates))
            foreach (var candidate in candidates.EnumerateArray())
                if (candidate.TryGetProperty("content", out var body) && body.TryGetProperty("parts", out var parts))
                    foreach (var part in parts.EnumerateArray())
                        if (part.TryGetProperty("inlineData", out var data) && data.TryGetProperty("mimeType", out var mime) && data.TryGetProperty("data", out var encoded))
                        {
                            if (mime.GetString() is not ("image/png" or "image/jpeg")) throw new PortraitFailureException("unsupported_image_mime");
                            byte[] bytes = Convert.FromBase64String(encoded.GetString() ?? "");
                            if (bytes.Length < 100 || bytes.Length > 8 * 1024 * 1024) throw new PortraitFailureException("image_byte_limit");
                            return bytes; // Main-thread service verifies decoded dimensions and actual pixels before caching.
                        }
        throw new PortraitFailureException("no_image_returned");
    }
}
