using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NpcArtAudit;

internal static class ClothesTherapyAudit
{
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        var previousLocation = Game1.currentLocation;
        try
        {
            var location = new GameLocation(); Game1.currentLocation = location;
            var script = helper.GameContent.Load<Dictionary<string,string>>("Data/Events/ManorHouse").Values.Single(s => s.Contains("addTemporaryActor ClothesTherapyCharacters"));
            var commands = Event.ParseCommands(script).Select(s => ArgUtility.SplitBySpaceQuoteAware(s)).ToArray();
            var scene = new Event(); helper.Reflection.GetField<bool>(scene,"eventFinished").SetValue(true);
            var create = commands.Single(a => a.Length > 1 && a[0] == "addTemporaryActor" && a[1] == "ClothesTherapyCharacters");
            Event.DefaultCommands.AddTemporaryActor(scene,create,new EventContext(scene,location,Game1.currentGameTime,create));
            var actor = scene.getActorByName("ClothesTherapyCharacters") ?? throw new Exception("Native costume actor absent");
            var texture = actor.Sprite.Texture;
            if(texture.Width != 64 || texture.Height != 192 || actor.Sprite.SpriteWidth != 16 || actor.Sprite.SpriteHeight != 32) throw new Exception("Native geometry differs");
            var pixels = new Color[64*192]; texture.GetData(pixels);
            var walking = 0; var offsets = 0; var showFrames = new List<int>();
            var device = Game1.graphics.GraphicsDevice;
            using var batch = new SpriteBatch(device);
            using var rendered = new RenderTarget2D(device,64,128);
            void CheckDraw(int frame, int offset)
            {
                var rect = new Rectangle(frame%4*16,frame/4*32+offset,16,32);
                if(rect.Bottom > texture.Height) throw new Exception("Costume frame exceeds atlas");
                var previous = device.GetRenderTargets();
                try
                {
                    device.SetRenderTarget(rendered); device.Clear(Color.Transparent);
                    batch.Begin(blendState:BlendState.Opaque,samplerState:SamplerState.PointClamp);
                    actor.Sprite.draw(batch,new Vector2(32,96),1f,0,offset,Color.White,false,4f,0f,true);
                    batch.End(); device.SetRenderTargets(previous);
                    var actual = new Color[64*128]; rendered.GetData(actual);
                    for(var y=0;y<128;y++) for(var x=0;x<64;x++)
                        if(actual[y*64+x] != pixels[(rect.Y+y/4)*64+rect.X+x/4]) throw new Exception("Native offset draw differs");
                }
                finally { device.SetRenderTargets(previous); }
            }
            foreach(var offset in new[]{0,32,64,96,128})
            {
                actor.Sprite.StopAnimation(); actor.Sprite.CurrentFrame=0; actor.Sprite.timer=0;
                for(var step=1;step<=8;step++)
                {
                    actor.Sprite.AnimateDown(new GameTime(TimeSpan.FromMilliseconds(step*180),TimeSpan.FromMilliseconds(180)));
                    if(actor.Sprite.CurrentFrame != step%4) throw new Exception("Native front walk differs");
                    CheckDraw(step%4,offset); walking++;
                }
            }
            foreach(var args in commands.Where(a=>a.Length>1 && a[1]=="ClothesTherapyCharacters"))
            {
                var context = new EventContext(scene,location,Game1.currentGameTime,args);
                if(args[0]=="changeYSourceRectOffset")
                {
                    Event.DefaultCommands.ChangeYSourceRectOffset(scene,args,context);
                    if(actor.ySourceRectOffset != int.Parse(args[2])) throw new Exception("Native row selection differs");
                    offsets++;
                }
                if(args[0]=="showFrame")
                {
                    actor.Sprite.StopAnimation(); Event.DefaultCommands.ShowFrame(scene,args,context);
                    var frame=int.Parse(args[2]); if(actor.Sprite.CurrentFrame!=frame) throw new Exception("Native reaction frame differs");
                    CheckDraw(frame,actor.ySourceRectOffset); showFrames.Add(frame+actor.ySourceRectOffset/32*4);
                }
            }
            if(offsets!=4 || !showFrames.SequenceEqual(new[]{21,20,22,23,12,22})) throw new Exception("Native costume event mapping differs");
            var clint=new NPC(new AnimatedSprite("Characters/Clint",0,16,32),Vector2.Zero,2,"Clint");
            foreach(var index in new[]{6,7}) if(new Dialogue(clint,null,"Costume.$"+index).getPortraitIndex()!=index) throw new Exception("Clint portrait fallback differs");
            var previousTargets=device.GetRenderTargets(); using var preview=new RenderTarget2D(device,768,800);
            try
            {
                device.SetRenderTarget(preview);device.Clear(new Color(35,45,58));batch.Begin(samplerState:SamplerState.PointClamp);
                batch.Draw(texture,new Rectangle(8,8,256,768),Color.White);
                var portraits=helper.GameContent.Load<Texture2D>("Portraits/Clint");
                foreach(var index in new[]{6,7})batch.Draw(portraits,new Rectangle(300+(index-6)*220,20,192,192),new Rectangle(index%2*64,index/2*64,64,64),Color.White);
                batch.End();device.SetRenderTargets(previousTargets);using var output=File.Create(Path.Combine(helper.DirectoryPath,"clothes-therapy-runtime-preview.png"));preview.SaveAsPng(output,preview.Width,preview.Height);
            }
            finally {device.SetRenderTargets(previousTargets);}
            helper.Data.WriteJsonFile("clothes-therapy-checks.json",new{Passed=true,NativeTemporaryActor=true,CostumePoses=24,WalkingDraws=walking,NativeOffsetCommands=offsets,NativeShowFrameDraws=showFrames,ClintPortraitFallbacks=new[]{6,7},FarmLoaded=false,FullEventPlayback=false});
            monitor.Log("Clothing therapy art and native offset draws passed; full event playback remains unverified.",LogLevel.Info);
        }
        catch(Exception ex){helper.Data.WriteJsonFile("clothes-therapy-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log("Clothing therapy audit failed: "+ex,LogLevel.Error);}
        finally {Game1.currentLocation=previousLocation;}
    }
}
