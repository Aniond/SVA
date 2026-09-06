using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NpcArtAudit;

// DRAFT: artwork worker did not compile or run this. Isolated animation checks only.
internal static class SasquatchAudit
{
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        try
        {
            const string asset="Characters/asldkfjsquaskutanfsldk";
            var texture=helper.GameContent.Load<Texture2D>(asset);
            if(texture.Width!=256||texture.Height!=128)throw new Exception("Sasquatch native atlas geometry differs");
            var pixels=new Color[texture.Width*texture.Height];texture.GetData(pixels);
            for(var frame=0;frame<16;frame++)
            {
                var occupied=0;for(var y=0;y<48;y++)for(var x=0;x<32;x++)if(pixels[(frame/8*48+y)*256+frame%8*32+x].A>0)occupied++;
                if(occupied==0)throw new Exception("Missing Sasquatch frame "+frame);
            }
            for(var y=96;y<128;y++)for(var x=0;x<256;x++)if(pixels[y*256+x]!=Color.Transparent)throw new Exception("Native blank bottom strip changed");
            var steps=0;
            foreach(var row in new[]{0,48})foreach(var flipped in new[]{false,true})foreach(var milliseconds in row==0?new[]{90,100}:new[]{120})
            {
                var sprite=new TemporaryAnimatedSprite(asset,new Rectangle(0,row,32,48),Vector2.Zero,flipped,0f,Color.White){animationLength=8,totalNumberOfLoops=99,interval=milliseconds,scale=row==0?5.5f:4f};
                for(var step=1;step<=16;step++)
                {
                    sprite.update(new GameTime(TimeSpan.FromMilliseconds(step*(milliseconds+1)),TimeSpan.FromMilliseconds(milliseconds+1)));
                    if(sprite.sourceRect!=new Rectangle(step%8*32,row,32,48))throw new Exception("Native temporary animation frame transition differs");
                    steps++;
                }
            }
            helper.Data.WriteJsonFile("sasquatch-checks.json",new{Passed=true,OccupiedFrames=16,NativeTemporaryAnimationSteps=steps,BlankBottomRows=32,PortraitRequired=false,ActualSightingTriggered=false,FullScenePlayback=false});
            monitor.Log("Sasquatch isolated animation audit passed; actual sightings remain unverified.",LogLevel.Info);
        }
        catch(Exception ex){helper.Data.WriteJsonFile("sasquatch-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log("Sasquatch audit failed: "+ex,LogLevel.Error);}
    }
}
