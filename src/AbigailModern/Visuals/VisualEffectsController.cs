using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Mods;
using StardewValley.TerrainFeatures;

namespace AbigailModern.Visuals;

/// <summary>Reads the world into cosmetic draw inputs; never edits native lights, weather or maps.</summary>
internal sealed class VisualEffectsController
{
    private const string SettingsFile="visual-effects.json";
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private VisualSettings settings;
    private readonly PerScreen<ScreenState> screens=new(()=>new ScreenState());

    private sealed class ScreenState : IDisposable
    {
        public readonly LightingEffect Lighting=new();
        public readonly ShadowEffect Shadows=new();
        public readonly FogEffect Fog=new();
        public readonly NightEffect Night=new();
        public readonly HashSet<string> Failed=new();
        public GameLocation? Location;
        public double Seconds,CollectedAt=-1;
        public Rectangle CollectedViewport;
        public IReadOnlyList<ShadowCaster> Casters=Array.Empty<ShadowCaster>();
        public void Dispose() {Lighting.Dispose();Shadows.Dispose();Fog.Dispose();Night.Dispose();}
    }

    public VisualEffectsController(IModHelper helper,IMonitor monitor)
    {
        this.helper=helper;this.monitor=monitor;settings=ReadSettings();
        helper.Events.GameLoop.UpdateTicked+=(_,_)=>
        {
            if(Eligible()&&Game1.shouldTimePass())
                screens.Value.Seconds+=Math.Clamp(Game1.currentGameTime.ElapsedGameTime.TotalSeconds,0,.1);
        };
        helper.Events.Display.RenderedStep+=(_,e)=>
        {
            if(e.Step!=RenderSteps.World_Background||!Eligible()||!settings.Enabled )return;
            var state=screens.Value;
            Render(state,"shadows",()=>state.Shadows.Draw(e.SpriteBatch,Snapshot(state),settings));
            Render(state,"stars",()=>state.Night.DrawStars(e.SpriteBatch,Snapshot(state),settings));
        };
        helper.Events.Display.RenderedWorld+=(_,e)=>
        {
            if(!Eligible()||!settings.Enabled)return;
            var state=screens.Value;VisualFrame frame;
            try {frame=Snapshot(state);} catch(Exception ex) {Fail(state,"world snapshot",ex);return;}
            // Moonlight and mist remain below warm local lights and the HUD.
            Render(state,"moonlight",()=>state.Night.DrawMoon(e.SpriteBatch,frame,settings));
            Render(state,"fog",()=>state.Fog.Draw(e.SpriteBatch,frame,settings));
            Render(state,"lighting",()=>state.Lighting.Draw(e.SpriteBatch,frame,settings));
        };
        helper.Events.GameLoop.ReturnedToTitle+=(_,_)=>Reset();
        helper.ConsoleCommands.Add("modern_effects","Usage: modern_effects [status|on|off|reload] or modern_effects [lighting|shadows|fog|moonlight|stars] [on|off|0..1]. Settings persist in visual-effects.json.",Command);
        monitor.Log("Visual effects ready: warm lighting, soft solar shadows, atmospheric fog, moonlight and sky stars. Use modern_effects to adjust each effect.",LogLevel.Info);
    }

    private static bool Eligible()=>VisualPolicy.Eligible(Context.IsWorldReady,Game1.currentLocation!=null,
        Game1.eventUp,Game1.currentMinigame!=null,Game1.game1?.takingMapScreenshot??true);

    private VisualFrame Snapshot(ScreenState state)
    {
        var location=Game1.currentLocation;
        var viewport=new Rectangle(Game1.viewport.X,Game1.viewport.Y,Game1.viewport.Width,Game1.viewport.Height);
        if(!ReferenceEquals(state.Location,location)) {state.Location=location;state.CollectedAt=-1;state.Casters=Array.Empty<ShadowCaster>();}
        if(viewport.Width!=state.CollectedViewport.Width||viewport.Height!=state.CollectedViewport.Height||state.CollectedAt<0||state.Seconds-state.CollectedAt>=.5||Math.Abs(viewport.X-state.CollectedViewport.X)>=64||Math.Abs(viewport.Y-state.CollectedViewport.Y)>=64)
        {
            state.Casters=CollectCasters(location,viewport,settings.MaxCasters);
            state.CollectedViewport=viewport;state.CollectedAt=state.Seconds;
        }
        var weather=location.GetWeather();
        return new VisualFrame
        {
            Viewport=viewport,Minutes=VisualPolicy.Minutes(Game1.timeOfDay,Game1.gameTimeInterval,Game1.realMilliSecondsPerGameTenMinutes),
            Seconds=state.Seconds,Outdoors=location.IsOutdoors,Season=location.GetSeason().ToString().ToLowerInvariant(),
            Raining=weather.IsRaining,Snowing=weather.IsSnowing,Lightning=weather.IsLightning,GreenRain=weather.IsGreenRain,
            SkyHeight=SkyHeight(location,viewport),
            Lights=CollectLights(location,viewport,settings.MaxLights),Casters=state.Casters
        };
    }

