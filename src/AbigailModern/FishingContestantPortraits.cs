using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;

namespace AbigailModern;

internal static class FishingContestantPortraits
{
    private static IModHelper Helper=null!;
    private static readonly System.Reflection.FieldInfo Text=AccessTools.Field(typeof(NPC),"textAboveHead");
    private static readonly System.Reflection.FieldInfo Timer=AccessTools.Field(typeof(NPC),"textAboveHeadTimer");
    private static readonly System.Reflection.FieldInfo PreTimer=AccessTools.Field(typeof(NPC),"textAboveHeadPreTimer");
    private static readonly int[] Reactions={4,1,2,1,3,1,4};

    public static void Initialize(IModHelper helper)
    {
        Helper=helper;
        helper.Events.Display.RenderedHud+=(_,e)=>
        {
            if(!Context.IsWorldReady||Game1.activeClickableMenu!=null||Game1.currentLocation is not (Beach or Forest))return;
            var actor=Game1.currentLocation.characters
                .Where(c=>(Game1.currentLocation is Beach?WinterIndex(c)>=0:SummerIndex(c)>=0)&&!c.IsInvisible&&Vector2.DistanceSquared(c.Position,Game1.player.Position)<=320*320)
                .OrderByDescending(IsSpeaking)
                .ThenBy(c=>Vector2.DistanceSquared(c.Position,Game1.player.Position)).FirstOrDefault();
            if(actor!=null)Draw(e.SpriteBatch,actor);
        };
    }
    public static int WinterIndex(NPC actor)
    {
        const string prefix="winter_derby_contestent";
        var name=actor.Name;
        return name!=null&&name.StartsWith(prefix,StringComparison.Ordinal)
            &&int.TryParse(name.AsSpan(prefix.Length),out var index)&&index is >=0 and <12?index:-1;
    }
    public static bool IsSpeaking(NPC actor)=>(int)Timer.GetValue(actor)!>0&&(int)PreTimer.GetValue(actor)!<=0;
    public static int SummerIndex(NPC actor)
    {
        const string prefix="derby_contestent";
        var name=actor.Name;
        return name!=null&&name.StartsWith(prefix,StringComparison.Ordinal)
            &&int.TryParse(name.AsSpan(prefix.Length),out var index)&&index is >=0 and <10?index:-1;
    }
    public static string? PortraitFor(NPC actor)
    {
        var winter=WinterIndex(actor);if(winter>=0)return "Portraits/FishingContestantWinter"+winter;
        var summer=SummerIndex(actor);return summer>=0?"Portraits/FishingContestantSummer"+summer:null;
    }
    public static int ExpressionFor(NPC actor)
    {
        if(!IsSpeaking(actor))return 0;
        var text=Text.GetValue(actor) as string;
        for(var i=0;i<Reactions.Length;i++)
            if(text==Dialogue.applyGenderSwitchBlocks(Game1.player.Gender,Game1.content.LoadString("Strings\\1_6_Strings:FishingDerby_Exclamation"+i)))return Reactions[i];
        return 0;
    }
    public static void Draw(SpriteBatch batch,NPC actor)
    {
        var path=PortraitFor(actor);if(path==null)return;
        var texture=Helper.GameContent.Load<Texture2D>(path);
        var emotion=ExpressionFor(actor);
        const int x=16,width=160,height=160;var y=Math.Max(16,Game1.uiViewport.Height-height-24);
        IClickableMenu.drawTextureBox(batch,x,y,width,height,Color.White);
        batch.Draw(texture,new Rectangle(x+16,y+16,128,128),new Rectangle(emotion%2*64,emotion/2*64,64,64),Color.White);
    }
}
