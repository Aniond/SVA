using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace NpcArtAudit;

internal static class ClintAudit
{
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        var oldSeason=Game1.season;var oldLocation=Game1.currentLocation;var oldPlayer=Game1.player;
        try
        {
            var location=new GameLocation("Maps/Town","Town");Game1.currentLocation=location;Game1.season=Season.Spring;
            Color[] Pixels(Texture2D t){var p=new Color[t.Width*t.Height];t.GetData(p);return p;}
            void Equal(Texture2D a,Texture2D b){if(a.Width!=b.Width||a.Height!=b.Height||!Pixels(a).SequenceEqual(Pixels(b)))throw new Exception("Appearance pixels differ");}
            NPC Create()=>new(new AnimatedSprite("Characters/Clint",0,16,32),Vector2.Zero,2,"Clint"){currentLocation=location};
            var npc=Create();var poses=0;var portraits=0;var blanks=0;var walking=0;var hammerSteps=0;var sleepSteps=0;var routes=0;var geodeSteps=0;var eventCommands=0;
            foreach(var variant in new[]{"Base","Winter","Beach"})
            {
                Game1.season=variant=="Winter"?Season.Winter:Season.Spring;
                if(variant=="Beach")npc.wearIslandAttire();else{npc.wearNormalClothes();npc.ChooseAppearance();}
                var suffix=variant=="Base"?"":"_"+variant;var spriteAsset="Characters/Clint"+suffix;var portraitAsset="Portraits/Clint"+suffix;
                Equal(npc.Sprite.Texture,helper.GameContent.Load<Texture2D>(spriteAsset));Equal(npc.Portrait,helper.GameContent.Load<Texture2D>(portraitAsset));
                var texture=npc.Sprite.Texture;var portrait=npc.Portrait;var p=Pixels(texture);var pp=Pixels(portrait);
                if(texture.Width!=64||texture.Height!=(variant=="Beach"?128:352)||portrait.Width!=128||portrait.Height!=(variant=="Beach"?128:256))throw new Exception("Native atlas geometry differs");
                void Pose(Rectangle r){if(r.X<0||r.Y<0||r.Right>texture.Width||r.Bottom>texture.Height)throw new Exception("Pose outside atlas");var count=0;for(var y=r.Y;y<r.Bottom;y++)for(var x=r.X;x<r.Right;x++)if(p[y*64+x].A>0)count++;if(count==0)throw new Exception("Empty semantic pose");poses++;}
                for(var f=0;f<16;f++)Pose(new Rectangle(f%4*16,f/4*32,16,32));
                if(variant!="Beach")
                {
                    for(var f=8;f<=11;f++)Pose(new Rectangle(f%2*32,f/2*32,32,32));
                    for(var f=8;f<=12;f++)Pose(new Rectangle(f%2*32,f/2*48,32,48));
                    foreach(var f in new[]{39,43})Pose(new Rectangle(f%4*16,f/4*32,16,32));
                    foreach(var f in new[]{38,42}){for(var y=0;y<32;y++)for(var x=0;x<16;x++)if(p[(f/4*32+y)*64+f%4*16+x]!=Color.Transparent)throw new Exception("Native blank changed");blanks++;}
                }
                for(var f=0;f<(variant=="Beach"?4:8);f++){if(new Dialogue(npc,null,"Expression.$"+f).getPortraitIndex()!=f)throw new Exception("Portrait index differs");var count=0;for(var y=0;y<64;y++)for(var x=0;x<64;x++)if(pp[(f/2*64+y)*128+f%2*64+x].A>0)count++;if(count==0)throw new Exception("Empty portrait");portraits++;}
                var walker=new AnimatedSprite(spriteAsset,0,16,32);for(var direction=0;direction<4;direction++){walker.CurrentFrame=direction*4;walker.timer=0;for(var step=1;step<=8;step++){var t=new GameTime(TimeSpan.FromMilliseconds(step*180),TimeSpan.FromMilliseconds(180));switch(direction){case 0:walker.AnimateDown(t);break;case 1:walker.AnimateRight(t);break;case 2:walker.AnimateUp(t);break;case 3:walker.AnimateLeft(t);break;}var f=direction*4+step%4;if(walker.CurrentFrame!=f||walker.SourceRect!=new Rectangle(f%4*16,f/4*32,16,32))throw new Exception("Walking differs");walking++;}}
                if(variant!="Beach")
                {
                    foreach(var key in new[]{"clint_hammer","clint_sleep"})
                    {
                        var sections=helper.GameContent.Load<Dictionary<string,string>>("Data/animationDescriptions")[key].Split('/').Take(3).Select(s=>s.Split(' ',StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray()).ToArray();
                        var actor=Create();actor.ChooseAppearance();var behavior=helper.Reflection.GetMethod(actor,"getRouteEndBehaviorFunction").Invoke<Delegate>(key,null);if(behavior==null)throw new Exception("Native route absent");behavior.DynamicInvoke(actor,location);if(!actor.Sprite.currentAnimation.Select(f=>f.frame).SequenceEqual(sections[0]))throw new Exception("Route intro differs");for(var step=1;step<=sections[0].Length;step++)actor.Sprite.animateOnce(new GameTime(TimeSpan.FromMilliseconds(step*100),TimeSpan.FromMilliseconds(100)));if(!actor.Sprite.currentAnimation.Select(f=>f.frame).SequenceEqual(sections[1]))throw new Exception("Route loop differs");routes++;
                        var routeStartIndex=sections[1].Length>1?1:0;if(actor.Sprite.currentAnimationIndex!=routeStartIndex)throw new Exception("Native intro callback index differs");var width=key=="clint_hammer"?32:16;if(actor.Sprite.SpriteWidth!=width)throw new Exception("Native hammer width switch failed");
                        for(var step=1;step<=sections[1].Length*2;step++){actor.Sprite.animateOnce(new GameTime(TimeSpan.FromMilliseconds(step*100),TimeSpan.FromMilliseconds(100)));var f=sections[1][(routeStartIndex+step)%sections[1].Length];if(actor.Sprite.CurrentFrame!=f||actor.Sprite.SourceRect!=new Rectangle(f%(64/width)*width,f/(64/width)*32,width,32))throw new Exception("Native route timing or rectangle differs");if(width==32)hammerSteps++;else sleepSteps++;}
                    }
                    var scene=new Event();scene.actors.Add(npc);helper.Reflection.GetField<bool>(scene,"eventFinished").SetValue(true);
                    foreach(var script in helper.GameContent.Load<Dictionary<string,string>>("Data/Events/Town").Values)foreach(var command in script.Split('/')){var args=command.Split(' ',StringSplitOptions.RemoveEmptyEntries);if(args.Length<3||args[0]!="showFrame"||args[1]!="Clint")continue;var f=int.Parse(args[2]);if(f!=39)throw new Exception("Unexpected native advertisement frame");Event.DefaultCommands.ShowFrame(scene,args,new EventContext(scene,location,Game1.currentGameTime,args));if(npc.Sprite.CurrentFrame!=39||npc.Sprite.SourceRect!=new Rectangle(48,288,16,32))throw new Exception("Advertisement frame differs");eventCommands++;}
                    // Exercise the actual geode menu with a disposable player; no save is loaded or modified.
                    helper.Reflection.GetField<Farmer>(typeof(Game1),"_player").SetValue(new Farmer(){Name="Artwork audit"});Game1.player.addUnearnedMoney(1000);
                    try
                    {
                        var moneyBefore=Game1.player.Money;var menu=new GeodeMenu();Equal(menu.clint.Texture,helper.GameContent.Load<Texture2D>("Characters/Clint"));if(menu.clint.SourceRect!=new Rectangle(0,192,32,48))throw new Exception("Geode menu initial rectangle differs");menu.heldItem=ItemRegistry.Create("(O)535");menu.startGeodeCrack();
                        var frames=new[]{8,9,10,11,12,8};var durations=new[]{300,200,80,200,100,300};if(!menu.clint.currentAnimation.Select(f=>f.frame).SequenceEqual(frames)||!menu.clint.currentAnimation.Select(f=>f.milliseconds).SequenceEqual(durations)||menu.clint.loop)throw new Exception("Native geode setup differs");
                        for(var step=0;step<frames.Length-1;step++){menu.clint.animateOnce(new GameTime(TimeSpan.FromMilliseconds(durations.Take(step+1).Sum()),TimeSpan.FromMilliseconds(durations[step])));var f=frames[step+1];if(menu.clint.CurrentFrame!=f||menu.clint.SourceRect!=new Rectangle(f%2*32,f/2*48,32,48))throw new Exception("Geode timed rectangle differs");geodeSteps++;}
                        if(Game1.player.Money!=moneyBefore-25||menu.geodeSpot.item?.QualifiedItemId!="(O)535")throw new Exception($"Disposable geode interaction failed: money {moneyBefore} -> {Game1.player.Money}, item {menu.geodeSpot.item?.QualifiedItemId}");
                    }
                    finally{helper.Reflection.GetField<Farmer>(typeof(Game1),"_player").SetValue(oldPlayer);}
                }
                var device=Game1.graphics.GraphicsDevice;var previous=device.GetRenderTargets();using var target=new RenderTarget2D(device,768,1152);using var batch=new SpriteBatch(device);try{device.SetRenderTarget(target);device.Clear(new Color(35,45,58));batch.Begin(samplerState:SamplerState.PointClamp);batch.Draw(texture,new Rectangle(8,8,192,texture.Height*3),Color.White);batch.Draw(portrait,new Rectangle(300,16,384,portrait.Height*3),Color.White);batch.End();device.SetRenderTargets(previous);using var file=File.Create(Path.Combine(helper.DirectoryPath,"clint-"+variant.ToLowerInvariant()+"-runtime-preview.png"));target.SaveAsPng(file,target.Width,target.Height);}finally{device.SetRenderTargets(previous);}
            }
            Game1.season=Season.Spring;npc.wearNormalClothes();npc.ChooseAppearance();Equal(npc.Sprite.Texture,helper.GameContent.Load<Texture2D>("Characters/Clint"));Equal(npc.Portrait,helper.GameContent.Load<Texture2D>("Portraits/Clint"));
            helper.Data.WriteJsonFile("clint-checks.json",new{Passed=true,SemanticPoses=poses,PortraitSlots=portraits,PreservedBlankCells=blanks,NativeWalkingSteps=walking,NativeHammerSteps=hammerSteps,NativeSleepSteps=sleepSteps,NativeRoutes=routes,ActualGeodeMenuSteps=geodeSteps,NativeShowFrameCommands=eventCommands,WinterGeodeUsesBase=true,EverydayRestored=true,FarmLoaded=false,FullEventPlayback=false});monitor.Log("Clint native artwork checks passed; full saved-game scenes remain unverified.",LogLevel.Info);
        }
        catch(Exception ex){helper.Data.WriteJsonFile("clint-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log("Clint audit failed: "+ex,LogLevel.Error);}
        finally{helper.Reflection.GetField<Farmer>(typeof(Game1),"_player").SetValue(oldPlayer);Game1.season=oldSeason;Game1.currentLocation=oldLocation;}
    }
}
