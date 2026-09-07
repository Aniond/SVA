using System.Reflection;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Minigames;
namespace NpcArtAudit;
internal static class ScenesAudit
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const string Root = @"C:\Users\david\SDV";
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        if (Context.IsWorldReady || !Game1.IsOnMainThread() || Game1.game1.isDrawing || Game1.uiMode || Game1.uiModeCount != 0)
            throw new InvalidOperationException("Scenes audit requires title update.");
        var device = Game1.graphics.GraphicsDevice;
        var targets = device.GetRenderTargets(); var viewport = device.Viewport; var scissor = device.ScissorRectangle;
        var blend = device.BlendState; var factor = device.BlendFactor; var depth = device.DepthStencilState;
        var rasterizer = device.RasterizerState; var vertices = GetVertexBuffers(device); var indices = device.Indices; var slots = SaveSlots(device);
        var player = Game1.player; var location = Game1.currentLocation; var random = Game1.random; var menu = Game1.activeClickableMenu;
        var gameViewport = Game1.viewport; var uiViewport = Game1.uiViewport;
        var checks = new Dictionary<string, bool>(); var routes = new List<object>(); var sheets = new List<object>(); var captures = new List<object>();
        string? error = null; var oldBatch = Game1.spriteBatch; bool gamepad = Game1.options.gamepadControls;
        try
        {
            Game1.random = new Random(210019); Game1.options.gamepadControls = false;
            typeof(Game1).GetField("_player", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, new Farmer());
            Game1.currentLocation = new GameLocation();
            Game1.viewport = new xTile.Dimensions.Rectangle(0, 0, 1024, 768); Game1.uiViewport = Game1.viewport;
            object info = helper.ModRegistry.Get("David.AbigailModern")!;
            object artMod = info.GetType().GetProperty("Mod", Flags)!.GetValue(info)!;
            var artHelper = (IModHelper)artMod.GetType().GetProperty("Helper", Flags)!.GetValue(artMod)!;
            Type assetType = artMod.GetType().GetNestedType("ArtAsset")!;
            var read = artHelper.Data.GetType().GetMethods().First(m => m.Name == "ReadJsonFile" && m.IsGenericMethodDefinition);
            var registry = (Array)read.MakeGenericMethod(assetType.MakeArrayType()).Invoke(artHelper.Data, new object[] { "artwork.json" })!;
            var registered = registry.Cast<object>().ToDictionary(a => ((string)assetType.GetProperty("Name")!.GetValue(a)!).Replace('\\', '/'), StringComparer.OrdinalIgnoreCase);
            string configured = Path.Combine(helper.DirectoryPath, "scenes-manifest.txt");
            string manifest = File.Exists(configured) ? File.ReadAllText(configured).Trim() : "artifacts/scenes-modern/install-manifest.json";
            using var manifestDoc = JsonDocument.Parse(File.ReadAllText(Path.GetFullPath(Path.Combine(Root, manifest))));
            JsonElement list = manifestDoc.RootElement;
            if (list.ValueKind != JsonValueKind.Array) list = list.TryGetProperty("assets", out var assets) ? assets : list.GetProperty("entries");
            string GetName(JsonElement row) => (row.TryGetProperty("Name", out var name) || row.TryGetProperty("name", out name) || row.TryGetProperty("asset", out name)) ? name.GetString()! : throw new InvalidDataException("Manifest row needs Name/name/asset");
            var names = list.EnumerateArray().Select(GetName).ToArray(); checks["ManifestNonemptyUnique"] = names.Length > 0 && names.Distinct().Count() == names.Length;
            using var contractDoc = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "artifacts/scenes-modern/contracts.json")));
            using var inventoryDoc = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "artifacts/scenes-modern/inventory.json")));
            var contracts = contractDoc.RootElement.GetProperty("rows").EnumerateArray().ToDictionary(r => r.GetProperty("asset").GetString()!);
            var inventory = inventoryDoc.RootElement.GetProperty("rows").EnumerateArray().ToDictionary(r => r.GetProperty("asset").GetString()!);
            using var sharedDoc = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "artifacts/scenes-modern/shared-contracts.json")));
            contracts[sharedDoc.RootElement.GetProperty("asset").GetString()!] = sharedDoc.RootElement;
            using var sharedLocalesDoc = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "artifacts/scenes-modern/shared-locales/install-manifest.json")));
            var sharedLocales = sharedLocalesDoc.RootElement.EnumerateArray().ToDictionary(r => r.GetProperty("asset").GetString()!);
            int sequence = 0;
            IRawTextureData Read(string path)
            {
                string copy = "scene-native-input/" + sequence++ + ".png"; Directory.CreateDirectory(Path.Combine(helper.DirectoryPath, "scene-native-input"));
                File.Copy(path, Path.Combine(helper.DirectoryPath, copy), true); return helper.ModContent.Load<IRawTextureData>(copy);
            }
            using var nativeContent = new Microsoft.Xna.Framework.Content.ContentManager(Game1.content.ServiceProvider,
                Path.Combine(Path.GetDirectoryName(typeof(Game1).Assembly.Location)!, "Content"));
            using var batch = new SpriteBatch(device); Game1.spriteBatch = batch;
            foreach (string asset in names)
            {
                bool sharedLocale = sharedLocales.TryGetValue(asset, out var localeManifest);
                var contract = sharedLocale ? sharedDoc.RootElement : contracts[asset]; var row = sharedLocale ? localeManifest : inventory[asset]; string key = asset.Replace("/", "--");
                var native = Read(sharedLocale ? Path.Combine(Root, localeManifest.GetProperty("native").GetString()!) : Path.Combine(Root, "artifacts/scenes-modern/native", key + ".png"));
                var actual = helper.GameContent.Load<Texture2D>(asset); var pixels = new Color[actual.Width * actual.Height]; actual.GetData(pixels);
                // Base MonoGame ContentManager reads the exact XNB name, without SMAPI's
                // LocalizedContentManager/GameContent asset-edit pipeline or locale fallback.
                var nativeXnb = nativeContent.Load<Texture2D>(asset);
                var nativeXnbPixels = new Color[nativeXnb.Width * nativeXnb.Height]; nativeXnb.GetData(nativeXnbPixels);
                bool dimensions = actual.Width == native.Width && actual.Height == native.Height && nativeXnb.Width == actual.Width && nativeXnb.Height == actual.Height; if (!dimensions) throw new InvalidDataException("Dimensions: " + asset);
                var eligible = new bool[pixels.Length]; var prior = new bool[pixels.Length];
                void Mask(JsonElement areas, bool[] destination, bool value)
                {
                    foreach (var area in areas.EnumerateArray())
                    {
                        int x = area.GetProperty("X").GetInt32(), y = area.GetProperty("Y").GetInt32(), w = area.GetProperty("Width").GetInt32(), h = area.GetProperty("Height").GetInt32();
                        if (!actual.Bounds.Contains(new Rectangle(x, y, w, h))) throw new InvalidDataException("Contract bounds: " + asset);
                        for (int py = y; py < y + h; py++) for (int px = x; px < x + w; px++) destination[py * actual.Width + px] = value;
                    }
                }
                Mask(contract.GetProperty("eligibleAreas"), eligible, true); Mask(contract.GetProperty("preserveAreas"), eligible, false); Mask(row.GetProperty("priorAreas"), prior, true);
                if (sharedLocale)
                {
                    var newScope = new bool[pixels.Length]; Mask(localeManifest.GetProperty("newPatchAreas"), newScope, true);
                    for (int i = 0; i < eligible.Length; i++) eligible[i] &= newScope[i] && !prior[i];
                }
                IRawTextureData? previous = sharedLocale ? Read(Path.Combine(Root, localeManifest.GetProperty("prior").GetString()!)) : prior.Any(b => b) ? Read(Path.Combine(Root, "artifacts/scenes-modern/prior", key + ".png")) : null;
                IRawTextureData? baseNative = sharedLocale ? Read(Path.Combine(Root, "artifacts/scenes-modern/native/LooseSprites--Cursors.png")) : row.GetProperty("localized").GetBoolean() ? Read(Path.Combine(Root, "artifacts/scenes-modern/native", row.GetProperty("baseAsset").GetString()!.Replace("/", "--") + ".png")) : null;
                // Compare authentic raw XNB outside patches and supplied raw PNG inside patches.
                // No PNG export or assumed alpha conversion participates in this reference.
                var patched = new bool[pixels.Length];
                var registration = registered[asset];
                // Rectangle coordinates are fields, omitted by default System.Text.Json.
                // Consume the production typed Areas directly; this also handles singular PatchArea.
                var registeredAreas = (IEnumerable<Rectangle>)assetType.GetProperty("Areas")!.GetValue(registration)!;
                foreach (Rectangle area in registeredAreas)
                {
                    if (area.Width <= 0 || area.Height <= 0 || !actual.Bounds.Contains(area)) throw new InvalidDataException("Registry mask bounds: " + asset);
                    for (int y = area.Y; y < area.Bottom; y++) for (int x = area.X; x < area.Right; x++) patched[y * actual.Width + x] = true;
                }
                Color NativeRuntime(int i)
                {
                    Color c = native.Data[i];
                    return patched[i] ? c : nativeXnbPixels[i];
                }
                bool alpha = true, preserved = true, locale = true, priorExact = true, hidden = true; int changed = 0;
                var rawMismatches = new List<object>(); int excludedMismatchCount = 0, localeMismatchCount = 0;
                int[] Rgba(Color c) => new[] { (int)c.R, c.G, c.B, c.A };
                void Evidence(int i, Color expected, bool localeFailure)
                {
                    if (localeFailure) localeMismatchCount++; else excludedMismatchCount++;
                    if (rawMismatches.Count < 24) rawMismatches.Add(new { X = i % actual.Width, Y = i / actual.Width,
                        Actual = Rgba(pixels[i]), Expected = Rgba(expected), NativePngRaw = Rgba(native.Data[i]), NativeXnbRaw = Rgba(nativeXnbPixels[i]),
                        NativePremultiplied = Rgba(Color.FromNonPremultiplied(native.Data[i].R, native.Data[i].G, native.Data[i].B, native.Data[i].A)),
                        PriorPngRaw = previous == null ? null : Rgba(previous.Data[i]), Patched = patched[i], Prior = prior[i], Eligible = eligible[i], LocaleFailure = localeFailure });
                }
                for (int i = 0; i < pixels.Length; i++)
                {
                    if (sharedLocale)
                    {
                        Color original = prior[i] ? previous!.Data[i] : NativeRuntime(i);
                        alpha &= pixels[i].A == original.A;
                        if (original.A == 0) hidden &= pixels[i] == original;
                        if (prior[i]) priorExact &= pixels[i] == original;
                        bool localizedDifference = baseNative!.Width != native.Width || baseNative.Height != native.Height || baseNative.Data[i] != native.Data[i];
                        if (!eligible[i] || localizedDifference) { preserved &= pixels[i] == original; if (pixels[i] != original) Evidence(i, original, false); }
                        if (localizedDifference) { locale &= pixels[i] == original; if (pixels[i] != original) Evidence(i, original, true); }
                        if (eligible[i]) { alpha &= pixels[i].A == native.Data[i].A; if (pixels[i].A > 0 && pixels[i] != original) changed++; }
                        continue;
                    }
                    if (prior[i]) { priorExact &= pixels[i] == previous!.Data[i]; continue; }
                    alpha &= pixels[i].A == native.Data[i].A;
                    if (native.Data[i].A == 0) hidden &= pixels[i] == NativeRuntime(i);
                    if (!eligible[i]) { preserved &= pixels[i] == NativeRuntime(i); if (pixels[i] != NativeRuntime(i)) Evidence(i, NativeRuntime(i), false); }
                    if (baseNative != null && (baseNative.Width != native.Width || baseNative.Height != native.Height || baseNative.Data[i] != native.Data[i])) locale &= pixels[i] == NativeRuntime(i);
                    if (eligible[i] && pixels[i].A > 0 && pixels[i] != native.Data[i]) changed++;
                }
                bool registeredMatch = registered.TryGetValue(asset, out var entry); if (registeredMatch) artMod.GetType().GetMethod("CheckAsset", Flags)!.Invoke(artMod, new[] { entry });
                checks[asset] = registeredMatch && alpha && hidden && preserved && locale && priorExact && changed > 0;
                sheets.Add(new { Asset = asset, ExactAlpha = alpha, HiddenRgbExact = hidden, ExcludedRegionsExact = preserved, LocalizedDifferencesExact = locale, PriorPixelsExact = priorExact, ChangedEligiblePixels = changed, ProductionMatches = registeredMatch, RawExcludedMismatchCount = excludedMismatchCount, RawLocaleMismatchCount = localeMismatchCount, RawMismatchSamples = rawMismatches });
                using var target = new RenderTarget2D(device, actual.Width, actual.Height); device.SetRenderTarget(target); device.Clear(Color.Transparent);
                batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp); batch.Draw(actual, Vector2.Zero, Color.White); batch.End(); device.SetRenderTarget(null);
                var gpu = new Color[pixels.Length]; target.GetData(gpu); checks["GpuAtlas:" + asset] = gpu.SequenceEqual(pixels);
                using var stream = File.Create(Path.Combine(helper.DirectoryPath, "scene-atlas-" + key + ".png")); target.SaveAsPng(stream, target.Width, target.Height);
            }
            var sharedTexture = helper.GameContent.Load<Texture2D>(sharedDoc.RootElement.GetProperty("asset").GetString()!);
            foreach (var call in sharedDoc.RootElement.GetProperty("calls").EnumerateArray())
            {
                bool bounded = call.GetProperty("rects").EnumerateArray().All(r => sharedTexture.Bounds.Contains(new Rectangle(r.GetProperty("X").GetInt32(), r.GetProperty("Y").GetInt32(), r.GetProperty("Width").GetInt32(), r.GetProperty("Height").GetInt32())));
                checks["NativeSharedFrames:" + call.GetProperty("family").GetString() + ":" + call.GetProperty("line").GetInt32()] = bounded;
            }
            // The alternate native Intro constructor does not acquire/play audio. Its omitted
            // tree-strip dependency is supplied explicitly; original drawRoadArea still renders.
            using (var target = new RenderTarget2D(device, 1024, 768))
            {
                device.SetRenderTarget(target); var intro = new Intro(2);
                typeof(Intro).GetField("treeStripTexture", Flags)!.SetValue(intro, Game1.content.Load<Texture2D>("Minigames/treestrip"));
                intro.draw(batch); device.SetRenderTarget(null);
                var pixels = new Color[1024 * 768]; target.GetData(pixels);
                var texture = Game1.content.Load<Texture2D>("Minigames/Intro"); var source = new Color[texture.Width * texture.Height]; texture.GetData(source);
                var palette = source.Where(c => c.A == 255 && c != Color.Black && c != new Color(130,130,130) && c != new Color(102,181,255)).ToHashSet();
                checks["NativeIntroSceneVisible"] = pixels.Count(palette.Contains) > 10000;
                using var stream = File.Create(Path.Combine(helper.DirectoryPath, "scene-native-intro.png")); target.SaveAsPng(stream, 1024, 768);
            }
            // Native Darts constructor only selects Aiming; gamepad is off to prevent mouse repositioning.
            // No tick, input, state transition, unload or reward method is invoked.
            using (var target = new RenderTarget2D(device, 1024, 768))
            {
                device.SetRenderTarget(target); device.Clear(Color.Black); var darts = new Darts(20);
                var texture = (Texture2D)typeof(Darts).GetField("texture", Flags)!.GetValue(darts)!;
                var expected = helper.GameContent.Load<Texture2D>("Minigames/Darts"); var a = new Color[texture.Width * texture.Height]; var b = new Color[expected.Width * expected.Height]; texture.GetData(a); expected.GetData(b);
                checks["NativeDartsTextureMatches"] = a.SequenceEqual(b);
                darts.draw(batch); device.SetRenderTarget(null); var pixels = new Color[1024 * 768]; target.GetData(pixels);
                var palette = a.Where(c => c.A == 255 && c != Color.Black).ToHashSet(); checks["NativeDartsBodyVisible"] = pixels.Count(palette.Contains) > 10000;
                checks["DartsSourceBankBounds"] = new[] { new Rectangle(0,0,320,320), new Rectangle(0,320,64,64), new Rectangle(0,384,48,32), new Rectangle(64,384,16,32) }.All(texture.Bounds.Contains);
                using var stream = File.Create(Path.Combine(helper.DirectoryPath, "scene-native-darts.png")); target.SaveAsPng(stream, 1024, 768);
            }
        }
        catch (Exception ex) { error = ex.ToString(); }
        finally
        {
            Game1.spriteBatch = oldBatch; Game1.options.gamepadControls = gamepad;
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
        helper.Data.WriteJsonFile("scenes-checks.json", new { Passed = passed, Error = error, Checks = checks, Sheets = sheets,
            Limitations = "All manifest atlases and finite exclusion masks GPU-validated; Darts and alternate-constructor Intro have actual native draw fixtures. No state transitions, score/reward, controller input or gameplay simulated. BoatJourney constructor changes fade/music; Intro default starts audio; other scene native fixtures are not claimed. Protected glyph/number pixels remain exact under contract, but readability across every display scale is not measured." });
        monitor.Log("Scenes audit " + (passed ? "passed" : "FAILED: " + error), passed ? LogLevel.Info : LogLevel.Error);
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
