using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace NpcArtAudit;

internal static class ToolbarAudit
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const int Width = 1280, Height = 720;
    private sealed record Rendered(Color[] Pixels, Rectangle[] Bounds, string[] Labels);
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        if (Context.IsWorldReady || !Game1.IsOnMainThread() || Game1.game1.isDrawing || Game1.uiMode || Game1.uiModeCount != 0)
            throw new InvalidOperationException("Toolbar audit requires title update outside drawing/UI mode.");
        var device = Game1.graphics.GraphicsDevice;
        var targets = device.GetRenderTargets(); var viewport = device.Viewport; var scissor = device.ScissorRectangle;
        var blend = device.BlendState; var factor = device.BlendFactor; var depth = device.DepthStencilState;
        var rasterizer = device.RasterizerState; var vertices = GetVertexBuffers(device); var indices = device.Indices; var slots = SaveSlots(device);
        var player = Game1.player; var location = Game1.currentLocation; var random = Game1.random; var menu = Game1.activeClickableMenu;
        var options = Game1.options; var gameViewport = Game1.viewport; var uiViewport = Game1.uiViewport;
        var checks = new Dictionary<string, bool>(); var cases = new List<object>(); string? error = null;
        try
        {
            Game1.random = new Random(80716);
            typeof(Game1).GetField("_player", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, new Farmer());
            Game1.currentLocation = new GameLocation(); Game1.activeClickableMenu = null;
            Game1.options = (Options)typeof(object).GetMethod("MemberwiseClone", Flags)!.Invoke(options, null)!;
            Game1.options.pinToolbarToggle = false; Game1.options.gamepadControls = false;
            Game1.viewport = new xTile.Dimensions.Rectangle(0, 0, Width, Height); Game1.uiViewport = Game1.viewport;
            Game1.player.Items.Clear();
            for (int i = 0; i < 12; i++) Game1.player.Items.Add(ItemRegistry.Create("(O)388", 11 + i));
            Game1.player.CurrentToolIndex = 5;
            using var batch = new SpriteBatch(device);
            Rendered Draw(bool top, float scale, bool labels)
            {
                Game1.options.baseUIScale = scale / Game1.game1.zoomModifier;
                Game1.options.desiredUIScale = scale;
                Game1.player.Position = new Vector2(100, top ? Height - 64 : 64);
                using var logical = new RenderTarget2D(device, Width, Height, false, SurfaceFormat.Color, DepthFormat.None);
                device.SetRenderTarget(logical); device.Clear(new Color(194, 210, 160));
                var toolbar = new Toolbar();
                string[] nativeLabels = toolbar.slotText.ToArray();
                if (!labels) toolbar.slotText = Enumerable.Repeat("", 12).ToArray();
                batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
                toolbar.draw(batch);
                batch.End(); device.SetRenderTarget(null);
                var pixels = new Color[Width * Height]; logical.GetData(pixels);
                if (labels)
                {
                    // Match the game's final point-sampled UI surface presentation at the requested UI scale.
                    int physicalWidth = (int)(Width * scale), physicalHeight = (int)(Height * scale);
                    using var physical = new RenderTarget2D(device, physicalWidth, physicalHeight, false, SurfaceFormat.Color, DepthFormat.None);
                    device.SetRenderTarget(physical); device.Clear(Color.Black);
                    batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
                    batch.Draw(logical, Vector2.Zero, null, Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
                    batch.End(); device.SetRenderTarget(null);
                    using var output = File.Create(Path.Combine(helper.DirectoryPath, $"toolbar-{(top ? "top" : "bottom")}-{scale * 100:0}.png"));
                    physical.SaveAsPng(output, physicalWidth, physicalHeight);
                }
                return new Rendered(pixels, toolbar.buttons.Select(b => b.bounds).ToArray(), nativeLabels);
            }
            foreach (float scale in new[] { 1f, 1.25f, 1.5f })
            foreach (bool top in new[] { true, false })
            {
                string key = (top ? "top" : "bottom") + "-" + (int)(scale * 100);
                var baseline = Draw(top, scale, false);
                var drawn = Draw(top, scale, true);
                checks["NativeHitRectanglesUnchanged:" + key] = baseline.Bounds.SequenceEqual(drawn.Bounds);
                checks["NativeSlotGeometry:" + key] = drawn.Bounds.Select((b, i) => b.Width == 64 && b.Height == 64 && b.Left == Width / 2 - 384 + i * 64).All(v => v);
                checks["NativeDocking:" + key] = top ? drawn.Bounds.All(b => b.Top < 100) : drawn.Bounds.All(b => b.Top > Height - 120);
                checks["AllTwelveNativeLabels:" + key] = drawn.Labels.SequenceEqual(new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=" });
                var labels = new List<object>();
                for (int slot = 0; slot < 12; slot++)
                {
                    var bounds = drawn.Bounds[slot]; var lightPixels = new List<Point>(); var alteredPixels = new List<Point>();
                    bool itemAndStackUnchanged = true;
                    for (int y = bounds.Top; y < bounds.Bottom; y++)
                    for (int x = bounds.Left; x < bounds.Right; x++)
                    {
                        int at = y * Width + x; Color color = drawn.Pixels[at];
                        if (color == baseline.Pixels[at]) continue;
                        alteredPixels.Add(new Point(x, y));
                        if (y >= bounds.Top + 24) itemAndStackUnchanged = false;
                        if (color.R >= 200 && color.G >= 220 && color.B >= 235) lightPixels.Add(new Point(x, y));
                    }
                    Rectangle glyph = lightPixels.Count == 0 ? Rectangle.Empty : new Rectangle(lightPixels.Min(p => p.X), lightPixels.Min(p => p.Y), lightPixels.Max(p => p.X) - lightPixels.Min(p => p.X) + 1, lightPixels.Max(p => p.Y) - lightPixels.Min(p => p.Y) + 1);
                    bool center = lightPixels.Count >= 4 && Math.Abs(glyph.Center.X - bounds.Center.X) <= 3;
                    bool unclipped = lightPixels.Count >= 4 && glyph.Left >= bounds.Left + 2 && glyph.Right <= bounds.Right - 2 && glyph.Top >= bounds.Top + 1 && glyph.Bottom <= bounds.Top + 24;
                    double contrast = lightPixels.Count == 0 ? 0 : lightPixels.Average(p => (Luminance(drawn.Pixels[p.Y * Width + p.X]) + .05) / (Luminance(baseline.Pixels[p.Y * Width + p.X]) + .05));
                    checks[$"CenteredLabel:{key}:{drawn.Labels[slot]}"] = center;
                    checks[$"UnclippedLabel:{key}:{drawn.Labels[slot]}"] = unclipped;
                    checks[$"LabelContrast:{key}:{drawn.Labels[slot]}"] = contrast >= 4.5;
                    checks[$"ItemAndStackPixelsUnchanged:{key}:{slot}"] = itemAndStackUnchanged;
                    labels.Add(new { Label = drawn.Labels[slot], Selected = slot == 5, NativeHitBounds = bounds, MeasuredGlyphBounds = glyph, LightPixels = lightPixels.Count, Contrast = contrast, ChangedPixels = alteredPixels.Count });
                }
                // No new glyph or outline may bleed outside the native button rectangles.
                checks["NoOutOfSlotInk:" + key] = drawn.Pixels.Where((color, i) => color != baseline.Pixels[i] && !drawn.Bounds.Any(b => b.Contains(i % Width, i / Width))).Count() == 0;
                cases.Add(new { Case = key, ActualUiScaleOption = Game1.options.uiScale, LogicalWidth = Width, LogicalHeight = Height, PhysicalWidth = (int)(Width * scale), PhysicalHeight = (int)(Height * scale), Labels = labels });
            }
            checks["NativeItemStacksPreserved"] = Game1.player.Items.Count == 12 && Game1.player.Items.Select(i => i.Stack).SequenceEqual(Enumerable.Range(11, 12));
            checks["SelectedSlotSixPreserved"] = Game1.player.CurrentToolIndex == 5;
        }
        catch (Exception ex) { error = ex.ToString(); }
        finally
        {
            typeof(Game1).GetField("_player", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, player);
            Game1.currentLocation = location; Game1.random = random; Game1.activeClickableMenu = menu; Game1.options = options;
            Game1.viewport = gameViewport; Game1.uiViewport = uiViewport;
            device.SetRenderTargets(targets); device.Viewport = viewport; device.ScissorRectangle = scissor; device.BlendState = blend; device.BlendFactor = factor;
            device.DepthStencilState = depth; device.RasterizerState = rasterizer; device.SetVertexBuffers(vertices); device.Indices = indices; foreach (var slot in slots) slot.Restore();
            checks["GameAndGraphicsRestored"] = ReferenceEquals(Game1.player, player) && ReferenceEquals(Game1.currentLocation, location) && ReferenceEquals(Game1.random, random)
                && ReferenceEquals(Game1.activeClickableMenu, menu) && ReferenceEquals(Game1.options, options) && Game1.viewport.Equals(gameViewport) && Game1.uiViewport.Equals(uiViewport)
                && device.GetRenderTargets().SequenceEqual(targets) && device.Viewport.Equals(viewport) && device.ScissorRectangle == scissor && ReferenceEquals(device.BlendState, blend)
                && device.BlendFactor == factor && ReferenceEquals(device.DepthStencilState, depth) && ReferenceEquals(device.RasterizerState, rasterizer)
                && ReferenceEquals(device.Indices, indices) && GetVertexBuffers(device).SequenceEqual(vertices) && slots.All(s => s.Matches());
        }
        bool passed = error == null && cases.Count == 6 && checks.Values.All(v => v);
        helper.Data.WriteJsonFile("toolbar-checks.json", new { Passed = passed, Error = error, Checks = checks, Cases = cases,
            Scope = "Actual native Toolbar.draw, no manual ink scope; top/bottom docking and cloned UI settings at100/125/150%. Native label-free draws provide item/stack/hitbox reference; actual glyph pixels are measured, including selected6. Native-size UI surfaces are point-composited at each scale. No save loaded or selected.",
            Limitations = "No physical window resize or controller input. Centering, contrast and slot clipping measured from native logical UI pixels; final scaled captures require visual inspection. Item/body lower40pixel regions must match exactly; top24pixels may change only for shortcut ink." });
        monitor.Log("Toolbar GPU audit " + (passed ? "passed." : "FAILED: " + error), passed ? LogLevel.Info : LogLevel.Error);
    }
    private static double Luminance(Color color)
    {
        static double Linear(byte value) { double c = value / 255d; return c <= .04045 ? c / 12.92 : Math.Pow((c + .055) / 1.055, 2.4); }
        return .2126 * Linear(color.R) + .7152 * Linear(color.G) + .0722 * Linear(color.B);
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
