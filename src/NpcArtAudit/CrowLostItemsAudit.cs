using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Network;
using xTile;
using xTile.Dimensions;
using xTile.Layers;
using xTile.Tiles;

namespace NpcArtAudit;

// Integration draft: Run on the game thread, in an isolated single-player session.
// Deferred check allows the production MenuChanged portrait hook to run first.
internal static class CrowLostItemsAudit
{
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        var data = DataLoader.Shops(Game1.content)["LostItems"];
        helper.Data.WriteJsonFile("crow-native-shop-data.json", data);
        if (Game1.IsMultiplayer || Context.IsMultiplayer || !Game1.IsMasterGame)
            throw new InvalidOperationException("Crow fixture requires a single-player host.");

        var playerField = typeof(Game1).GetField("_player", BindingFlags.Static | BindingFlags.NonPublic)!;
        var menuField = typeof(Game1).GetField("_activeClickableMenu", BindingFlags.Static | BindingFlags.NonPublic)!;
        var originalPlayer = Game1.player;
        var originalTeam = originalPlayer.team;
        var originalMenu = Game1.activeClickableMenu;
        var originalLocation = Game1.currentLocation;
        var originalRandom = Game1.random;
        var originalFreeze = Game1.freezeControls;
        var originalDialogue = Game1.dialogueUp;
        var originalAfter = Game1.afterDialogues;
        var originalCursor = Game1.mouseCursorTransparency;
        var originalViewport = Game1.uiViewport;
        var originalMerchantPortraits = Game1.options.showMerchantPortraits;
        Farmer? fixturePlayer = null;
        ShopMenu? shop = null;
        NetMutex? mutex = null;
        Action? verifyInventory = null;
        var checks = new List<string>();
        string? failure = null;
        bool finished = false;
        int deferredTicks = 0;

        void Check(bool value, string name)
        {
            if (!value) throw new InvalidOperationException(name);
            checks.Add(name);
        }

        void RestoreGlobals(bool restoreMenu)
        {
            // Do not use Game1.player's setter: it unloads the outgoing farmer.
            playerField.SetValue(null, originalPlayer);
            Game1.game1.instanceGameLocation = originalLocation;
            Game1.random = originalRandom;
            Game1.freezeControls = originalFreeze;
            Game1.dialogueUp = originalDialogue;
            Game1.afterDialogues = originalAfter;
            Game1.mouseCursorTransparency = originalCursor;
            Game1.uiViewport = originalViewport;
            Game1.options.showMerchantPortraits = originalMerchantPortraits;
            if (restoreMenu) menuField.SetValue(null, originalMenu);
        }

        void Finish()
        {
            if (finished) return;
            finished = true;
            helper.Events.GameLoop.UpdateTicked -= OnTick;
            try
            {
                // Native close callback resolves its mutex through Game1.player.team.
                playerField.SetValue(null, fixturePlayer ?? originalPlayer);
                if (shop != null && ReferenceEquals(Game1.activeClickableMenu, shop))
                    shop.exitThisMenu(playSound: false);
                if (mutex != null) Check(!mutex.IsLocked(), "Native close releases fixture mutex");
                verifyInventory?.Invoke();
                if (fixturePlayer != null)
                    Check(fixturePlayer.Money == 123456, "No purchase or currency change");
            }
            catch (Exception ex) { failure ??= ex.ToString(); }
            finally
            {
                // Failure cleanup touches only the temporary team's mutex.
                mutex?.ReleaseLock();
                RestoreGlobals(true);
            }
            helper.Data.WriteJsonFile("crow-lost-items-checks.json", new
            {
                Passed = failure == null,
                Error = failure,
                Checks = checks,
                OriginalPlayerAndTeamRestored = ReferenceEquals(Game1.player, originalPlayer)
                    && ReferenceEquals(Game1.player.team, originalTeam),
                NativeMapClockSamples = true,
                RealTimePlayback = false,
                PurchasePerformed = false,
                DialogueAdded = false,
                PortraitExpression = 0
            });
            monitor.Log(failure == null ? "Crow LostItems fixture passed." : "Crow LostItems fixture failed: " + failure,
                failure == null ? LogLevel.Info : LogLevel.Error);
        }

        void OnTick(object? sender, UpdateTickedEventArgs e)
        {
            if (++deferredTicks < 2) return;
            try
            {
                Check(ReferenceEquals(Game1.activeClickableMenu, shop), "Native shop remains active after MenuChanged");
                var portrait = helper.GameContent.Load<Texture2D>("Portraits/Crow");
                Check(portrait.Width == 128 && portrait.Height == 192, "Six native-size Crow portrait cells");
                Check(shop!.portraitTexture != null, "Native LostItems shop receives portrait");
                var actual = new Microsoft.Xna.Framework.Color[shop.portraitTexture.Width * shop.portraitTexture.Height];
                var expected = new Microsoft.Xna.Framework.Color[portrait.Width * portrait.Height];
                shop.portraitTexture.GetData(actual);
                portrait.GetData(expected);
                Check(actual.SequenceEqual(expected), "Shop portrait pixels match registered Crow asset");
                Check(string.IsNullOrEmpty(shop.potraitPersonDialogue), "Native silent shop retained after hook");
                CrowShopCapture.Save(helper,shop);
            }
            catch (Exception ex) { failure = ex.ToString(); }
            finally { Finish(); }
        }

