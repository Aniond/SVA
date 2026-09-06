using System.Diagnostics;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using xTile;
using xTile.Tiles;

namespace NpcArtAudit;

/// <summary>One native map per update with a shared texture cache and durable checkpoints.</summary>
internal static class LocationsAudit
{
    private static Runner? active;
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        if (active != null) return;
        if (!Game1.IsOnMainThread() || Game1.game1 == null || Game1.game1.isDrawing || Context.IsWorldReady)
        { helper.Data.WriteJsonFile("locations-checks.json", new { Passed=false, Deferred=true }); return; }
        try { active=new Runner(helper,monitor); active.Start(); }
        catch(Exception ex) { helper.Data.WriteJsonFile("locations-checks.json",new{Passed=false,Error=ex.ToString()});active?.Dispose();active=null; }
    }
    private sealed class Runner : IDisposable
    {
        private readonly IModHelper helper; private readonly IMonitor monitor;
        // Plain ContentManager reads native map data only. SMAPI textures are cached separately.
        private readonly ContentManager maps=new(Game1.content.ServiceProvider,Game1.content.RootDirectory);
        private readonly Dictionary<string,Texture2D> textures=new(StringComparer.OrdinalIgnoreCase);
        private readonly List<object> results=new(); private readonly string[] names;
        private readonly HashSet<string> previews=new(){"FarmHouse","SeedShop","Saloon","Hospital","BathHouse_Pool","Sewer","WitchHut","Island_N","Desert","Town","Forest","Beach","Mine","QiNutRoom","MovieTheater"};
        private readonly object player=Game1.player, location=Game1.currentLocation, random=Game1.random, menu=Game1.activeClickableMenu;
        private int index, previewCount; private long peakPrivate; private bool disposed;
        private const long MemoryLimit=8L*1024*1024*1024;
        public Runner(IModHelper helper,IMonitor monitor)
        {
            this.helper=helper;this.monitor=monitor;
            names=helper.Data.ReadJsonFile<string[]>("locations-map-names.json")??throw new InvalidOperationException("Missing map contract");
            if(names.Length!=259||names.Distinct().Count()!=259)throw new InvalidOperationException("Incomplete map contract");
        }
        public void Start()
        {
            helper.Data.WriteJsonFile("locations-checks.json",new{Passed=false,Running=true,StartedAt=DateTime.UtcNow});
            CheckMemory();
            var atlases=helper.Data.ReadJsonFile<AtlasContract[]>("locations-atlas-sizes.json")??throw new InvalidOperationException("Missing atlas contract");
            if(atlases.Length!=106)throw new InvalidOperationException("Incomplete atlas contract");
            foreach(var atlas in atlases){var texture=LoadTexture(atlas.Asset);if(texture.Width!=atlas.Width||texture.Height!=atlas.Height)throw new InvalidOperationException("Atlas size changed: "+atlas.Asset);CheckMemory();}
            helper.Data.WriteJsonFile("locations-progress.json",new{CompletedMaps=0,TotalMaps=names.Length,PeakPrivateBytes=peakPrivate});
            helper.Events.GameLoop.UpdateTicked+=Tick;
        }
        private Texture2D LoadTexture(string asset)
        {
            asset=asset.Replace('\\','/');
            if(!textures.TryGetValue(asset,out var texture)){texture=helper.GameContent.Load<Texture2D>(asset);textures.Add(asset,texture);}
            return texture;
        }
        private void CheckMemory()
        {
            using var process=Process.GetCurrentProcess();long bytes=process.PrivateMemorySize64;peakPrivate=Math.Max(peakPrivate,bytes);
            if(bytes>MemoryLimit)throw new InvalidOperationException("Audit stopped at 8 GiB private-memory safety limit.");
        }
        private void Tick(object? sender,UpdateTickedEventArgs args)
        {
            if(disposed)return;
            try
            {
                if(Context.IsWorldReady||Game1.game1.isDrawing||Game1.uiMode||!ReferenceEquals(Game1.activeClickableMenu,menu))throw new InvalidOperationException("Title-screen context changed; audit stopped.");
                CheckMemory();
                using(var state=new GraphicsState(Game1.graphics.GraphicsDevice))ProcessMap(names[index]);
                maps.Unload();index++;
                if(index%4==0){GC.Collect();GC.WaitForPendingFinalizers();}
                CheckMemory();
                helper.Data.WriteJsonFile("locations-progress.json",new{CompletedMaps=index,TotalMaps=names.Length,LastMap=names[index-1],CachedTextures=textures.Count,PeakPrivateBytes=peakPrivate,PreviewCount=previewCount});
                if(index==names.Length)Finish(null);
            }
            catch(Exception ex){Finish(ex.ToString());}
        }
        private void ProcessMap(string name)
        {
            var map=maps.Load<Map>("Maps/"+name);var device=Game1.graphics.GraphicsDevice;
            using var batch=new SpriteBatch(device);var adapter=new TileDisplay(LoadTexture,batch);map.LoadTileSheets(adapter);
            int frames=0,animated=0;
            foreach(var layer in map.Layers)for(int y=0;y<layer.LayerHeight;y++)for(int x=0;x<layer.LayerWidth;x++)
            {
                var tile=layer.Tiles[x,y];if(tile==null)continue;
                if(tile is AnimatedTile animation){animated++;foreach(var frame in animation.TileFrames){adapter.Check(frame);frames++;}}
                else {adapter.Check(tile);frames++;}
            }
            var files=new List<string>();
            if(previews.Contains(name))
            {
                int width=map.Layers.Max(l=>l.LayerWidth)*16,height=map.Layers.Max(l=>l.LayerHeight)*16;
                using var target=new RenderTarget2D(device,width,height,false,SurfaceFormat.Color,DepthFormat.None);
                foreach(string season in name is "Town" or "Forest" or "Beach"?new[]{"spring","summer","fall","winter"}:new[]{"native"})
                {
                    var renderer=season=="native"?adapter:new TileDisplay(asset=>LoadTexture(System.Text.RegularExpressions.Regex.Replace(asset.Replace('\\','/'),@"(?<=Maps/)(spring|summer|fall|winter)_",season+"_")),batch);
                    map.LoadTileSheets(renderer);device.SetRenderTarget(target);device.Clear(Color.Transparent);
                    batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.PointClamp,DepthStencilState.None,RasterizerState.CullNone);
                    foreach(var layer in map.Layers)if(layer.Id!="Paths")layer.Draw(renderer,new xTile.Dimensions.Rectangle(0,0,width,height),xTile.Dimensions.Location.Origin,false,1,1f);
                    batch.End();device.SetRenderTarget(null);
                    string file="locations-"+name+(season=="native"?"":"-"+season)+".png";
                    using(var stream=File.Create(Path.Combine(helper.DirectoryPath,file)))target.SaveAsPng(stream,width,height);
                    files.Add(file);previewCount++;
                }
            }
            results.Add(new{Map=name,SourceFramesChecked=frames,AnimatedTiles=animated,Previews=files});
        }
        private void Finish(string? error)
        {
            bool state=ReferenceEquals(Game1.player,player)&&ReferenceEquals(Game1.currentLocation,location)&&ReferenceEquals(Game1.random,random)&&ReferenceEquals(Game1.activeClickableMenu,menu);
            try
            {
            helper.Data.WriteJsonFile("locations-checks.json",new{Passed=error==null&&index==259&&state,Error=error,NativeMapsChecked=index,AtlasDimensionsChecked=106,WorldStatePreserved=state,PeakPrivateBytes=peakPrivate,MemoryLimitBytes=MemoryLimit,CachedTextures=textures.Count,PreviewCount=previewCount,Cases=results,Method="Bounded native-map source traversal and xTile GPU layer composites; all animation frame bounds checked, previews show initial frame. Shared texture cache, one map per update, no GameLocation construction or farm/save loading."});
            monitor.Log("Locations audit "+(error==null?"completed.":"stopped: "+error),error==null?LogLevel.Info:LogLevel.Error);
            }
            finally { Dispose();active=null; }
        }
        public void Dispose(){if(disposed)return;disposed=true;helper.Events.GameLoop.UpdateTicked-=Tick;maps.Dispose();textures.Clear();}
    }
    private sealed class AtlasContract{public string Asset{get;set;}="";public int Width{get;set;}public int Height{get;set;}}
    private sealed class TileDisplay : xTile.Display.IDisplayDevice
    {
        private readonly Func<string,Texture2D> load;private readonly SpriteBatch batch;private readonly Dictionary<TileSheet,Texture2D> sheets=new();
        public TileDisplay(Func<string,Texture2D> load,SpriteBatch batch){this.load=load;this.batch=batch;}
        public void LoadTileSheet(TileSheet sheet)=>sheets[sheet]=load(sheet.ImageSource);
        public void DisposeTileSheet(TileSheet sheet)=>sheets.Remove(sheet);
        public void BeginScene(SpriteBatch unused){} public void EndScene(){} public void SetClippingRegion(xTile.Dimensions.Rectangle region){}
        public void Check(Tile tile){var r=tile.TileSheet.GetTileImageBounds(tile.TileIndex);var t=sheets[tile.TileSheet];if(r.X<0||r.Y<0||r.X+r.Width>t.Width||r.Y+r.Height>t.Height)throw new InvalidOperationException($"Out of bounds {tile.TileSheet.ImageSource}:{tile.TileIndex}");}
        public void DrawTile(Tile tile,xTile.Dimensions.Location p,float depth){Check(tile);var r=tile.TileSheet.GetTileImageBounds(tile.TileIndex);batch.Draw(sheets[tile.TileSheet],new Vector2(p.X,p.Y),new Rectangle(r.X,r.Y,r.Width,r.Height),Color.White);}
    }
    private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    private static VertexBufferBinding[] GetVertexBuffers(GraphicsDevice device){var bindings=typeof(GraphicsDevice).GetField("_vertexBuffers",Flags)!.GetValue(device)!;return(VertexBufferBinding[])bindings.GetType().GetMethod("Get",Flags,null,Type.EmptyTypes,null)!.Invoke(bindings,null)!;}
    private sealed class GraphicsState : IDisposable
    {
        private readonly GraphicsDevice device;private readonly RenderTargetBinding[] targets;private readonly Viewport viewport;private readonly Rectangle scissor;
        private readonly BlendState blend;private readonly Color factor;private readonly DepthStencilState depth;private readonly RasterizerState raster;private readonly VertexBufferBinding[] vertices;private readonly IndexBuffer indices;
        private readonly List<(Action Restore,Func<bool> Matches)> slots=new();
        public GraphicsState(GraphicsDevice device)
        {
            this.device=device;targets=device.GetRenderTargets();viewport=device.Viewport;scissor=device.ScissorRectangle;blend=device.BlendState;factor=device.BlendFactor;depth=device.DepthStencilState;raster=device.RasterizerState;vertices=GetVertexBuffers(device);indices=device.Indices;
            foreach(string name in new[]{"Textures","SamplerStates","VertexTextures","VertexSamplerStates"}){var collection=typeof(GraphicsDevice).GetProperty(name)!.GetValue(device)!;var indexer=collection.GetType().GetProperty("Item")!;for(int i=0;i<32;i++){object[] index={i};object? value;try{value=indexer.GetValue(collection,index);}catch(TargetInvocationException ex)when(ex.InnerException is IndexOutOfRangeException or ArgumentOutOfRangeException){break;}slots.Add((()=>indexer.SetValue(collection,value,index),()=>ReferenceEquals(indexer.GetValue(collection,index),value)));}}
        }
        public void Dispose()
        {
            device.SetRenderTargets(targets);device.Viewport=viewport;device.ScissorRectangle=scissor;device.BlendState=blend;device.BlendFactor=factor;device.DepthStencilState=depth;device.RasterizerState=raster;device.SetVertexBuffers(vertices);device.Indices=indices;foreach(var slot in slots)slot.Restore();
            if(!slots.All(s=>s.Matches())||!device.GetRenderTargets().SequenceEqual(targets)||!device.Viewport.Equals(viewport)||device.ScissorRectangle!=scissor||!ReferenceEquals(device.BlendState,blend)||device.BlendFactor!=factor||!ReferenceEquals(device.DepthStencilState,depth)||!ReferenceEquals(device.RasterizerState,raster)||!GetVertexBuffers(device).SequenceEqual(vertices)||!ReferenceEquals(device.Indices,indices))throw new InvalidOperationException("Graphics state restore failed.");
        }
    }
}
