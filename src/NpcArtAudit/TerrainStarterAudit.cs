using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewValley;

namespace NpcArtAudit;

/// <summary>Isolated native water clock/draw proof; artwork identity stays in the registry verifier.</summary>
internal static class TerrainStarterAudit
{
    private const int Size = 192;
    private static readonly Color Clear = new(9, 7, 13, 255);

    // Coordinator calls this on an update/console callback, never during a SpriteBatch draw.
    // Native references: GameLocation.cs 4296-4310, 15797-15810, 16315-16329;
    // Grass.cs 103-169 and 520-528. Does not call loadMap, resetSharedState or seasonUpdate.
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        if (!Game1.IsOnMainThread() || Game1.game1 == null || Game1.game1.isDrawing
            || Game1.uiMode || Game1.uiModeCount != 0)
        {
            helper.Data.WriteJsonFile("terrain-starter-checks.json", new
            {
                Passed = false, Deferred = true,
                Error = "Run on the game thread outside drawing and UI mode."
            });
            return;
        }

        var device = Game1.graphics.GraphicsDevice;
        var targets = device.GetRenderTargets();
        var viewport = device.Viewport;
        var scissor = device.ScissorRectangle;
        var blend = device.BlendState;
        var blendFactor = device.BlendFactor;
        var depth = device.DepthStencilState;
        var rasterizer = device.RasterizerState;
        var sampler = device.SamplerStates[0];
        var boundTexture = device.Textures[0];
        var vertexBuffers = GetVertexBuffers(device);
        var indices = device.Indices;
        var gameViewport = Game1.viewport;
        var uiViewport = Game1.uiViewport;
        var uiMode = Game1.uiMode;
        var uiCount = Game1.uiModeCount;
        var nonUiTarget = Game1.nonUIRenderTarget;
        var cursors = Game1.mouseCursors;
        var location = Game1.currentLocation;
        var menu = Game1.activeClickableMenu;
        var random = Game1.random;
        string? error = null;
        bool restored = false;
        int renderCases = 0, tileCalls = 0, checkedPixels = 0, loadedWaterPixels = 0;
        var clockCases = new List<object>();
        var drawCases = new List<object>();
        var grassFrames = new List<object>();
        var previewFiles = new List<string>();

        try
        {
            using var content = Game1.content.CreateTemporary();
            var loadedCursors = content.Load<Texture2D>("LooseSprites/Cursors");
            Require(cursors != null && cursors.Width == 704 && cursors.Height == 2256,
                "The actual native water draw atlas must be 704x2256.");
            Require(loadedCursors.Width == 704 && loadedCursors.Height == 2256,
                "Loaded Cursors dimensions differ.");
            var atlas = new Color[cursors!.Width * cursors.Height];
            var loaded = new Color[loadedCursors.Width * loadedCursors.Height];
            cursors.GetData(atlas);
            loadedCursors.GetData(loaded);
            // Proves the actual global draw texture contains the current loaded bands.
            // The coordinator's existing verifier checks those loaded pixels against the new registry art.
            foreach (int stripY in new[] { 2064, 2192 })
                for (int y = stripY; y < stripY + 64; y++)
                    for (int x = 0; x < 640; x++)
                    {
                        Require(atlas[y * 704 + x] == loaded[y * 704 + x],
                            $"Actual draw Cursors cache differs from loaded water at {x},{y}.");
                        loadedWaterPixels++;
                    }

            var grass = content.Load<Texture2D>("TerrainFeatures/grass");
            Require(grass.Width == 66 && grass.Height == 240, "Grass must remain 66x240.");
            var grassPixels = new Color[grass.Width * grass.Height];
            grass.GetData(grassPixels);
            for (int frame = 0; frame < 3; frame++)
            {
                int occupied = 0;
                for (int y = 0; y < 20; y++) for (int x = frame * 15; x < frame * 15 + 15; x++)
                    if (grassPixels[y * 66 + x].A != 0) occupied++;
                Require(occupied > 0, "Ordinary spring grass variant is empty: " + frame);
                grassFrames.Add(new { Variant = frame, Rectangle = new[] { frame * 15, 0, 15, 20 }, Occupied = occupied });
            }
            foreach (string asset in new[] { "TerrainFeatures/Flooring", "TerrainFeatures/Flooring_winter" })
            {
                var floor = content.Load<Texture2D>(asset);
                Require(floor.Width == 256 && floor.Height == 256, asset + " must remain 256x256.");
            }

            // Skip the location constructor and all net registrations/map loading. These are the
            // only instance members read by updateWater and drawWaterTile on this local object.
#pragma warning disable SYSLIB0050
            var fixture = (GameLocation)FormatterServices.GetUninitializedObject(typeof(GameLocation));
#pragma warning restore SYSLIB0050
            helper.Reflection.GetField<NetBool>(fixture, "isFarm").SetValue(new NetBool(true));
            helper.Reflection.GetField<NetColor>(fixture, "waterColor").SetValue(new NetColor(Color.White));
            fixture.map = new xTile.Map();
            fixture.map.AddLayer(new xTile.Layers.Layer("Back", fixture.map,
                new xTile.Dimensions.Size(3, 3), new xTile.Dimensions.Size(16, 16)));
            fixture.waterTiles = new WaterTiles(3, 3);
            for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++) fixture.waterTiles[x, y] = true;

