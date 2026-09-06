using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NpcArtAudit;

internal static class AbigailAdventureAudit
{
    private const string Key = "David.AbigailModern/AbigailAdventureOutfit";
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        var oldSeason = Game1.season;
        var oldLocation = Game1.currentLocation;
        var host = Game1.MasterPlayer;
        var hadFlag = host.modData.TryGetValue(Key, out var oldFlag);
        try
        {
            var appearances = Game1.characterData["Abigail"].Appearance;
            if (appearances.Count(a => a.Id.StartsWith("David.AbigailModern/Adventure")) != 2)
                throw new Exception("Missing quest-controlled adventure appearance entries.");
            Game1.season = Season.Spring;
            var location = new GameLocation("Maps/Town", "Town");
            Game1.currentLocation = location;
            var npc = new NPC(new AnimatedSprite("Characters/Abigail", 0, 16, 32), Vector2.Zero, 2, "Abigail") { currentLocation = location };
            void Equal(Texture2D actual, string asset)
            {
                var expected = helper.GameContent.Load<Texture2D>(asset);
                if (actual.Width != expected.Width || actual.Height != expected.Height) throw new Exception("Texture dimensions differ: " + asset);
                var a = new Color[actual.Width * actual.Height]; var b = new Color[a.Length];
                actual.GetData(a); expected.GetData(b);
                if (!a.SequenceEqual(b)) throw new Exception("Texture pixels differ: " + asset);
            }
            void Check(string suffix)
            {
                Equal(npc.Sprite.Texture, "Characters/Abigail" + suffix);
                Equal(npc.Portrait, "Portraits/Abigail" + suffix);
            }
            host.modData.Remove(Key);
            npc.ChooseAppearance(); Check("");
            host.modData[Key] = "true";
            npc.ChooseAppearance(); Check("_Adventure");
            for (var slot = 0; slot < 10; slot++)
                if (new Dialogue(npc, null, "Adventure portrait.$" + slot).getPortraitIndex() != slot) throw new Exception("Portrait slot mismatch.");
            var texture = npc.Sprite.Texture;
            var pixels = new Color[texture.Width * texture.Height]; texture.GetData(pixels);
            for (var frame = 0; frame < 56; frame++)
            {
                npc.Sprite.CurrentFrame = frame;
                var rect = new Rectangle(frame % 4 * 16, frame / 4 * 32, 16, 32);
                if (npc.Sprite.SourceRect != rect) throw new Exception("Adventure frame layout mismatch.");
                var visible = false;
                for (var y = rect.Y; y < rect.Bottom; y++) for (var x = rect.X; x < rect.Right; x++) visible |= pixels[y * texture.Width + x].A > 0;
                if (visible != (frame < 54)) throw new Exception("Adventure occupied/blank cell mismatch.");
            }
            var device = Game1.graphics.GraphicsDevice;
            var previous = device.GetRenderTargets();
            using var target = new RenderTarget2D(device, 1024, 1024);
            using var batch = new SpriteBatch(device);
            try
            {
                device.SetRenderTarget(target); device.Clear(new Color(35, 45, 58));
                batch.Begin(samplerState: SamplerState.PointClamp);
                for (var frame = 0; frame < 56; frame++) batch.Draw(texture, new Rectangle(frame % 8 * 64 + 8, frame / 8 * 132 + 8, 48, 96), new Rectangle(frame % 4 * 16, frame / 4 * 32, 16, 32), Color.White);
                batch.Draw(npc.Portrait, new Rectangle(600, 16, 384, 960), Color.White);
                batch.End(); device.SetRenderTargets(previous);
                using var file = File.Create(Path.Combine(helper.DirectoryPath, "abigail-adventure-runtime-preview.png"));
                target.SaveAsPng(file, 1024, 1024);
            }
            finally { device.SetRenderTargets(previous); }

            Game1.season = Season.Winter;
            npc.ChooseAppearance(); Check("_Adventure");
            host.modData.Remove(Key);
            npc.ChooseAppearance(); Check("_Winter");
            Game1.season = Season.Summer;
            npc.wearIslandAttire(); Check("_Beach");
            host.modData[Key] = "true";
            npc.ChooseAppearance(); Check("_Adventure");
            host.modData.Remove(Key);
            npc.ChooseAppearance(); Check("_Beach");
            npc.wearNormalClothes(); Check("");
            host.modData[Key] = "false";
            npc.ChooseAppearance(); Check("");
            helper.Data.WriteJsonFile("abigail-adventure-checks.json", new { Passed = true, NativeAppearanceSelections = 9, OccupiedFrames = 54, BlankFrames = 2, PortraitSlots = 10, WinterAndBeachRestored = true, FarmLoaded = false, QuestEventPlayback = false });
            monitor.Log("Abigail adventure audit passed: quest flag selects sprite/portrait, winter and beach restore, 54 poses and 10 expressions. Quest scene playback remains unverified.", LogLevel.Info);
        }
        catch (Exception ex)
        {
            helper.Data.WriteJsonFile("abigail-adventure-checks.json", new { Passed = false, Error = ex.ToString() });
            monitor.Log("Abigail adventure audit failed: " + ex, LogLevel.Error);
        }
        finally
        {
            if (hadFlag) host.modData[Key] = oldFlag!; else host.modData.Remove(Key);
            Game1.season = oldSeason; Game1.currentLocation = oldLocation;
        }
    }
}
