using System.Reflection;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
namespace NpcArtAudit;
internal static class ItemsAudit
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const string Root = @"C:\Users\david\SDV";
    public static void Export(IModHelper helper, IMonitor monitor)
    {
        if (Context.IsWorldReady || !Game1.IsOnMainThread()) throw new InvalidOperationException("Item contracts require title main thread.");
        helper.Data.WriteJsonFile("items-native-data.json", new { Tools = DataLoader.Tools(Game1.content), Weapons = DataLoader.Weapons(Game1.content), Objects = DataLoader.Objects(Game1.content) });
        monitor.Log("Exported native Tools, Weapons and Objects typed definitions.", LogLevel.Info);
    }
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        if (Context.IsWorldReady || !Game1.IsOnMainThread() || Game1.game1.isDrawing || Game1.uiMode || Game1.uiModeCount != 0)
            throw new InvalidOperationException("Items audit requires title update, no loaded save.");
        var device = Game1.graphics.GraphicsDevice;
        var targets = device.GetRenderTargets(); var viewport = device.Viewport; var scissor = device.ScissorRectangle;
        var blend = device.BlendState; var factor = device.BlendFactor; var depth = device.DepthStencilState;
        var rasterizer = device.RasterizerState; var vertices = GetVertexBuffers(device); var indices = device.Indices; var slots = SaveSlots(device);
        var player = Game1.player; var location = Game1.currentLocation; var random = Game1.random; var menu = Game1.activeClickableMenu;
        var gameViewport = Game1.viewport; var uiViewport = Game1.uiViewport;
        var checks = new Dictionary<string, bool>(); var routes = new List<object>(); var sheets = new List<object>(); var captures = new List<object>();
        string? error = null; var oldBatch = Game1.spriteBatch;
        try
        {
            Game1.random = new Random(200719);
            var farmer = new Farmer();
            typeof(Game1).GetField("_player", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, farmer);
            Game1.currentLocation = new GameLocation(); farmer.currentLocation = Game1.currentLocation;
            Game1.viewport = new xTile.Dimensions.Rectangle(0, 0, 768, 768); Game1.uiViewport = Game1.viewport;
            helper.Data.WriteJsonFile("items-native-data.json", new { Tools = DataLoader.Tools(Game1.content), Weapons = DataLoader.Weapons(Game1.content), Objects = DataLoader.Objects(Game1.content) });
            var dataBefore = JsonSerializer.Serialize(new { Tools = DataLoader.Tools(Game1.content), Weapons = DataLoader.Weapons(Game1.content), Objects = DataLoader.Objects(Game1.content), Recipes = DataLoader.CraftingRecipes(Game1.content) });
            object info = helper.ModRegistry.Get("David.AbigailModern")!;
            object artMod = info.GetType().GetProperty("Mod", Flags)!.GetValue(info)!;
            var artHelper = (IModHelper)artMod.GetType().GetProperty("Helper", Flags)!.GetValue(artMod)!;
            Type assetType = artMod.GetType().GetNestedType("ArtAsset")!;
            var read = artHelper.Data.GetType().GetMethods().First(m => m.Name == "ReadJsonFile" && m.IsGenericMethodDefinition);
            var registry = (Array)read.MakeGenericMethod(assetType.MakeArrayType()).Invoke(artHelper.Data, new object[] { "artwork.json" })!;
            var registered = registry.Cast<object>().ToDictionary(a => ((string)assetType.GetProperty("Name")!.GetValue(a)!).Replace('\\', '/'), StringComparer.OrdinalIgnoreCase);
            using var extracted = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "artifacts/items-modern/extracted.json")));
            var expected = extracted.RootElement.EnumerateArray().ToArray();
            checks["ThirteenNativeAndLocalizedSheets"] = expected.Length == 13;
            using var batch = new SpriteBatch(device); Game1.spriteBatch = batch;
            int index = 0;
            foreach (var row in expected)
            {
                string asset = row.GetProperty("name").GetString()!;
                string copy = "items-native-input/" + index + ".png";
                Directory.CreateDirectory(Path.Combine(helper.DirectoryPath, "items-native-input"));
                File.Copy(Path.Combine(Root, row.GetProperty("file").GetString()!), Path.Combine(helper.DirectoryPath, copy), true);
                var native = helper.ModContent.Load<IRawTextureData>(copy); var actual = helper.GameContent.Load<Texture2D>(asset);
                var pixels = new Color[actual.Width * actual.Height]; actual.GetData(pixels);
                bool size = actual.Width == native.Width && actual.Height == native.Height;
                var protectedPixels = new bool[pixels.Length]; bool priorExact = true;
                if (row.TryGetProperty("previous", out var prior) && prior.ValueKind != JsonValueKind.Null)
                {
                    string priorPath = Path.Combine(Root, "artifacts/items-modern/prior", asset.Replace("/", "--") + ".png");
                    string priorCopy = "items-native-input/prior-" + index + ".png";
                    File.Copy(priorPath, Path.Combine(helper.DirectoryPath, priorCopy), true);
                    var priorTexture = helper.ModContent.Load<IRawTextureData>(priorCopy);
                    foreach (var area in prior.GetProperty("PatchAreas").EnumerateArray())
                    for (int y = area.GetProperty("Y").GetInt32(); y < area.GetProperty("Y").GetInt32() + area.GetProperty("Height").GetInt32(); y++)
                    for (int x = area.GetProperty("X").GetInt32(); x < area.GetProperty("X").GetInt32() + area.GetProperty("Width").GetInt32(); x++)
                    { int at = y * actual.Width + x; protectedPixels[at] = true; priorExact &= pixels[at] == priorTexture.Data[at]; }
                }
                bool alpha = size && pixels.Where((c, i) => !protectedPixels[i]).Select(c => c.A).SequenceEqual(native.Data.Where((c, i) => !protectedPixels[i]).Select(c => c.A));
                int changed = size ? pixels.Where((c, i) => !protectedPixels[i] && c.A > 0 && c != native.Data[i]).Count() : 0;
                bool match = registered.TryGetValue(asset, out var entry);
                if (match) artMod.GetType().GetMethod("CheckAsset", Flags)!.Invoke(artMod, new[] { entry });
                checks[asset] = size && alpha && changed > 0 && priorExact && match;
                sheets.Add(new { Asset = asset, Dimensions = size, NativeAlphaOutsidePrior = alpha, ChangedVisiblePixels = changed, PriorPatchPixelsExact = priorExact, ProtectedPixels = protectedPixels.Count(b => b), RegisteredPixelsMatch = match });
                using var target = new RenderTarget2D(device, actual.Width, actual.Height); device.SetRenderTarget(target); device.Clear(Color.Transparent);
                batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp); batch.Draw(actual, Vector2.Zero, Color.White); batch.End(); device.SetRenderTarget(null);
                var gpu = new Color[pixels.Length]; target.GetData(gpu); checks["GpuAtlas:" + asset] = gpu.SequenceEqual(pixels);
                using var stream = File.Create(Path.Combine(helper.DirectoryPath, $"items-atlas-{index++:D2}.png")); target.SaveAsPng(stream, target.Width, target.Height);
            }
            // Native parsed data defines source rectangles; do not infer semantic item IDs from atlas cells.
            var ids = DataLoader.Tools(Game1.content).Keys.Select(k => "(T)" + k)
                .Concat(DataLoader.Weapons(Game1.content).Keys.Select(k => "(W)" + k))
                .Concat(DataLoader.Objects(Game1.content).Keys.Select(k => "(O)" + k));
            int routeCount = 0; var verifiedTextures = new HashSet<Texture2D>();
            foreach (string id in ids)
            {
                var data = ItemRegistry.GetData(id); if (data == null || data.IsErrorItem) { checks["NativeId:" + id] = false; continue; }
                var texture = data.GetTexture(); var rect = data.GetSourceRect();
                checks["NativeSource:" + id] = rect.Width > 0 && rect.Height > 0 && texture.Bounds.Contains(rect);
                string route = data.TextureName.Replace('\\', '/');
                if (registered.ContainsKey(route) && verifiedTextures.Add(texture))
                {
                    var production = helper.GameContent.Load<Texture2D>(route);
                    bool matches = texture.Width == production.Width && texture.Height == production.Height;
                    if (matches)
                    {
                        var a = new Color[texture.Width * texture.Height]; var b = new Color[a.Length];
                        texture.GetData(a); production.GetData(b); matches = a.SequenceEqual(b);
                    }
                    checks["NativeCachedTexture:" + route] = matches;
                }
                routes.Add(new { Id = id, Asset = route, TextureWidth = texture.Width, TextureHeight = texture.Height, Source = new { rect.X, rect.Y, rect.Width, rect.Height } }); routeCount++;
            }
            checks["NativeRoutesEnumerated"] = routeCount > 500;
            string[] fixtureIds = { "(T)Axe", "(T)Pickaxe", "(T)Hoe", "(T)WateringCan", "(T)BambooPole", "(W)0", "(W)4", "(W)7", "(O)388", "(O)390", "(O)72", "(O)685" };
            foreach (string id in fixtureIds)
            {
                var item = ItemRegistry.Create(id); string identity = item.QualifiedItemId; int stack = item.Stack;
                using var target = new RenderTarget2D(device, 768, 768); var backdrop = new Color(37, 49, 61); device.SetRenderTarget(target); device.Clear(backdrop);
                batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
                item.drawInMenu(batch, new Vector2(64, 64), 2f, 1f, .9f, StackDrawType.Hide, Color.White, false);
                if (item is Tool tool)
                {
                    farmer.Items.Clear(); farmer.Items.Add(tool); farmer.CurrentToolIndex = 0;
                    for (int facing = 0; facing < 4; facing++)
                    {
                        farmer.faceDirection(facing); farmer.Position = new Vector2(160 + facing * 140, 400);
                        farmer.FarmerSprite.currentAnimationIndex = 0; Game1.drawTool(farmer);
                    }
                    tool.drawAttachments(batch, 64, 500);
                }
                batch.End(); device.SetRenderTarget(null); var pixels = new Color[768 * 768]; target.GetData(pixels);
                int icon = 0; for (int y = 64; y < 192; y++) for (int x = 64; x < 192; x++) if (pixels[y * 768 + x] != backdrop) icon++;
                checks["NativeIcon:" + id] = icon > 64;
                if (item is Tool)
                {
                    int held = 0;
                    for (int y = 200; y < 480; y++) for (int x = 100; x < 740; x++) if (pixels[y * 768 + x] != backdrop) held++;
                    checks["NativeHeldToolBody:" + id] = held > 64;
                }
                checks["IdentityAndStackPreserved:" + id] = item.QualifiedItemId == identity && item.Stack == stack;
                using var stream = File.Create(Path.Combine(helper.DirectoryPath, "items-native-" + identity.Replace('(', '_').Replace(')', '_') + ".png")); target.SaveAsPng(stream, 768, 768);
            }
            checks["TypedGameplayDataUnchangedDuringAudit"] = dataBefore == JsonSerializer.Serialize(new { Tools = DataLoader.Tools(Game1.content), Weapons = DataLoader.Weapons(Game1.content), Objects = DataLoader.Objects(Game1.content), Recipes = DataLoader.CraftingRecipes(Game1.content) });
        }
        catch (Exception ex) { error = ex.ToString(); }
        finally
        {
            Game1.spriteBatch = oldBatch;
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
        helper.Data.WriteJsonFile("items-checks.json", new { Passed = passed, Error = error, Checks = checks, Sheets = sheets, Routes = routes,
            Limitations = new[] { "Native source rectangles enumerate Tools/Weapons/Objects; thirteen atlas roundtrips cover auxiliary pixels. Tool action captures select animation index0 and four facings, not all swing/fishing states.", "No damage, harvesting, recipe crafting, attachment use, save persistence or multiplayer tested. Typed data equality proves this audit did not mutate definitions, not that all other mods preserve vanilla stats." } });
        monitor.Log("Items audit " + (passed ? "passed" : "FAILED: " + error), passed ? LogLevel.Info : LogLevel.Error);
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
