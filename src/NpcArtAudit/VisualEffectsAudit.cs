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

/// <summary>Actual production effect draws over a detached Town map crop, without loading a save.</summary>
internal static class VisualEffectsAudit
{
    private const int Width = 768, Height = 576;
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static void Run(IModHelper helper, IMonitor monitor)
    {
        if (!Game1.IsOnMainThread() || Game1.game1 == null || Game1.game1.isDrawing || Game1.uiMode || Game1.uiModeCount != 0)
        {
            helper.Data.WriteJsonFile("visual-effects-checks.json", new { Passed = false, Deferred = true, Error = "Requires main-thread update outside drawing/UI mode." });
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
        var vertices = GetVertexBuffers(device);
        var indices = device.Indices;
        var slots = SaveSlots(device);
        var location = Game1.currentLocation;
        var player = Game1.player;
        var gameViewport = Game1.viewport;
        var uiViewport = Game1.uiViewport;
        var menu = Game1.activeClickableMenu;
        var random = Game1.random;
        var cases = new List<object>();
        var checks = new Dictionary<string, bool>();
        var metrics = new Dictionary<string, double>();
        var effects = new List<IDisposable>();
        string? error = null;
        bool restored = false;
        try
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "AbigailModern");
            Type T(string name) => assembly.GetType("AbigailModern.Visuals." + name, true)!;
            object New(string name) => Activator.CreateInstance(T(name), true)!;
            var settings = New("VisualSettings");
            var frame = New("VisualFrame");
            Set(frame, "Viewport", new Rectangle(0, 0, Width, Height));
            Set(frame, "Outdoors", true);
            Set(frame, "Season", "fall");
            // Synthetic inputs intentionally isolate GPU effects from world snapshot discovery.
            var lights = Array.CreateInstance(T("SceneLight"), 80);
            for (int i = 0; i < lights.Length; i++)
                lights.SetValue(Activator.CreateInstance(T("SceneLight"), new object[] {
                    new Vector2(95 + i % 8 * 85, 140 + i / 8 * 34), 155f,
                    i % 2 == 0 ? new Color(255, 192, 106) : new Color(130, 179, 255), true, 1f }), i);
            Set(frame, "Lights", lights);
            var casters = Array.CreateInstance(T("ShadowCaster"), 12);
            for (int i = 0; i < casters.Length; i++)
                casters.SetValue(Activator.CreateInstance(T("ShadowCaster"), new object[] {
                    new Vector2(90 + i % 4 * 180, 185 + i / 4 * 150), 65f, 110f,
                    Enum.Parse(T("CasterKind"), i % 2 == 0 ? "Tree" : "Building") }), i);
            Set(frame, "Casters", casters);
            object shadow = New("ShadowEffect"), light = New("LightingEffect"), fog = New("FogEffect");
            effects.Add((IDisposable)shadow); effects.Add((IDisposable)light); effects.Add((IDisposable)fog);
            object nightEffect=New("NightEffect"); effects.Add((IDisposable)nightEffect);
            int NightDraw(string method,SpriteBatch b)=>(int)nightEffect.GetType().GetMethod(method)!.Invoke(nightEffect,new[]{b,frame,settings})!;
            int Draw(object effect, SpriteBatch batch) => (int)effect.GetType().GetMethod("Draw")!.Invoke(effect, new[] { batch, frame, settings })!;

            using var content = Game1.content.CreateTemporary();
            var map = content.Load<xTile.Map>("Maps/Town");
            metrics["TownMapWidth"] = map.GetLayer("Back").LayerWidth;
            metrics["TownMapHeight"] = map.GetLayer("Back").LayerHeight;
            checks["NativeTownDimensions"] = map.GetLayer("Back").LayerWidth == 130 && map.GetLayer("Back").LayerHeight == 110;
            var detachedTown = new GameLocation();
            detachedTown.map = map;
            helper.Reflection.GetField<Netcode.NetString>(detachedTown, "name").GetValue().Value = "Town";
            detachedTown.IsOutdoors = true;
            var collect = T("VisualEffectsController").GetMethod("CollectCasters", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
            var discovered = ((System.Collections.IEnumerable)collect.Invoke(null,
                new object[] { detachedTown, new Rectangle(0, 0, 130 * 64, 110 * 64), 128 })!).Cast<object>().ToArray();
            metrics["DetachedTownBuildingCasters"] = discovered.Length;
            checks["DetachedTownDiscoversBuildings"] = discovered.Length == 12;
            var sheets = new Dictionary<xTile.Tiles.TileSheet, Texture2D>();
            foreach (var sheet in map.TileSheets)
            {
                string source = sheet.ImageSource.Replace('\\', '/');
                if (source.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) source = source[..^4];
                if (!source.StartsWith("Maps/", StringComparison.OrdinalIgnoreCase)) source = "Maps/" + Path.GetFileName(source);
                sheets[sheet] = content.Load<Texture2D>(source);
            }
            using var batch = new SpriteBatch(device);
            using var target = new RenderTarget2D(device, Width, Height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            void MapLayer(string name, Color tint)
            {
                var layer = map.GetLayer(name);
                if (layer == null) return;
                for (int y = 0; y < 18; y++) for (int x = 0; x < 24; x++)
                {
                    int mx = 32 + x, my = 48 + y;
                    if (mx >= layer.LayerWidth || my >= layer.LayerHeight) continue;
                    var tile = layer.Tiles[mx, my];
                    if (tile == null || tile.TileIndex < 0) continue;
                    var sheet = tile.TileSheet;
                    int sizeX = sheet.TileSize.Width, sizeY = sheet.TileSize.Height;
                    var source = new Rectangle(tile.TileIndex % sheet.SheetSize.Width * sizeX,
                        tile.TileIndex / sheet.SheetSize.Width * sizeY, sizeX, sizeY);
                    batch.Draw(sheets[sheet], new Rectangle(x * 32, y * 32, 32, 32), source, tint);
                }
            }
            (Color[] Pixels, int Shadow, int Light, int Fog, int Moon, int Stars) Render(string name, float minutes, bool raining,
                bool enabled, bool s, bool l, bool f, double seconds = 0, bool save = true, int maxCasters = 6,
                bool moon=false,bool stars=false,bool sky=false)
            {
                Set(frame, "Minutes", minutes); Set(frame, "Raining", raining); Set(frame, "Seconds", seconds);
                Set(settings, "Enabled", enabled); Set(settings, "ShadowsEnabled", s);
                Set(settings, "LightingEnabled", l); Set(settings, "FogEnabled", f);
                Set(settings,"MoonlightEnabled",moon);Set(settings,"StarsEnabled",stars);
                Set(frame,"SkyHeight",sky?220:0);
                Set(settings, "MaxLights", 8); Set(settings, "MaxCasters", maxCasters);
                // Eight synthetic lights and six casters are intentional stress limits.
                device.SetRenderTarget(target);
                device.Clear(sky?new Color(8,14,32):new Color(38,52,42));
                Color tint = minutes >= 1200 ? new Color(64, 78, 112) : Color.White;
                batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
                if(!sky)MapLayer("Back", tint);
                int starCalls=NightDraw("DrawStars",batch);
                int shadowCalls = Draw(shadow, batch);
                if(!sky){MapLayer("Buildings", tint); MapLayer("Front", tint); MapLayer("AlwaysFront", tint);}
                int moonCalls=NightDraw("DrawMoon",batch);
                int fogCalls = Draw(fog, batch), lightCalls = Draw(light, batch);
                batch.End();
                device.SetRenderTarget(null);
                var pixels = new Color[Width * Height]; target.GetData(pixels);
                if (save)
                {
                    using var stream = File.Create(Path.Combine(helper.DirectoryPath, "runtime-effects-" + name + ".png"));
                    target.SaveAsPng(stream, Width, Height);
                }
                cases.Add(new { Name = name, Minutes = minutes, Rain = raining, Seconds = seconds,
                    ShadowCalls = shadowCalls, LightCalls = lightCalls, FogCalls = fogCalls, MoonCalls=moonCalls, StarCalls=starCalls,
                    Image = save ? "runtime-effects-" + name + ".png" : null });
                return (pixels, shadowCalls, lightCalls, fogCalls,moonCalls,starCalls);
            }
            int Changed(Color[] a, Color[] b) => a.Zip(b, (x, y) => x != y ? 1 : 0).Sum();
            double Average(Color[] p) => p.Average(c => (c.R + c.G + c.B) / 3d);
            var day = Render("day-before", 540, false, false, true, true, true);
            var morning = Render("shadow-morning", 540, false, true, true, false, false);
            var afternoon = Render("shadow-afternoon", 960, false, true, true, false, false);
            var night = Render("night-before", 1320, false, false, true, true, true);
            var lighting = Render("night-lighting", 1320, false, true, false, true, false);
            var lightingLater = Render("night-lighting-later", 1320, false, true, false, true, false, 2);
            var mist = Render("morning-fog", 420, true, true, false, false, true);
            var mistLater = Render("morning-fog-later", 420, true, true, false, false, true, 90);
            var combined = Render("combined", 1080, true, true, true, true, true);
            var disabled = Render("all-disabled", 540, false, true, false, false, false);
            checks["DisabledPixelIdentity"] = Changed(day.Pixels, disabled.Pixels) == 0;
            checks["DisabledDrawCountsZero"] = disabled.Shadow + disabled.Light + disabled.Fog == 0 && day.Shadow + day.Light + day.Fog == 0;
            checks["ShadowVisible"] = morning.Shadow > 0 && Changed(day.Pixels, morning.Pixels) > 100;
            checks["SunChangesShadow"] = afternoon.Shadow > 0 && Changed(morning.Pixels, afternoon.Pixels) > 100;
            checks["LightingVisible"] = lighting.Light > 0 && Changed(night.Pixels, lighting.Pixels) > 100;
            checks["FlickerAnimates"] = Changed(lighting.Pixels, lightingLater.Pixels) > 10;
            checks["FogVisible"] = mist.Fog > 0 && Changed(day.Pixels, mist.Pixels) > 100;
            checks["FogAnimates"] = Changed(mist.Pixels, mistLater.Pixels) > 100;
            checks["FogRemainsSubtle"] = Average(mist.Pixels) - Average(day.Pixels) < 15 && mist.Pixels.Count(c => c.R == 255 && c.G == 255 && c.B == 255) <= day.Pixels.Count(c => c.R == 255 && c.G == 255 && c.B == 255);
            checks["LightBudget"] = lighting.Light == 8;
            checks["CasterBudget"] = morning.Shadow <= 6;
            checks["FogBudget"] = mist.Fog <= 72 && combined.Fog <= 72;
            checks["CombinedEffects"] = combined.Shadow > 0 && combined.Light > 0 && combined.Fog > 0;
            metrics["DisabledChangedPixels"] = Changed(day.Pixels, disabled.Pixels);
            metrics["ShadowChangedPixels"] = Changed(day.Pixels, morning.Pixels);
            metrics["SunDirectionChangedPixels"] = Changed(morning.Pixels, afternoon.Pixels);
            metrics["LightingChangedPixels"] = Changed(night.Pixels, lighting.Pixels);
            metrics["FlickerChangedPixels"] = Changed(lighting.Pixels, lightingLater.Pixels);
            metrics["FogChangedPixels"] = Changed(day.Pixels, mist.Pixels);
            metrics["FogAnimationChangedPixels"] = Changed(mist.Pixels, mistLater.Pixels);
            metrics["FogAverageBrightnessIncrease255"] = Average(mist.Pixels) - Average(day.Pixels);
            var alignedCasters = Array.CreateInstance(T("ShadowCaster"), discovered.Length);
            for (int i = 0; i < discovered.Length; i++)
            {
                object sourceCaster = discovered[i];
                var casterType = sourceCaster.GetType();
                Vector2 foot = (Vector2)casterType.GetProperty("Foot")!.GetValue(sourceCaster)!;
                float width = (float)casterType.GetProperty("Width")!.GetValue(sourceCaster)!;
                float height = (float)casterType.GetProperty("Height")!.GetValue(sourceCaster)!;
                object kind = casterType.GetProperty("Kind")!.GetValue(sourceCaster)!;
                alignedCasters.SetValue(Activator.CreateInstance(T("ShadowCaster"), new object[] {
                    (foot - new Vector2(32 * 64, 48 * 64)) / 2, width / 2, height / 2, kind }), i);
            }
            Set(frame, "Casters", alignedCasters);
            var aligned = Render("town-aligned-shadows", 540, false, true, true, false, false, maxCasters: 128);
            checks["ActualTownAlignedShadowsVisible"] = aligned.Shadow > 0 && Changed(day.Pixels, aligned.Pixels) > 100;
            metrics["ActualTownAlignedShadowChangedPixels"] = Changed(day.Pixels, aligned.Pixels);
            metrics["ActualTownAlignedShadowDraws"] = aligned.Shadow;
            Set(frame, "Casters", casters);
            // Reuse after disposal must recreate owned GPU resources without touching game assets.
            foreach (var effect in effects) effect.Dispose();
            var recreated = Render("recreated", 1320, false, true, false, true, false, save: false);
            checks["TextureRecreation"] = Changed(lighting.Pixels, recreated.Pixels) == 0;
            var moonlit=Render("moonlight-town",1440,false,true,false,true,false,moon:true);
            var moonBefore=Render("moonlight-before",1440,false,true,false,true,false);
            var moonDay=Render("moonlight-day-off",540,false,true,false,false,false,moon:true);
            var moonDisabled=Render("moonlight-disabled",1440,false,false,false,true,false,moon:true);
            var nativeNight=Render("moonlight-disabled-before",1440,false,false,false,true,false);
            checks["MoonlightBlueTintVisible"]=moonlit.Moon==1 && Changed(moonBefore.Pixels,moonlit.Pixels)>100
                && moonlit.Pixels.Average(c=>(int)c.B-c.R)>moonBefore.Pixels.Average(c=>(int)c.B-c.R);
            checks["MoonlightDayAndMasterOff"]=moonDay.Moon==0 && Changed(day.Pixels,moonDay.Pixels)==0
                && moonDisabled.Moon==0 && Changed(nativeNight.Pixels,moonDisabled.Pixels)==0;
            var skyBefore=Render("sky-before",1440,false,true,false,false,false,sky:true);
            var starry=Render("sky-stars",1440,false,true,false,false,false,stars:true,sky:true);
            var laterStars=Render("sky-stars-later",1440,false,true,false,false,false,seconds:4,stars:true,sky:true);
            var rainStars=Render("sky-rain-no-stars",1440,true,true,false,false,false,stars:true,sky:true);
            var groundStars=Render("ground-no-stars",1440,false,true,false,true,false,stars:true);
            checks["StarsVisibleAndTwinkle"]=starry.Stars>0 && starry.Stars<=96
                && Changed(skyBefore.Pixels,starry.Pixels)>10 && Changed(starry.Pixels,laterStars.Pixels)>10;
            checks["StarsClippedToSky"]=starry.Pixels.Skip(Width*220).SequenceEqual(skyBefore.Pixels.Skip(Width*220));
            checks["StarsHiddenInRainAndGround"]=rainStars.Stars==0 && Changed(skyBefore.Pixels,rainStars.Pixels)==0
                && groundStars.Stars==0 && Changed(moonBefore.Pixels,groundStars.Pixels)==0;
        }
        catch (Exception ex) { error = ex.ToString(); }
        finally
        {
            foreach (var effect in effects) effect.Dispose();
            device.SetRenderTargets(targets);
            device.Viewport = viewport; device.ScissorRectangle = scissor;
            device.BlendState = blend; device.BlendFactor = blendFactor;
            device.DepthStencilState = depth; device.RasterizerState = rasterizer;
            device.SetVertexBuffers(vertices); device.Indices = indices;
            foreach (var slot in slots) slot.Restore();
            restored = device.GetRenderTargets().SequenceEqual(targets) && device.Viewport.Equals(viewport)
                && device.ScissorRectangle == scissor && ReferenceEquals(device.BlendState, blend)
                && device.BlendFactor == blendFactor && ReferenceEquals(device.DepthStencilState, depth)
                && ReferenceEquals(device.RasterizerState, rasterizer) && ReferenceEquals(device.Indices, indices)
                && GetVertexBuffers(device).SequenceEqual(vertices) && slots.All(s => s.Matches())
                && ReferenceEquals(Game1.currentLocation, location) && ReferenceEquals(Game1.player, player)
                && Game1.viewport.Equals(gameViewport) && Game1.uiViewport.Equals(uiViewport)
                && ReferenceEquals(Game1.activeClickableMenu, menu) && ReferenceEquals(Game1.random, random);
        }
        checks["GraphicsAndGameReferencesRestored"] = restored;
        bool passed = error == null && checks.Count > 10 && checks.Values.All(v => v);
        helper.Data.WriteJsonFile("visual-effects-checks.json", new { Passed = passed, Error = error,
            Scope = "Actual production Draw methods on GPU over loaded Town map crop (32,48), 24x18 tiles at half game scale. Most cases use synthetic caster/light positions and illustrative night tint. town-aligned-shadows uses the actual detached Town CollectCasters output transformed into the displayed crop. Native dimensions and authored building-discovery count checked. Moonlight cases use the same native Town crop; sky-stars cases use an illustrative dark sky with a220pixel clipping boundary, not a live Summit scene. No save or native lighting draw validation.",
            Checks = checks, Metrics = metrics, Cases = cases });
        monitor.Log("Visual effects GPU fixture " + (passed ? "passed." : "FAILED: " + error), passed ? LogLevel.Info : LogLevel.Error);
    }

    private static void Set(object obj, string name, object value) => obj.GetType().GetProperty(name)!.SetValue(obj, value);
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
            var collection = typeof(GraphicsDevice).GetProperty(name)!.GetValue(device)!;
            var indexer = collection.GetType().GetProperty("Item")!;
            for (int i = 0; i < 32; i++)
            {
                object[] index = { i };
                object? value;
                try { value = indexer.GetValue(collection, index); }
                catch (TargetInvocationException ex) when (ex.InnerException is IndexOutOfRangeException or ArgumentOutOfRangeException) { break; }
                slots.Add(new Slot(() => indexer.SetValue(collection, value, index), () => ReferenceEquals(indexer.GetValue(collection, index), value)));
            }
        }
        return slots;
    }
}
