using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace NpcArtAudit;

internal static class WelwickAudit
{
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        var menuField=typeof(Game1).GetField("_activeClickableMenu",BindingFlags.Static|BindingFlags.NonPublic)!;
        var previousMenu=Game1.activeClickableMenu;var previousLocation=Game1.currentLocation;
        var previousLuck=Game1.player.team.sharedDailyLuck.Value;var previousRandom=Game1.random;
        var previousAfter=Game1.afterDialogues;var previousDialogue=Game1.dialogueUp;var previousMove=Game1.player.CanMove;
        var lights=Game1.currentLightSources.Keys.ToHashSet();
        try
        {
            Game1.currentLocation=new GameLocation("Maps/FarmHouse","FarmHouse");
            Game1.random=new Random(12345);
            var assembly=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="AbigailModern");
            var panel=assembly.GetType("AbigailModern.PortraitPanel")!;
            var panelTable=panel.GetField("Panels",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
            object GetPanel(DialogueBox box) {var args=new object?[]{box,null};if(!(bool)panelTable.GetType().GetMethod("TryGetValue")!.Invoke(panelTable,args)!)throw new Exception("Missing Welwick panel");return args[1]!;}
            int Emotion(DialogueBox box) {var p=GetPanel(box);return (int)p.GetType().GetProperty("Emotion")!.GetValue(p)!;}
            void Capture(TV tv,DialogueBox box,string name)
            {
                var screen=(TemporaryAnimatedSprite)typeof(TV).GetField("screen",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(tv)!;
                var overlay=(TemporaryAnimatedSprite?)typeof(TV).GetField("screenOverlay",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(tv);
                var device=Game1.graphics.GraphicsDevice;var targets=device.GetRenderTargets();
                using var target=new RenderTarget2D(device,Game1.uiViewport.Width,Game1.uiViewport.Height);using var batch=new SpriteBatch(device);
                try
                {
                    device.SetRenderTarget(target);device.Clear(new Color(35,45,58));batch.Begin(samplerState:SamplerState.PointClamp);
                    batch.Draw(screen.texture,new Rectangle(80,60,252,168),screen.sourceRect,Color.White);
                    if(overlay!=null)batch.Draw(overlay.texture,new Rectangle(170,66,78,78),overlay.sourceRect,Color.White);
                    box.transitioning=false;box.transitionInitialized=true;box.characterIndexInDialogue=box.getCurrentString().Length;
                    box.draw(batch);panel.GetMethod("Render")!.Invoke(null,new object[]{batch,box});batch.End();device.SetRenderTargets(targets);
                    using var stream=File.Create(Path.Combine(helper.DirectoryPath,name));target.SaveAsPng(stream,target.Width,target.Height);
                }
                finally{device.SetRenderTargets(targets);}
            }
            var results=new List<object>();
            foreach(var (luck,expected) in new[]{(-.12,5),(-.08,5),(-.04,2),(0d,0),(.01,0),(.04,1),(.08,4),(.12,4)})
            {
                menuField.SetValue(null,null);Game1.dialogueUp=false;Game1.player.team.sharedDailyLuck.Value=luck;
                var tv=(TV)ItemRegistry.Create("(F)1468");
                tv.selectChannel(Game1.player,"Fortune");
                var opening=Game1.activeClickableMenu as DialogueBox??throw new Exception("Native opening absent");
                if(Emotion(opening)!=3 || Game1.afterDialogues?.Target!=tv)throw new Exception("Opening expression or callback changed");
                var p=GetPanel(opening);var actual=(Texture2D)p.GetType().GetProperty("Texture")!.GetValue(p)!;
                var expectedTexture=helper.GameContent.Load<Texture2D>("Portraits/Welwick");
                var a=new Color[actual.Width*actual.Height];var b=new Color[expectedTexture.Width*expectedTexture.Height];actual.GetData(a);expectedTexture.GetData(b);
                if(!a.SequenceEqual(b))throw new Exception("Wrong portrait pixels");
                if(luck==0)Capture(tv,opening,"welwick-opening-preview.png");
                Game1.afterDialogues!();
                var forecast=Game1.activeClickableMenu as DialogueBox??throw new Exception("Native forecast absent");
                if(Emotion(forecast)!=expected || Game1.afterDialogues?.Target!=tv)throw new Exception($"Wrong forecast expression at {luck}: {Emotion(forecast)}");
                if(luck is -.12 or 0 or .12)Capture(tv,forecast,$"welwick-forecast-{expected}-preview.png");
                Game1.afterDialogues!();
                if(typeof(TV).GetField("screen",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(tv)!=null)throw new Exception("TV did not turn off");
                results.Add(new{Luck=luck,Expression=expected,NativeOpening=true,NativeForecast=true,TurnedOff=true,PortraitPixelsMatch=true});
            }
            menuField.SetValue(null,null);var weather=(TV)ItemRegistry.Create("(F)1468");weather.selectChannel(Game1.player,"Weather");
            var weatherBox=Game1.activeClickableMenu as DialogueBox??throw new Exception("Weather absent");
            var weatherPanel=GetPanel(weatherBox);
            if((string)weatherPanel.GetType().GetProperty("Name")!.GetValue(weatherPanel)! != "Weather Report")throw new Exception("Wrong presenter on weather channel");weather.turnOffTV();
            helper.Data.WriteJsonFile("welwick-interaction-checks.json",new{Passed=true,Cases=results,OtherChannelExcluded=true,FarmLoaded=false,FullFurnitureSceneVerified=false});
            monitor.Log("Welwick checks passed: eight native fortune flows, portraits, callbacks, turn-off and weather exclusion.",LogLevel.Info);
        }
        catch(Exception ex){helper.Data.WriteJsonFile("welwick-interaction-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log($"Welwick audit failed: {ex}",LogLevel.Error);}
        finally
        {
            foreach(var key in Game1.currentLightSources.Keys.Where(k=>!lights.Contains(k)).ToArray())Game1.currentLightSources.Remove(key);
            menuField.SetValue(null,previousMenu);Game1.currentLocation=previousLocation;Game1.player.team.sharedDailyLuck.Value=previousLuck;Game1.random=previousRandom;
            Game1.afterDialogues=previousAfter;Game1.dialogueUp=previousDialogue;Game1.player.CanMove=previousMove;
        }
    }
}
