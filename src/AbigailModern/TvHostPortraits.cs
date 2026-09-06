using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
namespace AbigailModern;
internal static class TvHostPortraits
{
    private static IModHelper Helper=null!;
    private static IMonitor Monitor=null!;
    public static void Initialize(IModHelper helper,IMonitor monitor,string uniqueId)
    {
        Helper=helper;Monitor=monitor;var harmony=new Harmony(uniqueId);
        harmony.Patch(AccessTools.Method(typeof(TV),nameof(TV.selectChannel)),prefix:new HarmonyMethod(typeof(TvHostPortraits),nameof(Before)),postfix:new HarmonyMethod(typeof(TvHostPortraits),nameof(AfterOpening)));
        harmony.Patch(AccessTools.Method(typeof(TV),nameof(TV.proceedToNextScene)),prefix:new HarmonyMethod(typeof(TvHostPortraits),nameof(Before)),postfix:new HarmonyMethod(typeof(TvHostPortraits),nameof(AfterProgram)));
    }
    private static void Before(out IClickableMenu? __state)=>__state=Game1.activeClickableMenu;
    private static void AfterOpening(TV __instance,IClickableMenu? __state)=>Attach(__instance,__state,true);
    private static void AfterProgram(TV __instance,IClickableMenu? __state)=>Attach(__instance,__state,false);
    private static void Attach(TV tv,IClickableMenu? previous,bool opening)
    {
        var channel=(int)AccessTools.Field(typeof(TV),"currentChannel").GetValue(tv)!;
        if(channel is not (2 or 4 or 5) || ReferenceEquals(previous,Game1.activeClickableMenu) || Game1.activeClickableMenu is not DialogueBox box || box.characterDialogue!=null || box.isQuestion)return;
        var asset=channel switch {2=>"Portraits/WeatherPresenter",4=>"Portraits/LivinOffTheLand",_=>"Portraits/QueenOfSauce"};
        var name=channel switch {2=>"Weather Report",4=>"Livin' Off the Land",_=>"Queen of Sauce"};
        var expression=opening?0:channel==4?1:2;
        if(channel==2)
        {
            var screen=AccessTools.Field(typeof(TV),"screen").GetValue(tv) as TemporaryAnimatedSprite;
            expression=opening?1:screen?.id==776?3:0;
        }
        try{PortraitPanel.Attach(box,Helper.GameContent.Load<Texture2D>(asset),name,expression);}
        catch(Exception ex){Monitor.Log("Could not display TV host portrait: "+ex.Message,LogLevel.Warn);}
    }
}
