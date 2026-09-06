using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NpcArtAudit;

// Native outfit, movement, special-pose and expression verification.
internal static class PamAudit
{
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        var oldSeason=Game1.season; var oldLocation=Game1.currentLocation;
        try
        {
            var location=new GameLocation("Maps/Town","Town"); Game1.currentLocation=location; Game1.season=Season.Spring;
            Color[] Pixels(Texture2D t) {var p=new Color[t.Width*t.Height];t.GetData(p);return p;}
            void Equal(Texture2D a,Texture2D b) {if(a.Width!=b.Width||a.Height!=b.Height||!Pixels(a).SequenceEqual(Pixels(b)))throw new Exception("Selected appearance pixels differ");}
            NPC Create() => new(new AnimatedSprite("Characters/Pam",0,16,32),Vector2.Zero,2,"Pam"){currentLocation=location};
            var npc=Create();var walkingSteps=0;var specialSteps=0;var routes=0;var poseCount=0;var portraitCount=0;var placeholderCount=0;var tallRectangles=0;var eventShowFrameCommands=0;var eventAnimateCommands=0;var eventAnimationSteps=0;
            foreach(var variant in new[]{"Base","Winter","Beach"})
            {
                Game1.season=variant=="Winter"?Season.Winter:Season.Spring;
                if(variant=="Beach")npc.wearIslandAttire();else {npc.wearNormalClothes();npc.ChooseAppearance();}
                var suffix=variant=="Base"?"":"_"+variant;var spriteAsset="Characters/Pam"+suffix;var portraitAsset="Portraits/Pam"+suffix;
                Equal(npc.Sprite.Texture,helper.GameContent.Load<Texture2D>(spriteAsset));Equal(npc.Portrait,helper.GameContent.Load<Texture2D>(portraitAsset));
                var texture=npc.Sprite.Texture;var portrait=npc.Portrait;var frameCount=variant=="Beach"?16:37;
                if(texture.Width!=64||texture.Height!=(variant=="Beach"?128:320)||portrait.Width!=128||portrait.Height!=192)throw new Exception("Native atlas size differs");
                var p=Pixels(texture);var pp=Pixels(portrait);
                for(var i=0;i<frameCount;i++){npc.Sprite.CurrentFrame=i;var r=new Rectangle(i%4*16,i/4*32,16,32);if(npc.Sprite.SourceRect!=r)throw new Exception("Frame rectangle differs");var count=0;for(var y=r.Y;y<r.Bottom;y++)for(var x=r.X;x<r.Right;x++)if(p[y*64+x].A>0)count++;if(count==0)throw new Exception("Empty pose");if(i<20||i>23)poseCount++;}
                if(variant!="Beach")for(var i=37;i<40;i++){for(var y=0;y<32;y++)for(var x=0;x<16;x++)if(p[(i/4*32+y)*64+i%4*16+x]!=Color.White)throw new Exception("Native solid placeholder differs");placeholderCount++;}
                for(var i=0;i<5;i++){if(new Dialogue(npc,null,"Expression.$"+i).getPortraitIndex()!=i)throw new Exception("Portrait index differs");var count=0;for(var y=0;y<64;y++)for(var x=0;x<64;x++)if(pp[(i/2*64+y)*128+i%2*64+x].A>0)count++;if(count==0)throw new Exception("Empty portrait");portraitCount++;}
                for(var y=128;y<192;y++)for(var x=64;x<128;x++)if(pp[y*128+x]!=new Color(255,251,244,255))throw new Exception("Native portrait placeholder5 changed");
                var walker=new AnimatedSprite(spriteAsset,0,16,32);
                for(var direction=0;direction<4;direction++){walker.CurrentFrame=direction*4;walker.timer=0;for(var step=1;step<=8;step++){var time=new GameTime(TimeSpan.FromMilliseconds(step*180),TimeSpan.FromMilliseconds(180));switch(direction){case 0:walker.AnimateDown(time);break;case 1:walker.AnimateRight(time);break;case 2:walker.AnimateUp(time);break;case 3:walker.AnimateLeft(time);break;}var f=direction*4+step%4;if(walker.CurrentFrame!=f||walker.SourceRect!=new Rectangle(f%4*16,f/4*32,16,32))throw new Exception("Walking transition differs");walkingSteps++;}}
                if(variant!="Beach")foreach(var key in new[]{"pam_sit_down","pam_sleep"})
                {
                    var sections=helper.GameContent.Load<Dictionary<string,string>>("Data/animationDescriptions")[key].Split('/').Take(3).Select(s=>s.Split(' ',StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray()).ToArray();
                    foreach(var frames in sections){if(frames.Any(f=>f<0||f>=frameCount))throw new Exception("Invalid native special frame");npc.Sprite.loop=true;npc.Sprite.setCurrentAnimation(frames.Select(f=>new FarmerSprite.AnimationFrame(f,150)).ToList());for(var step=1;step<=frames.Length*2;step++){npc.Sprite.animateOnce(new GameTime(TimeSpan.FromMilliseconds(step*150),TimeSpan.FromMilliseconds(150)));if(npc.Sprite.CurrentFrame!=frames[step%frames.Length])throw new Exception("Special transition differs");specialSteps++;}}
                    var routeNpc=Create();routeNpc.ChooseAppearance();var behavior=helper.Reflection.GetMethod(routeNpc,"getRouteEndBehaviorFunction").Invoke<Delegate>(key,null);if(behavior==null)throw new Exception("Native route missing");behavior.DynamicInvoke(routeNpc,location);if(!routeNpc.Sprite.currentAnimation.Select(f=>f.frame).SequenceEqual(sections[0]))throw new Exception("Route intro differs");for(var step=1;step<=sections[0].Length;step++)routeNpc.Sprite.animateOnce(new GameTime(TimeSpan.FromMilliseconds(step*100),TimeSpan.FromMilliseconds(100)));if(!routeNpc.Sprite.currentAnimation.Select(f=>f.frame).SequenceEqual(sections[1]))throw new Exception("Route intro-to-loop differs");routes++;
                }

                if(variant!="Beach")
                {
                    var tall=new AnimatedSprite(spriteAsset,8,16,64);for(var f=8;f<12;f++){tall.CurrentFrame=f;if(tall.SourceRect!=new Rectangle((f-8)*16,128,16,64))throw new Exception("Tall prop rectangle differs");tallRectangles++;}
                    var scene=new Event();scene.actors.Add(npc);helper.Reflection.GetField<bool>(scene,"eventFinished").SetValue(true);var shown=new List<int>();var eventSets=new List<int[]>();
                    foreach(var map in new[]{"Saloon","Town","Trailer","Trailer_Big"})foreach(var script in helper.GameContent.Load<Dictionary<string,string>>("Data/Events/"+map).Values)foreach(var command in script.Split('/'))
                    {
                        var args=command.Split(' ',StringSplitOptions.RemoveEmptyEntries);if(args.Length<3||args[1]!="Pam")continue;
                        if(args[0]=="showFrame")
                        {
                            var frame=int.Parse(args[2]);if(frame<0||frame>=37||(frame>=20&&frame<=23))throw new Exception("Native event references missing pose");npc.Sprite.StopAnimation();npc.Sprite.CurrentFrame=frame==0?1:0;Event.DefaultCommands.ShowFrame(scene,args,new EventContext(scene,location,Game1.currentGameTime,args));if(npc.Sprite.CurrentFrame!=frame||npc.Sprite.SourceRect!=new Rectangle(frame%4*16,frame/4*32,16,32))throw new Exception("Native ShowFrame differs");shown.Add(frame);eventShowFrameCommands++;
                        }
                        else if(args[0]=="animate")
                        {
                            var frames=args.Skip(5).Select(int.Parse).ToArray();var duration=int.Parse(args[4]);if(frames.Any(f=>f<0||f>=37||(f>=20&&f<=23)))throw new Exception("Native Animate references missing pose");npc.Sprite.StopAnimation();Event.DefaultCommands.Animate(scene,args,new EventContext(scene,location,Game1.currentGameTime,args));if(!npc.Sprite.currentAnimation.Select(f=>f.frame).SequenceEqual(frames)||npc.Sprite.loop!=bool.Parse(args[3]))throw new Exception("Native event animation setup differs");npc.Sprite.CurrentFrame=frames[0];npc.Sprite.currentAnimationIndex=0;npc.Sprite.timer=0;for(var step=1;step<=frames.Length*2;step++){npc.Sprite.animateOnce(new GameTime(TimeSpan.FromMilliseconds(step*duration),TimeSpan.FromMilliseconds(duration)));if(npc.Sprite.CurrentFrame!=frames[step%frames.Length])throw new Exception("Event timed transition differs");eventAnimationSteps++;}eventSets.Add(frames);eventAnimateCommands++;
                        }
                    }
                    foreach(var f in new[]{25,32,35,36})if(!shown.Contains(f))throw new Exception("Expected event pose absent: "+f);
                    foreach(var seq in new[]{new[]{26,27},new[]{33,34},new[]{28,29}})if(!eventSets.Any(f=>f.SequenceEqual(seq)))throw new Exception("Native crying or drinking sequence absent");
                }
                var device=Game1.graphics.GraphicsDevice;var previous=device.GetRenderTargets();using var target=new RenderTarget2D(device,768,1344);using var batch=new SpriteBatch(device);
                try{device.SetRenderTarget(target);device.Clear(new Color(35,45,58));batch.Begin(samplerState:SamplerState.PointClamp);batch.Draw(texture,new Rectangle(8,8,256,texture.Height*4),Color.White);batch.Draw(portrait,new Rectangle(300,16,384,portrait.Height*3),Color.White);batch.End();device.SetRenderTargets(previous);using var file=File.Create(Path.Combine(helper.DirectoryPath,"pam-"+variant.ToLowerInvariant()+"-runtime-preview.png"));target.SaveAsPng(file,target.Width,target.Height);}finally{device.SetRenderTargets(previous);}
            }
            Game1.season=Season.Spring;npc.wearNormalClothes();npc.ChooseAppearance();Equal(npc.Sprite.Texture,helper.GameContent.Load<Texture2D>("Characters/Pam"));Equal(npc.Portrait,helper.GameContent.Load<Texture2D>("Portraits/Pam"));
            helper.Data.WriteJsonFile("pam-checks.json",new{Passed=true,OccupiedPoses=poseCount,PreservedPlaceholders=placeholderCount,PortraitSlots=portraitCount,PreservedPortraitPlaceholders=3,TallAtlasRectangles=tallRectangles,NativeEventShowFrameCommands=eventShowFrameCommands,NativeEventAnimateCommands=eventAnimateCommands,NativeEventAnimationSteps=eventAnimationSteps,NativeWalkingSteps=walkingSteps,NativeSpecialSteps=specialSteps,NativeRouteTransitions=routes,NativeWinterBeachTransitions=true,EverydayRestored=true,FarmLoaded=false,FullEventPlayback=false});monitor.Log("Pam art audit passed; full saved-game scenes remain unverified.",LogLevel.Info);
        }
        catch(Exception ex){helper.Data.WriteJsonFile("pam-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log("Pam audit failed: "+ex,LogLevel.Error);}
        finally{Game1.season=oldSeason;Game1.currentLocation=oldLocation;}
    }
}


