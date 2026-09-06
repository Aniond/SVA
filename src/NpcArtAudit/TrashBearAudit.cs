using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;

namespace NpcArtAudit;
internal static class TrashBearAudit
{
    public static void Run(IModHelper helper,IMonitor monitor)
    {
        var oldLocation=Game1.currentLocation;var oldMove=Game1.player.CanMove;
        var panel=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="AbigailModern").GetType("AbigailModern.TrashBearPortrait")!;
        int? Expression()=>(int?)panel.GetMethod("ActiveExpression")!.Invoke(null,null);
        try
        {
            if(Game1.player.ActiveObject!=null)throw new Exception("Audit requires an empty hand; no inventory changed.");
            var forest=new Forest();Game1.currentLocation=forest;
            var bear=new TrashBear();bear.currentLocation=forest;
            var result=bear.checkAction(Game1.player,forest);
            if(result||Expression()!=3||string.IsNullOrEmpty(bear.itemWantedIndex))throw new Exception("Native food request or eager portrait missing.");
            var device=Game1.graphics.GraphicsDevice;var targets=device.GetRenderTargets();
            using var target=new RenderTarget2D(device,Game1.uiViewport.Width,Game1.uiViewport.Height);
            using var batch=new SpriteBatch(device);
            try
            {
                device.SetRenderTarget(target);device.Clear(new Color(35,45,58));
                batch.Begin(samplerState:SamplerState.PointClamp);panel.GetMethod("Draw")!.Invoke(null,new object[]{batch});batch.End();
                device.SetRenderTargets(targets);
                using var stream=File.Create(Path.Combine(helper.DirectoryPath,"trash-bear-portrait-preview.png"));target.SaveAsPng(stream,target.Width,target.Height);
            }
            finally{device.SetRenderTargets(targets);}
            bear.doEatEvent(bear.itemWantedIndex);
            if(Expression()!=1)throw new Exception("Eating portrait missing.");
            bear.Sprite.CurrentFrame=8;
            if(Expression()!=5)throw new Exception("Satisfied portrait missing.");
            Game1.currentLocation=oldLocation;
            if(Expression()!=null)throw new Exception("Portrait remained after leaving.");
            helper.Data.WriteJsonFile("trash-bear-interaction-checks.json",new{Passed=true,NativeRequest=true,ItemBubblePreserved=true,EatingExpression=true,SatisfiedExpression=true,HidesAfterLeaving=true,FoodSubmissionTested=false,CleanupEventTested=false,FarmLoaded=false});
            monitor.Log("Trash Bear request, eating and satisfied portrait checks passed without submitting food.",LogLevel.Info);
        }
        catch(Exception ex){helper.Data.WriteJsonFile("trash-bear-interaction-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log($"Trash Bear audit failed: {ex}",LogLevel.Error);}
        finally{Game1.currentLocation=oldLocation;Game1.player.CanMove=oldMove;panel.GetField("Actor",BindingFlags.Static|BindingFlags.NonPublic)!.SetValue(null,null);panel.GetField("Location",BindingFlags.Static|BindingFlags.NonPublic)!.SetValue(null,null);}
    }
}
