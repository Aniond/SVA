using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NpcArtAudit;
internal static class HaleyBeachAudit
{
    public static void Run(IModHelper helper,IMonitor monitor)
    {
        var oldSeason=Game1.season;var oldLocation=Game1.currentLocation;
        try
        {
            Game1.season=Season.Summer;
            var location=new GameLocation("Maps/Town","Town");Game1.currentLocation=location;
            var npc=new NPC(new AnimatedSprite("Characters/Haley",0,16,32),Vector2.Zero,2,"Haley"){currentLocation=location};
            npc.wearIslandAttire();
            if((npc.Sprite.overrideTextureName??npc.Sprite.textureName.Value).Replace('\\','/')!="Characters/Haley_Beach")throw new Exception("Native beach appearance not selected");
            void Equal(Texture2D a,Texture2D b){var pa=new Color[a.Width*a.Height];var pb=new Color[b.Width*b.Height];a.GetData(pa);b.GetData(pb);if(!pa.SequenceEqual(pb))throw new Exception("Native texture pixels differ");}
            Equal(npc.Sprite.Texture,helper.GameContent.Load<Texture2D>("Characters/Haley_Beach"));
            Equal(npc.Portrait,helper.GameContent.Load<Texture2D>("Portraits/Haley_Beach"));
            for(var i=0;i<14;i++){var dialogue=new Dialogue(npc,null,"Beach portrait check.$"+i);if(dialogue.getPortraitIndex()!=i)throw new Exception("Portrait index mismatch "+i);}
            var texture=npc.Sprite.Texture;var pixels=new Color[texture.Width*texture.Height];texture.GetData(pixels);
            for(var frame=0;frame<24;frame++){npc.Sprite.CurrentFrame=frame;var rect=npc.Sprite.SourceRect;if(rect.X!=frame%4*16||rect.Y!=frame/4*32)throw new Exception("Frame layout mismatch");var visible=0;for(var y=rect.Y;y<rect.Bottom;y++)for(var x=rect.X;x<rect.Right;x++)if(pixels[y*64+x].A>0)visible++;if((frame!=23)!=(visible>0))throw new Exception("Occupied/blank frame mismatch");}
            var animationData=helper.GameContent.Load<Dictionary<string,string>>("Data/animationDescriptions");
            var animationSteps=0;
            foreach(var key in new[]{"haley_beach_towel","haley_beach_towel_2"})
            foreach(var section in animationData[key].Split('/').Take(3))
            {
                var frames=section.Split(' ',StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
                if(frames.Any(frame=>frame<0||frame>=23))throw new Exception("Native towel animation references empty or invalid frame");
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
            foreach(var key in new[]{"haley_beach_towel","haley_beach_towel_2"})
            {
                var towelNpc=new NPC(new AnimatedSprite("Characters/Haley",0,16,32),Vector2.Zero,2,"Haley"){currentLocation=location};
                towelNpc.wearIslandAttire();
                var behavior=helper.Reflection.GetMethod(towelNpc,"getRouteEndBehaviorFunction").Invoke<Delegate>(key,null);
                if(behavior==null)throw new Exception("Native towel behavior missing: "+key);
                behavior.DynamicInvoke(towelNpc,location);
                var expectedOffset=key=="haley_beach_towel"?new Vector2(0,16):Vector2.Zero;
                if(!towelNpc.layingDown||!towelNpc.HideShadow||towelNpc.drawOffset!=expectedOffset)throw new Exception("Native towel layout flags differ: "+key);
                nativeTowelBehaviors++;
            }
            var device=Game1.graphics.GraphicsDevice;var previous=device.GetRenderTargets();using var target=new RenderTarget2D(device,1024,1408);using var batch=new SpriteBatch(device);
            try{device.SetRenderTarget(target);device.Clear(new Color(35,45,58));batch.Begin(samplerState:SamplerState.PointClamp);for(var i=0;i<24;i++)batch.Draw(texture,new Rectangle(i%8*64+8,i/8*132+8,48,96),new Rectangle(i%4*16,i/4*32,16,32),Color.White);batch.Draw(npc.Portrait,new Rectangle(600,16,384,1344),Color.White);batch.End();device.SetRenderTargets(previous);using var f=File.Create(Path.Combine(helper.DirectoryPath,"haley-beach-runtime-preview.png"));target.SaveAsPng(f,1024,1408);}finally{device.SetRenderTargets(previous);}
            npc.wearNormalClothes();Equal(npc.Sprite.Texture,helper.GameContent.Load<Texture2D>("Characters/Haley"));Equal(npc.Portrait,helper.GameContent.Load<Texture2D>("Portraits/Haley"));
            helper.Data.WriteJsonFile("haley-beach-checks.json",new{Passed=true,OccupiedFrames=23,BlankFrames=1,PortraitSlots=14,NativeBeachSelected=true,NormalOutfitRestored=true,NativeTowelSteps=animationSteps,NativeTowelBehaviors=nativeTowelBehaviors,RecliningSceneVerified=false,FarmLoaded=false,FullEventPlayback=false});
            monitor.Log("Haley beach audit passed:23 poses,14 portrait slots, native beach selection and normal outfit restoration; event playback remains unverified.",LogLevel.Info);
        }
        catch(Exception ex){helper.Data.WriteJsonFile("haley-beach-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log("Haley beach audit failed: "+ex,LogLevel.Error);}
        finally{Game1.season=oldSeason;Game1.currentLocation=oldLocation;}
    }
}



