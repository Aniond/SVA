using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NpcArtAudit;
internal static class AlexBeachAudit
{
    public static void Run(IModHelper helper,IMonitor monitor)
    {
        var oldSeason=Game1.season;var oldLocation=Game1.currentLocation;
        try
        {
            Game1.season=Season.Summer;
            var location=new GameLocation("Maps/Town","Town");Game1.currentLocation=location;
            var npc=new NPC(new AnimatedSprite("Characters/Alex",0,16,32),Vector2.Zero,2,"Alex"){currentLocation=location};
            npc.wearIslandAttire();
            if((npc.Sprite.overrideTextureName??npc.Sprite.textureName.Value).Replace('\\','/')!="Characters/Alex_Beach")throw new Exception("Native beach appearance not selected");
            void Equal(Texture2D a,Texture2D b){var pa=new Color[a.Width*a.Height];var pb=new Color[b.Width*b.Height];a.GetData(pa);b.GetData(pb);if(!pa.SequenceEqual(pb))throw new Exception("Native texture pixels differ");}
            Equal(npc.Sprite.Texture,helper.GameContent.Load<Texture2D>("Characters/Alex_Beach"));
            Equal(npc.Portrait,helper.GameContent.Load<Texture2D>("Portraits/Alex_Beach"));
            for(var i=0;i<10;i++){var dialogue=new Dialogue(npc,null,"Beach portrait check.$"+i);if(dialogue.getPortraitIndex()!=i)throw new Exception("Portrait index mismatch "+i);}
            var portrait=npc.Portrait;var portraitPixels=new Color[portrait.Width*portrait.Height];portrait.GetData(portraitPixels);
            var winterPortrait=helper.GameContent.Load<Texture2D>("Portraits/Alex_Winter");var winterPixels=new Color[winterPortrait.Width*winterPortrait.Height];winterPortrait.GetData(winterPixels);
            foreach(var retainedIndex in new[]{6,8})for(var y=0;y<64;y++)for(var x=0;x<64;x++){
                var px=retainedIndex%2*64+x;var py=retainedIndex/2*64+y;
                if(portraitPixels[py*portrait.Width+px]!=winterPixels[py*winterPortrait.Width+px])throw new Exception("Reused beach portrait pixels differ: "+retainedIndex);
            }
            for(var index=0;index<10;index++) { var visible=false; for(var y=0;y<64;y++)for(var x=0;x<64;x++)if(portraitPixels[(index/2*64+y)*portrait.Width+index%2*64+x].A>0)visible=true; if(!visible)throw new Exception("Empty portrait expression "+index); }
            var texture=npc.Sprite.Texture;var pixels=new Color[texture.Width*texture.Height];texture.GetData(pixels);
            for(var frame=0;frame<20;frame++){npc.Sprite.CurrentFrame=frame;var rect=npc.Sprite.SourceRect;if(rect.X!=frame%4*16||rect.Y!=frame/4*32)throw new Exception("Frame layout mismatch");var visible=0;for(var y=rect.Y;y<rect.Bottom;y++)for(var x=rect.X;x<rect.Right;x++)if(pixels[y*64+x].A>0)visible++;if((frame!=19)!=(visible>0))throw new Exception("Occupied/blank frame mismatch");}
            var animationData=helper.GameContent.Load<Dictionary<string,string>>("Data/animationDescriptions");
            var animationSteps=0;
            foreach(var key in new[]{"alex_beach_towel"})
            foreach(var section in animationData[key].Split('/').Take(3))
            {
                var frames=section.Split(' ',StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
                if(frames.Any(frame=>frame<0||frame>=19))throw new Exception("Native towel animation references empty or invalid frame");
                npc.Sprite.loop=true;
                npc.Sprite.setCurrentAnimation(frames.Select(frame=>new FarmerSprite.AnimationFrame(frame,150)).ToList());
                for(var step=1;step<=frames.Length*2;step++)
                {
                    npc.Sprite.animateOnce(new GameTime(TimeSpan.FromMilliseconds(step*150),TimeSpan.FromMilliseconds(150)));
                    if(npc.Sprite.CurrentFrame!=frames[step%frames.Length])throw new Exception("Native towel animation playback mismatch: "+key);
                    animationSteps++;
                }
            }
            var nativeTowelBehaviors=0;
            foreach(var key in new[]{"alex_beach_towel"})
            {
                var towelNpc=new NPC(new AnimatedSprite("Characters/Alex",0,16,32),Vector2.Zero,2,"Alex"){currentLocation=location};
                towelNpc.wearIslandAttire();
                var behavior=helper.Reflection.GetMethod(towelNpc,"getRouteEndBehaviorFunction").Invoke<Delegate>(key,null);
                if(behavior==null)throw new Exception("Native towel behavior missing: "+key);
                behavior.DynamicInvoke(towelNpc,location);
                // Alex has laying_down metadata only; Maru's separate offset 0 16 does not apply.
                if(animationData[key].Split('/').Skip(4).Any(section=>section.StartsWith("offset ")))throw new Exception("Alex native towel metadata gained an offset");
                var expectedOffset=Vector2.Zero;
                if(!towelNpc.layingDown||!towelNpc.HideShadow||towelNpc.drawOffset!=expectedOffset)throw new Exception($"Native towel layout flags differ: {key}; layingDown={towelNpc.layingDown}, HideShadow={towelNpc.HideShadow}, offset={towelNpc.drawOffset}");
                nativeTowelBehaviors++;
            }
            var walkingSteps = 0;
            foreach (var asset in new[] { "Characters/Alex_Beach" })
            {
                var sprite = new AnimatedSprite(asset, 0, 16, 32);
                for (var direction = 0; direction < 4; direction++)
                {
                    sprite.CurrentFrame = direction * 4;
                    sprite.timer = 0;
                    for (var step = 1; step <= 8; step++)
                    {
                        var time = new GameTime(TimeSpan.FromMilliseconds(step * 180), TimeSpan.FromMilliseconds(180));
                        switch (direction)
                        {
                            case 0: sprite.AnimateDown(time); break;
                            case 1: sprite.AnimateRight(time); break;
                            case 2: sprite.AnimateUp(time); break;
                            case 3: sprite.AnimateLeft(time); break;
                        }
                        var expected = direction * 4 + step % 4;
                        if (sprite.CurrentFrame != expected || sprite.SourceRect != new Rectangle(expected % 4 * 16, expected / 4 * 32, 16, 32))
                            throw new Exception("Timed walking frame mismatch: " + asset);
                        walkingSteps++;
                    }
                }
            }

            Equal(npc.Portrait,helper.GameContent.Load<Texture2D>("Portraits/Alex_Beach"));
            helper.Data.WriteJsonFile("alex-beach-texture-details.json",new{npc.Portrait.Width,npc.Portrait.Height,TextureType=npc.Portrait.GetType().FullName});
            using(var portraitFile=File.Create(Path.Combine(helper.DirectoryPath,"alex-beach-loaded-portrait.png")))npc.Portrait.SaveAsPng(portraitFile,npc.Portrait.Width,npc.Portrait.Height);
            var device=Game1.graphics.GraphicsDevice;var previous=device.GetRenderTargets();using var target=new RenderTarget2D(device,1024,1024);using var batch=new SpriteBatch(device);
            try{device.SetRenderTarget(target);device.Clear(new Color(35,45,58));batch.Begin(samplerState:SamplerState.PointClamp);for(var i=0;i<20;i++)batch.Draw(texture,new Rectangle(i%8*64+8,i/8*132+8,48,96),new Rectangle(i%4*16,i/4*32,16,32),Color.White);batch.Draw(npc.Portrait,new Rectangle(600,16,384,960),Color.White);batch.End();device.SetRenderTargets(previous);using var f=File.Create(Path.Combine(helper.DirectoryPath,"alex-beach-runtime-preview.png"));target.SaveAsPng(f,target.Width,target.Height);}finally{device.SetRenderTargets(previous);}
            npc.wearNormalClothes();Equal(npc.Sprite.Texture,helper.GameContent.Load<Texture2D>("Characters/Alex"));Equal(npc.Portrait,helper.GameContent.Load<Texture2D>("Portraits/Alex"));
            helper.Data.WriteJsonFile("alex-beach-checks.json",new{Passed=true,OccupiedFrames=19,BlankFrames=1,PortraitSlots=10,NativeBeachSelected=true,NormalOutfitRestored=true,NativeTowelSteps=animationSteps,NativeWalkingSteps=walkingSteps,NativeTowelBehaviors=nativeTowelBehaviors,RecliningSceneVerified=false,FarmLoaded=false,FullEventPlayback=false});
            monitor.Log("Alex beach audit passed:19 poses,1 blank,10 portrait slots, native beach selection and normal outfit restoration; event playback remains unverified.",LogLevel.Info);
        }
        catch(Exception ex){helper.Data.WriteJsonFile("alex-beach-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log("Alex beach audit failed: "+ex,LogLevel.Error);}
        finally{Game1.season=oldSeason;Game1.currentLocation=oldLocation;}
    }
}



