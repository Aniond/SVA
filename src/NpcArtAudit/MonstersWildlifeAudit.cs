using System.Reflection;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;
namespace NpcArtAudit;
internal static class MonstersWildlifeAudit
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const string Root = @"C:\Users\david\SDV";
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        if (Context.IsWorldReady || !Game1.IsOnMainThread() || Game1.game1.isDrawing || Game1.uiMode || Game1.uiModeCount != 0)
            throw new InvalidOperationException("Monster fixtures require title update; no save may be loaded.");
        var device = Game1.graphics.GraphicsDevice;
        var targets = device.GetRenderTargets(); var viewport = device.Viewport; var scissor = device.ScissorRectangle;
        var blend = device.BlendState; var factor = device.BlendFactor; var depth = device.DepthStencilState;
        var rasterizer = device.RasterizerState; var vertices = GetVertexBuffers(device); var indices = device.Indices; var slots = SaveSlots(device);
        var player = Game1.player; var location = Game1.currentLocation; var random = Game1.random; var menu = Game1.activeClickableMenu;
        var gameViewport = Game1.viewport; var uiViewport = Game1.uiViewport;
        var checks = new Dictionary<string, bool>(); var routes = new List<object>(); var sheets = new List<object>(); var captures = new List<object>();
        string? error = null;
        var oldBatch = Game1.spriteBatch;
        try
        {
            Game1.random = new Random(731209);
            typeof(Game1).GetField("_player", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, new Farmer());
            Game1.currentLocation = new GameLocation();
            Game1.viewport = new xTile.Dimensions.Rectangle(0, 0, 768, 768); Game1.uiViewport = Game1.viewport;
            var expected = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in Directory.GetFiles(Path.Combine(Root, "artifacts/monsters-modern/native"), "*.png"))
                expected.Add("Characters/Monsters/" + Path.GetFileNameWithoutExtension(path), path);
            checks["All73NativeMonsterSheets"] = expected.Count == 73;
            using var wildlife = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "artifacts/wildlife-modern/inventory.json")));
            foreach (var row in wildlife.RootElement.EnumerateArray().Where(r => !r.TryGetProperty("patch", out _))) expected[row.GetProperty("asset").GetString()!] = Path.Combine(Root, row.GetProperty("native").GetString()!);
            checks["Combined82UniqueSheets"] = expected.Count == 82;
            object info = helper.ModRegistry.Get("David.AbigailModern")!;
            object artMod = info.GetType().GetProperty("Mod", Flags)!.GetValue(info)!;
            var artHelper = (IModHelper)artMod.GetType().GetProperty("Helper", Flags)!.GetValue(artMod)!;
            Type assetType = artMod.GetType().GetNestedType("ArtAsset")!;
            var read = artHelper.Data.GetType().GetMethods().First(m => m.Name == "ReadJsonFile" && m.IsGenericMethodDefinition);
            var registry = (Array)read.MakeGenericMethod(assetType.MakeArrayType()).Invoke(artHelper.Data, new object[] { "artwork.json" })!;
            var registered = registry.Cast<object>().ToDictionary(a => ((string)assetType.GetProperty("Name")!.GetValue(a)!).Replace('\\', '/'), StringComparer.OrdinalIgnoreCase);
            using var batch = new SpriteBatch(device); Game1.spriteBatch = batch;
            int sheetIndex = 0;
            foreach (var pair in expected.OrderBy(p => p.Key))
            {
                string copied = "monster-native-input/" + sheetIndex + ".png";
                Directory.CreateDirectory(Path.Combine(helper.DirectoryPath, "monster-native-input"));
                File.Copy(pair.Value, Path.Combine(helper.DirectoryPath, copied), true);
                var native = helper.ModContent.Load<IRawTextureData>(copied);
                var texture = helper.GameContent.Load<Texture2D>(pair.Key);
                var pixels = new Color[texture.Width * texture.Height]; texture.GetData(pixels);
                bool size = texture.Width == native.Width && texture.Height == native.Height;
                bool alpha = size && pixels.Select(c => c.A).SequenceEqual(native.Data.Select(c => c.A));
                int changed = size ? pixels.Where((c, i) => c.A > 0 && c != native.Data[i]).Count() : 0;
                bool registeredMatch = registered.TryGetValue(pair.Key, out var entry);
                if (registeredMatch) artMod.GetType().GetMethod("CheckAsset", Flags)!.Invoke(artMod, new[] { entry });
                bool retainedExisting = pair.Key.Equals("LooseSprites/parrots", StringComparison.OrdinalIgnoreCase);
                bool retainedHashMatches = false;
                if (retainedExisting && registeredMatch)
                {
                    using var baseline = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "artifacts/monsters-modern/baseline-source-art.json")));
                    string packagedFile = (string)assetType.GetProperty("File")!.GetValue(entry)!;
                    string baselineFile = "src/AbigailModern/" + packagedFile.Replace('\\', '/');
                    var original = baseline.RootElement.GetProperty("files").EnumerateArray().Single(r => r.GetProperty("file").GetString() == baselineFile);
                    string expectedHash = original.GetProperty("sha256").GetString()!;
                    using var installedStream = File.OpenRead(Path.Combine(artHelper.DirectoryPath, packagedFile));
                    using var hasher = System.Security.Cryptography.SHA256.Create();
                    string actualHash = Convert.ToHexString(hasher.ComputeHash(installedStream));
                    retainedHashMatches = actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
                    checks["RetainedParrotsMatchesPreBatchPackagedHash"] = retainedHashMatches;
                }
                checks[pair.Key] = size && registeredMatch && (retainedExisting ? retainedHashMatches : alpha && changed > 0);
                sheets.Add(new { Asset = pair.Key, Dimensions = size, ExactAlpha = alpha, ChangedVisiblePixels = changed, RegisteredPixelsMatch = registeredMatch,
                    RetainedExisting = retainedExisting, RetainedBaselineHashMatches = retainedHashMatches,
                    PreservationContract = retainedExisting ? "Pre-batch packaged file SHA256 and production CheckAsset; native alpha difference predates this batch" : "Native alpha exact" });
                // Entire atlas, without labels in the measured region. This is intentionally distinct from native entity coverage below.
                using var target = new RenderTarget2D(device, texture.Width, texture.Height);
                device.SetRenderTarget(target); device.Clear(Color.Transparent);
                batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp);
                batch.Draw(texture, Vector2.Zero, Color.White); batch.End(); device.SetRenderTarget(null);
                var gpu = new Color[pixels.Length]; target.GetData(gpu);
                checks["GpuAtlas:" + pair.Key] = gpu.SequenceEqual(pixels);
                string file = $"monster-wildlife-atlas-{sheetIndex++:D2}.png";
                using var output = File.Create(Path.Combine(helper.DirectoryPath, file)); target.SaveAsPng(output, target.Width, target.Height);
            }
            foreach (var row in wildlife.RootElement.EnumerateArray().Where(r => r.TryGetProperty("patch", out _)))
            {
                string asset = row.GetProperty("asset").GetString()!; var area = row.GetProperty("patch");
                var rect = new Rectangle(area.GetProperty("X").GetInt32(), area.GetProperty("Y").GetInt32(), area.GetProperty("Width").GetInt32(), area.GetProperty("Height").GetInt32());
                string copy = "monster-native-input/emily-parrot.png";
                File.Copy(Path.Combine(Root, row.GetProperty("native").GetString()!), Path.Combine(helper.DirectoryPath, copy), true);
                var native = helper.ModContent.Load<IRawTextureData>(copy); var texture = helper.GameContent.Load<Texture2D>(asset);
                var pixels = new Color[rect.Width * rect.Height]; texture.GetData(0, rect, pixels, 0, pixels.Length);
                bool match = registered.TryGetValue(asset, out var entry);
                if (match) artMod.GetType().GetMethod("CheckAsset", Flags)!.Invoke(artMod, new[] { entry });
                checks["EmilyParrotPatch"] = match && native.Width == rect.Width && native.Height == rect.Height
                    && pixels.Select(c => c.A).SequenceEqual(native.Data.Select(c => c.A)) && pixels.Where((c, i) => c.A > 0 && c != native.Data[i]).Any();
                bool gap = true;
                for (int y = 0; y < rect.Height; y++) for (int x = 39; x <= 40; x++) gap &= pixels[y * rect.Width + x] == native.Data[y * rect.Width + x];
                checks["EmilyParrotNativeGapPreserved"] = gap;
            }
            object CheckNativeTexture(AnimatedSprite sprite, string caseId)
            {
                // Read the actual native texture first, including any local appearance override.
                // No LoadTexture call here: replacing it would hide a stale native cache.
                var actual = sprite.Texture;
                string requested = (sprite.textureName.Value ?? "").Replace('\\', '/');
                string loaded = (sprite.loadedTexture ?? requested).Replace('\\', '/');
                bool routeRegistered = registered.ContainsKey(loaded);
                bool equal = false;
                if (routeRegistered)
                {
                    var production = helper.GameContent.Load<Texture2D>(loaded);
                    if (actual.Width == production.Width && actual.Height == production.Height)
                    {
                        var actualPixels = new Color[actual.Width * actual.Height];
                        var productionPixels = new Color[production.Width * production.Height];
                        actual.GetData(actualPixels); production.GetData(productionPixels);
                        equal = actualPixels.SequenceEqual(productionPixels);
                    }
                }
                checks["NativeTextureRegistered:" + caseId] = routeRegistered;
                checks["NativeTextureRgbaMatches:" + caseId] = equal;
                return new { RequestedAsset = requested, LoadedAsset = loaded, Registered = routeRegistered, ExactRgbaMatch = equal };
            }
            Func<Monster>[] constructors = {
                () => new GreenSlime(Vector2.Zero, Color.ForestGreen), () => new GreenSlime(Vector2.Zero, Color.CornflowerBlue),
                () => new BigSlime(Vector2.Zero, 40), () => new Grub(Vector2.Zero), () => new Grub(Vector2.Zero, true),
                () => new Bat(Vector2.Zero), () => new Ghost(Vector2.Zero), () => new Skeleton(Vector2.Zero),
                () => new Mummy(Vector2.Zero), () => new DustSpirit(Vector2.Zero), () => new MetalHead("Metal Head", Vector2.Zero),
                () => new DinoMonster(Vector2.Zero), () => new RockCrab(Vector2.Zero), () => new BlueSquid(Vector2.Zero)
            };
            for (int i = 0; i < constructors.Length; i++)
            {
                var monster = constructors[i](); monster.currentLocation = Game1.currentLocation; monster.Position = new Vector2(352, 384);
                var bounds = monster.GetBoundingBox(); var health = monster.Health;
                var nativeTexture = CheckNativeTexture(monster.Sprite, "Monster:" + i);
                Rectangle frame = monster.Sprite.SourceRect;
                checks["NativeFrame:" + i] = frame.Width > 0 && frame.Height > 0 && monster.Sprite.Texture.Bounds.Contains(frame);
                using var target = new RenderTarget2D(device, 768, 768); var backdrop = new Color(37, 49, 61);
                device.SetRenderTarget(target); device.Clear(backdrop);
                batch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp);
                monster.draw(batch); monster.drawAboveAllLayers(batch); monster.drawAboveAlwaysFrontLayer(batch);
                batch.End(); device.SetRenderTarget(null);
                var pixels = new Color[768 * 768]; target.GetData(pixels);
                // Ordinary bats are intentionally dark. Their white-tinted native pass must match
                // opaque atlas colors, excluding black shadow pixels, not a brightness cutoff.
                var framePixels = new Color[frame.Width * frame.Height];
                monster.Sprite.Texture.GetData(0, frame, framePixels, 0, framePixels.Length);
                var opaqueBody = framePixels.Where(c => c.A == 255 && c != Color.Black && c != backdrop).ToHashSet();
                int colored = pixels.Count(c => c != backdrop && (monster is Bat ? opaqueBody.Contains(c) : Math.Max(c.R, Math.Max(c.G, c.B)) > 75));
                checks["NativeVisibleBody:" + i] = colored > 64;
                checks["DrawPreservesCombatGeometry:" + i] = bounds == monster.GetBoundingBox() && health == monster.Health;
                string file = $"monster-native-entity-{i:D2}.png";
                using (var output = File.Create(Path.Combine(helper.DirectoryPath, file))) target.SaveAsPng(output, 768, 768);
                routes.Add(new { Type = monster.GetType().Name, monster.Name, NativeTexture = nativeTexture, Width = monster.Sprite.SpriteWidth, Height = monster.Sprite.SpriteHeight, Frame = new { frame.X, frame.Y, frame.Width, frame.Height }, ColoredBodyPixels = colored, File = file });
            }
            var critters = new StardewValley.BellsAndWhistles.Critter[] {
                new StardewValley.BellsAndWhistles.Crow(5, 6),
                new StardewValley.BellsAndWhistles.Frog(new Vector2(5, 6)),
                new StardewValley.BellsAndWhistles.Frog(new Vector2(5, 6), true)
            };
            for (int i = 0; i < critters.Length; i++)
            {
                var critter = critters[i]; var frame = critter.sprite.SourceRect;
                var nativeTexture = CheckNativeTexture(critter.sprite, "Critter:" + i);
                var source = new Color[frame.Width * frame.Height]; critter.sprite.Texture.GetData(0, frame, source, 0, source.Length);
                var palette = source.Where(c => c.A == 255).ToHashSet();
                using var target = new RenderTarget2D(device, 768, 768); var backdrop = new Color(37, 49, 61);
                device.SetRenderTarget(target); device.Clear(backdrop);
                batch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp);
                critter.draw(batch); critter.drawAboveFrontLayer(batch); batch.End(); device.SetRenderTarget(null);
                var pixels = new Color[768 * 768]; target.GetData(pixels);
                int body = pixels.Count(c => c != backdrop && palette.Contains(c));
                checks["NativeWildlifeBody:" + i] = body > 64;
                string file = $"wildlife-native-entity-{i:D2}.png";
                using (var output = File.Create(Path.Combine(helper.DirectoryPath, file))) target.SaveAsPng(output, 768, 768);
                routes.Add(new { Type = critter.GetType().Name, NativeTexture = nativeTexture, Frame = critter.sprite.currentFrame, BodyPixels = body, File = file });
            }
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
        helper.Data.WriteJsonFile("monsters-wildlife-checks.json", new { Passed = passed, Error = error, Checks = checks, Sheets = sheets, NativeEntities = routes,
            Limitations = new[] { "Whole-sheet GPU checks cover all 82 routes; fourteen native monster fixtures cover representative ground, airborne, tinted and large bodies; three native wildlife fixtures cover crow and land/water frogs.", "No combat, AI, animation transitions, wildlife behavior transitions, spawning, multiplayer or save persistence is simulated. Cat/Crow/Frog/Fireball native live routes remain unconfirmed. Full alpha and dimensions protect every atlas cell including debris; this is not a claim that every animation was played." } });
        monitor.Log("Monsters/wildlife audit " + (passed ? "passed" : "FAILED: " + error), passed ? LogLevel.Info : LogLevel.Error);
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
