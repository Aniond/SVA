using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NpcArtAudit;
internal static class AlexWinterAudit
{
    public static void Run(IModHelper helper,IMonitor monitor)
    {
        var oldSeason=Game1.season;var oldLocation=Game1.currentLocation;
        try
        {
            Game1.season=Season.Winter;
            var location=new GameLocation("Maps/Town","Town");Game1.currentLocation=location;
            var npc=new NPC(new AnimatedSprite("Characters/Alex",0,16,32),Vector2.Zero,2,"Alex"){currentLocation=location};
            npc.ChooseAppearance();
            if((npc.Sprite.overrideTextureName??npc.Sprite.textureName.Value).Replace('\\','/')!="Characters/Alex_Winter")throw new Exception("Native winter appearance not selected");
            void Equal(Texture2D a,Texture2D b){var pa=new Color[a.Width*a.Height];var pb=new Color[b.Width*b.Height];a.GetData(pa);b.GetData(pb);if(!pa.SequenceEqual(pb))throw new Exception("Native texture pixels differ");}
            Equal(npc.Sprite.Texture,helper.GameContent.Load<Texture2D>("Characters/Alex_Winter"));
            Equal(npc.Portrait,helper.GameContent.Load<Texture2D>("Portraits/Alex_Winter"));
            for(var i=0;i<12;i++){var dialogue=new Dialogue(npc,null,"Winter portrait check.$"+i);if(dialogue.getPortraitIndex()!=i)throw new Exception("Portrait index mismatch "+i);}
            var portrait=npc.Portrait;var portraitPixels=new Color[portrait.Width*portrait.Height];portrait.GetData(portraitPixels);
            for(var index=0;index<12;index++) { var visible=false; for(var y=0;y<64;y++)for(var x=0;x<64;x++)if(portraitPixels[(index/2*64+y)*portrait.Width+index%2*64+x].A>0)visible=true; if(!visible)throw new Exception("Empty portrait expression "+index); }
            var texture=npc.Sprite.Texture;var pixels=new Color[texture.Width*texture.Height];texture.GetData(pixels);
            for(var frame=0;frame<52;frame++){npc.Sprite.CurrentFrame=frame;var rect=npc.Sprite.SourceRect;if(rect.X!=frame%4*16||rect.Y!=frame/4*32)throw new Exception("Frame layout mismatch");var visible=0;for(var y=rect.Y;y<rect.Bottom;y++)for(var x=rect.X;x<rect.Right;x++)if(pixels[y*64+x].A>0)visible++;if((frame!=43)!=(visible>0))throw new Exception("Alex occupied/blank mismatch "+frame);}
            var nativeEvent=helper.GameContent.Load<Dictionary<string,string>>("Data/Events/Beach").First(pair=>pair.Key.StartsWith("288847/"));
            var eventFrames=nativeEvent.Value.Split('/').Where(command=>command.StartsWith("showFrame Alex ")).Select(command=>int.Parse(command.Split(' ')[2])).ToArray();
            if(!eventFrames.Contains(35)||!eventFrames.Contains(38))throw new Exception("Native music-box event frames missing");
            foreach(var frame in eventFrames){if(frame<0||frame>=52||frame==43)throw new Exception("Native music-box event references empty/invalid frame");npc.Sprite.CurrentFrame=frame;if(npc.Sprite.SourceRect!=new Rectangle(frame%4*16,frame/4*32,16,32))throw new Exception("Native music-box event frame layout mismatch");}
            helper.Data.WriteJsonFile("alex-winter-native-event.json",new{Key=nativeEvent.Key,Frames=eventFrames,FullEventPlayback=false});
            var animationData=helper.GameContent.Load<Dictionary<string,string>>("Data/animationDescriptions");
            var animationSteps=0;
            foreach(var key in new[]{"alex_football","alex_lift_weights","alex_sit_left","alex_sleep"})
            foreach(var section in animationData[key].Split('/').Take(3))
            {
                var frames=section.Split(' ',StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
                if(frames.Any(frame=>frame<0||frame>=52))throw new Exception("Native animation references invalid frame");
                npc.Sprite.loop=true;
                npc.Sprite.setCurrentAnimation(frames.Select(frame=>new FarmerSprite.AnimationFrame(frame,150)).ToList());
                for(var step=1;step<=frames.Length*2;step++)
                {
                    npc.Sprite.animateOnce(new GameTime(TimeSpan.FromMilliseconds(step*150),TimeSpan.FromMilliseconds(150)));
                    if(npc.Sprite.CurrentFrame!=frames[step%frames.Length])throw new Exception("Native animation playback mismatch: "+key);
                    animationSteps++;
                }
            }
            var walkingSteps = 0;
            foreach (var asset in new[] { "Characters/Alex_Winter" })
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

            var device=Game1.graphics.GraphicsDevice;var previous=device.GetRenderTargets();using var target=new RenderTarget2D(device,1024,1216);using var batch=new SpriteBatch(device);
            try{device.SetRenderTarget(target);device.Clear(new Color(35,45,58));batch.Begin(samplerState:SamplerState.PointClamp);for(var i=0;i<52;i++)batch.Draw(texture,new Rectangle(i%8*64+8,i/8*132+8,48,96),new Rectangle(i%4*16,i/4*32,16,32),Color.White);batch.Draw(npc.Portrait,new Rectangle(600,16,384,1152),Color.White);batch.End();device.SetRenderTargets(previous);using var f=File.Create(Path.Combine(helper.DirectoryPath,"alex-winter-runtime-preview.png"));target.SaveAsPng(f,target.Width,target.Height);}finally{device.SetRenderTargets(previous);}
            Game1.season=Season.Spring;npc.ChooseAppearance();Equal(npc.Sprite.Texture,helper.GameContent.Load<Texture2D>("Characters/Alex"));Equal(npc.Portrait,helper.GameContent.Load<Texture2D>("Portraits/Alex"));
            helper.Data.WriteJsonFile("alex-winter-checks.json",new{Passed=true,OccupiedArtworkFrames=51,BlankFrames=1,PortraitSlots=12,NativeWinterSelected=true,SpringRestored=true,NativeFootballWeightsSitSleepSteps=animationSteps,NativeWalkingSteps=walkingSteps,NativeMusicBoxEventFrames=eventFrames.Length,FarmLoaded=false,FullEventPlayback=false});
            monitor.Log("Alex winter audit passed:51 poses,1 blank,12 portrait slots, native winter selection and spring restoration; event playback remains unverified.",LogLevel.Info);
        }
        catch(Exception ex){helper.Data.WriteJsonFile("alex-winter-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log("Alex winter audit failed: "+ex,LogLevel.Error);}
        finally{Game1.season=oldSeason;Game1.currentLocation=oldLocation;}
    }
}

