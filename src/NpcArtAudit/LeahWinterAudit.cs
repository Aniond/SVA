using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NpcArtAudit;
internal static class LeahWinterAudit
{
    public static void Run(IModHelper helper,IMonitor monitor)
    {
        var oldSeason=Game1.season;var oldLocation=Game1.currentLocation;
        try
        {
            Game1.season=Season.Winter;
            var location=new GameLocation("Maps/Town","Town");Game1.currentLocation=location;
            var npc=new NPC(new AnimatedSprite("Characters/Leah",0,16,32),Vector2.Zero,2,"Leah"){currentLocation=location};
            npc.ChooseAppearance();
            if((npc.Sprite.overrideTextureName??npc.Sprite.textureName.Value).Replace('\\','/')!="Characters/Leah_Winter")throw new Exception("Native winter appearance not selected");
            void Equal(Texture2D a,Texture2D b){var pa=new Color[a.Width*a.Height];var pb=new Color[b.Width*b.Height];a.GetData(pa);b.GetData(pb);if(!pa.SequenceEqual(pb))throw new Exception("Native texture pixels differ");}
            Equal(npc.Sprite.Texture,helper.GameContent.Load<Texture2D>("Characters/Leah_Winter"));
            Equal(npc.Portrait,helper.GameContent.Load<Texture2D>("Portraits/Leah_Winter"));
            for(var i=0;i<10;i++){var dialogue=new Dialogue(npc,null,"Winter portrait check.$"+i);if(dialogue.getPortraitIndex()!=i)throw new Exception("Portrait index mismatch "+i);}
            var texture=npc.Sprite.Texture;var pixels=new Color[texture.Width*texture.Height];texture.GetData(pixels);
            for(var frame=0;frame<52;frame++){npc.Sprite.CurrentFrame=frame;var rect=npc.Sprite.SourceRect;if(rect.X!=frame%4*16||rect.Y!=frame/4*32)throw new Exception("Frame layout mismatch");var visible=0;for(var y=rect.Y;y<rect.Bottom;y++)for(var x=rect.X;x<rect.Right;x++)if(pixels[y*64+x].A>0)visible++;if((frame!=39&&frame!=50)!=(visible>0))throw new Exception("Occupied/blank frame mismatch");}
            var animationData=helper.GameContent.Load<Dictionary<string,string>>("Data/animationDescriptions");
            var animationSteps=0;
            foreach(var key in new[]{"leah_sculpt","leah_draw","leah_sleep"})
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
            var device=Game1.graphics.GraphicsDevice;var previous=device.GetRenderTargets();using var target=new RenderTarget2D(device,1024,1024);using var batch=new SpriteBatch(device);
            try{device.SetRenderTarget(target);device.Clear(new Color(35,45,58));batch.Begin(samplerState:SamplerState.PointClamp);for(var i=0;i<52;i++)batch.Draw(texture,new Rectangle(i%8*64+8,i/8*132+8,48,96),new Rectangle(i%4*16,i/4*32,16,32),Color.White);batch.Draw(npc.Portrait,new Rectangle(600,16,384,960),Color.White);batch.End();device.SetRenderTargets(previous);using var f=File.Create(Path.Combine(helper.DirectoryPath,"leah-winter-runtime-preview.png"));target.SaveAsPng(f,target.Width,target.Height);}finally{device.SetRenderTargets(previous);}
            Game1.season=Season.Spring;npc.ChooseAppearance();Equal(npc.Sprite.Texture,helper.GameContent.Load<Texture2D>("Characters/Leah"));Equal(npc.Portrait,helper.GameContent.Load<Texture2D>("Portraits/Leah"));
            helper.Data.WriteJsonFile("leah-winter-checks.json",new{Passed=true,OccupiedFrames=50,BlankFrames=2,PortraitSlots=10,NativeWinterSelected=true,SpringRestored=true,NativeSculptDrawSleepSteps=animationSteps,FarmLoaded=false,FullEventPlayback=false});
            monitor.Log("Leah winter audit passed:50 poses,10 portrait slots, native winter selection and spring restoration; event playback remains unverified.",LogLevel.Info);
        }
        catch(Exception ex){helper.Data.WriteJsonFile("leah-winter-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log("Leah winter audit failed: "+ex,LogLevel.Error);}
        finally{Game1.season=oldSeason;Game1.currentLocation=oldLocation;}
    }
}

