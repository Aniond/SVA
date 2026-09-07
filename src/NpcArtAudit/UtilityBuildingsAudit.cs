using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Objects;

namespace NpcArtAudit;
internal static class UtilityBuildingsAudit
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const string Root = @"C:\Users\david\SDV\artifacts\utility-buildings-modern";
    internal static readonly string[] Names = { "Stable", "Silo", "Mill", "Well", "Fish Pond", "Shipping Bin", "Slime Hutch", "Junimo Hut", "Gold Clock", "Desert Obelisk", "Earth Obelisk", "Island Obelisk", "Water Obelisk", "Mailbox", "Pet Bowl", "Hay Pet Bowl", "Stone Pet Bowl" };
    public static void Export(IModHelper helper, IMonitor monitor)
    {
        if (Context.IsWorldReady || !Game1.IsOnMainThread()) throw new InvalidOperationException("Utility building contracts require title update.");
        var wanted = Names.Select(n => "Buildings/" + n).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var definitions = DataLoader.Buildings(Game1.content).Where(p => Names.Contains(p.Key) || wanted.Contains((p.Value.Texture ?? "Buildings/" + p.Key).Replace('\\', '/'))
            || (p.Value.Skins?.Any(s => wanted.Contains((s.Texture ?? "").Replace('\\', '/'))) ?? false)
            || (p.Value.DrawLayers?.Any(l => wanted.Contains((l.Texture ?? "").Replace('\\', '/'))) ?? false)).ToDictionary(p => p.Key, p => p.Value);
        helper.Data.WriteJsonFile("utility-building-contracts.json", new { Definitions = definitions, PaintData = DataLoader.PaintData(Game1.content),
            Textures = Names.Select(n => { var t = helper.GameContent.Load<Texture2D>("Buildings/" + n); return new { Asset = "Buildings/" + n, t.Width, t.Height }; }).ToArray(),
            Scope = "Actual typed native building data, relevant skins and paint data at title. No save loaded or selected." });
        monitor.Log("Exported utility building contracts for17 textures.", LogLevel.Info);
    }
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        if (Context.IsWorldReady || !Game1.IsOnMainThread() || Game1.game1.isDrawing || Game1.uiMode || Game1.uiModeCount != 0)
            throw new InvalidOperationException("Utility buildings audit requires title update outside drawing/UI mode.");
        Export(helper, monitor);
        var device = Game1.graphics.GraphicsDevice;
        var targets = device.GetRenderTargets(); var viewport = device.Viewport; var scissor = device.ScissorRectangle;
        var blend = device.BlendState; var factor = device.BlendFactor; var depth = device.DepthStencilState;
        var rasterizer = device.RasterizerState; var vertices = GetVertexBuffers(device); var indices = device.Indices; var slots = SaveSlots(device);
        var player = Game1.player; var location = Game1.currentLocation; var random = Game1.random; var menu = Game1.activeClickableMenu;
        var gameViewport = Game1.viewport; var uiViewport = Game1.uiViewport; var season = Game1.season; int time = Game1.timeOfDay;
        var gameTime = Game1.currentGameTime; var locations = Game1.game1._locations; var lookup = Game1._locationLookup;
        bool clocksOff = Game1.netWorldState.Value.goldenClocksTurnedOff.Value;
        var paintCache = BuildingPainter.paintMaskLookup.ToArray();
        var checks = new Dictionary<string, bool>(); var sheets = new List<object>(); var cases = new List<object>(); var buildings = new List<Building>();
        string? error = null;
        try
        {
            Game1.random = new Random(420717);
            typeof(Game1).GetField("_player", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, new Farmer());
            var detached = new GameLocation(); helper.Reflection.GetField<Netcode.NetString>(detached, "name").GetValue().Value = "UtilityBuildingAudit";
            detached.IsOutdoors = true; detached.waterColor.Value = new Color(65, 130, 160);
            Game1.currentLocation = detached; Game1.game1._locations = new List<GameLocation> { detached };
            Game1._locationLookup = new Dictionary<string, GameLocation>(StringComparer.OrdinalIgnoreCase) { [detached.Name] = detached };
            Game1.viewport = new xTile.Dimensions.Rectangle(0, 0, 1024, 1024); Game1.uiViewport = Game1.viewport;
            Game1.netWorldState.Value.goldenClocksTurnedOff.Value = false;
            object info = helper.ModRegistry.Get("David.AbigailModern")!;
            object artMod = info.GetType().GetProperty("Mod", Flags)!.GetValue(info)!;
            var artHelper = (IModHelper)artMod.GetType().GetProperty("Helper", Flags)!.GetValue(artMod)!;
            Type assetType = artMod.GetType().GetNestedType("ArtAsset")!;
            var read = artHelper.Data.GetType().GetMethods().First(m => m.Name == "ReadJsonFile" && m.IsGenericMethodDefinition);
            var registry = (Array)read.MakeGenericMethod(assetType.MakeArrayType()).Invoke(artHelper.Data, new object[] { "artwork.json" })!;
            var registered = registry.Cast<object>().ToDictionary(a => ((string)Member(a, "Name")!).Replace('\\', '/'), StringComparer.OrdinalIgnoreCase);
            checks["Registry606"] = registry.Length == 606;
            foreach (string name in Names)
            {
                string asset = "Buildings/" + name;
                Texture2D texture = helper.GameContent.Load<Texture2D>(asset);
                string nativeFile = new[] { "utility", "magic", "small" }.SelectMany(dir => new[] { Path.Combine(Root, dir, "native", name + ".png"), Path.Combine(Root, dir, name + "-native.png") }).First(File.Exists);
                // Match production's raw PNG decoder: FromStream changes partial-alpha RGB.
                string baselinePath = "utility-native/" + name + ".png";
                Directory.CreateDirectory(Path.Combine(helper.DirectoryPath, "utility-native"));
                File.Copy(nativeFile, Path.Combine(helper.DirectoryPath, baselinePath), true);
                var native = helper.ModContent.Load<IRawTextureData>(baselinePath);
                var colors = new Color[texture.Width * texture.Height]; texture.GetData(colors);
                var original = native.Data;
                bool dimensions = texture.Width == native.Width && texture.Height == native.Height;
                bool alpha = dimensions && colors.Select(c => c.A).SequenceEqual(original.Select(c => c.A));
                int changed = dimensions ? colors.Where((c, i) => c.A > 0 && c != original[i]).Count() : 0;
                bool exactProtected = true;
                foreach (Rectangle rect in Protected(name))
                {
                    exactProtected &= texture.Bounds.Contains(rect);
                    if (texture.Bounds.Contains(rect))
                        for (int y = rect.Top; y < rect.Bottom; y++) for (int x = rect.Left; x < rect.Right; x++) exactProtected &= colors[y * texture.Width + x] == original[y * native.Width + x];
                }
                bool installed = registered.TryGetValue(asset, out var record);
                if (installed) artMod.GetType().GetMethod("CheckAsset", Flags)!.Invoke(artMod, new[] { record });
                checks[asset] = dimensions && alpha && changed > 10 && exactProtected && installed;
                sheets.Add(new { Asset = asset, Dimensions = dimensions, AlphaExact = alpha, ChangedVisiblePixels = changed, ProtectedRgbaExact = exactProtected, RegisteredAndPixelMatched = installed });
            }
            using var batch = new SpriteBatch(device);
            var covered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in DataLoader.Buildings(Game1.content))
            {
                var data = pair.Value;
                var styles = new List<(string? Skin, string Asset)> { (null, data.Texture ?? "Buildings/" + pair.Key) };
                if (data.Skins != null) styles.AddRange(data.Skins.Select(s => ((string?)s.Id, s.Texture ?? data.Texture ?? "Buildings/" + pair.Key)));
                foreach (var style in styles)
                {
                    string asset = style.Asset.Replace('\\', '/');
                    bool scopedBody = Names.Any(n => asset.Equals("Buildings/" + n, StringComparison.OrdinalIgnoreCase));
                    var scopedLayers = data.DrawLayers?.Where(l => Names.Any(n => (l.Texture ?? "").Replace('\\', '/').Equals("Buildings/" + n, StringComparison.OrdinalIgnoreCase))).ToArray();
                    if (!scopedBody && (style.Skin != null || scopedLayers?.Length is not > 0)) continue;
                    if (scopedBody) covered.Add(asset);
                    var building = Building.CreateInstanceFromId(pair.Key, new Vector2(2, 8)); buildings.Add(building);
                    building.skinId.Value = style.Skin; building.parentLocationName.Value = detached.Name;
                    building.daysOfConstructionLeft.Value = 0;
                    if (data.Chests != null)
                        foreach (var chestData in data.Chests)
                        {
                            var chest = new Chest(true) { Name = chestData.Id }; chest.Items.Add(ItemRegistry.Create("(O)24")); building.buildingChests.Add(chest);
                        }
                    string stem = string.Concat((pair.Key + "-" + (style.Skin ?? "default")).Select(c => char.IsLetterOrDigit(c) ? c : '-'));
                    int serial = 0;
                    Color[] Render(string state, int clock = 1200, int milliseconds = 0)
                    {
                        Game1.timeOfDay = clock; Game1.currentGameTime = new GameTime(TimeSpan.FromMilliseconds(milliseconds), TimeSpan.FromMilliseconds(16));
                        using var target = new RenderTarget2D(device, 1024, 1024, false, SurfaceFormat.Color, DepthFormat.None);
                        device.SetRenderTarget(target); Color backdrop = new(65, 83, 58); device.Clear(backdrop);
                        Rectangle source = building.getSourceRect();
                        checks[stem + "-source-" + state] = building.texture.Value.Bounds.Contains(source);
                        if (data.DrawLayers != null)
                            foreach (var layer in data.DrawLayers)
                            {
                                Texture2D layerTexture = layer.Texture == null ? building.texture.Value : helper.GameContent.Load<Texture2D>(layer.Texture);
                                int count = Convert.ToInt32(Member(layer, "FrameCount") ?? 1), duration = Math.Max(1, Convert.ToInt32(Member(layer, "FrameDuration") ?? 100));
                                if (count < 1 || count > 256) throw new InvalidDataException("Unbounded draw layer frame count.");
                                for (int frame = 0; frame < count; frame++)
                                    checks[$"{stem}-layer-{data.DrawLayers.IndexOf(layer)}-season{(int)Game1.season}-frame{frame}"] = layerTexture.Bounds.Contains(building.ApplySourceRectOffsets(layer.GetSourceRect(frame * duration)));
                            }
                        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
                        building.drawBackground(batch); building.draw(batch); batch.End(); device.SetRenderTarget(null);
                        var pixels = new Color[1024 * 1024]; target.GetData(pixels);
                        int visible = pixels.Count(c => c != backdrop);
                        var body = new Color[source.Width * source.Height]; building.texture.Value.GetData(0, source, body, 0, body.Length);
                        var palette = body.Where(c => c.A == 255 && c != backdrop).ToHashSet();
                        int bodyPixels = pixels.Count(palette.Contains);
                        checks[stem + "-native-draw-" + state] = visible > 200 && bodyPixels > 100;
                        if (scopedLayers != null)
                            foreach (var layer in scopedLayers)
                            {
                                string layerAsset = layer.Texture!.Replace('\\', '/');
                                var layerTexture = helper.GameContent.Load<Texture2D>(layerAsset);
                                Rectangle layerSource = building.ApplySourceRectOffsets(layer.GetSourceRect(milliseconds));
                                var layerColors = new Color[layerSource.Width * layerSource.Height];
                                layerTexture.GetData(0, layerSource, layerColors, 0, layerColors.Length);
                                Vector2 position = new Vector2(building.tileX.Value * 64, (building.tileY.Value + building.tilesHigh.Value) * 64)
                                    + (layer.DrawPosition - new Vector2(0, source.Height)) * 4;
                                int matched = 0;
                                for (int y = 0; y < layerSource.Height; y++) for (int x = 0; x < layerSource.Width; x++)
                                {
                                    Color expected = layerColors[y * layerSource.Width + x];
                                    int px = (int)position.X + x * 4 + 1, py = (int)position.Y + y * 4 + 1;
                                    if (expected.A == 255 && px >= 0 && px < 1024 && py >= 0 && py < 1024 && pixels[py * 1024 + px] == expected) matched++;
                                }
                                checks[stem + "-external-layer-" + layerAsset + "-" + state] = matched > 30;
                                if (matched > 30) covered.Add(layerAsset);
                            }
                        string file = $"utility-{stem}-{serial++:D2}-{state}.png";
                        using (var output = File.Create(Path.Combine(helper.DirectoryPath, file))) target.SaveAsPng(output, 1024, 1024);
                        cases.Add(new { Definition = pair.Key, style.Skin, Asset = asset, NativeType = building.GetType().FullName, State = state, Season = Game1.season.ToString(), Clock = clock, Milliseconds = milliseconds, Source = source, VisiblePixels = visible, BodyPalettePixels = bodyPixels, File = file });
                        return pixels;
                    }
                    for (int s = 0; s < 4; s++) { Game1.season = (Season)s; Render("season-" + s); }
                    Game1.season = Season.Spring;
                    var baseline = Render("baseline");
                    if (data.DrawLayers?.Any(layer => Convert.ToInt32(Member(layer, "FrameCount") ?? 1) > 1) == true)
                    {
                        var animatedLayer = data.DrawLayers.First(layer => Convert.ToInt32(Member(layer, "FrameCount") ?? 1) > 1);
                        var animated = Render("animation", milliseconds: Math.Max(1, Convert.ToInt32(Member(animatedLayer, "FrameDuration") ?? 100)));
                        checks[stem + "-animation-visible"] = !baseline.SequenceEqual(animated);
                    }
                    if (building is PetBowl bowl)
                    {
                        Rectangle wet = bowl.getSourceRect(); wet.X += wet.Width;
                        checks[stem + "-water-source-bounds"] = bowl.texture.Value.Bounds.Contains(wet);
                        bowl.watered.Value = true;
                        var wetPixels = Render("watered"); checks[stem + "-water-visible"] = !baseline.SequenceEqual(wetPixels);
                    }
                    if (building is FishPond pond)
                    {
                        for (int n = 1; n <= 3; n++) { pond.nettingStyle.Value = n; Render("net-" + n); }
                        pond.overrideWaterColor.Value = new Color(160, 60, 65); Render("colored-water");
                    }
                    if (building is ShippingBin bin)
                    {
                        var lid = (TemporaryAnimatedSprite)Member(bin, "shippingBinLid")!;
                        // Native lid's13-frame strip belongs to Cursors, not the replaced body sheet.
                        var last = lid.sourceRect; last.X += 12 * last.Width; lid.sourceRect = last;
                        checks[stem + "-lid-strip-bounds"] = Game1.mouseCursors.Bounds.Contains(last);
                        Render("lid-open");
                    }
                    if (building is JunimoHut hut)
                    {
                        hut.wasLit.Value = true; hut.raisinDays.Value = 2;
                        Render("lit-bag-raisins", clock: 2100);
                    }
                    if (pair.Key == "Gold Clock")
                    {
                        Render("hands-evening", clock: 1830);
                        Game1.netWorldState.Value.goldenClocksTurnedOff.Value = true; Render("disabled-clock");
                        Game1.netWorldState.Value.goldenClocksTurnedOff.Value = false;
                    }
                    if (!scopedBody) continue;
                    var paint = new BuildingPaintColor();
                    paint.Color1Default.Value = paint.Color2Default.Value = paint.Color3Default.Value = false;
                    paint.Color1Hue.Value = 210; paint.Color2Hue.Value = 30; paint.Color3Hue.Value = 120;
                    paint.Color1Saturation.Value = paint.Color2Saturation.Value = paint.Color3Saturation.Value = 65;
                    using var painted = BuildingPainter.Apply(helper.GameContent.Load<Texture2D>(asset), asset + "_PaintMask", paint);
                    if (painted != null)
                    {
                        var before = new Color[building.texture.Value.Width * building.texture.Value.Height]; building.texture.Value.GetData(before);
                        var after = new Color[painted.Width * painted.Height]; painted.GetData(after);
                        checks[stem + "-native-paint-alpha"] = before.Select(c => c.A).SequenceEqual(after.Select(c => c.A));
                        checks[stem + "-native-paint-visible"] = !before.SequenceEqual(after);
                        using var output = File.Create(Path.Combine(helper.DirectoryPath, "utility-" + stem + "-native-paint.png")); painted.SaveAsPng(output, painted.Width, painted.Height);
                    }
                }
            }
            checks["All17NativeTextureRoutes"] = Names.All(n => covered.Contains("Buildings/" + n));
        }
        catch (Exception ex) { error = ex.ToString(); }
        finally
        {
            foreach (var building in buildings) building.paintedTexture?.Dispose();
            BuildingPainter.paintMaskLookup.Clear(); foreach (var pair in paintCache) BuildingPainter.paintMaskLookup[pair.Key] = pair.Value;
            typeof(Game1).GetField("_player", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, player);
            Game1.currentLocation = location; Game1.random = random; Game1.activeClickableMenu = menu; Game1.viewport = gameViewport; Game1.uiViewport = uiViewport;
            Game1.season = season; Game1.timeOfDay = time; Game1.currentGameTime = gameTime; Game1.game1._locations = locations; Game1._locationLookup = lookup;
            Game1.netWorldState.Value.goldenClocksTurnedOff.Value = clocksOff;
            device.SetRenderTargets(targets); device.Viewport = viewport; device.ScissorRectangle = scissor; device.BlendState = blend; device.BlendFactor = factor;
            device.DepthStencilState = depth; device.RasterizerState = rasterizer; device.SetVertexBuffers(vertices); device.Indices = indices; foreach (var slot in slots) slot.Restore();
            checks["GameAndGraphicsRestored"] = ReferenceEquals(Game1.player, player) && ReferenceEquals(Game1.currentLocation, location) && ReferenceEquals(Game1.random, random)
                && ReferenceEquals(Game1.activeClickableMenu, menu) && Game1.viewport.Equals(gameViewport) && Game1.uiViewport.Equals(uiViewport)
                && Game1.season == season && Game1.timeOfDay == time && ReferenceEquals(Game1.currentGameTime, gameTime) && ReferenceEquals(Game1.game1._locations, locations)
                && ReferenceEquals(Game1._locationLookup, lookup) && Game1.netWorldState.Value.goldenClocksTurnedOff.Value == clocksOff
                && BuildingPainter.paintMaskLookup.Count == paintCache.Length && paintCache.All(p => ReferenceEquals(BuildingPainter.paintMaskLookup[p.Key], p.Value))
                && device.GetRenderTargets().SequenceEqual(targets) && device.Viewport.Equals(viewport) && device.ScissorRectangle == scissor && ReferenceEquals(device.BlendState, blend)
                && device.BlendFactor == factor && ReferenceEquals(device.DepthStencilState, depth) && ReferenceEquals(device.RasterizerState, rasterizer)
                && ReferenceEquals(device.Indices, indices) && GetVertexBuffers(device).SequenceEqual(vertices) && slots.All(s => s.Matches());
        }
        bool passed = error == null && sheets.Count == 17 && checks.Values.All(v => v);
        helper.Data.WriteJsonFile("utility-buildings-checks.json", new { Passed = passed, Error = error, Checks = checks, Sheets = sheets, Cases = cases,
            Scope = "Actual native building factories, data-defined/seasonal layers and Draw methods; isolated named GameLocation and temporary player at title.17 exact-alpha sheets, source bounds, native paint, bowl water, pond nets/water, clock hands/disabled overlay, shipping lid and Junimo overlay fixtures. No farm or save loaded.",
            Limitations = "No building placement, farm interaction, indoor pathfinding, production processing, shipping contents, NPC/pet assignment or save persistence. Native animation source positions and manually selected visual states are drawn without gameplay simulation. Previous589 assets and source paint-mask hashes are independently preserved by root." });
        monitor.Log("Utility buildings GPU audit " + (passed ? "passed." : "FAILED: " + error), passed ? LogLevel.Info : LogLevel.Error);
    }
    private static object? Member(object value, string name) => value.GetType().GetProperty(name, Flags)?.GetValue(value) ?? value.GetType().GetField(name, Flags)?.GetValue(value);
    private static Rectangle[] Protected(string name) => name switch
    {
        "Fish Pond" => new[] { new Rectangle(0, 80, 80, 96) },
        "Junimo Hut" => new[] { new Rectangle(192, 0, 64, 64) },
        "Gold Clock" => new[] { new Rectangle(7, 20, 34, 37) },
        "Desert Obelisk" or "Earth Obelisk" or "Island Obelisk" or "Water Obelisk" => new[] { new Rectangle(0, 0, 48, 32) },
        _ => Array.Empty<Rectangle>()
    };
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
