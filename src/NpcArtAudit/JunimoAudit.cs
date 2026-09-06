using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;

namespace NpcArtAudit;
internal static class JunimoAudit
{
    public static void Run(IModHelper helper,IMonitor monitor)
    {
        try
        {
            var type=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="AbigailModern").GetType("AbigailModern.JunimoPortraits")!;
            var expression=type.GetMethod("ExpressionFor")!;
            var junimo=new Junimo();
            int Expression(NPC actor)=>(int)expression.Invoke(null,new object[]{actor})!;
            void Check(int expected){if(Expression(junimo)!=expected)throw new Exception("Junimo expression mismatch: "+expected);}
            Check(2);junimo.friendly.Value=true;Check(0);
            var speech=typeof(NPC).GetField("textAboveHeadTimer",BindingFlags.NonPublic|BindingFlags.Instance)!;
            speech.SetValue(junimo,1000);Check(1);
            junimo.holdingBundle.Value=true;Check(3);junimo.holdingStar.Value=true;Check(4);
            ((NetBool)typeof(Junimo).GetField("sayingGoodbye",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(junimo)!).Value=true;Check(5);
            var worker=new JunimoHarvester{Sprite=new AnimatedSprite("Characters/Junimo",0,16,16)};
            if(Expression(worker)!=0)throw new Exception("Harvester neutral mismatch");
            worker.Sprite.CurrentFrame=44;if(Expression(worker)!=4)throw new Exception("Harvester carrying mismatch");
            typeof(JunimoHarvester).GetField("harvestTimer",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(worker,1000);
            if(Expression(worker)!=3)throw new Exception("Harvester harvest mismatch");
            foreach(var actor in new NPC[]{junimo,worker})
            {
                var color=(NetColor)actor.GetType().GetField("color",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(actor)!;
                color.Value=Color.MediumPurple;
                if((Color)type.GetMethod("TintFor")!.Invoke(null,new object[]{actor})! !=Color.MediumPurple)throw new Exception("Native tint mismatch");
            }
            var texture=helper.GameContent.Load<Texture2D>("Characters/Junimo");
            var colors=new Color[texture.Width*texture.Height];texture.GetData(colors);
            for(var p=0;p<128*96;p++)if(colors[p].A>0&&(colors[p].R!=colors[p].G||colors[p].G!=colors[p].B))throw new Exception("Body is not tint-neutral");
            var device=Game1.graphics.GraphicsDevice;var previous=device.GetRenderTargets();
            using var target=new RenderTarget2D(device,960,640);using var batch=new SpriteBatch(device);
            try
            {
                device.SetRenderTarget(target);device.Clear(new Color(35,45,58));batch.Begin(samplerState:SamplerState.PointClamp);
                var portrait=helper.GameContent.Load<Texture2D>("Portraits/Junimo");
                var tints=new[]{Color.LimeGreen,Color.Orange,Color.Turquoise,Color.Tan,Color.Gold,Color.MediumPurple};
                for(var i=0;i<6;i++)batch.Draw(portrait,new Rectangle(i*160+16,16,128,128),new Rectangle(i%2*64,i/2*64,64,64),tints[i]);
                for(var i=0;i<48;i++)batch.Draw(texture,new Rectangle(i%8*112+24,i/8*72+176,64,64),new Rectangle(i%8*16,i/8*16,16,16),tints[i/8]);
                batch.End();device.SetRenderTargets(previous);
                using var output=File.Create(Path.Combine(helper.DirectoryPath,"junimo-runtime-preview.png"));target.SaveAsPng(output,960,640);
            }
            finally{device.SetRenderTargets(previous);}
            helper.Data.WriteJsonFile("junimo-checks.json",new{Passed=true,Expressions=6,HarvesterStates=3,NativeTintPreserved=true,GrayscaleBodies=true,FarmLoaded=false,FullScenePlayback=false});
            monitor.Log("Junimo checks passed: six expressions, three harvester states, native colors and grayscale sprite bodies.",LogLevel.Info);
        }
        catch(Exception ex){helper.Data.WriteJsonFile("junimo-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log("Junimo audit failed: "+ex,LogLevel.Error);}
    }
}
