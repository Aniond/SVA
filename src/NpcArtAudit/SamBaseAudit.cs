using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NpcArtAudit;

internal static class SamBaseAudit
{
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        var oldSeason = Game1.season; var oldLocation = Game1.currentLocation;
        try
        {
            Game1.season = Season.Spring;
            var location = new GameLocation("Maps/Town", "Town"); Game1.currentLocation = location;
            NPC Create() => new(new AnimatedSprite("Characters/Sam", 0, 16, 32), Vector2.Zero, 2, "Sam") { currentLocation = location };
            var npc = Create(); npc.ChooseAppearance();
            if ((npc.Sprite.overrideTextureName ?? npc.Sprite.textureName.Value).Replace('\\', '/') != "Characters/Sam") throw new Exception("Native everyday appearance missing");
            void Equal(Texture2D a, Texture2D b)
            {
                if (a.Width != b.Width || a.Height != b.Height) throw new Exception("Selected texture size differs");
                var pa = new Color[a.Width * a.Height]; var pb = new Color[b.Width * b.Height]; a.GetData(pa); b.GetData(pb);
                if (!pa.SequenceEqual(pb)) throw new Exception("Selected texture pixels differ");
            }
            Equal(npc.Sprite.Texture, helper.GameContent.Load<Texture2D>("Characters/Sam"));
            Equal(npc.Portrait, helper.GameContent.Load<Texture2D>("Portraits/Sam"));
            var texture = npc.Sprite.Texture; var portrait = npc.Portrait;
            if (texture.Width != 64 || texture.Height != 448 || portrait.Width != 128 || portrait.Height != 384) throw new Exception("Native atlas dimensions differ");
            var pixels = new Color[texture.Width * texture.Height]; texture.GetData(pixels);
            var portraitPixels = new Color[portrait.Width * portrait.Height]; portrait.GetData(portraitPixels);
            var winterPixels = new Color[64 * 448]; helper.GameContent.Load<Texture2D>("Characters/Sam_Winter").GetData(winterPixels);
            foreach (var frame in new[] {40,41,42,44,45,46,47,48,49,50}) for (var y = 0; y < 32; y++) for (var x = 0; x < 16; x++)
            {
                var p = (frame / 4 * 32 + y) * 64 + frame % 4 * 16 + x;
                if (pixels[p] != winterPixels[p]) throw new Exception("Retained modern work/formal pose differs: " + frame);
            }
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
                if (visible == 0) throw new Exception("Empty Sam everyday pose: " + frame);
            }
            for (var y = 0; y < 32; y++) for (var x = 0; x < 16; x++)
                if (pixels[(416 + y) * 64 + 48 + x] != Color.White) throw new Exception("Native white placeholder changed");
            for (var index = 0; index < 12; index++)
            {
                if (new Dialogue(npc, null, "Winter expression.$" + index).getPortraitIndex() != index) throw new Exception("Portrait index mismatch");
                var visible = 0;
                for (var y = 0; y < 64; y++) for (var x = 0; x < 64; x++) if (portraitPixels[(index / 2 * 64 + y) * 128 + index % 2 * 64 + x].A > 0) visible++;
                if (visible == 0) throw new Exception("Empty portrait: " + index);
            }

