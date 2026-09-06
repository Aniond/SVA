using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NpcArtAudit;

internal static class ElliottBeachAudit
{
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        var oldSeason = Game1.season;
        var oldLocation = Game1.currentLocation;
        try
        {
            Game1.season = Season.Summer;
            var location = new GameLocation("Maps/Town", "Town");
            Game1.currentLocation = location;
            var npc = new NPC(new AnimatedSprite("Characters/Elliott", 0, 16, 32), Vector2.Zero, 2, "Elliott") { currentLocation = location };
            npc.wearIslandAttire();
            if ((npc.Sprite.overrideTextureName ?? npc.Sprite.textureName.Value).Replace('\\', '/') != "Characters/Elliott_Beach")
                throw new Exception("Native beach appearance not selected");

            void Equal(Texture2D actual, Texture2D expected)
            {
                if (actual.Width != expected.Width || actual.Height != expected.Height) throw new Exception("Texture dimensions differ");
                var a = new Color[actual.Width * actual.Height];
                var b = new Color[expected.Width * expected.Height];
                actual.GetData(a); expected.GetData(b);
                if (!a.SequenceEqual(b)) throw new Exception("Selected texture pixels differ");
            }
            Equal(npc.Sprite.Texture, helper.GameContent.Load<Texture2D>("Characters/Elliott_Beach"));
            Equal(npc.Portrait, helper.GameContent.Load<Texture2D>("Portraits/Elliott_Beach"));
            var texture = npc.Sprite.Texture;
            var portrait = npc.Portrait;
            if (texture.Width != 64 || texture.Height != 160 || portrait.Width != 128 || portrait.Height != 320)
                throw new Exception("Elliott beach atlas dimensions differ from native layout");
            var spritePixels = new Color[texture.Width * texture.Height]; texture.GetData(spritePixels);
            var portraitPixels = new Color[portrait.Width * portrait.Height]; portrait.GetData(portraitPixels);
            for (var frame = 0; frame < 20; frame++)
            {
                npc.Sprite.CurrentFrame = frame;
                var expected = new Rectangle(frame % 4 * 16, frame / 4 * 32, 16, 32);
                if (npc.Sprite.SourceRect != expected) throw new Exception("Native sprite rectangle mismatch: " + frame);
                var visible = 0;
                for (var y = expected.Y; y < expected.Bottom; y++)
                for (var x = expected.X; x < expected.Right; x++)
                    if (spritePixels[y * texture.Width + x].A > 0) visible++;
                if (visible == 0) throw new Exception("Empty beach pose: " + frame);
            }
            for (var index = 0; index < 10; index++)
            {
                if (new Dialogue(npc, null, "Beach expression check.$" + index).getPortraitIndex() != index)
                    throw new Exception("Portrait index mismatch: " + index);
                var visible = 0;
                for (var y = 0; y < 64; y++) for (var x = 0; x < 64; x++)
                    if (portraitPixels[(index / 2 * 64 + y) * portrait.Width + index % 2 * 64 + x].A > 0) visible++;
                if (visible == 0) throw new Exception("Empty beach portrait: " + index);
            }

            var rawAnimation = helper.GameContent.Load<Dictionary<string, string>>("Data/animationDescriptions")["elliott_beach_drink"];
            var sections = rawAnimation.Split('/').Take(3).Select(section => section.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray()).ToArray();
            var animationSteps = 0;
            foreach (var frames in sections)
            {
                if (frames.Length == 0 || frames.Any(frame => frame < 16 || frame > 19)) throw new Exception("Native beach drink references unexpected poses");
                npc.Sprite.loop = true;
                npc.Sprite.setCurrentAnimation(frames.Select(frame => new FarmerSprite.AnimationFrame(frame, 150)).ToList());
                for (var step = 1; step <= frames.Length * 2; step++)
                {
                    npc.Sprite.animateOnce(new GameTime(TimeSpan.FromMilliseconds(step * 150), TimeSpan.FromMilliseconds(150)));
                    if (npc.Sprite.CurrentFrame != frames[step % frames.Length]) throw new Exception("Timed native drink frame mismatch");
                    animationSteps++;
                }
            }

            // Exercise the actual route-end entry and its intro-to-loop callback, not only a copied frame list.
            var routeNpc = new NPC(new AnimatedSprite("Characters/Elliott", 0, 16, 32), Vector2.Zero, 2, "Elliott") { currentLocation = location };
            routeNpc.wearIslandAttire();
            var behavior = helper.Reflection.GetMethod(routeNpc, "getRouteEndBehaviorFunction").Invoke<Delegate>("elliott_beach_drink", null);
            if (behavior == null) throw new Exception("Native beach drinking behavior missing");
            behavior.DynamicInvoke(routeNpc, location);
            if (!routeNpc.Sprite.currentAnimation.Select(frame => frame.frame).SequenceEqual(sections[0])) throw new Exception("Native drink intro differs");
            if (routeNpc.layingDown || routeNpc.HideShadow || routeNpc.drawOffset != Vector2.Zero) throw new Exception("Drinking acquired incorrect reclining flags");
            routeNpc.Sprite.animateOnce(new GameTime(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100)));
            if (!routeNpc.Sprite.currentAnimation.Select(frame => frame.frame).SequenceEqual(sections[1])) throw new Exception("Native drink intro did not enter its loop");

