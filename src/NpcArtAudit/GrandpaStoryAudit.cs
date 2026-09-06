using System.Runtime.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Minigames;

namespace NpcArtAudit;

internal static class GrandpaStoryAudit
{
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        var oldViewport=Game1.viewport;
        var device=Game1.graphics.GraphicsDevice;
        var oldTargets=device.GetRenderTargets();
        try
        {
            using var content=Game1.content.CreateTemporary();
            var texture=content.Load<Texture2D>("Minigames/jojacorps");
            if(texture.Width!=1200||texture.Height!=800)throw new Exception("Opening atlas dimensions differ");
            var pixels=new Color[1200*800];texture.GetData(pixels);
            // Draw a controlled native scene without running the constructor's
            // player relocation, farmhouse loading or music changes.
            var story=(GrandpaStory)FormatterServices.GetUninitializedObject(typeof(GrandpaStory));
            void Set<T>(string key,T value)=>helper.Reflection.GetField<T>(story,key).SetValue(value);
            Set("texture",texture);Set("grandpaSpeech",new Queue<string>());Set("drawGrandpa",true);Set("scene",1);
            Set("letterPosition",new Vector2(600,731));Set("letterScale",1.5f);
            Game1.viewport=new xTile.Dimensions.Rectangle(0,0,1294,730);
            var states=new[]{(0,0,false,"quiet-light-a"),(200,0,false,"quiet-light-b"),(0,10000,false,"talking"),(200,10000,false,"quiet-mouth"),(400,28000,true,"empty-hand"),(100,28000,true,"hand-gesture")};
            var checkedPixels=0;
            foreach(var (time,speech,letter,name) in states)
            {
                Set("totalMilliseconds",time);Set("grandpaSpeechTimer",speech);Set("letterReceived",letter);
                using var target=new RenderTarget2D(device,1294,730);using var batch=new SpriteBatch(device);
                device.SetRenderTarget(target);device.Clear(Color.Magenta);story.draw(batch);device.SetRenderTargets(oldTargets);
                var rendered=new Color[1294*730];target.GetData(rendered);
                var bodyTop=time%300<150?290:50;
                for(var y=0;y<85;y++)for(var x=0;x<85;x++)
                {
                    var expected=pixels[(bodyTop+y)*1200+580+x];
                    if(speech>8000&&speech%10000<5000&&x>=36&&x<54&&y>=19&&y<37)
                        expected=pixels[(523+y-19)*1200+497+18*(time%400/200)+x-36];
                    if(letter&&x>=4&&x<41&&y>=63&&y<80)
                    {
                        var hand=speech>8000&&speech%10000>7000&&speech%10000<9000&&time%600<300?1:0;
                        expected=pixels[(556+y-63)*1200+463+hand*37+x-4];
                    }
                    var actual=rendered[(150+y*3+1)*1294+459+x*3+1];
                    if(actual!=expected)throw new Exception($"Native GrandpaStory draw differs in {name} at {x},{y}: {actual} != {expected}");
                    checkedPixels++;
                }
                using var file=File.Create(Path.Combine(helper.DirectoryPath,"grandpa-story-"+name+"-runtime-preview.png"));target.SaveAsPng(file,1294,730);
            }
            var placeholder=helper.GameContent.Load<Texture2D>("Characters/Grandpa");
            if(placeholder.Width!=1||placeholder.Height!=1)throw new Exception("Grandpa placeholder was replaced");
            var portrait=helper.GameContent.Load<Texture2D>("Portraits/Grandpa");
            if(portrait.Width!=128||portrait.Height!=64)throw new Exception("Spectral portraits differ");
            var cursors=helper.GameContent.Load<Texture2D>("LooseSprites/Cursors");var cursors2=helper.GameContent.Load<Texture2D>("LooseSprites/Cursors2");
            using(var target=new RenderTarget2D(device,768,288))using(var batch=new SpriteBatch(device))
            {
                device.SetRenderTarget(target);device.Clear(new Color(35,45,58));batch.Begin(samplerState:SamplerState.PointClamp);
                batch.Draw(cursors,new Rectangle(16,16,108,210),new Rectangle(555,1956,18,35),Color.White);
                batch.Draw(cursors2,new Rectangle(160,16,132,204),new Rectangle(186,265,22,34),Color.White);
                batch.Draw(portrait,new Rectangle(320,16,384,192),Color.White);batch.End();device.SetRenderTargets(oldTargets);
                using var file=File.Create(Path.Combine(helper.DirectoryPath,"grandpa-story-spectral-runtime-preview.png"));target.SaveAsPng(file,768,288);
            }
            helper.Data.WriteJsonFile("grandpa-story-checks.json",new{Passed=true,NativeDrawStates=states.Length,NativeScenePixelsCompared=checkedPixels,NativeTemporaryContentLoaded=true,OpeningBodyVariants=2,FaceFrames=2,HandFrames=2,SpectralPortraits=2,PlaceholderUnmodified=true,ConstructorSkippedForIsolation=true,FarmLoaded=false,FullScenePlayback=false});
            monitor.Log("Grandpa opening artwork passed native draw checks; complete story playback remains unverified.",LogLevel.Info);
        }
        catch(Exception ex){helper.Data.WriteJsonFile("grandpa-story-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log("Grandpa story audit failed: "+ex,LogLevel.Error);}
        finally{device.SetRenderTargets(oldTargets);Game1.viewport=oldViewport;}
    }
}
