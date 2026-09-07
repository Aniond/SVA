using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace SvaPersistenceAudit;

public sealed partial class ModEntry
{
    private void CapturePortraitReference()
    {
        RequireOwnedWorld();
        var device = Game1.graphics.GraphicsDevice;
        var targets = device.GetRenderTargets(); var viewport = device.Viewport; var scissor = device.ScissorRectangle;
        var blend = device.BlendState; var depth = device.DepthStencilState; var raster = device.RasterizerState; var sampler = device.SamplerStates[0];
        bool ui = FarmerRenderer.isDrawingForUI;
        var live = Game1.player;
        var before = new { live.Position, Facing = live.FacingDirection, Frame = live.FarmerSprite.CurrentFrame, Hair = live.hair.Value, Skin = live.skin.Value, Shirt = live.GetShirtId(), Pants = live.GetPantsId() };
        Farmer clone = live.CreateFakeEventFarmer();
        try
        {
            clone.faceDirection(2); clone.FarmerSprite.setCurrentFrame(0); clone.swimming.Value = false;
            FarmerRenderer.isDrawingForUI = true;
            using var target = new RenderTarget2D(device, 128, 192);
            using var batch = new SpriteBatch(device);
            device.SetRenderTarget(target); device.Clear(Color.Transparent);
            batch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
            clone.FarmerRenderer.draw(batch, clone.FarmerSprite.CurrentAnimationFrame, clone.FarmerSprite.CurrentFrame,
                clone.FarmerSprite.SourceRect, new Vector2(32, 32), Vector2.Zero, .8f, 2, Color.White, 0, 1, clone);
            batch.End();
            device.SetRenderTargets(targets);
            var pixels = new Color[128 * 192]; target.GetData(pixels);
            int visible = pixels.Count(p => p.A > 0);
            if (visible < 100) throw new InvalidOperationException("Farmer reference rendered no usable body.");
            using (var stream = File.Create(SafePath("player-portrait-reference.png"))) target.SaveAsPng(stream, 128, 192);
            var after = new { live.Position, Facing = live.FacingDirection, Frame = live.FarmerSprite.CurrentFrame, Hair = live.hair.Value, Skin = live.skin.Value, Shirt = live.GetShirtId(), Pants = live.GetPantsId() };
            if (!before.Equals(after)) throw new InvalidOperationException("Portrait capture mutated farmer state.");
            WriteProfile("player-portrait-reference.json", new { Passed = true, FarmId = Game1.uniqueIDForThisGame, FarmerId = live.UniqueMultiplayerID,
                Reference = "player-portrait-reference.png", VisiblePixels = visible, NativeComposedRenderer = true, LiveAppearanceUnchanged = true,
                Appearance = new { Body = live.IsMale ? "male" : "female", Hair = live.hair.Value, Skin = live.skin.Value,
                    HairColor = live.hairstyleColor.Value.ToString(), EyeColor = live.newEyeColor.Value.ToString(),
                    Shirt = live.GetShirtId(), Pants = live.GetPantsId(), PantsColor = live.GetPantsColor().ToString(), Accessory = live.accessory.Value, Hat = live.hat.Value?.QualifiedItemId },
                Note = "Only composed avatar reference and bounded appearance description may be sent; identity metadata stays local." });
        }
        finally
        {
            device.SetRenderTargets(targets); device.Viewport = viewport; device.ScissorRectangle = scissor;
            device.BlendState = blend; device.DepthStencilState = depth; device.RasterizerState = raster; device.SamplerStates[0] = sampler;
            FarmerRenderer.isDrawingForUI = ui;
        }
    }
}
