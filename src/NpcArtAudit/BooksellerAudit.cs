using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System.Reflection;

namespace NpcArtAudit;

internal sealed class BooksellerAudit
{
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly IClickableMenu originalMenu = Game1.activeClickableMenu;
    private readonly GameLocation originalLocation = Game1.currentLocation;
    private readonly int originalDay = Game1.dayOfMonth;
    private readonly bool originalMail = Game1.player.mailReceived.Contains("read_a_book");
    private readonly bool originalDialogue = Game1.dialogueUp;
    private readonly bool originalMove = Game1.player.CanMove;
    private readonly FieldInfo menuField = typeof(Game1).GetField("_activeClickableMenu", BindingFlags.Static | BindingFlags.NonPublic)!;
    private int stage;
    private int ticks;
    public bool Finished { get; private set; }
    public BooksellerAudit(IModHelper helper, IMonitor monitor) { this.helper = helper; this.monitor = monitor; }

    public void Tick()
    {
        if (Finished || ++ticks % 3 != 0) return;
        try
        {
            if (stage == 0)
            {
                menuField.SetValue(null, null);
                Game1.currentLocation = new GameLocation("Maps/Town", "Town");
                Game1.dayOfMonth = Utility.getDaysOfBooksellerThisSeason()[0];
                if (!originalMail) Game1.player.mailReceived.Add("read_a_book");
                Game1.currentLocation.performAction(new[] { "Bookseller" }, Game1.player, new xTile.Dimensions.Location(111, 26));
                var box = Game1.activeClickableMenu as DialogueBox ?? throw new Exception("Bookseller greeting did not open.");
                var panel = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "AbigailModern").GetType("AbigailModern.PortraitPanel")!;
                if (!(bool)panel.GetMethod("HasPanel")!.Invoke(null, new object[] { box })! || !box.responses.Select(r => r.responseKey).SequenceEqual(new[] { "Buy", "Trade", "Leave" }))
                    throw new Exception("Greeting portrait or native response keys missing.");
                Capture("bookseller-greeting-preview.png", batch => { box.transitioning = false; box.transitionInitialized = true; box.draw(batch); panel.GetMethod("Render")!.Invoke(null, new object[] { batch, box }); });
                Game1.activeClickableMenu = null;
                if (!Utility.TryOpenShopMenu("Bookseller", null, playOpenSound: false)) throw new Exception("Buy shop did not open.");
                stage = 1;
            }
            else if (stage == 1)
            {
                CheckShop("Bookseller", "bookseller-buy-preview.png");
                Game1.activeClickableMenu = null;
                if (!Utility.TryOpenShopMenu("BooksellerTrade", null, playOpenSound: false)) throw new Exception("Trade shop did not open.");
                stage = 2;
            }
            else
            {
                CheckShop("BooksellerTrade", "bookseller-trade-preview.png");
                helper.Data.WriteJsonFile("bookseller-interaction-checks.json", new { Passed = true, NativeGreeting = true, ResponseKeysPreserved = true, BuyPortrait = true, TradePortrait = true, TransactionsTested = false, FarmLoaded = false });
                monitor.Log("Bookseller greeting and both real shop portrait checks passed; no farm loaded or purchase made.", LogLevel.Info);
                Restore();
            }
        }
        catch (Exception ex)
        {
            helper.Data.WriteJsonFile("bookseller-interaction-checks.json", new { Passed = false, Stage = stage, Error = ex.GetBaseException().ToString() });
            monitor.Log($"Bookseller checks failed: {ex.GetBaseException()}", LogLevel.Warn);
            Restore();
        }
    }
    private void CheckShop(string id, string file)
    {
        var shop = Game1.activeClickableMenu as ShopMenu;
        var expected = helper.GameContent.Load<Texture2D>("Portraits/Marcello");
        if (shop?.ShopId != id || shop.portraitTexture == null) throw new Exception($"Shop state: wanted={id}, menu={Game1.activeClickableMenu?.GetType().Name}, actual={shop?.ShopId}, portrait={shop?.portraitTexture?.Width}x{shop?.portraitTexture?.Height}.");
        var actualPixels = new Color[shop.portraitTexture.Width * shop.portraitTexture.Height];
        var expectedPixels = new Color[expected.Width * expected.Height];
        shop.portraitTexture.GetData(actualPixels); expected.GetData(expectedPixels);
        if (!actualPixels.SequenceEqual(expectedPixels)) throw new Exception("Shop portrait pixels differ from Marcello artwork.");
        Capture(file, shop.draw);
    }
    private void Capture(string file, Action<SpriteBatch> draw)
    {
        var device = Game1.graphics.GraphicsDevice;
        var targets = device.GetRenderTargets();
        using var target = new RenderTarget2D(device, Game1.uiViewport.Width, Game1.uiViewport.Height);
        using var batch = new SpriteBatch(device);
        try
        {
            device.SetRenderTarget(target); device.Clear(new Color(35,45,58));
            batch.Begin(samplerState: SamplerState.PointClamp); draw(batch); batch.End();
            device.SetRenderTargets(targets);
            using var stream = File.Create(Path.Combine(helper.DirectoryPath,file));
            target.SaveAsPng(stream,target.Width,target.Height);
        }
        finally { device.SetRenderTargets(targets); }
    }
    private void Restore()
    {
        menuField.SetValue(null, originalMenu);
        Game1.currentLocation = originalLocation; Game1.dayOfMonth = originalDay;
        if (!originalMail) Game1.player.mailReceived.Remove("read_a_book");
        Game1.dialogueUp = originalDialogue; Game1.player.CanMove = originalMove;
        Finished = true;
    }
}
