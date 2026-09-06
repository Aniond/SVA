using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace NpcArtAudit;

internal static class CrowShopCapture
{
    // Invoke from UpdateTicked after portrait checks, before native close.
    // Construct the fixture shop with uiViewport=1920x1080 so its native layout
    // has space for the portrait. Do not resize an existing real player's shop.
    public static void Save(IModHelper helper, ShopMenu shop)
    {
        if (shop.ShopId != "LostItems" || shop.portraitTexture == null)
            throw new InvalidOperationException("Capture requires the native Crow shop and portrait.");
        if (shop.xPositionOnScreen <= 320)
            throw new InvalidOperationException("Create the fixture shop at 1920x1080 before capturing its portrait.");

        var uiViewport = Game1.uiViewport;
        bool merchantPortraits = Game1.options.showMerchantPortraits;
        var device = Game1.graphics.GraphicsDevice;
        var targets = device.GetRenderTargets();
        var viewport = device.Viewport;
        var scissor = device.ScissorRectangle;
        var blend = device.BlendState;
        var depth = device.DepthStencilState;
        var raster = device.RasterizerState;
        var sampler = device.SamplerStates[0];
        var texture = device.Textures[0];
        var indices = device.Indices;
        
        using var target = new RenderTarget2D(device, 1920, 1080);
        using var batch = new SpriteBatch(device);
        bool begun = false;
        try
        {
            Game1.uiViewport = new xTile.Dimensions.Rectangle(0, 0, 1920, 1080);
            Game1.options.showMerchantPortraits = true;
            device.SetRenderTarget(target);
            device.Clear(new Color(35, 45, 58));
            batch.Begin(samplerState: SamplerState.PointClamp);
            begun = true;
            shop.draw(batch);
            batch.End();
            begun = false;
            device.SetRenderTargets(targets);
            using var stream = File.Create(Path.Combine(helper.DirectoryPath, "crow-native-shop-preview.png"));
            target.SaveAsPng(stream, target.Width, target.Height);
        }
        finally
        {
            try { if (begun) batch.End(); }
            finally
            {
                device.SetRenderTargets(targets);
                device.Viewport = viewport;
                device.ScissorRectangle = scissor;
                device.BlendState = blend;
                device.DepthStencilState = depth;
                device.RasterizerState = raster;
                device.SamplerStates[0] = sampler;
                device.Textures[0] = texture;
                device.Indices = indices;
                
                Game1.uiViewport = uiViewport;
                Game1.options.showMerchantPortraits = merchantPortraits;
            }
        }
    }
}
