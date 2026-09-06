using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;

namespace NpcArtAudit;
internal static class MermaidAudit
{
    public static void Run(IModHelper helper,IMonitor monitor)
    {
        try
        {
            var type=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="AbigailModern").GetType("AbigailModern.MermaidPortrait")!;
            var island=new IslandSouthEast();var states=new[]{island.mermaidIdle,island.mermaidWave,island.mermaidReward,island.mermaidDance};var expected=new[]{0,1,4,5};
            for(var i=0;i<states.Length;i++)
            {
                island.currentMermaidAnimation=states[i];
                var actual=(int)type.GetMethod("ExpressionFor")!.Invoke(null,new object[]{island})!;
                if(actual!=expected[i]||!ReferenceEquals(island.currentMermaidAnimation,states[i])||island.mermaidPuzzleFinished.Value)throw new Exception("Expression selection changed native animation or reward state");
            }
            var atlas=helper.GameContent.Load<Texture2D>("LooseSprites/temporary_sprites_1");
            var device=Game1.graphics.GraphicsDevice;var previous=device.GetRenderTargets();using var target=new RenderTarget2D(device,784,430);using var batch=new SpriteBatch(device);
            try
            {
                device.SetRenderTarget(target);device.Clear(new Color(30,43,60));batch.Begin(samplerState:SamplerState.PointClamp);
                for(var i=0;i<7;i++)batch.Draw(atlas,new Rectangle(i*112,0,112,144),new Rectangle(304+i*28,592,28,36),Color.White);
                var portraits=helper.GameContent.Load<Texture2D>("Portraits/Mermaid");for(var i=0;i<4;i++)batch.Draw(portraits,new Rectangle(i*192,164,128,128),new Rectangle(expected[i]%2*64,expected[i]/2*64,64,64),Color.White);
                for(var i=0;i<9;i++)batch.Draw(atlas,new Rectangle(i*84,310,84,108),new Rectangle(i*28,80,28,36),Color.White);
                batch.End();device.SetRenderTargets(previous);using var stream=File.Create(Path.Combine(helper.DirectoryPath,"mermaid-runtime-preview.png"));target.SaveAsPng(stream,target.Width,target.Height);
            }
            finally{device.SetRenderTargets(previous);}
            helper.Data.WriteJsonFile("mermaid-checks.json",new{Passed=true,NativeAnimationExpressionMapping=true,RewardStateUnchanged=true,RuntimeTexturesRendered=true,FullSceneVerified=false,FarmLoaded=false});
            monitor.Log("Mermaid expression mapping and runtime artwork render passed; full scene remains unverified.",LogLevel.Info);
        }
        catch(Exception ex){helper.Data.WriteJsonFile("mermaid-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log($"Mermaid audit failed: {ex}",LogLevel.Error);}
    }
}
