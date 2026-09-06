using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;

namespace NpcArtAudit;

internal static class GilAudit
{
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        var menuField = typeof(Game1).GetField("_activeClickableMenu", BindingFlags.Static | BindingFlags.NonPublic)!;
        var originalMenu = Game1.activeClickableMenu;
        var originalLocation = Game1.currentLocation;
        var originalDialogue = Game1.dialogueUp;
        var originalMove = Game1.player.CanMove;
        var originalAfter = Game1.afterDialogues;
        try
        {
            var map = helper.GameContent.Load<xTile.Map>("Maps/AdventureGuild");
            var tiles = new List<object>();
            foreach (var layer in map.Layers)
                for (var y = 0; y < layer.LayerHeight; y++)
                    for (var x = 0; x < layer.LayerWidth; x++)
                    {
                        var tile = layer.Tiles[x,y];
                        if (tile is xTile.Tiles.AnimatedTile animation)
                            tiles.Add(new { Layer=layer.Id, X=x, Y=y, Frames=animation.TileFrames.Select(t=>new {Index=t.TileIndex,Texture=t.TileSheet.ImageSource}).ToArray() });
                    }
            helper.Data.WriteJsonFile("guild-animation-frames.json", tiles);
            var guild = new AdventureGuild();
            Game1.currentLocation = guild;
            var actual = guild.Gil.Portrait;
            var expected = helper.GameContent.Load<Texture2D>("Portraits/Gil");
            var a = new Color[actual.Width*actual.Height]; var b = new Color[expected.Width*expected.Height];
            actual.GetData(a); expected.GetData(b);
            if (!a.SequenceEqual(b)) throw new Exception("Gil's native actor portrait differs.");
            // Use the native no-reward dialogue paths directly; no reward collection is performed.
            foreach (var key in new[] {"ComeBackLater","Snoring"})
            {
                menuField.SetValue(null,null); Game1.dialogueUp=false;
                Game1.DrawDialogue(guild.Gil,"Characters\\Dialogue\\Gil:"+key);
                var box=Game1.activeClickableMenu as DialogueBox ?? throw new Exception("Gil dialogue did not open.");
                if (box.characterDialogue?.speaker != guild.Gil) throw new Exception("Wrong native speaker.");
                box.transitioning=false;box.transitionInitialized=true;
                box.characterIndexInDialogue=box.getCurrentString().Length;
                var device=Game1.graphics.GraphicsDevice;
                var targets=device.GetRenderTargets();
                using var target=new RenderTarget2D(device,Game1.uiViewport.Width,Game1.uiViewport.Height);
                using var batch=new SpriteBatch(device);
                try
                {
                    device.SetRenderTarget(target);device.Clear(new Color(35,45,58));
                    batch.Begin(samplerState:SamplerState.PointClamp);box.draw(batch);batch.End();
                    device.SetRenderTargets(targets);
                    using var stream=File.Create(Path.Combine(helper.DirectoryPath,"gil-"+key+"-preview.png"));
                    target.SaveAsPng(stream,target.Width,target.Height);
                }
                finally {device.SetRenderTargets(targets);}
            }
            helper.Data.WriteJsonFile("gil-interaction-checks.json",new {Passed=true,NativeActorPortrait=true,NativeDialogue=true,AnimatedTiles=tiles.Count,RewardFlowTested=false,FullSceneRendered=false,FarmLoaded=false});
            monitor.Log($"Gil checks passed: native portrait and two dialogue routes; exported {tiles.Count} animated guild tiles.",LogLevel.Info);
        }
        catch(Exception ex)
        {
            helper.Data.WriteJsonFile("gil-interaction-checks.json",new {Passed=false,Error=ex.ToString()});
            monitor.Log($"Gil audit failed: {ex}",LogLevel.Error);
        }
        finally
        {
            menuField.SetValue(null,originalMenu);Game1.currentLocation=originalLocation;
            Game1.dialogueUp=originalDialogue;Game1.player.CanMove=originalMove;Game1.afterDialogues=originalAfter;
        }
    }
}
