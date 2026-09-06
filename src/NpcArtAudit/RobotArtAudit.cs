using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Minigames;

namespace NpcArtAudit;
internal static class RobotArtAudit
{
    public static void Run(IModHelper helper,IMonitor monitor)
    {
        var oldLocation=Game1.currentLocation;var device=Game1.graphics.GraphicsDevice;var oldTargets=device.GetRenderTargets();
        try
        {
            var location=new GameLocation();Game1.currentLocation=location;
            var scene=new Event();var npc=new NPC(new AnimatedSprite("Characters/robot",0,35,42),Vector2.Zero,2,"robot"){currentLocation=location};scene.actors.Add(npc);helper.Reflection.GetField<bool>(scene,"eventFinished").SetValue(true);
            var texture=npc.Sprite.Texture;if(texture.Width!=140||texture.Height!=126)throw new Exception("Robot native dimensions differ");
            var pixels=new Color[140*126];texture.GetData(pixels);
            for(var f=0;f<12;f++){npc.Sprite.CurrentFrame=f;var r=new Rectangle(f%4*35,f/4*42,35,42);if(npc.Sprite.SourceRect!=r)throw new Exception("Robot frame rectangle differs");var visible=0;for(var y=r.Y;y<r.Bottom;y++)for(var x=r.X;x<r.Right;x++)if(pixels[y*140+x].A>0)visible++;if(visible<10)throw new Exception("Robot pose is empty");}
            var script=helper.GameContent.Load<Dictionary<string,string>>("Data/Events/ScienceHouse").Single(p=>p.Key.StartsWith("10/",StringComparison.Ordinal)).Value;
            var shown=0;var animated=0;var steps=0;
            foreach(var args in Event.ParseCommands(script).Select(ArgUtility.SplitBySpaceQuoteAware))
            {
                if(args.Length<3||args[1]!="robot")continue;
                if(args[0]=="showFrame")
                {
                    var f=int.Parse(args[2]);npc.Sprite.StopAnimation();npc.Sprite.CurrentFrame=f==0?1:0;Event.DefaultCommands.ShowFrame(scene,args,new EventContext(scene,location,Game1.currentGameTime,args));if(npc.Sprite.CurrentFrame!=f||npc.Sprite.SourceRect!=new Rectangle(f%4*35,f/4*42,35,42))throw new Exception("Robot native ShowFrame differs");shown++;
                }
                else if(args[0]=="animate")
                {
                    var frames=args.Skip(5).Select(int.Parse).ToArray();var duration=int.Parse(args[4]);npc.Sprite.StopAnimation();npc.Sprite.CurrentFrame=0;
                    Event.DefaultCommands.Animate(scene,args,new EventContext(scene,location,Game1.currentGameTime,args));
                    if(!npc.Sprite.currentAnimation.Select(f=>f.frame).SequenceEqual(frames)||npc.Sprite.loop!=bool.Parse(args[3]))throw new Exception("Robot native animation setup differs");
                    npc.Sprite.currentAnimationIndex=0;npc.Sprite.CurrentFrame=frames[0];npc.Sprite.timer=duration;
                    for(var step=1;step<frames.Length;step++){npc.Sprite.animateOnce(new GameTime(TimeSpan.FromMilliseconds(step*duration),TimeSpan.FromMilliseconds(duration)));if(npc.Sprite.CurrentFrame!=frames[step])throw new Exception("Robot native timed frame differs");steps++;}
                    if(!npc.Sprite.animateOnce(new GameTime(TimeSpan.FromMilliseconds(frames.Length*duration),TimeSpan.FromMilliseconds(duration)))||npc.Sprite.CurrentAnimation!=null)throw new Exception("Robot nonlooping animation did not finish");animated++;
                }
            }
            var launchArgs=new[]{"specificTemporarySprite","robot"};Event.DefaultCommands.SpecificTemporarySprite(scene,launchArgs,new EventContext(scene,location,Game1.currentGameTime,launchArgs));
            var launch=location.TemporarySprites.FirstOrDefault()??throw new Exception("Native launch actor missing");
            if(launch.sourceRect!=new Rectangle(35,42,35,42)||launch.acceleration.Y!=-0.01f)throw new Exception("Native launch pose differs");
            var flight=new RobotBlastoff{backgroundPosition=-100,robotPosition=new Vector2(160,120)};
            var cursors=helper.GameContent.Load<Texture2D>("LooseSprites/Cursors");var cp=new Color[cursors.Width*cursors.Height];cursors.GetData(cp);var flightStates=0;
            for(var frame=0;frame<4;frame++)
            {
                flight.millisecondsSinceStart=frame*50;using var target=new RenderTarget2D(device,400,400);using var batch=new SpriteBatch(device);
                device.SetRenderTarget(target);device.Clear(Color.Magenta);flight.draw(batch);device.SetRenderTargets(oldTargets);var rendered=new Color[400*400];target.GetData(rendered);var aligned=false;
                for(var dy=-1;dy<=1;dy++)for(var dx=-1;dx<=1;dx++)
                {
                    var matches=true;for(var y=0;y<27&&matches;y++)for(var x=0;x<15;x++){var expected=cp[(1827+y)*cursors.Width+206+frame*15+x];if(expected.A==255&&rendered[(120+dy+y*4+2)*400+160+dx+x*4+2]!=expected){matches=false;break;}}aligned|=matches;
                }
                if(!aligned)throw new Exception("Native RobotBlastoff frame pixels differ: "+frame);flightStates++;
                using var file=File.Create(Path.Combine(helper.DirectoryPath,"robot-flight-"+frame+"-runtime-preview.png"));target.SaveAsPng(file,400,400);
            }
            using(var target=new RenderTarget2D(device,1000,800))using(var batch=new SpriteBatch(device))
            {
                device.SetRenderTarget(target);device.Clear(new Color(35,45,58));batch.Begin(samplerState:SamplerState.PointClamp);batch.Draw(texture,new Rectangle(8,8,560,504),Color.White);batch.Draw(helper.GameContent.Load<Texture2D>("Portraits/robot"),new Rectangle(600,8,384,576),Color.White);batch.End();device.SetRenderTargets(oldTargets);using var file=File.Create(Path.Combine(helper.DirectoryPath,"robot-art-runtime-preview.png"));target.SaveAsPng(file,1000,800);
            }
            helper.Data.WriteJsonFile("robot-art-checks.json",new{Passed=true,Poses=12,Portraits=6,NativeShowFrameCommands=shown,NativeAnimateCommands=animated,NativeAnimationSteps=steps,NativeLaunchPoseVerified=true,NativeFlightDrawStates=flightStates,FarmLoaded=false,FullEventPlayback=false});
            monitor.Log("MarILDA art passed native event poses, animations and four RobotBlastoff draw frames.",LogLevel.Info);
        }
        catch(Exception ex){helper.Data.WriteJsonFile("robot-art-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log("Robot art audit failed: "+ex,LogLevel.Error);}
        finally{device.SetRenderTargets(oldTargets);Game1.currentLocation=oldLocation;}
    }
}
