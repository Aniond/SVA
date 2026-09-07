using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace AbigailModern;

/// <summary>Substitutes private 2x artwork at native farmer draw calls without changing logical assets.</summary>
internal static class PlayerHdRenderer
{
    public sealed class Manifest { public int Scale { get; set; } = 2; public Asset[] Assets { get; set; } = Array.Empty<Asset>(); }
    public sealed class Asset { public string Name { get; set; } = ""; public string File { get; set; } = ""; public bool Body { get; set; } public string? ShadeFile { get; set; } }
    private sealed class BodyCache
    {
        public Texture2D Logical = null!;
        public Texture2D Hd = null!;
        public Color[] Native = Array.Empty<Color>();
        public Color[] Output = Array.Empty<Color>();
        public Color[] Shade = Array.Empty<Color>();
    }
    private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly FieldInfo BaseTexture = AccessTools.Field(typeof(FarmerRenderer), "baseTexture");
    private static readonly FieldInfo SpriteDirty = AccessTools.Field(typeof(FarmerRenderer), "_spriteDirty");
    private static readonly Dictionary<string, Asset> assets = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<Texture2D, Texture2D> replacements = new();
    private static readonly ConditionalWeakTable<FarmerRenderer, BodyCache> bodies = new();
    private static readonly List<(WeakReference<FarmerRenderer> Owner, BodyCache Cache)> bodyOwners = new();
    private static readonly Dictionary<string, Texture2D> equipment = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> warned = new();
    private static IModHelper helper = null!;
    private static IMonitor monitor = null!;
    private static bool enabled;
    internal static int SubstitutedDraws { get; private set; }
    internal static int BodyRefreshes { get; private set; }
    internal static int PatchedCallSites { get; private set; }
    private static string Normalize(string value) => value.Replace('\\', '/');

