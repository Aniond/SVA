using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NpcArtAudit;

internal static class SamBeachAudit
{
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        var oldSeason = Game1.season; var oldLocation = Game1.currentLocation;
        try
        {
            Game1.season = Season.Summer;
            var location = new GameLocation("Maps/Town", "Town"); Game1.currentLocation = location;
            var npc = new NPC(new AnimatedSprite("Characters/Sam", 0, 16, 32), Vector2.Zero, 2, "Sam") { currentLocation = location };
            npc.wearIslandAttire();
            if ((npc.Sprite.overrideTextureName ?? npc.Sprite.textureName.Value).Replace('\\', '/') != "Characters/Sam_Beach") throw new Exception("Native beach appearance missing");
            void Equal(Texture2D a, Texture2D b)
            {
                if (a.Width != b.Width || a.Height != b.Height) throw new Exception("Selected texture dimensions differ");
                var pa = new Color[a.Width * a.Height]; var pb = new Color[b.Width * b.Height]; a.GetData(pa); b.GetData(pb);
                if (!pa.SequenceEqual(pb)) throw new Exception("Selected texture pixels differ");
            }
            Equal(npc.Sprite.Texture, helper.GameContent.Load<Texture2D>("Characters/Sam_Beach"));
            Equal(npc.Portrait, helper.GameContent.Load<Texture2D>("Portraits/Sam_Beach"));
            var texture = npc.Sprite.Texture; var portrait = npc.Portrait;
            if (texture.Width != 64 || texture.Height != 192 || portrait.Width != 128 || portrait.Height != 384) throw new Exception("Native beach atlas dimensions differ");
            var pixels = new Color[64 * 192]; texture.GetData(pixels);
            var portraits = new Color[128 * 384]; portrait.GetData(portraits);
            for (var frame = 0; frame < 21; frame++)
            {
                npc.Sprite.CurrentFrame = frame;
                var rect = new Rectangle(frame % 4 * 16, frame / 4 * 32, 16, 32);
                if (npc.Sprite.SourceRect != rect) throw new Exception("Native frame rectangle differs: " + frame);
                var visible = 0;
                for (var y = rect.Y; y < rect.Bottom; y++) for (var x = rect.X; x < rect.Right; x++) if (pixels[y * 64 + x].A > 0) visible++;
                if (visible == 0) throw new Exception("Empty walking pose: " + frame);
            }
            for (var index = 0; index < 12; index++)
            {
                if (new Dialogue(npc, null, "Beach expression.$" + index).getPortraitIndex() != index) throw new Exception("Portrait index differs");
                var colors = new HashSet<Color>(); var transparent = 0;
                for (var y = 0; y < 64; y++) for (var x = 0; x < 64; x++)
                {
                    var color = portraits[(index / 2 * 64 + y) * 128 + index % 2 * 64 + x];
                    if (color.A == 0) transparent++; else colors.Add(color);
                }
                if (colors.Count < 8 || transparent == 0) throw new Exception("Empty or solid placeholder portrait: " + index);
            }
            var walkingSteps = 0;
            for (var direction = 0; direction < 4; direction++)
            {
                npc.Sprite.CurrentFrame = direction * 4; npc.Sprite.timer = 0;
                for (var step = 1; step <= 8; step++)
                {
                    var time = new GameTime(TimeSpan.FromMilliseconds(step * 180), TimeSpan.FromMilliseconds(180));
                    switch (direction) { case 0: npc.Sprite.AnimateDown(time); break; case 1: npc.Sprite.AnimateRight(time); break; case 2: npc.Sprite.AnimateUp(time); break; case 3: npc.Sprite.AnimateLeft(time); break; }
                    var expected = direction * 4 + step % 4;
                    if (npc.Sprite.CurrentFrame != expected || npc.Sprite.SourceRect != new Rectangle(expected % 4 * 16, expected / 4 * 32, 16, 32)) throw new Exception("Native beach walk differs");
                    walkingSteps++;
                }
            }
            for (var frame = 21; frame < 24; frame++)
                for (var y = 0; y < 32; y++) for (var x = 0; x < 16; x++)
                    if (pixels[(frame / 4 * 32 + y) * 64 + frame % 4 * 16 + x].A != 0) throw new Exception("Native blank cell is occupied: " + frame);
            var description = helper.GameContent.Load<Dictionary<string, string>>("Data/animationDescriptions")["sam_beach_towel"];
            var sections = description.Split('/').Take(3).Select(section => section.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray()).ToArray();
            if (!sections[0].SequenceEqual(new[] {16}) || !sections[1].SequenceEqual(new[] {17,17,18,18,19,19,20,20,20,20}) || !sections[2].SequenceEqual(new[] {16})) throw new Exception("Native beach guitar sequence changed");
            var guitarSteps = 0;
            foreach (var frames in sections)
            {
                npc.Sprite.loop = true;
                npc.Sprite.setCurrentAnimation(frames.Select(frame => new FarmerSprite.AnimationFrame(frame, 150)).ToList());
                for (var step = 1; step <= frames.Length * 2; step++)
                {
                    npc.Sprite.animateOnce(new GameTime(TimeSpan.FromMilliseconds(step * 150), TimeSpan.FromMilliseconds(150)));
                    var expected = frames[step % frames.Length];
                    if (npc.Sprite.CurrentFrame != expected || npc.Sprite.SourceRect != new Rectangle(expected % 4 * 16, expected / 4 * 32, 16, 32)) throw new Exception("Native beach guitar animation differs");
                    guitarSteps++;
                }
            }
            var routeNpc = new NPC(new AnimatedSprite("Characters/Sam", 0, 16, 32), Vector2.Zero, 2, "Sam") { currentLocation = location };
            routeNpc.wearIslandAttire();
            var behavior = helper.Reflection.GetMethod(routeNpc, "getRouteEndBehaviorFunction").Invoke<Delegate>("sam_beach_towel", null);
            if (behavior == null) throw new Exception("Native beach guitar route missing");
            behavior.DynamicInvoke(routeNpc, location);
            if (!routeNpc.Sprite.currentAnimation.Select(frame => frame.frame).SequenceEqual(sections[0])) throw new Exception("Native guitar route intro differs");
            routeNpc.Sprite.animateOnce(new GameTime(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100)));
            if (!routeNpc.Sprite.currentAnimation.Select(frame => frame.frame).SequenceEqual(sections[1])) throw new Exception("Native guitar route intro-to-loop failed");
            if (routeNpc.layingDown || routeNpc.HideShadow || routeNpc.isSleeping.Value || routeNpc.drawOffset != Vector2.Zero) throw new Exception("Native beach guitar pose flags differ");
            var device = Game1.graphics.GraphicsDevice; var previous = device.GetRenderTargets();
            using var target = new RenderTarget2D(device, 1024, 1216); using var batch = new SpriteBatch(device);
            try
            {
                device.SetRenderTarget(target); device.Clear(new Color(35, 45, 58)); batch.Begin(samplerState: SamplerState.PointClamp);
                for (var i = 0; i < 21; i++) batch.Draw(texture, new Rectangle(i % 4 * 128 + 16, i / 4 * 160 + 16, 64, 128), new Rectangle(i % 4 * 16, i / 4 * 32, 16, 32), Color.White);
                batch.Draw(portrait, new Rectangle(600, 16, 384, 1152), Color.White); batch.End(); device.SetRenderTargets(previous);
                using var file = File.Create(Path.Combine(helper.DirectoryPath, "sam-beach-runtime-preview.png")); target.SaveAsPng(file, target.Width, target.Height);
            }
            finally { device.SetRenderTargets(previous); }
            npc.wearNormalClothes(); Equal(npc.Sprite.Texture, helper.GameContent.Load<Texture2D>("Characters/Sam")); Equal(npc.Portrait, helper.GameContent.Load<Texture2D>("Portraits/Sam"));
            helper.Data.WriteJsonFile("sam-beach-checks.json", new { Passed = true, OccupiedFrames = 21, PreservedBlankFrames = 3, PortraitSlots = 12, NativeBeachSelected = true, NormalOutfitRestored = true, NativeWalkingSteps = walkingSteps, NativeGuitarSteps = guitarSteps, NativeRouteIntroToLoopTransitions = 1, FarmLoaded = false, FullIslandScenePlayback = false });
            monitor.Log("Sam beach audit passed: 21 poses, three blank cells, 12 portraits, native guitar route and native outfit restoration; full island scene remains unverified.", LogLevel.Info);
        }
        catch (Exception ex) { helper.Data.WriteJsonFile("sam-beach-checks.json", new { Passed = false, Error = ex.ToString() }); monitor.Log("Sam beach audit failed: " + ex, LogLevel.Error); }
        finally { Game1.season = oldSeason; Game1.currentLocation = oldLocation; }
    }
}

