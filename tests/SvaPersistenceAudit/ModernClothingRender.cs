using System.Reflection;
using System.Text.Json;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace SvaPersistenceAudit;

public sealed partial class ModEntry
{
    private static SpriteBatch? clothingAuditBatch;
    private static int clothingDrawCalls, clothingCustomCalls, clothingBadSources;
    private static readonly List<object> clothingRequests = new();
    private static readonly HashSet<Texture2D> clothingLogicalTextures = new();
    private static Dictionary<Texture2D, Texture2D>? clothingHdReplacements;
    private static void ObserveClothingDraw(SpriteBatch __instance, Texture2D texture, Rectangle? sourceRectangle,
        Vector2 origin, float scale)
    {
        if (!ReferenceEquals(__instance, clothingAuditBatch)) return;
        clothingDrawCalls++;
        var rect = sourceRectangle ?? texture.Bounds;
        if (!texture.Bounds.Contains(rect) || rect.Width <= 0 || rect.Height <= 0 || !float.IsFinite(scale)
            || !float.IsFinite(origin.X) || !float.IsFinite(origin.Y)) clothingBadSources++;
        // HD companions are constructed Texture2D instances and need not have a Name.
        // Follow the production adapter's exact logical-to-HD references, without naming heuristics.
        var logical = clothingLogicalTextures.FirstOrDefault(t => ReferenceEquals(t, texture)
            || clothingHdReplacements?.TryGetValue(t, out var hd) == true && ReferenceEquals(hd, texture));
        if (logical != null)
        {
            clothingCustomCalls++;
            if (clothingRequests.Count < 32) clothingRequests.Add(new { LogicalAsset = logical.Name, texture.Name, texture.Width, texture.Height, Source = rect, Origin = origin, Scale = scale, Hd = !ReferenceEquals(logical, texture) });
        }
    }

