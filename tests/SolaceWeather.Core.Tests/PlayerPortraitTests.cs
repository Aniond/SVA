using System.Net;
using System.Reflection;
using System.Text;
using SolaceWeather.Core;

namespace SolaceWeather.Core.Tests;

public class PlayerPortraitTests
{
    [Fact]
    public async Task JpegImagePartIsAcceptedWithoutModelRetry()
    {
        byte[] image = new byte[128]; image[0] = 255; image[1] = 216;
        var handler = new Handler(_ => new(HttpStatusCode.OK) { Content = new StringContent("{\"candidates\":[{\"content\":{\"parts\":[{\"inlineData\":{\"mimeType\":\"image/jpeg\",\"data\":\"" + Convert.ToBase64String(image) + "\"}}]}}]}") });
        Assert.Equal(image, await new GeminiPlayerPortrait(new HttpClient(handler)).Generate("test-key", new byte[] { 1 }, CancellationToken.None));
        Assert.Equal(1, handler.Calls);
    }
    [Fact]
    public void DedicatedImageProviderExistsApartFromConversation()
        => Assert.NotNull(typeof(GeminiConversation).Assembly.GetType("SolaceWeather.Core.GeminiPlayerPortrait"));

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        { Calls++; return Task.FromResult(reply(request)); }
    }

    [Fact]
    public async Task HttpFailureIsSanitizedAndNeverRetried()
    {
        var handler = new Handler(_ => new(HttpStatusCode.TooManyRequests) { Content = new StringContent("private provider content") });
        var provider = new GeminiPlayerPortrait(new HttpClient(handler));
        var error = await Assert.ThrowsAnyAsync<InvalidOperationException>(() => provider.Generate("test-key", new byte[] { 1 }, CancellationToken.None));
        Assert.Equal(1, handler.Calls); Assert.DoesNotContain("private", error.Message);
    }

    [Fact]
    public async Task TextOnlyResponseDoesNotBecomeAnImage()
    {
        var handler = new Handler(_ => new(HttpStatusCode.OK) { Content = new StringContent("{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"no image\"}]}}]}") });
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => new GeminiPlayerPortrait(new HttpClient(handler)).Generate("test-key", new byte[] { 1 }, CancellationToken.None));
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task ImagePartUsesDedicatedEndpointAndNoIdentityMetadata()
    {
        byte[] image = Enumerable.Range(0, 128).Select(i => (byte)i).ToArray();
        Task<string>? bodyTask = null;
        var handler = new Handler(request =>
        {
            Assert.EndsWith("/v1/models/gemini-3.1-flash-image:generateContent", request.RequestUri!.ToString());
            Assert.Equal("test-key", request.Headers.GetValues("x-goog-api-key").Single());
            bodyTask = request.Content!.ReadAsStringAsync();
            return new(HttpStatusCode.OK) { Content = new StringContent("{\"candidates\":[{\"content\":{\"parts\":[{\"inlineData\":{\"mimeType\":\"image/png\",\"data\":\"" + Convert.ToBase64String(image) + "\"}}]}}]}") };
        });
        Assert.Equal(image, await new GeminiPlayerPortrait(new HttpClient(handler)).Generate("test-key", new byte[] { 1, 2 }, CancellationToken.None));
        string body = await bodyTask!;
        Assert.Equal(1, handler.Calls); Assert.DoesNotContain("test-key", body);
        Assert.DoesNotContain("farmerId", body!); Assert.DoesNotContain("responseSchema", body!);
    }

    [Fact]
    public void OldSessionCannotPublishToSameOrDifferentReloadedSave()
    {
        var sessions = new PortraitSessionGuard();
        int old = sessions.Current;
        Assert.True(sessions.IsCurrent(old));
        sessions.Reset(); Assert.False(sessions.IsCurrent(old));
        Assert.True(sessions.IsCurrent(sessions.Current));
    }

    [Fact]
    public async Task CacheKeepsSignedPlayerIdentityAndDetectsCorruption()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var cache = new PlayerPortraitCache(root, "42", "-57");
            byte[] bytes = { 10, 20, 30 };
            string hash = cache.WriteImage(bytes);
            Assert.Equal(bytes, cache.ReadImage(hash));
            File.WriteAllBytes(cache.ImagePath, new byte[] { 1 });
            Assert.Throws<InvalidDataException>(() => cache.ReadImage(hash));
            Assert.NotEqual(cache.ImagePath, new PlayerPortraitCache(root, "43", "-57").ImagePath);
            Assert.Throws<ArgumentException>(() => new PlayerPortraitCache(root, "../42", "-57"));
            await Task.CompletedTask;
        }
        finally { Directory.Delete(root, true); }
    }
    [Fact]
    public void AttemptClaimSurvivesReloadAndNeedsExplicitRetry()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var cache = new PlayerPortraitCache(root, "42", "-57");
            Assert.True(cache.TryClaim(false)); Assert.False(new PlayerPortraitCache(root, "42", "-57").TryClaim(false));
            Assert.True(cache.TryClaim(true)); Assert.False(cache.TryClaim(false));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void FailedMetadataCommitKeepsPreviousImageAndRecord()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var cache = new PlayerPortraitCache(root, "42", "-57");
            byte[] original = { 10, 20, 30 };
            var record = new PlayerPortraitRecord { FarmId = "42", FarmerId = "-57", Status = "Ready", Sha256 = cache.WriteImage(original) };
            cache.WriteRecord(record); string originalHash = record.Sha256;
            Assert.Throws<IOException>(() => cache.CommitImage(new byte[] { 40, 50, 60 }, record, updated =>
            {
                cache.WriteRecord(updated); throw new IOException("Simulated save metadata failure");
            }));
            Assert.Equal(original, cache.ReadImage(originalHash)); Assert.Equal(originalHash, record.Sha256);
            Assert.Equal(originalHash, cache.ReadRecord()!.Sha256);
        }
        finally { Directory.Delete(root, true); }
    }

    [Theory]
    [InlineData(0xC0)]
    [InlineData(0xC2)]
    public void JpegDimensionsAreReadBeforeDecode(int marker)
    {
        byte[] jpeg = new byte[128];
        byte[] header = { 255, 216, 255, 224, 0, 4, 0, 0, 255, (byte)marker, 0, 8, 8, 4, 0, 4, 0, 1 };
        header.CopyTo(jpeg, 0);
        Assert.Equal((1024, 1024), PortraitImageHeader.Read(jpeg));
        jpeg[13] = 32;
        Assert.Equal("image_dimensions", Assert.Throws<PortraitFailureException>(() => PortraitImageHeader.Read(jpeg)).Code);
    }

    [Fact]
    public void MalformedJpegAndWebpAreRejectedBeforeGraphicsAllocation()
    {
        byte[] jpeg = new byte[128]; new byte[] { 255, 216, 255, 224, 255, 255 }.CopyTo(jpeg, 0);
        Assert.Equal("invalid_jpeg_header", Assert.Throws<PortraitFailureException>(() => PortraitImageHeader.Read(jpeg)).Code);
        Assert.Equal("unsupported_image_format", Assert.Throws<PortraitFailureException>(() => PortraitImageHeader.Read(new byte[128])).Code);
    }

    [Fact]
    public void FailureDiagnosticsNeverIncludeExceptionText()
    {
        Assert.Equal("request:http_429", PortraitImageHeader.FailureCategory(new PortraitFailureException("http_429"), "request"));
        Assert.Equal("service:unexpected_failure", PortraitImageHeader.FailureCategory(new Exception("secret key or response"), "secret phase"));
        Assert.Equal("request:unexpected_failure", PortraitImageHeader.FailureCategory(new PortraitFailureException("secret key"), "request"));
    }
}