    public static void Initialize(IModHelper modHelper, IMonitor log, string id)
    {
        helper = modHelper; monitor = log;
        var manifest = helper.Data.ReadJsonFile<Manifest>("player-hd.json");
        if (manifest == null) return;
        if (manifest.Scale != 2) throw new InvalidDataException("Player HD supports exactly 2x companions.");
        foreach (var asset in manifest.Assets)
        {
            if (!Normalize(asset.Name).StartsWith("Characters/Farmer/", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(asset.File) || (asset.Body && string.IsNullOrWhiteSpace(asset.ShadeFile)))
                throw new InvalidDataException("Invalid player HD manifest entry: " + asset.Name);
            assets.Add(Normalize(asset.Name), asset);
        }
        var harmony = new Harmony(id + ".PlayerHdRenderer");
        try
        {
            foreach (var method in typeof(FarmerRenderer).GetMethods(Flags).Where(m => m.DeclaringType == typeof(FarmerRenderer) && (m.Name == "draw" || m.Name == "drawHairAndAccesories" || m.Name == "drawMiniPortrat")))
                harmony.Patch(method, transpiler: new HarmonyMethod(typeof(PlayerHdRenderer), nameof(Transpile)));
            if (PatchedCallSites != 34) throw new InvalidOperationException($"Expected 34 farmer draw sites; found {PatchedCallSites}.");
            harmony.Patch(AccessTools.Method(typeof(FarmerRenderer), "executeRecolorActions"), prefix: new HarmonyMethod(typeof(PlayerHdRenderer), nameof(BeforeRecolor)), postfix: new HarmonyMethod(typeof(PlayerHdRenderer), nameof(AfterRecolor)));
            harmony.Patch(AccessTools.Method(typeof(FarmerRenderer), "textureChanged"), prefix: new HarmonyMethod(typeof(PlayerHdRenderer), nameof(ReleaseBody)));
            harmony.Patch(AccessTools.Method(typeof(FarmerRenderer), "unload"), prefix: new HarmonyMethod(typeof(PlayerHdRenderer), nameof(ReleaseBody)));
            enabled = true;
        }
        catch (Exception ex) { harmony.UnpatchAll(harmony.Id); monitor.Log("Player HD disabled: " + ex.Message, LogLevel.Error); }
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => Clear();
        helper.Events.Content.AssetsInvalidated += (_, e) => { if (e.NamesWithoutLocale.Any(n => n.Name.StartsWith("Characters/Farmer/", StringComparison.OrdinalIgnoreCase))) Clear(); };
    }
    private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
    {
        var original = AccessTools.Method(typeof(SpriteBatch), "Draw", new[] { typeof(Texture2D), typeof(Vector2), typeof(Rectangle?), typeof(Color), typeof(float), typeof(Vector2), typeof(float), typeof(SpriteEffects), typeof(float) });
        foreach (var instruction in instructions)
        {
            if (instruction.Calls(original)) { instruction.opcode = OpCodes.Call; instruction.operand = AccessTools.Method(typeof(PlayerHdRenderer), nameof(Draw)); PatchedCallSites++; }
            yield return instruction;
        }
    }
    private static void BeforeRecolor(FarmerRenderer __instance, out bool __state) => __state = (bool)SpriteDirty.GetValue(__instance)!;
    private static void AfterRecolor(FarmerRenderer __instance, bool __state)
    {
        if (!enabled || (!__state && bodies.TryGetValue(__instance, out _))) return;
        if (!assets.TryGetValue(Normalize(__instance.textureName.Value ?? ""), out var asset) || !asset.Body) return;
        try
        {
            var logical = (Texture2D)BaseTexture.GetValue(__instance)!;
            if (!bodies.TryGetValue(__instance, out var cache) || !ReferenceEquals(cache.Logical, logical))
            {
                ReleaseBody(__instance);
                // Reclaim dead renderers and bound retained body GPU caches even without unload callbacks.
                foreach (var entry in bodyOwners.ToArray())
                    if (!entry.Owner.TryGetTarget(out _) || bodyOwners.Count >= 64)
                    {
                        if (entry.Owner.TryGetTarget(out var owner)) bodies.Remove(owner);
                        replacements.Remove(entry.Cache.Logical); entry.Cache.Hd.Dispose(); bodyOwners.Remove(entry);
                    }
                var shade = helper.ModContent.Load<IRawTextureData>(asset.ShadeFile!);
                var art = helper.ModContent.Load<IRawTextureData>(asset.File);
                if (shade.Width != logical.Width * 2 || shade.Height != logical.Height * 2 || art.Width != shade.Width || art.Height != shade.Height) throw new InvalidDataException("Body companion dimensions do not match native 2x: " + asset.Name);
                cache = new BodyCache { Logical = logical, Hd = new Texture2D(logical.GraphicsDevice, shade.Width, shade.Height), Native = new Color[logical.Width * logical.Height], Output = new Color[shade.Width * shade.Height], Shade = shade.Data };
                bodies.Add(__instance, cache); bodyOwners.Add((new WeakReference<FarmerRenderer>(__instance), cache)); replacements[logical] = cache.Hd;
            }
            logical.GetData(cache.Native);
            ApplyShade(cache.Native, logical.Width, logical.Height, cache.Shade, cache.Output);
            cache.Hd.SetData(cache.Output); BodyRefreshes++;
        }
        catch (Exception ex) { ReleaseBody(__instance); Warn(asset.Name, ex); }
    }
    internal static void ApplyShade(Color[] native, int width, int height, Color[] shade, Color[] output)
    {
        if (native.Length != width * height || shade.Length != native.Length * 4 || output.Length != shade.Length) throw new InvalidDataException("Invalid HD shade buffer lengths.");
        for (int y = 0; y < height * 2; y++) for (int x = 0; x < width * 2; x++)
        {
            int at = y * width * 2 + x; Color color = native[(y / 2) * width + x / 2]; Color delta = shade[at];
            if (delta.R != delta.G || delta.R != delta.B) throw new InvalidDataException("HD shade pixels must have equal RGB channels.");
            int adjustment = delta.R - 128;
            output[at] = color.A == 0 ? color : new Color(Math.Clamp(color.R + adjustment, 0, 255), Math.Clamp(color.G + adjustment, 0, 255), Math.Clamp(color.B + adjustment, 0, 255), color.A);
        }
    }
    private static void Draw(SpriteBatch batch, Texture2D texture, Vector2 position, Rectangle? source, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float depth)
    {
        if (enabled)
        {
            if (!replacements.TryGetValue(texture, out var replacement))
            {
                string name = Normalize(texture.Name ?? "");
                if (assets.TryGetValue(name, out var asset) && !asset.Body)
                {
                    try
                    {
                        if (!equipment.TryGetValue(name, out replacement))
                        {
                            var data = helper.ModContent.Load<IRawTextureData>(asset.File);
                            if (data.Width != texture.Width * 2 || data.Height != texture.Height * 2) throw new InvalidDataException("Equipment companion dimensions do not match native 2x: " + name);
                            replacement = new Texture2D(texture.GraphicsDevice, data.Width, data.Height); replacement.SetData(data.Data); equipment[name] = replacement;
                        }
                        replacements[texture] = replacement;
                    }
                    catch (Exception ex) { Warn(name, ex); }
                }
            }
            if (replacement != null && !replacement.IsDisposed)
            {
                Rectangle rect = source ?? texture.Bounds;
                source = new Rectangle(rect.X * 2, rect.Y * 2, rect.Width * 2, rect.Height * 2); origin *= 2; scale /= 2; texture = replacement; SubstitutedDraws++;
            }
        }
        batch.Draw(texture, position, source, color, rotation, origin, scale, effects, depth);
    }
    private static void ReleaseBody(FarmerRenderer __instance)
    {
        if (bodies.TryGetValue(__instance, out var cache)) { bodies.Remove(__instance); bodyOwners.RemoveAll(e => ReferenceEquals(e.Cache, cache)); replacements.Remove(cache.Logical); cache.Hd.Dispose(); }
    }
    private static void Clear()
    {
        foreach (var entry in bodyOwners) entry.Cache.Hd.Dispose();
        foreach (var texture in equipment.Values) texture.Dispose();
        bodies.Clear(); bodyOwners.Clear(); replacements.Clear(); equipment.Clear();
    }
    private static void Warn(string name, Exception ex) { if (warned.Add(name)) monitor.Log("Using native player art for " + name + ": " + ex.Message, LogLevel.Warn); }
}
