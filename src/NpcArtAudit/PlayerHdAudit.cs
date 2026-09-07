using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace NpcArtAudit;
internal static class PlayerHdAudit
{
    const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    const int Width = 1280, Height = 960;
    static int mode, substitutions, patchedCalls;
    static readonly Dictionary<Texture2D, Texture2D> companions = new();
    static readonly HashSet<Texture2D> allowed = new();
    static readonly Type[] DrawTypes = { typeof(Texture2D), typeof(Vector2), typeof(Rectangle?), typeof(Color), typeof(float), typeof(Vector2), typeof(float), typeof(SpriteEffects), typeof(float) };
    static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
    {
        var draw = typeof(SpriteBatch).GetMethod("Draw", DrawTypes)!;
        var helper = typeof(PlayerHdAudit).GetMethod(nameof(Draw), Flags)!;
        foreach (var instruction in instructions)
        {
            if (instruction.Calls(draw)) { instruction.opcode = OpCodes.Call; instruction.operand = helper; patchedCalls++; }
            yield return instruction;
        }
    }
    static void Draw(SpriteBatch batch, Texture2D texture, Vector2 position, Rectangle? source, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float depth)
    {
        if (mode != 0 && allowed.Contains(texture))
        {
            if (!companions.TryGetValue(texture, out var hd))
            {
                if ((long)texture.Width * texture.Height > 8_000_000) throw new InvalidOperationException("Companion exceeds bounded texture size.");
                var pixels = new Color[texture.Width * texture.Height]; texture.GetData(pixels);
                var enlarged = new Color[pixels.Length * 4];
                for (int y = 0; y < texture.Height * 2; y++) for (int x = 0; x < texture.Width * 2; x++) enlarged[y * texture.Width * 2 + x] = pixels[(y / 2) * texture.Width + x / 2];
                hd = new Texture2D(texture.GraphicsDevice, texture.Width * 2, texture.Height * 2); hd.SetData(enlarged); companions.Add(texture, hd);
            }
            var rect = source ?? texture.Bounds;
            if (mode == 1) { source = new Rectangle(rect.X * 2, rect.Y * 2, rect.Width * 2, rect.Height * 2); origin *= 2; scale /= 2; }
            // Mode2 intentionally omits mapping: negative control for naive doubled PNGs.
            texture = hd; substitutions++;
        }
        batch.Draw(texture, position, source, color, rotation, origin, scale, effects, depth);
    }
    static void ClearCompanions() { foreach (var texture in companions.Values) texture.Dispose(); companions.Clear(); }
    public static void Run(IModHelper helper, IMonitor monitor, bool production = false)
    {
        if (Context.IsWorldReady || !Game1.IsOnMainThread() || Game1.game1.isDrawing) throw new InvalidOperationException("Player HD proof requires title update.");
        var device = Game1.graphics.GraphicsDevice;
        var targets = device.GetRenderTargets(); var viewport = device.Viewport; var scissor = device.ScissorRectangle;
        var blend = device.BlendState; var factor = device.BlendFactor; var depth = device.DepthStencilState; var rasterizer = device.RasterizerState; var indices = device.Indices;
        var vertices = (VertexBufferBinding[])typeof(UtilityBuildingsAudit).GetMethod("GetVertexBuffers", Flags)!.Invoke(null, new object[] { device })!;
        var slots = (System.Collections.IEnumerable)typeof(UtilityBuildingsAudit).GetMethod("SaveSlots", Flags)!.Invoke(null, new object[] { device })!;
        var player = Game1.player; var location = Game1.currentLocation; var random = Game1.random; var menu = Game1.activeClickableMenu;
        var options = Game1.options; var gameViewport = Game1.viewport; var uiViewport = Game1.uiViewport; var subscriber = Game1.keyboardDispatcher.Subscriber;
        int cabins = Game1.startingCabins; bool ui = FarmerRenderer.isDrawingForUI;
        var recolorCache = FarmerRenderer.recolorOffsets;
        var nativeBatch = Game1.spriteBatch;
        var titleSubMenu = TitleMenu.subMenu;
        Type? productionType = production ? AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("AbigailModern.PlayerHdRenderer")).FirstOrDefault(t => t != null) : null;
        FieldInfo? enabledField = productionType?.GetField("enabled", Flags);
        bool productionEnabled = (bool?)enabledField?.GetValue(null) ?? false;
        int Counter(string name) => (int)productionType!.GetProperty(name, Flags)!.GetValue(null)!;
        var checks = new Dictionary<string, bool>(); var cases = new List<object>(); string? error = null;
        var owned = new List<Farmer>(); var harmony = new Harmony("NpcArtAudit.PlayerHd.Proof");
        try
        {
            Game1.random = new Random(221717); Game1.currentLocation = new GameLocation(); Game1.activeClickableMenu = null;
            Game1.options = (Options)typeof(object).GetMethod("MemberwiseClone", Flags)!.Invoke(options, null)!;
            Game1.options.gamepadControls = false;
            Game1.viewport = new xTile.Dimensions.Rectangle(0, 0, Width, Height); Game1.uiViewport = Game1.viewport;
            FarmerRenderer.recolorOffsets = new();
            patchedCalls = 0;
            if (production)
            {
                if (!productionEnabled) throw new InvalidOperationException("Production HD renderer is not enabled.");
                var entryType=productionType!.Assembly.GetType("AbigailModern.ModEntry")!;
                checks["ModernEntryCompleted"] = (bool?)(entryType.GetField("EntryCompleted",Flags)?.GetValue(null) ?? entryType.GetProperty("EntryCompleted",Flags)?.GetValue(null)) == true;
                var creatorDrawMethod=typeof(CharacterCustomization).GetMethod("draw",new[]{typeof(SpriteBatch)})!;
                checks["CreatorPreviewHookInstalled"] = Harmony.GetPatchInfo(creatorDrawMethod)?.Postfixes.Any(p=>p.owner=="David.AbigailModern.PlayerCreatorPreview")==true;
                checks["Production34CallSites"] = Counter("PatchedCallSites") == 34;
                var artHelper = (IModHelper)productionType!.GetField("helper", Flags)!.GetValue(null)!;
                using var manifest = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(artHelper.DirectoryPath,"player-hd.json")));
                var entries = manifest.RootElement.GetProperty("Assets").EnumerateArray().ToArray();
                checks["All17CompanionsDeclared"] = entries.Length == 17;
                foreach(var entry in entries)
                {
                    string name=entry.GetProperty("Name").GetString()!, file=entry.GetProperty("File").GetString()!;
                    var logical=helper.GameContent.Load<Texture2D>(name); var hd=artHelper.ModContent.Load<IRawTextureData>(file);
                    var pixels=new Color[logical.Width*logical.Height]; logical.GetData(pixels);
                    bool shape=hd.Width==logical.Width*2 && hd.Height==logical.Height*2; int fine=0; bool alpha=shape;
                    if(shape) for(int y=0;y<logical.Height;y++) for(int x=0;x<logical.Width;x++)
                    {
                        int at=y*2*hd.Width+x*2; var a=hd.Data[at];
                        foreach(int offset in new[]{0,1,hd.Width,hd.Width+1}) alpha &= hd.Data[at+offset].A==pixels[y*logical.Width+x].A;
                        if(a!=hd.Data[at+1] || a!=hd.Data[at+hd.Width] || a!=hd.Data[at+hd.Width+1]) fine++;
                    }
                    checks["Companion:"+name]=shape && alpha && fine>0;
                    cases.Add(new {Asset=name,Shape=shape,Alpha=alpha,SubpixelBlocks=fine});
                }
            }
            var methods = typeof(FarmerRenderer).GetMethods(Flags).Where(m => m.DeclaringType == typeof(FarmerRenderer) && (m.Name == "draw" || m.Name == "drawHairAndAccesories" || m.Name == "drawMiniPortrat"));
            if (!production) foreach (var method in methods) harmony.Patch(method, transpiler: new HarmonyMethod(typeof(PlayerHdAudit), nameof(Transpile)));
            if (!production) checks["ScopedNativeDrawCallSitesPatched"] = patchedCalls > 20;
            using var batch = new SpriteBatch(device);
            // Native creator helpers (including drawDialogueBox) use this global batch.
            Game1.spriteBatch = batch;
            Color[] Render(string key, int drawMode, Action draw)
            {
                mode = drawMode; substitutions = 0;
                int productionStart=production?Counter("SubstitutedDraws"):0;
                if(production) enabledField!.SetValue(null,drawMode==1);
                using var target = new RenderTarget2D(device, Width, Height, false, SurfaceFormat.Color, DepthFormat.None);
                device.SetRenderTarget(target); device.Clear(new Color(73, 86, 65));
                batch.Begin(key.StartsWith("preview") || key.StartsWith("creator") ? SpriteSortMode.Deferred : SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
                draw(); batch.End(); device.SetRenderTarget(null);
                var pixels = new Color[Width * Height]; target.GetData(pixels);
                // Native A/B mode may consume a dirty recolor while the adapter is disabled.
                // Re-run that native recolor when returning to HD, rather than testing a stale audit cache.
                if(production && drawMode==0) Game1.player.FarmerRenderer.MarkSpriteDirty();
                using (var output = File.Create(Path.Combine(helper.DirectoryPath, "player-hd-" + key + ".png"))) target.SaveAsPng(output, Width, Height);
                if (drawMode == 1 && !key.StartsWith("preview")) checks[key + "-substitutions"] = production ? Counter("SubstitutedDraws")>productionStart : substitutions > 0;
                return pixels;
            }
            for (int sex = 0; sex < 2; sex++)
            {
                var farmer = new Farmer(); owned.Add(farmer);
                typeof(Game1).GetField("_player", Flags)!.SetValue(null, farmer);
                farmer.changeGender(sex == 0); farmer.changeHairStyle(sex == 0 ? 0 : 16);
                farmer.changeHairColor(new Color(145, 70, 35)); farmer.changeEyeColor(new Color(35, 105, 175)); farmer.changeSkinColor(sex == 0 ? 0 : 12);
                farmer.changeShirt("1000"); farmer.changePantStyle("0"); farmer.changePantsColor(new Color(60, 115, 160));
                farmer.shirtItem.Value = new StardewValley.Objects.Clothing("1000");
                farmer.shirtItem.Value.clothesColor.Value = new Color(160, 70, 110);
                farmer.shirt.Value = "-1"; farmer.FarmerRenderer.MarkSpriteDirty();
                farmer.changeAccessory(1); farmer.changeHat(0); farmer.Items.Clear(); farmer.Items.Add(ItemRegistry.Create("(T)Axe")); farmer.CurrentToolIndex = 0;
                farmer.GetDisplayShirt(out var shirt, out _); farmer.GetDisplayPants(out var pants, out _);
                allowed.Clear(); allowed.UnionWith(new[] { shirt, pants, FarmerRenderer.hairStylesTexture, FarmerRenderer.accessoriesTexture, FarmerRenderer.hatsTexture });
                for (int facing = 0; facing < 4; facing++)
                {
                    farmer.faceDirection(facing); farmer.FarmerSprite.StopAnimation();
                    string key = $"body{sex}-face{facing}";
                    Action draw = () => { farmer.FarmerRenderer.draw(batch, farmer.FarmerSprite, farmer.FarmerSprite.SourceRect, new Vector2(280, 300), Vector2.Zero, .5f, Color.White, 0, farmer); farmer.FarmerRenderer.drawMiniPortrat(batch, new Vector2(520, 300), .5f, 4, facing, farmer); };
                    var before = Render(key + "-native", 0, draw);
                    var baseTexture = (Texture2D)typeof(FarmerRenderer).GetField("baseTexture", Flags)!.GetValue(farmer.FarmerRenderer)!; allowed.Add(baseTexture);
                    ClearCompanions(); var after = Render(key + "-mapped2x", 1, draw);
                    int changed = before.Where((c, i) => c != after[i]).Count();
                    checks[key + (production?"-authored-detail-visible":"-pixel-exact")] = production ? changed>0 : changed == 0;
                    if(production)
                    {
                        var backdrop=new Color(73,86,65);
                        checks[key+"-geometry-alpha-coverage"] = before.Select(c=>c!=backdrop).SequenceEqual(after.Select(c=>c!=backdrop));
                        int refreshes=Counter("BodyRefreshes");
                        var again=Render(key+"-stable",1,draw);
                        checks[key+"-stable-no-body-upload"] = Counter("BodyRefreshes")==refreshes && after.SequenceEqual(again);
                        if(facing==2)
                        {
                            farmer.changeEyeColor(new Color(170,80,30)); farmer.changeSkinColor(sex==0?12:0);
                            var dyed=Render(key+"-recolored",1,draw);
                            checks[key+"-native-recolor-refreshes-HD"] = Counter("BodyRefreshes")>refreshes && !after.SequenceEqual(dyed);
                        }
                    }
                    checks[key + "-nonblank"] = before.Count(c => c != new Color(73, 86, 65)) > 200;
                    cases.Add(new { Key = key, ChangedPixels = changed, HeldTool = farmer.CurrentTool?.QualifiedItemId, Note = "Renderer and mini portrait; tool equipped but tool action draw is not exercised." });
                    if (!production && sex == 0 && facing == 2)
                    {
                        var wrong = Render(key + "-unmapped2x-negative-control", 2, draw);
                        checks["NaiveDoubledPngDetected"] = !before.SequenceEqual(wrong);
                    }
                }
                farmer.faceDirection(2);
                farmer.FarmerSprite.setCurrentSingleFrame(64, 32000, secondaryArm: true);
                Action poseDraw = () => farmer.FarmerRenderer.draw(batch, farmer.FarmerSprite, farmer.FarmerSprite.SourceRect, new Vector2(280, 300), Vector2.Zero, .5f, Color.White, 0, farmer);
                var poseBefore = Render($"body{sex}-arm-pose-native", 0, poseDraw);
                allowed.Add((Texture2D)typeof(FarmerRenderer).GetField("baseTexture", Flags)!.GetValue(farmer.FarmerRenderer)!);
                ClearCompanions(); var poseAfter = Render($"body{sex}-arm-pose-mapped2x", 1, poseDraw);
                checks[$"body{sex}-arm-pose-result"] = production ? !poseBefore.SequenceEqual(poseAfter) : poseBefore.SequenceEqual(poseAfter);
                // Constructor and draw may alter only this detached player's fields.
                var creator = new CharacterCustomization(CharacterCustomization.Source.NewGame);
                Action creatorDraw = () => creator.draw(batch);
                var creatorBefore = Render($"creator{sex}-native", 0, creatorDraw);
                allowed.Add((Texture2D)typeof(FarmerRenderer).GetField("baseTexture", Flags)!.GetValue(farmer.FarmerRenderer)!);
                ClearCompanions(); var creatorAfter = Render($"creator{sex}-mapped2x", 1, creatorDraw);
                checks[$"creator{sex}-result"] = production ? !creatorBefore.SequenceEqual(creatorAfter) : creatorBefore.SequenceEqual(creatorAfter);
                if(production)
                {
                    var previewType=productionType!.Assembly.GetType("AbigailModern.PlayerCreatorPreview+Preview")!;
                    string appearance=farmer.hair.Value+"|"+farmer.skin.Value+"|"+farmer.FacingDirection+"|"+farmer.shirt.Value+"|"+farmer.pantsColor.Value;
                    foreach(bool inTitle in new[]{true,false})
                    {
                        var preview=(IClickableMenu)Activator.CreateInstance(previewType,Flags,null,new object[]{creator,inTitle,farmer},null)!;
                        if(inTitle) typeof(TitleMenu).GetField("_subMenu",Flags)!.SetValue(null,preview); else Game1.activeClickableMenu=preview;
                        var large=Render($"preview{sex}-{inTitle}-front",1,()=>preview.draw(batch));
                        var picture=(Texture2D)previewType.GetField("picture",Flags)!.GetValue(preview)!;
                        var pictureBounds=(Rectangle)previewType.GetField("pictureBounds",Flags)!.GetValue(preview)!;
                        var sourcePixels=new Color[picture.Width*picture.Height]; picture.GetData(sourcePixels);
                        int matchingBody=0;
                        for(int y=0;y<picture.Height;y++) for(int x=0;x<picture.Width;x++)
                        {
                            Color expected=sourcePixels[y*picture.Width+x];
                            int px=pictureBounds.X+(int)((x+.5f)*pictureBounds.Width/picture.Width), py=pictureBounds.Y+(int)((y+.5f)*pictureBounds.Height/picture.Height);
                            if(expected.A==255 && px>=0 && px<Width && py>=0 && py<Height && large[py*Width+px]==expected) matchingBody++;
                        }
                        checks[$"preview{sex}-{inTitle}-composed-body-visible"] = matchingBody>100;
                        checks[$"preview{sex}-{inTitle}-light-labels-visible"] = large.Count(c=>c.R>=220 && c.G>=230 && c.B>=240)>150;
                        preview.receiveGamePadButton(Microsoft.Xna.Framework.Input.Buttons.RightShoulder);
                        var rotated=Render($"preview{sex}-{inTitle}-rotated",1,()=>preview.draw(batch));
                        checks[$"preview{sex}-{inTitle}-rotation-visible"] = !large.SequenceEqual(rotated);
                        preview.receiveKeyPress(Microsoft.Xna.Framework.Input.Keys.Escape);
                        checks[$"preview{sex}-{inTitle}-exact-parent-restored"] = ReferenceEquals(inTitle?TitleMenu.subMenu:Game1.activeClickableMenu,creator);
                        checks[$"preview{sex}-{inTitle}-appearance-unchanged"] = appearance==farmer.hair.Value+"|"+farmer.skin.Value+"|"+farmer.FacingDirection+"|"+farmer.shirt.Value+"|"+farmer.pantsColor.Value;
                    }
                }
                ClearCompanions(); allowed.Clear();
            }
        }
        catch (Exception ex) { error = ex.ToString(); }
        finally
        {
            mode = 0; harmony.UnpatchAll(harmony.Id); ClearCompanions(); allowed.Clear();
            foreach (var farmer in owned)
            {
                (typeof(FarmerRenderer).GetField("baseTexture", Flags)!.GetValue(farmer.FarmerRenderer) as Texture2D)?.Dispose();
                farmer.FarmerRenderer.unload();
            }
            FarmerRenderer.recolorOffsets = recolorCache; FarmerRenderer.isDrawingForUI = ui;
            typeof(Game1).GetField("_player", Flags)!.SetValue(null, player);
            Game1.currentLocation = location; Game1.random = random; Game1.activeClickableMenu = menu; Game1.options = options; Game1.viewport = gameViewport; Game1.uiViewport = uiViewport;
            Game1.keyboardDispatcher.Subscriber = subscriber; Game1.startingCabins = cabins;
            Game1.spriteBatch = nativeBatch;
            typeof(TitleMenu).GetField("_subMenu",Flags)!.SetValue(null,titleSubMenu);
            enabledField?.SetValue(null,productionEnabled);
            device.SetRenderTargets(targets); device.Viewport = viewport; device.ScissorRectangle = scissor; device.BlendState = blend; device.BlendFactor = factor; device.DepthStencilState = depth; device.RasterizerState = rasterizer; device.SetVertexBuffers(vertices); device.Indices = indices;
            foreach (var slot in slots) ((Action)slot.GetType().GetProperty("Restore")!.GetValue(slot)!)();
            checks["PatchRemoved"] = !Harmony.GetAllPatchedMethods().Any(m => Harmony.GetPatchInfo(m)?.Owners.Contains(harmony.Id) == true);
            checks["StateRestored"] = ReferenceEquals(Game1.player, player) && ReferenceEquals(Game1.options, options) && ReferenceEquals(Game1.currentLocation, location) && ReferenceEquals(Game1.random, random) && ReferenceEquals(Game1.activeClickableMenu, menu) && ReferenceEquals(Game1.keyboardDispatcher.Subscriber, subscriber) && ReferenceEquals(FarmerRenderer.recolorOffsets, recolorCache) && device.GetRenderTargets().SequenceEqual(targets);
        }
        bool passed = error == null && checks.Values.All(v => v);
        helper.Data.WriteJsonFile(production?"player-hd-production-checks.json":"player-hd-checks.json", new { Passed = passed, Error = error, Checks = checks, Cases = cases, PatchedCallSites = production?Counter("PatchedCallSites"):patchedCalls,
            Scope = production ? "Actual production authored companions: all17 dimension/alpha/subpixel checks; native-versus-HD GPU fixtures, dyes/cache stability, native creator and enlarged composed preview rotation/return through title and active-menu routes. No saves." : "Test-only native FarmerRenderer call-site mapping, nearest-neighbor 2x colored companions, two bodies/four facings, mini portraits and actual creator; negative unmapped control. No saves.",
            Limitations = "Equipped axe and secondary arm pose only; swinging tool actions, all animation frames, bald/mannequin/cursed GPU bodies, alternate metadata hair/hats, swimming, slingshot, content reload and other zoom levels remain untested. Preview opens via its actual constructor and tests native click/key methods; SMAPI magnifier opening event itself is not synthesized." });
        monitor.Log("Player HD geometry proof " + (passed ? "passed" : "FAILED: " + error), passed ? LogLevel.Info : LogLevel.Error);
    }
}
