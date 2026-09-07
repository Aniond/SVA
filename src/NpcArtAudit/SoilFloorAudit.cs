using System.Reflection;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;
namespace NpcArtAudit;
internal static class SoilFloorAudit
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const string Root = @"C:\Users\david\SDV";
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        if (Context.IsWorldReady || !Game1.IsOnMainThread() || Game1.game1.isDrawing || Game1.uiMode || Game1.uiModeCount != 0)
            throw new InvalidOperationException("Soil/floor audit requires title update.");
        var device = Game1.graphics.GraphicsDevice;
        var targets = device.GetRenderTargets(); var viewport = device.Viewport; var scissor = device.ScissorRectangle;
        var blend = device.BlendState; var factor = device.BlendFactor; var depth = device.DepthStencilState;
        var rasterizer = device.RasterizerState; var vertices = GetVertexBuffers(device); var indices = device.Indices; var slots = SaveSlots(device);
        var player = Game1.player; var location = Game1.currentLocation; var random = Game1.random; var menu = Game1.activeClickableMenu;
        var gameViewport = Game1.viewport; var uiViewport = Game1.uiViewport;
        var checks = new Dictionary<string, bool>(); var routes = new List<object>(); var sheets = new List<object>(); var captures = new List<object>();
        string? error = null; var oldBatch = Game1.spriteBatch; string season = Game1.currentSeason;
        var light = HoeDirt.lightTexture; var dark = HoeDirt.darkTexture; var snow = HoeDirt.snowTexture;
        var soilGuide = HoeDirt.drawGuide; var floorGuide = Flooring.drawGuide; var floorGuideList = Flooring.drawGuideList;
        try
        {
            Game1.random = new Random(420019);
            typeof(Game1).GetField("_player", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, new Farmer());
            Game1.currentLocation = new GameLocation(); Game1.currentLocation.name.Value = "Farm";
            Game1.viewport = new xTile.Dimensions.Rectangle(0, 0, 1024, 1024); Game1.uiViewport = Game1.viewport;
            var definitions = DataLoader.FloorsAndPaths(Game1.content); string before = JsonSerializer.Serialize(definitions);
            helper.Data.WriteJsonFile("soil-floor-native-data.json", definitions);
            object info = helper.ModRegistry.Get("David.AbigailModern")!;
            object artMod = info.GetType().GetProperty("Mod", Flags)!.GetValue(info)!;
            var artHelper = (IModHelper)artMod.GetType().GetProperty("Helper", Flags)!.GetValue(artMod)!;
            Type assetType = artMod.GetType().GetNestedType("ArtAsset")!;
            var read = artHelper.Data.GetType().GetMethods().First(m => m.Name == "ReadJsonFile" && m.IsGenericMethodDefinition);
            var registry = (Array)read.MakeGenericMethod(assetType.MakeArrayType()).Invoke(artHelper.Data, new object[] { "artwork.json" })!;
            var registered = registry.Cast<object>().ToDictionary(a => ((string)assetType.GetProperty("Name")!.GetValue(a)!).Replace('\\', '/'), StringComparer.OrdinalIgnoreCase);
            using var input = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "artifacts/soil-floor-modern/extracted.json")));
            var rows = input.RootElement.EnumerateArray().ToArray(); checks["FourAtlasContracts"] = rows.Length == 4;
            int index = 0;
            foreach (var row in rows)
            {
                string asset = row.GetProperty("name").GetString()!; string copy = "soil-native-input/" + index++ + ".png";
                Directory.CreateDirectory(Path.Combine(helper.DirectoryPath, "soil-native-input"));
                File.Copy(Path.Combine(Root, row.GetProperty("file").GetString()!), Path.Combine(helper.DirectoryPath, copy), true);
                var native = helper.ModContent.Load<IRawTextureData>(copy); var actual = helper.GameContent.Load<Texture2D>(asset);
                var pixels = new Color[actual.Width * actual.Height]; actual.GetData(pixels);
                bool size = native.Width == actual.Width && native.Height == actual.Height;
                bool alpha = size && pixels.Select(c => c.A).SequenceEqual(native.Data.Select(c => c.A));
                int changed = size ? pixels.Where((c, i) => c.A > 0 && c != native.Data[i]).Count() : 0;
                bool match = registered.TryGetValue(asset, out var entry); if (match) artMod.GetType().GetMethod("CheckAsset", Flags)!.Invoke(artMod, new[] { entry });
                checks[asset] = size && alpha && changed > 0 && match;
                sheets.Add(new { Asset = asset, Dimensions = size, NativeAlphaExact = alpha, ChangedVisiblePixels = changed, RegisteredPixelsMatch = match });
            }
            var additional = DataLoader.AdditionalWallpaperFlooring(Game1.content);
            helper.Data.WriteJsonFile("additional-wallpaper-flooring-data.json", additional.Select(d => new { d.Id, d.Texture, d.Count, d.IsFlooring }).ToArray());
            using var interiorDoc = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "artifacts/soil-floor-modern/interior/install-manifest.json")));
            using var interiorContracts = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "artifacts/soil-floor-modern/interior-floor-contracts.json")));
            var interiorRows = interiorDoc.RootElement.EnumerateArray().ToArray(); checks["SixTotalSoilFloorRoutes"] = rows.Length + interiorRows.Length == 6;
            foreach (var row in interiorRows)
            {
                string asset = row.GetProperty("asset").GetString()!, key = asset.Replace("/", "--");
                var contract = interiorContracts.RootElement.GetProperty("rows").EnumerateArray().Single(c => c.GetProperty("asset").GetString() == asset);
                string copy = "soil-native-input/" + key + "-baseline.png";
                File.Copy(Path.Combine(Root, "artifacts/soil-floor-modern/interior/native", key + "-baseline.png"), Path.Combine(helper.DirectoryPath, copy), true);
                var baseline = helper.ModContent.Load<IRawTextureData>(copy); var texture = helper.GameContent.Load<Texture2D>(asset);
                var pixels = new Color[texture.Width * texture.Height]; texture.GetData(pixels);
                bool size = texture.Width == baseline.Width && texture.Height == baseline.Height; if (!size) throw new InvalidDataException(asset + " dimensions");
                var allowed = new bool[pixels.Length]; var priorMask = new bool[pixels.Length];
                void Mark(JsonElement areas, bool[] mask)
                {
                    foreach (var a in areas.EnumerateArray())
                    for (int y = a.GetProperty("Y").GetInt32(); y < a.GetProperty("Y").GetInt32() + a.GetProperty("Height").GetInt32(); y++)
                    for (int x = a.GetProperty("X").GetInt32(); x < a.GetProperty("X").GetInt32() + a.GetProperty("Width").GetInt32(); x++) mask[y * texture.Width + x] = true;
                }
                Mark(contract.GetProperty("eligibleAreas"), allowed); Mark(contract.GetProperty("priorAreas"), priorMask);
                bool preserved = true, alpha = true, priorExact = true; int changed = 0;
                for (int i = 0; i < pixels.Length; i++)
                {
                    alpha &= pixels[i].A == baseline.Data[i].A;
                    if (priorMask[i]) priorExact &= pixels[i] == baseline.Data[i];
                    if (!allowed[i] || baseline.Data[i].A == 0) preserved &= pixels[i] == baseline.Data[i];
                    if (pixels[i] != baseline.Data[i]) changed++;
                }
                bool match = registered.TryGetValue(asset, out var entry); if (match) artMod.GetType().GetMethod("CheckAsset", Flags)!.Invoke(artMod, new[] { entry });
                checks[asset] = alpha && preserved && priorExact && changed > 0 && match;
                sheets.Add(new { Asset = asset, AlphaMatchesBaseline = alpha, OutsideEligibleExact = preserved, PriorRegionsExact = priorExact, PriorAreas = contract.GetProperty("priorAreas").GetArrayLength(), ChangedPixels = changed, ProductionMatches = match });
            }
            using var batch = new SpriteBatch(device); Game1.spriteBatch = batch;
            void Render(string name, Action<int> draw)
            {
                using var target = new RenderTarget2D(device, 1024, 1024); var backdrop = new Color(37, 49, 61);
                device.SetRenderTarget(target); device.Clear(backdrop);
                batch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp);
                for (int mask = 0; mask < 256; mask++) draw(mask);
                batch.End(); device.SetRenderTarget(null); var pixels = new Color[1024 * 1024]; target.GetData(pixels);
                bool allVisible = true;
                for (int mask = 0; mask < 256; mask++)
                {
                    int body = 0; for (int y = mask / 16 * 64; y < mask / 16 * 64 + 64; y++) for (int x = mask % 16 * 64; x < mask % 16 * 64 + 64; x++)
                        if (pixels[y * 1024 + x] != backdrop && Math.Max(pixels[y * 1024 + x].R, Math.Max(pixels[y * 1024 + x].G, pixels[y * 1024 + x].B)) > 65) body++;
                    allVisible &= body > 32;
                }
                checks["NativeVisible256:" + name] = allVisible;
                using var output = File.Create(Path.Combine(helper.DirectoryPath, "soil-floor-" + name + ".png")); target.SaveAsPng(output, 1024, 1024);
            }
            var checkedSoilRoutes = new HashSet<string>();
            foreach (string route in new[] { "light", "dark", "snow" })
            foreach (int state in new[] { 0, 1, 3 })
            {
                Game1.currentSeason = route == "snow" ? "winter" : "spring";
                Game1.currentLocation.name.Value = route == "dark" ? "Mountain" : "Farm";
                Render(route + "-state" + state, mask => {
                    var dirt = new HoeDirt(state == 0 ? 0 : 1, Game1.currentLocation) { Tile = new Vector2(mask % 16, mask / 16) };
                    dirt.nearWaterForPaddy.Value = state == 3 ? 1 : 0;
                    int baseFrame = HoeDirt.drawGuide[(byte)(mask & 15)], wetFrame = HoeDirt.drawGuide[(byte)(mask >> 4)];
                    typeof(HoeDirt).GetField("sourceRectPosition", Flags)!.SetValue(dirt, baseFrame);
                    typeof(HoeDirt).GetField("wateredRectPosition", Flags)!.SetValue(dirt, wetFrame);
                    bool passable = dirt.isPassable(); dirt.DrawOptimized(batch, null, null);
                    var texture = (Texture2D)typeof(HoeDirt).GetField("texture", Flags)!.GetValue(dirt)!;
                    var expectedTexture = helper.GameContent.Load<Texture2D>("TerrainFeatures/hoeDirt" + (route == "dark" ? "Dark" : route == "snow" ? "Snow" : ""));
                    if (checkedSoilRoutes.Add(route))
                    {
                        bool equal = texture.Width == expectedTexture.Width && texture.Height == expectedTexture.Height;
                        if (equal) { var a = new Color[texture.Width * texture.Height]; var b = new Color[a.Length]; texture.GetData(a); expectedTexture.GetData(b); equal = a.SequenceEqual(b); }
                        checks["SoilRoute:" + route] = equal;
                    }
                    checks["SoilBounds:" + route + state + ":" + mask] = texture.Bounds.Contains(new Rectangle(baseFrame % 4 * 16, baseFrame / 4 * 16, 16, 16)) && texture.Bounds.Contains(new Rectangle(wetFrame % 4 * 16 + (state == 3 ? 128 : 64), wetFrame / 4 * 16, 16, 16));
                    checks["SoilState:" + route + state + ":" + mask] = dirt.state.Value == (state == 0 ? 0 : 1) && dirt.crop == null && dirt.isPassable() == passable;
                });
            }
            foreach (var pair in definitions)
            foreach (string currentSeason in new[] { "spring", "winter" })
            {
                Game1.currentSeason = currentSeason;
                var floor = new Flooring(pair.Key) { Location = Game1.currentLocation };
                var texture = floor.GetTexture(); var corner = floor.GetTextureCorner();
                string route = floor.ShouldDrawWinterVersion() ? pair.Value.WinterTexture : pair.Value.Texture;
                routes.Add(new { FloorId = pair.Key, Season = currentSeason, Asset = route, CornerX = corner.X, CornerY = corner.Y, AtlasInBatch = route.Replace('\\', '/').Equals("TileSheets/Floors", StringComparison.OrdinalIgnoreCase) });
                Render("floor-" + pair.Key + "-" + currentSeason, mask => {
                    floor.Tile = new Vector2(mask % 16, mask / 16); floor.whichView.Value = mask % 16;
                    typeof(Flooring).GetField("neighborMask", Flags)!.SetValue(floor, (byte)mask);
                    int frame = pair.Value.ConnectType == StardewValley.GameData.FloorsAndPaths.FloorPathConnectType.Random ? Flooring.drawGuideList[mask % 16] : Flooring.drawGuide[(byte)(mask & 15)];
                    checks["FloorBounds:" + pair.Key + currentSeason + mask] = texture.Bounds.Contains(new Rectangle(corner.X + frame * 16 % 256, corner.Y + frame / 16 * 16, 16, 16));
                    floor.draw(batch); checks["FloorPassable:" + pair.Key] = floor.isPassable();
                });
            }
            var flooringIds = ItemRegistry.RequireTypeDefinition("(FL)").GetAllIds().ToArray();
            checks["All88LegacyFloorIds"] = Enumerable.Range(0, 88).All(i => flooringIds.Contains(i.ToString()));
            for (int first = 0; first < flooringIds.Length; first += 48)
            {
                using var target = new RenderTarget2D(device, 1024, 768); device.SetRenderTarget(target); device.Clear(new Color(37,49,61));
                batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
                for (int i = first; i < Math.Min(first + 48, flooringIds.Length); i++)
                {
                    string id = "(FL)" + flooringIds[i]; var item = ItemRegistry.Create<StardewValley.Objects.Wallpaper>(id);
                    var data = ItemRegistry.GetData(id)!; var texture = data.GetTexture(); var menuRect = item.sourceRect.Value;
                    var pattern = new Rectangle(menuRect.X, menuRect.Y, 32, 32);
                    checks["IndoorFloorPatternBounds:" + id] = texture.Bounds.Contains(pattern) && menuRect.Width == 28 && menuRect.Height == 26 && item.isFloor.Value;
                    int slot = i - first; var point = new Vector2(slot % 8 * 128, slot / 8 * 128);
                    batch.Draw(texture, point, pattern, Color.White, 0, Vector2.Zero, 2, SpriteEffects.None, .1f);
                    item.drawInMenu(batch, point + new Vector2(64, 48), .75f, 1, .5f, StackDrawType.Hide, Color.White, false);
                    routes.Add(new { QualifiedId = id, Asset = data.TextureName, PlacedPattern = new { pattern.X, pattern.Y, pattern.Width, pattern.Height }, MenuWidth = menuRect.Width, MenuHeight = menuRect.Height });
                }
                batch.End(); device.SetRenderTarget(null);
                using var output = File.Create(Path.Combine(helper.DirectoryPath, "soil-floor-indoor-patterns-" + first + ".png")); target.SaveAsPng(output, 1024, 768);
            }
            checks["FloorDefinitionsUnchanged"] = before == JsonSerializer.Serialize(DataLoader.FloorsAndPaths(Game1.content));
        }
        catch (Exception ex) { error = ex.ToString(); }
        finally
        {
            Game1.spriteBatch = oldBatch; Game1.currentSeason = season;
            HoeDirt.lightTexture = light; HoeDirt.darkTexture = dark; HoeDirt.snowTexture = snow;
            HoeDirt.drawGuide = soilGuide; Flooring.drawGuide = floorGuide; Flooring.drawGuideList = floorGuideList;
            typeof(Game1).GetField("_player", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, player);
            Game1.currentLocation = location; Game1.random = random; Game1.activeClickableMenu = menu; Game1.viewport = gameViewport; Game1.uiViewport = uiViewport;
            device.SetRenderTargets(targets); device.Viewport = viewport; device.ScissorRectangle = scissor; device.BlendState = blend; device.BlendFactor = factor;
            device.DepthStencilState = depth; device.RasterizerState = rasterizer; device.SetVertexBuffers(vertices); device.Indices = indices; foreach (var slot in slots) slot.Restore();
            checks["GameAndGraphicsRestored"] = ReferenceEquals(Game1.player, player) && ReferenceEquals(Game1.currentLocation, location) && ReferenceEquals(Game1.random, random)
                && ReferenceEquals(Game1.activeClickableMenu, menu) && Game1.viewport.Equals(gameViewport) && Game1.uiViewport.Equals(uiViewport)
                && device.GetRenderTargets().SequenceEqual(targets) && device.Viewport.Equals(viewport) && device.ScissorRectangle == scissor && ReferenceEquals(device.BlendState, blend)
                && device.BlendFactor == factor && ReferenceEquals(device.DepthStencilState, depth) && ReferenceEquals(device.RasterizerState, rasterizer)
                && ReferenceEquals(device.Indices, indices) && GetVertexBuffers(device).SequenceEqual(vertices) && slots.All(s => s.Matches());
        }
        bool passed = error == null && checks.Values.All(v => v);
        helper.Data.WriteJsonFile("soil-floor-checks.json", new { Passed = passed, Error = error, Checks = checks, Sheets = sheets, Routes = routes,
            Limitations = "Direct native draw fixtures select all stored adjacency masks and dry/wet/paddy overlay combinations; no neighbor discovery, watering, crop growth, seasons ticking, collision actions or saves are simulated. TileSheets/Floors is only claimed as live terrain if exported typed routes confirm it; otherwise atlas validation only." });
        monitor.Log("Soil/floor audit " + (passed ? "passed" : "FAILED: " + error), passed ? LogLevel.Info : LogLevel.Error);
    }
    private static VertexBufferBinding[] GetVertexBuffers(GraphicsDevice device)
    {
        var bindings = typeof(GraphicsDevice).GetField("_vertexBuffers", Flags)!.GetValue(device)!;
        return (VertexBufferBinding[])bindings.GetType().GetMethod("Get", Flags, null, Type.EmptyTypes, null)!.Invoke(bindings, null)!;
    }
    private sealed record Slot(Action Restore, Func<bool> Matches);
    private static List<Slot> SaveSlots(GraphicsDevice device)
    {
        var slots = new List<Slot>();
        foreach (string name in new[] { "Textures", "SamplerStates", "VertexTextures", "VertexSamplerStates" })
        {
            var collection = typeof(GraphicsDevice).GetProperty(name)!.GetValue(device)!; var indexer = collection.GetType().GetProperty("Item")!;
            for (int i = 0; i < 32; i++)
            {
                object[] index = { i }; object? value;
                try { value = indexer.GetValue(collection, index); }
                catch (TargetInvocationException ex) when (ex.InnerException is IndexOutOfRangeException or ArgumentOutOfRangeException) { break; }
                slots.Add(new Slot(() => indexer.SetValue(collection, value, index), () => ReferenceEquals(indexer.GetValue(collection, index), value)));
            }
        }
        return slots;
    }
}
