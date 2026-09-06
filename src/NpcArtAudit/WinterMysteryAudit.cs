using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;

namespace NpcArtAudit;

internal static class WinterMysteryAudit
{
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        var menuField=typeof(Game1).GetField("_activeClickableMenu",BindingFlags.Static|BindingFlags.NonPublic)!;
        var previousMenu=Game1.activeClickableMenu;var previousLocation=Game1.currentLocation;
        var previousAfter=Game1.afterDialogues;var previousDialogue=Game1.dialogueUp;var previousMove=Game1.player.CanMove;
        var hadGlass=Game1.player.hasMagnifyingGlass;var hadQuest=Game1.player.hasQuest("31");
        try
        {
            var town=new Town();Game1.currentLocation=town;menuField.SetValue(null,null);
            typeof(Town).GetMethod("mgThief_speech",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(town,new object[]{0});
            var box=Game1.activeClickableMenu as DialogueBox??throw new Exception("Native mystery dialogue absent");
            static string Normalize(string text)=>string.Concat(text.Where(c=>!char.IsWhiteSpace(c)));
            if(Normalize(box.getCurrentString())!=Normalize(Game1.content.LoadString("Strings\\Locations:Town_mgThiefMessage")))throw new Exception("Native text changed");
            if(Game1.afterDialogues?.Target!=town || Game1.afterDialogues.Method.Name!="mgThief_afterSpeech")throw new Exception("Reward callback changed");
            var panel=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="AbigailModern").GetType("AbigailModern.PortraitPanel")!;
            var table=panel.GetField("Panels",BindingFlags.NonPublic|BindingFlags.Static)!.GetValue(null)!;
            var args=new object?[]{box,null};
            if(!(bool)table.GetType().GetMethod("TryGetValue")!.Invoke(table,args)!)throw new Exception("Missing portrait");
            var value=args[1]!;var type=value.GetType();
            if((string)type.GetProperty("Name")!.GetValue(value)! != "???" || (int)type.GetProperty("Emotion")!.GetValue(value)! !=2)throw new Exception("Hidden identity or expression changed");
            var texture=(Texture2D)type.GetProperty("Texture")!.GetValue(value)!;var expected=helper.GameContent.Load<Texture2D>("Portraits/Krobus");
            var a=new Color[texture.Width*texture.Height];var b=new Color[expected.Width*expected.Height];texture.GetData(a);expected.GetData(b);if(!a.SequenceEqual(b))throw new Exception("Wrong portrait pixels");
            var device=Game1.graphics.GraphicsDevice;var targets=device.GetRenderTargets();
            using var target=new RenderTarget2D(device,Game1.uiViewport.Width,Game1.uiViewport.Height);using var batch=new SpriteBatch(device);
            try
            {
                device.SetRenderTarget(target);device.Clear(new Color(35,45,58));batch.Begin(samplerState:SamplerState.PointClamp);
                box.transitioning=false;box.transitionInitialized=true;box.characterIndexInDialogue=box.getCurrentString().Length;box.draw(batch);panel.GetMethod("Render")!.Invoke(null,new object[]{batch,box});
                batch.End();device.SetRenderTargets(targets);using var stream=File.Create(Path.Combine(helper.DirectoryPath,"winter-mystery-preview.png"));target.SaveAsPng(stream,target.Width,target.Height);
            }
            finally{device.SetRenderTargets(targets);}
            if(Game1.player.hasMagnifyingGlass!=hadGlass || Game1.player.hasQuest("31")!=hadQuest)throw new Exception("Reward/quest state changed");
            helper.Data.WriteJsonFile("winter-mystery-checks.json",new{Passed=true,NativeTextPreserved=true,RewardCallbackPreserved=true,IdentityHidden=true,Expression=2,PortraitPixelsMatch=true,RewardSubmitted=false,FarmLoaded=false,FullSceneVerified=false});
            monitor.Log("Winter mystery checks passed: native dialogue, hidden identity, portrait and reward callback preserved.",LogLevel.Info);
        }
        catch(Exception ex){helper.Data.WriteJsonFile("winter-mystery-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log($"Winter mystery audit failed: {ex}",LogLevel.Error);}
        finally{menuField.SetValue(null,previousMenu);Game1.currentLocation=previousLocation;Game1.afterDialogues=previousAfter;Game1.dialogueUp=previousDialogue;Game1.player.CanMove=previousMove;}
    }
}