    private int SkyHeight(GameLocation location,Rectangle viewport)
    {
        if(location is not StardewValley.Locations.Summit || Game1.background==null || Game1.background.cursed || viewport.X<=-1000)return 0;
        // The native Summit mountain strip begins here. Keep stars wholly above its rectangle.
        int initial=helper.Reflection.GetField<int>(Game1.background,"initialViewportY").GetValue();
        int shift=-viewport.Y/4+initial/4;
        return NightPolicy.SkyHeight(viewport.Height,shift);
    }
    internal static IReadOnlyList<SceneLight> CollectLights(GameLocation location,Rectangle viewport,int limit)
    {
        var result=new List<SceneLight>();
        foreach(var light in Game1.currentLightSources.Values)
        {
            if(result.Count>=limit)break;
            string only=light.onlyLocation.Value;
            if(!string.IsNullOrEmpty(only)&&only!=location.NameOrUniqueName)continue;
            var native=light.color.Value;var position=light.position.Value;
            if(!float.IsFinite(position.X)||!float.IsFinite(position.Y)||native.A==0||light.lightTexture==null||light.lightTexture.IsDisposed||!float.IsFinite(light.radius.Value)||light.radius.Value<=0)continue;
            if(!light.IsOnScreen())continue;
            float radius=Math.Clamp(light.lightTexture.Width*light.radius.Value/2,12,400);
            float visibleRadius=Math.Clamp(radius*.65f,12,240);
            if(position.X+visibleRadius<viewport.Left||position.Y+visibleRadius<viewport.Top||position.X-visibleRadius>viewport.Right||position.Y-visibleRadius>viewport.Bottom)continue;
            // Stardew's lightmap stores subtractive colors; the glow needs display color.
            var color=new Color(255-native.R,255-native.G,255-native.B);
            if(Math.Abs(color.R-color.G)<18&&Math.Abs(color.G-color.B)<18)color=new Color(255,219,177);
            bool flicker=light.textureIndex.Value is LightSource.sconceLight or LightSource.cauldronLight or LightSource.lantern;
            result.Add(new SceneLight(position,radius,color,flicker,native.A/255f));
        }
        return result;
    }

    internal static IReadOnlyList<ShadowCaster> CollectCasters(GameLocation location,Rectangle viewport,int limit)
    {
        var result=new List<ShadowCaster>();
        if(!location.IsOutdoors)return result;
        var visible=viewport;visible.Inflate(640,640);
        void Add(float x,float y,float width,float height,CasterKind kind)
        {
            var foot=new Vector2(x,y);
            if(visible.Contains(foot))result.Add(new ShadowCaster(foot,width,height,kind));
        }
        if(location.Name=="Town"&&location.Map.GetLayer("Back") is {LayerWidth:130,LayerHeight:110})
        {
            // Authored feet and approximate heights for the reviewed native Town buildings.
            foreach(var (x,y,w,h) in new[] {(52.5f,20f,11f,8f),(35.5f,57f,7f,10f),(43.5f,58f,10f,10f),(43.5f,73f,9f,10f),
                (58f,64f,9f,10f),(11f,86f,8f,10f),(23f,89f,8f,9f),(60f,86f,9f,12f),(94f,82f,5f,7f),(103f,90f,8.5f,7f),(95f,51f,12f,9f)})
                Add(x*64,y*64,w*64,h*64,CasterKind.Building);
            var front=location.Map.GetLayer("AlwaysFront");
            bool rebuilt=front!=null&&front.LayerWidth>69&&front.LayerHeight>60&&front.Tiles[69,60]?.TileIndex==1336;
            Add(73*64,69*64,8*64,(rebuilt?9:3)*64,CasterKind.Building);
        }
        foreach(var building in location.buildings)
        {
            
            Add((building.tileX.Value+building.tilesWide.Value/2f)*64,(building.tileY.Value+building.tilesHigh.Value)*64,
                building.tilesWide.Value*64,Math.Clamp(building.tilesHigh.Value*64+128,96,640),CasterKind.Building);
        }
        foreach(var pair in location.terrainFeatures.Pairs)
        {
            
            if(pair.Value is Tree tree&&!tree.stump.Value&&tree.growthStage.Value>=3)
                Add((pair.Key.X+.5f)*64,(pair.Key.Y+1)*64,tree.growthStage.Value>=5?176:88,tree.growthStage.Value>=5?320:144,CasterKind.Tree);
            else if(pair.Value is FruitTree fruit&&!fruit.stump.Value&&fruit.growthStage.Value>=3)
                Add((pair.Key.X+.5f)*64,(pair.Key.Y+1)*64,160,280,CasterKind.Tree);
        }
        return result.OrderBy(c=>Vector2.DistanceSquared(c.Foot,viewport.Center.ToVector2())).Take(Math.Clamp(limit,0,256)).ToArray();
    }