        try
        {
            Game1.random = new Random(197731);
            fixturePlayer = new Farmer { UniqueMultiplayerID = originalPlayer.UniqueMultiplayerID };
            playerField.SetValue(null, fixturePlayer);
            fixturePlayer.Money = 123456;
            Check(!ReferenceEquals(fixturePlayer.team, originalTeam), "Fixture has an independent team and inventory");
            // Minimal fresh map: never mutate the loaded Woods map or run ResetLostItemsShop.
            var map = new Map();
            var buildings = new Layer("Buildings", map, new Size(32, 32), new Size(16, 16));
            var front = new Layer("Front", map, new Size(32, 32), new Size(16, 16));
            map.AddLayer(buildings);
            map.AddLayer(front);
            var baseSheet = new TileSheet("untitled tile sheet", map, "Maps/spring_outdoorsTileSheet", new Size(16, 16), new Size(16, 16));
            map.AddTileSheet(baseSheet);
            for (int x = 11; x <= 13; x++) buildings.Tiles[x, 6] = new StaticTile(buildings, baseSheet, BlendMode.Alpha, 0);
            var woods = new Woods { Map = map };
            var inventory = Woods.GetLostItemsShopInventory();
            // Resolver accepts the inventory contents directly. Ordinary items avoid
            // altering real lost-item eligibility, quest state or unique-item records.
            var first = ItemRegistry.Create("(O)388");
            var second = ItemRegistry.Create("(O)390");
            inventory.Add(first);
            inventory.Add(second);
            verifyInventory = () => Check(inventory.Count == 2 && ReferenceEquals(inventory[0], first)
                && ReferenceEquals(inventory[1], second) && first.Stack == 1 && second.Stack == 1,
                "Fixture inventory unchanged after native close");
            mutex = Woods.GetLostItemShopMutex();
            Check(!mutex.IsLocked(), "Fixture mutex starts unlocked");
            typeof(Woods).GetMethod("UpdateLostItemsShopTile", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(woods, null);
            var texture = helper.GameContent.Load<Texture2D>("Characters/Crow");
            Check(texture.Width == 512 && texture.Height == 32, "Crow native sprite sheet is 512 by 32");
            var upper = (AnimatedTile)front.Tiles[12, 4];
            var lower = (AnimatedTile)buildings.Tiles[12, 5];
            Check(upper.FrameInterval == 100 && lower.FrameInterval == 100, "Both native strips use 100ms frames");
            Check(upper.TileFrames.Length == 32 && lower.TileFrames.Length == 32, "Both native strips have 32 frames");
            for (int i = 0; i < 32; i++)
            {
                map.ElapsedTime = i * 100;
                Check(upper.TileIndex == i && lower.TileIndex == i + 32, "Paired native frame " + i);
                map.ElapsedTime = i * 100 + 99;
                Check(upper.TileIndex == i && lower.TileIndex == i + 32, "Frame holds through 99ms: " + i);
            }
            map.ElapsedTime = 3200;
            Check(upper.TileIndex == 0 && lower.TileIndex == 32, "Paired animation wraps at 3200ms");
            Check(lower.Properties["Action"].ToString() == "LostItemsShop", "Crow lower tile retains native shop action");
            Check(data.Currency == 0 && data.OpenSound == "crow", "Native currency and opening sound retained");
            Check(data.Owners.Count == 1 && data.Owners[0].Dialogues.Count == 0 && string.IsNullOrEmpty(data.Owners[0].Portrait), "Native data has no greeting or portrait override");
            Game1.game1.instanceGameLocation = woods;
            Game1.uiViewport = new xTile.Dimensions.Rectangle(0,0,1920,1080);
            Game1.options.showMerchantPortraits = true;
            Check(woods.performAction(new[] { "LostItemsShop" }, fixturePlayer, new Location(12, 5)), "Native Woods shop action handled");
            mutex.Update(new FarmerCollection());
            Check(mutex.IsLockHeld(), "Native shop action acquired fixture mutex");
            shop = Game1.activeClickableMenu as ShopMenu;
            Check(shop != null && shop.ShopId == "LostItems", "Native action opens LostItems shop");
            Check(ReferenceEquals(shop!.ShopData, data) && shop.currency == 0, "Native shop data and currency retained");
            Check(shop.forSale.Count == 2 && ReferenceEquals(shop.forSale[0], first) && ReferenceEquals(shop.forSale[1], second), "Native inventory references and ordering retained");
            foreach (var item in shop.forSale)
            {
                var stock = shop.itemPriceAndStock[item];
                Check(stock.Price == 10000 && stock.Stock == 1 && stock.TradeItem == null && ReferenceEquals(stock.ItemToSyncStack, item), "Native stock and stack synchronization: " + item.Name);
            }
            Check(shop.onPurchase == null && shop.onSell == null && shop.canPurchaseCheck == null, "Native transaction callbacks retained");
            Check(shop.behaviorBeforeCleanup != null && ReferenceEquals(shop.behaviorBeforeCleanup.Target, woods)
                && shop.behaviorBeforeCleanup.Method.Name == "OnLostItemsShopClosed", "Native mutex-release callback retained");
            Check(string.IsNullOrEmpty(shop.potraitPersonDialogue), "Native shop opens silently");
            RestoreGlobals(false);
            helper.Events.GameLoop.UpdateTicked += OnTick;
        }
        catch (Exception ex)
        {
            failure = ex.ToString();
            Finish();
        }
    }
}
