using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NpcArtAudit;

/// <summary>Coordinator invokes on the update thread, outside any open SpriteBatch.</summary>
internal static class StormHailAudit
{
    private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static void Run(IModHelper helper, IMonitor monitor)
    {
        if (!Game1.IsOnMainThread() || Game1.game1 == null || Game1.game1.isDrawing || Game1.uiMode || Game1.uiModeCount != 0)
        {
            helper.Data.WriteJsonFile("storm-hail-checks.json", new { Passed = false, Deferred = true, Error = "Run outside drawing and UI mode on the game thread." });
            return;
        }
        var device = Game1.graphics.GraphicsDevice;
        var targets = device.GetRenderTargets();
        var viewport = device.Viewport;
        var scissor = device.ScissorRectangle;
        var blend = device.BlendState;
        var factor = device.BlendFactor;
        var depth = device.DepthStencilState;
        var raster = device.RasterizerState;
        var sampler = device.SamplerStates[0];
        var texture = device.Textures[0];
        var indices = device.Indices;
        var bindings = typeof(GraphicsDevice).GetField("_vertexBuffers", Instance)!.GetValue(device)!;
        var vertices = (VertexBufferBinding[])bindings.GetType().GetMethod("Get", Instance, null, Type.EmptyTypes, null)!.Invoke(bindings, null)!;
        var random = Game1.random;
        var gameViewport = Game1.viewport;
        var counter = new CountingRandom();
        int checks = 0, draws = 0, pixels = 0;
        string? error = null;
        bool restored = false;
        var occupied = new List<int>();
        void Check(bool ok, string message) { checks++; if (!ok) throw new InvalidOperationException(message); }
        try
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("AbigailModern.StormHail")).FirstOrDefault(t => t != null)
                ?? throw new InvalidOperationException("StormHail renderer is not loaded.");
            object Call(string name, params object[] args) => type.GetMethod(name, Static)!.Invoke(null, args)!;
            var sheet = helper.GameContent.Load<Texture2D>("Mods/David.AbigailModern/Weather/Hail");
            Check(sheet.Width == 64 && sheet.Height == 16, "Hail sheet dimensions changed.");
            var source = new Color[64 * 16]; sheet.GetData(source);
            for (int frame = 0; frame < 4; frame++)
            {
                int count = 0;
                for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++) if (source[y * 64 + frame * 16 + x].A > 0) count++;
                occupied.Add(count); Check(count > 0, "Empty hail frame " + frame);
            }
            // The isolated calls below must not consume any game random values.
            Game1.random = counter;
            var eligible = new object[] { true, true, true, true, false, false, false, false, true, false, false };
            Check((bool)Call("IsEligible", eligible), "Outdoor storm rejected.");
            for (int i = 0; i < eligible.Length; i++)
            {
                var blocked = (object[])eligible.Clone(); blocked[i] = !(bool)blocked[i];
                Check(!(bool)Call("IsEligible", blocked), "Disabled gate " + i + " enabled hail.");
            }
            uint seed = (uint)Call("StableSeed", 123L, 42, "Forest");
            Check(seed == (uint)Call("StableSeed", 123L, 42, "Forest"), "Seed is unstable.");
            int active = 0;
            for (int i = 0; i < 1200; i++)
            {
                bool on = (bool)Call("IsBurst", i / 10d, seed);
                if (on) active++;
                Check(on == (bool)Call("IsBurst", i / 10d + 120, seed), "Burst timing changed.");
            }
            Check(active == 120, "Burst duration differs from twelve game minutes.");
            foreach (var (age, frame) in new[] { (-1d, 0), (0d, 1), (.09d, 2), (.18d, 3), (.27d, -1) })
                Check((int)Call("FrameAt", age) == frame, "Wrong animation frame.");

            var stateType = type.GetNestedType("ScreenState", BindingFlags.NonPublic)!;
            var state = Activator.CreateInstance(stateType, true)!;
            var particles = (Array)stateType.GetField("Particles", Instance)!.GetValue(state)!;
            var particleType = particles.GetType().GetElementType()!;
            // A private map only: no map loading, location switching, save or net registration.
#pragma warning disable SYSLIB0050
            var fixture = (GameLocation)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(GameLocation));
