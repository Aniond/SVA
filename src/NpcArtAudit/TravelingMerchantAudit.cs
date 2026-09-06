using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;

namespace NpcArtAudit;
internal sealed class TravelingMerchantAudit
{
    private readonly IModHelper helper;private readonly IMonitor monitor;
    private readonly IClickableMenu oldMenu=Game1.activeClickableMenu;
    private readonly GameLocation oldLocation=Game1.currentLocation;
    private readonly xTile.Dimensions.Rectangle oldViewport=Game1.uiViewport;
    private readonly bool oldDialogue=Game1.dialogueUp,oldMove=Game1.player.CanMove,oldMerchantPortraits=Game1.options.showMerchantPortraits;
    private readonly bool hadAchievement=Game1.player.achievements.Contains(0);
    private readonly int oldMoney=Game1.player.Money;
    private readonly FieldInfo menuField=typeof(Game1).GetField("_activeClickableMenu",BindingFlags.Static|BindingFlags.NonPublic)!;
    private int stage,ticks,stock;private string? greeting;public bool Finished{get;private set;}
    public TravelingMerchantAudit(IModHelper helper,IMonitor monitor){this.helper=helper;this.monitor=monitor;}
    public void Tick()
    {
        if(Finished||++ticks%3!=0)return;
        try
        {
            if(stage==0)
            {
                menuField.SetValue(null,null);Game1.dialogueUp=false;
                Game1.uiViewport=new xTile.Dimensions.Rectangle(0,0,1920,1080);Game1.options.showMerchantPortraits=true;
                var forest=new Forest();Game1.currentLocation=forest;
                forest.map=helper.GameContent.Load<xTile.Map>("Maps/Forest");
                Utility.TryOpenShopMenu("Traveler", forest, playOpenSound: false);
                var shop=Game1.activeClickableMenu as ShopMenu??throw new Exception("Native Traveling Merchant shop absent");
                if(shop.ShopId!="Traveler")throw new Exception("Wrong native shop");stock=shop.forSale.Count;greeting=shop.potraitPersonDialogue;stage=1;return;
            }
            var active=Game1.activeClickableMenu as ShopMenu??throw new Exception("Shop closed unexpectedly");
            if(active.ShopId!="Traveler"||active.portraitTexture==null||active.forSale.Count!=stock||active.potraitPersonDialogue!=greeting||stock==0)throw new Exception("Shop portrait, inventory or greeting mismatch");
            var expected=helper.GameContent.Load<Texture2D>("Portraits/TravelingMerchant");var a=new Color[active.portraitTexture.Width*active.portraitTexture.Height];var b=new Color[expected.Width*expected.Height];active.portraitTexture.GetData(a);expected.GetData(b);if(!a.SequenceEqual(b))throw new Exception("Wrong portrait pixels");
            if(Game1.player.Money!=oldMoney)throw new Exception("Money changed without purchase");
            Game1.uiViewport=new xTile.Dimensions.Rectangle(0,0,1920,1080);
            if(active.xPositionOnScreen<=320)throw new Exception("Merchant panel will not fit test viewport");
            var device=Game1.graphics.GraphicsDevice;var previousTargets=device.GetRenderTargets();using var target=new RenderTarget2D(device,1920,1080);using var batch=new SpriteBatch(device);
            try{device.SetRenderTarget(target);device.Clear(new Color(35,45,58));batch.Begin(samplerState:SamplerState.PointClamp);active.draw(batch);batch.End();device.SetRenderTargets(previousTargets);using var stream=File.Create(Path.Combine(helper.DirectoryPath,"traveling-merchant-shop-preview.png"));target.SaveAsPng(stream,target.Width,target.Height);}
            finally{device.SetRenderTargets(previousTargets);}
            helper.Data.WriteJsonFile("traveling-merchant-checks.json",new{Passed=true,NativeTravelerShop=true,PortraitPixelsMatch=true,StockPreserved=true,GreetingPreserved=true,AvailableItems=stock,WideScreenRendered=true,TransactionsTested=false,FarmLoaded=false});
            monitor.Log("Traveling Merchant checks passed: native Traveler shop, matching portrait, inventory and greeting preserved; wide preview rendered.",LogLevel.Info);
        }
        catch(Exception ex){helper.Data.WriteJsonFile("traveling-merchant-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log($"Traveling Merchant audit failed: {ex}",LogLevel.Error);}
        menuField.SetValue(null,oldMenu);Game1.currentLocation=oldLocation;Game1.uiViewport=oldViewport;Game1.dialogueUp=oldDialogue;Game1.player.CanMove=oldMove;Game1.options.showMerchantPortraits=oldMerchantPortraits;
        if(!hadAchievement)Game1.player.achievements.Remove(0);Finished=true;
    }
}

