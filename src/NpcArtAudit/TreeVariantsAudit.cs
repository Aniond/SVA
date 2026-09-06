using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NpcArtAudit;

/// <summary>Detached native source-region fixtures, without creating or saving a farm.</summary>
internal static class TreeVariantsAudit
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        if (!Game1.IsOnMainThread() || Game1.game1 == null || Game1.game1.isDrawing || Context.IsWorldReady)
        { helper.Data.WriteJsonFile("tree-variants-checks.json", new { Passed = false, Deferred = true }); return; }
        var device = Game1.graphics.GraphicsDevice;
        var targets = device.GetRenderTargets(); var viewport = device.Viewport; var scissor = device.ScissorRectangle;
        var blend = device.BlendState; var factor = device.BlendFactor; var depth = device.DepthStencilState;
        var raster = device.RasterizerState; var vertices = GetVertexBuffers(device); var indices = device.Indices;
        var slots = SaveSlots(device); var player = Game1.player; var location = Game1.currentLocation;
        var checks = new Dictionary<string, bool>(); var cases = new List<object>(); string? error = null;
        try
        {
            string output = Path.Combine(helper.DirectoryPath, "tree-variants-previews"); Directory.CreateDirectory(output);
            using var batch = new SpriteBatch(device);
            void Render(string asset, string name, Rectangle rect, bool requireVisible)
            {
                var texture = helper.GameContent.Load<Texture2D>(asset);
                checks[name + "-bounds"] = texture.Bounds.Contains(rect);
                if (!texture.Bounds.Contains(rect)) return;
                var pixels = new Color[rect.Width * rect.Height]; texture.GetData(0, rect, pixels, 0, pixels.Length);
                bool visible = pixels.Any(c => c.A > 0);
                if (requireVisible) checks[name + "-visible"] = visible;
                using var target = new RenderTarget2D(device, rect.Width * 4, rect.Height * 4, false, SurfaceFormat.Color, DepthFormat.None);
                device.SetRenderTarget(target); device.Clear(Color.Transparent);
                batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
                batch.Draw(texture, target.Bounds, rect, Color.White); batch.End(); device.SetRenderTarget(null);
                using var file = File.Create(Path.Combine(output, name + ".png")); target.SaveAsPng(file, target.Width, target.Height);
                cases.Add(new { Asset = asset, Name = name, Source = rect, Visible = visible, Preview = name + ".png" });
            }
            var wild = DataLoader.WildTrees(Game1.content);
            var assets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string id in new[] { "6", "7", "8", "9", "10", "11", "12", "13" })
            {
                var definition = wild[id];
                foreach (var texture in definition.Textures)
                {
                    string asset = texture.Texture.Replace('\\', '/');
                    if (!assets.Add(asset)) continue;
                    string stem = asset.Split('/').Last();
                    Render(asset, stem + "-mature", new Rectangle(0, 0, 48, 96), true);
                    Render(asset, stem + "-stump", new Rectangle(32, 96, 16, 32), true);
                    Render(asset, stem + "-sapling", new Rectangle(0, 96, 16, 32), false);
                    for (int stage = 0; stage < 3; stage++) Render(asset, stem + "-stage" + stage, new Rectangle(stage == 0 ? 32 : (stage - 1) * 16, 128, 16, 16), false);
                    for (int frame = 0; frame < 3; frame++) Render(asset, stem + "-damage" + frame, new Rectangle(frame * 16, 144, 16, 16), false);
                    for (int leaf = 0; leaf < 4; leaf++) Render(asset, stem + "-leaf" + leaf, new Rectangle(16 + leaf % 2 * 8, 112 + leaf / 2 * 8, 8, 8), false);
                    var loaded = helper.GameContent.Load<Texture2D>(asset);
                    if (definition.GrowsMoss && loaded.Width >= 144)
                    {
                        Render(asset, stem + "-moss", new Rectangle(96, 0, 48, 96), true);
                        Render(asset, stem + "-moss-stump", new Rectangle(128, 96, 16, 32), true);
                    }
                    if (definition.UseAlternateSpriteWhenSeedReady || definition.UseAlternateSpriteWhenNotShaken)
                        Render(asset, stem + "-alternate", new Rectangle(48, 0, 48, 96), true);
                }
            }
            checks["SeventeenSpecialTreeSheets"] = assets.Count == 17;
            var fruit = DataLoader.FruitTrees(Game1.content);
            checks["EightFruitSpecies"] = fruit.Count == 8;
            foreach (var pair in fruit)
            {
                var data = pair.Value; int y = data.TextureSpriteRow * 80; string asset = data.Texture.Replace('\\', '/');
                for (int stage = 0; stage < 4; stage++) Render(asset, pair.Key + "-growth" + stage, new Rectangle(stage * 48, y, 48, 80), true);
                for (int season = 0; season < 4; season++)
                {
                    Render(asset, pair.Key + "-season" + season, new Rectangle(192 + season * 48, y, 48, 64), true);
                    Render(asset, pair.Key + "-shadow" + season, new Rectangle(192 + season * 48, y + 64, 48, 16), false);
                    if (season < 3) Render(asset, pair.Key + "-leaves" + season, new Rectangle(384 + season * 16, y, 8, 8), false);
                }
                Render(asset, pair.Key + "-stump", new Rectangle(384, y + 48, 48, 32), true);
                // Native mature layering at a fixed tile: shadow, stump, canopy and
                // three fruit positions. These are composites, never live tree draws.
                var texture = helper.GameContent.Load<Texture2D>(asset);
                foreach (int season in Enumerable.Range(0, 4))
                foreach (string state in new[] { "fruit", "lightning", "falling" })
                {
                    using var target = new RenderTarget2D(device, 160 * 3, 144 * 3, false, SurfaceFormat.Color, DepthFormat.None);
                    device.SetRenderTarget(target); device.Clear(Color.Transparent);
                    batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, transformMatrix: Matrix.CreateScale(3));
                    var anchor = new Vector2(80, 112); var tint = state == "lightning" ? Color.Gray : Color.White;
                    if (state != "falling") batch.Draw(texture, anchor, new Rectangle(192 + season * 48, y + 64, 48, 16), tint, 0, new Vector2(24, 16), 1, SpriteEffects.None, 0);
                    batch.Draw(texture, anchor, new Rectangle(384, y + 48, 48, 32), tint, 0, new Vector2(24, 32), 1, SpriteEffects.None, 0);
                    batch.Draw(texture, anchor, new Rectangle(192 + season * 48, y, 48, 64), tint, state == "falling" ? .6f : 0, new Vector2(24, 80), 1, SpriteEffects.None, 0);
                    string fruitId = state == "lightning" ? "(O)382" : data.Fruit[0].ItemId;
                    var item = ItemRegistry.GetDataOrErrorItem(fruitId);
                    if (state != "falling")
                    {
                        Vector2[] positions = { new(56, 48), new(80, 32), new(72, 56) };
                        for (int index = 0; index < positions.Length; index++) batch.Draw(item.GetTexture(), positions[index], item.GetSourceRect(), Color.White, 0, Vector2.Zero, 1, index == 2 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
                    }
                    batch.End(); device.SetRenderTarget(null);
                    var pixels = new Color[target.Width * target.Height]; target.GetData(pixels);
                    string name = pair.Key + "-assembled-" + season + "-" + state;
                    checks[name + "-visible"] = pixels.Any(c => c.A > 0);
                    using var file = File.Create(Path.Combine(output, name + ".png")); target.SaveAsPng(file, target.Width, target.Height);
                    cases.Add(new { Asset = asset, Name = name, Season = season, State = state, Fruit = fruitId, Preview = name + ".png" });
                }
            }
        }
        catch (Exception ex) { error = ex.ToString(); }
        finally
        {
            device.SetRenderTargets(targets); device.Viewport = viewport; device.ScissorRectangle = scissor;
            device.BlendState = blend; device.BlendFactor = factor; device.DepthStencilState = depth; device.RasterizerState = raster;
            device.SetVertexBuffers(vertices); device.Indices = indices; foreach (var slot in slots) slot.Restore();
            checks["StateRestored"] = device.GetRenderTargets().SequenceEqual(targets) && device.Viewport.Equals(viewport)
                && device.ScissorRectangle == scissor && ReferenceEquals(device.BlendState, blend) && device.BlendFactor == factor
                && ReferenceEquals(device.DepthStencilState, depth) && ReferenceEquals(device.RasterizerState, raster)
                && GetVertexBuffers(device).SequenceEqual(vertices) && ReferenceEquals(device.Indices, indices)
                && slots.All(s => s.Matches()) && ReferenceEquals(player, Game1.player) && ReferenceEquals(location, Game1.currentLocation);
        }
        bool passed = error == null && checks.Count > 1 && checks.Values.All(v => v);
        helper.Data.WriteJsonFile("tree-variants-checks.json", new { Passed = passed, Error = error, Scope = "Detached native texture regions; no native Tree.draw, FruitTree.draw, or farm gameplay. Winter leaf legacy out-of-bounds is not drawn.", Checks = checks, Cases = cases });
        monitor.Log("Tree variant fixtures " + (passed ? "passed." : "FAILED: " + error), passed ? LogLevel.Info : LogLevel.Error);
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
