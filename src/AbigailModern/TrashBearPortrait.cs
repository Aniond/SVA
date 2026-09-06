using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Menus;

namespace AbigailModern;

internal static class TrashBearPortrait
{
    private static IModHelper Helper=null!;
    private static TrashBear? Actor;
    private static GameLocation? Location;
    private static double Expires;
    private static bool Eating;
    public static void Initialize(IModHelper helper,string id)
    {
        Helper=helper;
        var harmony=new Harmony(id);
        harmony.Patch(AccessTools.Method(typeof(TrashBear),nameof(TrashBear.checkAction)),postfix:new HarmonyMethod(typeof(TrashBearPortrait),nameof(AfterRequest)));
        harmony.Patch(AccessTools.Method(typeof(TrashBear),nameof(TrashBear.doEatEvent)),postfix:new HarmonyMethod(typeof(TrashBearPortrait),nameof(AfterEat)));
        helper.Events.Display.RenderedHud+=(_,e)=>{if(Context.IsWorldReady&&Game1.activeClickableMenu==null)Draw(e.SpriteBatch);};
        helper.Events.GameLoop.ReturnedToTitle+=(_,_)=>{Actor=null;Location=null;};
    }
    private static void AfterRequest(TrashBear __instance,GameLocation l)
    {
        if(AccessTools.Field(typeof(TrashBear),"showWantBubbleTimer").GetValue(__instance) is int timer&&timer>0)Start(__instance,l,false,3000);
    }
    private static void AfterEat(TrashBear __instance)
    {
        if(Game1.currentLocation is Forest)Start(__instance,Game1.currentLocation,true,8000);
    }
    private static void Start(TrashBear actor,GameLocation location,bool eating,int duration)
    {
        Actor=actor;Location=location;Eating=eating;Expires=(Game1.currentGameTime?.TotalGameTime.TotalMilliseconds??0)+duration;
    }
    public static int? ActiveExpression()
    {
        if(Actor==null||!ReferenceEquals(Location,Game1.currentLocation)||(Game1.currentGameTime?.TotalGameTime.TotalMilliseconds??0)>=Expires)return null;
        return !Eating?3:Actor.Sprite.CurrentFrame==8?5:1;
    }
    public static void Draw(SpriteBatch batch)
    {
        var emotion=ActiveExpression();if(emotion==null)return;
        var texture=Helper.GameContent.Load<Texture2D>("Portraits/TrashBear");
        const int x=16,size=128,width=160,height=186;
        var y=Math.Max(16,Game1.uiViewport.Height-height-24);
        IClickableMenu.drawTextureBox(batch,x,y,width,height,Color.White);
        batch.Draw(texture,new Rectangle(x+16,y+16,size,size),new Rectangle(emotion.Value%2*64,emotion.Value/2*64,64,64),Color.White);
        var label="Trash Bear";var textSize=Game1.smallFont.MeasureString(label);
        batch.DrawString(Game1.smallFont,label,new Vector2(x+(width-textSize.X)/2,y+size+24),Game1.textColor);
    }
}
