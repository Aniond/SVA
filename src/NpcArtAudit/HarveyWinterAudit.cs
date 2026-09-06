using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NpcArtAudit;

internal static class HarveyWinterAudit
{
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        var oldSeason = Game1.season; var oldLocation = Game1.currentLocation;
        try
        {
            Game1.season = Season.Winter;
            var location = new GameLocation("Maps/Town", "Town"); Game1.currentLocation = location;
            NPC Create() => new(new AnimatedSprite("Characters/Harvey", 0, 16, 32), Vector2.Zero, 2, "Harvey") { currentLocation = location };
            var npc = Create(); npc.ChooseAppearance();
            if ((npc.Sprite.overrideTextureName ?? npc.Sprite.textureName.Value).Replace('\\', '/') != "Characters/Harvey_Winter") throw new Exception("Native winter appearance missing");
            void Equal(Texture2D a, Texture2D b)
            {
                if (a.Width != b.Width || a.Height != b.Height) throw new Exception("Selected texture size differs");
                var pa = new Color[a.Width * a.Height]; var pb = new Color[b.Width * b.Height]; a.GetData(pa); b.GetData(pb);
                if (!pa.SequenceEqual(pb)) throw new Exception("Selected texture pixels differ");
            }
            Equal(npc.Sprite.Texture, helper.GameContent.Load<Texture2D>("Characters/Harvey_Winter"));
            Equal(npc.Portrait, helper.GameContent.Load<Texture2D>("Portraits/Harvey_Winter"));
            var texture = npc.Sprite.Texture; var portrait = npc.Portrait;
            if (texture.Width != 64 || texture.Height != 448 || portrait.Width != 128 || portrait.Height != 384) throw new Exception("Native atlas dimensions differ");
            var pixels = new Color[texture.Width * texture.Height]; texture.GetData(pixels);
            var portraitPixels = new Color[portrait.Width * portrait.Height]; portrait.GetData(portraitPixels);
            void Frame(int frame)
            {
                if (frame < 0 || frame >= 55) throw new Exception("Native command references invalid/placeholder frame: " + frame);
                npc.Sprite.CurrentFrame = frame;
                if (npc.Sprite.SourceRect != new Rectangle(frame % 4 * 16, frame / 4 * 32, 16, 32)) throw new Exception("Native frame rectangle mismatch: " + frame);
            }
            for (var frame = 0; frame < 55; frame++)
            {
                Frame(frame); var r = npc.Sprite.SourceRect; var visible = 0;
                for (var y = r.Y; y < r.Bottom; y++) for (var x = r.X; x < r.Right; x++) if (pixels[y * 64 + x].A > 0) visible++;
                if (visible == 0) throw new Exception("Empty Harvey winter pose: " + frame);
            }
            for (var y = 0; y < 32; y++) for (var x = 0; x < 16; x++)
                if (pixels[(416 + y) * 64 + 48 + x] != (y == 31 ? Color.Transparent : Color.Black)) throw new Exception("Native black placeholder changed");
            for (var index = 0; index < 12; index++)
            {
                if (new Dialogue(npc, null, "Winter expression.$" + index).getPortraitIndex() != index) throw new Exception("Portrait index mismatch");
                var visible = 0;
                for (var y = 0; y < 64; y++) for (var x = 0; x < 64; x++) if (portraitPixels[(index / 2 * 64 + y) * 128 + index % 2 * 64 + x].A > 0) visible++;
                if (visible == 0) throw new Exception("Empty portrait: " + index);
            }

            var requestedEvents = new[] { ("Hospital", "57/"), ("Hospital", "571102/"), ("Railroad", "528052/"), ("SeedShop", "58/"), ("FarmHouse", "3917626/") };
            var eventFrames = new List<int>(); var eventKeys = new List<string>(); var showCommands = 0;
            var scene = new Event(); scene.actors.Add(npc); helper.Reflection.GetField<bool>(scene, "eventFinished").SetValue(true);
            foreach (var (map, prefix) in requestedEvents)
            {
                var entry = helper.GameContent.Load<Dictionary<string, string>>("Data/Events/" + map).First(pair => pair.Key.StartsWith(prefix)); eventKeys.Add(map + ":" + entry.Key);
                foreach (var command in entry.Value.Split('/'))
                {
                    var args = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (args.Length < 3 || args[1] != "Harvey") continue;
                    if (args[0] == "showFrame")
                    {
                        var frame = int.Parse(args[2]); Frame(frame);
                        npc.Sprite.CurrentFrame = (frame + 1) % 55;
                        Event.DefaultCommands.ShowFrame(scene, args, new EventContext(scene, location, Game1.currentGameTime, args));
                        if (npc.Sprite.CurrentFrame != frame) throw new Exception("Native ShowFrame selected unexpected pose");
                        showCommands++; eventFrames.Add(frame);
                    }
                    if (args[0] == "animate") foreach (var arg in args.Skip(5)) { var frame = int.Parse(arg); Frame(frame); eventFrames.Add(frame); }
                }
            }
            foreach (var required in new[] { 20, 21, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 51, 52, 53 })
                if (!eventFrames.Contains(required)) throw new Exception("Expected event pose is absent: " + required);
            helper.Data.WriteJsonFile("harvey-winter-native-events.json", new { Keys = eventKeys, Frames = eventFrames, ActualShowFrameCommands = showCommands, FullEventPlayback = false });

            var descriptions = helper.GameContent.Load<Dictionary<string, string>>("Data/animationDescriptions");
            var animationSteps = 0; var routeTransitions = 0;
            foreach (var key in new[] { "harvey_read", "harvey_radio", "harvey_eat", "harvey_examine_left", "harvey_sleep" })
            {
                var sections = descriptions[key].Split('/').Take(3).Select(section => section.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray()).ToArray();
                foreach (var frames in sections)
                {
                    foreach (var frame in frames) Frame(frame);
                    npc.Sprite.loop = true; npc.Sprite.setCurrentAnimation(frames.Select(frame => new FarmerSprite.AnimationFrame(frame, 150)).ToList());
                    for (var step = 1; step <= frames.Length * 2; step++)
                    {
                        npc.Sprite.animateOnce(new GameTime(TimeSpan.FromMilliseconds(step * 150), TimeSpan.FromMilliseconds(150)));
                        if (npc.Sprite.CurrentFrame != frames[step % frames.Length]) throw new Exception("Native timed animation mismatch: " + key);
                        animationSteps++;
                    }
                }
                var routeNpc = Create(); routeNpc.ChooseAppearance();
                var behavior = helper.Reflection.GetMethod(routeNpc, "getRouteEndBehaviorFunction").Invoke<Delegate>(key, null);
                if (behavior == null) throw new Exception("Missing native route behavior: " + key);
                behavior.DynamicInvoke(routeNpc, location);
                if (!routeNpc.Sprite.currentAnimation.Select(frame => frame.frame).SequenceEqual(sections[0])) throw new Exception("Native route intro differs: " + key);
                for (var step = 1; step <= sections[0].Length; step++) routeNpc.Sprite.animateOnce(new GameTime(TimeSpan.FromMilliseconds(step * 100), TimeSpan.FromMilliseconds(100)));
                if (!routeNpc.Sprite.currentAnimation.Select(frame => frame.frame).SequenceEqual(sections[1])) throw new Exception("Native intro-to-loop callback failed: " + key);
                var sleep = key == "harvey_sleep";
                // The native intro callback calls playSleepingAnimation, which shifts an unmarried Harvey up four pixels.
                var expectedOffset = sleep ? new Vector2(0, -4) : Vector2.Zero;
                if (routeNpc.layingDown != sleep || routeNpc.HideShadow != sleep || routeNpc.drawOffset != expectedOffset || routeNpc.isSleeping.Value != sleep) throw new Exception($"Native route pose flags differ: {key}; layingDown={routeNpc.layingDown}, HideShadow={routeNpc.HideShadow}, offset={routeNpc.drawOffset}, sleeping={routeNpc.isSleeping.Value}");
                routeTransitions++;
            }
            var walker = new AnimatedSprite("Characters/Harvey_Winter", 0, 16, 32); var walkingSteps = 0;
            for (var direction = 0; direction < 4; direction++)
            {
                walker.CurrentFrame = direction * 4; walker.timer = 0;
                for (var step = 1; step <= 8; step++)
                {
                    var time = new GameTime(TimeSpan.FromMilliseconds(step * 180), TimeSpan.FromMilliseconds(180));
                    switch (direction) { case 0: walker.AnimateDown(time); break; case 1: walker.AnimateRight(time); break; case 2: walker.AnimateUp(time); break; case 3: walker.AnimateLeft(time); break; }
                    var frame = direction * 4 + step % 4;
                    if (walker.CurrentFrame != frame || walker.SourceRect != new Rectangle(frame % 4 * 16, frame / 4 * 32, 16, 32)) throw new Exception("Native walking rectangle differs");
                    walkingSteps++;
                }
            }
            var device = Game1.graphics.GraphicsDevice; var previous = device.GetRenderTargets();
            using var target = new RenderTarget2D(device, 1024, 1216); using var batch = new SpriteBatch(device);
            try
            {
                device.SetRenderTarget(target); device.Clear(new Color(35, 45, 58)); batch.Begin(samplerState: SamplerState.PointClamp);
                for (var i = 0; i < 56; i++) batch.Draw(texture, new Rectangle(i % 8 * 64 + 8, i / 8 * 132 + 8, 48, 96), new Rectangle(i % 4 * 16, i / 4 * 32, 16, 32), Color.White);
                batch.Draw(portrait, new Rectangle(600, 16, 384, 1152), Color.White); batch.End(); device.SetRenderTargets(previous);
                using var file = File.Create(Path.Combine(helper.DirectoryPath, "harvey-winter-runtime-preview.png")); target.SaveAsPng(file, target.Width, target.Height);
            }
            finally { device.SetRenderTargets(previous); }
            Game1.season = Season.Spring; npc.ChooseAppearance(); Equal(npc.Sprite.Texture, helper.GameContent.Load<Texture2D>("Characters/Harvey")); Equal(npc.Portrait, helper.GameContent.Load<Texture2D>("Portraits/Harvey"));
            helper.Data.WriteJsonFile("harvey-winter-checks.json", new { Passed = true, OccupiedPoses = 55, PreservedPlaceholders = 1, PortraitSlots = 12, NativeWinterSelected = true, SpringRestored = true, NativeSpecialSteps = animationSteps, NativeRouteIntroToLoopTransitions = routeTransitions, NativeWalkingSteps = walkingSteps, NativeEventFrameReferences = eventFrames.Count, NativeShowFrameCommands = showCommands, FarmLoaded = false, FullEventPlayback = false });
            monitor.Log("Harvey winter audit passed: 55 poses, native placeholder, 12 portraits, native route transitions and event frame commands; full scenes remain unverified.", LogLevel.Info);
        }
        catch (Exception ex) { helper.Data.WriteJsonFile("harvey-winter-checks.json", new { Passed = false, Error = ex.ToString() }); monitor.Log("Harvey winter audit failed: " + ex, LogLevel.Error); }
        finally { Game1.season = oldSeason; Game1.currentLocation = oldLocation; }
    }
}
