using System.Reflection;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Menus;

namespace NpcArtAudit;
internal static class RaccoonAudit
{
    public static void Run(IModHelper helper,IMonitor monitor)
    {
        var field=typeof(Game1).GetField("_activeClickableMenu",BindingFlags.Static|BindingFlags.NonPublic)!;
        var menu=Game1.activeClickableMenu;var location=Game1.currentLocation;var dialogue=Game1.dialogueUp;var move=Game1.player.CanMove;var after=Game1.afterDialogues;
        try
        {
            var panel=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="AbigailModern").GetType("AbigailModern.PortraitPanel")!;
            bool Has(DialogueBox box)=>(bool)panel.GetMethod("HasPanel")!.Invoke(null,new object[]{box})!;
            Game1.currentLocation=new Forest();
            var count=0;
            foreach(var key in helper.GameContent.Load<Dictionary<string,string>>("Strings/1_6_Strings").Keys.Where(k=>k.StartsWith("Raccoon_")))
            foreach(var line in Game1.content.LoadString("Strings\\1_6_Strings:"+key).Split('|'))
            {
                if(!Has(new DialogueBox(Game1.parseText(line))))throw new Exception("Missing raccoon portrait: "+key);
                count++;
            }
            if(Has(new DialogueBox("Unrelated forest dialogue.")))throw new Exception("Decorated unrelated text.");
            field.SetValue(null,null);Game1.dialogueUp=false;
            var actor=new Raccoon(false);actor.activate();
            if(Game1.activeClickableMenu is not DialogueBox request||!Has(request))throw new Exception("Native raccoon greeting missing portrait.");
            helper.Data.WriteJsonFile("raccoon-interaction-checks.json",new{Passed=true,LocalizedLinesChecked=count,NativeActivate=true,ShopTested=false,BundleSubmissionTested=false,FarmLoaded=false});
            monitor.Log($"Raccoon checks passed: {count} current-language lines and native greeting.",LogLevel.Info);
        }
        catch(Exception ex){helper.Data.WriteJsonFile("raccoon-interaction-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log($"Raccoon audit failed: {ex}",LogLevel.Error);}
        finally{field.SetValue(null,menu);Game1.currentLocation=location;Game1.dialogueUp=dialogue;Game1.player.CanMove=move;Game1.afterDialogues=after;}
    }
}
