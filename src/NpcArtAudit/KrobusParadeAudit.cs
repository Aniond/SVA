using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NpcArtAudit;

internal static class KrobusParadeAudit
{
    public static void Run(IModHelper helper,IMonitor monitor)
    {
        var previousLocation=Game1.currentLocation;
        var previousPlayerLocation=Game1.player.currentLocation;
        var previousEarnings=Game1.MasterPlayer.team.totalMoneyEarned.Value;
        try
        {
            var location=new GameLocation();location.name.Value="Beach";Game1.currentLocation=location;Game1.player.currentLocation=location;
            Game1.MasterPlayer.team.totalMoneyEarned.Value=100000000;
            var scene=new Event();var args=new[]{"specificTemporarySprite","krobusraven"};
            Event.DefaultCommands.SpecificTemporarySprite(scene,args,new EventContext(scene,location,Game1.currentGameTime,args));
            var sprites=location.TemporarySprites.Take(3).ToArray();if(sprites.Length!=3)throw new Exception("Native parade sprites absent");
            var results=new List<object>();
            var targetTexture=Path.Combine(Path.GetDirectoryName(helper.DirectoryPath)!,"AbigailModern");
            var expected=Texture2D.FromFile(Game1.graphics.GraphicsDevice,Path.Combine(targetTexture,"assets/KrobusRaven/characters.png"));
            try
            {
                for(var row=0;row<3;row++)
                {
                    var sprite=sprites[row];var count=row==2?4:5;var height=row==2?39:32;var interval=row==1?30:100;
                    if(sprite.animationLength!=count || sprite.interval!=interval || sprite.sourceRect!=new Rectangle(0,row*32,32,height))throw new Exception("Native frame layout/timing changed");
                    var delay=sprite.delayBeforeAnimationStart;if(delay!=(row==0?0:row==1?8000:15000))throw new Exception("Native start delay changed");
                    var actual=new Color[sprite.texture.Width*sprite.texture.Height];var packaged=new Color[expected.Width*expected.Height];sprite.texture.GetData(actual);expected.GetData(packaged);if(!actual.SequenceEqual(packaged))throw new Exception("Runtime parade pixels differ");
                    var frames=new HashSet<int>{sprite.sourceRect.X/32};var startX=sprite.position.X;
                    // Advance isolated native animation past its delay without progressing the real event.
                    sprite.delayBeforeAnimationStart=0;sprite.startSound=null;
                    for(var tick=1;tick<=count*3;tick++)
                    {
                        sprite.update(new GameTime(Game1.currentGameTime.TotalGameTime+TimeSpan.FromMilliseconds(tick*(interval+1)),TimeSpan.FromMilliseconds(interval+1)));
                        if(sprite.sourceRect.Y!=row*32 || sprite.sourceRect.Height!=height || sprite.sourceRect.X<0 || sprite.sourceRect.Right>count*32)throw new Exception("Animation left its registered frame region");
                        frames.Add(sprite.sourceRect.X/32);
                    }
                    if(frames.Count!=count || sprite.position.X>=startX)throw new Exception("Native animation did not visit every frame and move left");
                    results.Add(new{Row=row,FrameCount=count,FramesVisited=frames.OrderBy(x=>x).ToArray(),NativeDelay=delay,NativeInterval=interval,MovesLeft=true,PixelsMatch=true});
                }
                var pig=location.TemporarySprites.Single(s=>s.sourceRect==new Rectangle(125,108,34,50));
                if(pig.interval!=1090||pig.animationLength!=1||pig.motion.X!=-2||!pig.yPeriodic||pig.yPeriodicLoopTime!=3000||pig.yPeriodicRange!=8||pig.delayBeforeAnimationStart<30000)throw new Exception("Native pig parade parameters differ");
                var pigPixels=new Color[pig.texture.Width*pig.texture.Height];var expectedPixels=new Color[expected.Width*expected.Height];pig.texture.GetData(pigPixels);expected.GetData(expectedPixels);if(!pigPixels.SequenceEqual(expectedPixels))throw new Exception("Pig runtime pixels differ");
                var pigStartX=pig.position.X;var pigStartY=pig.position.Y;var bobbed=false;pig.delayBeforeAnimationStart=0;pig.startSound=null;
                for(var tick=1;tick<=40;tick++){pig.update(new GameTime(TimeSpan.FromMilliseconds(tick*100),TimeSpan.FromMilliseconds(100)));if(pig.sourceRect!=new Rectangle(125,108,34,50))throw new Exception("Pig frame changed");bobbed|=Math.Abs(pig.position.Y-pigStartY)>0.01f;}
                if(pig.position.X>=pigStartX||!bobbed)throw new Exception("Native pig movement did not advance");
            }
            finally{expected.Dispose();}
            helper.Data.WriteJsonFile("krobus-parade-checks.json",new{Passed=true,Animations=results,FullTimedEventVerified=false,LowerPigUpdated=true,PigNativeBranch=true,PigMovementSteps=40,PigPixelsMatch=true,FarmLoaded=false});
            monitor.Log("Krobus parade checks passed: three native animations, all fourteen frames, layout and movement.",LogLevel.Info);
        }
        catch(Exception ex){helper.Data.WriteJsonFile("krobus-parade-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log($"Krobus parade audit failed: {ex}",LogLevel.Error);}
        finally{Game1.MasterPlayer.team.totalMoneyEarned.Value=previousEarnings;Game1.currentLocation=previousLocation;Game1.player.currentLocation=previousPlayerLocation;}
    }
}

