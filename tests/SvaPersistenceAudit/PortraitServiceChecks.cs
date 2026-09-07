using System.Security.Cryptography;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SolaceWeather.Core;
using StardewValley;
using StardewValley.Menus;

namespace SvaPersistenceAudit;

public sealed partial class ModEntry
{
    private object PortraitService() => Member(Mod("David.SolaceWeather"), "playerPortraits")
        ?? throw new InvalidOperationException("Live player portrait service is missing.");

    private void PortraitStatus()
    {
        RequireOwnedWorld();
        object service = PortraitService();
        long id = Game1.player.UniqueMultiplayerID;
        string status = (string)service.GetType().GetMethod("GetStatus")!.Invoke(service, new object[] { id })!;
        object?[] lookup = { id, null, null };
        bool available = (bool)service.GetType().GetMethod("TryGetPortrait")!.Invoke(service, lookup)!;
        var texture = lookup[1] as Texture2D;
        Rectangle source = lookup[2] is Rectangle rectangle ? rectangle : Rectangle.Empty;
        var cache = Member(service, "cache") as PlayerPortraitCache;
        var record = Member(service, "record") as PlayerPortraitRecord;
        bool cachePresent = cache != null && File.Exists(cache.ImagePath);
        string? actualHash = cachePresent ? Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(cache!.ImagePath))).ToLowerInvariant() : null;
        var pending = Member(service, "pending") as Task;
        var result = new
        {
            Status = status, FarmId = Game1.uniqueIDForThisGame, FarmerId = id,
            TextureAvailable = available, Width = texture?.Width, Height = texture?.Height,
            Source = new { source.X, source.Y, source.Width, source.Height },
            SourceInBounds = texture != null && texture.Bounds.Contains(source) && source.Width > 0 && source.Height > 0,
            IsFallback = ReferenceEquals(texture, Member(service, "fallback")),
            CachePresent = cachePresent, CacheHashMatches = actualHash != null && actualHash == record?.Sha256,
            Sha256 = actualHash, SavedIdentityMatches = record?.FarmId == Game1.uniqueIDForThisGame.ToString(System.Globalization.CultureInfo.InvariantCulture)
                && record?.FarmerId == id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            RequestPending = pending != null, RequestCompleted = pending?.IsCompleted,
            CreationPending = Member(service, "creationPending"), Model = record?.Model, FailureCategory = record?.FailureCategory,
            ReadyVerified = status == "Ready" && available && actualHash != null && actualHash == record?.Sha256
                && texture != null && source == texture.Bounds && pending == null
        };
        WriteProfile("portrait-service-status.json", result);
        if (available && texture != null)
        {
            using var image = File.Create(SafePath("portrait-service-visible.png"));
            texture.SaveAsPng(image, texture.Width, texture.Height);
        }
    }

    /// <summary>Invoke only when a paid retry is explicitly authorized; never called from a tick or status check.</summary>
    private void PortraitRetry()
    {
        RequireOwnedWorld(); object service = PortraitService();
        if (Member(service, "pending") != null) throw new InvalidOperationException("A portrait request is already pending.");
        // One manual retry per disposable profile, even if the command is accidentally repeated.
        using (var marker = new FileStream(SafePath("portrait-retry-invoked"), FileMode.CreateNew)) { }
        bool started = (bool)service.GetType().GetMethod("Retry")!.Invoke(service, new object[] { Game1.player.UniqueMultiplayerID })!;
        WriteProfile("portrait-retry-status.json", new { Started = started, At = DateTime.UtcNow, ExplicitCommand = true });
    }

    /// <summary>Reconfirm a Ready portrait only, proving duplicate creation notifications cannot start another request.</summary>
    private void PortraitReuseCheck()
    {
        RequireOwnedWorld(); object service = PortraitService();
        string status = (string)service.GetType().GetMethod("GetStatus")!.Invoke(service, new object[] { Game1.player.UniqueMultiplayerID })!;
        if (status != "Ready" || Member(service, "pending") != null) throw new InvalidOperationException("Reuse check requires a completed Ready portrait.");
        service.GetType().GetMethod("MarkCreationConfirmed")!.Invoke(service, null);
        WriteProfile("portrait-reuse-requested.json", new { ReadyBefore = true, ExpectedNewRequests = 0, NextStep = "Run portrait-status after one update; ReadyVerified must be true and RequestPending false." });
    }

    private void PortraitChat()
    {
        RequireOwnedWorld();
        object conversation = Member(Mod("David.SolaceWeather"), "abigailConversation")!;
        if (Member(conversation, "pending") != null) throw new InvalidOperationException("Conversation request is already pending.");
        Game1.exitActiveMenu();
        conversation.GetType().GetMethod("Start", Members)!.Invoke(conversation, new object[] { Game1.getCharacterFromName("Abigail") });
        if (Game1.activeClickableMenu is not NamingMenu) throw new InvalidOperationException("Real conversation input did not open.");
        // Do not submit the input: this view/capture never sends a Gemini text request.
        var device = Game1.graphics.GraphicsDevice;
        var targets = device.GetRenderTargets(); var viewport = device.Viewport; var scissor = device.ScissorRectangle;
        var blend = device.BlendState; var depth = device.DepthStencilState; var raster = device.RasterizerState; var sampler = device.SamplerStates[0];
        var previousBatch = Game1.spriteBatch;
        try
        {
            using var target = new RenderTarget2D(device, Game1.uiViewport.Width, Game1.uiViewport.Height);
            using var batch = new SpriteBatch(device);
            Game1.spriteBatch = batch;
            device.SetRenderTarget(target); device.Clear(new Color(30, 40, 52));
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
            Game1.activeClickableMenu.draw(batch); batch.End(); device.SetRenderTargets(targets);
            using var file = File.Create(SafePath("portrait-chat.png")); target.SaveAsPng(file, target.Width, target.Height);
        }
        finally
        {
            device.SetRenderTargets(targets); device.Viewport = viewport; device.ScissorRectangle = scissor;
            device.BlendState = blend; device.DepthStencilState = depth; device.RasterizerState = raster; device.SamplerStates[0] = sampler;
            Game1.spriteBatch = previousBatch;
        }
        WriteProfile("portrait-chat-status.json", new { RealInputOpen = true, TextRequestStarted = Member(conversation, "pending") != null, Screenshot = "portrait-chat.png" });
        PortraitStatus();
    }
}
