using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NpcArtAudit;

internal static class AbigailBaseAudit
{
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        var oldSeason = Game1.season;
        var oldLocation = Game1.currentLocation;
        try
        {
            Game1.season = Season.Spring;
            var location = new GameLocation("Maps/Town", "Town");
            Game1.currentLocation = location;
            var npc = new NPC(new AnimatedSprite("Characters/Abigail", 0, 16, 32), Vector2.Zero, 2, "Abigail") { currentLocation = location };
            npc.ChooseAppearance();
            var texture = npc.Sprite.Texture;
            if (texture.Width != 64 || texture.Height != 448) throw new Exception("Unexpected base sprite size.");
            var pixels = new Color[texture.Width * texture.Height];
            texture.GetData(pixels);
            var packaged = new Color[pixels.Length];
            helper.GameContent.Load<Texture2D>("Characters/Abigail").GetData(packaged);
            if (!pixels.SequenceEqual(packaged)) throw new Exception("Selected base appearance differs.");
            for (var frame = 0; frame < 56; frame++)
            {
                npc.Sprite.CurrentFrame = frame;
                var rect = npc.Sprite.SourceRect;
                if (rect != new Rectangle(frame % 4 * 16, frame / 4 * 32, 16, 32)) throw new Exception("Frame layout mismatch.");
                var occupied = false;
                for (var y = rect.Y; y < rect.Bottom; y++)
                    for (var x = rect.X; x < rect.Right; x++) occupied |= pixels[y * texture.Width + x].A > 0;
                if (occupied != (frame < 54)) throw new Exception("Occupied/blank frame mismatch.");
            }
            for (var i = 0; i < 10; i++)
                if (new Dialogue(npc, null, "Base portrait check.$" + i).getPortraitIndex() != i) throw new Exception("Portrait slot mismatch.");

            // Exercise the game's own timed walking methods for all three outfits.
            var walkingSteps = 0;
            foreach (var asset in new[] { "Characters/Abigail", "Characters/Abigail_Winter", "Characters/Abigail_Beach" })
            {
                var sprite = new AnimatedSprite(asset, 0, 16, 32);
                for (var direction = 0; direction < 4; direction++)
                {
                    sprite.CurrentFrame = direction * 4;
                    sprite.timer = 0;
                    for (var step = 1; step <= 8; step++)
                    {
                        var time = new GameTime(TimeSpan.FromMilliseconds(step * 180), TimeSpan.FromMilliseconds(180));
                        switch (direction)
                        {
                            case 0: sprite.AnimateDown(time); break;
                            case 1: sprite.AnimateRight(time); break;
                            case 2: sprite.AnimateUp(time); break;
                            case 3: sprite.AnimateLeft(time); break;
                        }
                        var expected = direction * 4 + step % 4;
                        if (sprite.CurrentFrame != expected || sprite.SourceRect != new Rectangle(expected % 4 * 16, expected / 4 * 32, 16, 32))
                            throw new Exception("Timed walking frame mismatch: " + asset);
                        walkingSteps++;
                    }
                }
            }

            var device = Game1.graphics.GraphicsDevice;
            var previous = device.GetRenderTargets();
            using var target = new RenderTarget2D(device, 1024, 1024);
            using var batch = new SpriteBatch(device);
            try
            {
                device.SetRenderTarget(target);
                device.Clear(new Color(35, 45, 58));
                batch.Begin(samplerState: SamplerState.PointClamp);
                for (var frame = 0; frame < 56; frame++)
                    batch.Draw(texture, new Rectangle(frame % 8 * 64 + 8, frame / 8 * 132 + 8, 48, 96), new Rectangle(frame % 4 * 16, frame / 4 * 32, 16, 32), Color.White);
                batch.Draw(npc.Portrait, new Rectangle(600, 16, 384, 960), Color.White);
                batch.End();
                device.SetRenderTargets(previous);
                using var file = File.Create(Path.Combine(helper.DirectoryPath, "abigail-base-runtime-preview.png"));
                target.SaveAsPng(file, 1024, 1024);
            }
            finally { device.SetRenderTargets(previous); }
            helper.Data.WriteJsonFile("abigail-base-checks.json", new { Passed = true, OccupiedFrames = 54, BlankFrames = 2, PortraitSlots = 10, NativeWalkingSteps = walkingSteps, Outfits = 3, FarmLoaded = false, FullEventPlayback = false });
            monitor.Log("Abigail base audit passed: 54 poses, 10 portrait slots, 96 native walking steps across base/winter/beach. Full event playback remains unverified.", LogLevel.Info);
        }
        catch (Exception ex)
        {
            helper.Data.WriteJsonFile("abigail-base-checks.json", new { Passed = false, Error = ex.ToString() });
            monitor.Log("Abigail base audit failed: " + ex, LogLevel.Error);
        }
        finally { Game1.season = oldSeason; Game1.currentLocation = oldLocation; }
    }
}
