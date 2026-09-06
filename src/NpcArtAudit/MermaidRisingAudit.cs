using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
namespace NpcArtAudit;
internal static class MermaidRisingAudit
{
 public static void Run(IModHelper helper, IMonitor monitor)
 {
  try {
   const string asset="LooseSprites/temporary_sprites_1";
   var texture=helper.GameContent.Load<Texture2D>(asset);
   var pixels=new Color[texture.Width*texture.Height];texture.GetData(pixels);
   for(var f=0;f<3;f++){var occupied=0;for(var y=0;y<53;y++)for(var x=0;x<24;x++)if(pixels[(189+y)*texture.Width+67+f*24+x].A>0)occupied++;if(occupied==0)throw new Exception("Missing rising mermaid pose");}
   var steps=0;
   foreach(var submarine in new[]{false,true}){
    var tint=submarine?new Color(0,50,150):Color.White;
    var sprite=new TemporaryAnimatedSprite(asset,new Rectangle(67,189,24,53),new Vector2(192,640),false,0f,tint){animationLength=3,totalNumberOfLoops=100,interval=192f,scale=4f,pingPong=true,motion=new Vector2(0,submarine?-1:-4),xPeriodic=true,xPeriodicLoopTime=submarine?3500:2000,xPeriodicRange=submarine?12:32};
    for(var step=1;step<=12;step++){
     sprite.update(new GameTime(TimeSpan.FromMilliseconds(step*193),TimeSpan.FromMilliseconds(193)));
     var phase=step%4;var frame=phase<=2?phase:1;
     if(sprite.sourceRect!=new Rectangle(67+frame*24,189,24,53))throw new Exception("Rising mermaid ping-pong differs");
     if(Math.Abs(sprite.position.Y-(640-step*(submarine?1:4)))>0.001f)throw new Exception("Rising mermaid vertical motion differs");
     if(sprite.color!=tint)throw new Exception("Native mermaid tint changed");steps++;
    }
   }
   helper.Data.WriteJsonFile("mermaid-rising-checks.json",new{Passed=true,OccupiedFrames=3,NativeAnimationSteps=steps,IntervalMilliseconds=192,NativeTintRetained=true,FullPerformanceTriggered=false,SubmarineEncounterTriggered=false});
  }catch(Exception ex){helper.Data.WriteJsonFile("mermaid-rising-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log("Rising mermaid audit failed: "+ex,LogLevel.Error);}
 }
}
