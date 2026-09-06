using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NpcArtAudit;

// Integration draft: parent wires, builds, and runs against installed textures.
internal static class WillyAudit
{
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        var oldSeason=Game1.season;var oldLocation=Game1.currentLocation;
        try
        {
            var location=new GameLocation("Maps/Town","Town");Game1.currentLocation=location;Game1.season=Season.Spring;
            Color[] Pixels(Texture2D t){var p=new Color[t.Width*t.Height];t.GetData(p);return p;}
            void Equal(Texture2D a,Texture2D b){if(a.Width!=b.Width||a.Height!=b.Height||!Pixels(a).SequenceEqual(Pixels(b)))throw new Exception("Appearance differs");}
            NPC Create()=>new(new AnimatedSprite("Characters/Willy",0,16,32),Vector2.Zero,2,"Willy"){currentLocation=location};
            var npc=Create();var bodyCount=0;var overlayCount=0;var placeholders=0;var portraits=0;var walking=0;var fishingRects=0;var eventSteps=0;var routeTransitions=0;var showCommands=0;var animateCommands=0;var actualSteps=0;
            foreach(var variant in new[]{"Base","Winter"})
            {
                Game1.season=variant=="Winter"?Season.Winter:Season.Spring;npc.wearNormalClothes();npc.ChooseAppearance();
                var suffix=variant=="Base"?"":"_Winter";var asset="Characters/Willy"+suffix;
                Equal(npc.Sprite.Texture,helper.GameContent.Load<Texture2D>(asset));Equal(npc.Portrait,helper.GameContent.Load<Texture2D>("Portraits/Willy"+suffix));
                var texture=npc.Sprite.Texture;var portrait=npc.Portrait;if(texture.Width!=64||texture.Height!=352||portrait.Width!=128||portrait.Height!=128)throw new Exception("Native atlas dimensions changed");var p=Pixels(texture);var pp=Pixels(portrait);
                for(var i=0;i<44;i++)
                {
                    npc.Sprite.CurrentFrame=i;var rect=new Rectangle(i%4*16,i/4*32,16,32);if(npc.Sprite.SourceRect!=rect)throw new Exception("Frame rectangle differs");var count=0;
                    for(var y=rect.Y;y<rect.Bottom;y++)for(var x=rect.X;x<rect.Right;x++){var c=p[y*64+x];if(c.A>0)count++;if(i==36&&c!=Color.White)throw new Exception("Native white placeholder changed");}
                    if(new[]{35,39,43}.Contains(i)){if(count!=0)throw new Exception("Native transparent placeholder changed");placeholders++;}
                    else if(i==36)placeholders++;else if(new[]{20,21,22,23,37,38}.Contains(i)){if(count==0)throw new Exception("Fishing overlay missing");overlayCount++;}else{if(count==0)throw new Exception("Body pose missing");bodyCount++;}
                }
                for(var i=0;i<4;i++){if(new Dialogue(npc,null,"Expression.$"+i).getPortraitIndex()!=i)throw new Exception("Portrait slot differs");var count=0;for(var y=0;y<64;y++)for(var x=0;x<64;x++)if(pp[(i/2*64+y)*128+i%2*64+x].A>0)count++;if(count==0)throw new Exception("Empty portrait");portraits++;}
                var walker=new AnimatedSprite(asset,0,16,32);for(var d=0;d<4;d++){walker.CurrentFrame=d*4;walker.timer=0;for(var step=1;step<=8;step++){var time=new GameTime(TimeSpan.FromMilliseconds(step*180),TimeSpan.FromMilliseconds(180));switch(d){case 0:walker.AnimateDown(time);break;case 1:walker.AnimateRight(time);break;case 2:walker.AnimateUp(time);break;case 3:walker.AnimateLeft(time);break;}if(walker.CurrentFrame!=d*4+step%4)throw new Exception("Walking phase differs");walking++;}}
                var fisher=new AnimatedSprite(asset,0,16,64);foreach(var i in new[]{8,9,10,11,17,18}){fisher.CurrentFrame=i;if(fisher.SourceRect!=new Rectangle(i%4*16,i/4*64,16,64))throw new Exception("Native 64px fishing rectangle differs");fishingRects++;}
                foreach(var sequence in new[]{(Frames:new[]{28,29,30,31},Ms:250,Height:32),(Frames:new[]{8,9,10,11},Ms:500,Height:64),(Frames:new[]{17,18},Ms:100,Height:64),(Frames:new[]{17,18},Ms:200,Height:64)})
                {
                    var animated=new AnimatedSprite(asset,0,16,sequence.Height);animated.loop=true;animated.setCurrentAnimation(sequence.Frames.Select(f=>new FarmerSprite.AnimationFrame(f,sequence.Ms)).ToList());for(var step=1;step<=sequence.Frames.Length*2;step++){animated.animateOnce(new GameTime(TimeSpan.FromMilliseconds(step*sequence.Ms),TimeSpan.FromMilliseconds(sequence.Ms)));if(animated.CurrentFrame!=sequence.Frames[step%sequence.Frames.Length])throw new Exception("Native event animation phase differs");eventSteps++;}
                }
                var sections=helper.GameContent.Load<Dictionary<string,string>>("Data/animationDescriptions")["dick_fish"].Split('/').Take(3).Select(s=>s.Split(' ',StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray()).ToArray();
                var routeNpc=Create();routeNpc.ChooseAppearance();var behavior=helper.Reflection.GetMethod(routeNpc,"getRouteEndBehaviorFunction").Invoke<Delegate>("dick_fish",null);if(behavior==null)throw new Exception("Native fishing route missing");behavior.DynamicInvoke(routeNpc,location);if(!routeNpc.Sprite.currentAnimation.Select(f=>f.frame).SequenceEqual(sections[0]))throw new Exception("Fishing route intro differs");for(var step=1;step<=sections[0].Length;step++)routeNpc.Sprite.animateOnce(new GameTime(TimeSpan.FromMilliseconds(step*100),TimeSpan.FromMilliseconds(100)));if(!routeNpc.Sprite.currentAnimation.Select(f=>f.frame).SequenceEqual(sections[1]))throw new Exception("Fishing route loop differs");if(routeNpc.Sprite.SpriteHeight!=64)throw new Exception("Fishing route failed native 64px extension");routeTransitions++;
                var shown=new HashSet<int>();var tallSequences=0;
                foreach(var map in new[]{"Beach","Mountain","Saloon"})foreach(var script in helper.GameContent.Load<Dictionary<string,string>>("Data/Events/"+map).Values)
                {
                    var commands=Event.ParseCommands(script).Select(ArgUtility.SplitBySpaceQuoteAware).ToArray();if(!commands.Any(a=>a.Length>=3&&a[1]=="Willy"&&(a[0]=="animate"||a[0]=="showFrame")))continue;
                    var scene=new Event();var actor=Create();scene.actors.Add(actor);helper.Reflection.GetField<bool>(scene,"eventFinished").SetValue(true);var height=32;
                    foreach(var args in commands)
                    {
                        if(args.Length<3||args[1]!="Willy")continue;
                        if(args[0]=="addTemporaryActor"){height=int.Parse(args[3]);actor.Sprite=new AnimatedSprite(asset,0,16,height);continue;}
                        if(args[0]=="showFrame"){var f=int.Parse(args[2]);actor.Sprite.StopAnimation();actor.Sprite.CurrentFrame=f==0?1:0;Event.DefaultCommands.ShowFrame(scene,args,new EventContext(scene,location,Game1.currentGameTime,args));if(actor.Sprite.CurrentFrame!=f||actor.Sprite.SourceRect!=new Rectangle(f%4*16,f/4*height,16,height))throw new Exception("Actual Willy ShowFrame differs");shown.Add(f);showCommands++;}
                        else if(args[0]=="animate"){
                            var frames=args.Skip(5).Select(int.Parse).ToArray();var ms=int.Parse(args[4]);actor.Sprite.StopAnimation();Event.DefaultCommands.Animate(scene,args,new EventContext(scene,location,Game1.currentGameTime,args));if(!actor.Sprite.currentAnimation.Select(f=>f.frame).SequenceEqual(frames)||actor.Sprite.loop!=bool.Parse(args[3]))throw new Exception("Actual Willy Animate differs");actor.Sprite.currentAnimationIndex=0;actor.Sprite.CurrentFrame=frames[0];actor.Sprite.timer=ms;for(var step=1;step<=frames.Length*2;step++){actor.Sprite.animateOnce(new GameTime(TimeSpan.FromMilliseconds(step*ms),TimeSpan.FromMilliseconds(ms)));if(actor.Sprite.CurrentFrame!=frames[step%frames.Length])throw new Exception("Actual Willy timed frame differs");actualSteps++;}if(height==64)tallSequences++;animateCommands++;
                        }
                    }
                }
                foreach(var f in new[]{24,25,26,32})if(!shown.Contains(f))throw new Exception("Native Willy event pose missing");if(tallSequences<4)throw new Exception("Native temporary fishing actors missing");
                var device=Game1.graphics.GraphicsDevice;var previous=device.GetRenderTargets();using var target=new RenderTarget2D(device,800,1520);using var batch=new SpriteBatch(device);
                try{device.SetRenderTarget(target);device.Clear(new Color(35,45,58));batch.Begin(samplerState:SamplerState.PointClamp);for(var i=0;i<44;i++)batch.Draw(texture,new Rectangle(i%4*64+8,i/4*132+8,48,96),new Rectangle(i%4*16,i/4*32,16,32),Color.White);batch.Draw(portrait,new Rectangle(300,16,384,384),Color.White);var compositeFrames=new[]{8,9,10,11,17,18};for(var j=0;j<6;j++){var i=compositeFrames[j];batch.Draw(texture,new Rectangle(300+j%3*96,440+j/3*280,64,256),new Rectangle(i%4*16,i/4*64,16,64),Color.White);}batch.End();device.SetRenderTargets(previous);using var output=File.Create(Path.Combine(helper.DirectoryPath,"willy-"+variant.ToLowerInvariant()+"-runtime-preview.png"));target.SaveAsPng(output,target.Width,target.Height);}finally{device.SetRenderTargets(previous);}
            }
            Game1.season=Season.Spring;npc.ChooseAppearance();Equal(npc.Sprite.Texture,helper.GameContent.Load<Texture2D>("Characters/Willy"));Equal(npc.Portrait,helper.GameContent.Load<Texture2D>("Portraits/Willy"));
            helper.Data.WriteJsonFile("willy-checks.json",new{Passed=true,BodyPoses=bodyCount,FishingOverlayCells=overlayCount,NativePlaceholders=placeholders,Portraits=portraits,ActualShowFrameCommands=showCommands,ActualAnimateCommands=animateCommands,ActualTimedEventSteps=actualSteps,NativeWalkingSteps=walking,NativeFishingRectangles=fishingRects,NativeEventSteps=eventSteps,NativeFishingRouteTransitions=routeTransitions,EverydayRestored=true,FarmLoaded=false,FullEventPlayback=false});monitor.Log("Willy art audit passed; full saved-game scenes remain unverified.",LogLevel.Info);
        }
        catch(Exception ex){helper.Data.WriteJsonFile("willy-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log("Willy audit failed: "+ex,LogLevel.Error);}
        finally{Game1.season=oldSeason;Game1.currentLocation=oldLocation;}
    }
}

