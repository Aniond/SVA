using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NpcArtAudit;

// Runs in temporary title-screen locations without loading a saved farm.
internal static class RobinAudit
{
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        var oldSeason=Game1.season; var oldLocation=Game1.currentLocation;
        try
        {
            var location=new GameLocation("Maps/Town","Town"); Game1.currentLocation=location; Game1.season=Season.Spring;
            Color[] Pixels(Texture2D t) {var p=new Color[t.Width*t.Height];t.GetData(p);return p;}
            void Equal(Texture2D a,Texture2D b) {if(a.Width!=b.Width||a.Height!=b.Height||!Pixels(a).SequenceEqual(Pixels(b)))throw new Exception("Selected appearance pixels differ");}
            NPC Create() => new(new AnimatedSprite("Characters/Robin",0,16,32),Vector2.Zero,2,"Robin"){currentLocation=location};
            var npc=Create();var walkingSteps=0;var specialSteps=0;var routes=0;var poseCount=0;var portraitCount=0;var placeholderCount=0;var eventShowFrameCommands=0;var hammerSteps=0;var hammerRestCallbacks=0;
            foreach(var variant in new[]{"Base","Winter","Beach"})
            {
                Game1.season=variant=="Winter"?Season.Winter:Season.Spring;
                if(variant=="Beach")npc.wearIslandAttire();else {npc.wearNormalClothes();npc.ChooseAppearance();}
                var suffix=variant=="Base"?"":"_"+variant;var spriteAsset="Characters/Robin"+suffix;var portraitAsset="Portraits/Robin"+suffix;
                Equal(npc.Sprite.Texture,helper.GameContent.Load<Texture2D>(spriteAsset));Equal(npc.Portrait,helper.GameContent.Load<Texture2D>(portraitAsset));
                var texture=npc.Sprite.Texture;var portrait=npc.Portrait;var frameCount=variant=="Beach"?16:38;
                if(texture.Width!=64||texture.Height!=(variant=="Beach"?128:320)||portrait.Width!=128||portrait.Height!=256)throw new Exception("Native atlas size differs");
                var p=Pixels(texture);var pp=Pixels(portrait);
                for(var i=0;i<frameCount;i++){npc.Sprite.CurrentFrame=i;var r=new Rectangle(i%4*16,i/4*32,16,32);if(npc.Sprite.SourceRect!=r)throw new Exception("Frame rectangle differs");var count=0;for(var y=r.Y;y<r.Bottom;y++)for(var x=r.X;x<r.Right;x++)if(p[y*64+x].A>0)count++;if(count==0)throw new Exception("Empty pose");poseCount++;}
                if(variant!="Beach")for(var i=38;i<40;i++){for(var y=0;y<32;y++)for(var x=0;x<16;x++)if(p[(i/4*32+y)*64+i%4*16+x]!=new Color(123,66,26,255))throw new Exception("Native solid placeholder differs");placeholderCount++;}
                for(var i=0;i<8;i++){if(new Dialogue(npc,null,"Expression.$"+i).getPortraitIndex()!=i)throw new Exception("Portrait index differs");var count=0;for(var y=0;y<64;y++)for(var x=0;x<64;x++)if(pp[(i/2*64+y)*128+i%2*64+x].A>0)count++;if(count==0)throw new Exception("Empty portrait");portraitCount++;}
                var walker=new AnimatedSprite(spriteAsset,0,16,32);
                for(var direction=0;direction<4;direction++){walker.CurrentFrame=direction*4;walker.timer=0;for(var step=1;step<=8;step++){var time=new GameTime(TimeSpan.FromMilliseconds(step*180),TimeSpan.FromMilliseconds(180));switch(direction){case 0:walker.AnimateDown(time);break;case 1:walker.AnimateRight(time);break;case 2:walker.AnimateUp(time);break;case 3:walker.AnimateLeft(time);break;}var f=direction*4+step%4;if(walker.CurrentFrame!=f||walker.SourceRect!=new Rectangle(f%4*16,f/4*32,16,32))throw new Exception("Walking transition differs");walkingSteps++;}}
                if(variant!="Beach")foreach(var key in new[]{"robin_exercise","robin_dance","robin_sleep"})
                {
                    var sections=helper.GameContent.Load<Dictionary<string,string>>("Data/animationDescriptions")[key].Split('/').Take(3).Select(s=>s.Split(' ',StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray()).ToArray();
                    foreach(var frames in sections){if(frames.Any(f=>f<0||f>=38))throw new Exception("Invalid native special frame");npc.Sprite.loop=true;npc.Sprite.setCurrentAnimation(frames.Select(f=>new FarmerSprite.AnimationFrame(f,150)).ToList());for(var step=1;step<=frames.Length*2;step++){npc.Sprite.animateOnce(new GameTime(TimeSpan.FromMilliseconds(step*150),TimeSpan.FromMilliseconds(150)));if(npc.Sprite.CurrentFrame!=frames[step%frames.Length])throw new Exception("Special transition differs");specialSteps++;}}
                    var routeNpc=Create();routeNpc.ChooseAppearance();var behavior=helper.Reflection.GetMethod(routeNpc,"getRouteEndBehaviorFunction").Invoke<Delegate>(key,null);if(behavior==null)throw new Exception("Native route missing");behavior.DynamicInvoke(routeNpc,location);if(!routeNpc.Sprite.currentAnimation.Select(f=>f.frame).SequenceEqual(sections[0]))throw new Exception("Route intro differs");for(var step=1;step<=sections[0].Length;step++)routeNpc.Sprite.animateOnce(new GameTime(TimeSpan.FromMilliseconds(step*100),TimeSpan.FromMilliseconds(100)));if(!routeNpc.Sprite.currentAnimation.Select(f=>f.frame).SequenceEqual(sections[1]))throw new Exception("Route intro-to-loop differs");routes++;
                }
                if(variant!="Beach")
                {
                    var scene=new Event();scene.actors.Add(npc);helper.Reflection.GetField<bool>(scene,"eventFinished").SetValue(true);
                    var seen=new List<int>();
                    foreach(var map in new[]{"BusStop","Town","ScienceHouse","Saloon","Farm","AnimalShop","Mountain","CommunityCenter","ManorHouse"})
                    foreach(var script in helper.GameContent.Load<Dictionary<string,string>>("Data/Events/"+map).Values)foreach(var command in script.Split('/'))
                    {
                        var args=command.Split(' ',StringSplitOptions.RemoveEmptyEntries);if(args.Length<3||args[0]!="showFrame"||args[1]!="Robin")continue;var frame=int.Parse(args[2]);if(frame<0||frame>=38)throw new Exception("Native event references missing pose");npc.Sprite.StopAnimation();npc.Sprite.CurrentFrame=frame==0?1:0;
                        Event.DefaultCommands.ShowFrame(scene,args,new EventContext(scene,location,Game1.currentGameTime,args));if(npc.Sprite.CurrentFrame!=frame||npc.Sprite.SourceRect!=new Rectangle(frame%4*16,frame/4*32,16,32))throw new Exception("Native ShowFrame differs");seen.Add(frame);eventShowFrameCommands++;
                    }
                    if(!seen.Contains(37))throw new Exception("Omelet event pose missing");
                    var hammerNpc=Create();hammerNpc.ChooseAppearance();var previousUpgrade=Game1.player.daysUntilHouseUpgrade.Value;
                    try
                    {
                        Game1.player.daysUntilHouseUpgrade.Value=1;helper.Reflection.GetMethod(hammerNpc,"doPlayRobinHammerAnimation").Invoke();
                        if(!hammerNpc.Sprite.currentAnimation.Select(f=>f.frame).SequenceEqual(new[]{24,25,26,27})||!hammerNpc.Sprite.currentAnimation.Select(f=>f.milliseconds).SequenceEqual(new[]{75,75,300,1000}))throw new Exception("Native hammer sequence differs");
                        hammerNpc.Sprite.CurrentFrame=24;hammerNpc.Sprite.currentAnimationIndex=0;hammerNpc.Sprite.timer=0;
                        foreach(var pair in new[]{(75,25),(75,26),(300,27)}){hammerNpc.Sprite.animateOnce(new GameTime(TimeSpan.FromMilliseconds(pair.Item1),TimeSpan.FromMilliseconds(pair.Item1)));// Native frame27 runs its start callback immediately and may choose rest23.
                        if(pair.Item2==27 ? (hammerNpc.Sprite.CurrentFrame!=23&&hammerNpc.Sprite.CurrentFrame!=27) : hammerNpc.Sprite.CurrentFrame!=pair.Item2)throw new Exception("Hammer timed transition differs: expected "+pair.Item2+", got "+hammerNpc.Sprite.CurrentFrame);hammerSteps++;}
                        for(var i=0;i<8;i++){helper.Reflection.GetMethod(hammerNpc,"robinVariablePause").Invoke(Game1.player);var f=hammerNpc.Sprite.currentAnimation[hammerNpc.Sprite.currentAnimationIndex];if(f.frame!=23&&f.frame!=27)throw new Exception("Hammer rest pose differs");hammerNpc.Sprite.CurrentFrame=f.frame;if(hammerNpc.Sprite.SourceRect!=new Rectangle(f.frame%4*16,f.frame/4*32,16,32))throw new Exception("Hammer rest rectangle differs");hammerRestCallbacks++;}
                    }finally{Game1.player.daysUntilHouseUpgrade.Value=previousUpgrade;}
                }
                var device=Game1.graphics.GraphicsDevice;var previous=device.GetRenderTargets();using var target=new RenderTarget2D(device,768,1320);using var batch=new SpriteBatch(device);
                try{device.SetRenderTarget(target);device.Clear(new Color(35,45,58));batch.Begin(samplerState:SamplerState.PointClamp);for(var i=0;i<texture.Height/32*4;i++)batch.Draw(texture,new Rectangle(i%4*64+8,i/4*132+8,48,96),new Rectangle(i%4*16,i/4*32,16,32),Color.White);batch.Draw(portrait,new Rectangle(300,16,384,768),Color.White);batch.End();device.SetRenderTargets(previous);using var file=File.Create(Path.Combine(helper.DirectoryPath,"robin-"+variant.ToLowerInvariant()+"-runtime-preview.png"));target.SaveAsPng(file,target.Width,target.Height);}finally{device.SetRenderTargets(previous);}
            }
            npc.wearNormalClothes();npc.ChooseAppearance();Equal(npc.Sprite.Texture,helper.GameContent.Load<Texture2D>("Characters/Robin"));Equal(npc.Portrait,helper.GameContent.Load<Texture2D>("Portraits/Robin"));
            helper.Data.WriteJsonFile("robin-checks.json",new{Passed=true,OccupiedPoses=poseCount,PreservedPlaceholders=placeholderCount,PortraitSlots=portraitCount,NativeWalkingSteps=walkingSteps,NativeSpecialSteps=specialSteps,NativeRouteTransitions=routes,NativeEventShowFrameCommands=eventShowFrameCommands,NativeHammerTimedSteps=hammerSteps,NativeHammerRestCallbacks=hammerRestCallbacks,NativeWinterBeachTransitions=true,EverydayRestored=true,FarmLoaded=false,FullEventPlayback=false});monitor.Log("Robin art audit passed; full saved-game scenes remain unverified.",LogLevel.Info);
        }
        catch(Exception ex){helper.Data.WriteJsonFile("robin-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log("Robin audit failed: "+ex,LogLevel.Error);}
        finally{Game1.season=oldSeason;Game1.currentLocation=oldLocation;}
    }
}

