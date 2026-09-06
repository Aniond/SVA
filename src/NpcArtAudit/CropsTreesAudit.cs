using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
namespace NpcArtAudit;
/// <summary>Detached crop source-rectangle composites; no growth, harvesting, or save mutations.</summary>
internal static class CropsTreesAudit
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        if (!Game1.IsOnMainThread() || Game1.game1 == null || Game1.game1.isDrawing || Game1.uiMode || Game1.uiModeCount != 0)
        { helper.Data.WriteJsonFile("crops-trees-checks.json", new { Passed=false, Deferred=true }); return; }
        var device=Game1.graphics.GraphicsDevice;
        var targets=device.GetRenderTargets(); var viewport=device.Viewport; var scissor=device.ScissorRectangle;
        var blend=device.BlendState; var factor=device.BlendFactor; var depth=device.DepthStencilState;
        var raster=device.RasterizerState; var vertices=GetVertexBuffers(device); var indices=device.Indices; var slots=SaveSlots(device);
        var location=Game1.currentLocation; var player=Game1.player; var random=Game1.random; var menu=Game1.activeClickableMenu; var worldViewport=Game1.viewport;
        var checks=new Dictionary<string,bool>(); var cases=new List<object>(); string? error=null;
        try
        {
            var definitions=DataLoader.Crops(Game1.content); var giants=DataLoader.GiantCrops(Game1.content);
            helper.Data.WriteJsonFile("crops-native-definitions.json",definitions); helper.Data.WriteJsonFile("crops-native-giants.json",giants);
            checks["FiftyNativeDefinitions"]=definitions.Count==50; checks["FiveGiantDefinitions"]=giants.Count==5;
            string output=Path.Combine(helper.DirectoryPath,"crops-previews"); Directory.CreateDirectory(output);
            using var batch=new SpriteBatch(device);
            var cache=new Dictionary<Texture2D,Color[]>();
            void Sample(string id,string asset,Texture2D texture,Rectangle source, Rectangle? overlay=null, Color? tint=null)
            {
                if(!cache.TryGetValue(texture,out var pixels)){pixels=new Color[texture.Width*texture.Height];texture.GetData(pixels);cache.Add(texture,pixels);}
                bool bounds=texture.Bounds.Contains(source) && (!overlay.HasValue || texture.Bounds.Contains(overlay.Value));
                checks[id+"-Bounds"]=bounds;
                checks[id+"-Visible"]=bounds && Enumerable.Range(source.Y,source.Height).Any(y=>Enumerable.Range(source.X,source.Width).Any(x=>pixels[y*texture.Width+x].A!=0));
                if(overlay.HasValue) checks[id+"-OverlayVisible"]=bounds && Enumerable.Range(overlay.Value.Y,overlay.Value.Height).Any(y=>Enumerable.Range(overlay.Value.X,overlay.Value.Width).Any(x=>pixels[y*texture.Width+x].A!=0));
                cases.Add(new {Id=id,Asset=asset,Source=source,Overlay=overlay,Tint=tint,TextureWidth=texture.Width,TextureHeight=texture.Height});
                if(!bounds)return;
                using var target=new RenderTarget2D(device,Math.Max(64,source.Width*4),Math.Max(128,source.Height*4));
                device.SetRenderTarget(target);device.Clear(Color.Transparent);
                batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.PointClamp);
                batch.Draw(texture,Vector2.Zero,source,Color.White,0,Vector2.Zero,4,SpriteEffects.None,0);
                if(overlay.HasValue)batch.Draw(texture,Vector2.Zero,overlay.Value,tint??Color.White,0,Vector2.Zero,4,SpriteEffects.None,0);
                batch.End();device.SetRenderTarget(null);
                using var stream=File.Create(Path.Combine(output,id+".png")); target.SaveAsPng(stream,target.Width,target.Height);
            }
            foreach(var pair in definitions)
            {
                var d=pair.Value;
                var crop=new Crop(); crop.currentLocation=new GameLocation(); crop.netSeedIndex.Value=pair.Key;
                crop.rowInSpriteSheet.Value=d.SpriteIndex;crop.indexOfHarvest.Value=d.HarvestItemId;crop.overrideTexturePath.Value=d.Texture;
                crop.phaseDays.AddRange(d.DaysInPhase);crop.phaseDays.Add(99999);crop.raisedSeeds.Value=d.IsRaised;crop.tintColor.Value=Color.White;
                foreach(var season in Enum.GetValues<Season>())
                {
                    typeof(GameLocation).GetField("seasonOverride",Flags)!.SetValue(crop.currentLocation,new Lazy<Season?>(()=>season));
                    int max=d.DaysInPhase.Count;
                    for(int phase=0;phase<=max;phase++)
                    {
                        crop.currentPhase.Value=phase;crop.phaseToShow.Value=-1;crop.fullyGrown.Value=false;
                        foreach(int variant in new[]{0,1})
                        {
                            crop.updateDrawMath(new Vector2(variant+1,1));
                            var rect=crop.getSourceRect(variant);
                            Sample($"{pair.Key}-{season}-phase{phase}-v{variant}",d.Texture,crop.DrawnCropTexture,rect);
                        }
                    }
                    if(d.SpriteIndex==23)for(int phase=1;phase<=6;phase++)
                    {crop.phaseToShow.Value=phase; Sample($"{pair.Key}-{season}-wild{phase}",d.Texture,crop.DrawnCropTexture,crop.getSourceRect(0));}
                    crop.phaseToShow.Value=-1;crop.currentPhase.Value=max;
                    if(d.RegrowDays>0)foreach(int days in new[]{0,d.RegrowDays})
                    {crop.fullyGrown.Value=true;crop.dayOfCurrentPhase.Value=days;crop.updateDrawMath(new Vector2(1,1));Sample($"{pair.Key}-{season}-regrow{days}",d.Texture,crop.DrawnCropTexture,crop.sourceRect);}
                    crop.fullyGrown.Value=false;crop.dayOfCurrentPhase.Value=0;crop.updateDrawMath(new Vector2(1,1));
                    if(d.TintColors!=null)for(int t=0;t<d.TintColors.Count;t++)
                    { var color=Utility.StringToColor(d.TintColors[t]); checks[$"{pair.Key}-tint{t}-Valid"]=color.HasValue;Sample($"{pair.Key}-{season}-tint{t}",d.Texture,crop.DrawnCropTexture,crop.sourceRect,crop.coloredSourceRect,color); }
                }
            }
            var sheet=Game1.content.Load<Texture2D>("TileSheets/crops");
            for(int v=0;v<4;v++){var crop=new Crop();crop.dead.Value=true;Sample("dead"+v,"TileSheets/crops",sheet,crop.getSourceRect(v));}
            foreach(var p in giants){var d=p.Value;Sample("giant-"+p.Key,d.Texture,Game1.content.Load<Texture2D>(d.Texture),new Rectangle(d.TexturePosition.X,d.TexturePosition.Y,d.TileSize.X*16,(d.TileSize.Y+1)*16));}
            var cursors=Game1.content.Load<Texture2D>("LooseSprites/Cursors");
            for(int v=0;v<3;v++){Sample("spring-onion"+v,"LooseSprites/Cursors",cursors,new Rectangle(v*16,144,16,16));Sample("ginger-offset"+v,"LooseSprites/Cursors",cursors,new Rectangle(v*16,160,16,16));}
            for(int v=0;v<4;v++)Sample("ginger-animation"+v,"LooseSprites/Cursors",cursors,new Rectangle(128+v*16,128,16,16));
            foreach(string id in new[]{"16","18","20","22","396","398","402","404","406","408","410","412","414","416","418"})
            {var item=ItemRegistry.GetDataOrErrorItem("(O)"+id);Sample("wild-mature-"+id,item.GetTexture().Name,item.GetTexture(),item.GetSourceRect());}
        }
        catch(Exception ex){error=ex.ToString();}
        finally
        {
            device.SetRenderTargets(targets);device.Viewport=viewport;device.ScissorRectangle=scissor;device.BlendState=blend;device.BlendFactor=factor;
            device.DepthStencilState=depth;device.RasterizerState=raster;device.SetVertexBuffers(vertices);device.Indices=indices;foreach(var slot in slots)slot.Restore();
            checks["StateRestored"]=device.GetRenderTargets().SequenceEqual(targets)&&device.Viewport.Equals(viewport)&&device.ScissorRectangle==scissor&&ReferenceEquals(device.BlendState,blend)&&device.BlendFactor==factor&&ReferenceEquals(device.DepthStencilState,depth)&&ReferenceEquals(device.RasterizerState,raster)&&GetVertexBuffers(device).SequenceEqual(vertices)&&ReferenceEquals(device.Indices,indices)&&slots.All(s=>s.Matches())&&ReferenceEquals(Game1.player,player)&&ReferenceEquals(Game1.currentLocation,location)&&ReferenceEquals(Game1.random,random)&&ReferenceEquals(Game1.activeClickableMenu,menu)&&Game1.viewport.Equals(worldViewport);
        }
        bool passed=error==null&&checks.Count>1&&checks.Values.All(v=>v);
        helper.Data.WriteJsonFile("crops-trees-checks.json",new{Passed=passed,Error=error,Scope="Detached native Crop.getSourceRect/updateDrawMath + GPU composites; all phases, four seasons, regrowth, tint palette, wild variants, mature forage, dead variants, giants. No native growth/harvest/draw, save, world, or random changes.",Checks=checks,Cases=cases});
        monitor.Log("Crop fixtures "+(passed?"passed.":"FAILED: "+error),passed?LogLevel.Info:LogLevel.Error);
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