            var requestedEvents = new[] { ("Beach", "733330/f Sam 750/w sunny/t 700 1500/z winter/y 1"), ("FarmHouse", "3918601/e 3918600/O Sam/t 610 1700/A samJob1/p Sam/L"), ("FarmHouse", "3918602/e 3918601/O Sam/t 610 1700/A samJob2/p Sam/L"), ("SamHouse", "44/f Sam 500/p Sam"), ("SamHouse", "46/f Sam 1000/p Sam"), ("SamHouse", "stayPut"), ("SamHouse", "rejectSam"), ("SebastianRoom", "27/f Sebastian 1500/p Sebastian"), ("Temp", "poppy"), ("Temp", "heavy"), ("Temp", "techno"), ("Temp", "honkytonk"), ("Town", "45/f Sam 1500/t 1200 1600/w sunny"), ("Town", "233104/f Sam 2500/t 2000 2400/w sunny/n samMessage") };
            var eventFrames = new List<int>(); var eventKeys = new List<string>(); var showCommands = 0;
            var scene = new Event(); scene.actors.Add(npc); helper.Reflection.GetField<bool>(scene, "eventFinished").SetValue(true);
            foreach (var (map, prefix) in requestedEvents)
            {
                var entry = helper.GameContent.Load<Dictionary<string, string>>("Data/Events/" + map).First(pair => pair.Key == prefix); eventKeys.Add(map + ":" + entry.Key);
                foreach (var command in entry.Value.Split('/'))
                {
                    var args = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (args.Length < 3 || args[1] != "Sam") continue;
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
            if (eventFrames.Count != 50 || showCommands != 20) throw new Exception("Native event frame inventory changed");
            foreach (var required in new[] { 20, 21, 22, 23, 24, 25, 33, 34, 35, 36, 37, 38, 39, 43, 51, 52, 53 })
                if (!eventFrames.Contains(required)) throw new Exception("Expected event pose is absent: " + required);
            helper.Data.WriteJsonFile("sam-base-native-events.json", new { Keys = eventKeys, Frames = eventFrames, ActualShowFrameCommands = showCommands, FullEventPlayback = false });

            // Exercise the native skateboard trick callbacks with isolated actors and a temporary carrier.
            scene.farmerActors.Clear(); scene.farmerActors.Add(new Farmer());
            var skateArgs = new[] { "specificTemporarySprite", "samSkate1" };
            Event.DefaultCommands.SpecificTemporarySprite(scene, skateArgs, new EventContext(scene, location, Game1.currentGameTime, skateArgs));
            var carrier = location.getTemporarySpriteByID(92473);
            if (carrier == null || carrier.attachedCharacter != npc) throw new Exception("Native skateboard carrier missing");
            void SkateCallback(string method, int expected)
            {
                npc.Sprite.CurrentFrame = 0;
                helper.Reflection.GetMethod(scene, method).Invoke(0);
                if (npc.Sprite.CurrentFrame != expected) throw new Exception("Native skate pose differs: " + method);
                npc.Sprite.UpdateSourceRect();
                if (npc.Sprite.SourceRect != new Rectangle(expected % 4 * 16, expected / 4 * 32, 16, 32)) throw new Exception("Native skate rectangle differs");
            }
            SkateCallback("samPreOllie", 27);
            if (carrier.xStopCoordinate != 1408 || carrier.motion.X != 2) throw new Exception("Native pre-ollie motion differs");
            SkateCallback("samOllie", 26);
            if (carrier.motion.Y != -9 || carrier.acceleration.Y != .4f) throw new Exception("Native ollie motion differs");
            SkateCallback("samGrind", 28);
            if (carrier.xStopCoordinate != 1664 || carrier.motion.Y != 0) throw new Exception("Native grind motion differs");
            helper.Reflection.GetMethod(scene, "samDropOff").Invoke(0);
            if (!npc.Sprite.currentAnimation.Select(frame => frame.frame).SequenceEqual(new[] { 29, 30, 31, 32 }) || npc.Sprite.loop) throw new Exception("Native drop-off animation differs");
            var commandBeforeLanding = scene.CurrentCommand;
            helper.Reflection.GetMethod(scene, "samGround").Invoke(0);
            if (carrier.attachedCharacter != null || !carrier.destroyable || scene.CurrentCommand != commandBeforeLanding + 1) throw new Exception("Native landing cleanup differs");
            location.TemporarySprites.Remove(carrier); npc.Sprite.StopAnimation();

            var descriptions = helper.GameContent.Load<Dictionary<string, string>>("Data/animationDescriptions");
            var animationSteps = 0; var routeTransitions = 0;
            foreach (var key in new[] { "sam_guitar", "sam_gameboy", "sam_skateboarding", "sam_pool", "sam_work", "sam_sleep" })
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
                var sleep = key == "sam_sleep";
                // The native intro callback calls playSleepingAnimation, which shifts an unmarried Sam up four pixels.
                var expectedOffset = sleep ? new Vector2(0, -4) : Vector2.Zero;
                if (routeNpc.layingDown != sleep || routeNpc.HideShadow != sleep || routeNpc.drawOffset != expectedOffset || routeNpc.isSleeping.Value != sleep) throw new Exception($"Native route pose flags differ: {key}; layingDown={routeNpc.layingDown}, HideShadow={routeNpc.HideShadow}, offset={routeNpc.drawOffset}, sleeping={routeNpc.isSleeping.Value}");
                routeTransitions++;
            }
            var walkingSteps = 0;
            foreach (var asset in new[] {"Characters/Sam", "Characters/Sam_Winter", "Characters/Sam_Beach"})
            {
            var walker = new AnimatedSprite(asset, 0, 16, 32);
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
            }
            var device = Game1.graphics.GraphicsDevice; var previous = device.GetRenderTargets();
            using var target = new RenderTarget2D(device, 1024, 1216); using var batch = new SpriteBatch(device);
            try
            {
                device.SetRenderTarget(target); device.Clear(new Color(35, 45, 58)); batch.Begin(samplerState: SamplerState.PointClamp);
                for (var i = 0; i < 56; i++) batch.Draw(texture, new Rectangle(i % 8 * 64 + 8, i / 8 * 132 + 8, 48, 96), new Rectangle(i % 4 * 16, i / 4 * 32, 16, 32), Color.White);
                batch.Draw(portrait, new Rectangle(600, 16, 384, 1152), Color.White); batch.End(); device.SetRenderTargets(previous);
                using var file = File.Create(Path.Combine(helper.DirectoryPath, "sam-base-runtime-preview.png")); target.SaveAsPng(file, target.Width, target.Height);
            }
            finally { device.SetRenderTargets(previous); }
            Game1.season = Season.Winter; npc.ChooseAppearance(); Equal(npc.Sprite.Texture, helper.GameContent.Load<Texture2D>("Characters/Sam_Winter")); Equal(npc.Portrait, helper.GameContent.Load<Texture2D>("Portraits/Sam_Winter"));
            Game1.season = Season.Summer; npc.wearIslandAttire(); Equal(npc.Sprite.Texture, helper.GameContent.Load<Texture2D>("Characters/Sam_Beach")); Equal(npc.Portrait, helper.GameContent.Load<Texture2D>("Portraits/Sam_Beach"));
            npc.wearNormalClothes(); Equal(npc.Sprite.Texture, helper.GameContent.Load<Texture2D>("Characters/Sam")); Equal(npc.Portrait, helper.GameContent.Load<Texture2D>("Portraits/Sam"));
            helper.Data.WriteJsonFile("sam-base-checks.json", new { Passed = true, OccupiedPoses = 55, PreservedPlaceholders = 1, PortraitSlots = 12, NativeEverydaySelected = true, WinterAndBeachTransitions = true, NormalRestored = true, RetainedWinterFrames = 10, NativeSpecialSteps = animationSteps, NativeRouteIntroToLoopTransitions = routeTransitions, NativeWalkingSteps = walkingSteps, NativeEventFrameReferences = eventFrames.Count, NativeShowFrameCommands = showCommands, NativeSkateCallbacks = 5, FarmLoaded = false, FullEventPlayback = false });
            monitor.Log("Sam everyday audit passed: 55 poses, native placeholder, 12 portraits, native route transitions and event frame commands; full scenes remain unverified.", LogLevel.Info);
        }
        catch (Exception ex) { helper.Data.WriteJsonFile("sam-base-checks.json", new { Passed = false, Error = ex.ToString() }); monitor.Log("Sam everyday audit failed: " + ex, LogLevel.Error); }
        finally { Game1.season = oldSeason; Game1.currentLocation = oldLocation; }
    }
}

