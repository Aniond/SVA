using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;

namespace NpcArtAudit;
internal static class FishingContestantAudit
{
    public static void Run(IModHelper helper,IMonitor monitor)
    {
        RunSeason(helper,monitor,false);
        RunSeason(helper,monitor,true);
    }
    private static void RunSeason(IModHelper helper,IMonitor monitor,bool summer)
    {
        var season=summer?"summer":"winter";
        var assetSeason=summer?"Summer":"Winter";
        var count=summer?10:12;
        try
        {
            var type=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="AbigailModern").GetType("AbigailModern.FishingContestantPortraits")!;
            int Emotion(NPC npc)=>(int)type.GetMethod("ExpressionFor")!.Invoke(null,new object[]{npc})!;
            int Index(NPC npc)=>(int)type.GetMethod(summer?"SummerIndex":"WinterIndex")!.Invoke(null,new object[]{npc})!;
            var frames=new[]{0,2,3,1,2,8,9,10,11,12,6,7};
            var actors=new List<NPC>();GameLocation location=summer?new Forest():new Beach();
            var adjust=location.GetType().GetMethod("adjustDerbyFisherman",BindingFlags.NonPublic|BindingFlags.Instance)!;
            var expected=new[]{4,1,2,1,3,1,4};
            for(var i=0;i<count;i++)
            {
                var width=i==4||i is >=5 and <=9?32:16;var height=i is >=5 and <=9?32:64;
                var npc=new NPC(new AnimatedSprite("Characters/Assorted_Fishermen"+(summer?"":"_Winter"),frames[i],width,height),Vector2.Zero,-1,(summer?"":"winter_")+"derby_contestent"+i);
                adjust.Invoke(location,new object[]{npc});
                if(Index(npc)!=i||Emotion(npc)!=0||npc.Sprite.CurrentFrame!=frames[i]||npc.Sprite.SpriteWidth!=width||npc.Sprite.SpriteHeight!=height)throw new Exception("Native actor mapping mismatch "+i);
                var path="Portraits/FishingContestant"+assetSeason+i;
                if((string?)type.GetMethod("PortraitFor")!.Invoke(null,new object[]{npc})!=path)throw new Exception("Seasonal portrait routing mismatch");
                var portrait=helper.GameContent.Load<Texture2D>(path);
                if(portrait.Width!=128||portrait.Height!=192)throw new Exception("Portrait dimensions mismatch "+i);
                for(var reaction=0;reaction<7;reaction++)
                {
                    npc.showTextAboveHead(Game1.content.LoadString("Strings\\1_6_Strings:FishingDerby_Exclamation"+reaction));
                    if(Emotion(npc)!=expected[reaction])throw new Exception("Fishing reaction mismatch "+i+"/"+reaction);
                }
                npc.showTextAboveHead("unrelated text");if(Emotion(npc)!=0)throw new Exception("Unknown text reaction mismatch");
                npc.showTextAboveHead(Game1.content.LoadString("Strings\\1_6_Strings:FishingDerby_Exclamation0"),preTimer:500);
                if(Emotion(npc)!=0)throw new Exception("Premature reaction");
                npc.showTextAboveHead("expired",duration:0);if(Emotion(npc)!=0)throw new Exception("Expired reaction");
                actors.Add(npc);
            }
            foreach(var name in new[]{"Abigail",summer?"winter_derby_contestent0":"derby_contestent0",summer?"derby_contestent10":"winter_derby_contestent12",summer?"derby_contestent-1":"winter_derby_contestent-1"})if(Index(new NPC{Name=name})!=-1)throw new Exception("Unrelated NPC accepted");
            var device=Game1.graphics.GraphicsDevice;var previous=device.GetRenderTargets();
            using var target=new RenderTarget2D(device,1536,640);using var batch=new SpriteBatch(device);
            try
            {
                device.SetRenderTarget(target);device.Clear(new Color(35,45,58));batch.Begin(samplerState:SamplerState.PointClamp);
                for(var i=0;i<count;i++)
                {
                    var npc=actors[i];batch.Draw(npc.Sprite.Texture,new Rectangle(i*128+32,16,npc.Sprite.SpriteWidth*2,npc.Sprite.SpriteHeight*2),npc.Sprite.SourceRect,Color.White);
                    var portrait=helper.GameContent.Load<Texture2D>("Portraits/FishingContestant"+assetSeason+i);
                    batch.Draw(portrait,new Rectangle(i*128,176,128,192),Color.White);
                }
                batch.End();device.SetRenderTargets(previous);
                using var file=File.Create(Path.Combine(helper.DirectoryPath,"fishing-"+season+"-runtime-preview.png"));target.SaveAsPng(file,1536,640);
            }
            finally{device.SetRenderTargets(previous);}
            helper.Data.WriteJsonFile("fishing-"+season+"-checks.json",new{Passed=true,NativeActors=count,ReactionsChecked=count*7,Portraits=count,FarmLoaded=false,FullFestivalScene=false,HudProximityVerified=false});
            monitor.Log($"{season} fishing audit passed:{count} native actor layouts and{count*7} localized reactions; full festival scene remains unverified.",LogLevel.Info);
        }
        catch(Exception ex){helper.Data.WriteJsonFile("fishing-"+season+"-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log(season+" fishing audit failed: "+ex,LogLevel.Error);}
    }
}
