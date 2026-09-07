using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using StardewValley.Audio;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Mods;

namespace AbigailModern.Visuals;

/// <summary>Grounded cave rocks and sparse water details. Native ambient light and audio remain authoritative.</summary>
internal sealed class CaveAtmosphereController
{
    public sealed class Settings { public bool Enabled {get;set;}=true; public bool LocalizedDrips {get;set;}=true; public float Strength {get;set;}=.18f; }
    private static CaveAtmosphereController? audioOwner;
    private readonly PerScreen<State> screens=new(()=>new State());
    private readonly Settings settings;
    private readonly IMonitor monitor;
    private sealed class State
    {
        internal GameLocation? Location;
        internal object? Map;
        internal double Seconds,Collected=-1;
        internal Rectangle Viewport;
        internal readonly List<(Point Tile,StardewValley.Object Object)> Contacts=new();
        internal readonly List<Point> Wet=new();
        internal Texture2D? Patch;
        internal bool Failed;
        internal double NextDrip;
        internal void Reset() { Location=null; Map=null; Seconds=0; NextDrip=0; Collected=-1; Contacts.Clear();Wet.Clear(); Patch?.Dispose();Patch=null; Failed=false; }
    }
    public CaveAtmosphereController(IModHelper helper,IMonitor monitor)
    {
        this.monitor=monitor; settings=helper.Data.ReadJsonFile<Settings>("cave-atmosphere.json")??new Settings();
        var harmony=new Harmony("David.AbigailModern.CaveDrip");
        try
        {
            harmony.Patch(AccessTools.Method(typeof(MineShaft),nameof(MineShaft.UpdateWhenCurrentLocation),new[]{typeof(GameTime)}),transpiler:new HarmonyMethod(typeof(CaveAtmosphereController),nameof(RouteNativeDripCall)));
            audioOwner=this;
        }
        catch(Exception ex) { harmony.UnpatchAll(harmony.Id); monitor.Log("Cave drip localization unavailable; native audio retained: "+ex.Message,LogLevel.Warn); }
        helper.Events.GameLoop.UpdateTicked+=(_,_)=>
        {
            var state=screens.Value;
            if(!ReferenceEquals(state.Location,Game1.currentLocation)||!ReferenceEquals(state.Map,Game1.currentLocation?.Map))
            { state.Reset();state.Location=Game1.currentLocation;state.Map=state.Location?.Map; }
            if(Eligible() && Game1.shouldTimePass()) state.Seconds+=Math.Clamp(Game1.currentGameTime.ElapsedGameTime.TotalSeconds,0,.1);
        };
        helper.Events.Display.RenderedStep+=(_,e)=> { if(e.Step==RenderSteps.World_Background) Draw(e.SpriteBatch,false); };
        helper.Events.Display.RenderedWorld+=(_,e)=>Draw(e.SpriteBatch,true);
        helper.Events.GameLoop.ReturnedToTitle+=(_,_)=> {foreach(var state in screens.GetActiveValues()) state.Value.Reset();};
        monitor.Log("Cave atmosphere ready: rock contact shadows, water-edge details and native drips localized to nearby water. Native ambient beds and lighting preserved.",LogLevel.Info);
    }
    private static IEnumerable<CodeInstruction> RouteNativeDripCall(IEnumerable<CodeInstruction> instructions)
    {
        var list=instructions.ToList();
        var native=AccessTools.Method(typeof(GameLocation),nameof(GameLocation.localSound),new[]{typeof(string),typeof(Vector2?),typeof(int?),typeof(SoundContext)});
        int matches=0;
        for(int i=0;i<list.Count;i++)
        {
            if(list[i].opcode!=OpCodes.Ldstr || !Equals(list[i].operand,"cavedrip"))continue;
            int call=-1;
            for(int j=i+1;j<Math.Min(list.Count,i+20);j++)
            {
                if(list[j].Calls(native)){call=j;break;}
                if(list[j].opcode==OpCodes.Call || list[j].opcode==OpCodes.Callvirt || list[j].opcode.FlowControl==FlowControl.Branch)break;
            }
            if(call>=0){list[call].opcode=OpCodes.Call;list[call].operand=AccessTools.Method(typeof(CaveAtmosphereController),nameof(PlayNativeDrip));matches++;}
        }
        if(matches!=1)throw new InvalidOperationException("Expected exactly one native MineShaft cavedrip call; found "+matches);
        return list;
    }
    private static void PlayNativeDrip(GameLocation location,string cue,Vector2? position,int? pitch,SoundContext context)
    {
        var owner=audioOwner;
        if(owner==null || !owner.settings.Enabled || !owner.settings.LocalizedDrips)
        {location.localSound(cue,position,pitch,context);return;}
        if(!owner.Eligible() || !ReferenceEquals(location,Game1.currentLocation) || !Game1.shouldTimePass() || Game1.options.soundVolumeLevel<=0 || Game1.options.ambientVolumeLevel<=0)return;
        if(location is MineShaft mine && !CaveAtmospherePolicy.WetAllowed(true,mine.getMineArea()))return;
        var state=owner.screens.Value;
        if(!ReferenceEquals(state.Location,location)) {state.Reset();state.Location=location;state.Map=location.Map;}
        if(state.Seconds<state.NextDrip)return;
        // The native cue remains the only trigger. Select a real nearby water tile, not a screen-wide sound.
        var playerTile=Game1.player.TilePoint;Point? anchor=null;int best=int.MaxValue;
        for(int y=Math.Max(0,playerTile.Y-8);y<=playerTile.Y+8;y++)for(int x=Math.Max(0,playerTile.X-8);x<=playerTile.X+8;x++)
        {
            int distance=(x-playerTile.X)*(x-playerTile.X)+(y-playerTile.Y)*(y-playerTile.Y);
            if(distance>64||distance>=best||!location.isTileOnMap(x,y)||!location.isWaterTile(x,y))continue;
            anchor=new Point(x,y);best=distance;
        }
        if(anchor is not Point tile)return;
        state.NextDrip=state.Seconds+CaveAtmospherePolicy.DripCooldown(tile.X,tile.Y);
        // localSound position is measured in tiles; no music/category volume is changed.
        location.localSound(cue,new Vector2(tile.X+.5f,tile.Y+.5f),pitch,context);
    }
    private bool Eligible() => settings.Enabled && Context.IsWorldReady && Game1.currentLocation is MineShaft or FarmCave && !Game1.eventUp && Game1.currentMinigame==null && !Game1.game1.takingMapScreenshot;
    private void Collect(State state,Rectangle viewport)
    {
        var location=Game1.currentLocation; state.Contacts.Clear();state.Wet.Clear();
        int minX=Math.Max(0,viewport.Left/64-1),minY=Math.Max(0,viewport.Top/64-1);
        int maxX=Math.Min(minX+64,viewport.Right/64+2),maxY=Math.Min(minY+64,viewport.Bottom/64+2);
        bool wet=CaveAtmospherePolicy.WetAllowed(location is MineShaft,location is MineShaft mine?mine.getMineArea():0);
        for(int y=minY;y<maxY;y++) for(int x=minX;x<maxX;x++)
        {
            if(!location.isTileOnMap(x,y)) continue;
            var tile=new Vector2(x,y);
            if(state.Contacts.Count<CaveAtmospherePolicy.ContactLimit && location.objects.TryGetValue(tile,out var obj) && obj.Name=="Stone" && obj.Type=="Litter")
                state.Contacts.Add((new Point(x,y),obj));
            // Validated native Water metadata at its upper perimeter; a separate Buildings tile is not required for native lake banks.
            if(wet && state.Wet.Count<CaveAtmospherePolicy.WetLimit && y>0 && location.isWaterTile(x,y) && !location.isWaterTile(x,y-1) && CaveAtmospherePolicy.Hash(x,y)%3==0)
                state.Wet.Add(new Point(x,y));
        }
        state.Viewport=viewport;state.Collected=state.Seconds;
    }
    private void Draw(SpriteBatch batch,bool surface)
    {
        if(!Eligible())return;
        var state=screens.Value;if(state.Failed)return;
        try
        {
            var viewport=new Rectangle(Game1.viewport.X,Game1.viewport.Y,Game1.viewport.Width,Game1.viewport.Height);
            if(state.Collected<0 || state.Seconds-state.Collected>=.5 || viewport!=state.Viewport) Collect(state,viewport);
            float strength=CaveAtmospherePolicy.Strength(settings.Strength);
            if(!surface)
            {
                state.Patch ??= ContactTexture(batch.GraphicsDevice);
                foreach(var anchor in state.Contacts)
                {
                    if(!Game1.currentLocation.objects.TryGetValue(new Vector2(anchor.Tile.X,anchor.Tile.Y),out var current)||!ReferenceEquals(current,anchor.Object))continue;
                    var rect=new Rectangle(anchor.Tile.X*64+8-viewport.X,anchor.Tile.Y*64+47-viewport.Y,48,14);
                    batch.Draw(state.Patch,rect,Color.Black*strength);
                }
                return;
            }
            int particles=0;
            foreach(var tile in state.Wet)
            {
                if(!Game1.currentLocation.isWaterTile(tile.X,tile.Y))continue;
                int x=tile.X*64+32-viewport.X,y=tile.Y*64+22-viewport.Y;
                // Water-only glints cannot cover ore, paths or ladder silhouettes.
                float glint=.45f+.3f*MathF.Sin((float)state.Seconds*.7f+(CaveAtmospherePolicy.Hash(tile.X,tile.Y)%31));
                var ink=new Color(170,200,215)*(strength*glint);
                batch.Draw(Game1.staminaRect,new Rectangle(x-5,y,10,1),ink);
                double phase=CaveAtmospherePolicy.Phase(state.Seconds,tile.X,tile.Y);
                if(phase>1.1 || particles>=CaveAtmospherePolicy.ParticleLimit)continue;
                particles++;
                if(phase<.55) batch.Draw(Game1.staminaRect,new Rectangle(x,y-18+(int)(phase/.55*18),1,3),new Color(180,210,225)*(strength*.8f));
                else
                {
                    int radius=2+(int)((phase-.55)/.55*6);float alpha=(float)(1-(phase-.55)/.55);
                    batch.Draw(Game1.staminaRect,new Rectangle(x-radius,y,2*radius,1),new Color(180,210,225)*(strength*alpha));
                }
            }
        }
        catch(Exception ex) {state.Failed=true;monitor.Log("Cave atmosphere paused for this location: "+ex.Message,LogLevel.Warn);}
    }
    private static Texture2D ContactTexture(GraphicsDevice device)
    {
        const int width=32,height=16;var pixels=new Color[width*height];
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
        { float dx=(x+0.5f-width/2f)/(width/2f),dy=(y+.5f-height/2f)/(height/2f);float a=Math.Clamp(1-dx*dx-dy*dy,0,1);pixels[y*width+x]=Color.White*(a*a); }
        var texture=new Texture2D(device,width,height);texture.SetData(pixels);return texture;
    }
}
