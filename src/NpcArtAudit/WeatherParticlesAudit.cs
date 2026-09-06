using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Network;

namespace NpcArtAudit;

internal static class WeatherParticlesAudit
{
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        var device=Game1.graphics.GraphicsDevice;var targets=device.GetRenderTargets();var dv=device.Viewport;
        var blend=device.BlendState;var depth=device.DepthStencilState;var raster=device.RasterizerState;
        var sampler=device.SamplerStates[0];var texture=device.Textures[0];var scissor=device.ScissorRectangle;
        var location=Game1.currentLocation;var viewport=Game1.viewport;var random=Game1.random;var time=Game1.currentGameTime;
        var drops=Game1.rainDrops;var debris=Game1.debrisWeather;var snow=Game1.snowPos;var batch=Game1.spriteBatch;
        var eventUp=Game1.eventUp;var screenshot=Game1.game1.takingMapScreenshot;
        var contexts=Game1.netWorldState.Value.LocationWeather;
        bool hadWeather=contexts.TryGetValue("Default",out var originalWeather);
        var weather=new LocationWeather();
        int rainCases=0,snowCases=0,windCases=0,boltCases=0;string? error=null;
        try
        {
            void CheckDrawCache(Texture2D cached,string asset,IEnumerable<Rectangle> areas)
            {
                var loaded=helper.GameContent.Load<Texture2D>(asset);
                if(cached.Width!=loaded.Width||cached.Height!=loaded.Height)throw new Exception("Weather cache dimensions: "+asset);
                foreach(var area in areas){var a=new Color[area.Width*area.Height];var b=new Color[a.Length];cached.GetData(0,area,a,0,a.Length);loaded.GetData(0,area,b,0,b.Length);if(!a.SequenceEqual(b))throw new Exception("Stale native weather draw cache: "+asset);if(!a.Any(p=>p.A>0))throw new Exception("Empty weather source: "+asset);}
            }
            CheckDrawCache(Game1.rainTexture,"TileSheets/rain",new[]{new Rectangle(0,0,64,32)});
            CheckDrawCache(Game1.mouseCursors,"LooseSprites/Cursors",new[]{new Rectangle(368,192,256,16),new Rectangle(352,1184,176,48),new Rectangle(391,1236,20,4),new Rectangle(644,1078,37,57)});
            var directory=Path.Combine(helper.DirectoryPath,"weather-particles-previews");Directory.CreateDirectory(directory);
            using var target=new RenderTarget2D(device,384,256);using var testBatch=new SpriteBatch(device);
            Game1.spriteBatch=testBatch;Game1.viewport=new xTile.Dimensions.Rectangle(0,0,384,256);
            Game1.random=new Random(803);Game1.eventUp=false;Game1.game1.takingMapScreenshot=false;
            contexts["Default"]=weather;
            var fixture=new GameLocation();fixture.name.Value="WeatherArtAudit";fixture.IsOutdoors=true;Game1.game1.instanceGameLocation=fixture;
            if(!ReferenceEquals(fixture.GetWeather(),weather))throw new Exception("Unexpected weather context");
            Game1.debrisWeather=new List<WeatherDebris>();
            void Weather(bool rain=false,bool green=false,bool snowing=false,bool wind=false)
            {weather.IsGreenRain=green;weather.IsRaining=rain;weather.IsSnowing=snowing;weather.IsDebrisWeather=wind;}
            void Render(string name)
            {
                device.SetRenderTarget(target);device.Clear(new Color(31,47,41));
                Game1.game1.drawWeather(Game1.currentGameTime,target);
                device.SetRenderTargets(targets);
                var pixels=new Color[384*256];target.GetData(pixels);
                if(!pixels.Any(p=>p!=new Color(31,47,41)))throw new Exception("Empty native weather draw: "+name);
                using var file=File.Create(Path.Combine(directory,name+".png"));target.SaveAsPng(file,384,256);
            }
            foreach(bool green in new[]{false,true})for(int frame=0;frame<4;frame++)
            {
                Weather(true,green);Game1.rainDrops=Enumerable.Range(0,12).Select(i=>new RainDrop(24+i%6*60,40+i/6*100,frame,0)).ToArray();
                Render((green?"green-rain-":"rain-")+frame);rainCases++;
            }
            Weather(snowing:true);
            foreach(float transparency in new[]{0.35f,1f})
            {
                var original=Game1.options.snowTransparency;
                try{Game1.options.snowTransparency=transparency;for(int frame=0;frame<16;frame++){Game1.currentGameTime=new GameTime(TimeSpan.FromMilliseconds(frame*75),TimeSpan.FromMilliseconds(16));Game1.snowPos=new Vector2(0,0);Render("snow-"+transparency+"-"+frame);snowCases++;}}
                finally{Game1.options.snowTransparency=original;}
            }
            Weather(wind:true);
            for(int kind=0;kind<4;kind++)for(int frame=0;frame<(kind==3?5:11);frame++)
            {
                var particle=new WeatherDebris(new Vector2(150,100),kind,0,0,0);
                particle.sourceRect=kind==3?new Rectangle(391+frame*4,1236,4,4):new Rectangle(352+frame*16,1184+kind*16,16,16);
                particle.animationIndex=frame;Game1.debrisWeather=new List<WeatherDebris>{particle};Render("wind-"+kind+"-"+frame);windCases++;
            }
            // Native bolt creation establishes tiling, source rectangle, delay and fade without firing a farm strike.
            Weather();Utility.drawLightningBolt(new Vector2(192,240),fixture);
            if(fixture.temporarySprites.Count<2)throw new Exception("Missing stacked lightning segments");
            foreach(var bolt in fixture.temporarySprites)
            {
                if(bolt.sourceRect!=new Rectangle(644,1078,37,57)||bolt.delayBeforeAnimationStart!=200||bolt.alphaFade!=0.025f)throw new Exception("Native lightning contract changed");
                bolt.delayBeforeAnimationStart=0;
            }
            foreach(float alpha in new[]{1f,0.5f,0.1f})
            {
                device.SetRenderTarget(target);device.Clear(new Color(31,47,41));testBatch.Begin(samplerState:SamplerState.PointClamp);
                foreach(var bolt in fixture.temporarySprites){bolt.alpha=alpha;bolt.draw(testBatch);}
                testBatch.End();device.SetRenderTargets(targets);
                var pixels=new Color[384*256];target.GetData(pixels);if(!pixels.Any(p=>p!=new Color(31,47,41)))throw new Exception("Empty lightning draw");
                using var file=File.Create(Path.Combine(directory,"lightning-"+alpha+".png"));target.SaveAsPng(file,384,256);boltCases++;
            }
        }
        catch(Exception ex){error=ex.ToString();monitor.Log("Weather particles audit failed: "+ex,LogLevel.Error);}
        finally
        {
            if(hadWeather)contexts["Default"]=originalWeather!;else contexts.Remove("Default");
            Game1.game1.instanceGameLocation=location;Game1.viewport=viewport;Game1.random=random;Game1.currentGameTime=time;
            Game1.rainDrops=drops;Game1.debrisWeather=debris;Game1.snowPos=snow;Game1.spriteBatch=batch;Game1.eventUp=eventUp;Game1.game1.takingMapScreenshot=screenshot;
            device.SetRenderTargets(targets);device.Viewport=dv;device.BlendState=blend;device.DepthStencilState=depth;device.RasterizerState=raster;device.SamplerStates[0]=sampler;device.Textures[0]=texture;device.ScissorRectangle=scissor;
        }
        helper.Data.WriteJsonFile("weather-particles-checks.json",new{Passed=error==null,Error=error,RainDrawCases=rainCases,SnowDrawCases=snowCases,WindDrawCases=windCases,LightningFadeCases=boltCases,Scope="Native drawWeather renders with injected frames, native WeatherDebris draw and native lightning sprite construction/draw. No farm strikes, forecast transitions or save writes."});
    }
}


