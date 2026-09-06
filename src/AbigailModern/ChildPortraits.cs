using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Menus;

namespace AbigailModern;
internal static class ChildPortraits
{
    private static IModHelper Helper=null!;
    public static void Initialize(IModHelper helper)
    {
        Helper=helper;
        helper.Events.Display.RenderedHud+=(_,e)=>
        {
            if(!Context.IsWorldReady||Game1.activeClickableMenu!=null||Game1.currentLocation==null)return;
            var child=Game1.currentLocation.characters.OfType<Child>()
                .Where(c=>!c.IsInvisible&&Vector2.DistanceSquared(c.Position,Game1.player.Position)<=256*256)
                .OrderBy(c=>Vector2.DistanceSquared(c.Position,Game1.player.Position)).FirstOrDefault();
            if(child!=null)Draw(e.SpriteBatch,child);
        };
    }
    public static string AppearanceFor(Child child)=>child.Age<3
        ?"Baby"+(child.darkSkinned.Value?"_dark":"")
        :"Toddler"+(child.Gender==Gender.Male?"":"_girl")+(child.darkSkinned.Value?"_dark":"");
    public static int ExpressionFor(Child child)
    {
        if(child.isSleeping.Value||(child.Age<3&&Game1.timeOfDay>=1800))return 5;
        if(child.drawOnTop)return 4;
        if(child.IsEmoting)return 1;
        if(child.Age==2&&child.Sprite?.CurrentFrame is >=40 and <=43)return 3;
        if(child.Age>=3&&child.Sprite?.CurrentFrame is >=16 and <=19)return 4;
        return 0;
    }
    public static void Draw(SpriteBatch batch,Child child)
    {
        var texture=Helper.GameContent.Load<Texture2D>("Portraits/"+AppearanceFor(child));
        var emotion=ExpressionFor(child);
        const int x=16,width=200,height=196;var y=Math.Max(16,Game1.uiViewport.Height-height-24);
        IClickableMenu.drawTextureBox(batch,x,y,width,height,Color.White);
        batch.Draw(texture,new Rectangle(x+36,y+16,128,128),new Rectangle(emotion%2*64,emotion/2*64,64,64),Color.White);
        var name=child.displayName??child.Name;var size=Game1.smallFont.MeasureString(name);var scale=Math.Min(1f,176f/Math.Max(1,size.X));
        batch.DrawString(Game1.smallFont,name,new Vector2(x+width/2f-size.X*scale/2,y+153),Game1.textColor,0,Vector2.Zero,scale,SpriteEffects.None,0);
    }
}
