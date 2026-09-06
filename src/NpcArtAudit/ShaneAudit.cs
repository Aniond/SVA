using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NpcArtAudit;

// Verifies native walking, drinking, work and native event poses, portraits and season changes.
internal static class ShaneAudit
{
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        var oldSeason=Game1.season; var oldLocation=Game1.currentLocation;
        try
        {
            var location=new GameLocation("Maps/Town","Town"); Game1.currentLocation=location; Game1.season=Season.Spring;
            Color[] Pixels(Texture2D t) {var p=new Color[t.Width*t.Height];t.GetData(p);return p;}
            void Equal(Texture2D a,Texture2D b) {if(a.Width!=b.Width||a.Height!=b.Height||!Pixels(a).SequenceEqual(Pixels(b)))throw new Exception("Selected appearance pixels differ");}
            NPC Create() => new(new AnimatedSprite("Characters/Shane",0,16,32),Vector2.Zero,2,"Shane"){currentLocation=location};
            var npc=Create();var walkingSteps=0;var specialSteps=0;var routes=0;var poseCount=0;var portraitCount=0;var placeholderCount=0;var eventCommands=0;var nativeEventReferences=0;var workPrecedenceChecks=0;
            foreach(var variant in new[]{"Base","Winter","Beach","JojaMart"})
            {
                location=new GameLocation(variant=="JojaMart"?"Maps/JojaMart":"Maps/Town",variant=="JojaMart"?"JojaMart":"Town");Game1.currentLocation=location;npc.currentLocation=location;
                Game1.season=variant=="Winter"?Season.Winter:Season.Spring;
                if(variant=="Beach")npc.wearIslandAttire();else {npc.wearNormalClothes();npc.ChooseAppearance();}
                var suffix=variant=="Base"?"":"_"+variant;var spriteAsset="Characters/Shane"+suffix;var portraitAsset="Portraits/Shane"+suffix;
                Equal(npc.Sprite.Texture,helper.GameContent.Load<Texture2D>(spriteAsset));Equal(npc.Portrait,helper.GameContent.Load<Texture2D>(portraitAsset));
                var texture=npc.Sprite.Texture;var portrait=npc.Portrait;var frameCount=variant=="Beach"?24:variant=="JojaMart"?28:52;var placeholders=variant=="Beach"?new[]{21,22,23}:variant=="JojaMart"?Array.Empty<int>():new[]{37,38,39,40,41,42,43,51};
                if(texture.Width!=64||texture.Height!=(variant=="Beach"?192:variant=="JojaMart"?224:416)||portrait.Width!=128||portrait.Height!=384)throw new Exception("Native atlas size differs");
                var p=Pixels(texture);var pp=Pixels(portrait);
                for(var i=0;i<frameCount;i++){if(placeholders.Contains(i))continue;npc.Sprite.CurrentFrame=i;var r=new Rectangle(i%4*16,i/4*32,16,32);if(npc.Sprite.SourceRect!=r)throw new Exception("Frame rectangle differs");var count=0;for(var y=r.Y;y<r.Bottom;y++)for(var x=r.X;x<r.Right;x++)if(p[y*64+x].A>0)count++;if(count==0)throw new Exception("Empty pose");poseCount++;}
                var retained=new List<byte>();foreach(var i in placeholders){for(var y=0;y<32;y++)for(var x=0;x<16;x++){var c=p[(i/4*32+y)*64+i%4*16+x];retained.AddRange(new[]{c.R,c.G,c.B,c.A});}placeholderCount++;}
                var placeholderHash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(retained.ToArray())).ToLowerInvariant();
                var expectedHash=variant=="Base"?"5dfcc76033cb65a8ebdf42642b9978cf2c88ca4acfe77a0aaa4dba05d161cae9":variant=="Winter"?"5dfcc76033cb65a8ebdf42642b9978cf2c88ca4acfe77a0aaa4dba05d161cae9":variant=="Beach"?"fd9243e1ba57263ed469c3bdbd7ade6ec5254e7ed924a9f5737fa44749933cc0":"e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";if(placeholderHash!=expectedHash)throw new Exception("Native placeholder RGBA differs");
                for(var i=0;i<12;i++){if(new Dialogue(npc,null,"Expression.$"+i).getPortraitIndex()!=i)throw new Exception("Portrait index differs");var count=0;for(var y=0;y<64;y++)for(var x=0;x<64;x++)if(pp[(i/2*64+y)*128+i%2*64+x].A>0)count++;if(count==0)throw new Exception("Empty portrait");portraitCount++;}
                var walker=new AnimatedSprite(spriteAsset,0,16,32);
                for(var direction=0;direction<4;direction++){walker.CurrentFrame=direction*4;walker.timer=0;for(var step=1;step<=8;step++){var time=new GameTime(TimeSpan.FromMilliseconds(step*180),TimeSpan.FromMilliseconds(180));switch(direction){case 0:walker.AnimateDown(time);break;case 1:walker.AnimateRight(time);break;case 2:walker.AnimateUp(time);break;case 3:walker.AnimateLeft(time);break;}var f=direction*4+step%4;if(walker.CurrentFrame!=f||walker.SourceRect!=new Rectangle(f%4*16,f/4*32,16,32))throw new Exception("Walking transition differs");walkingSteps++;}}
                foreach(var key in (variant=="Beach"?new[]{"shane_beach_drink"}:variant=="JojaMart"?new[]{"shane_work"}:new[]{"shane_drink","shane_sleep"}))
                {
                    var sections=helper.GameContent.Load<Dictionary<string,string>>("Data/animationDescriptions")[key].Split('/').Take(3).Select(s=>s.Split(' ',StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray()).ToArray();
                    foreach(var frames in sections){if(frames.Any(f=>f<0||f>=frameCount||placeholders.Contains(f)))throw new Exception("Invalid native special frame");npc.Sprite.loop=true;npc.Sprite.setCurrentAnimation(frames.Select(f=>new FarmerSprite.AnimationFrame(f,150)).ToList());for(var step=1;step<=frames.Length*2;step++){npc.Sprite.animateOnce(new GameTime(TimeSpan.FromMilliseconds(step*150),TimeSpan.FromMilliseconds(150)));if(npc.Sprite.CurrentFrame!=frames[step%frames.Length])throw new Exception("Special transition differs");specialSteps++;}}
                    var routeNpc=Create();if(variant=="Beach")routeNpc.wearIslandAttire();else routeNpc.ChooseAppearance();var behavior=helper.Reflection.GetMethod(routeNpc,"getRouteEndBehaviorFunction").Invoke<Delegate>(key,null);if(behavior==null)throw new Exception("Native route missing");behavior.DynamicInvoke(routeNpc,location);if(!routeNpc.Sprite.currentAnimation.Select(f=>f.frame).SequenceEqual(sections[0]))throw new Exception("Route intro differs");for(var step=1;step<=sections[0].Length;step++)routeNpc.Sprite.animateOnce(new GameTime(TimeSpan.FromMilliseconds(step*100),TimeSpan.FromMilliseconds(100)));if(!routeNpc.Sprite.currentAnimation.Select(f=>f.frame).SequenceEqual(sections[1]))throw new Exception("Route intro-to-loop differs");routes++;
                }
                if(variant=="Base"||variant=="Winter")
                {
                    var scene=new Event();scene.actors.Add(npc);helper.Reflection.GetField<bool>(scene,"eventFinished").SetValue(true);
                    var seen=new List<int>();
                    foreach(var map in new[]{"AnimalShop","BusStop","Farm","Forest","ManorHouse","Saloon","Town"})foreach(var script in helper.GameContent.Load<Dictionary<string,string>>("Data/Events/"+map).Values)foreach(var command in script.Split('/'))
                    {
                        var args=command.Split(' ',StringSplitOptions.RemoveEmptyEntries);if(args.Length>=6&&args[0]=="animate"&&args[1]=="Shane"){foreach(var f in args.Skip(5).Select(int.Parse)){if(f<0||f>=frameCount||placeholders.Contains(f))throw new Exception("Native animate references missing frame");nativeEventReferences++;}}if(args.Length<3||args[0]!="showFrame"||args[1]!="Shane")continue;var frame=int.Parse(args[2]);if(frame<0||frame>=frameCount||placeholders.Contains(frame))throw new Exception("Native event references missing pose");
                        npc.Sprite.StopAnimation();npc.Sprite.CurrentFrame=frame==0?1:0;Event.DefaultCommands.ShowFrame(scene,args,new EventContext(scene,location,Game1.currentGameTime,args));if(npc.Sprite.CurrentFrame!=frame||npc.Sprite.SourceRect!=new Rectangle(frame%4*16,frame/4*32,16,32))throw new Exception("Native ShowFrame differs");seen.Add(frame);eventCommands++;nativeEventReferences++;
                    }
                    if(!new[]{16,17,18,19,34,35}.All(seen.Contains))throw new Exception("Camera or kiss ShowFrame coverage missing");
                }
                if(variant=="JojaMart")
                {
                    foreach(var season in new[]{Season.Spring,Season.Winter}){Game1.season=season;npc.ChooseAppearance();if(npc.LastAppearanceId!="WorkOutfit")throw new Exception("Work appearance not selected");Equal(npc.Sprite.Texture,helper.GameContent.Load<Texture2D>("Characters/Shane_JojaMart"));Equal(npc.Portrait,helper.GameContent.Load<Texture2D>("Portraits/Shane_JojaMart"));workPrecedenceChecks++;}
                }
                var device=Game1.graphics.GraphicsDevice;var previous=device.GetRenderTargets();using var target=new RenderTarget2D(device,1024,1216);using var batch=new SpriteBatch(device);
                try{device.SetRenderTarget(target);device.Clear(new Color(35,45,58));batch.Begin(samplerState:SamplerState.PointClamp);for(var i=0;i<texture.Height/32*4;i++)batch.Draw(texture,new Rectangle(i%8*64+8,i/8*132+8,48,96),new Rectangle(i%4*16,i/4*32,16,32),Color.White);batch.Draw(portrait,new Rectangle(600,16,384,1152),Color.White);batch.End();device.SetRenderTargets(previous);using var file=File.Create(Path.Combine(helper.DirectoryPath,"shane-"+variant.ToLowerInvariant()+"-runtime-preview.png"));target.SaveAsPng(file,target.Width,target.Height);}finally{device.SetRenderTargets(previous);}
            }
            location=new GameLocation("Maps/Town","Town");Game1.currentLocation=location;npc.currentLocation=location;Game1.season=Season.Spring;npc.wearNormalClothes();npc.ChooseAppearance();Equal(npc.Sprite.Texture,helper.GameContent.Load<Texture2D>("Characters/Shane"));Equal(npc.Portrait,helper.GameContent.Load<Texture2D>("Portraits/Shane"));
            helper.Data.WriteJsonFile("shane-checks.json",new{Passed=true,OccupiedPoses=poseCount,PreservedPlaceholders=placeholderCount,PortraitSlots=portraitCount,NativeWalkingSteps=walkingSteps,NativeSpecialSteps=specialSteps,NativeRouteTransitions=routes,NativeWinterBeachTransitions=true,NativeWorkPrecedenceChecks=workPrecedenceChecks,NativeEventShowFrameCommands=eventCommands,NativeEventFrameReferences=nativeEventReferences,EverydayRestored=true,FarmLoaded=false,FullEventPlayback=false});monitor.Log("Shane art audit passed; full saved-game scenes remain unverified.",LogLevel.Info);
        }
        catch(Exception ex){helper.Data.WriteJsonFile("shane-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log("Shane audit failed: "+ex,LogLevel.Error);}
        finally{Game1.season=oldSeason;Game1.currentLocation=oldLocation;}
    }
}


