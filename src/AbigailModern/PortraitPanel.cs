using System.Runtime.CompilerServices;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;

namespace AbigailModern;

/// <summary>Adds artwork to native object dialogue without changing its text or response handling.</summary>
public static class PortraitPanel
{
    private sealed record Panel(Texture2D Texture, string Name, int Emotion);
    private static readonly ConditionalWeakTable<DialogueBox, Panel> Panels = new();
    private static IModHelper Helper = null!;
    private static IMonitor Monitor = null!;

    public static void Initialize(IModHelper helper, IMonitor monitor, string uniqueId)
    {
        Helper = helper;
        Monitor = monitor;
        helper.Events.Display.RenderedActiveMenu += (_, e) =>
        {
            if (Game1.activeClickableMenu is DialogueBox box) Render(e.SpriteBatch, box);
        };
        helper.Events.Display.MenuChanged += (_, e) =>
        {
            if (e.NewMenu is ShopMenu islandShop && islandShop.ShopId == "IslandTrade")
                islandShop.portraitTexture = Helper.GameContent.Load<Texture2D>("Portraits/IslandTrader");
            if (e.NewMenu is ShopMenu desertShop && desertShop.ShopId == "DesertTrade")
                desertShop.portraitTexture = Helper.GameContent.Load<Texture2D>("Portraits/DesertTrader");
            if (e.NewMenu is ShopMenu travelerShop && travelerShop.ShopId == "Traveler")
            {
                try { travelerShop.portraitTexture = Helper.GameContent.Load<Texture2D>("Portraits/TravelingMerchant"); }
                catch (Exception ex) { Monitor.Log($"Could not display the Traveling Merchant portrait: {ex.Message}", LogLevel.Warn); }
            }
            if (e.NewMenu is ShopMenu mouseShop && mouseShop.ShopId == "HatMouse")
            {
                try { mouseShop.portraitTexture = Helper.GameContent.Load<Texture2D>("Portraits/HatMouse"); }
                catch (Exception ex) { Monitor.Log($"Could not display Hat Mouse's shop portrait: {ex.Message}", LogLevel.Warn); }
            }
            if (e.NewMenu is ShopMenu shop && shop.ShopId is "Bookseller" or "BooksellerTrade")
            {
                try { shop.portraitTexture = Helper.GameContent.Load<Texture2D>("Portraits/Marcello"); }
                catch (Exception ex) { Monitor.Log($"Could not display the Bookseller shop portrait: {ex.Message}", LogLevel.Warn); }
            }
        };
        new Harmony(uniqueId).Patch(
            AccessTools.Method(typeof(GameLocation), nameof(GameLocation.performAction), new[] { typeof(string[]), typeof(Farmer), typeof(xTile.Dimensions.Location) }),
            prefix: new HarmonyMethod(typeof(PortraitPanel), nameof(BeforeBooksellerAction)),
            postfix: new HarmonyMethod(typeof(PortraitPanel), nameof(AfterBooksellerAction)));
        new Harmony(uniqueId).Patch(
            AccessTools.Method(typeof(Beach), nameof(Beach.checkAction)),
            prefix: new HarmonyMethod(typeof(PortraitPanel), nameof(BeforeBeachAction)),
            postfix: new HarmonyMethod(typeof(PortraitPanel), nameof(AfterBeachAction)));
    }

    public static void Attach(DialogueBox box, Texture2D texture, string name, int emotion)
    {
        if (texture.Width < 64 || texture.Height < 64) throw new ArgumentException("Portrait needs at least one 64px cell.");
        var count = (texture.Width / 64) * (texture.Height / 64);
        Panels.Remove(box);
        Panels.Add(box, new Panel(texture, name, Math.Clamp(emotion, 0, count - 1)));
    }

    public static bool HasPanel(DialogueBox box) => Panels.TryGetValue(box, out _);


    private static void BeforeBooksellerAction(out IClickableMenu? __state) => __state = Game1.activeClickableMenu;

    private static void AfterBooksellerAction(string[] action, IClickableMenu? __state)
    {
        if (action.Length == 0 || action[0] != "Bookseller" || ReferenceEquals(__state, Game1.activeClickableMenu)
            || Game1.activeClickableMenu is not DialogueBox box || !box.isQuestion || box.characterDialogue != null) return;
        try
        {
            Attach(box, Helper.GameContent.Load<Texture2D>("Portraits/Marcello"), Game1.content.LoadString("Strings\\1_6_Strings:Bookseller"), 1);
        }
        catch (Exception ex) { Monitor.Log($"Could not display the Bookseller greeting portrait: {ex.Message}", LogLevel.Warn); }
    }

    public static void Render(SpriteBatch batch, DialogueBox box)
    {
        GourmandPortraits.Refresh(box);
        RaccoonPortraits.Refresh(box);
        if (Panels.TryGetValue(box, out var panel)) Draw(batch, box, panel);
    }

    private static void BeforeBeachAction(Beach __instance, xTile.Dimensions.Location tileLocation, out NPC? __state)
    {
        var mariner = AccessTools.Field(typeof(Beach), "oldMariner").GetValue(__instance) as NPC;
        __state = mariner != null && mariner.TilePoint.X == tileLocation.X && mariner.TilePoint.Y == tileLocation.Y ? mariner : null;
    }

    private static void AfterBeachAction(bool __result, NPC? __state)
    {
        if (!__result || __state == null || Game1.activeClickableMenu is not DialogueBox box || box.characterDialogue != null) return;
        try
        {
            var texture = Helper.GameContent.Load<Texture2D>("Portraits/Mariner");
            Attach(box, texture, __state.displayName, box.isQuestion ? 1 : 0);
        }
        catch (Exception ex)
        {
            Monitor.Log($"Could not display the Old Mariner portrait: {ex.Message}", LogLevel.Warn);
        }
    }

    private static void Draw(SpriteBatch batch, DialogueBox box, Panel panel)
    {
        // Keep the original dialogue and its clickable response area untouched.
        var portraitSize = 64 * Math.Clamp((box.y - 108) / 64, 1, 3);
        var width = portraitSize + 32;
        var height = portraitSize + 64;
        var x = Math.Clamp(box.x, 16, Math.Max(16, Game1.uiViewport.Width - width - 16));
        var y = Math.Clamp(box.y - height - 44, 16, Math.Max(16, Game1.uiViewport.Height - height - 16));
        IClickableMenu.drawTextureBox(batch, x, y, width, height, Color.White);
        var columns = panel.Texture.Width / 64;
        var source = new Rectangle(panel.Emotion % columns * 64, panel.Emotion / columns * 64, 64, 64);
        batch.Draw(panel.Texture, new Rectangle(x + 16, y + 16, portraitSize, portraitSize), source, Color.White);
        var nameSize = Game1.smallFont.MeasureString(panel.Name);
        var scale = Math.Min(1f, portraitSize / Math.Max(1f, nameSize.X));
        batch.DrawString(Game1.smallFont, panel.Name, new Vector2(x + (width - nameSize.X * scale) / 2, y + portraitSize + 24), Game1.textColor, 0, Vector2.Zero, scale, SpriteEffects.None, 1);
    }
}


