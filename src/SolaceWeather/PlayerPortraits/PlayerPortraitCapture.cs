using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace SolaceWeather.PlayerPortraits;

public static class PlayerPortraitCapture
{
    /// <summary>Call on the game thread outside an active SpriteBatch. The live farmer is never posed.</summary>
    public static (byte[] Reference, Texture2D Fallback) Capture(Farmer live)
    {
        var device = Game1.graphics.GraphicsDevice;
        var targets = device.GetRenderTargets(); var viewport = device.Viewport; var scissor = device.ScissorRectangle;
        var blend = device.BlendState; var depth = device.DepthStencilState; var raster = device.RasterizerState; var sampler = device.SamplerStates[0];
        bool ui = FarmerRenderer.isDrawingForUI;
        var previousBatch = Game1.spriteBatch;
        Farmer clone = live.CreateFakeEventFarmer();
        try
        {
            clone.faceDirection(2); clone.FarmerSprite.setCurrentFrame(0); clone.swimming.Value = false;
            FarmerRenderer.isDrawingForUI = true;
            using var target = new RenderTarget2D(device, 128, 192);
            using var batch = new SpriteBatch(device);
            Game1.spriteBatch = batch;
            device.SetRenderTarget(target); device.Clear(Color.Transparent);
            batch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
            clone.FarmerRenderer.draw(batch, clone.FarmerSprite.CurrentAnimationFrame, clone.FarmerSprite.CurrentFrame,
                clone.FarmerSprite.SourceRect, new Vector2(32, 32), Vector2.Zero, .8f, 2, Color.White, 0, 1, clone);
            batch.End(); device.SetRenderTargets(targets);
            var pixels = new Color[128 * 192]; target.GetData(pixels);
            if (pixels.Count(p => p.A > 0) < 100) throw new InvalidOperationException("No usable player portrait reference.");
            using var stream = new MemoryStream(); target.SaveAsPng(stream, 128, 192);
            var head = new Color[64 * 64];
            for (int y = 0; y < 64; y++) Array.Copy(pixels, (y + 32) * 128 + 32, head, y * 64, 64);
            var fallback = new Texture2D(device, 64, 64);
            try { fallback.SetData(head); return (stream.ToArray(), fallback); }
            catch { fallback.Dispose(); throw; }
        }
        finally
        {
            FarmerRenderer.isDrawingForUI = ui;
            Game1.spriteBatch = previousBatch;
            // The native clone creates its own temporary content manager and recolored GPU texture.
            // These are not the live farmer's renderer or globally shared clothing textures.
            try
            {
                var generated = typeof(FarmerRenderer).GetField("baseTexture", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.GetValue(clone.FarmerRenderer) as Texture2D;
                generated?.Dispose();
                clone.FarmerRenderer.unload();
            }
            catch { /* Graphics state restoration must still happen during device loss. */ }
            device.SetRenderTargets(targets); device.Viewport = viewport; device.ScissorRectangle = scissor;
            device.BlendState = blend; device.DepthStencilState = depth; device.RasterizerState = raster; device.SamplerStates[0] = sampler;
        }
    }
}
