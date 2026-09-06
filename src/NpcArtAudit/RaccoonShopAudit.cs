using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Menus;

namespace NpcArtAudit;
internal sealed class RaccoonShopAudit
{
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly IClickableMenu menu=Game1.activeClickableMenu;
    private readonly GameLocation location=Game1.currentLocation;
    private readonly bool dialogue=Game1.dialogueUp,move=Game1.player.CanMove;
    private readonly FieldInfo field=typeof(Game1).GetField("_activeClickableMenu",BindingFlags.Static|BindingFlags.NonPublic)!;
    private int ticks,stage;
    public bool Finished{get;private set;}
    public RaccoonShopAudit(IModHelper h,IMonitor m){helper=h;monitor=m;}
    public void Tick()
    {
        if(Finished||++ticks%3!=0)return;
        try
        {
            if(stage==0)
            {
                field.SetValue(null,null);Game1.dialogueUp=false;Game1.currentLocation=new Forest();
                new Raccoon(true).activate();stage=1;return;
            }
            var shop=Game1.activeClickableMenu as ShopMenu;
            if(shop?.ShopId!="Raccoon"||shop.portraitTexture==null)throw new Exception("Native Mrs. Raccoon shop or portrait missing.");
            var expected=helper.GameContent.Load<Texture2D>("Portraits/MrsRaccoon");
            var a=new Color[shop.portraitTexture.Width*shop.portraitTexture.Height];var b=new Color[expected.Width*expected.Height];
            shop.portraitTexture.GetData(a);expected.GetData(b);
            if(!a.SequenceEqual(b))throw new Exception("Wrong shop portrait pixels.");
            helper.Data.WriteJsonFile("raccoon-shop-checks.json",new{Passed=true,NativeActivate=true,PortraitPixelsMatch=true,TransactionsTested=false,WideScreenRendered=false,FarmLoaded=false});
            monitor.Log("Mrs. Raccoon native shop and portrait pixels verified.",LogLevel.Info);
        }
        catch(Exception ex){helper.Data.WriteJsonFile("raccoon-shop-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log($"Raccoon shop audit failed: {ex}",LogLevel.Error);}
        field.SetValue(null,menu);Game1.currentLocation=location;Game1.dialogueUp=dialogue;Game1.player.CanMove=move;Finished=true;
    }
}
