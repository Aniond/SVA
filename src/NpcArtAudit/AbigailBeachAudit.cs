using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NpcArtAudit;
internal static class AbigailBeachAudit
{
    public static void Run(IModHelper helper,IMonitor monitor)
    {
        var oldSeason=Game1.season;var oldLocation=Game1.currentLocation;
        try
        {
            Game1.season=Season.Summer;
            var location=new GameLocation("Maps/Town","Town");Game1.currentLocation=location;
            var npc=new NPC(new AnimatedSprite("Characters/Abigail",0,16,32),Vector2.Zero,2,"Abigail"){currentLocation=location};
            npc.wearIslandAttire();
            if((npc.Sprite.overrideTextureName??npc.Sprite.textureName.Value).Replace('\\','/')!="Characters/Abigail_Beach")throw new Exception("Native beach appearance not selected");
            void Equal(Texture2D a,Texture2D b){var pa=new Color[a.Width*a.Height];var pb=new Color[b.Width*b.Height];a.GetData(pa);b.GetData(pb);if(!pa.SequenceEqual(pb))throw new Exception("Native texture pixels differ");}
            Equal(npc.Sprite.Texture,helper.GameContent.Load<Texture2D>("Characters/Abigail_Beach"));
            Equal(npc.Portrait,helper.GameContent.Load<Texture2D>("Portraits/Abigail_Beach"));
            for(var i=0;i<10;i++){var dialogue=new Dialogue(npc,null,"Beach portrait check.$"+i);if(dialogue.getPortraitIndex()!=i)throw new Exception("Portrait index mismatch "+i);}
            var texture=npc.Sprite.Texture;var pixels=new Color[texture.Width*texture.Height];texture.GetData(pixels);
            for(var frame=0;frame<20;frame++){npc.Sprite.CurrentFrame=frame;var rect=npc.Sprite.SourceRect;if(rect.X!=frame%4*16||rect.Y!=frame/4*32)throw new Exception("Frame layout mismatch");var visible=0;for(var y=rect.Y;y<rect.Bottom;y++)for(var x=rect.X;x<rect.Right;x++)if(pixels[y*64+x].A>0)visible++;if((frame<19)!=(visible>0))throw new Exception("Occupied/blank frame mismatch");}
            var device=Game1.graphics.GraphicsDevice;var previous=device.GetRenderTargets();using var target=new RenderTarget2D(device,1024,1024);using var batch=new SpriteBatch(device);
            try{device.SetRenderTarget(target);device.Clear(new Color(35,45,58));batch.Begin(samplerState:SamplerState.PointClamp);for(var i=0;i<20;i++)batch.Draw(texture,new Rectangle(i%8*64+8,i/8*132+8,48,96),new Rectangle(i%4*16,i/4*32,16,32),Color.White);batch.Draw(npc.Portrait,new Rectangle(600,16,384,960),Color.White);batch.End();device.SetRenderTargets(previous);using var f=File.Create(Path.Combine(helper.DirectoryPath,"abigail-beach-runtime-preview.png"));target.SaveAsPng(f,1024,1024);}finally{device.SetRenderTargets(previous);}
            npc.wearNormalClothes();Equal(npc.Sprite.Texture,helper.GameContent.Load<Texture2D>("Characters/Abigail"));Equal(npc.Portrait,helper.GameContent.Load<Texture2D>("Portraits/Abigail"));
            helper.Data.WriteJsonFile("abigail-beach-checks.json",new{Passed=true,OccupiedFrames=19,BlankFrames=1,PortraitSlots=10,NativeBeachSelected=true,NormalOutfitRestored=true,FarmLoaded=false,FullEventPlayback=false});
            monitor.Log("Abigail beach audit passed:19 poses,10 portrait slots, native beach selection and normal outfit restoration; event playback remains unverified.",LogLevel.Info);
        }
        catch(Exception ex){helper.Data.WriteJsonFile("abigail-beach-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log("Abigail beach audit failed: "+ex,LogLevel.Error);}
        finally{Game1.season=oldSeason;Game1.currentLocation=oldLocation;}
    }
}

