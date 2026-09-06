using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;

namespace NpcArtAudit;
internal static class ChildAudit
{
    public static void Run(IModHelper helper,IMonitor monitor)
    {
        var oldTime=Game1.timeOfDay;var oldLocation=Game1.currentLocation;
        try
        {
            Game1.timeOfDay=1200;Game1.currentLocation=new GameLocation();
            var type=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="AbigailModern").GetType("AbigailModern.ChildPortraits")!;
            var records=new List<object>();var examples=new List<Child>();
            int Emotion(Child child)=>(int)type.GetMethod("ExpressionFor")!.Invoke(null,new object[]{child})!;
            foreach(var dark in new[]{false,true})foreach(var male in new[]{true,false})
            {
                var child=new Child("Robin's Little Star",male,dark,Game1.player);
                for(var age=0;age<4;age++)
                {
                    child.Age=age;child.reloadSprite(onlyAppearance:true);
                    var appearance=age<3?"Baby"+(dark?"_dark":""):"Toddler"+(male?"":"_girl")+(dark?"_dark":"");
                    if((string)type.GetMethod("AppearanceFor")!.Invoke(null,new object[]{child})! !=appearance)throw new Exception("Appearance mismatch "+appearance);
                    if(child.Sprite.textureName.Value.Replace('\\','/')!="Characters/"+appearance)throw new Exception("Native sprite path mismatch");
                    if(child.Sprite.SpriteWidth!=(age<3?22:16)||child.Sprite.SpriteHeight!=(age is 1 or 3?32:16))throw new Exception("Native frame size changed");
                    var portrait=helper.GameContent.Load<Texture2D>("Portraits/"+appearance);
                    if(portrait.Width!=128||portrait.Height!=192)throw new Exception("Portrait layout mismatch");
                    if(child.Name!="Robin's Little Star")throw new Exception("Child name changed");
                    if(Emotion(child)!=0)throw new Exception("Neutral state mismatch");
                    child.drawOnTop=true;if(Emotion(child)!=4)throw new Exception("Toss state mismatch");child.drawOnTop=false;
                    child.isSleeping.Value=true;if(Emotion(child)!=5)throw new Exception("Sleep state mismatch");child.isSleeping.Value=false;
                    records.Add(new{age,male,dark,appearance,Width=child.Sprite.SpriteWidth,Height=child.Sprite.SpriteHeight});
                }
                examples.Add(child);
            }
            var crawler=new Child("Alexandra Long Child Name",true,false,Game1.player);crawler.Age=2;crawler.reloadSprite(true);crawler.Sprite.CurrentFrame=40;
            if(Emotion(crawler)!=3)throw new Exception("Block-play state mismatch");
            Game1.timeOfDay=1800;if(Emotion(crawler)!=5)throw new Exception("Baby night state mismatch");Game1.timeOfDay=1200;
            crawler.doEmote(20,false);if(Emotion(crawler)!=1)throw new Exception("Heart reaction mismatch");
            examples[0].Sprite.CurrentFrame=16;if(Emotion(examples[0])!=4)throw new Exception("Toddler arm animation mismatch");
            var device=Game1.graphics.GraphicsDevice;var previous=device.GetRenderTargets();
            using var target=new RenderTarget2D(device,Game1.uiViewport.Width,Game1.uiViewport.Height);using var batch=new SpriteBatch(device);
            try
            {
                device.SetRenderTarget(target);device.Clear(new Color(35,45,58));batch.Begin(samplerState:SamplerState.PointClamp);
                var names=new[]{"Baby","Baby_dark","Toddler","Toddler_dark","Toddler_girl","Toddler_girl_dark"};
                for(var i=0;i<names.Length;i++)
                {
                    var texture=helper.GameContent.Load<Texture2D>("Portraits/"+names[i]);
                    batch.Draw(texture,new Rectangle(i*200+16,20,128,192),Color.White);
                    var sprite=helper.GameContent.Load<Texture2D>("Characters/"+names[i]);
                    batch.Draw(sprite,new Rectangle(i*200+24,240,sprite.Width*2,384),Color.White);
                }
                type.GetMethod("Draw")!.Invoke(null,new object[]{batch,crawler});
                batch.End();device.SetRenderTargets(previous);using var file=File.Create(Path.Combine(helper.DirectoryPath,"child-runtime-preview.png"));target.SaveAsPng(file,target.Width,target.Height);
            }
            finally{device.SetRenderTargets(previous);}
            helper.Data.WriteJsonFile("child-checks.json",new{Passed=true,GrowthCases=records,NamePreserved=true,PortraitStates=true,FarmLoaded=false,NativeTossExecuted=false,HatRenderingVerified=false});
            monitor.Log("Child checks passed: 16 growth/appearance cases, native frame sizes, portrait states and chosen name.",LogLevel.Info);
        }
        catch(Exception ex){helper.Data.WriteJsonFile("child-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log("Child audit failed: "+ex,LogLevel.Error);}
        finally{Game1.timeOfDay=oldTime;Game1.currentLocation=oldLocation;}
    }
}
