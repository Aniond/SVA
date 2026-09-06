using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NpcArtAudit;

/// <summary>Detached building texture/source-rectangle fixtures. Never loads or changes a save.</summary>
internal static class FarmBuildingsAudit
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        if (!Game1.IsOnMainThread() || Game1.game1 == null || Game1.game1.isDrawing || Game1.uiMode || Game1.uiModeCount != 0)
        {
            helper.Data.WriteJsonFile("farm-buildings-checks.json", new { Passed = false, Deferred = true });
            return;
        }
        var device = Game1.graphics.GraphicsDevice;
        var targets = device.GetRenderTargets(); var viewport = device.Viewport; var scissor = device.ScissorRectangle;
        var blend = device.BlendState; var factor = device.BlendFactor; var depth = device.DepthStencilState;
        var raster = device.RasterizerState; var vertices = GetVertexBuffers(device); var indices = device.Indices;
        var slots = SaveSlots(device);
        var location = Game1.currentLocation; var player = Game1.player; var random = Game1.random;
        var menu = Game1.activeClickableMenu; var worldViewport = Game1.viewport;
        var paintCache = BuildingPainter.paintMaskLookup.ToArray();
        var checks = new Dictionary<string, bool>(); var cases = new List<object>();
        var scopedTextures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? error = null;
        try
        {
            var wanted = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Farmhouse", "Cabin", "Barn", "Big Barn", "Deluxe Barn", "Coop", "Big Coop", "Deluxe Coop", "Greenhouse", "Shed", "Big Shed" };
            var definitions = DataLoader.Buildings(Game1.content).Where(p => wanted.Contains(p.Key)).ToDictionary(p => p.Key, p => p.Value);
            helper.Data.WriteJsonFile("farm-buildings-definitions.json", definitions);
            helper.Data.WriteJsonFile("farm-buildings-paint-data.json", DataLoader.PaintData(Game1.content));
            checks["AllScopedDefinitionsFound"] = definitions.Count == wanted.Count;
            string output = Path.Combine(helper.DirectoryPath, "farm-buildings-previews"); Directory.CreateDirectory(output);
            using var batch = new SpriteBatch(device);
            foreach (var entry in definitions)
            {
                var data = entry.Value;
                var styles = new List<(string Id, string Texture)> { ("default", data.Texture ?? "Buildings/" + entry.Key) };
                if (data.Skins != null) styles.AddRange(data.Skins.Select(s => (s.Id, s.Texture ?? data.Texture ?? "Buildings/" + entry.Key)));
                foreach (var style in styles)
                {
                    scopedTextures.Add(style.Texture.Replace('\\', '/'));
                    var texture = Game1.content.Load<Texture2D>(style.Texture);
                    Color[] pixels = new Color[texture.Width * texture.Height]; texture.GetData(pixels);
                    string stem = string.Concat((entry.Key + "-" + style.Id).Select(c => char.IsLetterOrDigit(c) ? c : '-'));
                    var paint = new BuildingPaintColor();
                    paint.Color1Default.Value = paint.Color2Default.Value = paint.Color3Default.Value = false;
                    paint.Color1Hue.Value = 210; paint.Color2Hue.Value = 30; paint.Color3Hue.Value = 120;
                    paint.Color1Saturation.Value = paint.Color2Saturation.Value = paint.Color3Saturation.Value = 65;
                    using var painted = BuildingPainter.Apply(texture, style.Texture + "_PaintMask", paint);
                    bool expectsPaint = entry.Key is "Farmhouse" or "Cabin" or "Deluxe Barn" or "Deluxe Coop" or "Big Shed";
                    checks[stem + "-NativePaintAvailability"] = (painted != null) == expectsPaint;
                    if (painted != null)
                    {
                        Color[] recolored = new Color[painted.Width * painted.Height]; painted.GetData(recolored);
                        checks[stem + "-PaintDimensionsAndAlpha"] = painted.Bounds == texture.Bounds && pixels.Select(c => c.A).SequenceEqual(recolored.Select(c => c.A));
                        checks[stem + "-PaintChangesVisiblePixels"] = pixels.Where((c, i) => c.A != 0 && c != recolored[i]).Any();
                        using var file = File.Create(Path.Combine(output, stem + "-painted-sheet.png")); painted.SaveAsPng(file, painted.Width, painted.Height);
                    }
                    int upgrades = entry.Key.Equals("Farmhouse", StringComparison.OrdinalIgnoreCase) || entry.Key == "Cabin" ? 3 : 1;
                    for (int season = 0; season < 4; season++)
                    for (int upgrade = 0; upgrade < upgrades; upgrade++)
                    for (int broken = 0; broken < (entry.Key == "Greenhouse" ? 2 : 1); broken++)
                    {
                        Rectangle rect = data.SourceRect == Rectangle.Empty ? texture.Bounds : data.SourceRect;
                        if (entry.Key == "Cabin") rect.X += rect.Width * upgrade;
                        else if (upgrades > 1) rect.Y += rect.Height * upgrade;
                        rect.Offset(data.SeasonOffset.X * season, data.SeasonOffset.Y * season);
                        if (broken == 1) rect.Y -= rect.Height;
                        string name = $"{stem}-season{season}-upgrade{upgrade}-broken{broken}";
                        bool inBounds = texture.Bounds.Contains(rect);
                        checks[name + "-Bounds"] = inBounds;
                        checks[name + "-Visible"] = inBounds && Enumerable.Range(rect.Y, rect.Height).Any(y => Enumerable.Range(rect.X, rect.Width).Any(x => pixels[y * texture.Width + x].A != 0));
                        var layers = new List<object>();
                        if (data.DrawLayers != null)
                        foreach (var layer in data.DrawLayers)
                        {
                            var sheet = layer.Texture == null ? texture : Game1.content.Load<Texture2D>(layer.Texture);
                            // Check all distinct animation rectangles sampled across one minute, including door layers.
                            var frames = Enumerable.Range(0, 600).Select(t => layer.GetSourceRect(t * 100)).Distinct().ToArray();
                            foreach (var frame in frames)
                            {
                                var adjusted = frame; adjusted.Offset(data.SeasonOffset.X * season, data.SeasonOffset.Y * season);
                                checks[name + "-Layer-" + layers.Count] = sheet.Bounds.Contains(adjusted);
                                layers.Add(new { Texture = layer.Texture ?? style.Texture, Source = adjusted, layer.DrawPosition, layer.AnimalDoorOffset, layer.DrawInBackground, layer.OnlyDrawIfChestHasContents });
                            }
                        }
                        cases.Add(new { Building = entry.Key, Skin = style.Id, style.Texture, Season = season, Upgrade = upgrade, Broken = broken == 1, Source = rect, TextureWidth = texture.Width, TextureHeight = texture.Height, PaintAvailable = painted != null, Layers = layers, Preview = name + ".png" });
                        if (!inBounds) continue;
                        // Explicit composite fixture: base + unconditional t=0 layers at native offsets, closed animal door.
                        int width = Math.Max(rect.Width + 64, 256), height = Math.Max(rect.Height + 64, 256);
                        using var target = new RenderTarget2D(device, width, height);
                        device.SetRenderTarget(target); device.Clear(Color.Transparent);
                        batch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp);
                        void DrawLayers(bool background)
                        {
                            if (data.DrawLayers == null) return;
                            foreach (var layer in data.DrawLayers.Where(l => l.DrawInBackground == background && l.OnlyDrawIfChestHasContents == null))
                            {
                                var source = layer.GetSourceRect(0); source.Offset(data.SeasonOffset.X * season, data.SeasonOffset.Y * season);
                                var sheet = layer.Texture == null ? texture : Game1.content.Load<Texture2D>(layer.Texture);
                                if (sheet.Bounds.Contains(source)) batch.Draw(sheet, new Vector2(32, 32) + layer.DrawPosition, source, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, background ? 0 : 0.5f - layer.SortTileOffset * 0.001f + 0.000015625f);
                            }
                        }
                        DrawLayers(true); batch.Draw(texture, new Vector2(32, 32), rect, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.5f - data.SortTileOffset * 0.001f); DrawLayers(false); batch.End();
                        device.SetRenderTarget(null);
                        using var stream = File.Create(Path.Combine(output, name + ".png")); target.SaveAsPng(stream, width, height);
                    }
                }
            }
            checks["SeventeenScopedTextures"] = scopedTextures.Count == 17;
            checks["NativeScopedSeasonOffsetsZero"] = definitions.Values.All(d => d.SeasonOffset == Point.Zero);
        }
        catch (Exception ex) { error = ex.ToString(); }
        finally
        {
            BuildingPainter.paintMaskLookup.Clear(); foreach (var pair in paintCache) BuildingPainter.paintMaskLookup[pair.Key] = pair.Value;
            device.SetRenderTargets(targets); device.Viewport = viewport; device.ScissorRectangle = scissor;
            device.BlendState = blend; device.BlendFactor = factor; device.DepthStencilState = depth; device.RasterizerState = raster;
            device.SetVertexBuffers(vertices); device.Indices = indices; foreach (var slot in slots) slot.Restore();
            checks["StateRestored"] = device.GetRenderTargets().SequenceEqual(targets) && device.Viewport.Equals(viewport)
                && device.ScissorRectangle == scissor && ReferenceEquals(device.BlendState, blend) && device.BlendFactor == factor
                && ReferenceEquals(device.DepthStencilState, depth) && ReferenceEquals(device.RasterizerState, raster)
                && GetVertexBuffers(device).SequenceEqual(vertices) && ReferenceEquals(device.Indices, indices)
                && slots.All(s => s.Matches()) && ReferenceEquals(Game1.player, player) && ReferenceEquals(Game1.currentLocation, location)
                && ReferenceEquals(Game1.random, random) && ReferenceEquals(Game1.activeClickableMenu, menu) && Game1.viewport.Equals(worldViewport);
        }
        bool passed = error == null && checks.Count > 1 && checks.Values.All(v => v);
        helper.Data.WriteJsonFile("farm-buildings-checks.json", new { Passed = passed, Error = error, MusicVolumeLevel = Game1.options.musicVolumeLevel, MusicPlayerVolume = Game1.musicPlayerVolume, SoundVolumeLevel=Game1.options.soundVolumeLevel, AmbientVolumeLevel=Game1.options.ambientVolumeLevel, Scope = "Detached GPU composites using native BuildingData source rectangles and layers. Not native Building.draw or save-based gameplay. Upgrade 2 represents 2 and 3. Animation bounds sampled every 100ms for one minute. No player/world assignments.", Checks = checks, Cases = cases });
        monitor.Log("Farm building fixtures " + (passed ? "passed." : "FAILED: " + error), passed ? LogLevel.Info : LogLevel.Error);
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

