using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace NpcArtAudit;

/// <summary>Bounded GPU fixtures; never loads, writes, or selects a save.</summary>
internal static class BlueUiAudit
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        if (Context.IsWorldReady || !Game1.IsOnMainThread() || Game1.game1 == null || Game1.game1.isDrawing || Game1.uiMode || Game1.uiModeCount != 0)
        {
            helper.Data.WriteJsonFile("blue-ui-checks.json", new { Passed = false, Deferred = true, Error = "Requires title-screen main-thread update outside drawing/UI mode, with no loaded world." });
            return;
        }
        var device = Game1.graphics.GraphicsDevice;
        var targets = device.GetRenderTargets(); var viewport = device.Viewport; var scissor = device.ScissorRectangle;
        var blend = device.BlendState; var factor = device.BlendFactor; var depth = device.DepthStencilState;
        var rasterizer = device.RasterizerState; var vertices = GetVertexBuffers(device); var indices = device.Indices;
        var slots = SaveSlots(device);
        var player = Game1.player; var menu = Game1.activeClickableMenu; var random = Game1.random;
        var location = Game1.currentLocation; var options = Game1.options;
        var gameViewport = Game1.viewport; var uiViewport = Game1.uiViewport;
        var cursorAlpha = Game1.mouseCursorTransparency;
        var originalHud = Game1.dayTimeMoneyBox;
        var checks = new Dictionary<string, bool>(); var cases = new List<object>();
        string? error = null;
        Type? ink = null; object[] inkState = { 0 }; bool inkEntered = false;
        try
        {
            ink = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "AbigailModern").GetType("AbigailModern.BlueUiText", true)!;
            ink.GetMethod("EnterScope", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, inkState); inkEntered = true;
            var mapInk = ink.GetMethod("MapInk", BindingFlags.Static | BindingFlags.NonPublic)!;
            Color Map(Color c) => (Color)mapInk.Invoke(null, new object[] { c })!;
            checks["LightBodyInk"] = Map(new Color(34, 17, 34)) == new Color(232, 242, 255);
            checks["SemanticWarningHue"] = Map(Color.Red).R > Map(Color.Red).G;
            checks["FadedInkPreservesAlpha"] = Map(new Color(34, 17, 34) * .5f).A == (new Color(34, 17, 34) * .5f).A;
            ink.GetMethod("ExitScope", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new object?[] { inkState[0], null }); inkEntered = false;
            checks["OutsideScopeInkUnchanged"] = Map(new Color(34, 17, 34)) == new Color(34, 17, 34);
            bool ScopeClear() => (int)ink.GetField("scopeDepth", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)! == 0;
            // Load only assemblies for their drawing methods: never instantiate the mod Entry.
            string fixtureDirectory = Path.Combine(helper.DirectoryPath, "solace-fixtures");
            Assembly.LoadFrom(Path.Combine(fixtureDirectory, "SolaceWeather.Core.dll"));
            var solaceAssembly = Assembly.LoadFrom(Path.Combine(fixtureDirectory, "SolaceWeather.dll"));
            ink.GetMethod("RegisterSolaceUi", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!.Invoke(null, new object[] { solaceAssembly });
            var questButtonDraw = solaceAssembly.GetType("SolaceWeather.Relationships.QuestTextRenderer", true)!
                .GetMethod("DrawButton", BindingFlags.Static | BindingFlags.NonPublic)!;
            Game1.random = new Random(71023);
            typeof(Game1).GetField("_player", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, new Farmer());
            Game1.player.Money = 12345678;
            Game1.options = (Options)typeof(object).GetMethod("MemberwiseClone", Flags)!.Invoke(options, null)!;
            Game1.options.gamepadControls = false;
            Game1.currentLocation = new GameLocation();
            var inventoryItems = new List<Item> { ItemRegistry.Create("(O)24", 12), ItemRegistry.Create("(O)388", 99), ItemRegistry.Create("(O)72") };
            while (inventoryItems.Count < 36) inventoryItems.Add(null!);
            using var batch = new SpriteBatch(device);
            using var pixel = new Texture2D(device, 1, 1); pixel.SetData(new[] { Color.White });
            var menuPixels = new Color[Game1.menuTexture.Width * Game1.menuTexture.Height];
            Game1.menuTexture.GetData(menuPixels);
            checks["MenuTextureContainsTranslucentBlue"] = menuPixels.Count(c => c.A > 32 && c.A < 250 && c.B > c.R + 15) > 100;
            Color[] Render(string name, Color backdrop, int width = 1280, int height = 720, float scale = 1f, bool content = true, bool optionsPage = false, bool customButton = false)
            {
                using var target = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.None);
                device.SetRenderTarget(target); device.Clear(backdrop);
                Game1.uiViewport = new xTile.Dimensions.Rectangle(0, 0, (int)(width / scale), (int)(height / scale));
                Game1.viewport = Game1.uiViewport;
                var inventory = new InventoryMenu(56, 170, false, inventoryItems, i => i.QualifiedItemId != "(O)72", 36, 3);
                Game1.activeClickableMenu = inventory;
                checks["InventoryControllerNeighbors"] = inventory.inventory[0].rightNeighborID == 1 && inventory.inventory[0].downNeighborID == 12 && inventory.inventory[13].upNeighborID == 1;
                batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.CreateScale(scale));
                if (!optionsPage && !customButton) IClickableMenu.drawTextureBox(batch, 32, 32, 816, 410, Color.White);
                if (customButton)
                {
                    questButtonDraw.Invoke(null, new object[] { batch, new Rectangle(90, 140, 600, 60), "Bring a Parsnip", new[] { "Parsnip" } });
                    checks["CustomButtonScopeRestored"] = ScopeClear();
                    checks["CustomButtonSurfaceScopeRestored"] = ink.GetField("surfaceContext", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null) == null;
                }
                else if (optionsPage)
                {
                    var page = new OptionsPage(40, 20, 1050, 650);
                    Game1.activeClickableMenu = page;
                    IClickableMenu.drawTextureBox(batch, 20, 10, 1180, 690, Color.White);
                    page.draw(batch);
                    checks["OptionsScopeRestored"] = ScopeClear();
                }
                else if (content)
                {
                    batch.DrawString(Game1.dialogueFont, "Inventory", new Vector2(64, 60), new Color(232, 242, 255));
                    batch.DrawString(Game1.smallFont, "Items / quality / unavailable / focus", new Vector2(64, 116), new Color(232, 242, 255));
                    inventory.draw(batch);
                    checks["InventoryScopeRestored"] = ScopeClear();
                    // A native slot focus marker, without moving the operating system mouse.
                    batch.Draw(Game1.menuTexture, new Rectangle(52, 162, 72, 72), Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 56), Color.White);
                    IClickableMenu.drawHoverText(batch, "A fresh crop. Ready to share.", Game1.smallFont, boldTitleText: "Parsnip", overrideX: 864, overrideY: 310);
                    checks["TooltipScopeRestored"] = ScopeClear();
                    var dialogue = new DialogueBox(40, 485, 1160, 170) { transitioning = false, transitionInitialized = true, showTyping = false, characterIndexInDialogue = 99999 };
                    dialogue.dialogues.Add("Welcome home. The town has missed you!");
                    Game1.activeClickableMenu = dialogue; dialogue.draw(batch);
                    checks["DialogueScopeRestored"] = ScopeClear();
                    var hud = new DayTimeMoneyBox(); Game1.dayTimeMoneyBox = hud; hud.draw(batch);
                    checks["HudScopeRestored"] = ScopeClear();
                }
                batch.End(); device.SetRenderTarget(null);
                var pixels = new Color[width * height]; target.GetData(pixels);
                if (customButton)
                {
                    var fill = pixels.Where((c, i) => i / width >= 150 && i / width < 190 && i % width >= 500 && i % width < 660).ToArray();
                    checks["CustomButtonBlueBackground"] = fill.Count(c => c != backdrop && c.B > c.R + 15) > fill.Length * .9;
                    var text = pixels.Where((c, i) => i / width >= 153 && i / width < 184 && i % width >= 104 && i % width < 360).ToArray();
                    checks["CustomButtonLightText"] = text.Count(c => c.R >= 210 && c.G >= 220 && c.B >= 230 && c != backdrop) > 30;
                    checks["CustomButtonItemHighlight"] = text.Count(c => c.B > c.G + 20 && c.R > c.G + 10 && c.R > 140) > 20;
                }
                if (content && scale == 1f && !optionsPage && !customButton)
                {
                    // Sample below the single text line and inside the native box edges. A bare
                    // bright backdrop must never count as a successful light-text fixture.
                    var interior = pixels.Where((c, i) => i / width >= 570 && i / width < 610 && i % width >= 80 && i % width < 1100).ToArray();
                    checks["NativeDialogueBlueBackground-" + name] = interior.Count(c => c != backdrop && c.B > c.R + 15) > interior.Length * .9;
                    var glyphs = pixels.Where((c, i) => i / width >= 495 && i / width < 540 && i % width >= 60 && i % width < 900)
                        .Where(c => c.R >= 210 && c.G >= 220 && c.B >= 230 && c != backdrop).ToArray();
                    double backgroundLuminance = interior.Average(Luminance);
                    checks["NativeDialogueLightInkContrast-" + name] = glyphs.Length > 120
                        && (glyphs.Average(Luminance) + .05) / (backgroundLuminance + .05) >= 4.5;
                    // Eight stable digits avoid a zero/rolling-number fixture. Exclude money panel edges.
                    checks["NativeMoneyDigitsLight-" + name] = pixels.Where((c, i) => i / width >= 206 && i / width < 232 && i % width >= 1050 && i % width < 1234)
                        .Count(c => c.R >= 190 && c.G >= 190 && c.B >= 190 && c != backdrop) > 120;
                }
                using (var stream = File.Create(Path.Combine(helper.DirectoryPath, "blue-ui-" + name + ".png"))) target.SaveAsPng(stream, width, height);
                cases.Add(new { Name = name, Width = width, Height = height, DrawScale = scale, NativeInventory = content && !optionsPage && !customButton, NativeTooltip = content && !optionsPage && !customButton, NativeDialogue = content && !optionsPage && !customButton, NativeHud = content && !optionsPage && !customButton, NativeOptions = optionsPage, CustomQuestButton = customButton, ChangedPixels = pixels.Count(c => c != backdrop) });
                return pixels;
            }
            var dark = Render("dark", new Color(12, 20, 30));
            var bright = Render("bright", new Color(245, 236, 196));
            var panelDark = Render("panel-dark", new Color(12, 20, 30), content: false);
            var panelBright = Render("panel-bright", Color.White, content: false);
            int sample = 220 * 1280 + 420;
            checks["PanelBackdropTransmits"] = panelBright[sample].R > panelDark[sample].R + 3 && panelBright[sample].R < 250;
            checks["PanelBlueOnDark"] = panelDark[sample].B > panelDark[sample].R;
            checks["NativeWidgetsDrawVisiblePixels"] = dark.Where((c, i) => c != panelDark[i]).Count() > 2000;
            checks["BrightAndDarkAreDistinct"] = !dark.SequenceEqual(bright);
            Render("scaled-125", new Color(100, 145, 110), 1600, 900, 1.25f);
            Render("compact", new Color(30, 45, 70), 1024, 576, .8f);
            Render("options", new Color(30, 45, 70), optionsPage: true);
            Render("solace-quest-button", new Color(245, 236, 196), customButton: true);
            checks["BoundedFixtureMatrixCompleted"] = cases.Count == 8;
        }
        catch (Exception ex) { error = ex.ToString(); }
        finally
        {
            try { if (inkEntered) ink!.GetMethod("ExitScope", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new object?[] { inkState[0], null }); }
            catch (Exception ex) { error = (error ?? "") + " Ink scope restoration failed: " + ex; }
            typeof(Game1).GetField("_player", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, player); Game1.activeClickableMenu = menu; Game1.random = random; Game1.options = options;
            Game1.dayTimeMoneyBox = originalHud;
            Game1.currentLocation = location; Game1.viewport = gameViewport; Game1.uiViewport = uiViewport; Game1.mouseCursorTransparency = cursorAlpha;
            device.SetRenderTargets(targets); device.Viewport = viewport; device.ScissorRectangle = scissor;
            device.BlendState = blend; device.BlendFactor = factor; device.DepthStencilState = depth; device.RasterizerState = rasterizer;
            device.SetVertexBuffers(vertices); device.Indices = indices; foreach (var slot in slots) slot.Restore();
            checks["GameAndGraphicsRestored"] = ReferenceEquals(Game1.player, player) && ReferenceEquals(Game1.options, options)
                && ReferenceEquals(Game1.activeClickableMenu, menu) && ReferenceEquals(Game1.dayTimeMoneyBox, originalHud) && ReferenceEquals(Game1.random, random) && ReferenceEquals(Game1.currentLocation, location)
                && Game1.viewport.Equals(gameViewport) && Game1.uiViewport.Equals(uiViewport) && Game1.mouseCursorTransparency == cursorAlpha
                && device.GetRenderTargets().SequenceEqual(targets) && device.Viewport.Equals(viewport) && device.ScissorRectangle == scissor
                && ReferenceEquals(device.BlendState, blend) && device.BlendFactor == factor && ReferenceEquals(device.DepthStencilState, depth)
                && ReferenceEquals(device.RasterizerState, rasterizer) && ReferenceEquals(device.Indices, indices) && GetVertexBuffers(device).SequenceEqual(vertices) && slots.All(s => s.Matches());
        }
        bool passed = error == null && checks.Count >= 8 && checks.Values.All(v => v);
        helper.Data.WriteJsonFile("blue-ui-checks.json", new { Passed = passed, Error = error, Checks = checks, Cases = cases,
            Scope = "GPU draw of native InventoryMenu, hover tooltip, non-speaker DialogueBox, DayTimeMoneyBox, OptionsPage and actual SolaceWeather QuestTextRenderer.DrawButton loaded without its mod Entry; isolated temporary Farmer and cloned Options. Rendering uses native Harmony scopes without a manual outer scope. No save loads/writes. Eight sequential render targets, at most 1600x900 each.",
            Limitations = new[] { "Draw matrix scales do not change real window size or native UI-scale options.", "Controller neighbor links are checked; no physical controller input or OS cursor movement is simulated.", "No shop, crafting, relationship, mod menu or NPC portrait interaction coverage.", "Visual/text readability, semantic icon recognizability and all unrendered atlas regions still require manual review; Passed is fixture-only." } });
        monitor.Log("Blue UI GPU fixture " + (passed ? "passed." : "FAILED: " + error), passed ? LogLevel.Info : LogLevel.Error);
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

