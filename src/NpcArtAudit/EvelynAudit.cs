using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NpcArtAudit;

internal static class EvelynAudit
{
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        var oldSeason = Game1.season; var oldLocation = Game1.currentLocation;
        try
        {
            Color[] Pixels(Texture2D texture) { var data = new Color[texture.Width * texture.Height]; texture.GetData(data); return data; }
            void Equal(Texture2D actual, Texture2D expected)
            {
                if (actual.Width != expected.Width || actual.Height != expected.Height || !Pixels(actual).SequenceEqual(Pixels(expected))) throw new Exception("Selected Evelyn texture differs from registered artwork");
            }
            var walkingSteps = 0; var specialSteps = 0; var routeTransitions = 0; var showFrameCommands = 0;
            foreach (var winter in new[] { false, true })
            {
                Game1.season = winter ? Season.Winter : Season.Spring;
                var location = new GameLocation("Maps/Town", "Town"); Game1.currentLocation = location;
                NPC Create() => new(new AnimatedSprite("Characters/Evelyn", 0, 16, 32), Vector2.Zero, 2, "Evelyn") { currentLocation = location };
                var npc = Create(); npc.ChooseAppearance();
                var suffix = winter ? "_Winter" : "";
                var texture = helper.GameContent.Load<Texture2D>("Characters/Evelyn" + suffix); var portrait = helper.GameContent.Load<Texture2D>("Portraits/Evelyn" + suffix);
                Equal(npc.Sprite.Texture, texture); Equal(npc.Portrait, portrait);
                if (winter && npc.LastAppearanceId != "Winter") throw new Exception("Native winter appearance was not selected");
                if (texture.Width != 64 || texture.Height != 192 || portrait.Width != 128 || portrait.Height != 128) throw new Exception("Evelyn native atlas dimensions differ");
                var pixels = Pixels(texture); var portraitPixels = Pixels(portrait);
                void CheckFrame(int frame)
                {
                    if (frame < 0 || frame > 20) throw new Exception("Animation used an invalid Evelyn pose: " + frame);
                    var expected = new Rectangle(frame % 4 * 16, frame / 4 * 32, 16, 32);
                    if (npc.Sprite.CurrentFrame != frame || npc.Sprite.SourceRect != expected) throw new Exception("Native frame rectangle differs: " + frame);
                }
                for (var frame = 0; frame < 21; frame++)
                {
                    npc.Sprite.CurrentFrame = frame; CheckFrame(frame); var colors = new HashSet<Color>();
                    for (var y = 0; y < 32; y++) for (var x = 0; x < 16; x++) { var c = pixels[(frame / 4 * 32 + y) * 64 + frame % 4 * 16 + x]; if (c.A != 0) colors.Add(c); }
                    if (colors.Count < 8) throw new Exception("Empty or solid Evelyn sprite: " + frame);
                }
                for (var frame = 21; frame < 24; frame++) for (var y = 0; y < 32; y++) for (var x = 0; x < 16; x++)
                    if (pixels[(frame / 4 * 32 + y) * 64 + frame % 4 * 16 + x] != new Color(247, 255, 252, 255)) throw new Exception("Native pale placeholder differs: " + frame);
                for (var index = 0; index < 4; index++)
                {
                    if (new Dialogue(npc, null, "Expression.$" + index).getPortraitIndex() != index) throw new Exception("Evelyn portrait index differs");
                    var colors = new HashSet<Color>(); var transparent = 0;
                    for (var y = 0; y < 64; y++) for (var x = 0; x < 64; x++) { var c = portraitPixels[(index / 2 * 64 + y) * 128 + index % 2 * 64 + x]; if (c.A != 0) colors.Add(c); else transparent++; }
                    if (colors.Count < 8 || transparent == 0) throw new Exception("Empty or solid Evelyn portrait: " + index);
                }
                for (var direction = 0; direction < 4; direction++)
                {
                    npc.Sprite.CurrentFrame = direction * 4; npc.Sprite.timer = 0;
                    for (var step = 1; step <= 8; step++)
                    {
                        var time = new GameTime(TimeSpan.FromMilliseconds(step * 180), TimeSpan.FromMilliseconds(180));
                        switch (direction) { case 0: npc.Sprite.AnimateDown(time); break; case 1: npc.Sprite.AnimateRight(time); break; case 2: npc.Sprite.AnimateUp(time); break; case 3: npc.Sprite.AnimateLeft(time); break; }
                        CheckFrame(direction * 4 + step % 4); walkingSteps++;
                    }
                }
                var descriptions = helper.GameContent.Load<Dictionary<string, string>>("Data/animationDescriptions");
                foreach (var key in new[] { "evelyn_sit_left", "evelyn_garden" })
                {
                    var sections = descriptions[key].Split('/').Take(3).Select(s => s.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray()).ToArray();
                    foreach (var frames in sections)
                    {
                        npc.Sprite.loop = true; npc.Sprite.setCurrentAnimation(frames.Select(f => new FarmerSprite.AnimationFrame(f, 150)).ToList());
                        for (var step = 1; step <= frames.Length * 2; step++) { npc.Sprite.animateOnce(new GameTime(TimeSpan.FromMilliseconds(step * 150), TimeSpan.FromMilliseconds(150))); CheckFrame(frames[step % frames.Length]); specialSteps++; }
                    }
                    var routeNpc = Create(); routeNpc.ChooseAppearance();
                    var behavior = helper.Reflection.GetMethod(routeNpc, "getRouteEndBehaviorFunction").Invoke<Delegate>(key, null);
                    if (behavior == null) throw new Exception("Native Evelyn route missing: " + key);
                    behavior.DynamicInvoke(routeNpc, location);
                    if (!routeNpc.Sprite.currentAnimation.Select(f => f.frame).SequenceEqual(sections[0])) throw new Exception("Native route intro differs: " + key);
                    for (var step = 1; step <= sections[0].Length; step++) routeNpc.Sprite.animateOnce(new GameTime(TimeSpan.FromMilliseconds(step * 100), TimeSpan.FromMilliseconds(100)));
                    if (!routeNpc.Sprite.currentAnimation.Select(f => f.frame).SequenceEqual(sections[1])) throw new Exception("Native Evelyn route loop differs: " + key);
                    routeTransitions++;
                }
                var scene = new Event(); scene.actors.Add(npc); helper.Reflection.GetField<bool>(scene, "eventFinished").SetValue(true);
                foreach (var frame in new[] { 16, 17, 18, 19, 20 })
                {
                    npc.Sprite.StopAnimation(); npc.Sprite.CurrentFrame = 0;
                    var args = new[] { "showFrame", "Evelyn", frame.ToString() };
                    Event.DefaultCommands.ShowFrame(scene, args, new EventContext(scene, location, Game1.currentGameTime, args)); CheckFrame(frame); showFrameCommands++;
                }
                var eventText = helper.GameContent.Load<Dictionary<string, string>>("Data/Events/Town").First(p => p.Key.StartsWith("191393/")).Value;
                if (!eventText.Split('/').Contains("animate Evelyn true false 500 17 18 17 19 19 19 19")) throw new Exception("Native community-center gardening command changed");
                var device = Game1.graphics.GraphicsDevice; var previous = device.GetRenderTargets(); using var target = new RenderTarget2D(device, 960, 960); using var batch = new SpriteBatch(device);
                try
                {
                    device.SetRenderTarget(target); device.Clear(new Color(35, 45, 58)); batch.Begin(samplerState: SamplerState.PointClamp);
                    for (var i = 0; i < 21; i++) batch.Draw(texture, new Rectangle(i % 4 * 110 + 10, i / 4 * 150 + 10, 64, 128), new Rectangle(i % 4 * 16, i / 4 * 32, 16, 32), Color.White);
                    batch.Draw(portrait, new Rectangle(500, 16, 448, 448), Color.White); batch.End(); device.SetRenderTargets(previous);
                    using var file = File.Create(Path.Combine(helper.DirectoryPath, "evelyn-" + (winter ? "winter" : "base") + "-runtime-preview.png")); target.SaveAsPng(file, target.Width, target.Height);
                }
                finally { device.SetRenderTargets(previous); }
                Game1.season = winter ? Season.Spring : Season.Winter; npc.ChooseAppearance(); var nextSuffix = winter ? "" : "_Winter";
                Equal(npc.Sprite.Texture, helper.GameContent.Load<Texture2D>("Characters/Evelyn" + nextSuffix)); Equal(npc.Portrait, helper.GameContent.Load<Texture2D>("Portraits/Evelyn" + nextSuffix));
            }
            helper.Data.WriteJsonFile("evelyn-checks.json", new { Passed = true, OutfitVariants = 2, OccupiedPosesPerVariant = 21, PreservedPlaceholdersPerVariant = 3, PortraitSlotsPerVariant = 4, NativeWalkingSteps = walkingSteps, NativeSpecialSteps = specialSteps, NativeRouteTransitions = routeTransitions, ActualShowFrameCommands = showFrameCommands, CommunityCenterGardeningCommandMatched = true, BothSeasonTransitions = true, FarmLoaded = false, FullEventPlayback = false });
            monitor.Log("Evelyn audit passed: everyday and winter poses, portraits, walking, gardening and sitting routes, and seasonal transitions. Full scenes remain unverified.", LogLevel.Info);
        }
        catch (Exception ex) { helper.Data.WriteJsonFile("evelyn-checks.json", new { Passed = false, Error = ex.ToString() }); monitor.Log("Evelyn audit failed: " + ex, LogLevel.Error); }
        finally { Game1.season = oldSeason; Game1.currentLocation = oldLocation; }
    }
}