            fixture.waterAnimationIndex = 0;
            fixture.waterAnimationTimer = 200;
            fixture.waterPosition = 0;
            var total = TimeSpan.Zero;
            var seen = new HashSet<int> { 0 };
            for (int step = 0; step < 10; step++)
            {
                total += TimeSpan.FromMilliseconds(199);
                fixture.updateWater(new GameTime(total, TimeSpan.FromMilliseconds(199)));
                Require(fixture.waterAnimationIndex == step && fixture.waterAnimationTimer == 1,
                    "Water frame advanced before the 200ms boundary: " + step);
                clockCases.Add(new { Case = "BeforeBoundary", Frame = fixture.waterAnimationIndex, Timer = fixture.waterAnimationTimer, TotalMilliseconds = total.TotalMilliseconds });
                total += TimeSpan.FromMilliseconds(1);
                fixture.updateWater(new GameTime(total, TimeSpan.FromMilliseconds(1)));
                Require(fixture.waterAnimationIndex == (step + 1) % 10 && fixture.waterAnimationTimer == 200,
                    "Water frame did not advance/reset/wrap at 200ms: " + step);
                seen.Add(fixture.waterAnimationIndex);
                clockCases.Add(new { Case = "AtBoundary", Frame = fixture.waterAnimationIndex, Timer = fixture.waterAnimationTimer, TotalMilliseconds = total.TotalMilliseconds });
            }
            Require(seen.Count == 10 && Math.Abs(fixture.waterPosition - 2f) < 0.00001f,
                "Ten-frame coverage or farm's 0.1-per-update scroll differs.");
            fixture.waterPosition = 63.95f;
            fixture.waterTileFlip = false;
            fixture.updateWater(new GameTime(total, TimeSpan.Zero));
            Require(fixture.waterTileFlip && Math.Abs(fixture.waterPosition - 0.05f) < 0.00002f,
                "Water position did not wrap at 64 and flip parity.");
            clockCases.Add(new { Case = "FarmScrollWrap", fixture.waterPosition, fixture.waterTileFlip });
            fixture.isFarm.Value = false;
            fixture.waterPosition = 0;
            fixture.waterAnimationTimer = 200;
            fixture.waterAnimationIndex = 0;
            fixture.updateWater(new GameTime(TimeSpan.FromMilliseconds(1250), TimeSpan.FromMilliseconds(1200)));
            float expectedPosition = (float)((Math.Sin(0.25f) + 1.0) * 0.15000000596046448);
            Require(fixture.waterAnimationIndex == 1 && fixture.waterAnimationTimer == 200
                && Math.Abs(fixture.waterPosition - expectedPosition) < 0.000001f,
                "Native millisecond-component clock behavior differs.");
            clockCases.Add(new { Case = "NonFarmComponentClock", ElapsedMilliseconds = 1200, ElapsedComponent = 200, TotalMilliseconds = 1250, TotalComponent = 250, fixture.waterPosition, fixture.waterAnimationIndex });
            fixture.waterAnimationTimer = 200;
            fixture.updateWater(new GameTime(TimeSpan.FromMilliseconds(1500), TimeSpan.FromMilliseconds(650)));
            Require(fixture.waterAnimationIndex == 2 && fixture.waterAnimationTimer == 200,
                "Native expired clock must advance once without multi-frame catch-up.");
            clockCases.Add(new { Case = "NoCatchUp", ElapsedMilliseconds = 650, fixture.waterAnimationIndex, fixture.waterAnimationTimer });

