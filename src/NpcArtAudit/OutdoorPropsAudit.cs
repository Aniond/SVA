using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace NpcArtAudit;

internal static class OutdoorPropsAudit
{
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        var random=Game1.random;var viewport=Game1.viewport;var gameTime=Game1.currentGameTime;
        var device=Game1.graphics.GraphicsDevice;var targets=device.GetRenderTargets();var deviceViewport=device.Viewport;
        var blend=device.BlendState;var depth=device.DepthStencilState;var raster=device.RasterizerState;
        var sampler=device.SamplerStates[0];var texture=device.Textures[0];var scissor=device.ScissorRectangle;
        int floorCases=0,fenceCases=0,chestCases=0,furnitureCases=0,bridgeCases=0,garbageFrames=0;var items=new List<object>();string? error=null;
        try
        {
            Game1.random=new Random(802);Game1.viewport=new xTile.Dimensions.Rectangle(0,0,512,320);
            var directory=Path.Combine(helper.DirectoryPath,"outdoor-props-previews");Directory.CreateDirectory(directory);
            using var target=new RenderTarget2D(device,512,320);using var batch=new SpriteBatch(device);
            void Draw(Action action,string? filename=null)
            {
                device.SetRenderTarget(target);device.Clear(new Color(35,45,38));
                batch.Begin(samplerState:SamplerState.PointClamp);try{action();}finally{batch.End();}
                if(filename!=null){device.SetRenderTargets(targets);using var file=File.Create(Path.Combine(directory,filename+".png"));target.SaveAsPng(file,512,320);}
            }
            foreach(var entry in Game1.bigCraftableData)
            {
                var data=ItemRegistry.GetDataOrErrorItem("(BC)"+entry.Key);var sheet=data.GetTexture();var rect=data.GetSourceRect();
                if(rect.X<0||rect.Y<0||rect.Right>sheet.Width||rect.Bottom>sheet.Height)throw new Exception("Prop bounds: "+entry.Key);
                items.Add(new{Id=entry.Key,Name=entry.Value.Name,Rectangle=rect,Texture=entry.Value.Texture??"TileSheets/Craftables"});
            }
            foreach(var id in new[]{"93","94","298","322","323","324","325","599","621","645","710"})
            {
                var data=ItemRegistry.GetDataOrErrorItem("(O)"+id);var sheet=data.GetTexture();var rect=data.GetSourceRect();
                if(data.IsErrorItem||rect.X<0||rect.Y<0||rect.Right>sheet.Width||rect.Bottom>sheet.Height)throw new Exception("Small prop bounds: "+id);
                items.Add(new{Id=id,Name=data.InternalName,Rectangle=rect,Texture=data.TextureName});
            }
            var cursors2=helper.GameContent.Load<Texture2D>("LooseSprites/Cursors2");
            for(int season=0;season<4;season++)foreach(var rect in new[]{new Rectangle(22+17*season,0,16,10),new Rectangle(22+17*season,11,16,16)})
            {
                var pixels=new Color[rect.Width*rect.Height];cursors2.GetData(0,rect,pixels,0,pixels.Length);
                if(!pixels.Any(p=>p.A>0))throw new Exception("Garbage lid/body source is empty");garbageFrames++;
            }
            foreach(var entry in DataLoader.Furniture(Game1.content))
            {
                var data=ItemRegistry.GetDataOrErrorItem("(F)"+entry.Key);
                var sheet=data.GetTexture();var furniture=new Furniture(entry.Key,new Vector2(1,2));
                for(int turn=0;turn<Math.Max(1,furniture.rotations.Value);turn++)
                {
                    var rect=furniture.sourceRect.Value;
                    if(rect.X<0||rect.Y<0||rect.Right>sheet.Width||rect.Bottom>sheet.Height)throw new Exception("Furniture rotation bounds: "+entry.Key);
                    furnitureCases++;furniture.rotate();
                }
            }
            foreach(var season in new[]{Season.Spring,Season.Winter})for(int id=0;id<13;id++)
            {
                var location=new GameLocation();helper.Reflection.GetField<Lazy<Season?>>(location,"seasonOverride").SetValue(new Lazy<Season?>(()=>season));
                var floor=new Flooring(id.ToString()){Location=location,Tile=new Vector2(2,2)};
                if(floor.ShouldDrawWinterVersion()!=(season==Season.Winter))throw new Exception("Wrong floor season");
                for(int mask=0;mask<256;mask++)
                {
                    helper.Reflection.GetField<byte>(floor,"neighborMask").SetValue((byte)mask);floor.whichView.Value=mask%16;
                    Draw(()=>floor.draw(batch),mask==15?$"floor-{id}-{season}":null);floorCases++;
                }
            }
            var directions=new[]{new Vector2(1,0),new Vector2(-1,0),new Vector2(0,1),new Vector2(0,-1)};
            var weights=new[]{100,10,500,1000};
            foreach(var id in new[]{"322","323","324","298"})for(int mask=0;mask<16;mask++)foreach(var gate in new[]{false,true})foreach(var open in new[]{false,true})
            {
                if(!gate&&open)continue;
                var location=new GameLocation();var center=new Vector2(2,3);var fence=new Fence(center,id,gate){Location=location};int expected=0;
                for(int bit=0;bit<4;bit++)if((mask&(1<<bit))!=0){var pos=center+directions[bit];location.objects[pos]=new Fence(pos,id,false){Location=location};expected+=weights[bit];}
                if(fence.getDrawSum()!=expected)throw new Exception("Fence connection routing differs");
                fence.gatePosition.Value=open?88:0;
                if(gate&&fence.isPassable()!=open)throw new Exception("Gate passability differs");
                Draw(()=>fence.draw(batch,2,3),gate&&mask==3?$"gate-{id}-{open}":null);fenceCases++;
            }
            var bridge=new StardewValley.BellsAndWhistles.SuspensionBridge(1,3);
            bridge.OnFootstep(new Vector2(bridge.bridgeBounds.X+160,bridge.bridgeBounds.Y+32));
            if(bridge.shakeTime!=.4f)throw new Exception("Bridge footstep did not start sway");
            for(int frame=0;frame<=10;frame++)
            {
                bridge.shakeTime=Math.Max(0,.4f-frame*.04f);Game1.currentGameTime=new GameTime(TimeSpan.FromSeconds(frame*.04),TimeSpan.FromSeconds(.04));
                Draw(()=>bridge.Draw(batch),$"suspension-bridge-{frame}");bridgeCases++;
            }
            foreach(var id in new[]{"130","232","BigChest","BigStoneChest"})foreach(var tint in new[]{Color.White,Color.CornflowerBlue})
            {
                var chest=new Chest(true,new Vector2(2,2),id);chest.playerChoiceColor.Value=tint;
                for(int frame=0;frame<chest.lidFrameCount.Value;frame++)
                {
                    helper.Reflection.GetField<int>(chest,"currentLidFrame").SetValue(chest.startingLidFrame.Value+frame);
                    Draw(()=>chest.draw(batch,2,2),$"chest-{id}-{(tint==Color.White?"plain":"blue")}-{frame}");chestCases++;
                }
            }
        }
        catch(Exception ex){error=ex.ToString();}
        finally
        {
            device.SetRenderTargets(targets);device.Viewport=deviceViewport;device.ScissorRectangle=scissor;device.BlendState=blend;
            device.DepthStencilState=depth;device.RasterizerState=raster;device.SamplerStates[0]=sampler;device.Textures[0]=texture;
            Game1.random=random;Game1.viewport=viewport;Game1.currentGameTime=gameTime;
        }
        helper.Data.WriteJsonFile("outdoor-props-checks.json",new{Passed=error==null,Error=error,FloorDrawCases=floorCases,FenceDrawCases=fenceCases,ChestDrawCases=chestCases,FurnitureRotationBoundsCases=furnitureCases,SuspensionBridgeDrawCases=bridgeCases,GarbageLidBodySourceCases=garbageFrames,Items=items,FarmLoaded=false,SaveWritten=false,Scope="Native floor drawing with injected connection masks; native fence neighbor selection and gate drawing; chest opening-frame rendering, prop item source bounds, furniture rotation bounds, garbage lid/body source occupancy and suspension-bridge footstep trigger/sway drawing. No live farm, crafting, decay, garbage interaction or bridge repair quest."});
        monitor.Log(error==null?"Outdoor props checks passed.":"Outdoor props checks failed: "+error,error==null?LogLevel.Info:LogLevel.Error);
    }
}
