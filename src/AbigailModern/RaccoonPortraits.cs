using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;

namespace AbigailModern;

internal static class RaccoonPortraits
{
    private static IModHelper Helper = null!;
    private static IMonitor Monitor = null!;
    private static Dictionary<string,int>? Lines;
    private static LocalizedContentManager.LanguageCode Language;
    public static void Initialize(IModHelper helper, IMonitor monitor,string id)
    {
        Helper=helper;Monitor=monitor;
        var harmony=new Harmony(id);
        foreach(var types in new[]{new[]{typeof(string)},new[]{typeof(List<string>)}})
            harmony.Patch(AccessTools.Constructor(typeof(DialogueBox),types),postfix:new HarmonyMethod(typeof(RaccoonPortraits),nameof(AfterConstruct)));
        helper.Events.Display.MenuChanged+=(_,e)=>
        {
            if(e.NewMenu is ShopMenu crowShop && crowShop.ShopId=="LostItems")
                try{crowShop.portraitTexture=Helper.GameContent.Load<Texture2D>("Portraits/Crow");}
                catch(Exception ex){Monitor.Log($"Could not show the crow shop portrait: {ex.Message}",LogLevel.Warn);}
            if(e.NewMenu is ShopMenu shop && shop.ShopId=="Raccoon")
                try{shop.portraitTexture=Helper.GameContent.Load<Texture2D>("Portraits/MrsRaccoon");}
                catch(Exception ex){Monitor.Log($"Could not show Mrs. Raccoon's shop portrait: {ex.Message}",LogLevel.Warn);}
        };
    }
    private static void AfterConstruct(DialogueBox __instance)=>Refresh(__instance);
    private static string Normalize(string text)=>string.Concat(text.Where(c=>!char.IsWhiteSpace(c)));
    public static void Refresh(DialogueBox box)
    {
        if(Game1.currentLocation is not Forest || box.characterDialogue!=null || box.isQuestion)return;
        try
        {
            if(Lines==null||Language!=LocalizedContentManager.CurrentLanguageCode)
            {
                Language=LocalizedContentManager.CurrentLanguageCode;Lines=new();
                foreach(var key in Helper.GameContent.Load<Dictionary<string,string>>("Strings/1_6_Strings").Keys.Where(k=>k.StartsWith("Raccoon_",StringComparison.Ordinal)))
                {
                    var expression=key.Contains("receive")?1:key.Contains("busy")?2:key.Contains("intro")?3:0;
                    foreach(var line in Game1.content.LoadString("Strings\\1_6_Strings:"+key).Split('|'))Lines[Normalize(Game1.parseText(line))]=expression;
                }
            }
            if(Lines.TryGetValue(Normalize(box.getCurrentString()),out var emotion))PortraitPanel.Attach(box,Helper.GameContent.Load<Texture2D>("Portraits/MrRaccoon"),"Mr. Raccoon",emotion);
        }
        catch(Exception ex){Monitor.Log($"Could not show Mr. Raccoon's portrait: {ex.Message}",LogLevel.Warn);}
    }
}
