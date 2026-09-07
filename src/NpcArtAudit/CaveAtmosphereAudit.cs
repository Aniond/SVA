using System.Reflection;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;

namespace NpcArtAudit;
internal static class CaveAtmosphereAudit
{
    private const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
    private static object? fixtureController;
    private static bool Eligible(object __instance,ref bool __result) { if(!ReferenceEquals(__instance,fixtureController))return true;__result=true;return false; }
    public static void Run(IModHelper helper,IMonitor monitor)
    {
        if(Context.IsWorldReady||!Game1.IsOnMainThread()||Game1.game1.isDrawing)throw new InvalidOperationException("Cave audit requires title update.");
        var checks=new Dictionary<string,bool>();var cases=new List<object>();string? error=null;
        var device=Game1.graphics.GraphicsDevice;var targets=device.GetRenderTargets();var viewport=device.Viewport;var scissor=device.ScissorRectangle;var blend=device.BlendState;var factor=device.BlendFactor;var depth=device.DepthStencilState;var rasterizer=device.RasterizerState;var indices=device.Indices;
        var vertices=(VertexBufferBinding[])typeof(UtilityBuildingsAudit).GetMethod("GetVertexBuffers",F)!.Invoke(null,new object[]{device})!;
        var slots=(System.Collections.IEnumerable)typeof(UtilityBuildingsAudit).GetMethod("SaveSlots",F)!.Invoke(null,new object[]{device})!;
        var location=Game1.currentLocation;var player=Game1.player;var random=Game1.random;var gv=Game1.viewport;var uv=Game1.uiViewport;var ambient=Game1.ambientLight;var batchGlobal=Game1.spriteBatch;
        var harmony=new Harmony("NpcArtAudit.CaveAtmosphere");object? state=null;Type? stateType=null;
        try
        {
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("AbigailModern.Visuals.CaveAtmosphereController")).First(t=>t!=null)!;
            // Avoid constructor event subscriptions; invoke the exact production collection/draw methods on owned state.
            fixtureController=FormatterServices.GetUninitializedObject(type);stateType=type.GetNestedType("State",F)!;state=Activator.CreateInstance(stateType,true)!;
            var factoryType=typeof(Func<>).MakeGenericType(stateType);
            var factory=Expression.Lambda(factoryType,Expression.Constant(state,stateType)).Compile();
            var screenField=type.GetField("screens",F)!;var screen=Activator.CreateInstance(screenField.FieldType,factory)!;screenField.SetValue(fixtureController,screen);
            var settings=Activator.CreateInstance(type.GetNestedType("Settings",F)!)!;type.GetField("settings",F)!.SetValue(fixtureController,settings);type.GetField("monitor",F)!.SetValue(fixtureController,monitor);
            harmony.Patch(type.GetMethod("Eligible",F)!,prefix:new HarmonyMethod(typeof(CaveAtmosphereAudit),nameof(Eligible)));
            var collect=type.GetMethod("Collect",F)!;var draw=type.GetMethod("Draw",F)!;
            Game1.random=new Random(441709);typeof(Game1).GetField("_player",F)!.SetValue(null,new Farmer());
            using var content=Game1.content.CreateTemporary();using var batch=new SpriteBatch(device);Game1.spriteBatch=batch;
            var map=content.Load<xTile.Map>("Maps/Mines/20");
            var layer=map.GetLayer("Back");var nativeWater=new List<Point>();
            var template=new MineShaft(20){map=map}; int waterTileCount=0;
            for(int y=0;y<layer.LayerHeight;y++)for(int x=0;x<layer.LayerWidth;x++)if(template.isWaterTile(x,y))waterTileCount++;
            for(int y=1;y<layer.LayerHeight;y++)for(int x=0;x<layer.LayerWidth;x++) if(template.isWaterTile(x,y)&&!template.isWaterTile(x,y-1))nativeWater.Add(new Point(x,y));
            checks["NativeWaterPerimeterContractExists"]=nativeWater.Count>0;
            cases.Add(new {Contract="Native layout20",WaterTiles=waterTileCount,UpperWaterPerimeterTiles=nativeWater.Count});
            var center=nativeWater.FirstOrDefault(new Point(layer.LayerWidth/2,layer.LayerHeight/2));
            var worldViewport=new Rectangle(Math.Max(0,center.X*64-384),Math.Max(0,center.Y*64-256),768,576);
            Game1.viewport=new xTile.Dimensions.Rectangle(worldViewport.X,worldViewport.Y,768,576);Game1.uiViewport=Game1.viewport;
            foreach(var spec in new[]{(Level:20,Sheet:"mine"),(Level:60,Sheet:"mine_frost"),(Level:100,Sheet:"mine_lava")})
            {
                var mine=new MineShaft(spec.Level){map=map};Game1.currentLocation=mine;stateType.GetField("Location",F)!.SetValue(state,mine);stateType.GetField("Map",F)!.SetValue(state,map);
                // Real native objects and source routes, with disposable positions away from the water bank.
                foreach(string id in new[]{"343","290","751","8","14"})
                {
                    var obj=ItemRegistry.Create<StardewValley.Object>("(O)"+id);var tile=new Vector2(worldViewport.X/64+2+mine.objects.Pairs.Count(),worldViewport.Y/64+6);obj.TileLocation=tile;mine.objects.Add(tile,obj);
                    checks["NativeBreakableStone:"+id]=obj.IsBreakableStone();
                }
                collect.Invoke(fixtureController,new object[]{state,worldViewport});
                int contacts=((System.Collections.ICollection)stateType.GetField("Contacts",F)!.GetValue(state)!).Count,wet=((System.Collections.ICollection)stateType.GetField("Wet",F)!.GetValue(state)!).Count;
                checks[spec.Sheet+"-caps"]=contacts<=24&&wet<=12&&contacts>0;
                if(spec.Level==100)checks["LavaNoWetDecoration"]=wet==0; else checks[spec.Sheet+"-native-wet-anchors"]=wet>0;
                var sheets=new Dictionary<xTile.Tiles.TileSheet,Texture2D>();
                foreach(var sheet in map.TileSheets)
                {
                    string path=sheet.Id=="mine"||sheet.ImageSource.Contains("mine",StringComparison.OrdinalIgnoreCase)?"Maps/Mines/"+spec.Sheet:sheet.ImageSource.Replace('\\','/');
                    if(path.EndsWith(".png"))path=path[..^4];if(!path.StartsWith("Maps/"))path="Maps/"+Path.GetFileName(path);sheets[sheet]=content.Load<Texture2D>(path);
                }
                void Layer(string name)
                {
                    var l=map.GetLayer(name);if(l==null)return;
                    for(int y=worldViewport.Y/64;y<Math.Min(l.LayerHeight,worldViewport.Bottom/64+1);y++)for(int x=worldViewport.X/64;x<Math.Min(l.LayerWidth,worldViewport.Right/64+1);x++)
                    {var tile=l.Tiles[x,y];if(tile==null||tile.TileIndex<0)continue;var sheet=tile.TileSheet;var source=new Rectangle(tile.TileIndex%sheet.SheetSize.Width*sheet.TileSize.Width,tile.TileIndex/sheet.SheetSize.Width*sheet.TileSize.Height,sheet.TileSize.Width,sheet.TileSize.Height);batch.Draw(sheets[sheet],new Rectangle(x*64-worldViewport.X,y*64-worldViewport.Y,64,64),source,Color.White);}
                }
                Color[] Render(bool on,double seconds)
                {
                    stateType.GetField("Seconds",F)!.SetValue(state,seconds);
                    using var target=new RenderTarget2D(device,768,576);device.SetRenderTarget(target);device.Clear(new Color(35,40,45));
                    batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.PointClamp);Layer("Back");if(on)draw.Invoke(fixtureController,new object[]{batch,false});Layer("Buildings");batch.End();
                    batch.Begin(SpriteSortMode.FrontToBack,BlendState.AlphaBlend,SamplerState.PointClamp);foreach(var pair in mine.objects.Pairs)pair.Value.draw(batch,(int)pair.Key.X,(int)pair.Key.Y,1);batch.End();
                    batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.PointClamp);Layer("Front");if(on)draw.Invoke(fixtureController,new object[]{batch,true});batch.End();device.SetRenderTarget(null);
                    var pixels=new Color[768*576];target.GetData(pixels);using var output=File.Create(Path.Combine(helper.DirectoryPath,$"cave-{spec.Sheet}-{on}-{seconds:0}.png"));target.SaveAsPng(output,768,576);return pixels;
                }
                Game1.random=new Random(441709); var before=Render(false,0);var after=Render(true,0);var timed=Render(true,4); if(spec.Level!=100)checks[spec.Sheet+"-wet-animation-visible"]=!after.SequenceEqual(timed);
                checks[spec.Sheet+"-cosmetic-RNG-unchanged"]=Game1.random.Next()==new Random(441709).Next();
                checks[spec.Sheet+"-effect-draws"]=!before.SequenceEqual(after);
                checks[spec.Sheet+"-no-production-error"]=!(bool)stateType.GetField("Failed",F)!.GetValue(state)!;
                checks[spec.Sheet+"-ambient-unchanged"]=Game1.ambientLight==ambient;
                int changed=before.Where((c,i)=>c!=after[i]).Count();checks[spec.Sheet+"-bounded-pixel-impact"]=changed>0&&changed<768*576/8;
                cases.Add(new{spec.Level,spec.Sheet,Contacts=contacts,Wet=wet,ChangedPixels=changed,AnimationChanged=!after.SequenceEqual(timed)});
            }

        }
        catch(Exception ex){error=ex.ToString();}
        finally
        {
            harmony.UnpatchAll(harmony.Id);if(state!=null)stateType!.GetMethod("Reset",F)!.Invoke(state,null);fixtureController=null;
            typeof(Game1).GetField("_player",F)!.SetValue(null,player);Game1.currentLocation=location;Game1.random=random;Game1.viewport=gv;Game1.uiViewport=uv;Game1.ambientLight=ambient;Game1.spriteBatch=batchGlobal;
            device.SetRenderTargets(targets);device.Viewport=viewport;device.ScissorRectangle=scissor;device.BlendState=blend;device.BlendFactor=factor;device.DepthStencilState=depth;device.RasterizerState=rasterizer;device.SetVertexBuffers(vertices);device.Indices=indices;
            foreach(var slot in slots)((Action)slot.GetType().GetProperty("Restore")!.GetValue(slot)!)();
            checks["GlobalsRestored"]=ReferenceEquals(Game1.currentLocation,location)&&ReferenceEquals(Game1.player,player)&&ReferenceEquals(Game1.random,random)&&Game1.ambientLight==ambient;
        }
        helper.Data.WriteJsonFile("cave-atmosphere-checks.json",new{Passed=error==null&&checks.Values.All(v=>v),Error=error,Checks=checks,Cases=cases,Scope="Actual MineShaft objects, native layout20 Water/perimeter contracts and native biome atlases; exact production contact/surface methods at their ground/world stages. No mine generation or save.",Limitations="Detached map crop, not full native lighting pass; ambient untouched. No pickaxe destruction/drop/ladder simulation, resource-clump interaction, dangerous floors, live audio or multiplayer tested. Native IsBreakableStone contract checked only. Controller relocates the single native drip cue; that audio route is not exercised by this fixture. No added ambient loops."});
    }
}
