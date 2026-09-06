using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NpcArtAudit;

internal static class SeaMonsterAudit
{
    public static void Run(IModHelper helper,IMonitor monitor)
    {
        var previousLocation=Game1.currentLocation;
        try
        {
            var location=new GameLocation();Game1.currentLocation=location;
            var script=helper.GameContent.Load<Dictionary<string,string>>("Data/Events/Beach").Values.Single(s=>s.Contains("addTemporaryActor SeaMonsterKrobus"));
            var commands=script.Split('/');var scene=new Event();
            // Exercise native actor/animation commands while disabling unrelated event progression.
            helper.Reflection.GetField<bool>(scene,"eventFinished").SetValue(true);
            var create=commands.Single(s=>s.StartsWith("addTemporaryActor SeaMonsterKrobus ")).Split(' ',StringSplitOptions.RemoveEmptyEntries);
            Event.DefaultCommands.AddTemporaryActor(scene,create,new EventContext(scene,location,Game1.currentGameTime,create));
            var actor=scene.getActorByName("SeaMonsterKrobus")??throw new Exception("Native temporary actor absent");
            if(actor.Sprite.SpriteWidth!=32||actor.Sprite.SpriteHeight!=32)throw new Exception("Native frame size changed");
            using var expected=Texture2D.FromFile(Game1.graphics.GraphicsDevice,Path.Combine(Path.GetDirectoryName(helper.DirectoryPath)!,"AbigailModern/assets/SeaMonsterKrobus/characters.png"));
            var a=new Color[actor.Sprite.Texture.Width*actor.Sprite.Texture.Height];var b=new Color[expected.Width*expected.Height];actor.Sprite.Texture.GetData(a);expected.GetData(b);if(!a.SequenceEqual(b))throw new Exception("Native actor pixels differ");
            var checks=new List<object>();
            foreach(var command in commands.Where(s=>s.StartsWith("animate SeaMonsterKrobus ")))
            {
                var args=command.Split(' ',StringSplitOptions.RemoveEmptyEntries);var frames=args.Skip(5).Select(int.Parse).ToArray();
                scene.CurrentCommand=0;
                Event.DefaultCommands.Animate(scene,args,new EventContext(scene,location,Game1.currentGameTime,args));
                if(actor.Sprite.CurrentAnimation==null||!actor.Sprite.CurrentAnimation.Select(f=>f.frame).SequenceEqual(frames))throw new Exception("Native animation frame sequence differs");
                var visited=new HashSet<int>{actor.Sprite.CurrentFrame};var duration=int.Parse(args[4]);
                for(var tick=0;tick<frames.Length*2&&actor.Sprite.CurrentAnimation!=null;tick++)
                {
                    actor.Sprite.animateOnce(new GameTime(Game1.currentGameTime.TotalGameTime+TimeSpan.FromMilliseconds((tick+1)*(duration+1)),TimeSpan.FromMilliseconds(duration+1)));
                    visited.Add(actor.Sprite.CurrentFrame);
                }
                if(frames.Any(f=>!visited.Contains(f)))throw new Exception("Not every native frame was visited");
                checks.Add(new{Command=command,FramesVisited=frames.Distinct().OrderBy(f=>f).ToArray(),NativeSequencePreserved=true});
            }
            if(checks.Count!=5)throw new Exception("Expected five native animation commands");
            helper.Data.WriteJsonFile("sea-monster-checks.json",new{Passed=true,NativeActorCreated=true,SpritePixelsMatch=true,Animations=checks,FarmLoaded=false,FullBeachEventVerified=false});
            monitor.Log("Sea-monster checks passed: native actor and five event animation sequences.",LogLevel.Info);
        }
        catch(Exception ex){helper.Data.WriteJsonFile("sea-monster-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log($"Sea-monster audit failed: {ex}",LogLevel.Error);}
        finally{Game1.currentLocation=previousLocation;}
    }
}