            Game1.viewport = new xTile.Dimensions.Rectangle(0, 0, Size, Size);
            var seasons = new[]
            {
                (Name: "spring", Tint: new Color(120, 200, 255) * 0.5f),
                (Name: "summer", Tint: new Color(60, 240, 255) * 0.5f),
                (Name: "fall", Tint: new Color(255, 130, 200) * 0.5f),
                (Name: "winter", Tint: new Color(130, 80, 255) * 0.5f)
            };
            string previewDirectory = Path.Combine(helper.DirectoryPath, "terrain-starter-runtime-previews");
            Directory.CreateDirectory(previewDirectory);
            using var target = new RenderTarget2D(device, Size, Size);
            using var batch = new SpriteBatch(device);
            foreach (var season in seasons)
                foreach (bool flip in new[] { false, true })
                    foreach (int position in new[] { 0, 16, 63 })
                        for (int frame = 0; frame < 10; frame++)
                        {
                            fixture.waterColor.Value = season.Tint;
                            fixture.waterTileFlip = flip;
                            fixture.waterPosition = position;
                            fixture.waterAnimationIndex = frame;
                            RenderNative(fixture, device, target, batch, BlendState.Opaque, Clear);
                            tileCalls += 9;
                            var actual = new Color[Size * Size];
                            target.GetData(actual);
                            var expected = ExpectedPatch(atlas, frame, position, flip, season.Tint);
                            // At position zero native emits a bottom fill of height -1. Its final
                            // boundary row depends on SpriteBatch negative-rectangle rasterization;
                            // that row is previewed but deliberately excluded from the pixel oracle.
                            int rowsChecked = position == 0 ? Size - 1 : Size;
                            for (int y = 0; y < rowsChecked; y++) for (int x = 0; x < Size; x++)
                            {
                                int i = y * Size + x;
                                Require(Close(actual[i], expected[i]),
                                    $"Native water pixel differs: {season.Name}, flip={flip}, position={position}, frame={frame}, xy={x},{y}, actual={actual[i]}, expected={expected[i]}.");
                                checkedPixels++;
                            }
                            renderCases++;
                            drawCases.Add(new { Season = season.Name, Flip = flip, Position = position, Frame = frame, RowsChecked = rowsChecked });
                            // Ten spring frames at native timing, plus other season/scroll/parity examples.
                            if ((!flip && position == 16 && (season.Name == "spring" || frame == 0))
                                || (frame == 0 && (position != 16 || flip)))
                            {
                                RenderNative(fixture, device, target, batch, BlendState.AlphaBlend, new Color(48, 70, 57));
                                tileCalls += 9;
                                string file = $"water-{season.Name}-flip{(flip ? 1 : 0)}-position{position}-frame{frame}.png";
                                using var stream = File.Create(Path.Combine(previewDirectory, file));
                                target.SaveAsPng(stream, Size, Size);
                                previewFiles.Add("terrain-starter-runtime-previews/" + file);
                            }
                        }
        }
        catch (Exception ex) { error = ex.ToString(); }
        finally
        {
            Game1.viewport = gameViewport;
            Game1.uiViewport = uiViewport;
            Game1.uiMode = uiMode;
            Game1.uiModeCount = uiCount;
            Game1.nonUIRenderTarget = nonUiTarget;
            device.SetRenderTargets(targets);
            device.Viewport = viewport;
            device.ScissorRectangle = scissor;
            device.BlendState = blend;
            device.BlendFactor = blendFactor;
            device.DepthStencilState = depth;
            device.RasterizerState = rasterizer;
            device.SamplerStates[0] = sampler;
            device.Textures[0] = boundTexture;
            device.SetVertexBuffers(vertexBuffers);
            device.Indices = indices;
            restored = Game1.viewport.Equals(gameViewport) && Game1.uiViewport.Equals(uiViewport)
                && Game1.uiMode == uiMode && Game1.uiModeCount == uiCount
                && ReferenceEquals(Game1.nonUIRenderTarget, nonUiTarget)
                && ReferenceEquals(Game1.mouseCursors, cursors) && ReferenceEquals(Game1.currentLocation, location)
                && ReferenceEquals(Game1.activeClickableMenu, menu) && ReferenceEquals(Game1.random, random)
                && device.GetRenderTargets().SequenceEqual(targets) && device.Viewport.Equals(viewport)
                && device.ScissorRectangle.Equals(scissor) && ReferenceEquals(device.BlendState, blend)
                && device.BlendFactor == blendFactor
                && ReferenceEquals(device.DepthStencilState, depth)
                && ReferenceEquals(device.RasterizerState, rasterizer) && ReferenceEquals(device.SamplerStates[0], sampler)
                && ReferenceEquals(device.Textures[0], boundTexture) && ReferenceEquals(device.Indices, indices)
                && GetVertexBuffers(device).SequenceEqual(vertexBuffers);
        }

        helper.Data.WriteJsonFile("terrain-starter-checks.json", new
        {
            Passed = error == null && restored, Error = error, StateRestored = restored,
            NativeClockCases = clockCases, NativeDrawCases = renderCases, NativeDrawWaterTileCalls = tileCalls,
            NativeRenderedPixelsCompared = checkedPixels, LoadedToActualDrawWaterPixelsCompared = loadedWaterPixels,
            DrawCases = drawCases, GrassDimensions = new[] { 66, 240 }, GrassVariants = grassFrames,
            FlooringDimensions = new[] { 256, 256 }, WaterBands = new[] { new[] { 0, 2064, 640, 64 }, new[] { 0, 2192, 640, 64 } },
            FrameCount = 10, FrameIntervalMilliseconds = 200, Previews = previewFiles,
            PreviewMeaning = "Actual native 3x3 water draw over neutral green with AlphaBlend; filenames identify frame, parity and scroll.",
            PixelOracle = "Opaque render, nearest sampling, independently assembled source rectangles and tint; channel tolerance 1 for GPU UNORM rounding.",
            PositionZeroLastRowExcluded = true,
            ArtworkIdentityVerifiedHere = false, ArtworkIdentityVerifier = "Coordinator existing registry-to-loaded-texture pixel verifier",
            NewGrassOrFloorArtAsserted = false, FarmLoaded = false, SaveWritten = false,
            ConstructorSkipped = true, NativeSeasonCallbacksInvoked = false, NativeDrawWaterGridLoopInvoked = false,
            FullScenePlayback = false, ShorelineMapArtVerified = false, SubmergedEventsVerified = false,
            Notes = "Four seasonal tint values applied to isolated NetColor; no game season changed. Native updateWater and drawWaterTile are invoked directly. AlphaBlend previews are visual evidence; deterministic pixel assertions use Opaque. Beach special tint and other location render overrides are outside this starter fixture."
        });
        monitor.Log(error == null && restored
            ? $"Terrain starter native clock/draw fixture passed: {renderCases} render cases, {checkedPixels} pixel checks. Registry artwork identity and broader terrain coverage are separate."
            : "Terrain starter fixture failed: " + (error ?? "State restoration mismatch."),
            error == null && restored ? LogLevel.Info : LogLevel.Error);
    }

    private static void RenderNative(GameLocation fixture, GraphicsDevice device, RenderTarget2D target,
        SpriteBatch batch, BlendState blend, Color background)
    {
        var previousTargets = device.GetRenderTargets();
        bool begun = false;
        try
        {
            device.SetRenderTarget(target);
            device.Clear(background);
            batch.Begin(SpriteSortMode.Deferred, blend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
            begun = true;
            for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++) fixture.drawWaterTile(batch, x, y);
            batch.End();
            begun = false;
        }
        finally
        {
            try { if (begun) batch.End(); }
            finally { device.SetRenderTargets(previousTargets); }
        }
    }

    private static Color[] ExpectedPatch(Color[] atlas, int frame, int position, bool flip, Color tint)
    {
        var expected = Enumerable.Repeat(Clear, Size * Size).ToArray();
        void Copy(int dx, int dy, int sy, int height)
        {
            for (int y = 0; y < height; y++) for (int x = 0; x < 64; x++)
            {
                int targetY = dy + y;
                if (targetY < 0 || targetY >= Size) continue;
                var source = atlas[(sy + y) * 704 + frame * 64 + x];
                expected[targetY * Size + dx + x] = new Color(
                    Multiply(source.R, tint.R), Multiply(source.G, tint.G),
                    Multiply(source.B, tint.B), Multiply(source.A, tint.A));
            }
        }
        for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++)
        {
            int strip = (((x + y) & 1) ^ (flip ? 1 : 0)) * 128;
            bool topShore = y == 0;
            Copy(x * 64, y * 64 - (topShore ? 0 : position),
                2064 + strip + (topShore ? position : 0), 64 - (topShore ? position : 0));
            if (y == 2 && position > 0)
                Copy(x * 64, 192 - position, 2064 + (128 - strip), position - 1);
        }
        return expected;
    }

    private static VertexBufferBinding[] GetVertexBuffers(GraphicsDevice device)
    {
        // Bundled MonoGame has public SetVertexBuffers but its matching getter is internal.
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
        var bindings = typeof(GraphicsDevice).GetField("_vertexBuffers", flags)!.GetValue(device)!;
        return (VertexBufferBinding[])bindings.GetType().GetMethod("Get", flags, null, Type.EmptyTypes, null)!.Invoke(bindings, null)!;
    }

    private static byte Multiply(byte a, byte b) => (byte)Math.Round(a * b / 255.0);
    private static bool Close(Color a, Color b) => Math.Abs(a.R - b.R) <= 1 && Math.Abs(a.G - b.G) <= 1
        && Math.Abs(a.B - b.B) <= 1 && Math.Abs(a.A - b.A) <= 1;
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
