using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NpcArtAudit;
internal static class ElliottBaseAudit
{
    public static void Run(IModHelper helper,IMonitor monitor)
    {
        var oldSeason=Game1.season;var oldLocation=Game1.currentLocation;
        try
        {
            Game1.season=Season.Spring;
            var location=new GameLocation("Maps/Town","Town");Game1.currentLocation=location;
            var npc=new NPC(new AnimatedSprite("Characters/Elliott",0,16,32),Vector2.Zero,2,"Elliott"){currentLocation=location};
            npc.ChooseAppearance();
            if((npc.Sprite.overrideTextureName??npc.Sprite.textureName.Value).Replace('\\','/')!="Characters/Elliott")throw new Exception("Native everyday appearance not selected");
            void Equal(Texture2D a,Texture2D b){var pa=new Color[a.Width*a.Height];var pb=new Color[b.Width*b.Height];a.GetData(pa);b.GetData(pb);if(!pa.SequenceEqual(pb))throw new Exception("Native texture pixels differ");}
            Equal(npc.Sprite.Texture,helper.GameContent.Load<Texture2D>("Characters/Elliott"));
            Equal(npc.Portrait,helper.GameContent.Load<Texture2D>("Portraits/Elliott"));
            for(var i=0;i<10;i++){var dialogue=new Dialogue(npc,null,"Everyday portrait check.$"+i);if(dialogue.getPortraitIndex()!=i)throw new Exception("Portrait index mismatch "+i);}
            var portrait=npc.Portrait;var portraitPixels=new Color[portrait.Width*portrait.Height];portrait.GetData(portraitPixels);
            for(var index=0;index<10;index++) { var visible=false; for(var y=0;y<64;y++)for(var x=0;x<64;x++)if(portraitPixels[(index/2*64+y)*portrait.Width+index%2*64+x].A>0)visible=true; if(!visible)throw new Exception("Empty portrait expression "+index); }
            var texture=npc.Sprite.Texture;var pixels=new Color[texture.Width*texture.Height];texture.GetData(pixels);
            var winterTexture=helper.GameContent.Load<Texture2D>("Characters/Elliott_Winter");
            var winterPixels=new Color[winterTexture.Width*winterTexture.Height];winterTexture.GetData(winterPixels);
            foreach(var retained in new[]{44,45,46,47,48,49,50})for(var y=0;y<32;y++)for(var x=0;x<16;x++){
                var offset=(retained/4*32+y)*64+retained%4*16+x;
                if(pixels[offset]!=winterPixels[offset])throw new Exception("Reused winter pose differs: "+retained);
            }
            for(var frame=0;frame<52;frame++){npc.Sprite.CurrentFrame=frame;var rect=npc.Sprite.SourceRect;if(rect.X!=frame%4*16||rect.Y!=frame/4*32)throw new Exception("Frame layout mismatch");var visible=0;for(var y=rect.Y;y<rect.Bottom;y++)for(var x=rect.X;x<rect.Right;x++)if(pixels[y*64+x].A>0)visible++;if((frame!=23)!=(visible>0))throw new Exception("Elliott occupied/blank mismatch "+frame);}
            var nativeEvents=new[]{
                helper.GameContent.Load<Dictionary<string,string>>("Data/Events/ArchaeologyHouse").First(pair=>pair.Key.StartsWith("1848481/")),
                helper.GameContent.Load<Dictionary<string,string>>("Data/Events/ElliottHouse").First(pair=>pair.Key.StartsWith("423502/")),
                helper.GameContent.Load<Dictionary<string,string>>("Data/Events/Farm").First(pair=>pair.Key.StartsWith("3912125/"))
            };
            var eventFrames=new List<int>();
            foreach(var nativeEvent in nativeEvents)foreach(var command in nativeEvent.Value.Split('/')){
                var args=command.Split(' ',StringSplitOptions.RemoveEmptyEntries);
                if(args.Length<3||args[1]!="Elliott")continue;
                if(args[0]=="showFrame")eventFrames.Add(int.Parse(args[2]));
                if(args[0]=="animate")eventFrames.AddRange(args.Skip(5).Select(int.Parse));
            }
            foreach(var required in new[]{25,26,27,32,33,34,38,39})if(!eventFrames.Contains(required))throw new Exception("Required native event frame absent: "+required);
            foreach(var frame in eventFrames){if(frame<0||frame>=52||frame==23)throw new Exception("Native event references empty/invalid frame");npc.Sprite.CurrentFrame=frame;if(npc.Sprite.SourceRect!=new Rectangle(frame%4*16,frame/4*32,16,32))throw new Exception("Native event frame layout mismatch");}
            helper.Data.WriteJsonFile("elliott-base-native-events.json",new{Keys=nativeEvents.Select(e=>e.Key),Frames=eventFrames,FullEventPlayback=false});
            var animationData=helper.GameContent.Load<Dictionary<string,string>>("Data/animationDescriptions");
            var animationSteps=0;
            foreach(var key in new[]{"elliott_read","elliott_sit_down","elliott_drink","elliott_sleep"})
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
            foreach (var asset in new[] { "Characters/Elliott", "Characters/Elliott_Winter", "Characters/Elliott_Beach" })
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

            var festival=helper.GameContent.Load<Dictionary<string,string>>("Data/Festivals/winter8");
            var scene=new Event();scene.actors.Add(npc);helper.Reflection.GetField<bool>(scene,"eventFinished").SetValue(true);
            npc.Sprite.CurrentFrame=40;
            var extend=festival["mainEvent"].Split('/').Single(c=>c.StartsWith("extendSourceRect Elliott ")).Split(' ');
            Event.DefaultCommands.ExtendSourceRect(scene,extend,new EventContext(scene,location,Game1.currentGameTime,extend));
            if(npc.Sprite.SourceRect!=new Rectangle(0,320,32,32))throw new Exception("Native fishing extension mismatch");
            var fishingLayouts=0;
            for(var step=0;step<8;step++){
                npc.Sprite.sourceRect.Offset(npc.Sprite.SourceRect.Width,0);
                if(npc.Sprite.SourceRect.X>=npc.Sprite.Texture.Width)npc.Sprite.sourceRect.Offset(-npc.Sprite.Texture.Width,0);
                if(npc.Sprite.SourceRect!=new Rectangle(step%2==0?32:0,320,32,32))throw new Exception("Wide fishing frame wrap mismatch");
                fishingLayouts++;
            }
            var reset=festival["afterIceFishing"].Split('/').Single(c=>c=="extendSourceRect Elliott reset").Split(' ');
            Event.DefaultCommands.ExtendSourceRect(scene,reset,new EventContext(scene,location,Game1.currentGameTime,reset));
            npc.Sprite.CurrentFrame=0;
            if(npc.Sprite.SourceRect.Width!=16||npc.Sprite.SourceRect.Height!=32)throw new Exception("Native fishing reset mismatch");
            var device=Game1.graphics.GraphicsDevice;var previous=device.GetRenderTargets();using var target=new RenderTarget2D(device,1024,1216);using var batch=new SpriteBatch(device);
            try{device.SetRenderTarget(target);device.Clear(new Color(35,45,58));batch.Begin(samplerState:SamplerState.PointClamp);for(var i=0;i<52;i++)batch.Draw(texture,new Rectangle(i%8*64+8,i/8*132+8,48,96),new Rectangle(i%4*16,i/4*32,16,32),Color.White);batch.Draw(npc.Portrait,new Rectangle(600,16,384,960),Color.White);for(var fishingPose=0;fishingPose<2;fishingPose++)batch.Draw(texture,new Rectangle(16+fishingPose*180,976,128,128),new Rectangle(fishingPose*32,320,32,32),Color.White);batch.End();device.SetRenderTargets(previous);using var f=File.Create(Path.Combine(helper.DirectoryPath,"elliott-base-runtime-preview.png"));target.SaveAsPng(f,target.Width,target.Height);}finally{device.SetRenderTargets(previous);}
            Game1.season=Season.Winter;npc.ChooseAppearance();Equal(npc.Sprite.Texture,helper.GameContent.Load<Texture2D>("Characters/Elliott_Winter"));Equal(npc.Portrait,helper.GameContent.Load<Texture2D>("Portraits/Elliott_Winter"));
            helper.Data.WriteJsonFile("elliott-base-checks.json",new{Passed=true,OccupiedArtworkCells=51,CompletePoses=49,BlankFrames=1,PortraitSlots=10,NativeEverydaySelected=true,WinterSelected=true,RetainedWinterFrames=7,NativeReadSitDrinkSleepSteps=animationSteps,NativeWalkingSteps=walkingSteps,NativeEventFrameReferences=eventFrames.Count,NativeFishingLayouts=fishingLayouts,NativeFishingExtendReset=true,FarmLoaded=false,FullEventPlayback=false});
            monitor.Log("Elliott everyday audit passed:51 occupied cells,1 blank,10 portrait slots, native everyday selection and winter transition; event playback remains unverified.",LogLevel.Info);
        }
        catch(Exception ex){helper.Data.WriteJsonFile("elliott-base-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log("Elliott everyday audit failed: "+ex,LogLevel.Error);}
        finally{Game1.season=oldSeason;Game1.currentLocation=oldLocation;}
    }
}

