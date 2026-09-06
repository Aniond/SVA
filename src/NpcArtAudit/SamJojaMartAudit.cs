using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NpcArtAudit;

// The test uses temporary title-screen locations and leaves the saved game untouched.
internal static class SamJojaMartAudit
{
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        var oldSeason = Game1.season; var oldLocation = Game1.currentLocation;
        try
        {
            Game1.season = Season.Spring;
            var location = new GameLocation("Maps/JojaMart", "JojaMart"); Game1.currentLocation = location;
            var npc = new NPC(new AnimatedSprite("Characters/Sam", 0, 16, 32), Vector2.Zero, 2, "Sam") { currentLocation = location };
            Color[] Pixels(Texture2D t) { var p = new Color[t.Width * t.Height]; t.GetData(p); return p; }
            void Equal(Texture2D a, Texture2D b) { if (a.Width != b.Width || a.Height != b.Height || !Pixels(a).SequenceEqual(Pixels(b))) throw new Exception("Appearance texture pixels differ"); }
            void WorkAppearance() { npc.ChooseAppearance(); if (npc.LastAppearanceId != "WorkOutfit") throw new Exception($"Native WorkOutfit not selected at {npc.currentLocation?.Name ?? "null"}"); Equal(npc.Sprite.Texture, helper.GameContent.Load<Texture2D>("Characters/Sam_JojaMart")); Equal(npc.Portrait, helper.GameContent.Load<Texture2D>("Portraits/Sam_JojaMart")); }
            WorkAppearance();
            var texture = npc.Sprite.Texture; var portrait = npc.Portrait;
            if (texture.Width != 64 || texture.Height != 448 || portrait.Width != 128 || portrait.Height != 384) throw new Exception("Native atlas dimensions differ");
            var pixels = Pixels(texture); var portraitPixels = Pixels(portrait);
            var basePixels = Pixels(helper.GameContent.Load<Texture2D>("Characters/Sam"));
            var basePortraits = Pixels(helper.GameContent.Load<Texture2D>("Portraits/Sam"));
            var reuse = new[] {16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,39,43,44,45,46,47,48,49,50,51,52,53,54};
            foreach (var frame in reuse) for (var y = 0; y < 32; y++) for (var x = 0; x < 16; x++) { var p = (frame / 4 * 32 + y) * 64 + frame % 4 * 16 + x; if (pixels[p] != basePixels[p]) throw new Exception("Reused Base pose differs: " + frame); }
            for (var frame = 0; frame < 55; frame++)
            {
                npc.Sprite.CurrentFrame = frame; var r = new Rectangle(frame % 4 * 16, frame / 4 * 32, 16, 32);
                if (npc.Sprite.SourceRect != r) throw new Exception("Frame rectangle differs: " + frame);
                var count = 0; for (var y = r.Y; y < r.Bottom; y++) for (var x = r.X; x < r.Right; x++) if (pixels[y * 64 + x].A > 0) count++;
                if (count == 0) throw new Exception("Empty frame: " + frame);
            }
            for (var y = 0; y < 32; y++) for (var x = 0; x < 16; x++) if (pixels[(416 + y) * 64 + 48 + x] != Color.White) throw new Exception("White placeholder differs");
            for (var i = 0; i < 12; i++)
            {
                if (new Dialogue(npc, null, "Joja expression.$" + i).getPortraitIndex() != i) throw new Exception("Portrait index differs");
                var count = 0; for (var y = 0; y < 64; y++) for (var x = 0; x < 64; x++) { var p = (i / 2 * 64 + y) * 128 + i % 2 * 64 + x; if (portraitPixels[p].A > 0) count++; if (new[] {6,10,11}.Contains(i) && portraitPixels[p] != basePortraits[p]) throw new Exception("Reused portrait differs: " + i); }
                if (count == 0) throw new Exception("Empty portrait: " + i);
            }
            var walkingSteps = 0; var walker = new AnimatedSprite("Characters/Sam_JojaMart", 0, 16, 32);
            for (var direction = 0; direction < 4; direction++)
            {
                walker.CurrentFrame = direction * 4; walker.timer = 0;
                for (var step = 1; step <= 8; step++) { var time = new GameTime(TimeSpan.FromMilliseconds(step * 180), TimeSpan.FromMilliseconds(180)); switch (direction) { case 0: walker.AnimateDown(time); break; case 1: walker.AnimateRight(time); break; case 2: walker.AnimateUp(time); break; case 3: walker.AnimateLeft(time); break; } if (walker.CurrentFrame != direction * 4 + step % 4) throw new Exception("Walking frame differs"); walkingSteps++; }
            }
            var sections = helper.GameContent.Load<Dictionary<string,string>>("Data/animationDescriptions")["sam_work"].Split('/').Take(3).Select(s => s.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray()).ToArray();
            var workSteps = 0;
            foreach (var frames in sections)
            {
                npc.Sprite.loop = true; npc.Sprite.setCurrentAnimation(frames.Select(f => new FarmerSprite.AnimationFrame(f, 150)).ToList());
                for (var step = 1; step <= frames.Length * 2; step++) { npc.Sprite.animateOnce(new GameTime(TimeSpan.FromMilliseconds(step * 150), TimeSpan.FromMilliseconds(150))); if (npc.Sprite.CurrentFrame != frames[step % frames.Length]) throw new Exception("Work animation differs"); workSteps++; }
            }
            npc.Sprite.StopAnimation();
            var behavior = helper.Reflection.GetMethod(npc, "getRouteEndBehaviorFunction").Invoke<Delegate>("sam_work", null); if (behavior == null) throw new Exception("Missing work route"); behavior.DynamicInvoke(npc, location);
            if (!npc.Sprite.currentAnimation.Select(f => f.frame).SequenceEqual(sections[0])) throw new Exception("Work intro differs");
            for (var step = 1; step <= sections[0].Length; step++) npc.Sprite.animateOnce(new GameTime(TimeSpan.FromMilliseconds(step * 100), TimeSpan.FromMilliseconds(100)));
            if (!npc.Sprite.currentAnimation.Select(f => f.frame).SequenceEqual(sections[1])) throw new Exception("Work route loop differs");
            var device = Game1.graphics.GraphicsDevice; var previous = device.GetRenderTargets(); using var target = new RenderTarget2D(device, 1024, 1216); using var batch = new SpriteBatch(device);
            try { device.SetRenderTarget(target); device.Clear(new Color(35,45,58)); batch.Begin(samplerState: SamplerState.PointClamp); for (var i = 0; i < 56; i++) batch.Draw(texture, new Rectangle(i % 8 * 64 + 8, i / 8 * 132 + 8, 48, 96), new Rectangle(i % 4 * 16, i / 4 * 32, 16, 32), Color.White); batch.Draw(portrait, new Rectangle(600,16,384,1152), Color.White); batch.End(); device.SetRenderTargets(previous); using var file = File.Create(Path.Combine(helper.DirectoryPath,"sam-jojamart-runtime-preview.png")); target.SaveAsPng(file,target.Width,target.Height); } finally { device.SetRenderTargets(previous); }
            Game1.season = Season.Winter; WorkAppearance(); // Work precedence -1000 beats Winter -100.
            var museum = new GameLocation("Maps/ArchaeologyHouse", "ArchaeologyHouse"); Game1.currentLocation = museum; npc.currentLocation = museum; WorkAppearance();
            Game1.season = Season.Spring; WorkAppearance();
            var town = new GameLocation("Maps/Town", "Town"); Game1.currentLocation = town; npc.currentLocation = town; npc.ChooseAppearance(); Equal(npc.Sprite.Texture,helper.GameContent.Load<Texture2D>("Characters/Sam")); Equal(npc.Portrait,helper.GameContent.Load<Texture2D>("Portraits/Sam"));
            Game1.season = Season.Winter; npc.ChooseAppearance(); Equal(npc.Sprite.Texture,helper.GameContent.Load<Texture2D>("Characters/Sam_Winter")); Equal(npc.Portrait,helper.GameContent.Load<Texture2D>("Portraits/Sam_Winter"));
            helper.Data.WriteJsonFile("sam-jojamart-checks.json",new {Passed=true,OccupiedPoses=55,PreservedPlaceholders=1,PortraitSlots=12,ReusedBasePoses=33,ReusedBasePortraits=3,NativeWalkingSteps=walkingSteps,NativeWorkSteps=workSteps,NativeWorkRouteIntroToLoop=true,JojaAndMuseumSpringWinter=true,EverydayAndWinterRestored=true,FarmLoaded=false,FullEventPlayback=false});
            monitor.Log("Sam Joja audit passed; full saved-game scenes remain unverified.",LogLevel.Info);
        }
        catch(Exception ex) { helper.Data.WriteJsonFile("sam-jojamart-checks.json",new {Passed=false,Error=ex.ToString()}); monitor.Log("Sam Joja audit failed: "+ex,LogLevel.Error); }
        finally { Game1.season=oldSeason; Game1.currentLocation=oldLocation; }
    }
}
