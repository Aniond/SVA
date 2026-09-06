using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NpcArtAudit;
internal static class PennyWinterAudit
{
    public static void Run(IModHelper helper,IMonitor monitor)
    {
        var oldSeason=Game1.season;var oldLocation=Game1.currentLocation;
        try
        {
            Game1.season=Season.Winter;
            var location=new GameLocation("Maps/Town","Town");Game1.currentLocation=location;
            var npc=new NPC(new AnimatedSprite("Characters/Penny",0,16,32),Vector2.Zero,2,"Penny"){currentLocation=location};
            npc.ChooseAppearance();
            if((npc.Sprite.overrideTextureName??npc.Sprite.textureName.Value).Replace('\\','/')!="Characters/Penny_Winter")throw new Exception("Native winter appearance not selected");
            void Equal(Texture2D a,Texture2D b){var pa=new Color[a.Width*a.Height];var pb=new Color[b.Width*b.Height];a.GetData(pa);b.GetData(pb);if(!pa.SequenceEqual(pb))throw new Exception("Native texture pixels differ");}
            Equal(npc.Sprite.Texture,helper.GameContent.Load<Texture2D>("Characters/Penny_Winter"));
            Equal(npc.Portrait,helper.GameContent.Load<Texture2D>("Portraits/Penny_Winter"));
            for(var i=0;i<14;i++){var dialogue=new Dialogue(npc,null,"Winter portrait check.$"+i);if(dialogue.getPortraitIndex()!=i)throw new Exception("Portrait index mismatch "+i);}
            var portrait=npc.Portrait;var portraitPixels=new Color[portrait.Width*portrait.Height];portrait.GetData(portraitPixels);
            for(var index=0;index<14;index++) { var visible=false; for(var y=0;y<64;y++)for(var x=0;x<64;x++)if(portraitPixels[(index/2*64+y)*portrait.Width+index%2*64+x].A>0)visible=true; if(!visible)throw new Exception("Empty portrait expression "+index); }
            var texture=npc.Sprite.Texture;var pixels=new Color[texture.Width*texture.Height];texture.GetData(pixels);
            for(var frame=0;frame<52;frame++){npc.Sprite.CurrentFrame=frame;var rect=npc.Sprite.SourceRect;if(rect.X!=frame%4*16||rect.Y!=frame/4*32)throw new Exception("Frame layout mismatch");var visible=0;for(var y=rect.Y;y<rect.Bottom;y++)for(var x=rect.X;x<rect.Right;x++)if(pixels[y*64+x].A>0)visible++;if(visible==0)throw new Exception("Unexpected empty Penny cell "+frame);if(frame>=49)for(var y=rect.Y;y<rect.Bottom;y++)for(var x=rect.X;x<rect.Right;x++)if(pixels[y*64+x]!=new Color(123,66,26,255))throw new Exception("Brown placeholder changed "+frame);}
            var animationData=helper.GameContent.Load<Dictionary<string,string>>("Data/animationDescriptions");
            var animationSteps=0;
            foreach(var key in new[]{"penny_dishes","penny_read","penny_sit_down","penny_wave_left","penny_sleep"})
            foreach(var section in animationData[key].Split('/').Take(3))
            {
                var frames=section.Split(' ',StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
                if(frames.Any(frame=>frame<0||frame>=49))throw new Exception("Native animation references invalid frame");
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
            foreach (var asset in new[] { "Characters/Penny_Winter" })
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

            var device=Game1.graphics.GraphicsDevice;var previous=device.GetRenderTargets();using var target=new RenderTarget2D(device,1024,1408);using var batch=new SpriteBatch(device);
            try{device.SetRenderTarget(target);device.Clear(new Color(35,45,58));batch.Begin(samplerState:SamplerState.PointClamp);for(var i=0;i<52;i++)batch.Draw(texture,new Rectangle(i%8*64+8,i/8*132+8,48,96),new Rectangle(i%4*16,i/4*32,16,32),Color.White);batch.Draw(npc.Portrait,new Rectangle(600,16,384,1344),Color.White);batch.End();device.SetRenderTargets(previous);using var f=File.Create(Path.Combine(helper.DirectoryPath,"penny-winter-runtime-preview.png"));target.SaveAsPng(f,target.Width,target.Height);}finally{device.SetRenderTargets(previous);}
            Game1.season=Season.Spring;npc.ChooseAppearance();Equal(npc.Sprite.Texture,helper.GameContent.Load<Texture2D>("Characters/Penny"));Equal(npc.Portrait,helper.GameContent.Load<Texture2D>("Portraits/Penny"));
            helper.Data.WriteJsonFile("penny-winter-checks.json",new{Passed=true,OccupiedArtworkFrames=49,BrownPlaceholderFrames=3,PortraitSlots=14,NativeWinterSelected=true,SpringRestored=true,NativeDishesReadSitWaveSleepSteps=animationSteps,NativeWalkingSteps=walkingSteps,FarmLoaded=false,FullEventPlayback=false});
            monitor.Log("Penny winter audit passed:49 poses,3 unchanged brown placeholders,14 portrait slots, native winter selection and spring restoration; event playback remains unverified.",LogLevel.Info);
        }
        catch(Exception ex){helper.Data.WriteJsonFile("penny-winter-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log("Penny winter audit failed: "+ex,LogLevel.Error);}
        finally{Game1.season=oldSeason;Game1.currentLocation=oldLocation;}
    }
}

