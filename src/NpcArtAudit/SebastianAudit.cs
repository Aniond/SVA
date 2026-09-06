using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NpcArtAudit;

internal static class SebastianAudit
{
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        var oldSeason = Game1.season; var oldLocation = Game1.currentLocation;
        try
        {
            Color[] Pixels(Texture2D texture) { var data = new Color[texture.Width * texture.Height]; texture.GetData(data); return data; }
            void Equal(Texture2D actual, Texture2D expected)
            {
                if (actual.Width != expected.Width || actual.Height != expected.Height || !Pixels(actual).SequenceEqual(Pixels(expected))) throw new Exception("Selected Sebastian texture differs from registered artwork");
            }
            var walkingSteps = 0; var specialSteps = 0; var routeTransitions = 0; var showFrameCommands = 0; var eventReferences = 0;
            foreach (var variant in new[] { "Base", "Winter", "Beach" })
            {
                var winter = variant == "Winter"; var beach = variant == "Beach"; var frameCount = beach ? 24 : 57; var portraitCount = beach ? 8 : 10;
                Game1.season = winter ? Season.Winter : beach ? Season.Summer : Season.Spring;
                var location = new GameLocation("Maps/Town", "Town"); Game1.currentLocation = location;
                NPC Create() => new(new AnimatedSprite("Characters/Sebastian", 0, 16, 32), Vector2.Zero, 2, "Sebastian") { currentLocation = location };
                var npc = Create(); if (beach) npc.wearIslandAttire(); else npc.ChooseAppearance();
                var suffix = winter ? "_Winter" : beach ? "_Beach" : "";
                var texture = helper.GameContent.Load<Texture2D>("Characters/Sebastian" + suffix); var portrait = helper.GameContent.Load<Texture2D>("Portraits/Sebastian" + suffix);
                Equal(npc.Sprite.Texture, texture); Equal(npc.Portrait, portrait);
                if (winter && npc.LastAppearanceId != "Winter") throw new Exception("Native winter appearance was not selected");
                if (texture.Width != 64 || texture.Height != (beach ? 192 : 480) || portrait.Width != 128 || portrait.Height != (beach ? 256 : 320)) throw new Exception("Sebastian native atlas dimensions differ");
                var pixels = Pixels(texture); var portraitPixels = Pixels(portrait);
                void CheckFrame(int frame)
                {
                    if (frame < 0 || frame >= frameCount) throw new Exception("Animation used an invalid Sebastian pose: " + frame);
                    var expected = new Rectangle(frame % 4 * 16, frame / 4 * 32, 16, 32);
                    if (npc.Sprite.CurrentFrame != frame || npc.Sprite.SourceRect != expected) throw new Exception("Native frame rectangle differs: " + frame);
                }
                for (var frame = 0; frame < frameCount; frame++)
                {
                    npc.Sprite.CurrentFrame = frame; CheckFrame(frame); var colors = new HashSet<Color>();
                    for (var y = 0; y < 32; y++) for (var x = 0; x < 16; x++) { var c = pixels[(frame / 4 * 32 + y) * 64 + frame % 4 * 16 + x]; if (c.A != 0) colors.Add(c); }
                    if (colors.Count < 8) throw new Exception("Empty or solid Sebastian sprite: " + frame);
                }
                if (!beach) for (var frame = 57; frame < 60; frame++) for (var y = 0; y < 32; y++) for (var x = 0; x < 16; x++)
                    if (pixels[(frame / 4 * 32 + y) * 64 + frame % 4 * 16 + x] != new Color(123, 66, 26, 255)) throw new Exception("Native brown placeholder differs: " + frame);
                for (var index = 0; index < portraitCount; index++)
                {
                    if (new Dialogue(npc, null, "Expression.$" + index).getPortraitIndex() != index) throw new Exception("Sebastian portrait index differs");
                    var colors = new HashSet<Color>(); var transparent = 0;
                    for (var y = 0; y < 64; y++) for (var x = 0; x < 64; x++) { var c = portraitPixels[(index / 2 * 64 + y) * 128 + index % 2 * 64 + x]; if (c.A != 0) colors.Add(c); else transparent++; }
                    if (colors.Count < 8 || transparent == 0) throw new Exception("Empty or solid Sebastian portrait: " + index);
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
                foreach (var key in beach ? new[] { "sebastian_beach_towel", "sebastian_beach_umbrella" } : new[] { "sebastian_computer", "sebastian_cardsLeft", "sebastian_smoking", "sebastian_sleep" })
                {
                    var sections = descriptions[key].Split('/').Take(3).Select(s => s.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray()).ToArray();
                    foreach (var frames in sections)
                    {
                        npc.Sprite.loop = true; npc.Sprite.setCurrentAnimation(frames.Select(f => new FarmerSprite.AnimationFrame(f, 150)).ToList());
                        for (var step = 1; step <= frames.Length * 2; step++) { npc.Sprite.animateOnce(new GameTime(TimeSpan.FromMilliseconds(step * 150), TimeSpan.FromMilliseconds(150))); CheckFrame(frames[step % frames.Length]); specialSteps++; }
                    }
                    var routeNpc = Create(); if (beach) routeNpc.wearIslandAttire(); else routeNpc.ChooseAppearance();
                    var behavior = helper.Reflection.GetMethod(routeNpc, "getRouteEndBehaviorFunction").Invoke<Delegate>(key, null);
                    if (behavior == null) throw new Exception("Native Sebastian route missing: " + key);
                    behavior.DynamicInvoke(routeNpc, location);
                    if (!routeNpc.Sprite.currentAnimation.Select(f => f.frame).SequenceEqual(sections[0])) throw new Exception("Native route intro differs: " + key);
                    for (var step = 1; step <= sections[0].Length; step++) routeNpc.Sprite.animateOnce(new GameTime(TimeSpan.FromMilliseconds(step * 100), TimeSpan.FromMilliseconds(100)));
                    if (!routeNpc.Sprite.currentAnimation.Select(f => f.frame).SequenceEqual(sections[1])) throw new Exception("Native Sebastian route loop differs: " + key);
                    routeTransitions++;
                }
                if (!beach)
                {
                    var scene = new Event(); scene.actors.Add(npc); helper.Reflection.GetField<bool>(scene, "eventFinished").SetValue(true);
                    var requiredFrames = new HashSet<int>();
                    foreach (var map in new[] { "Beach", "FarmHouse", "Mountain", "SamHouse", "SebastianRoom", "Temp" })
                    foreach (var entry in helper.GameContent.Load<Dictionary<string, string>>("Data/Events/" + map))
                    foreach (var command in entry.Value.Split('/'))
                    {
                        var args = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (args.Length < 3 || args[1] != "Sebastian") continue;
                        if (args[0] == "showFrame")
                        {
                            var frame = int.Parse(args[2]); npc.Sprite.StopAnimation(); npc.Sprite.CurrentFrame = frame == 0 ? 1 : 0;
                            Event.DefaultCommands.ShowFrame(scene, args, new EventContext(scene, location, Game1.currentGameTime, args)); CheckFrame(frame); showFrameCommands++; eventReferences++; requiredFrames.Add(frame);
                        }
                        else if (args[0] == "animate") foreach (var arg in args.Skip(5)) { var frame = int.Parse(arg); npc.Sprite.CurrentFrame = frame; CheckFrame(frame); eventReferences++; requiredFrames.Add(frame); }
                    }
                    foreach (var required in new[] { 16, 23, 24, 27, 28, 29, 30, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 51, 52, 53, 54, 55 })
                        if (!requiredFrames.Contains(required)) throw new Exception("Missing expected native event pose: " + required);
                }
                var device = Game1.graphics.GraphicsDevice; var previous = device.GetRenderTargets(); using var target = new RenderTarget2D(device, 1024, 1216); using var batch = new SpriteBatch(device);
                try
                {
                    device.SetRenderTarget(target); device.Clear(new Color(35, 45, 58)); batch.Begin(samplerState: SamplerState.PointClamp);
                    for (var i = 0; i < frameCount; i++) batch.Draw(texture, new Rectangle(i % 8 * 64 + 8, i / 8 * 132 + 8, 48, 96), new Rectangle(i % 4 * 16, i / 4 * 32, 16, 32), Color.White);
                    batch.Draw(portrait, new Rectangle(600, 16, 384, portrait.Height * 3), Color.White); batch.End(); device.SetRenderTargets(previous);
                    using var file = File.Create(Path.Combine(helper.DirectoryPath, "sebastian-" + variant.ToLowerInvariant() + "-runtime-preview.png")); target.SaveAsPng(file, target.Width, target.Height);
                }
                finally { device.SetRenderTargets(previous); }
                Game1.season = Season.Winter; npc.wearNormalClothes(); npc.ChooseAppearance();
                Equal(npc.Sprite.Texture, helper.GameContent.Load<Texture2D>("Characters/Sebastian_Winter")); Equal(npc.Portrait, helper.GameContent.Load<Texture2D>("Portraits/Sebastian_Winter"));
                Game1.season = Season.Spring; npc.ChooseAppearance();
                Equal(npc.Sprite.Texture, helper.GameContent.Load<Texture2D>("Characters/Sebastian")); Equal(npc.Portrait, helper.GameContent.Load<Texture2D>("Portraits/Sebastian"));

            }
            helper.Data.WriteJsonFile("sebastian-checks.json", new { Passed = true, OutfitVariants = 3, OccupiedPoses = 138, PreservedPlaceholders = 6, PortraitSlots = 28, NativeWalkingSteps = walkingSteps, NativeSpecialSteps = specialSteps, NativeRouteTransitions = routeTransitions, ActualShowFrameCommands = showFrameCommands, NativeEventFrameReferences = eventReferences, AllOutfitTransitions = true, GarageOverlayScenePlayback = false, FarmLoaded = false, FullEventPlayback = false });
            monitor.Log("Sebastian audit passed: all three outfits, native poses, portraits, walking, special routes, event commands and outfit transitions. Full scenes remain unverified.", LogLevel.Info);
        }
        catch (Exception ex) { helper.Data.WriteJsonFile("sebastian-checks.json", new { Passed = false, Error = ex.ToString() }); monitor.Log("Sebastian audit failed: " + ex, LogLevel.Error); }
        finally { Game1.season = oldSeason; Game1.currentLocation = oldLocation; }
    }
}
