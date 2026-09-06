using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NpcArtAudit;

internal static class JunimoRepairsAudit
{
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        try
        {
            const string asset = "LooseSprites/Cursors";
            var texture = helper.GameContent.Load<Texture2D>(asset);
            var pixels = new Color[texture.Width * texture.Height];
            texture.GetData(pixels);
            for (var frame = 0; frame < 4; frame++)
            {
                var occupied = 0;
                for (var y = 0; y < 16; y++) for (var x = 0; x < 16; x++)
                {
                    var pixel = pixels[(1432 + y) * texture.Width + 294 + frame * 16 + x];
                    if (pixel.A != 0 && pixel.A != 255) throw new Exception("Repair Junimo alpha is not binary");
                    if (pixel.A != 0) occupied++;
                }
                if (occupied == 0) throw new Exception("Empty repair Junimo frame");
            }
            var sprite = new TemporaryAnimatedSprite(asset, new Rectangle(294,1432,16,16), Vector2.Zero, false, 0f, Color.White)
                { animationLength = 4, totalNumberOfLoops = 99, interval = 300f, scale = 4f };
            for (var step = 1; step <= 16; step++)
            {
                sprite.update(new GameTime(TimeSpan.FromMilliseconds(step * 301), TimeSpan.FromMilliseconds(301)));
                if (sprite.sourceRect != new Rectangle(294 + step % 4 * 16,1432,16,16)) throw new Exception("Repair animation transition differs");
            }
            helper.Data.WriteJsonFile("junimo-repairs-checks.json", new { Passed = true, OccupiedFrames = 4, NativeAnimationSteps = 16, IntervalMilliseconds = 300, FullWorldChangeEventTriggered = false, PortraitRequired = false });
        }
        catch (Exception ex)
        {
            helper.Data.WriteJsonFile("junimo-repairs-checks.json", new { Passed = false, Error = ex.ToString() });
            monitor.Log("Repair Junimo audit failed: " + ex, LogLevel.Error);
        }
    }
}
