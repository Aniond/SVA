using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace NpcArtAudit;
internal static class IslandParrotAudit
{
    public static void Run(IModHelper helper,IMonitor monitor)
    {
        var field=typeof(Game1).GetField("_activeClickableMenu",BindingFlags.Static|BindingFlags.NonPublic)!;
        var oldMenu=Game1.activeClickableMenu;var oldLocation=Game1.currentLocation;var oldDialogue=Game1.dialogueUp;var oldMove=Game1.player.CanMove;var oldNuts=Game1.netWorldState.Value.GoldenWalnuts;
        var oldFound=Game1.netWorldState.Value.GoldenWalnutsFound;var oldMoney=Game1.player.Money;
        try
        {
            var location=new GameLocation();Game1.currentLocation=location;
            var applied=false;var perch=new ParrotUpgradePerch(location,new Point(4,4),new Rectangle(0,0,1,1),10,()=>applied=true,()=>false,"Trader");
            var panel=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="AbigailModern").GetType("AbigailModern.PortraitPanel")!;
            var table=panel.GetField("Panels",BindingFlags.NonPublic|BindingFlags.Static)!.GetValue(null)!;
            foreach(var nuts in new[]{0,10})
            {
                field.SetValue(null,null);Game1.netWorldState.Value.GoldenWalnuts=nuts;
                if(!perch.CheckAction(new xTile.Dimensions.Location(4,4),Game1.player))throw new Exception("Native perch action failed");
                var box=Game1.activeClickableMenu as DialogueBox??throw new Exception("Native dialogue absent");
                static string Normalize(string value)=>string.Concat(value.Where(c=>!char.IsWhiteSpace(c)));
                var expectedText=string.Format(Game1.content.LoadString("Strings\\UI:UpgradePerch_Trader"),10);
                if(Normalize(box.getCurrentString())!=Normalize(expectedText)||box.isQuestion!=(nuts==10))throw new Exception("Native text or question changed");
                if(nuts==10&&(!box.responses.Select(r=>r.responseKey).SequenceEqual(new[]{"Yes","No"})||location.lastQuestionKey!="UpgradePerch_Trader"))throw new Exception("Response routing changed");
                var args=new object?[]{box,null};if(!(bool)table.GetType().GetMethod("TryGetValue")!.Invoke(table,args)!)throw new Exception("No parrot portrait");
                var value=args[1]!;if((int)value.GetType().GetProperty("Emotion")!.GetValue(value)! !=(nuts==10?3:2))throw new Exception("Wrong emotion");
                CheckTexture(value,"Portraits/UpgradeParrot");
                if(Game1.netWorldState.Value.GoldenWalnuts!=nuts||applied)throw new Exception("Walnuts or construction changed");
                var device=Game1.graphics.GraphicsDevice;var targets=device.GetRenderTargets();using var target=new RenderTarget2D(device,Game1.uiViewport.Width,Game1.uiViewport.Height);using var batch=new SpriteBatch(device);
                try{device.SetRenderTarget(target);device.Clear(new Color(35,45,58));batch.Begin(samplerState:SamplerState.PointClamp);box.transitioning=false;box.transitionInitialized=true;box.characterIndexInDialogue=box.getCurrentString().Length;box.draw(batch);panel.GetMethod("Render")!.Invoke(null,new object[]{batch,box});batch.End();device.SetRenderTargets(targets);using var stream=File.Create(Path.Combine(helper.DirectoryPath,$"parrot-dialogue-{nuts}.png"));target.SaveAsPng(stream,target.Width,target.Height);}finally{device.SetRenderTargets(targets);}
            }
            Game1.netWorldState.Value.GoldenWalnutsFound=129;
            var golden=new ParrotUpgradePerch(location,new Point(5,5),new Rectangle(0,0,1,1),0,()=>applied=true,()=>false,"GoldenParrot");
            foreach(var money in new[]{0,10000})
            {
                field.SetValue(null,null);Game1.player.Money=money;
                if(!golden.CheckAction(new xTile.Dimensions.Location(5,5),Game1.player))throw new Exception("Golden parrot action failed");
                var initial=Game1.activeClickableMenu as DialogueBox??throw new Exception("Golden question absent");
                CheckGolden(initial,3,"Strings\\1_6_Strings:GoldenParrot",true);
                if(location.lastQuestionKey!="GoldenParrot")throw new Exception("Golden question route changed");
                golden.AnswerQuestion(new Response("Yes","Yes"));
                var reply=Game1.activeClickableMenu as DialogueBox??throw new Exception("Golden reply absent");
                CheckGolden(reply,money==0?2:3,money==0?"Strings\\UI:NotEnoughMoney3":"Strings\\1_6_Strings:GoldenParrot_Sure",money!=0);
                if(money!=0&&location.lastQuestionKey!="GoldenParrotConfirm")throw new Exception("Confirmation route changed");
                if(Game1.player.Money!=money||applied)throw new Exception("Golden check spent money or started construction");
            }
            void CheckGolden(DialogueBox box,int emotion,string textKey,bool question)
            {
                static string Normalize(string text)=>string.Concat(text.Where(c=>!char.IsWhiteSpace(c)));
                if(Normalize(box.getCurrentString())!=Normalize(Game1.content.LoadString(textKey))||box.isQuestion!=question)throw new Exception("Golden native dialogue changed");
                if(question&&!box.responses.Select(r=>r.responseKey).SequenceEqual(new[]{"Yes","No"}))throw new Exception("Golden responses changed");
                var args=new object?[]{box,null};if(!(bool)table.GetType().GetMethod("TryGetValue")!.Invoke(table,args)!)throw new Exception("Golden portrait missing");
                var value=args[1]!;if((int)value.GetType().GetProperty("Emotion")!.GetValue(value)! !=emotion)throw new Exception("Golden emotion mismatch");
                CheckTexture(value,"Portraits/GoldenParrot");
            }
            void CheckTexture(object value,string key)
            {
                var actual=(Texture2D)value.GetType().GetProperty("Texture")!.GetValue(value)!;
                var expected=helper.GameContent.Load<Texture2D>(key);
                var a=new Color[actual.Width*actual.Height];var b=new Color[expected.Width*expected.Height];actual.GetData(a);expected.GetData(b);
                if(!a.SequenceEqual(b))throw new Exception("Portrait texture mismatch: "+key);
            }
            helper.Data.WriteJsonFile("upgrade-parrot-checks.json",new{Passed=true,GreenPortraitPixels=true,GoldenPortraitPixels=true,GoldenInitialAndConfirmationQuestions=true,GoldenInsufficientMoney=true,MoneyUnchanged=true,PurchaseExecuted=false,FarmLoaded=false});
            helper.Data.WriteJsonFile("island-parrot-checks.json",new{Passed=true,NativePerchAction=true,TextAndResponseKeysPreserved=true,WalnutsUnchanged=true,ConstructionStarted=false,Expressions=new[]{2,3},FarmLoaded=false});
            monitor.Log("Island parrot native question and insufficient-walnut portrait checks passed.",LogLevel.Info);
        }
        catch(Exception ex){helper.Data.WriteJsonFile("island-parrot-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log($"Island parrot audit failed: {ex}",LogLevel.Error);}
        finally{field.SetValue(null,oldMenu);Game1.currentLocation=oldLocation;Game1.dialogueUp=oldDialogue;Game1.player.CanMove=oldMove;Game1.netWorldState.Value.GoldenWalnuts=oldNuts;Game1.netWorldState.Value.GoldenWalnutsFound=oldFound;Game1.player.Money=oldMoney;}
    }
}