    private void ModernClothingRender()
    {
        if (Context.IsWorldReady || !Game1.IsOnMainThread() || Game1.game1.isDrawing)
            throw new InvalidOperationException("Clothing rendering requires an isolated title update, without a loaded save.");
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        var hd = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("AbigailModern.PlayerHdRenderer")).First(t => t != null)!;
        var artHelper = (IModHelper)hd.GetField("helper", flags)!.GetValue(null)!;
        using var catalog = JsonDocument.Parse(File.ReadAllText(Path.Combine(artHelper.DirectoryPath, "assets/modern-clothing.json")));
        var items = catalog.RootElement.GetProperty("Items").EnumerateArray().ToArray();
        var shirts = items.Where(i => i.GetProperty("Slot").GetString() == "shirt").ToArray();
        var pants = items.Where(i => i.GetProperty("Slot").GetString() == "pants").ToArray();
        if (shirts.Length != 6 || pants.Length != 6) throw new InvalidOperationException("Expected six complete modern looks.");
        var device = Game1.graphics.GraphicsDevice;
        var targets = device.GetRenderTargets(); var viewport = device.Viewport; var scissor = device.ScissorRectangle;
        var blend = device.BlendState; var factor = device.BlendFactor; var depth = device.DepthStencilState; var raster = device.RasterizerState;
        var indices = device.Indices;
        var bindings = typeof(GraphicsDevice).GetField("_vertexBuffers", flags)!.GetValue(device)!;
        var vertices = (VertexBufferBinding[])bindings.GetType().GetMethod("Get", flags, null, Type.EmptyTypes, null)!.Invoke(bindings, null)!;
        var slots = new List<Action>();
        foreach (string name in new[] { "Textures", "SamplerStates", "VertexTextures", "VertexSamplerStates" })
        {
            var collection = typeof(GraphicsDevice).GetProperty(name)!.GetValue(device)!;
            var indexer = collection.GetType().GetProperty("Item")!;
            for (int i = 0; i < 32; i++)
            {
                object[] index = { i }; object? value;
                try { value = indexer.GetValue(collection, index); }
                catch (TargetInvocationException ex) when (ex.InnerException is IndexOutOfRangeException or ArgumentOutOfRangeException) { break; }
                slots.Add(() => indexer.SetValue(collection, value, index));
            }
        }
        var nativeBatch = Game1.spriteBatch; var original = Game1.player; var random = Game1.random;
        string Appearance(Farmer? f) => f == null ? "none" : $"{f.IsMale}|{f.hair.Value}|{f.skin.Value}|{f.FacingDirection}|{f.shirtItem.Value?.QualifiedItemId}|{f.shirtItem.Value?.clothesColor.Value}|{f.pantsItem.Value?.QualifiedItemId}|{f.pantsItem.Value?.clothesColor.Value}|{f.accessory.Value}|{f.hat.Value?.QualifiedItemId}";
        string originalAppearance = Appearance(original);
        var recolor = FarmerRenderer.recolorOffsets; bool ui = FarmerRenderer.isDrawingForUI;
        var owned = new List<Farmer>(); var checks = new Dictionary<string, bool>(); var cases = new List<object>();
        var routes = new List<object>();
        string? error = null;
        var harmony = new Harmony("SvaPersistenceAudit.ModernClothingRender");
        var drawMethod = typeof(SpriteBatch).GetMethod("Draw", new[] { typeof(Texture2D), typeof(Vector2), typeof(Rectangle?), typeof(Color), typeof(float), typeof(Vector2), typeof(float), typeof(SpriteEffects), typeof(float) })!;
        int hdStart = (int)hd.GetProperty("SubstitutedDraws", flags)!.GetValue(null)!;
        clothingDrawCalls = clothingCustomCalls = clothingBadSources = 0; clothingRequests.Clear();
        clothingLogicalTextures.Clear();
        clothingHdReplacements = (Dictionary<Texture2D, Texture2D>)hd.GetField("replacements", flags)!.GetValue(null)!;
        try
        {
            harmony.Patch(drawMethod, prefix: new HarmonyMethod(typeof(ModEntry), nameof(ObserveClothingDraw)));
            Game1.random = new Random(63817); FarmerRenderer.recolorOffsets = new(); FarmerRenderer.isDrawingForUI = false;
            using var batch = new SpriteBatch(device); Game1.spriteBatch = clothingAuditBatch = batch;
            Color Default(JsonElement item) { var rgb = item.GetProperty("DefaultColor").GetString()!.Split(' ').Select(byte.Parse).ToArray(); return new Color(rgb[0], rgb[1], rgb[2]); }
            Farmer Make(int body, int look, int dye)
            {
                var f = original?.CreateFakeEventFarmer() ?? new Farmer(); owned.Add(f);
                typeof(Game1).GetField("_player", flags)!.SetValue(null, f);
                f.changeGender(body == 0); f.changeHairStyle(body == 0 ? 0 : 16); f.changeAccessory(-1); f.hat.Value = null;
                f.swimming.Value = false; f.isSitting.Value = false;
                string shirtId = shirts[look].GetProperty("Id").GetString()!;
                string pantsId = pants.Single(p => p.GetProperty("Id").GetString() == shirtId.Replace("_Shirt", "_Pants")).GetProperty("Id").GetString()!;
                f.shirt.Value = "-1"; f.pants.Value = "-1";
                f.shirtItem.Value = ItemRegistry.Create<Clothing>("(S)" + shirtId);
                f.pantsItem.Value = ItemRegistry.Create<Clothing>("(P)" + pantsId);
                var pantData = pants.Single(p => p.GetProperty("Id").GetString() == pantsId);
                var color = dye switch { 1 => Color.White, 2 => new Color(30, 35, 45), 3 => new Color(220, 40, 145), _ => Default(shirts[look]) };
                f.shirtItem.Value.clothesColor.Value = color;
                f.pantsItem.Value.clothesColor.Value = dye == 0 ? Default(pantData) : color;
                f.UpdateClothing();
                f.FarmerRenderer.MarkSpriteDirty();
                f.GetDisplayShirt(out var st, out int shirtIndex); f.GetDisplayPants(out var pt, out int pantsIndex);
                bool Route(string qualified, JsonElement entry, Texture2D actual, int actualIndex)
                {
                    var data = ItemRegistry.GetDataOrErrorItem(qualified);
                    var expected = data.GetTexture();
                    string expectedName = entry.GetProperty("Texture").GetString()!;
                    int expectedIndex = entry.GetProperty("SpriteIndex").GetInt32();
                    bool valid = !data.IsErrorItem && data.TextureName.Replace('\\', '/') == expectedName
                        && data.SpriteIndex == expectedIndex && actualIndex == expectedIndex && ReferenceEquals(actual, expected);
                    if (valid) clothingLogicalTextures.Add(actual);
                    routes.Add(new { QualifiedId = qualified, Body = body, Dye = dye, Valid = valid, data.IsErrorItem,
                        ExpectedAsset = expectedName, ActualAsset = data.TextureName, TextureObjectName = actual.Name,
                        ExpectedIndex = expectedIndex, ActualIndex = actualIndex, NativeIndex = data.SpriteIndex,
                        actual.Width, actual.Height, ExpectedWidth = expected.Width, ExpectedHeight = expected.Height,
                        SameNativeTexture = ReferenceEquals(actual, expected) });
                    return valid;
                }
                bool shirtRoute = Route("(S)" + shirtId, shirts[look], st, shirtIndex);
                bool pantsRoute = Route("(P)" + pantsId, pantData, pt, pantsIndex);
                checks[$"look{look}-body{body}-dye{dye}-custom-route"] = shirtRoute && pantsRoute;
                return f;
            }
            void Render(string name, int width, int height, Action draw)
            {
                using var target = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.None);
                device.SetRenderTarget(target); device.Clear(Color.Transparent);
                bool begun = false;
                try { batch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone); begun = true; draw(); }
                finally { if (begun) batch.End(); device.SetRenderTargets(targets); }
                var pixels = new Color[width * height]; target.GetData(pixels);
                checks[name + "-nonblank"] = pixels.Count(c => c.A != 0) > 500;
                checks[name + "-premultiplied"] = pixels.All(c => c.R <= c.A && c.G <= c.A && c.B <= c.A);
                int columns = name.Contains("overview") ? 6 : name.Contains("poses") ? 4 : 16, cellWidth = width / columns;
                int rows = name.Contains("overview") ? 2 : 6;
                for (int row = 0; row < rows; row++) for (int column = 0; column < columns; column++)
                {
                    int visible = 0;
                    for (int y = row * 170 + 26; y < (row + 1) * 170 - 2; y++)
                        for (int x = column * cellWidth + 8; x < (column + 1) * cellWidth - 8; x++)
                            if (pixels[y * width + x].A != 0) visible++;
                    checks[$"{name}-row{row}-column{column}-body-visible"] = visible > 40;
                }
                using var stream = File.Create(Path.Combine(Helper.DirectoryPath, name + ".png")); target.SaveAsPng(stream, width, height);
            }
            Render("modern-clothing-overview", 720, 340, () =>
            {
                for (int body = 0; body < 2; body++) for (int look = 0; look < 6; look++)
                {
                    var f = Make(body, look, 0); f.faceDirection(2); f.FarmerSprite.StopAnimation();
                    int x = look * 120, y = body * 170;
                    batch.DrawString(Game1.smallFont, $"Look {look + 1}", new Vector2(x + 10, y + 4), Color.White, 0, Vector2.Zero, .65f, SpriteEffects.None, .99f);
                    Draw(f, new Vector2(x + 25, y + 28));
                    cases.Add(new { Body = body, Look = look, Dye = 0, Facing = 2, Pose = "overview native front idle" });
                }
            });
            void Draw(Farmer f, Vector2 at) => f.FarmerRenderer.draw(batch, f.FarmerSprite, f.FarmerSprite.SourceRect, at, Vector2.Zero, .5f, Color.White, 0, f);
            for (int body = 0; body < 2; body++)
            {
                int selectedBody = body;
                Render("modern-clothing-body" + body, 1920, 1020, () =>
                {
                    for (int look = 0; look < 6; look++) for (int dye = 0; dye < 4; dye++)
                    {
                        var f = Make(selectedBody, look, dye);
                        for (int facing = 0; facing < 4; facing++)
                        {
                            f.faceDirection(facing); f.FarmerSprite.StopAnimation();
                            int x = (dye * 4 + facing) * 120, y = look * 170;
                            batch.DrawString(Game1.smallFont, $"{look+1}/{dye}/{facing}", new Vector2(x + 4, y + 4), Color.White, 0, Vector2.Zero, .65f, SpriteEffects.None, .99f);
                            Draw(f, new Vector2(x + 25, y + 28));
                            cases.Add(new { Body = selectedBody, Look = look, Dye = dye, Facing = facing, Frame = f.FarmerSprite.CurrentFrame, Pose = "native direction idle via faceDirection/StopAnimation" });
                        }
                    }
                });
                Render("modern-clothing-poses-body" + body, 960, 1020, () =>
                {
                    for (int look = 0; look < 6; look++) for (int pose = 0; pose < 4; pose++)
                    {
                        var f = Make(selectedBody, look, 0); f.faceDirection(2);
                        string label;
                        if (pose == 0) { f.FarmerSprite.setCurrentSingleFrame(1); label = "walk frame 1 sample"; }
                        else if (pose == 1) { f.FarmerSprite.setCurrentSingleFrame(64, 32000, secondaryArm: true); label = "arm frame 64 sample"; }
                        else if (pose == 2) { f.isSitting.Value = true; f.ShowSitting(); label = "native ShowSitting"; }
                        else { f.swimming.Value = true; f.FarmerSprite.setCurrentSingleFrame(0); label = "native swimming branch"; }
                        int x = pose * 240, y = look * 170;
                        batch.DrawString(Game1.smallFont, label, new Vector2(x + 2, y + 3), Color.White, 0, Vector2.Zero, .6f, SpriteEffects.None, .99f);
                        // Native swimming drawing adds a vertical offset; keep its waterline inside this review cell.
                        Draw(f, new Vector2(x + 70, y + 32 - (pose == 3 ? 64 : 0)));
                        cases.Add(new { Body = selectedBody, Look = look, Pose = label, Frame = f.FarmerSprite.CurrentFrame });
                    }
                });
            }
            checks["custom-texture-draws-observed"] = clothingCustomCalls > 0;
            checks["all-observed-sources-in-bounds"] = clothingBadSources == 0;
            checks["production-HD-adapter-active"] = (int)hd.GetProperty("SubstitutedDraws", flags)!.GetValue(null)! > hdStart;
        }
        catch (Exception ex) { error = ex.ToString(); }
        finally
        {
            clothingAuditBatch = null; harmony.UnpatchAll(harmony.Id);
            clothingLogicalTextures.Clear(); clothingHdReplacements = null;
            foreach (var f in owned) { f.FarmerRenderer.unload(); (typeof(FarmerRenderer).GetField("baseTexture", flags)!.GetValue(f.FarmerRenderer) as Texture2D)?.Dispose(); }
            typeof(Game1).GetField("_player", flags)!.SetValue(null, original);
            Game1.spriteBatch = nativeBatch; Game1.random = random; FarmerRenderer.recolorOffsets = recolor; FarmerRenderer.isDrawingForUI = ui;
            device.SetRenderTargets(targets); device.Viewport = viewport; device.ScissorRectangle = scissor;
            device.BlendState = blend; device.BlendFactor = factor; device.DepthStencilState = depth; device.RasterizerState = raster;
            device.SetVertexBuffers(vertices); device.Indices = indices;
            foreach (var restore in slots) restore();
            checks["global-player-and-cache-restored"] = ReferenceEquals(Game1.player, original) && ReferenceEquals(FarmerRenderer.recolorOffsets, recolor) && ReferenceEquals(Game1.spriteBatch, nativeBatch);
            checks["original-appearance-unchanged"] = Appearance(original) == originalAppearance;
        }
        Helper.Data.WriteJsonFile("modern-clothing-render.json", new { Passed = error == null && checks.Values.All(v => v), Error = error,
            Checks = checks, DrawCalls = clothingDrawCalls, CustomDrawCalls = clothingCustomCalls, InvalidSources = clothingBadSources,
            Requests = clothingRequests, Routes = routes, Cases = cases,
            Legend = "Rows are six catalog looks. Each four-column group is default, white, dark, saturated pink dye. Within each group directions are up/right/down/left. Two body boards plus two pose boards.",
            Limitations = "Actual GPU draw proof only after command execution. Idle native facings and ShowSitting/swimming render branches are exercised. Walk frame1 and secondary-arm frame64 are static samples, not full walking/tool simulations; tool sprite/actions, all animations and multiplayer are not claimed. No save is loaded or modified." });
    }
}
