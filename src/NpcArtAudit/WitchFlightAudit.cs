using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Events;
namespace NpcArtAudit;
internal static class WitchFlightAudit
{
 public static void Run(IModHelper helper, IMonitor monitor)
 {
  try {
   var device=Game1.graphics.GraphicsDevice;
   using var batch=new SpriteBatch(device);
   using var target=new RenderTarget2D(device,136,116);
   var flags=BindingFlags.Instance|BindingFlags.NonPublic;
   var draws=0;
   foreach(var golden in new[]{false,true}){
    var asset=golden?"LooseSprites/Cursors2":"LooseSprites/Cursors";
    var texture=helper.GameContent.Load<Texture2D>(asset);
    var pixels=new Color[texture.Width*texture.Height];texture.GetData(pixels);
    var witch=new WitchEvent{goldenWitch=golden};
    typeof(WitchEvent).GetField("witchPosition",flags)!.SetValue(witch,new Vector2(Game1.viewport.X,Game1.viewport.Y));
    foreach(var frame in new[]{0,1,0}){
     typeof(WitchEvent).GetField("witchFrame",flags)!.SetValue(witch,frame);
     var previous=device.GetRenderTargets();
     try {
      device.SetRenderTarget(target);device.Clear(Color.Transparent);
      batch.Begin(blendState:BlendState.Opaque,samplerState:SamplerState.PointClamp);
      witch.draw(batch);batch.End();device.SetRenderTargets(previous);
      var actual=new Color[136*116];target.GetData(actual);
      var sx=golden?215:277;var sy=(golden?262:1886)+frame*29;
      for(var y=0;y<116;y++)for(var x=0;x<136;x++)if(actual[y*136+x]!=pixels[(sy+y/4)*texture.Width+sx+x/4])throw new Exception("Native witch draw differs");
      draws++;
     }finally{device.SetRenderTargets(previous);}
    }
   }
   helper.Data.WriteJsonFile("witch-flight-checks.json",new{Passed=true,NativeDraws=draws,Variants=2,SelectedFrameSequence=new[]{0,1,0},NativeTickTimingExecuted=false,FullVisitTriggered=false,LocationChangesApplied=false,PortraitRequired=false});
  }catch(Exception ex){helper.Data.WriteJsonFile("witch-flight-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log("Witch draw audit failed: "+ex,LogLevel.Error);}
 }
}
