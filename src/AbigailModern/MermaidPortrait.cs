using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;

namespace AbigailModern;

internal static class MermaidPortrait
{
    private static IModHelper Helper=null!;
    public static void Initialize(IModHelper helper)
    {
        Helper=helper;
        helper.Events.Display.RenderedHud+=(_,e)=>
        {
            if(!Context.IsWorldReady||Game1.activeClickableMenu!=null||Game1.currentLocation is not IslandSouthEast island
                ||!island.MermaidIsHere()||Game1.player.TilePoint.X<25||Game1.player.TilePoint.Y<26)return;
            Draw(e.SpriteBatch,ExpressionFor(island));
        };
    }
    public static int ExpressionFor(IslandSouthEast island)
    {
        if(ReferenceEquals(island.currentMermaidAnimation,island.mermaidReward))return 4;
        if(ReferenceEquals(island.currentMermaidAnimation,island.mermaidDance))return 5;
        if(ReferenceEquals(island.currentMermaidAnimation,island.mermaidWave))return 1;
        return 0;
    }
    public static void Draw(SpriteBatch batch,int emotion)
    {
        var texture=Helper.GameContent.Load<Texture2D>("Portraits/Mermaid");
        const int x=16,width=160,height=160;var y=Math.Max(16,Game1.uiViewport.Height-height-24);
        IClickableMenu.drawTextureBox(batch,x,y,width,height,Color.White);
        batch.Draw(texture,new Rectangle(x+16,y+16,128,128),new Rectangle(emotion%2*64,emotion/2*64,64,64),Color.White);
    }
}
