using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace NpcArtAudit;

internal static class VegetationAudit
{
    private sealed class TestTree : Tree
    {
        public TestTree(string id,int stage,Season season):base(id,stage){localSeason=season;Tile=new Vector2(2,5);}
        public string? Selected()=>ChooseTexture();
    }
    private sealed class TestBush : Bush
    {
        public Season TestSeason;
        public override Season GetCosmeticSeason()=>TestSeason;
    }
    public static void Run(IModHelper helper,IMonitor monitor)
    {
        var random=Game1.random;var viewport=Game1.viewport;
        var device=Game1.graphics.GraphicsDevice;var targets=device.GetRenderTargets();var deviceViewport=device.Viewport;
        var blend=device.BlendState;var depth=device.DepthStencilState;var raster=device.RasterizerState;
        var sampler=device.SamplerStates[0];var texture=device.Textures[0];var scissor=device.ScissorRectangle;
        var cases=new List<object>();string? error=null;
        try
        {
            Game1.random=new Random(801);
            Game1.viewport=new xTile.Dimensions.Rectangle(0,0,320,448);
            var directory=Path.Combine(helper.DirectoryPath,"vegetation-runtime-previews");Directory.CreateDirectory(directory);
            using var target=new RenderTarget2D(device,320,448);using var batch=new SpriteBatch(device);
            foreach(var id in new[]{"1","2","3"})foreach(var season in Enum.GetValues<Season>())
            {
                for(int stage=0;stage<=5;stage++)
                {
                    var location=new GameLocation();
                    helper.Reflection.GetField<Lazy<Season?>>(location,"seasonOverride").SetValue(new Lazy<Season?>(()=>season));
                    var tree=new TestTree(id,stage,season);tree.Location=location;tree.flipped.Value=false;
                    var selected=tree.Selected()??throw new Exception("No tree texture selected");
                    var expectedSeason=season==Season.Summer&&id=="3"?"spring":season.ToString().ToLowerInvariant();
                    var expected="TerrainFeatures/tree"+id+"_"+expectedSeason;
                    if(!selected.Replace((char)92,(char)47).Equals(expected,StringComparison.OrdinalIgnoreCase))throw new Exception("Wrong seasonal tree texture: "+selected);
                    var sheet=helper.GameContent.Load<Texture2D>(selected);
                    if(sheet.Height!=160||sheet.Width<48)throw new Exception("Tree atlas geometry changed: "+selected);
                    device.SetRenderTarget(target);device.Clear(new Color(35,45,38));
                    batch.Begin(samplerState:SamplerState.PointClamp);try{tree.draw(batch);}finally{batch.End();}
                    var pixels=new Color[320*448];target.GetData(pixels);
                    if(!pixels.Any(p=>p!=new Color(35,45,38)))throw new Exception("Empty tree draw");
                    if(stage==5)
                    {
                        device.SetRenderTargets(targets);
                        using var file=File.Create(Path.Combine(directory,$"tree-{id}-{season}.png"));target.SaveAsPng(file,320,448);
                    }
                    cases.Add(new{Kind="Tree",Id=id,Season=season.ToString(),Stage=stage,Texture=selected});
                }
            }
            var bushes=helper.GameContent.Load<Texture2D>("TileSheets/bushes");
            foreach(var season in Enum.GetValues<Season>())foreach(var size in new[]{0,1,2})foreach(var town in new[]{false,true})foreach(var offset in new[]{0,1})
            {
                var bush=new TestBush{TestSeason=season};bush.size.Value=size;bush.townBush.Value=town;bush.tileSheetOffset.Value=offset;bush.setUpSourceRect();
                var rect=bush.sourceRect.Value;
                if(rect.X<0||rect.Y<0||rect.Right>bushes.Width||rect.Bottom>bushes.Height)throw new Exception("Bush source outside atlas");
                var pixels=new Color[rect.Width*rect.Height];bushes.GetData(0,rect,pixels,0,pixels.Length);
                var reserved=season==Season.Winter&&size==1&&!town&&offset==1;
                if(pixels.Any(p=>p.A>0)==reserved)throw new Exception("Bush occupancy differs from native reserved-slot contract");
                cases.Add(new{Kind="Bush",Season=season.ToString(),Size=size,Town=town,Offset=offset,Rectangle=rect});
            }
        }
        catch(Exception ex){error=ex.ToString();}
        finally
        {
            device.SetRenderTargets(targets);device.Viewport=deviceViewport;device.ScissorRectangle=scissor;
            device.BlendState=blend;device.DepthStencilState=depth;device.RasterizerState=raster;device.SamplerStates[0]=sampler;device.Textures[0]=texture;
            Game1.random=random;Game1.viewport=viewport;
        }
        helper.Data.WriteJsonFile("vegetation-checks.json",new{Passed=error==null,Error=error,Cases=cases,RandomRestored=ReferenceEquals(Game1.random,random),FarmLoaded=false,SaveWritten=false,Scope="Native tree texture selection and growth-stage draw; native ordinary bush source selection. No chopping, berry harvesting, full scenes, green-rain or fruit-tree gameplay."});
        monitor.Log(error==null?"Vegetation native selection/draw checks passed.":"Vegetation check failed: "+error,error==null?LogLevel.Info:LogLevel.Error);
    }
}
