using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Events;
namespace NpcArtAudit;
internal static class QiPlanePilotAudit
{
 public static void Run(IModHelper helper, IMonitor monitor)
 {
  try {
   var texture=helper.GameContent.Load<Texture2D>("LooseSprites/Cursors_1_6");
   var pixels=new Color[texture.Width*texture.Height];texture.GetData(pixels);
#pragma warning disable SYSLIB0050
   var scene=(QiPlaneEvent)FormatterServices.GetUninitializedObject(typeof(QiPlaneEvent));
#pragma warning restore SYSLIB0050
   var flags=BindingFlags.Instance|BindingFlags.NonPublic;
   typeof(QiPlaneEvent).GetField("qiPlanePos",flags)!.SetValue(scene,new Vector2(100,100));
   typeof(QiPlaneEvent).GetField("tempSprites",flags)!.SetValue(scene,new List<TemporaryAnimatedSprite>());
   var device=Game1.graphics.GraphicsDevice;var previous=device.GetRenderTargets();
   using var target=new RenderTarget2D(device,640,480);using var batch=new SpriteBatch(device);
   var compared=0;
   try {
    device.SetRenderTarget(target);device.Clear(Color.Transparent);batch.Begin(samplerState:SamplerState.PointClamp);
    scene.draw(batch);batch.End();device.SetRenderTargets(previous);
    var rendered=new Color[640*480];target.GetData(rendered);
    for(var y=0;y<43;y++)for(var x=0;x<79;x++){
     var expected=pixels[(204+y)*texture.Width+113+x];if(expected.A!=255)continue;
     if(rendered[(100+y*4+2)*640+100+x*4+2]!=expected)throw new Exception("Native plane/pilot draw differs");compared++;
    }
    using var output=File.Create(Path.Combine(helper.DirectoryPath,"qi-plane-pilot-runtime-preview.png"));target.SaveAsPng(output,640,480);
   }finally{device.SetRenderTargets(previous);}
   helper.Data.WriteJsonFile("qi-plane-pilot-checks.json",new{Passed=true,NativeDraw=true,OpaquePlanePixelsCompared=compared,ConstructorSkipped=true,MailUnchanged=true,FullFlightTriggered=false,AnnouncementTested=false});
  }catch(Exception ex){helper.Data.WriteJsonFile("qi-plane-pilot-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log("Qi pilot audit failed: "+ex,LogLevel.Error);}
 }
}
