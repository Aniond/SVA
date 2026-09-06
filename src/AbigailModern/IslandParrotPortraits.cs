using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace AbigailModern;
internal static class IslandParrotPortraits
{
    private static IModHelper Helper=null!;
    private static IMonitor Monitor=null!;
    public static void Initialize(IModHelper helper,IMonitor monitor,string id)
    {
        Helper=helper;Monitor=monitor;
        new Harmony(id).Patch(AccessTools.Method(typeof(ParrotUpgradePerch),nameof(ParrotUpgradePerch.CheckAction)),
            prefix:new HarmonyMethod(typeof(IslandParrotPortraits),nameof(Before)),postfix:new HarmonyMethod(typeof(IslandParrotPortraits),nameof(After)));
        new Harmony(id).Patch(AccessTools.Method(typeof(ParrotUpgradePerch),nameof(ParrotUpgradePerch.AnswerQuestion)),
            prefix:new HarmonyMethod(typeof(IslandParrotPortraits),nameof(Before)),postfix:new HarmonyMethod(typeof(IslandParrotPortraits),nameof(AfterAnswer)));
    }
    private static void Before(out IClickableMenu? __state)=>__state=Game1.activeClickableMenu;
    private static void AfterAnswer(ParrotUpgradePerch __instance,IClickableMenu? __state)
    {
        // The native insufficient-money branch opens dialogue but returns false.
        if(__instance.upgradeName.Value=="GoldenParrot") Attach(__instance,__state);
    }
    private static void After(ParrotUpgradePerch __instance,IClickableMenu? __state,bool __result)
    {
        if(__result) Attach(__instance,__state);
    }
    private static void Attach(ParrotUpgradePerch perch,IClickableMenu? previous)
    {
        if(ReferenceEquals(previous,Game1.activeClickableMenu)
            ||Game1.activeClickableMenu is not DialogueBox box||box.characterDialogue!=null)return;
        var golden=perch.upgradeName.Value=="GoldenParrot";
        var emotion=box.isQuestion?3:2;
        if(golden&&!box.isQuestion&&string.Concat(box.getCurrentString().Where(c=>!char.IsWhiteSpace(c)))==string.Concat(Game1.content.LoadString("Strings\\1_6_Strings:GoldenParrot_Tonight").Where(c=>!char.IsWhiteSpace(c)))) emotion=5;
        try{PortraitPanel.Attach(box,Helper.GameContent.Load<Texture2D>(golden?"Portraits/GoldenParrot":"Portraits/UpgradeParrot"),golden?"Golden Parrot":"Parrot",emotion);}
        catch(Exception ex){Monitor.Log($"Could not display island parrot portrait: {ex.Message}",LogLevel.Warn);}
    }
}
