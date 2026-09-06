using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NpcArtAudit;
internal static class MaruHospitalAudit
{
    public static void Run(IModHelper helper,IMonitor monitor)
    {
        var oldSeason=Game1.season;var oldLocation=Game1.currentLocation;
        try
        {
            Game1.season=Season.Winter;
            var location=new GameLocation("Maps/Hospital","Hospital");Game1.currentLocation=location;
            var npc=new NPC(new AnimatedSprite("Characters/Maru",0,16,32),Vector2.Zero,2,"Maru"){currentLocation=location};
            helper.Data.WriteJsonFile("maru-hospital-native-events.json",helper.GameContent.Load<Dictionary<string,string>>("Data/Events/Hospital"));
            npc.ChooseAppearance();
            if((npc.Sprite.overrideTextureName??npc.Sprite.textureName.Value).Replace('\\','/')!="Characters/Maru_Hospital")throw new Exception("Native clinic appearance not selected");
            void Equal(Texture2D a,Texture2D b){var pa=new Color[a.Width*a.Height];var pb=new Color[b.Width*b.Height];a.GetData(pa);b.GetData(pb);if(!pa.SequenceEqual(pb))throw new Exception("Native texture pixels differ");}
            Equal(npc.Sprite.Texture,helper.GameContent.Load<Texture2D>("Characters/Maru_Hospital"));
            Equal(npc.Portrait,helper.GameContent.Load<Texture2D>("Portraits/Maru_Hospital"));
            for(var i=0;i<6;i++){var dialogue=new Dialogue(npc,null,"Clinic portrait check.$"+i);if(dialogue.getPortraitIndex()!=i)throw new Exception("Portrait index mismatch "+i);}
            var portrait=npc.Portrait;var portraitPixels=new Color[portrait.Width*portrait.Height];portrait.GetData(portraitPixels);
            for(var index=0;index<6;index++) { var visible=false; for(var y=0;y<64;y++)for(var x=0;x<64;x++)if(portraitPixels[(index/2*64+y)*portrait.Width+index%2*64+x].A>0)visible=true; if(!visible)throw new Exception("Empty portrait expression "+index); }
            var texture=npc.Sprite.Texture;var pixels=new Color[texture.Width*texture.Height];texture.GetData(pixels);
            for(var frame=0;frame<32;frame++){npc.Sprite.CurrentFrame=frame;var rect=npc.Sprite.SourceRect;if(rect.X!=frame%4*16||rect.Y!=frame/4*32)throw new Exception("Frame layout mismatch");var visible=0;for(var y=rect.Y;y<rect.Bottom;y++)for(var x=rect.X;x<rect.Right;x++)if(pixels[y*64+x].A>0)visible++;if((frame<=18||frame==28)!=(visible>0))throw new Exception("Occupied/blank frame mismatch");}
            var walkingSteps = 0;
            foreach (var asset in new[] { "Characters/Maru_Hospital" })
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

            var device=Game1.graphics.GraphicsDevice;var previous=device.GetRenderTargets();using var target=new RenderTarget2D(device,1024,1024);using var batch=new SpriteBatch(device);
            try{device.SetRenderTarget(target);device.Clear(new Color(35,45,58));batch.Begin(samplerState:SamplerState.PointClamp);for(var i=0;i<32;i++)batch.Draw(texture,new Rectangle(i%8*64+8,i/8*132+8,48,96),new Rectangle(i%4*16,i/4*32,16,32),Color.White);batch.Draw(npc.Portrait,new Rectangle(600,16,384,576),Color.White);batch.End();device.SetRenderTargets(previous);using var f=File.Create(Path.Combine(helper.DirectoryPath,"maru-hospital-runtime-preview.png"));target.SaveAsPng(f,target.Width,target.Height);}finally{device.SetRenderTargets(previous);}
            var appearanceCases=0;
            foreach(var season in new[]{Season.Spring,Season.Summer,Season.Fall,Season.Winter})
            {
                Game1.season=season;npc.currentLocation=location;Game1.currentLocation=location;npc.ChooseAppearance();
                Equal(npc.Sprite.Texture,helper.GameContent.Load<Texture2D>("Characters/Maru_Hospital"));Equal(npc.Portrait,helper.GameContent.Load<Texture2D>("Portraits/Maru_Hospital"));appearanceCases++;
                var town=new GameLocation("Maps/Town","Town");npc.currentLocation=town;Game1.currentLocation=town;npc.ChooseAppearance();
                var suffix=season==Season.Winter?"_Winter":"";Equal(npc.Sprite.Texture,helper.GameContent.Load<Texture2D>("Characters/Maru"+suffix));Equal(npc.Portrait,helper.GameContent.Load<Texture2D>("Portraits/Maru"+suffix));appearanceCases++;
            }
            helper.Data.WriteJsonFile("maru-hospital-checks.json",new{Passed=true,OccupiedFrames=20,BlankFrames=12,PortraitSlots=6,NativeClinicSelected=true,SeasonalOutfitsRestored=true,NativeAppearanceCases=appearanceCases,NativeWalkingSteps=walkingSteps,FarmLoaded=false,FullEventPlayback=false});
            monitor.Log("Maru clinic audit passed:20 poses,6 portrait expressions, clinic selection and seasonal outfit restoration; event playback remains unverified.",LogLevel.Info);
        }
        catch(Exception ex){helper.Data.WriteJsonFile("maru-hospital-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log("Maru clinic audit failed: "+ex,LogLevel.Error);}
        finally{Game1.season=oldSeason;Game1.currentLocation=oldLocation;}
    }
}