#pragma warning restore SYSLIB0050
            fixture.map = new xTile.Map();
            var back = new xTile.Layers.Layer("Back", fixture.map, new xTile.Dimensions.Size(10, 10), new xTile.Dimensions.Size(16, 16));
            fixture.map.AddLayer(back);
            var tileSheet = new xTile.Tiles.TileSheet("hail-audit", fixture.map, "unused", new xTile.Dimensions.Size(1, 1), new xTile.Dimensions.Size(16, 16));
            fixture.map.AddTileSheet(tileSheet);
            for (int y = 0; y < 10; y++) for (int x = 0; x < 10; x++)
                back.Tiles[x, y] = new xTile.Tiles.StaticTile(back, tileSheet, xTile.Tiles.BlendMode.Alpha, 0);
            Game1.viewport = new xTile.Dimensions.Rectangle(0, 0, 320, 180);
            stateType.GetField("Random")!.SetValue(state, seed);
            Call("Spawn", state, fixture);
            object emitted = particles.GetValue(0)!;
            Check((bool)particleType.GetField("Active")!.GetValue(emitted)!, "Actual emission did not create a particle.");
            float startY = (float)particleType.GetField("Y")!.GetValue(emitted)!;
            Call("Advance", state, .05d);
            Check((float)particleType.GetField("Y")!.GetValue(particles.GetValue(0))! > startY, "Emitted hail did not move.");
            var seenFrames = new HashSet<int> { 0 };
            for (int step = 0; step < 200; step++)
            {
                Call("Advance", state, .01d);
                object current = particles.GetValue(0)!;
                if (!(bool)particleType.GetField("Active")!.GetValue(current)!) break;
                seenFrames.Add((int)Call("FrameAt", (double)particleType.GetField("ImpactAge")!.GetValue(current)!));
            }
            Check(seenFrames.SetEquals(new[] { 0, 1, 2, 3 }), "Movement did not visit all impact frames.");
            Check(!(bool)particleType.GetField("Active")!.GetValue(particles.GetValue(0))!, "Impact never expired.");
            Game1.viewport = gameViewport;
            var particle = Activator.CreateInstance(particleType)!;
            particleType.GetField("Active")!.SetValue(particle, true);
            particles.SetValue(particle, 0);
            Call("ApplyEligibility", state, false);
            Call("Advance", state, .1d);
            Check(!(bool)particleType.GetField("Active")!.GetValue(particles.GetValue(0))!, "Paused particles remain active.");
            Check(particles.Length == 24, "Particle cap changed.");

            using var target = new RenderTarget2D(device, 128, 64, false, SurfaceFormat.Color, DepthFormat.None);
            using var batch = new SpriteBatch(device);
            device.SetRenderTarget(target);
            device.Clear(Color.Transparent);
            bool begun = false;
            try
            {
                batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
                begun = true;
                for (int frame = 0; frame < 4; frame++)
                {
                    Check((bool)Call("DrawParticle", batch, sheet, 20f + frame * 28, 40f, frame, 128, 64), "Visible frame rejected.");
                    draws++;
                }
                foreach (var (x, y, frame) in new[] { (0f, 0f, 0), (128f, 64f, 1), (64f, 40f, -1), (64f, 40f, 4) })
                    Check(!(bool)Call("DrawParticle", batch, sheet, x, y, frame, 128, 64), "Out-of-bounds/invalid frame emitted.");
                batch.End(); begun = false;
            }
            finally { if (begun) batch.End(); device.SetRenderTargets(targets); }
            var rendered = new Color[128 * 64]; target.GetData(rendered);
            for (int frame = 0; frame < 4; frame++)
            {
                int count = 0;
                for (int y = 22; y < 46; y++) for (int x = 8 + frame * 28; x < 32 + frame * 28; x++)
                    if (rendered[y * 128 + x].A > 0) count++;
                Check(count > 0, "Frame drew no visible pixels: " + frame); pixels += count;
            }
            for (int y = 0; y < 64; y++) for (int x = 0; x < 128; x++)
            {
                bool inside = y >= 22 && y < 46 && Enumerable.Range(0, 4).Any(f => x >= 8 + f * 28 && x < 32 + f * 28);
                if (!inside) Check(rendered[y * 128 + x].A == 0, "Draw escaped its viewport/frame bounds.");
            }
            Check(counter.Calls == 0, "Hail consumed game RNG.");
            using var output = File.Create(Path.Combine(helper.DirectoryPath, "storm-hail-preview.png"));
            target.SaveAsPng(output, 128, 64);
        }
        catch (Exception ex) { error = ex.ToString(); }
        finally
        {
            Game1.random = random;
            Game1.viewport = gameViewport;
            device.SetRenderTargets(targets);
            device.Viewport = viewport;
            device.ScissorRectangle = scissor;
            device.BlendState = blend;
            device.BlendFactor = factor;
            device.DepthStencilState = depth;
            device.RasterizerState = raster;
            device.SamplerStates[0] = sampler;
            device.Textures[0] = texture;
            device.SetVertexBuffers(vertices);
            device.Indices = indices;
            restored = ReferenceEquals(Game1.random, random) && device.Viewport.Equals(viewport)
                && device.ScissorRectangle == scissor && device.GetRenderTargets().SequenceEqual(targets);
        }
        helper.Data.WriteJsonFile("storm-hail-checks.json", new
        {
            Passed = error == null && restored, Error = error, Restored = restored, Checks = checks,
            RenderedFrames = draws, RenderedOccupiedPixels = pixels, HailFrameOccupancy = occupied,
            GameRandomCalls = counter.Calls, Scale = 1.5,
            Scope = "Actual Spawn and Advance lifecycle on a private Back-layer fixture, four-frame DrawParticle method, plus pure eligibility, schedule and pause clearing. Does not force weather, invoke live event hooks, advance the native clock, or alter save state."
        });
        monitor.Log(error == null && restored ? $"Storm hail audit passed: {checks} checks, {draws} frames." : "Storm hail audit failed: " + error, error == null && restored ? LogLevel.Info : LogLevel.Error);
    }

    private sealed class CountingRandom : Random
    {
        public int Calls;
        public override int Next() { Calls++; return 0; }
        public override int Next(int maxValue) { Calls++; return 0; }
        public override int Next(int minValue, int maxValue) { Calls++; return minValue; }
        public override double NextDouble() { Calls++; return 0; }
        public override void NextBytes(byte[] buffer) { Calls++; Array.Clear(buffer, 0, buffer.Length); }
        public override void NextBytes(Span<byte> buffer) { Calls++; buffer.Clear(); }
    }
}
