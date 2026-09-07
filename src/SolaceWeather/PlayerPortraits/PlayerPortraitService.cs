using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SolaceWeather.Core;
using StardewModdingAPI;
using StardewValley;

namespace SolaceWeather.PlayerPortraits;

/// <summary>Owns one local player's portrait for the active save. All texture and save operations run on the game thread.</summary>
public sealed class PlayerPortraitService
{
    private const string SaveKey = "player-portrait-v1";
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(120) };
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly Func<bool> enabled;
    private readonly Func<string, byte[], CancellationToken, Task<byte[]>> generate;
    private PlayerPortraitCache? cache;
    private PlayerPortraitRecord? record;
    private Texture2D? portrait, fallback;
    private byte[]? reference;
    private string? farmId, farmerId;
    private bool creationPending;
    private PortraitCaptureRecovery captureRecovery = new();
    private readonly PortraitSessionGuard sessions = new();
    private Task<byte[]>? pending;
    private CancellationTokenSource? cancellation;
    private int requestSession;
    public event Action<long>? PortraitChanged;

    public PlayerPortraitService(IModHelper helper, IMonitor monitor, Func<bool> enabled,
        Func<string, byte[], CancellationToken, Task<byte[]>>? generate = null)
    {
        this.helper = helper; this.monitor = monitor; this.enabled = enabled;
        this.generate = generate ?? new GeminiPlayerPortrait(Http).Generate;
        helper.Events.GameLoop.UpdateTicked += (_, _) => SafeTick();
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => Reset(clearCreation: true);
        helper.Events.GameLoop.SaveLoaded += (_, _) => Reset(clearCreation: false);
        helper.Events.GameLoop.Saving += (_, _) =>
        {
            if (CurrentIdentityMatches() && record != null) helper.Data.WriteSaveData(SaveKey, record);
        };
    }

    /// <summary>Call only after successful native creation confirmation, never after a cancelled menu.</summary>
    public void MarkCreationConfirmed() => creationPending = true;

    public string GetStatus(long id) => CurrentIdentityMatches() && farmerId == id.ToString(CultureInfo.InvariantCulture)
        ? record?.Status ?? "Missing" : "Missing";
    public string? GetFailureCategory(long id) => CurrentIdentityMatches() && farmerId == id.ToString(CultureInfo.InvariantCulture)
        ? record?.FailureCategory : null;

    /// <summary>The source rectangle covers a single image, not a native 64px emotion atlas. Do not dispose this texture.</summary>
    public bool TryGetPortrait(long id, out Texture2D texture, out Rectangle source)
    {
        texture = null!; source = Rectangle.Empty;
        if (!CurrentIdentityMatches() || farmerId != id.ToString(CultureInfo.InvariantCulture)) return false;
        var available = portrait ?? fallback;
        if (available == null || available.IsDisposed) return false;
        texture = available; source = new Rectangle(0, 0, available.Width, available.Height); return true;
    }

    /// <summary>Explicit user retry only. Draw calls, save loads, and clothing changes never call this.</summary>
    public bool Retry(long id)
    {
        if (!CurrentIdentityMatches() || farmerId != id.ToString(CultureInfo.InvariantCulture) || pending != null || !enabled()) return false;
        try
        {
            if (reference == null && !CaptureReference(explicitRetry: true)) { Fail(new PortraitFailureException("capture_failed"), "capture"); return false; }
            return StartRequest(explicitRetry: true);
        }
        catch (Exception ex) { Fail(ex, "retry"); return false; }
    }

    private static bool WorldReady => Context.IsWorldReady && !Context.IsMultiplayer && Game1.player != null && Game1.uniqueIDForThisGame != 0;
    private bool CurrentIdentityMatches() => WorldReady && farmId == Game1.uniqueIDForThisGame.ToString(CultureInfo.InvariantCulture)
        && farmerId == Game1.player.UniqueMultiplayerID.ToString(CultureInfo.InvariantCulture);

    private void SafeTick()
    {
        try { Tick(); }
        catch (Exception ex) { Fail(ex); } // No raw provider response, avatar, or credentials in logs.
    }

    private void Tick()
    {
        if (!WorldReady) return;
        if (!CurrentIdentityMatches())
        {
            Reset(clearCreation: false);
            farmId = Game1.uniqueIDForThisGame.ToString(CultureInfo.InvariantCulture);
            farmerId = Game1.player.UniqueMultiplayerID.ToString(CultureInfo.InvariantCulture);
            cache = new PlayerPortraitCache(Path.Combine(helper.DirectoryPath, "data", "player-portraits"), farmId, farmerId);
            try { record = cache.ReadRecord(); }
            catch { record = null; }
            if (record == null)
            {
                try { record = helper.Data.ReadSaveData<PlayerPortraitRecord>(SaveKey); }
                catch { record = null; }
            }
            if (record?.Version != 1 || record.FarmId != farmId || record.FarmerId != farmerId) record = null;
            record ??= new PlayerPortraitRecord { FarmId = farmId, FarmerId = farmerId };
            if (record.Sha256 != null)
            {
                try { portrait = Decode(cache.ReadImage(record.Sha256)); record.Status = "Ready"; }
                catch { record.Status = "Failed"; }
            }
            else if (record.Status == "Generating") record.Status = "Failed"; // Unknown old request outcome; never retry automatically.
        }
        if (!captureRecovery.Attempted)
        {
            if (!CaptureReference(explicitRetry: false) && portrait == null) Fail(new PortraitFailureException("capture_failed"), "capture");
        }
        if (pending?.IsCompleted == true)
        {
            var completed = pending; pending = null; cancellation?.Dispose(); cancellation = null;
            if (!sessions.IsCurrent(requestSession) || !CurrentIdentityMatches()) { _ = completed.Exception; return; }
            string phase = "request";
            try
            {
                byte[] bytes = completed.GetAwaiter().GetResult();
                phase = "decode";
                using var candidate = Decode(bytes);
                // Normalize through the native decoder, then verify the exact stored bytes on reload.
                using var normalized = new MemoryStream(); candidate.SaveAsPng(normalized, candidate.Width, candidate.Height);
                Texture2D? ready = null;
                phase = "cache";
                try
                {
                    cache!.CommitImage(normalized.ToArray(), record!, updated =>
                    {
                        ready = Decode(cache.ReadImage(updated.Sha256!));
                        updated.Status = "Ready"; updated.FailureCategory = null; Persist();
                    });
                    portrait?.Dispose(); portrait = ready; ready = null; Changed();
                }
                finally { ready?.Dispose(); }
            }
            catch (Exception ex) { Fail(ex, phase); }
        }
        if (creationPending)
        {
            creationPending = false;
            if (record?.Status != "Ready" && enabled()) StartRequest(explicitRetry: false);
        }
    }

    private bool StartRequest(bool explicitRetry)
    {
        if (!CurrentIdentityMatches() || cache == null || record == null || pending != null || reference == null) return false;
        string? key = ResolveKey();
        if (string.IsNullOrWhiteSpace(key)) { Fail(new PortraitFailureException("credential_missing"), "request"); return false; }
        if (!cache.TryClaim(explicitRetry)) { record.Status = "Failed"; Persist(); return false; }
        record.Status = "Generating"; record.FailureCategory = null; record.AppearanceHash = PlayerPortraitCache.Hash(reference); Persist();
        cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(120)); requestSession = sessions.Current;
        // Generate receives only the avatar image and credential, never farm/player metadata.
        pending = generate(key, reference, cancellation.Token); Changed(); return true;
    }

    private bool CaptureReference(bool explicitRetry) => captureRecovery.TryCapture(() =>
    {
        var captured = PlayerPortraitCapture.Capture(Game1.player);
        reference = captured.Reference; fallback?.Dispose(); fallback = captured.Fallback; return true;
    }, explicitRetry);

    private static string? ResolveKey()
    {
        string? key = Environment.GetEnvironmentVariable("GEMINI_API_KEY", EnvironmentVariableTarget.User);
        if (string.IsNullOrWhiteSpace(key)) key = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (string.IsNullOrWhiteSpace(key)) key = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
        return key;
    }

    private static Texture2D Decode(byte[] bytes)
    {
        var (width, height) = PortraitImageHeader.Read(bytes);
        using var stream = new MemoryStream(bytes); Texture2D texture;
        try { texture = Texture2D.FromStream(Game1.graphics.GraphicsDevice, stream); }
        catch { throw new PortraitFailureException("native_decode_failed"); }
        try
        {
            if (texture.Width != width || texture.Height != height) throw new PortraitFailureException("decoded_dimensions_mismatch");
            var pixels = new Color[width * height]; texture.GetData(pixels);
            var visible = pixels.Where(p => p.A > 16).Take(100).ToArray();
            if (visible.Length < 100 || pixels.All(p => p == pixels[0])) throw new PortraitFailureException("blank_image");
            return texture;
        }
        catch { texture.Dispose(); throw; }
    }

    private void Persist()
    {
        if (record == null || cache == null || !CurrentIdentityMatches()) return;
        cache.WriteRecord(record);
        helper.Data.WriteSaveData(SaveKey, record);
    }
    private void Changed()
    {
        if (farmerId == null) return;
        try { PortraitChanged?.Invoke(long.Parse(farmerId, CultureInfo.InvariantCulture)); }
        catch { monitor.Log("A portrait display could not refresh.", LogLevel.Warn); }
    }
    private void Fail(Exception? error = null, string phase = "service")
    {
        string category = PortraitImageHeader.FailureCategory(error, phase);
        if (record != null) { record.Status = "Failed"; record.FailureCategory = category; }
        try { Persist(); } catch { /* Preserve fallback even when storage is unavailable. */ }
        monitor.Log($"Player portrait unavailable ({category}); keeping the existing avatar. Use portrait retry to try again.", LogLevel.Warn);
        Changed();
    }
    private void Reset(bool clearCreation)
    {
        sessions.Reset();
        cancellation?.Cancel(); cancellation?.Dispose(); cancellation = null;
        if (pending != null) _ = pending.ContinueWith(t => { _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
        pending = null; portrait?.Dispose(); fallback?.Dispose(); portrait = fallback = null; reference = null;
        cache = null; record = null; farmId = farmerId = null; captureRecovery = new();
        if (clearCreation) creationPending = false;
    }
}