    private void Render(ScreenState state,string effect,Func<int> draw)
    {
        if(state.Failed.Contains(effect))return;
        try {draw();}catch(Exception ex){Fail(state,effect,ex);}
    }
    private void Fail(ScreenState state,string effect,Exception ex)
    {
        if(state.Failed.Add(effect))monitor.Log($"Modern {effect} paused after a rendering error: {ex.Message}. Use modern_effects reload to retry.",LogLevel.Warn);
    }
    private VisualSettings ReadSettings()
    {
        try {var value=helper.Data.ReadJsonFile<VisualSettings>(SettingsFile)??new VisualSettings();value.Normalize();return value;}
        catch(Exception ex){monitor.Log("Could not read visual-effects.json; using default visual settings. "+ex.Message,LogLevel.Warn);return new VisualSettings();}
    }
    private void Reset()
    {
        foreach(var pair in screens.GetActiveValues())pair.Value.Dispose();
        screens.ResetAllScreens();
    }
    private void Command(string command,string[] args)
    {
        if(args.Length==1&&args[0].Equals("reload",StringComparison.OrdinalIgnoreCase)){settings=ReadSettings();Reset();}
        else if(args.Length==1&&args[0].ToLowerInvariant() is "on" or "off") {settings.Enabled=args[0].Equals("on",StringComparison.OrdinalIgnoreCase);Save();}
        else if(args.Length==2&&args[0].ToLowerInvariant() is "lighting" or "shadows" or "fog" or "moonlight" or "stars")
        {
            string effect=args[0].ToLowerInvariant(),value=args[1].ToLowerInvariant();
            bool? enabled=value=="on"?true:value=="off"?false:null;
            float strength=0;
            if(enabled==null&&(!float.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out strength)||!float.IsFinite(strength)||strength<0||strength>1))
            {monitor.Log("Use on, off, or a strength from 0 to 1.",LogLevel.Info);return;}
            if(effect=="lighting"){if(enabled.HasValue)settings.LightingEnabled=enabled.Value;else settings.LightingStrength=strength;}
            if(effect=="shadows"){if(enabled.HasValue)settings.ShadowsEnabled=enabled.Value;else settings.ShadowStrength=strength;}
            if(effect=="fog"){if(enabled.HasValue)settings.FogEnabled=enabled.Value;else settings.FogStrength=strength;}
            if(effect=="moonlight"){if(enabled.HasValue)settings.MoonlightEnabled=enabled.Value;else settings.MoonlightStrength=strength;}
            if(effect=="stars"){if(enabled.HasValue)settings.StarsEnabled=enabled.Value;else settings.StarsStrength=strength;}
            Save();
        }
        else if(args.Length>0&&!(args.Length==1&&args[0].Equals("status",StringComparison.OrdinalIgnoreCase)))
        {monitor.Log("Usage: modern_effects on|off|status|reload, or modern_effects lighting|shadows|fog|moonlight|stars on|off|0..1",LogLevel.Info);return;}
        monitor.Log($"Visual effects {(settings.Enabled?"on":"off")}; lighting {settings.LightingEnabled} ({settings.LightingStrength:0.##}), shadows {settings.ShadowsEnabled} ({settings.ShadowStrength:0.##}), fog {settings.FogEnabled} ({settings.FogStrength:0.##}), moonlight {settings.MoonlightEnabled} ({settings.MoonlightStrength:0.##}), stars {settings.StarsEnabled} ({settings.StarsStrength:0.##}).",LogLevel.Info);
    }
    private void Save(){settings.Normalize();helper.Data.WriteJsonFile(SettingsFile,settings);Reset();}
}
