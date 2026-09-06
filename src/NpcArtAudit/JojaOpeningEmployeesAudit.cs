using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Minigames;

namespace NpcArtAudit;

// DRAFT: coordinator owns compilation, registration and running this fixture.
// Source: decompiled StardewValley.Minigames/GrandpaStory.cs, draw lines 425-452.
internal static class JojaOpeningEmployeesAudit
{
    public sealed class Evidence
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int[][] EmployeePixels { get; set; } = Array.Empty<int[]>();
        public int[][] GrandpaPixels { get; set; } = Array.Empty<int[]>();
        public Patch[] Patches { get; set; } = Array.Empty<Patch>();
        public Group[] Groups { get; set; } = Array.Empty<Group>();
    }
    public sealed class Patch
    {
        public string Id { get; set; } = "";
        public int[] Rectangle { get; set; } = Array.Empty<int>();
        public int MaskPixels { get; set; }
    }
    public sealed class Group
    {
        public string Id { get; set; } = "";
        public bool Pan { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Count { get; set; }
        public int Duration { get; set; }
        public int DestinationX { get; set; }
        public int DestinationY { get; set; }
        public int DistinctFrames { get; set; }
        public int NativeDistinctFrames { get; set; }
        public int[] OpaquePixels { get; set; } = Array.Empty<int>();
    }

    public static void Run(IModHelper helper, IMonitor monitor)
    {
        // Invoke once from an update/console callback on the game thread, never a draw callback.
        // UI mode switching in native draw can redirect render targets while isDrawing is true.
        if (!Game1.IsOnMainThread() || Game1.game1 == null || Game1.game1.isDrawing
            || Game1.uiMode || Game1.uiModeCount != 0)
        {
            helper.Data.WriteJsonFile("joja-opening-checks.json", new { Passed = false, Deferred = true, Error = "Run on the game thread outside drawing and UI mode." });
            monitor.Log("Joja employee fixture deferred: requires a game-thread update outside drawing/UI mode.", LogLevel.Warn);
            return;
        }
        var device = Game1.graphics.GraphicsDevice;
        var oldTargets = device.GetRenderTargets();
        var oldDeviceViewport = device.Viewport;
        var oldScissor = device.ScissorRectangle;
        var oldBlend = device.BlendState;
        var oldDepth = device.DepthStencilState;
        var oldRasterizer = device.RasterizerState;
        var oldSampler = device.SamplerStates[0];
        var oldTexture = device.Textures[0];
        var oldViewport = Game1.viewport;
        var oldUiViewport = Game1.uiViewport;
        var oldUiMode = Game1.uiMode;
        var oldUiCount = Game1.uiModeCount;
        var oldNonUiTarget = Game1.nonUIRenderTarget;
        var checkedPixels = 0;
        var states = 0;
        var results = new List<object>();
        Exception? failure = null;
        try
        {
            var evidence = helper.Data.ReadJsonFile<Evidence>("joja-opening-runtime-evidence.json")
                ?? throw new Exception("Missing Joja runtime evidence pack.");
            if (evidence.Width != 1200 || evidence.Height != 800 || evidence.EmployeePixels.Length != 3262
                || evidence.GrandpaPixels.Length != 3650 || evidence.Patches.Length != 46
                || evidence.Groups.Length != 8 || evidence.Groups.Sum(g => g.Count) != 27)
                throw new Exception("Joja evidence dimensions/counts differ from reviewed handoff.");
            using var content = Game1.content.CreateTemporary();
            var texture = content.Load<Texture2D>("Minigames/jojacorps");
            if (texture.Width != 1200 || texture.Height != 800) throw new Exception("Joja atlas dimensions differ.");
            var pixels = new Color[1200 * 800];
            texture.GetData(pixels);
            void CheckExpected(int[][] expected, string label)
            {
                foreach (var p in expected)
                {
                    if (p.Length != 5 || p[0] < 0 || p[0] >= pixels.Length || p[4] != 255)
                        throw new Exception("Invalid opaque evidence pixel: " + label);
                    if (pixels[p[0]] != new Color(p[1], p[2], p[3], p[4]))
                        throw new Exception($"{label} differs at atlas {p[0] % 1200},{p[0] / 1200}.");
                }
            }
            CheckExpected(evidence.EmployeePixels, "Prepared employee mask");
            CheckExpected(evidence.GrandpaPixels, "Retained Grandpa integration");
            foreach (var patch in evidence.Patches)
            {
                var r = patch.Rectangle;
                if (r.Length != 4 || r[0] < 0 || r[1] < 0 || r[0] + r[2] > 1200 || r[1] + r[3] > 800 || patch.MaskPixels <= 0)
                    throw new Exception("Invalid employee patch: " + patch.Id);
                var count = evidence.EmployeePixels.Count(p => p[0] % 1200 >= r[0] && p[0] % 1200 < r[0] + r[2]
                    && p[0] / 1200 >= r[1] && p[0] / 1200 < r[1] + r[3]);
                if (count != patch.MaskPixels) throw new Exception("Patch mask occupancy differs: " + patch.Id);
            }

            // Skip constructor, which changes music, moves the player and loads a farmhouse.
            // Never call tick/unload or install this object as Game1.currentMinigame.
#pragma warning disable SYSLIB0050
            var story = (GrandpaStory)FormatterServices.GetUninitializedObject(typeof(GrandpaStory));
#pragma warning restore SYSLIB0050
            void Set<T>(string key, T value) => helper.Reflection.GetField<T>(story, key).SetValue(value);
            Set("texture", texture);
            Set("grandpaSpeech", new Queue<string>());
            Set("drawGrandpa", false);
            Set("scene", 4);
            Set("mouseActive", false);
            Set("foregroundFade", 0f);
            Set("backgroundFade", 0f);
            Game1.viewport = new xTile.Dimensions.Rectangle(0, 0, 1294, 730);
            Directory.CreateDirectory(Path.Combine(helper.DirectoryPath, "joja-opening-runtime-previews"));
            foreach (var g in evidence.Groups)
            {
                if (g.Count <= 0 || g.Duration <= 0 || g.OpaquePixels.Length != g.Count)
                    throw new Exception("Invalid timing evidence: " + g.Id);
                var distinct = new HashSet<string>();
                for (var f = 0; f < g.Count; f++)
                {
                    var frameBytes = new List<byte>();
                    for (var y = 0; y < g.Height; y++) for (var x = 0; x < g.Width; x++)
                    {
                        var c = pixels[(g.Y + y) * 1200 + g.X + f * g.Width + x];
                        if (c.A != 255) throw new Exception("Native employee overlay became transparent: " + g.Id);
                        frameBytes.AddRange(new[] { c.R, c.G, c.B, c.A });
                    }
                    if (g.OpaquePixels[f] != g.Width * g.Height) throw new Exception("Overlay occupancy evidence differs: " + g.Id);
                    distinct.Add(Convert.ToBase64String(frameBytes.ToArray()));
                }
                if (distinct.Count != g.DistinctFrames || distinct.Count != g.NativeDistinctFrames)
                    throw new Exception("Animation phase diversity differs: " + g.Id);
                var times = Enumerable.Range(0, g.Count).SelectMany(f => new[] { f * g.Duration, (f + 1) * g.Duration - 1 })
                    .Concat(new[] { g.Count * g.Duration, (g.Count + 1) * g.Duration - 1 }).ToArray();
                var scale = g.Pan ? 4 : 3;
                var panX = g.Pan ? 128 - g.DestinationX * 4 : 0;
                // Scene 4 at 7000 ms: overview opacity 1, pan opacity 0.
                // At 9000 ms: overview opacity 0, pan opacity 1, same employee draw calls as scene 5.
                // Keeping scene=4 explicitly skips Game1.player.draw.
                Set("grandpaSpeechTimer", g.Pan ? 9000 : 7000);
                Set("panX", (float)panX);
                var origin = g.Pan ? new Vector2(panX, 5) : Utility.getTopLeftPositionForCenteringOnScreen(Game1.viewport, 1294, 730);
                foreach (var time in times)
                {
                    Set("totalMilliseconds", time);
                    var f = time % (g.Count * g.Duration) / g.Duration;
                    using var target = new RenderTarget2D(device, 1294, 730);
                    using var batch = new SpriteBatch(device);
                    device.SetRenderTarget(target);
                    device.Clear(Color.Magenta);
                    try { story.draw(batch); }
                    finally { device.SetRenderTargets(oldTargets); }
                    var rendered = new Color[1294 * 730];
                    target.GetData(rendered);
                    for (var y = 0; y < g.Height; y++) for (var x = 0; x < g.Width; x++)
                    {
                        var expected = pixels[(g.Y + y) * 1200 + g.X + f * g.Width + x];
                        var sx = (int)origin.X + (g.DestinationX + x) * scale + scale / 2;
                        var sy = (int)origin.Y + (g.DestinationY + y) * scale + scale / 2;
                        if (rendered[sy * 1294 + sx] != expected)
                            throw new Exception($"Native draw mismatch {g.Id}, time={time}, frame={f}, local={x},{y}.");
                        checkedPixels++;
                    }
                    if (time < g.Count * g.Duration && time % g.Duration == 0)
                    {
                        using var file = File.Create(Path.Combine(helper.DirectoryPath, "joja-opening-runtime-previews", $"{g.Id}-{f}.png"));
                        target.SaveAsPng(file, 1294, 730);
                    }
                    states++;
                }
                results.Add(new { g.Id, g.Count, g.Duration, g.DistinctFrames, Times = times });
            }
        }
        catch (Exception ex) { failure = ex; }
        finally
        {
            // Native UI push/pop normally balances; restore exact entry state even after an exception.
            Game1.uiMode = oldUiMode;
            Game1.uiModeCount = oldUiCount;
            Game1.nonUIRenderTarget = oldNonUiTarget;
            Game1.viewport = oldViewport;
            Game1.uiViewport = oldUiViewport;
            device.SetRenderTargets(oldTargets);
            device.Viewport = oldDeviceViewport;
            device.ScissorRectangle = oldScissor;
            device.BlendState = oldBlend;
            device.DepthStencilState = oldDepth;
            device.RasterizerState = oldRasterizer;
            device.SamplerStates[0] = oldSampler;
            device.Textures[0] = oldTexture;
        }
        helper.Data.WriteJsonFile("joja-opening-checks.json", new
        {
            Passed = failure == null, Error = failure?.ToString(), NativeDrawStates = states,
            NativeOverlayPixelsCompared = checkedPixels, EmployeeMaskPixels = 3262, RetainedGrandpaPixels = 3650,
            NativeTemporaryContentLoaded = failure == null, Groups = results,
            ConstructorSkipped = true, TickSkipped = true, PlayerDrawSkipped = true,
            FullScenePlayback = false, Scene5PlayerPlayback = false, StateRestored = true
        });
        monitor.Log(failure == null ? "Joja employee masks and native overlay fixtures passed; story playback is unverified."
            : "Joja employee audit failed: " + failure, failure == null ? LogLevel.Info : LogLevel.Error);
    }
}