            var walkingSteps = 0;
            var walker = new AnimatedSprite("Characters/Elliott_Beach", 0, 16, 32);
            for (var direction = 0; direction < 4; direction++)
            {
                walker.CurrentFrame = direction * 4; walker.timer = 0;
                for (var step = 1; step <= 8; step++)
                {
                    var time = new GameTime(TimeSpan.FromMilliseconds(step * 180), TimeSpan.FromMilliseconds(180));
                    switch (direction)
                    {
                        case 0: walker.AnimateDown(time); break;
                        case 1: walker.AnimateRight(time); break;
                        case 2: walker.AnimateUp(time); break;
                        case 3: walker.AnimateLeft(time); break;
                    }
                    var frame = direction * 4 + step % 4;
                    if (walker.CurrentFrame != frame || walker.SourceRect != new Rectangle(frame % 4 * 16, frame / 4 * 32, 16, 32))
                        throw new Exception("Native beach walk mismatch");
                    walkingSteps++;
                }
            }

            var device = Game1.graphics.GraphicsDevice;
            var previous = device.GetRenderTargets();
            using var target = new RenderTarget2D(device, 1024, 1024);
            using var batch = new SpriteBatch(device);
            try
            {
                device.SetRenderTarget(target); device.Clear(new Color(35, 45, 58));
                batch.Begin(samplerState: SamplerState.PointClamp);
                for (var i = 0; i < 20; i++)
                    batch.Draw(texture, new Rectangle(i % 8 * 64 + 8, i / 8 * 132 + 8, 48, 96), new Rectangle(i % 4 * 16, i / 4 * 32, 16, 32), Color.White);
                batch.Draw(portrait, new Rectangle(600, 16, 384, 960), Color.White);
                batch.End(); device.SetRenderTargets(previous);
                using var file = File.Create(Path.Combine(helper.DirectoryPath, "elliott-beach-runtime-preview.png"));
                target.SaveAsPng(file, target.Width, target.Height);
            }
            finally { device.SetRenderTargets(previous); }

            npc.wearNormalClothes();
            Equal(npc.Sprite.Texture, helper.GameContent.Load<Texture2D>("Characters/Elliott"));
            Equal(npc.Portrait, helper.GameContent.Load<Texture2D>("Portraits/Elliott"));
            helper.Data.WriteJsonFile("elliott-beach-checks.json", new
            {
                Passed = true, OccupiedFrames = 20, BlankFrames = 0, PortraitSlots = 10,
                NativeBeachSelected = true, NormalOutfitRestored = true,
                NativeDrinkSteps = animationSteps, NativeWalkingSteps = walkingSteps,
                NativeDrinkRouteIntroToLoop = true, NativeAnimation = rawAnimation,
                FarmLoaded = false, FullIslandScenePlayback = false
            });
            monitor.Log("Elliott beach audit passed: 20 poses, 10 portraits, native drink route intro-to-loop and outfit restoration; full island scene remains unverified.", LogLevel.Info);
        }
        catch (Exception ex)
        {
            helper.Data.WriteJsonFile("elliott-beach-checks.json", new { Passed = false, Error = ex.ToString() });
            monitor.Log("Elliott beach audit failed: " + ex, LogLevel.Error);
        }
        finally { Game1.season = oldSeason; Game1.currentLocation = oldLocation; }
    }
}
