using System.Reflection;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;

namespace NpcArtAudit;

internal static class AnimalsAudit
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const string Root = @"C:\Users\david\SDV\artifacts\farm-animals";
    public static void Export(IModHelper helper, IMonitor monitor)
    {
        if (Context.IsWorldReady || !Game1.IsOnMainThread()) throw new InvalidOperationException("Animal contracts require a title-screen main-thread fixture.");
        var animals = DataLoader.FarmAnimals(Game1.content);
        var pets = DataLoader.Pets(Game1.content);
        helper.Data.WriteJsonFile("animal-contracts.json", new { FarmAnimals = animals, Pets = pets,
            Scope = "Actual typed DataLoader.FarmAnimals and DataLoader.Pets from installed game content at title. No save loaded or selected." });
        monitor.Log($"Exported native animal contracts: {animals.Count} farm animal definitions and {pets.Count} pet types.", LogLevel.Info);
    }

    private sealed record Fixture(string Label, string Asset, Character Character, int Frame, Point Offset, bool Swimming = false);
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        if (Context.IsWorldReady || !Game1.IsOnMainThread() || Game1.game1.isDrawing || Game1.uiMode || Game1.uiModeCount != 0)
            throw new InvalidOperationException("Animal audit requires title-screen update outside drawing/UI mode.");
        Export(helper, monitor);
        var device = Game1.graphics.GraphicsDevice;
        var targets = device.GetRenderTargets(); var viewport = device.Viewport; var scissor = device.ScissorRectangle;
        var blend = device.BlendState; var factor = device.BlendFactor; var depth = device.DepthStencilState;
        var rasterizer = device.RasterizerState; var vertices = GetVertexBuffers(device); var indices = device.Indices; var slots = SaveSlots(device);
        var player = Game1.player; var location = Game1.currentLocation; var random = Game1.random; var menu = Game1.activeClickableMenu;
        var gameViewport = Game1.viewport; var uiViewport = Game1.uiViewport;
        var checks = new Dictionary<string, bool>(); var routes = new List<object>(); var sheets = new List<object>(); var captures = new List<object>();
        string? error = null;
        try
        {
            Game1.random = new Random(190713);
            typeof(Game1).GetField("_player", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, new Farmer());
            Game1.currentLocation = new GameLocation();
            Game1.viewport = new xTile.Dimensions.Rectangle(0, 0, 1024, 768); Game1.uiViewport = Game1.viewport;
            using var inventory = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "inventory.json")));
            var expected = inventory.RootElement.GetProperty("rows").EnumerateArray().Where(r => !r.GetProperty("asset").GetString()!.Equals("Animals/Error", StringComparison.OrdinalIgnoreCase)).ToArray();
            var expectedNames = expected.Select(r => r.GetProperty("asset").GetString()!).ToHashSet(StringComparer.OrdinalIgnoreCase);
            checks["Expected42CreatureSheets"] = expected.Length == 42;
            object info = helper.ModRegistry.Get("David.AbigailModern")!;
            object artMod = info.GetType().GetProperty("Mod", Flags)!.GetValue(info)!;
            var artHelper = (IModHelper)artMod.GetType().GetProperty("Helper", Flags)!.GetValue(artMod)!;
            Type assetType = artMod.GetType().GetNestedType("ArtAsset")!;
            var read = artHelper.Data.GetType().GetMethods().First(m => m.Name == "ReadJsonFile" && m.IsGenericMethodDefinition);
            var registry = (Array)read.MakeGenericMethod(assetType.MakeArrayType()).Invoke(artHelper.Data, new object[] { "artwork.json" })!;
            object? Property(object value, string name) => value.GetType().GetProperty(name, Flags)?.GetValue(value) ?? value.GetType().GetField(name, Flags)?.GetValue(value);
            var registered = registry.Cast<object>().ToDictionary(a => ((string)Property(a, "Name")!).Replace('\\', '/'), StringComparer.OrdinalIgnoreCase);
            checks["RegistryAtLeast589Entries"] = registry.Length >= 589;
            checks["All42Registered"] = expectedNames.All(registered.ContainsKey);
            checks["TechnicalErrorNotReplaced"] = !registered.ContainsKey("Animals/Error");
            foreach (var row in expected)
            {
                string asset = row.GetProperty("asset").GetString()!;
                var texture = helper.GameContent.Load<Texture2D>(asset);
                var colors = new Color[texture.Width * texture.Height]; texture.GetData(colors);
                using var source = File.OpenRead(Path.Combine(Root, "native", asset[8..] + ".png"));
                using var native = Texture2D.FromStream(device, source);
                var original = new Color[native.Width * native.Height]; native.GetData(original);
                bool dimensions = texture.Width == row.GetProperty("width").GetInt32() && texture.Height == row.GetProperty("height").GetInt32()
                    && texture.Width == native.Width && texture.Height == native.Height;
                bool alpha = dimensions && colors.Select(c => c.A).SequenceEqual(original.Select(c => c.A));
                int changed = 0;
                int frameWidth = row.GetProperty("frameWidth").GetInt32(), frameHeight = row.GetProperty("frameHeight").GetInt32();
                bool cells = dimensions, technicalCellsUnchanged = true;
                int preservedTechnicalCells = 0;
                foreach (var frame in row.GetProperty("frames").EnumerateArray())
                {
                    int index = frame.GetProperty("index").GetInt32(), x = index % (texture.Width / frameWidth) * frameWidth, y = index / (texture.Width / frameWidth) * frameHeight;
                    bool technical = (asset.Equals("Animals/turtle", StringComparison.OrdinalIgnoreCase) || asset.Equals("Animals/turtle1", StringComparison.OrdinalIgnoreCase)) && index is 34 or 35;
                    int count = 0;
                    for (int py = y; py < y + frameHeight; py++) for (int px = x; px < x + frameWidth; px++)
                    {
                        int at = py * texture.Width + px;
                        if (colors[at].A > 0) count++;
                        if (technical) technicalCellsUnchanged &= colors[at] == original[at];
                        else if (colors[at].A > 0 && colors[at] != original[at]) changed++;
                    }
                    if (technical) preservedTechnicalCells++;
                    cells &= count == frame.GetProperty("visiblePixels").GetInt32();
                }
                bool productionMatches = false;
                if (registered.TryGetValue(asset, out var entry)) { artMod.GetType().GetMethod("CheckAsset", Flags)!.Invoke(artMod, new[] { entry }); productionMatches = true; }
                sheets.Add(new { Asset = asset, Dimensions = dimensions, NativeAlphaExact = alpha, EveryFrameOccupancy = cells, VisiblePixelsChanged = changed,
                    PreservedTechnicalCells = preservedTechnicalCells, TechnicalCellsRgbaExact = technicalCellsUnchanged, ProductionRegistryMatches = productionMatches });
                checks[asset] = dimensions && alpha && cells && technicalCellsUnchanged && changed > 10 && productionMatches;
            }
            var fixtures = new List<Fixture>(); var covered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            long id = 900000;
            foreach (var pair in DataLoader.FarmAnimals(Game1.content))
            {
                var data = pair.Value;
                var skinIds = new List<string?> { null };
                if (data.Skins != null) skinIds.AddRange(data.Skins.Select(s => s.Id));
                foreach (var skin in skinIds)
                foreach (string phase in new[] { "adult", "baby", "harvested" })
                {
                    var animal = new FarmAnimal(pair.Key, id++, 0);
                    animal.currentLocation = Game1.currentLocation; animal.skinID.Value = skin;
                    animal.age.Value = phase == "baby" ? 0 : data.DaysToMature;
                    animal.currentProduce.Value = phase == "harvested" ? null : "24";
                    animal.ReloadTextureIfNeeded(true);
                    string asset = animal.GetTexturePath().Replace('\\', '/'); covered.Add(asset);
                    bool valid = expectedNames.Contains(asset) && animal.Sprite.SpriteWidth == data.SpriteWidth && animal.Sprite.SpriteHeight == data.SpriteHeight;
                    routes.Add(new { Type = pair.Key, Skin = skin, Phase = phase, Asset = asset, Width = animal.Sprite.SpriteWidth, Height = animal.Sprite.SpriteHeight, Valid = valid });
                    checks[$"Route:{pair.Key}:{skin}:{phase}"] = valid;
                    fixtures.Add(new Fixture(pair.Key + " " + (skin ?? "default") + " " + phase, asset, animal, 0, Point.Zero));
                    if (phase == "adult") fixtures.Add(new Fixture(pair.Key + " sleep", asset, animal, data.SleepFrame, Point.Zero));
                    if (phase != "harvested" && animal.CanSwim())
                        fixtures.Add(new Fixture(pair.Key + " " + phase + " swim", asset, animal, 0, data.SwimOffset, true));
                }
            }
            foreach (var pair in DataLoader.Pets(Game1.content))
            foreach (var breed in pair.Value.Breeds)
            {
                var pet = new Pet(0, 0, breed.Id, pair.Key);
                string asset = pet.getPetTextureName().Replace('\\', '/'); covered.Add(asset);
                pet.GetPetIcon(out string iconAsset, out Rectangle iconRect);
                var icon = helper.GameContent.Load<Texture2D>(iconAsset);
                bool valid = expectedNames.Contains(asset) && pet.Sprite.SpriteWidth == 32 && pet.Sprite.SpriteHeight == 32
                    && iconRect.X >= 0 && iconRect.Y >= 0 && iconRect.Right <= icon.Width && iconRect.Bottom <= icon.Height;
                checks[$"Pet:{pair.Key}:{breed.Id}"] = valid;
                var frames = pair.Value.Behaviors.Where(b => b.Animation != null).SelectMany(b => b.Animation).Select(f => f.Frame).Concat(new[] { 28, 29 }).Distinct().ToArray();
                checks[$"PetBehaviorFrames:{pair.Key}:{breed.Id}"] = frames.All(f => f >= 0 && (f / (pet.Sprite.Texture.Width / 32) + 1) * 32 <= pet.Sprite.Texture.Height);
                routes.Add(new { Type = pair.Key, Breed = breed.Id, Asset = asset, IconAsset = iconAsset, IconRectangle = iconRect, Valid = valid });
                fixtures.Add(new Fixture(pair.Key + " " + breed.Id, asset, pet, 0, Point.Zero));
                // Native pet texture layouts are validated in full above; these select walk rows only.
                fixtures.Add(new Fixture(pair.Key + " " + breed.Id + " side", asset, pet, 4, Point.Zero));
                fixtures.Add(new Fixture(pair.Key + " " + breed.Id + " sleep", asset, pet, 28, Point.Zero));
            }
            var horse = new Horse(); horse.currentLocation = Game1.currentLocation;
            covered.Add("Animals/horse"); fixtures.Add(new Fixture("Horse / native draw", "Animals/horse", horse, 0, Point.Zero));
            var dormant = expectedNames.Except(covered, StringComparer.OrdinalIgnoreCase).OrderBy(s => s).ToArray();
            checks["NativeRoutes40AndTwoDormantSheets"] = dormant.SequenceEqual(new[] { "Animals/cat5", "Animals/dog5" }, StringComparer.OrdinalIgnoreCase);
            foreach (string asset in dormant)
            {
                var pet = new Pet(0, 0, "0", asset.Contains("cat", StringComparison.OrdinalIgnoreCase) ? "Cat" : "Dog");
                pet.Sprite.LoadTexture(asset);
                fixtures.Add(new Fixture(asset + " dormant atlas", asset, pet, 0, Point.Zero));
                routes.Add(new { Asset = asset, Kind = "Dormant sheet: no native breed entry; explicit texture override on Pet draw only" });
            }
            using var batch = new SpriteBatch(device);
            var sourcePalettes = new Dictionary<int, HashSet<Color>>();
            const int width = 1024, height = 768;
            for (int start = 0; start < fixtures.Count; start += 12)
            {
                using var target = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.None);
                device.SetRenderTarget(target); var backdrop = new Color(47, 65, 52); device.Clear(backdrop);
                batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
                int end = Math.Min(fixtures.Count, start + 12);
                for (int i = start; i < end; i++)
                {
                    Fixture fixture = fixtures[i]; int slot = i - start, x = slot % 4 * 256, y = slot / 4 * 256;
                    var character = fixture.Character; character.Position = new Vector2(x + 64, y + 150);
                    if (character is FarmAnimal farmAnimal) farmAnimal.isSwimming.Value = fixture.Swimming;
                    character.Sprite.currentFrame = fixture.Frame; character.Sprite.UpdateSourceRect();
                    Rectangle sourceRect = character.Sprite.SourceRect; sourceRect.Offset(fixture.Offset); character.Sprite.SourceRect = sourceRect;
                    bool sourceValid = sourceRect.Left >= 0 && sourceRect.Top >= 0 && sourceRect.Right <= character.Sprite.Texture.Width && sourceRect.Bottom <= character.Sprite.Texture.Height;
                    checks["Frame:" + i] = sourceValid;
                    if (!sourceValid) throw new InvalidDataException("Native draw frame outside atlas: " + fixture.Label);
                    var sourcePixels = new Color[sourceRect.Width * sourceRect.Height];
                    character.Sprite.Texture.GetData(0, sourceRect, sourcePixels, 0, sourcePixels.Length);
                    sourcePalettes[i] = sourcePixels.Where(c => c.A == 255 && c != backdrop).ToHashSet();
                    character.draw(batch);
                    batch.DrawString(Game1.smallFont, fixture.Label, new Vector2(x + 6, y + 210), Color.White, 0, Vector2.Zero, .65f, SpriteEffects.None, 1);
                }
                batch.End(); device.SetRenderTarget(null);
                var pixels = new Color[width * height]; target.GetData(pixels);
                for (int i = start; i < end; i++)
                {
                    int slot = i - start, x = slot % 4 * 256, y = slot / 4 * 256, changed = 0, bodyPixels = 0;
                    var drawnColors = new HashSet<Color>();
                    // Labels start at y+210; this region excludes them. Match the selected
                    // opaque native sprite colors so a shadow alone cannot pass the draw check.
                    for (int py = y + 12; py < y + 205; py++) for (int px = x + 12; px < x + 240; px++)
                    {
                        Color color = pixels[py * width + px];
                        if (color != backdrop) changed++;
                        if (sourcePalettes[i].Contains(color)) { bodyPixels++; drawnColors.Add(color); }
                    }
                    checks["GpuCreature:" + i] = changed > 100 && bodyPixels > 100 && drawnColors.Count >= 2;
                }
                string file = $"animals-native-{start / 12:D2}.png";
                using (var output = File.Create(Path.Combine(helper.DirectoryPath, file))) target.SaveAsPng(output, width, height);
                captures.Add(new { File = file, Cases = fixtures.Skip(start).Take(12).Select(f => new { f.Label, f.Asset, f.Frame, f.Offset, f.Swimming }).ToArray() });
            }
            checks["NativeDrawsCompleted"] = fixtures.Count >= 42;
        }
        catch (Exception ex) { error = ex.ToString(); }
        finally
        {
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
        bool passed = error == null && checks.Values.All(v => v) && sheets.Count == 42;
        helper.Data.WriteJsonFile("animals-checks.json", new { Passed = passed, Error = error, Checks = checks, Sheets = sheets, Routes = routes, Captures = captures,
            Scope = "Native typed data and real FarmAnimal/Pet/Horse constructors and Draw methods on GPU; detached title fixtures, no save or farm loaded. Exact alpha and per-cell occupancy against extracted native atlases; actual production CheckAsset verifies registrations. One 1024x768 target at a time.",
            Limitations = new[] { "Fixtures select native sleep/walk/swim source frames; they do not simulate live animal AI, swimming pathfinding, pet behavior transitions, horse mounting/rider animation, farming or save persistence.", "Prior 547 entry hash preservation is verified separately by root. Pet icon routes are bounded but shared UI icon repainting is outside this batch." } });
        monitor.Log("Animals GPU audit " + (passed ? "passed." : "FAILED: " + error), passed ? LogLevel.Info : LogLevel.Error);
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
