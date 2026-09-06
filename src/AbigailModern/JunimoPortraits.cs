using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Menus;

namespace AbigailModern;

internal static class JunimoPortraits
{
    private static IModHelper Helper=null!;
    private static readonly System.Reflection.FieldInfo TintField=AccessTools.Field(typeof(Junimo),"color");
    private static readonly System.Reflection.FieldInfo FarewellField=AccessTools.Field(typeof(Junimo),"sayingGoodbye");
    private static readonly System.Reflection.FieldInfo SpeechField=AccessTools.Field(typeof(NPC),"textAboveHeadTimer");
    private static readonly System.Reflection.FieldInfo HarvesterTint=AccessTools.Field(typeof(JunimoHarvester),"color");
    private static readonly System.Reflection.FieldInfo HarvestTimer=AccessTools.Field(typeof(JunimoHarvester),"harvestTimer");
    private static readonly System.Reflection.FieldInfo HarvesterAlpha=AccessTools.Field(typeof(JunimoHarvester),"alpha");
    private static readonly System.Reflection.FieldInfo JunimoAlpha=AccessTools.Field(typeof(Junimo),"alpha");
    public static void Initialize(IModHelper helper)
    {
        Helper=helper;
        helper.Events.Display.RenderedHud+=(_,e)=>
        {
            if(!Context.IsWorldReady||Game1.activeClickableMenu!=null||Game1.currentLocation==null)return;
            var junimo=Game1.currentLocation.characters
                .Where(j=>(j is Junimo or JunimoHarvester)&&!j.IsInvisible&&OpacityFor(j)>0.1f&&Vector2.DistanceSquared(j.Position,Game1.player.Position)<=320*320)
                .OrderBy(j=>Vector2.DistanceSquared(j.Position,Game1.player.Position)).FirstOrDefault();
            if(junimo!=null)Draw(e.SpriteBatch,ExpressionFor(junimo),TintFor(junimo)*OpacityFor(junimo));
        };
    }
    public static Color TintFor(NPC actor)=>((NetColor)(actor is Junimo?TintField:HarvesterTint).GetValue(actor)!).Value;
    public static float OpacityFor(NPC actor)=>actor is Junimo?((NetFloat)JunimoAlpha.GetValue(actor)!).Value:(float)HarvesterAlpha.GetValue(actor)!;
    public static int ExpressionFor(NPC actor)
    {
        if(actor is JunimoHarvester harvester)return (int)HarvestTimer.GetValue(harvester)!>0?3:harvester.Sprite?.CurrentFrame is >=44 and <=47?4:0;
        var junimo=(Junimo)actor;
        if(((NetBool)FarewellField.GetValue(junimo)!).Value)return 5;
        if(junimo.holdingStar.Value)return 4;
        if(junimo.holdingBundle.Value)return 3;
        if((int)SpeechField.GetValue(junimo)!>0)return 1;
        return junimo.friendly.Value?0:2;
    }
    public static void Draw(SpriteBatch batch,int emotion,Color tint)
    {
        var texture=Helper.GameContent.Load<Texture2D>("Portraits/Junimo");
        const int x=16,width=160,height=160;
        var y=Math.Max(16,Game1.uiViewport.Height-height-24);
        IClickableMenu.drawTextureBox(batch,x,y,width,height,Color.White);
        batch.Draw(texture,new Rectangle(x+16,y+16,128,128),new Rectangle(emotion%2*64,emotion/2*64,64,64),tint);
    }
}
