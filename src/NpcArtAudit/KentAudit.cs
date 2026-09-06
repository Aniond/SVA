using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NpcArtAudit;

// Verifies native movement, sleep animation, portraits and season changes.
internal static class KentAudit
{
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        var oldSeason=Game1.season; var oldLocation=Game1.currentLocation;
        try
        {
            var location=new GameLocation("Maps/Town","Town"); Game1.currentLocation=location; Game1.season=Season.Spring;
            Color[] Pixels(Texture2D t) {var p=new Color[t.Width*t.Height];t.GetData(p);return p;}
            void Equal(Texture2D a,Texture2D b) {if(a.Width!=b.Width||a.Height!=b.Height||!Pixels(a).SequenceEqual(Pixels(b)))throw new Exception("Selected appearance pixels differ");}
            NPC Create() => new(new AnimatedSprite("Characters/Kent",0,16,32),Vector2.Zero,2,"Kent"){currentLocation=location};
            var npc=Create();var walkingSteps=0;var specialSteps=0;var routes=0;var poseCount=0;var portraitCount=0;var placeholderCount=0;var shownCommands=0;var animatedCommands=0;var timedEventSteps=0;
            foreach(var variant in new[]{"Base","Winter"})
            {
                Game1.season=variant=="Winter"?Season.Winter:Season.Spring;
                if(variant=="Beach")npc.wearIslandAttire();else {npc.wearNormalClothes();npc.ChooseAppearance();}
                var suffix=variant=="Base"?"":"_"+variant;var spriteAsset="Characters/Kent"+suffix;var portraitAsset="Portraits/Kent"+suffix;
                Equal(npc.Sprite.Texture,helper.GameContent.Load<Texture2D>(spriteAsset));Equal(npc.Portrait,helper.GameContent.Load<Texture2D>(portraitAsset));
                var texture=npc.Sprite.Texture;var portrait=npc.Portrait;var frameCount=19;
                if(texture.Width!=64||texture.Height!=160||portrait.Width!=128||portrait.Height!=192)throw new Exception("Native atlas size differs");
                var p=Pixels(texture);var pp=Pixels(portrait);
                for(var i=0;i<frameCount;i++){npc.Sprite.CurrentFrame=i;var r=new Rectangle(i%4*16,i/4*32,16,32);if(npc.Sprite.SourceRect!=r)throw new Exception("Frame rectangle differs");var count=0;for(var y=r.Y;y<r.Bottom;y++)for(var x=r.X;x<r.Right;x++)if(p[y*64+x].A>0)count++;if(count==0)throw new Exception("Empty pose");poseCount++;}
                for(var y=128;y<160;y++)for(var x=48;x<64;x++)if(p[y*64+x]!=new Color(244,128,90,255))throw new Exception("Native placeholder19 changed");placeholderCount++;
                for(var i=0;i<6;i++){if(new Dialogue(npc,null,"Expression.$"+i).getPortraitIndex()!=i)throw new Exception("Portrait index differs");var count=0;for(var y=0;y<64;y++)for(var x=0;x<64;x++)if(pp[(i/2*64+y)*128+i%2*64+x].A>0)count++;if(count==0)throw new Exception("Empty portrait");portraitCount++;}
                var walker=new AnimatedSprite(spriteAsset,0,16,32);
                for(var direction=0;direction<4;direction++){walker.CurrentFrame=direction*4;walker.timer=0;for(var step=1;step<=8;step++){var time=new GameTime(TimeSpan.FromMilliseconds(step*180),TimeSpan.FromMilliseconds(180));switch(direction){case 0:walker.AnimateDown(time);break;case 1:walker.AnimateRight(time);break;case 2:walker.AnimateUp(time);break;case 3:walker.AnimateLeft(time);break;}var f=direction*4+step%4;if(walker.CurrentFrame!=f||walker.SourceRect!=new Rectangle(f%4*16,f/4*32,16,32))throw new Exception("Walking transition differs");walkingSteps++;}}
                if(variant!="Beach")foreach(var key in new[]{"kent_sleep"})
                {
                    var sections=helper.GameContent.Load<Dictionary<string,string>>("Data/animationDescriptions")[key].Split('/').Take(3).Select(s=>s.Split(' ',StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray()).ToArray();
                    foreach(var frames in sections){if(frames.Any(f=>f<0||f>=19))throw new Exception("Invalid native special frame");npc.Sprite.loop=true;npc.Sprite.setCurrentAnimation(frames.Select(f=>new FarmerSprite.AnimationFrame(f,150)).ToList());for(var step=1;step<=frames.Length*2;step++){npc.Sprite.animateOnce(new GameTime(TimeSpan.FromMilliseconds(step*150),TimeSpan.FromMilliseconds(150)));if(npc.Sprite.CurrentFrame!=frames[step%frames.Length])throw new Exception("Special transition differs");specialSteps++;}}
                    var routeNpc=Create();routeNpc.ChooseAppearance();var behavior=helper.Reflection.GetMethod(routeNpc,"getRouteEndBehaviorFunction").Invoke<Delegate>(key,null);if(behavior==null)throw new Exception("Native route missing");behavior.DynamicInvoke(routeNpc,location);if(!routeNpc.Sprite.currentAnimation.Select(f=>f.frame).SequenceEqual(sections[0]))throw new Exception("Route intro differs");for(var step=1;step<=sections[0].Length;step++)routeNpc.Sprite.animateOnce(new GameTime(TimeSpan.FromMilliseconds(step*100),TimeSpan.FromMilliseconds(100)));if(!routeNpc.Sprite.currentAnimation.Select(f=>f.frame).SequenceEqual(sections[1]))throw new Exception("Route intro-to-loop differs");routes++;
                }
                var data=npc.GetData();if(data.HiddenProfileEmoteStartFrame!=16||data.HiddenProfileEmoteFrameCount!=1)throw new Exception("Grief profile metadata differs");
                var scene=new Event();scene.actors.Add(npc);helper.Reflection.GetField<bool>(scene,"eventFinished").SetValue(true);var griefSeen=false;var cheerSeen=false;
                foreach(var map in new[]{"SamHouse","Saloon"})foreach(var script in helper.GameContent.Load<Dictionary<string,string>>("Data/Events/"+map).Values)foreach(var command in script.Split('/'))
                {
                    var args=command.Split(' ',StringSplitOptions.RemoveEmptyEntries);if(args.Length<3||args[1]!="Kent")continue;
                    if(args[0]=="showFrame")
                    {
                        var f=int.Parse(args[2]);if(f<0||f>=19)throw new Exception("Native event frame outside sheet");npc.Sprite.StopAnimation();npc.Sprite.CurrentFrame=f==0?1:0;Event.DefaultCommands.ShowFrame(scene,args,new EventContext(scene,location,Game1.currentGameTime,args));if(npc.Sprite.CurrentFrame!=f||npc.Sprite.SourceRect!=new Rectangle(f%4*16,f/4*32,16,32))throw new Exception("Event ShowFrame differs");griefSeen|=f==16;shownCommands++;
                    }
                    else if(args[0]=="animate")
                    {
                        var frames=args.Skip(5).Select(int.Parse).ToArray();var duration=int.Parse(args[4]);if(frames.Any(f=>f<0||f>=19))throw new Exception("Event animation outside sheet");npc.Sprite.StopAnimation();Event.DefaultCommands.Animate(scene,args,new EventContext(scene,location,Game1.currentGameTime,args));if(!npc.Sprite.currentAnimation.Select(f=>f.frame).SequenceEqual(frames)||npc.Sprite.loop!=bool.Parse(args[3]))throw new Exception("Event animation setup differs");npc.Sprite.CurrentFrame=frames[0];npc.Sprite.currentAnimationIndex=0;npc.Sprite.timer=0;for(var step=1;step<=frames.Length*2;step++){npc.Sprite.animateOnce(new GameTime(TimeSpan.FromMilliseconds(step*duration),TimeSpan.FromMilliseconds(duration)));if(npc.Sprite.CurrentFrame!=frames[step%frames.Length])throw new Exception("Event timed frame differs");timedEventSteps++;}cheerSeen|=frames.SequenceEqual(new[]{17});animatedCommands++;
                    }
                }
                if(!griefSeen||!cheerSeen)throw new Exception("Expected Kent native event poses missing");
                var device=Game1.graphics.GraphicsDevice;var previous=device.GetRenderTargets();using var target=new RenderTarget2D(device,768,1056);using var batch=new SpriteBatch(device);
                try{device.SetRenderTarget(target);device.Clear(new Color(35,45,58));batch.Begin(samplerState:SamplerState.PointClamp);for(var i=0;i<texture.Height/32*4;i++)batch.Draw(texture,new Rectangle(i%4*64+8,i/4*132+8,48,96),new Rectangle(i%4*16,i/4*32,16,32),Color.White);batch.Draw(portrait,new Rectangle(300,16,384,576),Color.White);batch.End();device.SetRenderTargets(previous);using var file=File.Create(Path.Combine(helper.DirectoryPath,"kent-"+variant.ToLowerInvariant()+"-runtime-preview.png"));target.SaveAsPng(file,target.Width,target.Height);}finally{device.SetRenderTargets(previous);}
            }
            Game1.season=Season.Spring;npc.wearNormalClothes();npc.ChooseAppearance();Equal(npc.Sprite.Texture,helper.GameContent.Load<Texture2D>("Characters/Kent"));Equal(npc.Portrait,helper.GameContent.Load<Texture2D>("Portraits/Kent"));
            helper.Data.WriteJsonFile("kent-checks.json",new{Passed=true,OccupiedPoses=poseCount,PreservedPlaceholders=placeholderCount,PortraitSlots=portraitCount,NativeShowFrameCommands=shownCommands,NativeAnimateCommands=animatedCommands,NativeEventAnimationSteps=timedEventSteps,NativeGriefProfileVerified=true,NativeWalkingSteps=walkingSteps,NativeSpecialSteps=specialSteps,NativeRouteTransitions=routes,NativeWinterTransition=true,EverydayRestored=true,FarmLoaded=false,FullEventPlayback=false});monitor.Log("Kent art audit passed; full saved-game scenes remain unverified.",LogLevel.Info);
        }
        catch(Exception ex){helper.Data.WriteJsonFile("kent-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log("Kent audit failed: "+ex,LogLevel.Error);}
        finally{Game1.season=oldSeason;Game1.currentLocation=oldLocation;}
    }
}


