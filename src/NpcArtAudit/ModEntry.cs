using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NpcArtAudit;

public sealed class ModEntry : Mod
{
    public override void Entry(IModHelper helper)
    {
        var interactionChecked = false;
        BooksellerAudit? booksellerAudit = null;
        RaccoonShopAudit? raccoonShopAudit = null;
        HatMouseAudit? hatMouseAudit = null; TravelingMerchantAudit? travelingMerchantAudit = null; DesertTraderAudit? desertTraderAudit = null; IslandTraderAudit? islandTraderAudit = null;
        helper.Events.GameLoop.UpdateTicked += (_, e) =>
        {
            if (hatMouseAudit != null) { hatMouseAudit.Tick(); if (hatMouseAudit.Finished) { hatMouseAudit=null; travelingMerchantAudit=new TravelingMerchantAudit(helper,Monitor); } return; }
            if (travelingMerchantAudit != null) { travelingMerchantAudit.Tick(); if (travelingMerchantAudit.Finished) { travelingMerchantAudit=null; desertTraderAudit=new DesertTraderAudit(helper,Monitor); } return; }
            if (desertTraderAudit != null) { desertTraderAudit.Tick(); if (desertTraderAudit.Finished) { desertTraderAudit=null; islandTraderAudit=new IslandTraderAudit(helper,Monitor); } return; }
            if (islandTraderAudit != null) { islandTraderAudit.Tick(); if (islandTraderAudit.Finished) { islandTraderAudit=null; raccoonShopAudit=new RaccoonShopAudit(helper,Monitor); } return; }
            if (booksellerAudit != null) { booksellerAudit.Tick(); if (booksellerAudit.Finished) { booksellerAudit=null; CrowLostItemsAudit.Run(helper,Monitor); } return; }
            if (raccoonShopAudit != null) { raccoonShopAudit.Tick(); if (raccoonShopAudit.Finished) { raccoonShopAudit=null; booksellerAudit=new BooksellerAudit(helper,Monitor); } return; }
            if (interactionChecked || e.Ticks < 120 || Context.IsWorldReady || Game1.currentGameTime == null || Game1.activeClickableMenu is not StardewValley.Menus.TitleMenu) return;
            interactionChecked = true;
            if (helper.Data.ReadJsonFile<string[]>("selected-checks.json") is { Length: > 0 } selected)
            {
                foreach (string check in selected)
                {
                    switch (check)
                    {
                        case "alex-base": AlexBaseAudit.Run(helper, Monitor); break;
                        case "elliott-base": ElliottBaseAudit.Run(helper, Monitor); break;
                        case "elliott-beach": ElliottBeachAudit.Run(helper, Monitor); break;
                        case "harvey-winter": HarveyWinterAudit.Run(helper, Monitor); break;
                        case "sam-beach": SamBeachAudit.Run(helper, Monitor); break;
                        case "sam-winter": SamWinterAudit.Run(helper, Monitor); break;
                        case "visual-effects": VisualEffectsAudit.Run(helper, Monitor); break;
                        case "farm-buildings": FarmBuildingsAudit.Run(helper, Monitor); break;
                        case "crops-trees": CropsTreesAudit.Run(helper, Monitor); break;
                        case "tree-variants": TreeVariantsAudit.Run(helper, Monitor); break;
                        case "locations": LocationsAudit.Run(helper, Monitor); break;
                        case "blue-ui": BlueUiAudit.Run(helper, Monitor); break;
                        case "animal-contracts": AnimalsAudit.Export(helper, Monitor); break;
                        case "item-contracts": ItemsAudit.Export(helper, Monitor); break;
                        case "scenes": ScenesAudit.Run(helper, Monitor); break;
                        case "soil-floor": SoilFloorAudit.Run(helper, Monitor); break;
                        case "items": ItemsAudit.Run(helper, Monitor); break;
                        case "monsters-wildlife": MonstersWildlifeAudit.Run(helper, Monitor); break;
                        case "animals": AnimalsAudit.Run(helper, Monitor); break;
                        case "toolbar": ToolbarAudit.Run(helper, Monitor); break;
                        case "player-hd": PlayerHdAudit.Run(helper, Monitor); break;
                        case "player-hd-production": PlayerHdAudit.Run(helper, Monitor, true); break;
                        case "cave-atmosphere": CaveAtmosphereAudit.Run(helper, Monitor); break;
                        case "utility-building-contracts": UtilityBuildingsAudit.Export(helper, Monitor); break;
                        case "utility-buildings": UtilityBuildingsAudit.Run(helper, Monitor); break;
                        default: throw new InvalidOperationException("Unknown selected audit: " + check);
                    }
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
                return;
            }
            helper.Data.WriteJsonFile("crow-native-shop-data.json", StardewValley.DataLoader.Shops(Game1.content)["LostItems"]);
            WeatherParticlesAudit.Run(helper, Monitor);
            TownBuildingsAudit.Run(helper, Monitor);
            TownHousesAudit.Run(helper, Monitor);
            TownCivicAudit.Run(helper, Monitor);
            VisualEffectsAudit.Run(helper, Monitor);
            FarmBuildingsAudit.Run(helper, Monitor);
            CropsTreesAudit.Run(helper, Monitor);
            TreeVariantsAudit.Run(helper, Monitor);
            LocationsAudit.Run(helper, Monitor);
            StormHailAudit.Run(helper, Monitor);
            OutdoorPropsAudit.Run(helper, Monitor);
            VegetationAudit.Run(helper, Monitor);
            TerrainStarterAudit.Run(helper, Monitor);
            WelwickAudit.Run(helper, Monitor);
            QueenOfSauceAudit.Run(helper, Monitor);
            LivinOffTheLandAudit.Run(helper, Monitor);
            WeatherPresenterAudit.Run(helper, Monitor);
            MermaidAudit.Run(helper, Monitor);
            MermaidRisingAudit.Run(helper, Monitor);
            IslandParrotAudit.Run(helper, Monitor);
            JunimoAudit.Run(helper, Monitor);
            JunimoRepairsAudit.Run(helper, Monitor);
            ConstructionWorkersAudit.Run(helper, Monitor);
            GoldenParrotWorkerAudit.Run(helper, Monitor);
            WitchFlightAudit.Run(helper, Monitor);
            QiPlanePilotAudit.Run(helper, Monitor);
            JojaOpeningEmployeesAudit.Run(helper, Monitor);
            ChildAudit.Run(helper, Monitor);
            AbigailWinterAudit.Run(helper, Monitor);
            AbigailBeachAudit.Run(helper, Monitor);
            AbigailBaseAudit.Run(helper, Monitor);
            EmilyWinterAudit.Run(helper, Monitor);
            EmilyBeachAudit.Run(helper, Monitor);
            EmilyBaseAudit.Run(helper, Monitor);
            HaleyWinterAudit.Run(helper, Monitor);
            HaleyBeachAudit.Run(helper, Monitor);
            HaleyBaseAudit.Run(helper, Monitor);
            LeahWinterAudit.Run(helper, Monitor);
            LeahBaseAudit.Run(helper, Monitor);
            LeahBeachAudit.Run(helper, Monitor);
            MaruWinterAudit.Run(helper, Monitor);
            MaruBaseAudit.Run(helper, Monitor);
            MaruHospitalAudit.Run(helper, Monitor);
            MaruBeachAudit.Run(helper, Monitor);
            PennyWinterAudit.Run(helper, Monitor);
            PennyBeachAudit.Run(helper, Monitor);
            PennyBaseAudit.Run(helper, Monitor);
            AlexWinterAudit.Run(helper, Monitor);
            AlexBeachAudit.Run(helper, Monitor);
            AlexBaseAudit.Run(helper, Monitor);
            ElliottWinterAudit.Run(helper, Monitor);
            ElliottBeachAudit.Run(helper, Monitor);
            ElliottBaseAudit.Run(helper, Monitor);
            HarveyWinterAudit.Run(helper, Monitor);
            HarveyBeachAudit.Run(helper, Monitor);
            HarveyBaseAudit.Run(helper, Monitor);
            SamWinterAudit.Run(helper, Monitor);
            SamBeachAudit.Run(helper, Monitor);
            SamBaseAudit.Run(helper, Monitor);
            SamJojaMartAudit.Run(helper, Monitor);
            EvelynAudit.Run(helper, Monitor);
            SebastianAudit.Run(helper, Monitor);
            CarolineAudit.Run(helper, Monitor);
            GeorgeAudit.Run(helper, Monitor);
            DemetriusAudit.Run(helper, Monitor);
            RobinAudit.Run(helper, Monitor);
            ShaneAudit.Run(helper, Monitor);
            PierreAudit.Run(helper, Monitor);
            GusAudit.Run(helper, Monitor);
            JodiAudit.Run(helper, Monitor);
            LewisAudit.Run(helper, Monitor);
            ClintAudit.Run(helper, Monitor);
            PamAudit.Run(helper, Monitor);
            MarnieAudit.Run(helper, Monitor);
            KentAudit.Run(helper, Monitor);
            LinusAudit.Run(helper, Monitor);
            GrandpaStoryAudit.Run(helper, Monitor);
            RobotPortraitAudit.Run(helper, Monitor);
            RobotArtAudit.Run(helper, Monitor);
            WillyAudit.Run(helper, Monitor);
            VincentAudit.Run(helper, Monitor);
            JasAudit.Run(helper, Monitor);
            ClothesTherapyAudit.Run(helper, Monitor);
            SasquatchAudit.Run(helper, Monitor);
            LeoAudit.Run(helper, Monitor);
            AbigailAdventureAudit.Run(helper, Monitor);
            FishingContestantAudit.Run(helper, Monitor);
            WinterMysteryAudit.Run(helper, Monitor);
            KrobusDisguiseAudit.Run(helper, Monitor);
            KrobusParadeAudit.Run(helper, Monitor);
            SeaMonsterAudit.Run(helper, Monitor);
            GilAudit.Run(helper, Monitor);
            RaccoonAudit.Run(helper, Monitor);
            TrashBearAudit.Run(helper, Monitor);
            KelAudit.Run(helper, Monitor);
            GourmandAudit.Run(helper, Monitor);
            CheckMarinerInteraction(helper);
            hatMouseAudit = new HatMouseAudit(helper, Monitor);
        };
        helper.Events.GameLoop.GameLaunched += (_, _) =>
        {
            try
            {
                helper.Data.WriteJsonFile("characters.json", Game1.characterData);
                var traderMap = helper.GameContent.Load<xTile.Map>("Maps/Island_N_Trader");
                var traderTiles = new List<object>();
                foreach (var layer in traderMap.Layers)
                    for (var y = 0; y < layer.LayerHeight; y++)
                        for (var x = 0; x < layer.LayerWidth; x++)
                        {
                            var tile=layer.Tiles[x,y]; if(tile==null)continue;
                            var frames=tile is xTile.Tiles.AnimatedTile animated ? animated.TileFrames : new[]{(xTile.Tiles.StaticTile)tile};
                            traderTiles.Add(new {Layer=layer.Id,X=x,Y=y,Frames=frames.Select(f=>new{Index=f.TileIndex,Texture=f.TileSheet.ImageSource,Columns=f.TileSheet.SheetWidth}).ToArray()});
                        }
                helper.Data.WriteJsonFile("island-trader-tiles.json",traderTiles);
                var guildMap = helper.GameContent.Load<xTile.Map>("Maps/AdventureGuild");
                var guildTiles = new List<object>();
                foreach (var layer in guildMap.Layers)
                    for (var y = 0; y < layer.LayerHeight; y++)
                        for (var x = 0; x < layer.LayerWidth; x++)
                        {
                            var tile = layer.Tiles[x, y];
                            if (tile != null && tile.TileIndex >= 1220 && tile.TileIndex <= 1425)
                                guildTiles.Add(new { Layer = layer.Id, X = x, Y = y, Index = tile.TileIndex, Sheet = tile.TileSheet.Id, Texture = tile.TileSheet.ImageSource, Columns = tile.TileSheet.SheetWidth });
                        }
                helper.Data.WriteJsonFile("guild-embedded-tiles.json", guildTiles);
                var roster = Game1.characterData.Keys.OrderBy(name => name).Select(name =>
                {
                    var textureName = NPC.getTextureNameForCharacter(name);
                    return new
                    {
                        Name = name,
                        TextureName = textureName,
                        HasSprite = helper.GameContent.DoesAssetExist<Texture2D>(helper.GameContent.ParseAssetName($"Characters/{textureName}")),
                        HasPortrait = helper.GameContent.DoesAssetExist<Texture2D>(helper.GameContent.ParseAssetName($"Portraits/{textureName}"))
                    };
                }).ToArray();
                helper.Data.WriteJsonFile("roster.json", roster);
                helper.Data.WriteJsonFile("npc-types.json", typeof(NPC).Assembly.GetTypes()
                    .Where(type => type.IsSubclassOf(typeof(NPC))).Select(type => type.FullName).OrderBy(name => name).ToArray());
                Monitor.Log($"Exported {roster.Length} character definitions and NPC subclass inventory without loading a farm.", LogLevel.Info);
                CheckMissingPortraitPanel(helper);
            }
            catch (Exception ex)
            {
                Monitor.Log($"NPC inventory failed: {ex}", LogLevel.Error);
            }
        };
    }

    private void CheckMissingPortraitPanel(IModHelper helper)
    {
        try
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "AbigailModern");
            var panel = assembly.GetType("AbigailModern.PortraitPanel")
                ?? throw new InvalidOperationException("Missing portrait panel has not been implemented.");
            var attach = panel.GetMethod("Attach")!;
            var hasPanel = panel.GetMethod("HasPanel")!;
            var answers = new[] { new Response("Buy", "Buy pendant"), new Response("Not", "Not now") };
            var question = new StardewValley.Menus.DialogueBox("Original shop question", answers);
            var plain = new StardewValley.Menus.DialogueBox("Original plain dialogue");
            var originalQuestion = question.dialogues.ToArray();
            var originalPlain = plain.dialogues.ToArray();
            using var portrait = new Texture2D(Game1.graphics.GraphicsDevice, 128, 192);
            attach.Invoke(null, new object[] { question, portrait, "Old Mariner", 1 });
            attach.Invoke(null, new object[] { plain, portrait, "Old Mariner", 2 });
            bool questionsPreserved = ReferenceEquals(question.responses, answers)
                && question.responses.Select(a => a.responseKey).SequenceEqual(new[] { "Buy", "Not" })
                && question.dialogues.SequenceEqual(originalQuestion)
                && question.characterDialogue == null && question.isQuestion;
            bool plainPreserved = plain.dialogues.SequenceEqual(originalPlain) && plain.characterDialogue == null;
            bool attached = (bool)hasPanel.Invoke(null, new object[] { question })! && (bool)hasPanel.Invoke(null, new object[] { plain })!;
            helper.Data.WriteJsonFile("portrait-checks.json", new { Passed = questionsPreserved && plainPreserved && attached, QuestionsPreserved = questionsPreserved, PlainDialoguePreserved = plainPreserved, PanelsAttached = attached });
            if (!questionsPreserved || !plainPreserved || !attached) throw new InvalidOperationException("Portrait panels changed native dialogue behavior or failed to attach.");
            Monitor.Log("Missing-portrait panel checks passed: native questions, response keys and plain text preserved.", LogLevel.Info);
        }
        catch (Exception ex)
        {
            helper.Data.WriteJsonFile("portrait-checks.json", new { Passed = false, Error = ex.GetBaseException().Message });
            Monitor.Log($"Missing-portrait panel checks failed: {ex.GetBaseException().Message}", LogLevel.Warn);
        }
    }

    private void CheckMarinerInteraction(IModHelper helper)
    {
        var originalMenu = Game1.activeClickableMenu;
        var originalDialogueUp = Game1.dialogueUp;
        var originalCanMove = Game1.player.CanMove;
        var menuField = typeof(Game1).GetField("_activeClickableMenu", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        var originalLocation = Game1.currentLocation;
        StardewValley.Locations.Beach? fixtureBeach = null;
        var device = Game1.graphics.GraphicsDevice;
        var originalTargets = device.GetRenderTargets();
        try
        {
            // Detach the title screen without disposing it through the public menu setter.
            menuField.SetValue(null, null);
            var beach = new StardewValley.Locations.Beach();
            fixtureBeach = beach;
            Game1.currentLocation = beach;
            Game1.locations.Add(beach);
            var friend = new NPC { Name = "Abigail" };
            friend.datable.Value = true;
            beach.characters.Add(friend);
            var mariner = new NPC(new AnimatedSprite("Characters/Mariner", 0, 16, 32), new Microsoft.Xna.Framework.Vector2(80 * 64, 5 * 64), 2, "Old Mariner");
            typeof(StardewValley.Locations.Beach).GetField("oldMariner", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(beach, mariner);
            var farmer = new Farmer();
            var panel = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "AbigailModern").GetType("AbigailModern.PortraitPanel")!;
            bool handled = beach.checkAction(new xTile.Dimensions.Location(80, 5), Game1.viewport, farmer);
            var plain = Game1.activeClickableMenu as StardewValley.Menus.DialogueBox;
            bool plainAttached = plain != null && (bool)panel.GetMethod("HasPanel")!.Invoke(null, new object[] { plain })!;
            farmer.friendshipData["Abigail"] = new Friendship(2500);
            farmer.HouseUpgradeLevel = 1;
            Game1.activeClickableMenu = null;
            bool purchaseHandled = beach.checkAction(new xTile.Dimensions.Location(80, 5), Game1.viewport, farmer);
            var question = Game1.activeClickableMenu as StardewValley.Menus.DialogueBox;
            bool purchaseAttached = question != null && question.isQuestion && question.characterDialogue == null
                && question.responses.Select(a => a.responseKey).SequenceEqual(new[] { "Buy", "Not" })
                && (bool)panel.GetMethod("HasPanel")!.Invoke(null, new object[] { question })!;
            if (!handled || !plainAttached || !purchaseHandled || !purchaseAttached) throw new InvalidOperationException($"Real Beach.checkAction failed: handled={handled}, plainAttached={plainAttached}, purchaseHandled={purchaseHandled}, purchaseAttached={purchaseAttached}, eligible={farmer.hasAFriendWithHeartLevel(10, true)}.");
            using var target = new RenderTarget2D(device, Game1.uiViewport.Width, Game1.uiViewport.Height);
            using var batch = new SpriteBatch(device);
            device.SetRenderTarget(target);
            device.Clear(new Microsoft.Xna.Framework.Color(35, 45, 58));
            batch.Begin(samplerState: SamplerState.PointClamp);
            question!.transitioning = false;
            question.transitionInitialized = true;
            question.draw(batch);
            panel.GetMethod("Render")!.Invoke(null, new object[] { batch, question });
            batch.End();
            device.SetRenderTargets(originalTargets);
            using (var stream = File.Create(Path.Combine(helper.DirectoryPath, "mariner-purchase-preview.png"))) target.SaveAsPng(stream, target.Width, target.Height);
            helper.Data.WriteJsonFile("mariner-interaction-checks.json", new { Passed = true, PlainAttached = plainAttached, PurchaseAttached = purchaseAttached, NativeResponsesPreserved = true, FarmLoaded = false });
            Monitor.Log("Real Mariner interaction checks passed; rendered native purchase dialogue and portrait to PNG without loading a farm.", LogLevel.Info);
        }
        catch (Exception ex)
        {
            helper.Data.WriteJsonFile("mariner-interaction-checks.json", new { Passed = false, Error = ex.GetBaseException().ToString() });
            Monitor.Log($"Mariner interaction checks failed: {ex.GetBaseException()}", LogLevel.Warn);
        }
        finally
        {
            device.SetRenderTargets(originalTargets);
            menuField.SetValue(null, originalMenu);
            if (fixtureBeach != null) Game1.locations.Remove(fixtureBeach);
            Game1.currentLocation = originalLocation;
            Game1.dialogueUp = originalDialogueUp;
            Game1.player.CanMove = originalCanMove;
        }
    }
}


















