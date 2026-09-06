using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
namespace NpcArtAudit;
internal static class GoldenParrotWorkerAudit
{
 public static void Run(IModHelper helper, IMonitor monitor)
 {
  try {
   const string asset="LooseSprites/Cursors_1_6";
   var texture=helper.GameContent.Load<Texture2D>(asset);
   var pixels=new Color[texture.Width*texture.Height];texture.GetData(pixels);
   for(var f=0;f<2;f++){var occupied=0;for(var y=0;y<32;y++)for(var x=0;x<28;x++)if(pixels[(89+y)*texture.Width+200+f*28+x].A>0)occupied++;if(occupied==0)throw new Exception("Missing purchase worker pose");}
   var sprite=new TemporaryAnimatedSprite(asset,new Rectangle(200,89,28,32),new Vector2(2496,2048),false,0f,Color.White){animationLength=2,totalNumberOfLoops=999,interval=700f,scale=4f,layerDepth=0.1f};
   for(var step=1;step<=12;step++){
    sprite.update(new GameTime(TimeSpan.FromMilliseconds(step*701),TimeSpan.FromMilliseconds(701)));
    if(sprite.sourceRect!=new Rectangle(200+step%2*28,89,28,32))throw new Exception("Purchase worker animation differs at step "+step+": "+sprite.sourceRect);
    if(sprite.position!=new Vector2(2496,2048))throw new Exception("Purchase worker moved");
   }
   helper.Data.WriteJsonFile("golden-parrot-worker-checks.json",new{Passed=true,OccupiedFrames=2,NativeAnimationSteps=12,IntervalMilliseconds=700,FullPurchaseEventTriggered=false,PortraitRequired=false});
  }catch(Exception ex){helper.Data.WriteJsonFile("golden-parrot-worker-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log("Golden parrot worker audit failed: "+ex,LogLevel.Error);}
 }
}

