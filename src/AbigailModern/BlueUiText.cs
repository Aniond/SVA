using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace AbigailModern;

/// <summary>Changes ink only during UI drawing; never writes the game's shared text colors.</summary>
internal static class BlueUiText
{
    [ThreadStatic] private static int scopeDepth;
    [ThreadStatic] private static int shadowHelperDepth;
    [ThreadStatic] private static int nativeShadowDepth;
    [ThreadStatic] private static string? surfaceContext;
    private static readonly HashSet<MethodBase> Patched = new();
    private static Harmony? harmony;
    private static IMonitor? monitor;

    public static void Initialize(IModHelper helper, IMonitor log, string id)
    {
        monitor = log;
        harmony = new Harmony(id + ".BlueUiText");
        PatchMenus(typeof(IClickableMenu).Assembly);
        try
        {
            harmony.Patch(AccessTools.Method(typeof(Toolbar), nameof(Toolbar.draw), new[] { typeof(SpriteBatch) }),
                transpiler: new HarmonyMethod(typeof(BlueUiText), nameof(ToolbarShortcutTranspiler)));
        }
        catch (Exception ex)
        {
            log.Log($"Could not center toolbar shortcuts: {ex.Message}", LogLevel.Warn);
        }
        foreach (var method in PortraitDrawMethods(typeof(BlueUiText).Assembly))
            Patch(method, nameof(EnterScope), nameof(ExitScope));
        foreach (var method in typeof(IClickableMenu).GetMethods(BindingFlags.Public | BindingFlags.Static)
                     .Where(m => m.Name is "drawHoverText" or "drawToolTip"))
            Patch(method, nameof(EnterScope), nameof(ExitScope));
        // DrawString is gated by a menu/tooltip scope, not by whether a menu happens to be open.
        foreach (var method in typeof(SpriteBatch).GetMethods().Where(m => m.Name == "DrawString" && m.GetParameters().Any(p => p.Name == "color")))
            Patch(method, nameof(DrawStringInk));
        foreach (var method in typeof(Utility).GetMethods(BindingFlags.Public | BindingFlags.Static)
                     .Where(m => m.Name is "drawTextWithShadow" or "drawTextWithColoredShadow"))
            Patch(method, method.Name == "drawTextWithShadow" ? nameof(DefaultShadowInk) : nameof(ShadowInk),
                method.Name == "drawTextWithShadow" ? nameof(ExitDefaultShadow) : nameof(ExitShadow));
        Patch(AccessTools.Method(typeof(SpriteText), "drawString"), nameof(BitmapInk), nameof(ExitScope));
        Patch(AccessTools.Method(typeof(SpriteBatch), "Draw", new[] { typeof(Texture2D), typeof(Rectangle), typeof(Color) }), nameof(SurfaceInk));
        foreach (var method in typeof(SpriteBatch).GetMethods().Where(m => m.Name == "Draw" && m.GetParameters().Any(p => p.Name == "sourceRectangle")))
            Patch(method, nameof(MoneyInk));
        helper.Events.GameLoop.GameLaunched += (_, _) =>
        {
            // SolaceWeather is loaded by this point; avoid changing unrelated third-party menu styles.
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name == "SolaceWeather"))
                RegisterSolaceUi(assembly);
            log.Log($"Blue UI ink installed on {Patched.Count} drawing methods.", LogLevel.Trace);
        };
    }

    // Replace the single shortcut call inside Toolbar.draw, leaving item draw/count calls intact.
    internal static IEnumerable<CodeInstruction> ToolbarShortcutTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var result = instructions.Select(instruction => new CodeInstruction(instruction)).ToList();
        var native = AccessTools.Method(typeof(SpriteBatch), nameof(SpriteBatch.DrawString),
            new[] { typeof(SpriteFont), typeof(string), typeof(Vector2), typeof(Color) });
        var calls = result.Where(instruction => instruction.Calls(native)).ToArray();
        if (calls.Length != 1)
            throw new InvalidOperationException($"Expected one native toolbar shortcut call; found {calls.Length}.");
        calls[0].opcode = OpCodes.Call;
        calls[0].operand = AccessTools.Method(typeof(BlueUiText), nameof(DrawToolbarShortcut));
        return result;
    }

    internal static Vector2 ToolbarShortcutPosition(Vector2 nativePosition, Vector2 measuredSize)
    {
        // Native input is slot+(4,-8); tinyFont's glyph cropping already supplies its top inset.
        return new Vector2(MathF.Round(nativePosition.X - 4 + (64 - measuredSize.X) / 2), nativePosition.Y + 2);
    }

    private static void DrawToolbarShortcut(SpriteBatch batch, SpriteFont font, string text, Vector2 position, Color color)
    {
        if (string.IsNullOrEmpty(text)) return;
        position = ToolbarShortcutPosition(position, font.MeasureString(text));
        float alpha = color.A / 255f;
        Color outline = new Color(8, 22, 35) * alpha;
        Color foreground = new Color(240, 246, 255) * alpha;
        int previousShadowDepth = shadowHelperDepth;
        shadowHelperDepth++;
        try
        {
            batch.DrawString(font, text, position + new Vector2(-1, 0), outline);
            batch.DrawString(font, text, position + new Vector2(1, 0), outline);
            batch.DrawString(font, text, position + new Vector2(0, -1), outline);
            batch.DrawString(font, text, position + new Vector2(0, 1), outline);
            batch.DrawString(font, text, position, foreground);
        }
        finally
        {
            shadowHelperDepth = previousShadowDepth;
        }
    }

    internal static void RegisterSolaceUi(Assembly assembly)
    {
        if (assembly.GetName().Name != "SolaceWeather") return;
        foreach (var method in CustomWidgetMethods(assembly))
            Patch(method, nameof(EnterCustomScope), nameof(ExitCustomScope));
        PatchMenus(assembly);
    }

    private static void MoneyInk(Texture2D texture, Rectangle? sourceRectangle, ref Color color)
    {
        if (scopeDepth > 0)
            color = MapMoneyInk(color, ReferenceEquals(texture, Game1.mouseCursors), sourceRectangle);
    }

    internal static Color MapMoneyInk(Color color, bool cursorTexture, Rectangle? source)
    {
        if (scopeDepth == 0 || !cursorTexture || source is not Rectangle r || color.A == 0
            || r.X != 286 || r.Width != 5 || r.Height != 8 || r.Y < 430 || r.Y > 502 || (502 - r.Y) % 8 != 0)
            return color;
        float alpha = color.A / 255f;
        if (Math.Abs(color.R / alpha - 128) > 2 || color.G != 0 || color.B != 0) return color;
        return new Color(255, 237, 191) * alpha;
    }

    private readonly record struct CustomScopeState(int Depth, string? Surface);

    private static IEnumerable<MethodInfo> CustomWidgetMethods(Assembly assembly)
    {
        (string Type, string Method)[] widgets = {
            ("SolaceWeather.Presentation.WeatherUi", "DrawHud"),
            ("SolaceWeather.Relationships.QuestTextRenderer", "DrawButton"),
            ("SolaceWeather.Relationships.QuestTextRenderer", "OverlayDialogue"),
            ("SolaceWeather.Relationships.RelationshipSocialEntry", "BeforeHeart"),
            ("SolaceWeather.Controls.ClickFeedback", "Draw"),
            ("SolaceWeather.Relationships.NpcConversation+ServiceChoicesMenu", "draw")
        };
        return widgets.SelectMany(widget => assembly.GetType(widget.Type)?.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.Name == widget.Method && method.GetParameters().Any(p => p.ParameterType == typeof(SpriteBatch)))
            ?? Enumerable.Empty<MethodInfo>());
    }

    private static void EnterCustomScope(MethodBase __originalMethod, out CustomScopeState __state)
    {
        __state = new CustomScopeState(scopeDepth, surfaceContext);
        scopeDepth++;
        surfaceContext = __originalMethod.DeclaringType?.Name;
    }

    private static Exception? ExitCustomScope(CustomScopeState __state, Exception? __exception)
    {
        scopeDepth = __state.Depth;
        surfaceContext = __state.Surface;
        return __exception;
    }

    private static void SurfaceInk(Texture2D texture, ref Color color)
    {
        if (surfaceContext == null || scopeDepth == 0) return;
        color = MapSurface(color, surfaceContext, ReferenceEquals(texture, Game1.staminaRect), ReferenceEquals(texture, Game1.fadeToBlackRect));
    }

    internal static Color MapSurface(Color color, string? context, bool stamina, bool fade)
    {
        // Exact helper, texture, and original palette matches prevent world/semantic recoloring.
        bool panel = false, hover = false;
        if (context == "QuestTextRenderer" && fade)
        {
            panel = color == new Color(250, 222, 171) || color == new Color(255, 238, 203);
            hover = color == new Color(255, 238, 203);
        }
        else if (context == "ServiceChoicesMenu" && stamina)
        {
            panel = color == new Color(33, 25, 43) || color == new Color(56, 41, 70) || color == new Color(88, 59, 113);
            hover = color == new Color(88, 59, 113);
            if (color == Color.Plum) return new Color(132, 205, 244);
        }
        else if (context == "RelationshipSocialEntry" && stamina && color == new Color(104, 63, 145))
            return new Color(132, 205, 244);
        else if (context == "ClickFeedback" && stamina)
            panel = color == Color.Black * .85f;
        if (!panel) return color;
        return (hover ? new Color(45, 91, 124) : new Color(20, 57, 86)) * Math.Min(color.A / 255f, 219 / 255f);
    }

    private static IEnumerable<MethodInfo> PortraitDrawMethods(Assembly assembly)
    {
        // These panels render in SMAPI display callbacks after the native menu scope has ended.
        // Limit the extra scope to known panel helpers; never scope an entire HUD/world event.
        string[] panels = { "PortraitPanel", "ChildPortraits", "TrashBearPortrait",
            "FishingContestantPortraits", "JunimoPortraits", "MermaidPortrait" };
        return panels.Select(name => assembly.GetType("AbigailModern." + name))
            .Where(type => type != null)
            .SelectMany(type => type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            .Where(method => method.Name == "Draw" && method.GetParameters().Any(p => p.ParameterType == typeof(SpriteBatch)));
    }

    private static void PatchMenus(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes().Where(t => typeof(IClickableMenu).IsAssignableFrom(t)))
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                         .Where(m => m.Name == "draw" && !m.IsAbstract && m.GetParameters().Any(p => p.ParameterType == typeof(SpriteBatch))))
                Patch(method, nameof(EnterScope), nameof(ExitScope));
    }

    private static void Patch(MethodBase? method, string prefix, string? finalizer = null)
    {
        if (method == null || Patched.Contains(method)) return;
        try
        {
            harmony!.Patch(method, prefix: new HarmonyMethod(typeof(BlueUiText), prefix),
                finalizer: finalizer == null ? null : new HarmonyMethod(typeof(BlueUiText), finalizer));
            Patched.Add(method);
        }
        catch (Exception ex) { monitor!.Log($"Blue UI ink could not attach to {method.DeclaringType?.Name}.{method.Name}: {ex.Message}", LogLevel.Warn); }
    }

    private static void EnterScope(out int __state)
    {
        __state = scopeDepth;
        scopeDepth++;
    }

    private static Exception? ExitScope(int __state, Exception? __exception)
    {
        scopeDepth = __state;
        return __exception;
    }

    private static void DrawStringInk(ref Color color)
    {
        if (scopeDepth == 0 || shadowHelperDepth > 0) return;
        if (nativeShadowDepth > 0)
        {
            // Native default helpers use these two orange shades; keep semantic colored shadows separate.
            if (color.A == 0) return;
            float alpha = color.A / 255f;
            int r = (int)Math.Round(color.R / alpha), g = (int)Math.Round(color.G / alpha), b = (int)Math.Round(color.B / alpha);
            if ((Math.Abs(r - 206) <= 2 && Math.Abs(g - 156) <= 2 && Math.Abs(b - 95) <= 2)
                || (Math.Abs(r - 221) <= 2 && Math.Abs(g - 148) <= 2 && Math.Abs(b - 84) <= 2))
                color = new Color(8, 22, 35) * alpha;
            return;
        }
        color = MapInk(color);
    }

    private static void DefaultShadowInk(ref Color color, out int __state)
    {
        color = MapInk(color);
        __state = nativeShadowDepth;
        nativeShadowDepth++;
    }

    private static Exception? ExitDefaultShadow(int __state, Exception? __exception)
    {
        nativeShadowDepth = __state;
        return __exception;
    }

    private static void ShadowInk(ref Color color, out int __state)
    {
        color = MapInk(color);
        __state = shadowHelperDepth;
        shadowHelperDepth++;
    }

    private static Exception? ExitShadow(int __state, Exception? __exception)
    {
        shadowHelperDepth = __state;
        return __exception;
    }

    private static void BitmapInk(ref Color? color, bool junimoText, int drawBGScroll, out int __state)
    {
        __state = scopeDepth;
        if (drawBGScroll is 0 or 2 or 3) scopeDepth++;
        if (scopeDepth == 0 || junimoText) return;
        // Supplying a color selects SpriteText.coloredTexture, the native tintable glyph atlas.
        color = MapInk(color ?? new Color(34, 17, 34));
    }

    internal static Color MapInk(Color color)
    {
        if (scopeDepth == 0 || color.A == 0) return color;
        // XNA uses premultiplied alpha. Compare the original shade so faded text stays faded.
        float alpha = color.A / 255f;
        int r = Math.Min(255, (int)Math.Round(color.R / alpha));
        int g = Math.Min(255, (int)Math.Round(color.G / alpha));
        int b = Math.Min(255, (int)Math.Round(color.B / alpha));
        int max = Math.Max(r, Math.Max(g, b));
        int min = Math.Min(r, Math.Min(g, b));
        if (r * .2126 + g * .7152 + b * .0722 > 190) return color;
        // SpriteText.color_Default is brown, while Game1.textColor is near-black purple.
        // Allow rounding from faded premultiplied colors without treating warning reds as body ink.
        if (Math.Abs(r - 86) <= 2 && Math.Abs(g - 22) <= 2 && Math.Abs(b - 12) <= 2)
            return new Color(232, 242, 255) * alpha;
        if (max - min <= 45)
            return (max >= 80 ? new Color(170, 196, 222) : new Color(232, 242, 255)) * alpha;
        // Keep hue for warnings, skill bonuses, prices, and unavailable actions while lifting contrast.
        return new Color((int)(r + (255 - r) * .55f), (int)(g + (255 - g) * .55f), (int)(b + (255 - b) * .55f)) * alpha;
    }
}
