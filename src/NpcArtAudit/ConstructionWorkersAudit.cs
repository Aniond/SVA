using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
namespace NpcArtAudit;
internal static class ConstructionWorkersAudit
{
 public static void Run(IModHelper helper, IMonitor monitor)
 {
  try {
   const string asset="LooseSprites/Cursors";
   var texture=helper.GameContent.Load<Texture2D>(asset);
   var pixels=new Color[texture.Width*texture.Height];texture.GetData(pixels);
   var routes=new (int Event,int X,int Y,int W,int H,int Frames,int Ms)[]{
            (0, 288, 1349, 19, 28, 5, 150),
            (0, 288, 1377, 19, 28, 5, 140),
            (0, 390, 1405, 18, 32, 2, 1000),
            (2, 288, 1377, 19, 28, 5, 100),
            (2, 288, 1406, 22, 26, 2, 700),
            (2, 390, 1405, 18, 32, 2, 1500),
            (4, 383, 1378, 28, 27, 2, 400),
            (4, 288, 1406, 22, 26, 2, 350),
            (4, 390, 1405, 18, 32, 2, 1500),
            (6, 288, 1349, 19, 28, 5, 150),
            (6, 288, 1377, 19, 28, 5, 140),
            (6, 390, 1405, 18, 32, 2, 1500),
            (8, 288, 1377, 19, 28, 5, 100),
            (8, 387, 1340, 17, 37, 2, 50),
            (8, 390, 1405, 18, 32, 2, 1500),
            (10, 288, 1349, 19, 28, 5, 150),
            (10, 288, 1377, 19, 28, 5, 140),
            (10, 390, 1405, 18, 32, 2, 1000),
   };
   var steps=0;var movingSteps=0;
   foreach(var r in routes){
    for(var f=0;f<r.Frames;f++){var occupied=0;for(var y=0;y<r.H;y++)for(var x=0;x<r.W;x++)if(pixels[(r.Y+y)*texture.Width+r.X+f*r.W+x].A>0)occupied++;if(occupied==0)throw new Exception("Empty worker cell");}
    var sprite=new TemporaryAnimatedSprite(asset,new Rectangle(r.X,r.Y,r.W,r.H),Vector2.Zero,false,0f,Color.White){animationLength=r.Frames,totalNumberOfLoops=999,interval=r.Ms,scale=4f};
    if(r.X==383)sprite.motion=new Vector2(0.5f,0f);
    if(r.X==387){sprite.yPeriodic=true;sprite.yPeriodicLoopTime=100;sprite.yPeriodicRange=2;}
    for(var step=1;step<=r.Frames*2;step++){
     sprite.update(new GameTime(TimeSpan.FromMilliseconds(step*(r.Ms+1)),TimeSpan.FromMilliseconds(r.Ms+1)));
     if(sprite.sourceRect!=new Rectangle(r.X+step%r.Frames*r.W,r.Y,r.W,r.H))throw new Exception("Worker frame transition differs");
     if(r.X==383){if(Math.Abs(sprite.position.X-step*0.5f)>0.001f)throw new Exception("Lumber motion differs");movingSteps++;}steps++;
    }
   }
   helper.Data.WriteJsonFile("construction-workers-checks.json",new{Passed=true,NativeEventMappings=6,WorkerAnimationRoutes=routes.Length,NativeAnimationSteps=steps,LumberMotionSteps=movingSteps,FullOvernightEventsTriggered=false,PortraitRequired=false});
  }catch(Exception ex){helper.Data.WriteJsonFile("construction-workers-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log("Construction worker audit failed: "+ex,LogLevel.Error);}
 }
}
